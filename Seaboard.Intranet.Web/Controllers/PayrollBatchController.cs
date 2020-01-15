using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Seaboard.Intranet.Domain.ViewModels;
using Seaboard.Intranet.Domain;
using System;
using Seaboard.Intranet.Data.Repository;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class PayrollBatchController : Controller
    {
        readonly GenericRepository _repository;
        int _xStatus;

        public PayrollBatchController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PayrollBatch", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            return View();
        }

        // valida que no se inserte mas informacion a un lote ya transferido 
        public ActionResult BatchVerify(string batch)
        {
            var sqlQuery = "SELECT BACHNUMB FROM " + Helpers.InterCompanyId + ".dbo.UPR10301 WHERE BACHNUMB = '" + batch + "'";
            var batchNma = _repository.ExecuteScalarQuery<string>(sqlQuery);

            _xStatus = !string.IsNullOrEmpty(batchNma) ? 1 : 0;
            return new JsonResult { Data = new { status = _xStatus } };
        }

        public ActionResult SavePayrollBatch(string batch, string batchDesc, DateTime postedDate)
        {
            try
            {
                var sqlQuery = "SELECT COUNT(BATCHID) FROM " + Helpers.InterCompanyId + ".dbo.TEMPUPR10302 WHERE BATCHID = '" + batch + "'";

                var aModel = _repository.ExecuteScalarQuery<int>(sqlQuery);

                if (aModel >= 1)
                {
                    _repository.ExecuteCommand($"INTRANET.dbo.InsertPayrollBatch '{Helpers.InterCompanyId}','{batch}','{batchDesc}','{postedDate.ToString("yyyyMMdd")}'");
                    _xStatus = 1;
                }
                else
                {
                    _xStatus = 0;
                }
            }
            catch
            {
                _xStatus = 2;
            }
            return new JsonResult { Data = new { status = _xStatus } };
        }

        [HttpPost]
        public JsonResult UploadFile(HttpPostedFileBase fileData, string batch, string detail, string date)
        {

            var fileName = Path.GetFileName(fileData.FileName);
            var excelPath = Server.MapPath("~/Content/File/");
            var path = Path.Combine(excelPath, fileName ?? "");

            //batch header
            _repository.ExecuteCommand($"INTRANET.dbo.insertBatchHeader '{Helpers.InterCompanyId}','{batch}','{detail}','{date}'");

            //Delete si existe
            var sqlQuery = $"DELETE FROM {Helpers.InterCompanyId}.dbo.TEMPUPR10302 WHERE BATCHID = '{batch}'";
            _repository.ExecuteQuery<PayrollBatchViewModel>(sqlQuery);

            if (!Directory.Exists(excelPath))
            {
                Directory.CreateDirectory(excelPath);
            }
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            fileData.SaveAs(path);

            //Excel file
            if (fileData.FileName.EndsWith("xls") || fileData.FileName.EndsWith("xlsx"))
            {
                FileLogger.GetExcelData(path, batch);
            }

            //txt file
            if (fileData.FileName.EndsWith("txt"))
            {
                FileLogger.GetTxtData(path, batch);
            }

            //Delete null rows
            sqlQuery = "DELETE FROM " + Helpers.InterCompanyId + ".dbo.TEMPUPR10302 WHERE EMPLOYEEID IS NULL";
            _repository.ExecuteQuery<PayrollBatchViewModel>(sqlQuery);

            return new JsonResult { Data = new { status = "OK" } };
        }

        public JsonResult GetPayrollBatch(string batch)
        {
            var query = "SELECT LTRIM(RTRIM(A.BATCHID)) AS [BatchId], "
                           + "LTRIM(RTRIM(A.EMPLOYEEID)) AS [EmployeeId], "
                           + "LTRIM(RTRIM(A.TRANSACTIONTYPE)) AS [TransactionType], "
                           + "LTRIM(RTRIM(A.PAYCODE)) AS [PayCode], "
                           + "CONVERT(VARCHAR(12),CONVERT(DATETIME,A.BEGINNINGDATE),101) AS [BeginningDate], "
                           + "CONVERT(VARCHAR(12),CONVERT(DATETIME,A.ENDINGDATE),101) AS [EndingDate], "
                           + "LTRIM(RTRIM(A.UNIST)) AS [Units], "
                           + "ISNULL(LTRIM(RTRIM(A.NOTE)),'') AS [Note], '' AS [UserName] "
                           + "FROM ( SELECT BATCHID,EMPLOYEEID,TRANSACTIONTYPE,PAYCODE, "
                           + "BEGINNINGDATE,ENDINGDATE,UNIST,NOTE FROM " + Helpers.InterCompanyId + ".dbo.TEMPUPR10302 "
                           + "UNION ALL "
                           + "SELECT BACHNUMB, EMPLOYID, COMPTRTP, UPRTRXCD, TRXBEGDT, TRXENDDT, "
                           + "0, '' FROM " + Helpers.InterCompanyId + ".dbo.HISTEMPUPR10302) A "
                           + "WHERE BATCHID = '" + batch + "'";

            var b = _repository.ExecuteQuery<PayrollBatchViewModel>(query);

            return Json(b, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Print(string id, string batch)
        {
            try
            {
                string xStatus = "";
                ReportHelper.Export(Helpers.ReportPath + "Nomina", Server.MapPath("~/PDF/Nomina/") + id + ".pdf",
                    $"INTRANET.dbo.UPR000000Rpt '{Helpers.InterCompanyId}','{batch}','{Account.GetAccount(User.Identity.GetUserName()).UserId}'", 13, ref xStatus);
            }
            catch (Exception ex)
            {
                return View(ex.Message);
            }
            return File("~/PDF/Nomina/" + id + ".pdf", "application/pdf");
        }
    }
}

