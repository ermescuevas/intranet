﻿using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Enums;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Seaboard.Intranet.Data.Repository;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class AccountPayablesController : Controller
    {
        private readonly GenericRepository _repository;

        public AccountPayablesController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountPayables", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            var sqlQuery = "SELECT DISTINCT A.DEPRTMDS FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                        + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                        + "WHERE RTRIM(B.USERID) = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "'";

            var filter = "";

            var departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

            foreach (var item in departments)
            {
                if (filter.Length == 0)
                {
                    filter = "'" + item + "'";
                }
                else
                {
                    filter += ",'" + item + "'";
                }
            }

            sqlQuery = "SELECT RTRIM(VCHRNMBR) VoucherNumber, VENDORID VendorId, VENDNAME VendName, A.DOCNUMBR DocumentNumber, "
                    + "A.DOCAMNT Amount, A.DOCDATE DocumentDate, A.CURNCYID Currency, TRXDSCRN Description "
                    + "FROM (SELECT A.VCHNUMWK VCHRNMBR, A.VENDORID, B.VENDNAME, A.DOCNUMBR, A.DOCAMNT, A.DOCDATE, "
                    + "A.CURNCYID, A.TRXDSCRN, A.PORDNMBR PONUMBER, A.DEX_ROW_ID "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.PM10000 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.PM00200 B ON A.VENDORID = B.VENDORID "
                    + "UNION ALL "
                    + "SELECT RTRIM(A.VCHRNMBR), A.VENDORID, B.VENDNAME, A.DOCNUMBR, A.DOCAMNT, A.DOCDATE, A.CURNCYID, "
                    + "TRXDSCRN, A.PONUMBER, A.DEX_ROW_ID "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.PM20000 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.PM00200 B ON A.VENDORID = B.VENDORID) A "
                    + "WHERE A.PONUMBER IN(" + filter + ")"
                    + "ORDER BY A.DEX_ROW_ID DESC";

            var accountPayablesList = _repository.ExecuteQuery<AccountPayablesViewModel>(sqlQuery);

            return View(accountPayablesList.ToList());
        }

        public ActionResult Create()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountPayables", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            if (ViewBag.RequestSecuence == null)
            {
                ViewBag.RequestSecuence = HelperLogic.AsignaciónSecuencia("PM10000", Account.GetAccount(User.Identity.GetUserName()).UserId);
            }

            ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;
            ViewBag.Discount = 0.00;
            ViewBag.Freight = 0.00;
            ViewBag.Miscellaneous = 0.00;
            ViewBag.Tax = 0.00;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Type,VoucherNumber,Description,VendorId,VendName,DocumentNumber,PurchaseOrder,TaxDetailId,PurchaseAmount,DiscountAmount,FreightAmount,MiscellaneousAmount,TaxAmount,Currency,Note,DocumentDate, NCF")] AccountPayables accountPayables)
        {
            var isValid = false;

            var service = new ServiceContract();
            var payablesDocument = new GpPayablesDocument();
            payablesDocument.Currency = accountPayables.Currency;
            payablesDocument.DocumentDate = accountPayables.DocumentDate;
            payablesDocument.DocumentNumber = accountPayables.DocumentNumber;
            payablesDocument.FreightAmount = accountPayables.FreightAmount;
            payablesDocument.MiscellaneousAmount = accountPayables.MiscellaneousAmount;
            payablesDocument.PurchaseAmount = accountPayables.PurchaseAmount;
            payablesDocument.PurchaseOrder = accountPayables.PurchaseOrder;
            payablesDocument.TaxAmount = accountPayables.TaxAmount;
            payablesDocument.TaxDetail = accountPayables.TaxDetailId;
            payablesDocument.TradeDiscountAmount = accountPayables.DiscountAmount;
            payablesDocument.VendorId = accountPayables.VendorId;
            payablesDocument.VoucherNumber = accountPayables.VoucherNumber;
            payablesDocument.Ncf = accountPayables.Ncf;
            payablesDocument.Note = accountPayables.Note;
            payablesDocument.Description = accountPayables.Description;

            if (accountPayables.Type == AccountPayablesType.NotaDeCrédito)
            {
                if (service.CreatePayablesCreditNote(payablesDocument, Request.Cookies["UserAccount"]?["username"], Request.Cookies["UserAccount"]?["password"]))
                    isValid = true;
            }
            else
            {
                if (service.CreatePayablesInvoice(payablesDocument, Request.Cookies["UserAccount"]?["username"], Request.Cookies["UserAccount"]?["password"]))
                {
                    var sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.SY90000(ObjectType, ObjectID, PropertyName, PropertyValue) "
                                   + "VALUES ('PayablesTransactionEntry','" + payablesDocument.VendorId.Trim() + "_5_" + payablesDocument.VoucherNumber.Trim() + "', "
                                   + "'NCF', '" + payablesDocument.Ncf + "')";
                    _repository.ExecuteCommand(sqlQuery);

                    sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.SY90000(ObjectType, ObjectID, PropertyName, PropertyValue) "
                       + "VALUES ('PayablesTransactionEntry','" + payablesDocument.VendorId.Trim() + "_5_" + payablesDocument.VoucherNumber.Trim() + "', "
                       + "'NCFTIPOGASTO', 9) ";
                    _repository.ExecuteCommand(sqlQuery);

                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachPayableNote '{0}','{1}','{2}'",
                        Helpers.InterCompanyId, payablesDocument.VoucherNumber, payablesDocument.Note));

                    isValid = true;
                }
            }

            if (isValid)
            {
                HelperLogic.DesbloqueoSecuencia(payablesDocument.VoucherNumber, "PM10000", Account.GetAccount(User.Identity.GetUserName()).UserId);
                return RedirectToAction("Index");
            }

            ViewBag.RequestSecuence = accountPayables.VoucherNumber;
            ViewBag.Discount = accountPayables.DiscountAmount;
            ViewBag.Freight = accountPayables.FreightAmount;
            ViewBag.Miscellaneous = accountPayables.MiscellaneousAmount;
            ViewBag.Tax = accountPayables.TaxAmount;
            ViewBag.DepartmentId = accountPayables.PurchaseOrder;

            return View(accountPayables);
        }

        public ActionResult Edit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountPayables", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var sqlQuery = "SELECT TOP 1 A.VCHRNMBR VoucherNumber, A.VENDORID VendorId, A.VENDNAME VendName, A.DOCNUMBR DocumentNumber, "
                    + "A.DOCDATE DocumentDate, A.CURNCYID Currency, A.TRXDSCRN Description, "
                    + "A.PONUMBER PurchaseOrder, CONVERT(NUMERIC(32, 2), A.PRCHAMNT) PurchaseAmount, "
                    + "CONVERT(NUMERIC(32, 2), A.TRDISAMT) DiscountAmount, CONVERT(NUMERIC(32, 2), A.TAXAMNT) TaxAmount, "
                    + "CONVERT(NUMERIC(32, 2), A.FRTAMNT) FreightAmount, CONVERT(NUMERIC(32, 2), A.MSCCHAMT) MiscellaneousAmount, "
                    + "(CASE A.DOCTYPE WHEN 1 THEN 1 ELSE 2 END) Type, ISNULL(C.TXTFIELD, '') Note, ISNULL(B.PropertyValue,'') NCF, ISNULL(D.TAXDTLID, '') TaxDetailId "
                    + "FROM (SELECT RTRIM(A.VCHNUMWK) VCHRNMBR, A.VENDORID, B.VENDNAME, A.DOCNUMBR, A.DOCAMNT, A.DOCDATE, "
                    + "A.CURNCYID, A.TRXDSCRN, A.PORDNMBR PONUMBER, PRCHAMNT, TRDISAMT, TAXAMNT, FRTAMNT, MSCCHAMT, DOCTYPE, A.NOTEINDX "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.PM10000 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.PM00200 B ON A.VENDORID = B.VENDORID) A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY90000 B "
                    + "ON RTRIM(A.VENDORID) + '_5_' + RTRIM(A.VCHRNMBR) = RTRIM(B.ObjectID) AND B.PropertyName = 'NCF' "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY03900 C "
                    + "ON A.NOTEINDX = C.NOTEINDX "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.PM10500 D "
                    + "ON A.VCHRNMBR = D.VCHRNMBR AND A.VENDORID = D.VENDORID "
                    + "WHERE A.VCHRNMBR = '" + id + "'";

            var accountPayable = _repository.ExecuteScalarQuery<AccountPayables>(sqlQuery);

            if (accountPayable == null)
            {
                RedirectToAction("Inquiry", id);
                return HttpNotFound();
            }
            return View(accountPayable);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Type,VoucherNumber,Description,VendorId,VendName,DocumentNumber,PurchaseOrder,TaxDetailId,PurchaseAmount,DiscountAmount,FreightAmount,MiscellaneousAmount,TaxAmount,Currency,Note,DocumentDate, NCF")] AccountPayables accountPayables)
        {
            var isValid = false;

            var service = new ServiceContract();
            var payablesDocument = new GpPayablesDocument();
            payablesDocument.Currency = accountPayables.Currency;
            payablesDocument.DocumentDate = accountPayables.DocumentDate;
            payablesDocument.DocumentNumber = accountPayables.DocumentNumber;
            payablesDocument.FreightAmount = accountPayables.FreightAmount;
            payablesDocument.MiscellaneousAmount = accountPayables.MiscellaneousAmount;
            payablesDocument.PurchaseAmount = accountPayables.PurchaseAmount;
            payablesDocument.PurchaseOrder = accountPayables.PurchaseOrder;
            payablesDocument.TaxAmount = accountPayables.TaxAmount;
            payablesDocument.TaxDetail = accountPayables.TaxDetailId;
            payablesDocument.TradeDiscountAmount = accountPayables.DiscountAmount;
            payablesDocument.VendorId = accountPayables.VendorId;
            payablesDocument.VoucherNumber = accountPayables.VoucherNumber;
            payablesDocument.Ncf = accountPayables.Ncf;
            payablesDocument.Note = accountPayables.Note;
            payablesDocument.Description = accountPayables.Description;

            if (accountPayables.Type == AccountPayablesType.NotaDeCrédito)
            {
                if (service.CreatePayablesCreditNote(payablesDocument, Request.Cookies["UserAccount"]["username"], Request.Cookies["UserAccount"]["password"]))
                    isValid = true;
            }
            else
            {
                var amountDebit = _repository.ExecuteScalarQuery<decimal>("SELECT TOP 1 ISNULL(CURTRXAM, 0) FROM " + Helpers.InterCompanyId + ".dbo.PM10000 WHERE VCHNUMWK = '" + payablesDocument.VoucherNumber + "' AND DOCTYPE = 1");
                _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10000 WHERE VCHNUMWK = '" + payablesDocument.VoucherNumber + "' AND DOCTYPE = 1");
                _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10500 WHERE VCHRNMBR = '" + payablesDocument.VoucherNumber + "' AND DOCTYPE = 1");
                _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM00400 WHERE CNTRLNUM = '" + payablesDocument.VoucherNumber + "' AND DOCTYPE = 1");
                _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10100 WHERE VCHRNMBR = '" + payablesDocument.VoucherNumber + "' AND PSTGSTUS = 0");
                _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHTOTAL -= '" + amountDebit + "', NUMOFTRX -= 1 WHERE BACHNUMB = '" + payablesDocument.DocumentNumber + "'");

                if (service.CreatePayablesInvoice(payablesDocument, Request.Cookies["UserAccount"]["username"], Request.Cookies["UserAccount"]["password"]))
                {
                    var sqlQuery = "DELETE " + Helpers.InterCompanyId + ".dbo.SY90000 "
                                   + "WHERE ObjectID = '" + payablesDocument.VendorId.Trim() + "_5_" + payablesDocument.VoucherNumber.Trim() + "'"
                                   + "AND PropertyName = 'NCF'";
                    _repository.ExecuteCommand(sqlQuery);

                    sqlQuery = "DELETE " + Helpers.InterCompanyId + ".dbo.SY90000 "
                        + "WHERE ObjectID = '" + payablesDocument.VendorId.Trim() + "_5_" + payablesDocument.VoucherNumber.Trim() + "' "
                        + "AND PropertyName = 'NCFTIPOGASTO'";
                    _repository.ExecuteCommand(sqlQuery);

                    sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.SY90000(ObjectType, ObjectID, PropertyName, PropertyValue) "
                        + "VALUES ('PayablesTransactionEntry','" + payablesDocument.VendorId.Trim() + "_5_" + payablesDocument.VoucherNumber.Trim() + "', "
                        + "'NCF', '" + payablesDocument.Ncf + "')";
                    _repository.ExecuteCommand(sqlQuery);

                    sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.SY90000(ObjectType, ObjectID, PropertyName, PropertyValue) "
                       + "VALUES ('PayablesTransactionEntry','" + payablesDocument.VendorId.Trim() + "_5_" + payablesDocument.VoucherNumber.Trim() + "', "
                       + "'NCFTIPOGASTO', 9) ";
                    _repository.ExecuteCommand(sqlQuery);

                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachPayableNote '{0}','{1}','{2}'",
                        Helpers.InterCompanyId, payablesDocument.VoucherNumber, payablesDocument.Note));

                    isValid = true;
                }
            }

            if (isValid)
            {
                HelperLogic.DesbloqueoSecuencia(payablesDocument.VoucherNumber, "PM10000", Account.GetAccount(User.Identity.GetUserName()).UserId);
                return RedirectToAction("Index");
            }

            return View(accountPayables);
        }

        public ActionResult Inquiry(string id)
        {
            var sqlQuery = "SELECT A.VCHRNMBR VoucherNumber, A.VENDORID VendorId, A.VENDNAME VendName, A.DOCNUMBR DocumentNumber, "
                    + "A.DOCDATE DocumentDate, A.CURNCYID Currency, A.TRXDSCRN Description, "
                    + "A.PONUMBER PurchaseOrder, CONVERT(NUMERIC(32, 2), A.PRCHAMNT) PurchaseAmount, "
                    + "CONVERT(NUMERIC(32, 2), A.TRDISAMT) DiscountAmount, CONVERT(NUMERIC(32, 2), A.TAXAMNT) TaxAmount, "
                    + "CONVERT(NUMERIC(32, 2), A.FRTAMNT) FreightAmount, CONVERT(NUMERIC(32, 2), A.MSCCHAMT) MiscellaneousAmount, "
                    + "(CASE A.DOCTYPE WHEN 1 THEN 1 ELSE 2 END) Type, ISNULL(C.TXTFIELD, '') Note, ISNULL(B.PropertyValue,'') NCF "
                    + "FROM (SELECT RTRIM(A.VCHNUMWK) VCHRNMBR, A.VENDORID, B.VENDNAME, A.DOCNUMBR, A.DOCAMNT, A.DOCDATE, "
                    + "A.CURNCYID, A.TRXDSCRN, A.PORDNMBR PONUMBER, PRCHAMNT, TRDISAMT, TAXAMNT, FRTAMNT, MSCCHAMT, DOCTYPE, A.NOTEINDX "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.PM10000 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.PM00200 B ON A.VENDORID = B.VENDORID "
                    + "UNION ALL "
                    + "SELECT RTRIM(A.VCHRNMBR), A.VENDORID, B.VENDNAME, A.DOCNUMBR, A.DOCAMNT, A.DOCDATE, A.CURNCYID, "
                    + "TRXDSCRN, A.PONUMBER, PRCHAMNT, TRDISAMT, TAXAMNT, FRTAMNT, MSCCHAMT, DOCTYPE, A.NOTEINDX "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.PM20000 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.PM00200 B "
                    + "ON A.VENDORID = B.VENDORID) A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY90000 B "
                    + "ON RTRIM(A.VENDORID) + '_5_' + RTRIM(A.VCHRNMBR) = RTRIM(B.ObjectID) AND B.PropertyName = 'NCF' "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY03900 C "
                    + "ON A.NOTEINDX = C.NOTEINDX "
                    + "WHERE A.VCHRNMBR = '" + id + "'";

            var accountPayables = _repository.ExecuteScalarQuery<AccountPayables>(sqlQuery);
            return View(accountPayables);
        }

        public JsonResult UnblockSecuence(string secuencia, string formulario, string usuario)
        {
            HelperLogic.DesbloqueoSecuencia(secuencia, "PM10000", Account.GetAccount(User.Identity.GetUserName()).UserId);
            return Json("");
        }

        [HttpPost]
        public JsonResult AttachFile(HttpPostedFileBase fileData, string voucherNumber)
        {
            bool aStatus;

            try
            {
                byte[] fileStream;
                using (var binaryReader = new BinaryReader(fileData.InputStream))
                {
                    fileStream = binaryReader.ReadBytes(fileData.ContentLength);
                }

                var fileName = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1];
                var fileType = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].Split('.')[1];

                _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachmentInsert '{0}','{1}','{2}',{3},'{4}','{5}','{6}'",
                        Helpers.InterCompanyId, voucherNumber, fileName, "0x" + BitConverter.ToString(fileStream).Replace("-", String.Empty),
                        fileType, Account.GetAccount(User.Identity.GetUserName()).UserId, ""));
                aStatus = true;
            }
            catch
            {
                aStatus = false;
            }

            return new JsonResult { Data = new { status = aStatus } };
        }

        public class AttachmentViewModel
        {
            public HttpPostedFileBase FileData { get; set; }
        }

        [HttpPost]
        public ActionResult LoadAttachmentFiles(string voucherNumber)
        {
            try
            {
                var sqlQuery = "SELECT RTRIM(fileName) FileName FROM " + Helpers.InterCompanyId + ".dbo.CO00105 WHERE DOCNUMBR = '" + voucherNumber + "' AND DELETE1 = 0";
                var files = _repository.ExecuteQuery<string>(sqlQuery).ToList();
                return Json(files, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        public ActionResult Download(string documentId, string FileName)
        {
            var sqlQuery = "SELECT BinaryBlob, FileType, A.fileName "
                + "FROM " + Helpers.InterCompanyId + ".dbo.CO00105 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.coAttachmentItems B ON A.Attachment_ID = B.Attachment_ID "
                + "WHERE A.DOCNUMBR = '" + documentId + "' AND A.DELETE1 = 0 AND RTRIM(A.fileName) = '" + FileName + "'";

            var adjunto = ConnectionDb.GetDt(sqlQuery);
            byte[] contents = null;
            var fileType = "";
            var fileName = "";

            if (adjunto.Rows.Count > 0)
            {
                contents = (byte[])adjunto.Rows[0][0];
                fileType = adjunto.Rows[0][1].ToString();
                fileName = adjunto.Rows[0][2].ToString();
            }

            return File(contents, fileType.Trim(), fileName.Trim());
        }

        [HttpPost]
        public ActionResult DeleteFile(string id, string fileName)
        {
            string aStatus;
            try
            {
                var sqlQuery = "UPDATE " + Helpers.InterCompanyId +
                                  ".dbo.CO00105 SET DELETE1 = 1 WHERE DOCNUMBR = '" + id + "' AND RTRIM(fileName) = '" +
                                  fileName + "'";
                _repository.ExecuteCommand(sqlQuery);

                aStatus = "OK";
            }
            catch (Exception ex)
            {
                aStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = aStatus } };
        }

        public ActionResult ApplyDocuments()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountPayables", "ApplyDocuments"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        public JsonResult GetDocumentsForApply(string vendorId, string documentNumber)
        {

            string xStatus;
            var xRegistros = new List<MemNetTransDetail>();
            try
            {
                var sqlQuery = "SELECT RTRIM(DOCNUMBR) DocumentNumber, CONVERT(NUMERIC(32,2), CURTRXAM) CurrentAmount, "
                               + "CONVERT(nvarchar(20), CONVERT(DATE, DOCDATE, 112)) DocumentDate, RTRIM(CURNCYID) CurrencyId FROM " + Helpers.InterCompanyId +
                               ".dbo.PM20000 WHERE VENDORID = '" + vendorId + "' AND CURTRXAM > 0 AND DOCTYPE IN (1, 2, 3)";

                xRegistros = _repository.ExecuteQuery<MemNetTransDetail>(sqlQuery).ToList();

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult PostApplyDocument(CashReceipt cashReceipt, string docDate)
        {
            string xStatus;

            try
            {
                var voucherNumber = _repository.ExecuteScalarQuery<string>(
                    "SELECT VCHRNMBR FROM " + Helpers.InterCompanyId + ".dbo.PM20000 " +
                    "WHERE RTRIM(DOCNUMBR) = '" + cashReceipt.CashReceiptId + "' " +
                    "AND RTRIM(VENDORID) = '" + cashReceipt.CustomerId + "'");
                foreach (var item in cashReceipt.InvoiceLines)
                {
                    

                    var sqlQuery = string.Format(
                        "INTRANET.dbo.ApplyPostedAccountPayablesDocuments '{0}','{1}','{2:yyyyMMdd}','{3}','{4}','{5}','{6}','{7}','{8}'",
                        Helpers.InterCompanyId, cashReceipt.CustomerId, DateTime.ParseExact(docDate, "dd/MM/yyyy", null),
                        voucherNumber, item.DocumentNumber, Convert.ToDecimal(item.DocumentAmount).ToString(new CultureInfo("en-US")),
                        cashReceipt.Currency, cashReceipt.CashReceiptId, Account.GetAccount(User.Identity.GetUserName()).UserId);

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