using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Seaboard.Intranet.Data.Repository;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class PaymentVoucherController : Controller
    {
        private readonly GenericRepository _repository;

        public PaymentVoucherController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {

            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PaymentVoucher", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            List<Lookup> year = new List<Lookup>();
            string sqlQuery = "SELECT A.EMPLOYID EmployeeId, A.BRTHDATE BirthDay, RTRIM(B.ADDRESS1) + ' ' + RTRIM(B.ADDRESS2) + ' ' + RTRIM(B.ADDRESS3) PersonAddress, "
            + "RTRIM(B.CITY) City, RTRIM(B.COUNTRY) Country, A.STRTDATE StartDate, RTRIM(A.USERDEF1) PersonId, "
            + "RTRIM(A.FRSTNAME) + ' ' + RTRIM(A.MIDLNAME) + ' ' + RTRIM(A.LASTNAME) PersonName, C.DSCRIPTN Department, D.DSCRIPTN JobTitle "
            + "FROM " + Helpers.InterCompanyId + ".dbo.UPR00100 A "
            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR00102 B "
            + "ON A.EMPLOYID = B.EMPLOYID "
            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40300 C "
            + "ON A.DEPRTMNT = C.DEPRTMNT "
            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40301 D "
            + "ON A.JOBTITLE = D.JOBTITLE "
            + "WHERE A.EMPLOYID = '" + Account.GetAccount(User.Identity.GetUserName()).EmployeeId + "'";

            var employee = _repository.ExecuteScalarQuery<EmployeeViewModel>(sqlQuery);

            ViewBag.EmployeeDetail = _repository.ExecuteQuery<EmployeeDetailViewModel>
                ("INTRANET.dbo.EmployeeInfoInitial @INTERID = '" + Helpers.InterCompanyId + "', @EMPLOYID = '" + Account.GetAccount(User.Identity.GetUserName()).EmployeeId + "'").ToList();

            ViewBag.Year = new SelectList(year, "Id", "Descripción");
            return View(employee);
        }

        [HttpPost]
        public ActionResult ListPayroll(string from, string to)
        {
            try
            {
                List<PayrollHistoryViewModel> payrolls = new List<PayrollHistoryViewModel>();
                string sqlQuery = "SELECT DISTINCT CASE MONTH(A.CHEKDATE) WHEN 1 THEN 'ENERO' WHEN 2 THEN 'FEBRERO' WHEN 3 THEN 'MARZO' WHEN 4 THEN 'ABRIL' "
                    + "WHEN 5 THEN 'MAYO' WHEN 6 THEN 'JUNIO' WHEN 7 THEN 'JULIO' WHEN 8 THEN 'AGOSTO' WHEN 9 THEN 'SEPTIEMBRE' WHEN 10 THEN 'OCTUBRE' "
                    + "WHEN 11 THEN 'NOVIEMBRE' ELSE 'DICIEMBRE' END MonthDescription, CONVERT(INT, MONTH(A.CHEKDATE)) Month, CONVERT(NVARCHAR(10), CONVERT(date, A.CHEKDATE, 106),103) PeriodId, CONVERT(INT, A.YEAR1) Year, "
                    + "A.AUCTRLCD PayrollId "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.UPR30300 A "
                    + "INNER JOIN INTRANET.dbo.PAYROLL B ON A.AUCTRLCD = B.AUCTRLCD "
                    + "WHERE A.YEAR1 BETWEEN '" + from + "' AND '" + to + "' AND A.EMPLOYID = '" + Account.GetAccount(User.Identity.GetUserName()).EmployeeId + "'"
                    + "ORDER BY Month, Year";
                payrolls = _repository.ExecuteQuery<PayrollHistoryViewModel>(sqlQuery).ToList();
                return Json(payrolls, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public ActionResult Print(string payrollId)
        {
            bool status = true;
            try
            {
                string from = _repository.ExecuteScalarQuery<string>("SELECT CONVERT(NVARCHAR(8), TRXBEGDT, 112) FROM " + Helpers.InterCompanyId + ".dbo.UPR30300 WHERE AUCTRLCD = '" + payrollId + "'");
                string to = _repository.ExecuteScalarQuery<string>("SELECT CONVERT(NVARCHAR(8), TRXENDDT, 112) FROM " + Helpers.InterCompanyId + ".dbo.UPR30300 WHERE AUCTRLCD = '" + payrollId + "'");
                string xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Volante", Server.MapPath("~/PDF/Volante/") + "Volante" + ".pdf",
                    String.Format("LODYNDEV.dbo.VOUPR30100R1 '{0}','{1}','{2}','{3}','{4}'",
                    Helpers.InterCompanyId, from, to, Account.GetAccount(User.Identity.GetUserName()).EmployeeId, Account.GetAccount(User.Identity.GetUserName()).UserId), 0, ref xStatus);

                status = true;
            }
            catch
            {
                status = false;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult PayrollView()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PaymentVoucher", "PayrollView"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult ListPayrollAccess()
        {
            try
            {
                List<Lookup> payrolls = new List<Lookup>();
                string sqlQuery = "SELECT DISTINCT A.AUCTRLCD Id, CONVERT(NVARCHAR(20), A.CHEKDATE, 103) Descripción, CONVERT(NVARCHAR(20), LEN(ISNULL(B.AUCTRLCD,''))) DataExtended, A.CHEKDATE " +
                    "FROM " + Helpers.InterCompanyId + ".dbo.UPR30100 A " +
                    "LEFT JOIN INTRANET..PAYROLL B ON A.AUCTRLCD = B.AUCTRLCD " +
                    "WHERE A.AUCTRLCD NOT IN ('UPRCC00000173') " +
                    "ORDER BY A.CHEKDATE DESC";
                payrolls = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();
                return Json(payrolls, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public ActionResult SavePayroll(List<Lookup> payrolls)
        {
            string xStatus;
            try
            {
                var sqlQuery = "DELETE INTRANET.dbo.PAYROLL";
                _repository.ExecuteCommand(sqlQuery);
                foreach (var item in payrolls)
                {
                    sqlQuery = "INSERT INTO INTRANET.dbo.PAYROLL(AUCTRLCD) VALUES ('" + item.Id + "')";
                    _repository.ExecuteCommand(sqlQuery);
                }

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return new JsonResult { Data = new { status = xStatus } };
        }
    }
}