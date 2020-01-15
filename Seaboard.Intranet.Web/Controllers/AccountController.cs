using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;
using Seaboard.Intranet.Web.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Seaboard.Intranet.Data.Repository;
using Seaboard.Intranet.Domain;

namespace Seaboard.Intranet.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly GenericRepository _repository;

        public AccountController() : this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }

        public UserManager<ApplicationUser> UserManager { get; }

        [Authorize]
        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Account", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            var users = _repository.GetAll<User>().ToList();
            return View(users);
        }

        public virtual ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);
            var username = model.Username.Trim();
            var password = model.Password;

            var userExists = _repository.GetAll<User>(user => user.UserName == username && user.Inactive == 0).Any();
            if (userExists)
            {
                var isActiveDirectory =_repository.GetAll<User>(user => user.UserName == username && user.Inactive == 0).FirstOrDefault()?.IsActiveDirectory ?? false;
                if (isActiveDirectory)
                {
                    var ctx = new PrincipalContext(ContextType.Domain);
                    var isAuthenticated = ctx.ValidateCredentials(model.Username, model.Password);
                    if (isAuthenticated)
                    {
                        var loginInfo = GetWindowsLoginInfo(username);
                        var user = await UserManager.FindAsync(loginInfo);
                        if (user != null)
                        {
                            var cookie = Request.Cookies["UserAccount"];
                            if (cookie == null)
                            {
                                cookie = new HttpCookie("UserAccount");
                                cookie.Values["username"] = model.Username;
                                cookie.Values["password"] = model.Password;
                            }
                            else
                            {
                                cookie.Values["username"] = model.Username;
                                cookie.Values["password"] = model.Password;
                            }

                            cookie.Expires = DateTime.UtcNow.AddDays(30);
                            cookie.Path = "/";
                            cookie.Expires = DateTime.UtcNow.AddYears(1);
                            cookie.Domain = Request.Url.Host;
                            Response.Cookies.Set(cookie);

                            await SignInAsync(user, false);
                            return RedirectToLocal(returnUrl);
                        }
                        else
                        {
                            var cookie = Request.Cookies["UserAccount"];
                            if (cookie == null)
                            {
                                cookie = new HttpCookie("UserAccount");
                                cookie.Values["username"] = model.Username;
                                cookie.Values["password"] = model.Password;
                            }
                            else
                            {
                                cookie.Values["username"] = model.Username;
                                cookie.Values["password"] = model.Password;
                            }

                            cookie.Expires = DateTime.UtcNow.AddDays(30);
                            cookie.Path = "/";
                            cookie.Expires = DateTime.UtcNow.AddYears(1);
                            cookie.Domain = Request.Url.Host;
                            Response.Cookies.Set(cookie);

                            var info = GetWindowsLoginInfo(username);
                            var appUser = new ApplicationUser() {UserName = username};
                            await UserManager.CreateAsync(appUser);
                            await UserManager.AddLoginAsync(appUser.Id, info);
                            user = await UserManager.FindAsync(loginInfo);

                            await SignInAsync(user, false);
                            return RedirectToLocal(returnUrl);
                        }
                    }

                    ModelState.AddModelError("", @"El usuario o contraseña estan incorrectos");
                }
                else
                {
                    var isValid = _repository.GetAll<User>(u => u.UserName == username && u.Password == password && u.Inactive == 0).Any();
                    if (isValid)
                    {
                        var user = await UserManager.FindAsync(model.Username, model.Password);
                        if (user != null)
                        {
                            var cookie = Request.Cookies["UserAccount"];
                            if (cookie == null)
                            {
                                cookie = new HttpCookie("UserAccount");
                                cookie.Values["username"] = model.Username;
                                cookie.Values["password"] = model.Password;
                            }
                            else
                            {
                                cookie.Values["username"] = model.Username;
                                cookie.Values["password"] = model.Password;
                            }

                            cookie.Expires = DateTime.UtcNow.AddDays(30);
                            cookie.Path = "/";
                            cookie.Expires = DateTime.UtcNow.AddYears(1);
                            cookie.Domain = Request.Url.Host;
                            Response.SetCookie(cookie);

                            await SignInAsync(user, false);
                            return RedirectToLocal(returnUrl);
                        }
                        else
                        {
                            var cookie = Request.Cookies["UserAccount"];
                            if (cookie == null)
                            {
                                cookie = new HttpCookie("UserAccount");
                                cookie.Values["username"] = model.Username;
                                cookie.Values["password"] = model.Password;
                            }
                            else
                            {
                                cookie.Values["username"] = model.Username;
                                cookie.Values["password"] = model.Password;
                            }

                            cookie.Expires = DateTime.UtcNow.AddDays(30);
                            cookie.Path = "/";
                            cookie.Expires = DateTime.UtcNow.AddYears(1);
                            cookie.Domain = Request.Url.Host;
                            Response.SetCookie(cookie);

                            var userAsp = new ApplicationUser() {UserName = username};
                            await UserManager.CreateAsync(userAsp, password);
                            user = await UserManager.FindAsync(username, password);

                            await SignInAsync(user, false);
                            return RedirectToLocal(returnUrl);
                        }
                    }

                    ModelState.AddModelError("", @"El usuario o contraseña estan incorrectos");
                }

            }
            else
            {
                ModelState.AddModelError("", @"El usuario o contraseña estan incorrectos");
            }

            return View(model);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> SaveUser(User user)
        {
            bool xStatus;
            try
            {
                var oldPassword = "";
                if (_repository.GetAll<User>(m => m.UserId == user.UserId).Any())
                {
                    if (_repository.GetAll<User>(m => m.UserId == user.UserId).First().IsActiveDirectory == false)
                    {
                        oldPassword = _repository.GetAll<User>(m => m.UserId == user.UserId).First().Password;
                    }
                }
                _repository.ExecuteCommand("DELETE FROM INTRANET.dbo.USERPERMISSIONS WHERE USERID = '" + user.UserName +
                                           "'");
                _repository.ExecuteCommand("DELETE FROM INTRANET.dbo.USERS WHERE USERID = '" + user.UserName + "'");
                var sqlQuery =
                    "INSERT INTO INTRANET.dbo.USERS([UserId],[UserName],[Password],[FirstName],[LastName],[Department], "
                    + "[JobTitle],[EmployeeId],[Email],[Identification],[IsActiveDirectory],[Inactive]) "
                    + "VALUES ('" + user.UserName + "','" + user.UserId + "','" + user.Password + "','" +
                    user.FirstName + "','" + user.LastName + "', "
                    + "'" + user.Department + "','" + user.JobTitle + "','" + user.EmployeeId + "','" + user.Email +
                    "','" + user.Identification + "', "
                    + "'" + user.IsActiveDirectory + "', '" + user.Inactive + "')";

                if (user.IsActiveDirectory)
                {
                    var loginInfo = GetWindowsLoginInfo(user.UserId);
                    var userinfo = await UserManager.FindAsync(loginInfo);
                    if (userinfo == null)
                    {
                        var appUser = new ApplicationUser() {UserName = user.UserId};
                        await UserManager.CreateAsync(appUser);
                        await UserManager.AddLoginAsync(appUser.Id, loginInfo);
                    }
                }
                else
                {
                    var userinfo = await UserManager.FindAsync(user.UserId, oldPassword);
                    if (userinfo == null)
                    {
                        var userAsp = new ApplicationUser() {UserName = user.UserId};
                        await UserManager.CreateAsync(userAsp, user.Password);
                    }

                    if (userinfo != null)
                    {
                        if (oldPassword != user.Password)
                        {
                            await UserManager.ChangePasswordAsync(userinfo.Id, oldPassword, user.Password);
                        }
                    }
                }
                _repository.ExecuteCommand(sqlQuery);

                foreach (var item in user.UserPermissions)
                {
                    sqlQuery = "INSERT INTO INTRANET.dbo.USERPERMISSIONS([UserId],[PermissionId]) "
                               + "VALUES ('" + user.UserName + "','" + item.PermissionId + "')";
                    _repository.ExecuteCommand(sqlQuery);
                }

                xStatus = true;

            }
            catch
            {
                xStatus = false;
            }

            return new JsonResult {Data = new {status = xStatus } };
        }

        public JsonResult Delete(string userId, string userName)
        {
            bool xStatus;
            try
            {
                _repository.ExecuteCommand("DELETE FROM INTRANET.dbo.USERPERMISSIONS WHERE USERID = '" + userName +
                                           "'");
                _repository.ExecuteCommand("DELETE FROM INTRANET.dbo.USERS WHERE USERID = '" + userName + "'");

                var user = _repository.ExecuteScalarQuery<string>(
                    "SELECT Id FROM INTRANET.dbo.AspNetUsers WHERE UserName = '" + userId + "'");
                if (user != null)
                {
                    _repository.ExecuteCommand(
                        "DELETE FROM INTRANET.dbo.AspNetUserLogins WHERE UserId = '" + user + "'");
                }

                _repository.ExecuteCommand("DELETE FROM INTRANET.dbo.AspNetUsers WHERE UserName = '" + userId + "'");

                xStatus = true;
            }
            catch
            {
                xStatus = false;
            }

            return new JsonResult {Data = new {status = xStatus } };
        }

        public ActionResult Edit(string id)
        {
            var user = _repository.GetAll<User>(u => u.EmployeeId == id).FirstOrDefault();
            ViewBag.Password = user?.Password;
            return View(user);
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (string.IsNullOrEmpty(returnUrl))
                return RedirectToAction("Index", "Home");
            return Redirect(returnUrl);
        }

        public virtual ActionResult Logoff()
        {
            AuthenticationManager.SignOut();

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public ActionResult UserPermissionReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Account", "UserPermission"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult UserPermissionReport(string filterFrom, string filterTo, int typeFilter)
        {
            var status = "";
            try
            {
                status = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "UserPermission.pdf",
                    String.Format("INTRANET.dbo.UserPermissionReport '{0}','{1}','{2}'",typeFilter, filterFrom, filterTo), 21, ref status);
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult CopyPermission(string userIdFrom, string userIdTo)
        {
            bool xStatus;

            try
            {
                _repository.ExecuteCommand("DELETE INTRANET.dbo.USERPERMISSIONS WHERE USERID = '" + userIdTo + "'");
                _repository.ExecuteCommand("INSERT INTO INTRANET.dbo.USERPERMISSIONS (UserId, PermissionId) SELECT '" +
                                           userIdTo + "', PermissionId FROM INTRANET.dbo.USERPERMISSIONS WHERE USERID = '" + userIdFrom + "'");
                xStatus = true;
            }
            catch (Exception)
            {
                xStatus = false;
            }

            return new JsonResult {Data = new {status = xStatus } };
        }

        #region Password

        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public JsonResult VerifyPassword(string oldPassword)
        {
            var xStatus = _repository.GetAll<User>(u => u.UserId == Account.GetAccount(User.Identity.GetUserName()).UserId &&
                                                         u.Password == oldPassword).Any();

            return new JsonResult {Data = new {status = xStatus } };
        }

        [HttpPost]
        public async Task<ActionResult> ChangePass(string oldPassword, string newPassword)
        {
            bool xStatus;

            try
            {
                _repository.ExecuteCommand("UPDATE INTRANET.dbo.USERS SET Password = '" + newPassword +
                                           "' WHERE USERID = '" +
                                           Account.GetAccount(User.Identity.GetUserName()).UserId + "'");
                await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), oldPassword, newPassword);
                xStatus = true;
            }
            catch (Exception)
            {
                xStatus = false;
            }

            return new JsonResult {Data = new {status = xStatus } };
        }

        #endregion

        [HttpPost]
        public ActionResult LoadPermissions(string userId)
        {
            try
            {
                var sqlQuery = "SELECT DISTINCT A.PermissionId, A.Name, A.Controller, A.Action, "
                               + "CASE LEN(ISNULL(B.UserId, '')) WHEN 0 THEN 0 ELSE 1 END ActivePermission "
                               + "FROM INTRANET.dbo.PERMISSIONS A "
                               + "LEFT JOIN INTRANET.dbo.USERPERMISSIONS B "
                               + "ON A.PermissionId = B.PermissionId AND B.UserId = '" + userId + "'";

                var permissions = _repository.ExecuteQuery<UserPermissionViewModel>(sqlQuery).ToList();
                return Json(permissions, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        #region Helpers

        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties() {IsPersistent = isPersistent}, identity);
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private static UserLoginInfo GetWindowsLoginInfo(string userName)
        {
            var ctx = new PrincipalContext(ContextType.Domain);
            var user = UserPrincipal.FindByIdentity(ctx, userName);
            var userSid = user?.Sid;
            return new UserLoginInfo("Windows", userSid?.ToString());
        }

        #endregion
    }

    public class LoginViewModel
    {
        [Required, AllowHtml]
        public string Username { get; set; }

        [Required]
        [AllowHtml]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; }

        public bool RemenberMe { get; set; }
    }
}