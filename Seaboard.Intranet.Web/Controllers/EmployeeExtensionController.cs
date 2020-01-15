using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.BusinessLogic;
using Microsoft.AspNet.Identity;
using Seaboard.Intranet.Data.Repository;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class EmployeeExtensionController : Controller
    {
        private readonly SeaboContext _db;
        private readonly GenericRepository _repository;

        public EmployeeExtensionController()
        {
            _db = new SeaboContext();
            _repository = new GenericRepository(_db);
        }

        [Authorize]
        public ActionResult Index()
        {

            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "EmployeeExtension", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            var employeeExtensions = _repository.GetAll<EmployeeExtension>(includeProperties: "Department");
            return View(employeeExtensions.ToList());
        }

        [Authorize]
        public ActionResult Create()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "EmployeeExtension", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            var departments = _repository.GetAll<Department>();
            ViewBag.DepartmentId = new SelectList(departments, "DepartmentId", "Description");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,LastName,ExtensionNumber,CellPhone,DepartmentId")] EmployeeExtension employeeExtension)
        {
            if (ModelState.IsValid)
            {
                if (_repository.Add<EmployeeExtension>(employeeExtension))
                {
                    return RedirectToAction("Index");
                }
            }

            var departments = _repository.GetAll<Department>();
            ViewBag.DepartmentId = new SelectList(departments, "DepartmentId", "Description");
            return View(employeeExtension);
        }

        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "EmployeeExtension", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            EmployeeExtension employeeExtension = _repository.GetBy< EmployeeExtension>(id);
            if (employeeExtension == null)
            {
                return HttpNotFound();
            }

            var departments = _repository.GetAll<Department>();
            ViewBag.DepartmentId = new SelectList(departments, "DepartmentId", "Description", employeeExtension.DepartmentId);
            return View(employeeExtension);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,LastName,ExtensionNumber,CellPhone,DepartmentId")] EmployeeExtension employeeExtension)
        {
            if (ModelState.IsValid)
            {
                if(_repository.Update<EmployeeExtension>(employeeExtension, employeeExtension.Id))
                                return RedirectToAction("Index");
            }
            ViewBag.DepartmentId = new SelectList(_db.Departments, "DepartmentId", "Description", employeeExtension.DepartmentId);
            return View(employeeExtension);
        }

        [Authorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            EmployeeExtension employeeExtension = _repository.GetBy<EmployeeExtension>(id);
            if (employeeExtension == null)
            {
                return HttpNotFound();
            }
            return View(employeeExtension);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            _repository.Delete<EmployeeExtension>(id) ;
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult ListExtension()
        {
            var list = _repository.GetAll<EmployeeExtension>(includeProperties: "Department");
            return View(list.ToList());
        }
    }
}
