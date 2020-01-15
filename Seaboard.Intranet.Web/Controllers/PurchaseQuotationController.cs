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
    public class PurchaseQuotationController : Controller
    {
        private readonly GenericRepository _repository;

        public PurchaseQuotationController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseQuotation", "Index"))
                return RedirectToAction("NotPermission", "Home");

            if (Account.GetAccount(User.Identity.GetUserName()).Department == "COMPRAS" || Account.GetAccount(User.Identity.GetUserName()).Department == "ADUANAS")
            {
                var sqlQuery = "SELECT RequestId, Description, Note, Requester, DepartmentId, UserId, ApproveUser, DocumentDate, Status StatusInteger, ApproveDate, "
                + "(CASE Status WHEN 0 THEN 'No enviada' WHEN 1 THEN 'Enviado' WHEN 2 THEN 'Rechazado' WHEN 3 THEN 'Anulado' WHEN 4 THEN 'Procesado' "
                + "WHEN 5 THEN 'Cerrada' ELSE 'No enviada' END) Status "
                + "FROM INTRANET.dbo.PurchQuoteRequest WHERE INTERID = '" + Helpers.InterCompanyId + "'"
                + "ORDER BY DocumentDate ";

                var purchQuotehRequestList = _repository.ExecuteQuery<PurchaseQuotationRequestViewModel>(sqlQuery);
                return View("ApproveIndex", purchQuotehRequestList);
            }
            else
            {
                string sqlQuery = "SELECT DISTINCT A.DEPRTMDS FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                              + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                              + "WHERE RTRIM(B.USERID) = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "'";

                string filter = "";
                string[] departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

                foreach (var item in departments)
                    if (filter.Length == 0)
                        filter = "'" + item + "'";
                    else
                        filter += ",'" + item + "'";

                sqlQuery = "SELECT RequestId, Description, Note, Requester, DepartmentId, UserId, ApproveUser, DocumentDate, ApproveDate, "
                    + "(CASE Status WHEN 0 THEN 'No enviada' WHEN 1 THEN 'Enviado' WHEN 2 THEN 'Rechazado' WHEN 3 THEN 'Anulado' WHEN 4 THEN 'Procesado' "
                    + "WHEN 5 THEN 'Cerrada' ELSE 'No enviada' END) Status "
                    + "FROM INTRANET.dbo.PurchQuoteRequest WHERE DepartmentId IN (" + filter + ") AND INTERID = '" + Helpers.InterCompanyId + "'"
                    + "ORDER BY DocumentDate ";

                var purchQuotehRequestList = _repository.ExecuteQuery<PurchaseQuotationRequestViewModel>(sqlQuery);
                return View(purchQuotehRequestList);
            }
        }

        public ActionResult Create()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseQuotation", "Index"))
                return RedirectToAction("NotPermission", "Home");

            if (ViewBag.RequestSecuence == null)
                ViewBag.RequestSecuence = HelperLogic.AsignaciónSecuencia("PurchQuote", Account.GetAccount(User.Identity.GetUserName()).UserId);

            ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;
            ViewBag.UserId = Account.GetAccount(User.Identity.GetUserName()).UserId;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RequestId,Description,Note,Requester,DepartmentId,StatusInteger")]PurchaseQuotationRequestViewModel purchaseQuotation)
        {
            if (ModelState.IsValid)
            {
                _repository.ExecuteCommand($"INTRANET.dbo.InsertPurchaseQuotation '{Helpers.InterCompanyId}', '{purchaseQuotation.RequestId}','{purchaseQuotation.Description}','{purchaseQuotation.Requester}'," +
                    $"'{purchaseQuotation.DepartmentId}','{DateTime.Now.ToString("yyyyMMdd")}','{purchaseQuotation.Note}','{Account.GetAccount(User.Identity.GetUserName()).UserId}','{purchaseQuotation.StatusInteger}'");
                if (purchaseQuotation.StatusInteger == 1)
                {
                    string emails = "";
                    _repository.ExecuteQuery<string>($"SELECT Email FROM INTRANET.dbo.PurchQuoteEmail").ToList().ForEach(p =>
                    {
                        emails += p + ";";
                    });
                    string attachments = "";
                    GetAttachmentsForEmail(purchaseQuotation.RequestId).ForEach(p =>
                    {
                        if (attachments.Length == 0)
                            attachments += p.Trim();
                        else
                            attachments += ";" + p.Trim();
                    });
                    _repository.ExecuteCommand($"INTRANET.dbo.SendMailPurchaseQuotation '{Helpers.InterCompanyId}', '{purchaseQuotation.RequestId}', '{emails}', '{attachments}', 0");
                }
                return RedirectToAction("Index");
            }

            return View(purchaseQuotation);
        }

        public ActionResult Edit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseQuotation", "Index"))
                return RedirectToAction("NotPermission", "Home");

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            string sqlQuery = "SELECT RequestId, Description, Note, Requester, DepartmentId, ApproveUser, ApproveDate, PurchaseNote, Status StatusInteger, " +
                "(CASE Status WHEN 0 THEN 'No enviada' WHEN 1 THEN 'Enviado' WHEN 2 THEN 'Rechazado' WHEN 3 THEN 'Anulado' WHEN 4 THEN 'Procesado' "
                + "WHEN 5 THEN 'Cerrada' ELSE 'No enviada' END) Status "
                + "FROM INTRANET.dbo.PurchQuoteRequest "
                + "WHERE RequestId = '" + id + "' ";

            PurchaseQuotationRequestViewModel purchaseQuotation = _repository.ExecuteScalarQuery<PurchaseQuotationRequestViewModel>(sqlQuery);
            if (purchaseQuotation == null)
                return HttpNotFound();

            if (purchaseQuotation.Status == "Cerrada" || purchaseQuotation.Status == "Anulado")
                return View("Inquiry", purchaseQuotation);

            return View(purchaseQuotation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RequestId,Description,Note,Requester,DepartmentId,StatusInteger")]PurchaseQuotationRequestViewModel purchaseQuotation)
        {
            if (ModelState.IsValid)
            {
                _repository.ExecuteCommand($"INTRANET.dbo.InsertPurchaseQuotation '{purchaseQuotation.RequestId}','{purchaseQuotation.Description}','{purchaseQuotation.Requester}','{purchaseQuotation.DepartmentId}'," +
                    $"'{DateTime.Now.ToString("yyyyMMdd")}','{purchaseQuotation.Note}','{Account.GetAccount(User.Identity.GetUserName()).UserId}','{purchaseQuotation.StatusInteger}'");
                if (purchaseQuotation.StatusInteger == 1)
                {
                    string emails = "";
                    _repository.ExecuteQuery<string>($"SELECT Email FROM INTRANET.dbo.PurchQuoteEmail").ToList().ForEach(p =>
                    {
                        emails += p + ";";
                    });
                    _repository.ExecuteCommand($"INTRANET.dbo.SendMailPurchaseQuotation '{Helpers.InterCompanyId}', '{purchaseQuotation.RequestId}', '{emails}', 0");
                }
                    
                return RedirectToAction("Index");
            }

            return View(purchaseQuotation);
        }

        public ActionResult Approve(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseQuotation", "Index"))
                return RedirectToAction("NotPermission", "Home");

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            string sqlQuery = "SELECT RequestId, Description, Note, Requester, DepartmentId, ApproveUser, ApproveDate, PurchaseNote, Status StatusInteger, " +
                "(CASE Status WHEN 0 THEN 'No enviada' WHEN 1 THEN 'Enviado' WHEN 2 THEN 'Rechazado' WHEN 3 THEN 'Anulado' WHEN 4 THEN 'Procesado' "
                + "WHEN 5 THEN 'Cerrada' ELSE 'No enviada' END) Status "
                + "FROM INTRANET.dbo.PurchQuoteRequest "
                + "WHERE RequestId = '" + id + "' ";

            PurchaseQuotationRequestViewModel purchaseQuotation = _repository.ExecuteScalarQuery<PurchaseQuotationRequestViewModel>(sqlQuery);
            if (purchaseQuotation == null)
                return HttpNotFound();

            if (purchaseQuotation.Status == "Cerrada" || purchaseQuotation.Status == "Anulado")
                return View("Inquiry", purchaseQuotation);

            return View(purchaseQuotation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve([Bind(Include = "RequestId,Description,Note,PurchaseNote,Requester,DepartmentId,StatusInteger")]PurchaseQuotationRequestViewModel purchaseQuotation)
        {
            if (ModelState.IsValid)
            {
                _repository.ExecuteCommand($"UPDATE INTRANET.dbo.PurchQuoteRequest SET Status = '{purchaseQuotation.StatusInteger}', " +
                    $"PurchaseNote = '{purchaseQuotation.PurchaseNote}', ApproveDate = '{DateTime.Now.ToString("yyyyMMdd")}', ApproveUser = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                    $"WHERE RequestId = '{purchaseQuotation.RequestId}'");
                if (purchaseQuotation.StatusInteger == 4)
                {
                   var email = _repository.ExecuteScalarQuery<string>($"SELECT RTRIM(Email) FROM INTRANET.dbo.USERS WHERE UserId = '{purchaseQuotation.Requester}'");
                    string attachments = "";
                    GetAttachmentsForEmail(purchaseQuotation.RequestId).ForEach(p =>
                    {
                        if (attachments.Length == 0)
                            attachments += p.Trim();
                        else
                            attachments += ";" + p.Trim();
                    });
                    _repository.ExecuteCommand($"INTRANET.dbo.SendMailPurchaseQuotation '{Helpers.InterCompanyId}', '{purchaseQuotation.RequestId}', '{email}', '{attachments}', 1");
                }
                return RedirectToAction("Index");
            }

            return View(purchaseQuotation);
        }

        public JsonResult Delete(string id)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand($"DELETE FROM INTRANET.dbo.PurchQuoteRequest WHERE RequestId = '{id}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(xStatus, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UnblockSecuence(string secuencia, string formulario, string usuario)
        {
            HelperLogic.DesbloqueoSecuencia(secuencia, "PurchQuote", Account.GetAccount(User.Identity.GetUserName()).UserId);
            return View();
        }

        #region Attachments

        [HttpPost]
        public JsonResult AttachFile(HttpPostedFileBase fileData, string requestId)
        {
            bool status = false;

            try
            {
                byte[] fileStream = null;
                using (var binaryReader = new BinaryReader(fileData.InputStream))
                    fileStream = binaryReader.ReadBytes(fileData.ContentLength);

                string fileName = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].ToString();
                string fileType = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].Split('.')[1].ToString();

                _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachmentInsert '{0}','{1}','{2}',{3},'{4}','{5}','{6}'",
                    Helpers.InterCompanyId, requestId, fileName, "0x" + BitConverter.ToString(fileStream).Replace("-", String.Empty),
                    fileType, Account.GetAccount(User.Identity.GetUserName()).UserId, "NOTAS"));
                status = true;
            }
            catch
            {
                status = false;
            }

            return new JsonResult {Data = new {status = status}};
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
                string sqlQuery = "SELECT RTRIM(fileName) FileName FROM " + Helpers.InterCompanyId + ".dbo.CO00105 WHERE DOCNUMBR = '" + requestId + "' AND DELETE1 = 0";
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
                string sqlQuery = "UPDATE " + Helpers.InterCompanyId + ".dbo.CO00105 SET DELETE1 = 1 WHERE DOCNUMBR = '" + id + "' AND RTRIM(fileName) = '" + fileName + "'";
                _repository.ExecuteCommand(sqlQuery);

                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status = status } };
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

        public List<string> GetAttachmentsForEmail(string id)
        {
            string sqlQuery = "SELECT BinaryBlob, FileType, A.fileName "
                              + "FROM " + Helpers.InterCompanyId + ".dbo.CO00105 A "
                              + "INNER JOIN " + Helpers.InterCompanyId +
                              ".dbo.coAttachmentItems B ON A.Attachment_ID = B.Attachment_ID "
                              + "WHERE A.DOCNUMBR = '" + id + "' AND A.DELETE1 = 0";

            DataTable adjunto = ConnectionDb.GetDt(sqlQuery);
            var files = new List<string>();

            var directoryPath = Helpers.ReportPath + "Files";
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            foreach (DataRow item in adjunto.Rows)
            {
                var path = Path.Combine(directoryPath, item[2].ToString());
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
                System.IO.File.WriteAllBytes(path, (byte[])item[0]);
                files.Add(path);
            }

            return files;
        }

        #endregion

        #region Configuration

        public ActionResult Configuration()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseQuotation", "Configuration"))
                return RedirectToAction("NotPermission", "Home");

            return View();
        }

        public ActionResult GetEmailAccounts()
        {
            var sqlQuery = "SELECT Email Id, RTRIM(Name) Descripción, '' DataExtended FROM INTRANET.dbo.PurchQuoteEmail ORDER BY Email";
            var products = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();
            return Json(products, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveEmailAccount(string email, string name)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM INTRANET.dbo.PurchQuoteEmail WHERE Email = '{email}'");
                var sqlQuery = "";

                if (count == 0)
                {
                    sqlQuery = $"INSERT INTO INTRANET.dbo.PurchQuoteEmail (Email, Name) VALUES ('{email}', '{name}')";
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

        [HttpPost]
        public JsonResult DeleteEmailAccount(string id)
        {
            string xStatus;

            try
            {

                var sqlQuery = $"DELETE INTRANET.dbo.PurchQuoteEmail WHERE Email = '{id}'";
                _repository.ExecuteCommand(sqlQuery);

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        #endregion
    }
}