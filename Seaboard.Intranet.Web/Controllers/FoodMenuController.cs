using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;
using Seaboard.Intranet.BusinessLogic;
using Microsoft.AspNet.Identity;
using Seaboard.Intranet.Data.Repository;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class FoodMenuController : Controller
    {
        private readonly SeaboContext _db;
        private readonly GenericRepository _repository;

        public FoodMenuController()
        {
            _db = new SeaboContext();
            _repository = new GenericRepository(_db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "FoodMenu", "Index"))
                return RedirectToAction("NotPermission", "Home");

            var today = DateTime.Today;
            var currentDayOfWeek = (int)today.DayOfWeek;
            var sunday = today.AddDays(-currentDayOfWeek);
            var monday = sunday.AddDays(1);
            if (currentDayOfWeek == 0)
                monday = monday.AddDays(-7);

            var last = monday.AddDays(7);
            var foodMenus = _repository.GetAll<FoodMenu>(m => m.FoodMenuDate >= monday && m.FoodMenuDate <= last).OrderBy(m => Convert.ToDateTime(m.FoodMenuDate));
            return View(foodMenus.ToList());
        }

        public ActionResult Create()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "FoodMenu", "Index"))
                return RedirectToAction("NotPermission", "Home");

            return View();
        }

        [HttpPost]
        public JsonResult Save(FoodMenu foodMenu)
        {
            var status = false;

            try
            {
                if (_repository.GetAll<FoodMenu>(m => m.FoodMenuId == foodMenu.FoodMenuId).Any())
                    _repository.Delete<FoodMenu>(foodMenu.FoodMenuId);

                _repository.Add<FoodMenu>(foodMenu);
                status = true;
            }
            catch
            {
                status = false;
            }

            return new JsonResult { Data = new { status = status } };
        }

        public ActionResult Edit(int? id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "FoodMenu", "Index"))
                return RedirectToAction("NotPermission", "Home");

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            
            var foodMenu = _db.FoodMenus.Find(id);
            if (foodMenu == null)
                return HttpNotFound();
            
            var foodLines = _repository.GetAll<FoodMenuLine>(m => m.FoodMenuId == id, includeProperties: "Food").ToList();
            var foodMenuLines = new List<FoodMenuLineViewModel>();

            foreach (var item in foodLines)
                foodMenuLines.Add(new FoodMenuLineViewModel
                {
                    FoodId = item.FoodId,
                    FoodName = item.Food.FoodName,
                    FoodLineScheduleId = item.FoodLineScheduleId.ToString()
                });

            ViewBag.FoodMenuLines = foodMenuLines;
            return View(foodMenu);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            
            var foodMenu = _db.FoodMenus.Find(id);
            if (foodMenu == null)
                return HttpNotFound();
            
            return View(foodMenu);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var foodMenu = _db.FoodMenus.Find(id);
            _db.FoodMenus.Remove(foodMenu);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult FoodMenuWeek()
        {
            var today = DateTime.Today;
            var currentDayOfWeek = (int)today.DayOfWeek;
            var sunday = today.AddDays(-currentDayOfWeek);
            var monday = sunday.AddDays(1);
            if (currentDayOfWeek == 0)
                monday = monday.AddDays(-7);

            var last = monday.AddDays(7);
            var foodMenus = _repository.GetAll<FoodMenu>(m => m.FoodMenuDate >= monday && m.FoodMenuDate <= last).OrderBy(m => Convert.ToDateTime(m.FoodMenuDate));

            foreach (var item in foodMenus)
                item.FoodMenuLines = _repository.GetAll<FoodMenuLine>(i => i.FoodMenuId == item.FoodMenuId, includeProperties: "Food").ToList();

            return View(foodMenus.ToList());
        }
    }
}