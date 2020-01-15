using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Seaboard.Intranet.Data.Repository;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class PettyCashController : Controller
    {
        private readonly GenericRepository _repository;

        public PettyCashController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PettyCash", "Index"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = "SELECT DISTINCT A.DEPRTMDS FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                              + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                              + "WHERE RTRIM(B.USERID) = '" + Account.GetAccount(User.Identity.GetUserName()).UserId +
                              "'";

            string filter = "";

            string[] departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

            foreach (var item in departments)
            {
                if (filter.Length == 0)
                    filter = "'" + item + "'";
                else
                    filter += ",'" + item + "'";
            }

            sqlQuery = "SELECT TOP 30 REQUESID RequestId, DESCRIPT Description, DOCAMNT Amount, COMMENT1 Note, REQUESTBY Requester, CURRCYID Currency, "
                + "DEPRTMID Department, LSTUSRID UserId, CREATDDT DocumentDate, "
                + "(CASE REQUSTS WHEN 0 THEN 'No enviada' WHEN 1 THEN 'Enviado' WHEN 2 THEN 'Rechazado' WHEN 3 THEN 'Anulado' WHEN 4 THEN 'Aprobado' "
                + "WHEN 5 THEN 'Cerrada' ELSE 'No enviada' END) Status "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30600 WHERE DEPRTMID IN(" + filter + ") "
                + "ORDER BY DEX_ROW_ID DESC";

            var pettyCashRequestList = _repository.ExecuteQuery<PettyCashRequestViewModel>(sqlQuery);
            return View(pettyCashRequestList);
        }

        public ActionResult List(string fromDate, string toDate)
        {
            string sqlQuery = "SELECT DISTINCT A.DEPRTMDS FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                              + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                              + "WHERE RTRIM(B.USERID) = '" + Account.GetAccount(User.Identity.GetUserName()).UserId +
                              "'";

            string filter = "";

            string[] departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

            foreach (var item in departments)
            {
                if (filter.Length == 0)
                    filter = "'" + item + "'";
                else
                    filter += ",'" + item + "'";
            }

            sqlQuery = "SELECT REQUESID RequestId, DESCRIPT Description, DOCAMNT Amount, COMMENT1 Note, REQUESTBY Requester, CURRCYID Currency, "
                + "DEPRTMID Department, LSTUSRID UserId, CREATDDT DocumentDate, "
                + "(CASE REQUSTS WHEN 0 THEN 'No enviada' WHEN 1 THEN 'Enviado' WHEN 2 THEN 'Rechazado' WHEN 3 THEN 'Anulado' WHEN 4 THEN 'Aprobado' "
                + "WHEN 5 THEN 'Cerrada' ELSE 'No enviada' END) Status "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30600 WHERE DEPRTMID IN(" + filter + ") AND "
                + $"CREATDDT BETWEEN '{DateTime.ParseExact(fromDate, "MM/dd/yyyy", null).ToString("yyyyMMdd")}' AND '{DateTime.ParseExact(toDate, "MM/dd/yyyy", null).ToString("yyyyMMdd")}' "
                + "ORDER BY DEX_ROW_ID DESC";

            var pettyCashRequestList = _repository.ExecuteQuery<PettyCashRequestViewModel>(sqlQuery);
            return PartialView("~/Views/PettyCash/_List.cshtml", pettyCashRequestList);
        }

        public ActionResult Create()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PettyCash", "Index"))
                return RedirectToAction("NotPermission", "Home");
 
            if (ViewBag.RequestSecuence == null)
                ViewBag.RequestSecuence =HelperLogic.AsignaciónSecuencia("LPPOP30600",Account.GetAccount(User.Identity.GetUserName()).UserId);

            ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;
            ViewBag.UserId = Account.GetAccount(User.Identity.GetUserName()).UserId;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RequestId,Description,Amount,Note,Requester,DepartmentId,Currency")]PettyCashRequest pettyCashRequest, int postType = 0)
        {
            var status = "";
            if (ModelState.IsValid)
            {
                _repository.ExecuteCommand(String.Format(
                    "LODYNDEV.dbo.LPPOP30600SI '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}'",
                    Helpers.InterCompanyId, pettyCashRequest.RequestId, pettyCashRequest.Description,
                    pettyCashRequest.Amount, pettyCashRequest.Note,
                    pettyCashRequest.Requester, pettyCashRequest.DepartmentId, pettyCashRequest.Currency,
                    DateTime.Now.ToString("yyyyMMdd"),
                    DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.ToString("yyyyMMdd"), 0, 1,
                    Account.GetAccount(User.Identity.GetUserName()).UserId));

                if (postType == 1)
                {
                    _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPPOP30603SI '{0}','{1}','{2}','{3}','{4}','{5}','{6}'",
                        Helpers.InterCompanyId, pettyCashRequest.RequestId,
                        DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss"),
                        Account.GetAccount(User.Identity.GetUserName()).UserId, "", 1,
                        Account.GetAccount(User.Identity.GetUserName()).UserId));

                    _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPPOP30600P1 '{0}','{1}','{2}','{3}','{4}'",
                        Helpers.InterCompanyId, pettyCashRequest.RequestId,
                        DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss"), 1,
                        Account.GetAccount(User.Identity.GetUserName()).UserId));

                    ProcessLogic.SendToSharepoint(pettyCashRequest.RequestId, 2, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
                }

                if (status == "OK")
                    return RedirectToAction("Index");
            }

            return View(pettyCashRequest);
        }

        public ActionResult Edit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PettyCash",
                "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            string sqlQuery =
                "SELECT REQUESID RequestId, DESCRIPT Description, DOCAMNT Amount, COMMENT1 Note, REQUESTBY Requester, DEPRTMID DepartmentId, "
                + "CURRCYID Currency, REQUSTS Status, REQURVW Review "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30600 "
                + "WHERE REQUESID = '" + id + "' ";

            PettyCashRequest pettyCashRequest = _repository.ExecuteScalarQuery<PettyCashRequest>(sqlQuery);
            if (pettyCashRequest == null)
            {
                return HttpNotFound();
            }

            if (pettyCashRequest.Status == 4 || pettyCashRequest.Status == 1 || pettyCashRequest.Status == 5)
            {
                return View("Inquiry", pettyCashRequest);
            }

            return View(pettyCashRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RequestId,Description,Amount,Note,Requester,DepartmentId,Currency")] PettyCashRequest pettyCashRequest, int postType = 0)
        {
            var status = "";
            if (ModelState.IsValid)
            {
                _repository.ExecuteCommand(String.Format(
                    "LODYNDEV.dbo.LPPOP30600SI '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8:yyyyMMdd}','{9:yyyyMMdd}','{10:yyyyMMdd}','{11}','{12}','{13}'",
                    Helpers.InterCompanyId, pettyCashRequest.RequestId, pettyCashRequest.Description,
                    pettyCashRequest.Amount, pettyCashRequest.Note,
                    pettyCashRequest.Requester, pettyCashRequest.DepartmentId, pettyCashRequest.Currency, DateTime.Now, DateTime.Now, DateTime.Now, 0, 1,
                    Account.GetAccount(User.Identity.GetUserName()).UserId));

                if (postType == 1)
                {
                    _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPPOP30603SI '{0}','{1}','{2:yyyy-MM-ddThh:mm:ss}','{3}','{4}','{5}','{6}'",
                        Helpers.InterCompanyId, pettyCashRequest.RequestId, DateTime.Now,
                        Account.GetAccount(User.Identity.GetUserName()).UserId, "", 1,
                        Account.GetAccount(User.Identity.GetUserName()).UserId));

                    _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPPOP30600P1 '{0}','{1}','{2:yyyy-MM-ddThh:mm:ss}','{3}','{4}'",
                        Helpers.InterCompanyId, pettyCashRequest.RequestId, DateTime.Now, 1,
                        Account.GetAccount(User.Identity.GetUserName()).UserId));
                    ProcessLogic.SendToSharepoint(pettyCashRequest.RequestId, 2, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
                }


                if (status == "OK")
                    return RedirectToAction("Index");
            }

            return View(pettyCashRequest);
        }

        public ActionResult Delete(string requestId)
        {
            if (requestId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            string sqlQuery =
                "SELECT REQUESID RequestId, DESCRIPT Description, DOCAMNT Amount, COMMENT1 Note, REQUESTBY Requester, DEPRTMID DepartmentId, "
                + "CURRCYID Currency, REQUSTS Status, REQURVW Review "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30600 "
                + "WHERE REQUESID = '" + requestId + "' ";

            PettyCashRequest pettyCashRequest = _repository.ExecuteScalarQuery<PettyCashRequest>(sqlQuery);

            if (pettyCashRequest == null)
            {
                return HttpNotFound();
            }

            return View(pettyCashRequest);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string requestId)
        {
            _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPPOP30600SD '{0}','{1}'", Helpers.InterCompanyId,
                requestId));
            return RedirectToAction("Index");
        }

        public ActionResult UnblockSecuence(string secuencia, string formulario, string usuario)
        {
            HelperLogic.DesbloqueoSecuencia(secuencia, "LPPOP30600",
                Account.GetAccount(User.Identity.GetUserName()).UserId);
            return View();
        }

        [HttpPost]
        public JsonResult AttachFile(HttpPostedFileBase fileData, string requestId)
        {
            bool status = false;

            try
            {
                byte[] fileStream = null;
                using (var binaryReader = new BinaryReader(fileData.InputStream))
                {
                    fileStream = binaryReader.ReadBytes(fileData.ContentLength);
                }

                string fileName = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].ToString();
                string fileType = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].Split('.')[1]
                    .ToString();

                _repository.ExecuteCommand(String.Format(
                    "INTRANET.dbo.AttachmentInsert '{0}','{1}','{2}',{3},'{4}','{5}','{6}'",
                    Helpers.InterCompanyId, requestId, fileName,
                    "0x" + BitConverter.ToString(fileStream).Replace("-", String.Empty),
                    fileType, Account.GetAccount(User.Identity.GetUserName()).UserId, "NOTAS"));
                status = true;
            }
            catch
            {
                status = false;
            }

            return new JsonResult {Data = new { status } };
        }

        [HttpPost]
        public JsonResult SendWorkFlow(string requestId)
        {
            string status;

            try
            {
                _repository.ExecuteCommand(String.Format(
                    "LODYNDEV.dbo.LPPOP30603SI '{0}','{1}','{2}','{3}','{4}','{5}','{6}'",
                    Helpers.InterCompanyId, requestId, DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss"),
                    Account.GetAccount(User.Identity.GetUserName()).UserId, "", 1,
                    Account.GetAccount(User.Identity.GetUserName()).UserId));

                _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPPOP30600P1 '{0}','{1}','{2}','{3}','{4}'",
                    Helpers.InterCompanyId, requestId, DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss"), 1,
                    Account.GetAccount(User.Identity.GetUserName()).UserId));
                status = "OK";
                ProcessLogic.SendToSharepoint(requestId, 2, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
            }
            catch(Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult {Data = new { status } };
        }

        public class AttachmentViewModel
        {
            public HttpPostedFileBase FileData { get; set; }
        }

        [HttpPost]
        public ActionResult LoadAttachmentFiles(string requestId)
        {
            try
            {
                List<string> files = new List<string>();
                string sqlQuery = "SELECT RTRIM(fileName) FileName FROM " + Helpers.InterCompanyId +
                                  ".dbo.CO00105 WHERE DOCNUMBR = '" + requestId + "' AND DELETE1 = 0";
                files = _repository.ExecuteQuery<string>(sqlQuery).ToList();
                return Json(files, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public ActionResult DeleteFile(string id, string fileName)
        {
            string status = "";
            try
            {
                string sqlQuery = "UPDATE " + Helpers.InterCompanyId +
                                  ".dbo.CO00105 SET DELETE1 = 1 WHERE DOCNUMBR = '" + id + "' AND RTRIM(fileName) = '" +
                                  fileName + "'";
                _repository.ExecuteCommand(sqlQuery);

                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult Download(string documentId, string FileName)
        {
            string sqlQuery = "SELECT BinaryBlob, FileType, A.fileName "
                              + "FROM " + Helpers.InterCompanyId + ".dbo.CO00105 A "
                              + "INNER JOIN " + Helpers.InterCompanyId +
                              ".dbo.coAttachmentItems B ON A.Attachment_ID = B.Attachment_ID "
                              + "WHERE A.DOCNUMBR = '" + documentId + "' AND A.DELETE1 = 0 AND RTRIM(A.fileName) = '" +
                              FileName + "'";

            DataTable adjunto = ConnectionDb.GetDt(sqlQuery);
            byte[] contents = null;
            string fileType = "";
            string fileName = "";

            if (adjunto.Rows.Count > 0)
            {
                contents = (byte[]) adjunto.Rows[0][0];
                fileType = adjunto.Rows[0][1].ToString();
                fileName = adjunto.Rows[0][2].ToString();
            }

            return File(contents, fileType.Trim(), fileName.Trim());
        }

        public ActionResult Print(string id)
        {
            string status = "";
            try
            {

                string sqlQuery = "SELECT ISNULL(DEPRTMID,'') DEPARTAMENTO "
                                  + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30600 "
                                  + "WHERE REQUESID = '" + id.Trim() + "'";

                string departamento = _repository.ExecuteScalarQuery<string>(sqlQuery);

                sqlQuery = "SELECT ISNULL(REQUSTS, 0) STATUS "
                           + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30600 "
                           + "WHERE REQUESID = '" + id.Trim() + "'";

                short Status = _repository.ExecuteScalarQuery<short>(sqlQuery);

                if (Status == 4 || Status == 5)
                {
                    HelperLogic.InsertSignature(HelperLogic.GetAproverByDepartmentDescription(departamento));
                }
                else
                {
                    HelperLogic.InsertSignature("");
                }

                ReportHelper.Export(Helpers.ReportPath + "Caja", Server.MapPath("~/PDF/Caja/") + id + ".pdf",
                    String.Format("LODYNDEV.dbo.LPPOP30600R1 '{0}','{1}'", Helpers.InterCompanyId, id.Trim()), 3, ref status);

                status = id + ".pdf";
            }
            catch
            {
                status = "";
            }

            return new JsonResult {Data = new { status } };
        }
    }
}