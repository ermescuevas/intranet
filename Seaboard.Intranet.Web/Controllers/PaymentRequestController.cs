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
    public class PaymentRequestController : Controller
    {
        private readonly GenericRepository _repository;

        public PaymentRequestController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {

            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PaymentRequest", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            string sqlQuery = "SELECT DISTINCT A.DEPRTMDS FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                        + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                        + "WHERE RTRIM(B.USERID) = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "'";

            string filter = "";

            string[] departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

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

            if (Account.GetAccount(User.Identity.GetUserName()).Department != "NEGOCIOS")
            {
                sqlQuery = "SELECT LTRIM(RTRIM(A.PMNTNMBR)) PaymentRequestId, RTRIM(A.COMMENT1) Description, A.CHEKTOTL Amount, A.CURNCYID CurrencyId, "
                           + "RTRIM(A.VENDORID) VendorId, RTRIM(A.VENDNAME) VendName, B.PRIORITY Priority, A.DOCDATE DocumentDate, "
                           + "(CASE ISNULL(C.WFSTS, 0) WHEN 0 THEN 'No enviado' WHEN 1 THEN 'Enviado' WHEN 2 THEN 'Rechazado' WHEN 3 THEN 'Anulado' WHEN 4 THEN 'Aprobado' ELSE 'No enviado' END) Status, CONVERT(BIT, 0) Voided "
                           + "FROM "
                           + "(SELECT PMNTNMBR, COMMENT1, CHEKTOTL, DOCDATE, CURNCYID, VENDORID, VENDNAME, BACHNUMB, APPLDAMT, NOTEINDX FROM " + Helpers.InterCompanyId + ".dbo.PM10300 "
                           + "UNION ALL "
                           + "SELECT VCHRNMBR PMNTNMBR, TRXDSCRN COMMENT1, DOCAMNT CHEKTOTL, DOCDATE, CURNCYID, VENDORID, VNDCHKNM VENDNAME, BACHNUMB, 0 APPLDAMT, NOTEINDX "
                           + "FROM " + Helpers.InterCompanyId + ".dbo.PM30200 ) A "
                           + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP30900 B "
                           + "ON A.PMNTNMBR = B.PMNTNMBR "
                           + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPWF00101 C "
                           + "ON A.PMNTNMBR = C.DOCNUM "
                           + "WHERE B.DEPTMTID IN (" + filter + ")";
            }
            else
            {
                sqlQuery = "SELECT LTRIM(RTRIM(A.PMNTNMBR)) PaymentRequestId, RTRIM(A.TRXDSCRN) Description, A.DOCAMNT Amount, A.CURNCYID CurrencyId, "
                           + "RTRIM(A.VENDORID) VendorId, RTRIM(A.VENDNAME) VendName, B.PRIORITY Priority, A.DOCDATE DocumentDate, "
                           + "CASE ISNULL(A.WFSTS, 0) WHEN 0 THEN 'Abierto' ELSE 'Contabilizado' END Status, CONVERT(BIT, A.VOIDED) Voided "
                           + "FROM "
                           + "(SELECT A.PMNTNMBR, A.TRXDSCRN, A.DOCAMNT, A.DOCDATE, A.CURNCYID, A.VENDORID, B.VENDNAME, A.BACHNUMB, A.APPLDAMT, A.NOTEINDX, 0 WFSTS, 0 VOIDED FROM " + Helpers.InterCompanyId + ".dbo.PM10400 A "
                           + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.PM00200 B ON A.VENDORID = B.VENDORID "
                           + "UNION ALL "
                           + "SELECT A.VCHRNMBR PMNTNMBR, A.TRXDSCRN, A.DOCAMNT, A.DOCDATE, A.CURNCYID, A.VENDORID, B.VENDNAME, A.BACHNUMB, 0 APPLDAMT, A.NOTEINDX, 1 WFSTS, VOIDED "
                           + "FROM " + Helpers.InterCompanyId + ".dbo.PM30200 A "
                           + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.PM00200 B ON A.VENDORID = B.VENDORID) A "
                           + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP30900 B "
                           + "ON A.PMNTNMBR = B.PMNTNMBR "
                           + "WHERE B.DEPTMTID IN (" + filter + ")";
            }

            var paymentRequestList = _repository.ExecuteQuery<PaymentRequestViewModel>(sqlQuery);

            return View(paymentRequestList);
        }

        public ActionResult Create()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PaymentRequest", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            if (ViewBag.PaymentRequestId == null)
            {
                ViewBag.PaymentRequestId = HelperLogic.AsignaciónSecuencia("PM10300", Account.GetAccount(User.Identity.GetUserName()).UserId);
            }

            ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;
            ViewBag.Requester = Account.GetAccount(User.Identity.GetUserName()).UserId;
            ViewBag.Priority = "Baja";

            switch (Account.GetAccount(User.Identity.GetUserName()).Department)
            {
                case "COMPRAS":
                case "ADUANAS":
                    return View("CreatePurchase");
                case "NEGOCIOS":
                    return View("CreateSales");
            }

            return View();
        }

        public ActionResult Edit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PaymentRequest", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            string sqlQuery;

            if (Account.GetAccount(User.Identity.GetUserName()).Department != "NEGOCIOS")
            {
                sqlQuery = "SELECT RTRIM(A.PMNTNMBR) PaymentRequestId, RTRIM(A.COMMENT1) Description, B.TXTFIELD Note, CONVERT(NUMERIC(32,2), A.CHEKTOTL) Amount, "
                                  + "C.DEPTMTID DepartmentId, C.PRIORITY Priority, C.PAYMTERM PaymentCondition, A.DOCDATE DocumentDate, A.CURNCYID Currency, "
                                  + "A.VENDORID VendorId, A.VENDNAME VendName, C.REQSTDBY Requester, A.APPLDAMT ApplyAmount, A.BACHNUMB BatchNumber "
                                  + "FROM ("
                                  + "SELECT PMNTNMBR, COMMENT1, CHEKTOTL, DOCDATE, CURNCYID, VENDORID, VENDNAME, BACHNUMB, APPLDAMT, NOTEINDX FROM " + Helpers.InterCompanyId + ".dbo.PM10300 "
                                  + "UNION ALL "
                                  + "SELECT VCHRNMBR, TRXDSCRN COMMENT1, DOCAMNT CHEKTOTL, DOCDATE, CURNCYID, VENDORID, VNDCHKNM VENDNAME, BACHNUMB, 0 APPLDAMT, NOTEINDX "
                                  + "FROM " + Helpers.InterCompanyId + ".dbo.PM30200 ) A "
                                  + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY03900 B "
                                  + "ON A.NOTEINDX = B.NOTEINDX "
                                  + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP30900 C "
                                  + "ON A.PMNTNMBR = C.PMNTNMBR "
                                  + "WHERE A.PMNTNMBR = '" + id + "'";
            }
            else
            {
                sqlQuery = "SELECT RTRIM(A.PMNTNMBR) PaymentRequestId, RTRIM(A.TRXDSCRN) Description, B.TXTFIELD Note, CONVERT(NUMERIC(32,2), A.DOCAMNT) Amount, "
                                  + "'' DepartmentId, C.PRIORITY Priority, C.PAYMTERM PaymentCondition, A.DOCDATE DocumentDate, A.CURNCYID Currency, "
                                  + "A.VENDORID VendorId, A.VENDNAME VendName, C.REQSTDBY Requester, A.APPLDAMT ApplyAmount, A.BACHNUMB BatchNumber "
                                  + "FROM ("
                                  + "SELECT A.PMNTNMBR, A.TRXDSCRN, A.DOCAMNT, A.DOCDATE, A.CURNCYID, A.VENDORID, B.VENDNAME, A.BACHNUMB, A.APPLDAMT, A.NOTEINDX FROM " + Helpers.InterCompanyId + ".dbo.PM10400 A "
                                  + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.PM00200 B ON A.VENDORID = B.VENDORID "
                                  + "UNION ALL "
                                  + "SELECT VCHRNMBR, TRXDSCRN, DOCAMNT, DOCDATE, CURNCYID, VENDORID, VNDCHKNM VENDNAME, BACHNUMB, 0 APPLDAMT, NOTEINDX "
                                  + "FROM " + Helpers.InterCompanyId + ".dbo.PM30200 ) A "
                                  + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY03900 B "
                                  + "ON A.NOTEINDX = B.NOTEINDX "
                                  + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP30900 C "
                                  + "ON A.PMNTNMBR = C.PMNTNMBR "
                                  + "WHERE A.PMNTNMBR = '" + id + "'";
            }
            

            PaymentRequest paymentRequest = _repository.ExecuteScalarQuery<PaymentRequest>(sqlQuery);

            short status = 0;
            if (Account.GetAccount(User.Identity.GetUserName()).Department == "NEGOCIOS")
            {
                var count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.PM30200 WHERE VCHRNMBR = '" + id + "'");
                if (count > 0)
                    status = 4;
                else
                    status = 1;
            }
            else
            {
                status = _repository.ExecuteScalarQuery<short>("SELECT WFSTS FROM " + Helpers.InterCompanyId + ".dbo.LPWF00101 WHERE DOCNUM = '" + id + "'");
            }
            

            sqlQuery = "SELECT RTRIM(DEPTMTID) Department, CONVERT(NUMERIC(32,2), AMOUNT) ChargeAmount "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30800 "
                + "WHERE PMNTNMBR = '" + id + "'";

            ViewBag.Charges = _repository.ExecuteQuery<PaymentRequestChargeViewModel>(sqlQuery).ToList();

            sqlQuery = "SELECT RTRIM(APTODCNM) DocumentNumber, CONVERT(NUMERIC(32,2), APPLDAMT) DocumentAmount, CONVERT(NVARCHAR(20), CONVERT(DATE, DOCDATE, 112)) DocumentDate "
                + "FROM " + Helpers.InterCompanyId + ".dbo.PM10200 "
                + "WHERE VCHRNMBR = '" + id + "'";

            ViewBag.InvoicesApplied = _repository.ExecuteQuery<PaymentRequestInvoiceViewModel>(sqlQuery).ToList();

            sqlQuery = "SELECT RTRIM(PONUMBER) PaymentPurchaseOrder "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30700 "
                + "WHERE PMNTNMBR = '" + id + "'";

            ViewBag.PurchaseOrders = _repository.ExecuteQuery<PaymentRequestPurchOrderViewModel>(sqlQuery).ToList();

            if (paymentRequest == null)
            {
                return HttpNotFound();
            }

            switch (Account.GetAccount(User.Identity.GetUserName()).Department)
            {
                case "COMPRAS":
                case "ADUANAS":
                    return View(status == 4 ? "InquiryPurchase" : "EditPurchase", paymentRequest);
                case "NEGOCIOS":
                    return View(status == 4 ? "InquirySales" : "EditSales", paymentRequest);
            }

            return status == 4 ? View("Inquiry", paymentRequest) : View(paymentRequest);
        }

        public ActionResult Inquiry(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PaymentRequest", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            string sqlQuery = "SELECT RTRIM(A.PMNTNMBR) PaymentRequestId, RTRIM(A.COMMENT1) Description, B.TXTFIELD Note, CONVERT(NUMERIC(32,2), A.CHEKTOTL) Amount, "
                + "C.DEPTMTID DepartmentId, C.PRIORITY Priority, C.PAYMTERM PaymentCondition, A.DOCDATE DocumentDate, A.CURNCYID Currency, "
                + "A.VENDORID VendorId, A.VENDNAME VendName, C.REQSTDBY Requester, A.APPLDAMT ApplyAmount, A.BACHNUMB BatchNumber "
                + "SELECT PMNTNMBR, COMMENT1, CHEKTOTL, DOCDATE, CURNCYID, VENDORID, VENDNAME, BACHNUMB, APPLDAMT, NOTEINDX FROM " + Helpers.InterCompanyId + ".dbo.PM10300 "
                + "UNION ALL "
                + "SELECT VCHRNMBR PMNTNMBR, TRXDSCRN COMMENT1, DOCAMNT CHEKTOTL, DOCDATE, CURNCYID, VENDORID, VNDCHKNM VENDNAME, BACHNUMB, 0 APPLDAMT, NOTEINDX "
                + "FROM " + Helpers.InterCompanyId + ".dbo.PM30200 ) A "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY03900 B "
                + "ON A.NOTEINDX = B.NOTEINDX "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP30900 C "
                + "ON A.PMNTNMBR = C.PMNTNMBR "
                + "WHERE A.PMNTNMBR = '" + id + "'";

            PaymentRequest paymentRequest = _repository.ExecuteScalarQuery<PaymentRequest>(sqlQuery);

            sqlQuery = "SELECT RTRIM(DEPTMTID) Department, CONVERT(NUMERIC(32,2), AMOUNT) ChargeAmount "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30800 "
                + "WHERE PMNTNMBR = '" + id + "'";
            ViewBag.Charges = _repository.ExecuteQuery<PaymentRequestChargeViewModel>(sqlQuery).ToList();

            sqlQuery = "SELECT RTRIM(APTODCNM) DocumentNumber, CONVERT(NUMERIC(32,2), APPLDAMT) DocumentAmount "
                + "FROM " + Helpers.InterCompanyId + ".dbo.PM10200 "
                + "WHERE VCHRNMBR = '" + id + "'";
            ViewBag.InvoicesApplied = _repository.ExecuteQuery<PaymentRequestInvoiceViewModel>(sqlQuery).ToList();

            if (paymentRequest == null)
            {
                return HttpNotFound();
            }

            return View(paymentRequest);
        }

        public ActionResult Details(string id)
        {
            if (id != null)
            {
                string sqlQuery = "";
                sqlQuery = "SELECT USERID Id, CONVERT(NVARCHAR(25), CREATDTE) Descripción, "
                    + "(CASE WFSTS WHEN 1 THEN 'Enviado' WHEN 2 THEN 'Rechazado' WHEN 3 THEN 'En aprobacion' WHEN 4 THEN 'Aprobado' ELSE 'Enviado' END) DataExtended "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.LPWF00201 "
                    + "WHERE DOCNUM = '" + id + "' ";

                var aprovalInformation = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();

                ViewBag.AprovalInformation = aprovalInformation;
            }

            return PartialView();
        }

        [HttpPost]
        public JsonResult SavePaymentRequest(PaymentRequest request, int postType = 0)
        {
            bool xStatus;
            var order = 16384;

            try
            {
                _repository.ExecuteCommand(String.Format("INTRANET.dbo.BatchInsert '{0}','{1}','{2}','{3}','{4}','{5}'",
                    Helpers.InterCompanyId,
                    Account.GetAccount(User.Identity.GetUserName()).Department == "NEGOCIOS"
                        ? request.BatchNumber
                        : request.PaymentRequestId + "_" + Account.GetAccount(User.Identity.GetUserName()).UserId,
                    Math.Abs(request.Amount), request.Currency,
                    Account.GetAccount(User.Identity.GetUserName()).UserId,
                    Account.GetAccount(User.Identity.GetUserName()).Department == "NEGOCIOS"
                        ? "PM_Payment"
                        : "XPM_Cchecks"));

                if (Account.GetAccount(User.Identity.GetUserName()).Department == "NEGOCIOS")
                {
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHCOMNT = '" +
                                               request.Description + "' WHERE BACHNUMB = '" + request.BatchNumber +
                                               "' AND SERIES = 4 AND BCHSOURC = 'PM_Payment'");
                }
                else
                {
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHCOMNT = '" +
                                               request.Description + "' WHERE BACHNUMB = '" + request.PaymentRequestId +
                                               "_" + Account.GetAccount(User.Identity.GetUserName()).UserId +
                                               "' AND SERIES = 4 AND BCHSOURC = 'XPM_Cchecks'");
                }

                if (Account.GetAccount(User.Identity.GetUserName()).Department == "NEGOCIOS")
                {
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10400 WHERE PMNTNMBR = '" +
                                               request.PaymentRequestId + "'");

                    var secuencia = HelperLogic.AsignaciónSecuencia("PM10400",
                        Account.GetAccount(User.Identity.GetUserName()).UserId);
                    _repository.ExecuteCommand(
                        String.Format(
                            "INTRANET.dbo.PaymentInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9:yyyyMMdd}','{10}', '{11}'",
                            Helpers.InterCompanyId, request.BatchNumber, request.PaymentRequestId, request.Note,
                            Math.Abs(request.Amount - request.ApplyAmount),
                            request.ApplyAmount, request.VendorId, request.Description, request.Currency,
                            request.DocumentDate, secuencia, Account.GetAccount(User.Identity.GetUserName()).UserId));
                }
                else
                {
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10300 WHERE PMNTNMBR = '" +
                                               request.PaymentRequestId + "'");
                    _repository.ExecuteCommand(
                        String.Format(
                            "INTRANET.dbo.PaymentRequestInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9:yyyyMMdd}','{10}','{11}','{12}'",
                            Helpers.InterCompanyId,
                            request.PaymentRequestId + "_" + Account.GetAccount(User.Identity.GetUserName()).UserId,
                            request.PaymentRequestId, request.Note, Math.Abs(request.Amount - request.ApplyAmount),
                            request.ApplyAmount, request.VendorId, request.Description, request.Currency, request.DocumentDate, request.DepartmentId,
                            request.Priority, Account.GetAccount(User.Identity.GetUserName()).UserId));
                }

                _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10100 WHERE VCHRNMBR = '" +
                                           request.PaymentRequestId + "' AND CNTRLTYP = 1");
                _repository.ExecuteCommand(String.Format(
                    "INTRANET.dbo.PaymentGeneralLedgerInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'",
                    Helpers.InterCompanyId, request.PaymentRequestId, request.DepartmentId, order, request.VendorId,
                    request.Amount, 0, 1,
                    request.Currency, Account.GetAccount(User.Identity.GetUserName()).UserId));

                order += 16384;

                _repository.ExecuteCommand(String.Format(
                    "INTRANET.dbo.PaymentGeneralLedgerInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'",
                    Helpers.InterCompanyId, request.PaymentRequestId, request.DepartmentId, order, request.VendorId, 0,
                    request.Amount, 2,
                    request.Currency, Account.GetAccount(User.Identity.GetUserName()).UserId));

                _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM00400 WHERE CNTRLNUM = '" +
                                           request.PaymentRequestId + "'");

                _repository.ExecuteCommand(String.Format(
                    "INTRANET.dbo.PaymentControlInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}'",
                    Helpers.InterCompanyId, request.PaymentRequestId, request.VendorId, request.Amount,
                    request.Currency, Account.GetAccount(User.Identity.GetUserName()).UserId,
                    Account.GetAccount(User.Identity.GetUserName()).Department == "NEGOCIOS" ? "PM_Payment" : "XPM_Cchecks"));

                _repository.ExecuteCommand(String.Format(
                    "LODYNDEV.dbo.LPPOP30900SI '{0}','{1}','{2}','{3}','{4}','{5}','{6}'",
                    Helpers.InterCompanyId, request.PaymentRequestId,
                    Account.GetAccount(User.Identity.GetUserName()).Department == "NEGOCIOS"
                        ? "NEGOCIOS"
                        : request.DepartmentId, request.Priority,
                    request.PaymentCondition, request.Requester,
                    Account.GetAccount(User.Identity.GetUserName()).UserId));

                _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10200 WHERE VCHRNMBR = '" +
                                           request.PaymentRequestId + "' AND DOCTYPE = 5");

                if (request.PaymentRequestInvoiceLines != null)
                {
                    foreach (var item in request.PaymentRequestInvoiceLines)
                    {
                        _repository.ExecuteCommand(
                            String.Format(
                                "INTRANET.dbo.ApplyPayablesDocuments '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}'",
                                Helpers.InterCompanyId, request.VendorId, request.DocumentDate.ToString("yyyyMMdd"),
                                request.PaymentRequestId, item.DocumentNumber, item.DocumentAmount,
                                request.Currency, Account.GetAccount(User.Identity.GetUserName()).UserId));
                    }
                }

                _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.LPPOP30700 WHERE PMNTNMBR = '" +
                                           request.PaymentRequestId + "'");
                if (request.PaymentRequestPurchaseOrders != null)
                {

                    foreach (var item in request.PaymentRequestPurchaseOrders)
                    {
                        _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPPOP30700SI '{0}','{1}','{2}','{3}'",
                            Helpers.InterCompanyId, request.PaymentRequestId.Trim(), item.PaymentPurchaseOrder.Trim(),
                            Account.GetAccount(User.Identity.GetUserName()).UserId));
                    }
                }

                _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.LPPOP30800 WHERE PMNTNMBR = '" +
                                           request.PaymentRequestId + "'");
                if (request.PaymentRequestChargeLines != null)
                {
                    foreach (var item in request.PaymentRequestChargeLines)
                    {
                        _repository.ExecuteCommand(
                            String.Format("LODYNDEV.dbo.LPPOP30800SI '{0}','{1}','{2}','{3}','{4}'",
                                Helpers.InterCompanyId, request.PaymentRequestId.Trim(), item.Department.Trim(),
                                item.ChargeAmount, Account.GetAccount(User.Identity.GetUserName()).UserId));
                    }
                }

                if (postType == 1)
                {
                    _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPWF00101SI '{0}','{1}','{2}','{3}','{4}'",
                        Helpers.InterCompanyId, request.PaymentRequestId, request.Description, 1, 3));

                    _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPWF00201SI '{0}','{1}','{2}','{3}','{4}'",
                        Helpers.InterCompanyId, request.PaymentRequestId,
                        Account.GetAccount(User.Identity.GetUserName()).UserId, "", 1));
                }

                xStatus = true;
            }
            catch
            {
                xStatus = false;
            }

            return new JsonResult {Data = new {status = xStatus}};
        }

        [HttpPost]
        public ActionResult InvoiceApply(string vendorId = "", string currency = "")
        {
            try
            {
                List<Lookup> lookup = null;
                string sqlQuery = "";

                sqlQuery = "SELECT A.DOCNUMBR Id, A.DOCAMNT Descripción, A.DOCDATE DataExtended FROM "
                    /*+ "(SELECT RTRIM(DOCNUMBR) DOCNUMBR, CONVERT(NVARCHAR(20), CONVERT(NUMERIC(32,2), DOCAMNT)) DOCAMNT, CONVERT(nvarchar(20), "
                    + "CONVERT(DATE, DOCDATE, 112)) DOCDATE, VENDORID, CURTRXAM, CURNCYID  FROM " + Helpers.InterCompanyId + ".dbo.PM10000 "
                    + "UNION ALL "*/
                    + "(SELECT RTRIM(DOCNUMBR) DOCNUMBR, CONVERT(NVARCHAR(20), CONVERT(NUMERIC(32,2), CURTRXAM)) DOCAMNT, CONVERT(nvarchar(20), "
                    + "CONVERT(DATE, DOCDATE, 112)) DOCDATE, VENDORID, CURTRXAM, CURNCYID  FROM " + Helpers.InterCompanyId + ".dbo.PM20000 WHERE DOCTYPE IN (1, 2, 3) AND CURTRXAM > 0) A "
                    + "WHERE A.VENDORID = '" + vendorId + "' AND RTRIM(A.CURNCYID) = '" + currency + "'";

                lookup = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();
                return Json(lookup, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        public JsonResult UnblockSecuence(string secuencia, string formulario, string usuario)
        {
            HelperLogic.DesbloqueoSecuencia(secuencia, "PM10300", Account.GetAccount(User.Identity.GetUserName()).UserId);
            return Json("");
        }

        [HttpPost]
        public JsonResult AttachFile(HttpPostedFileBase fileData, string paymentRequestId)
        {
            bool status;

            try
            {
                byte[] fileStream = null;
                using (var binaryReader = new BinaryReader(fileData.InputStream))
                {
                    fileStream = binaryReader.ReadBytes(fileData.ContentLength);
                }

                string fileName = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1];
                string fileType = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].Split('.')[1];

                _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachmentInsert '{0}','{1}','{2}',{3},'{4}','{5}','{6}'",
                        Helpers.InterCompanyId, paymentRequestId, fileName, "0x" + BitConverter.ToString(fileStream).Replace("-", String.Empty),
                        fileType, Account.GetAccount(User.Identity.GetUserName()).UserId, "NOTAS"));
                status = true;
            }
            catch
            {
                status = false;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult SendWorkFlow(string paymentRequestId)
        {
            bool status = false;

            try
            {
                _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPWF00101SI '{0}','{1}','{2}','{3}','{4}'",
                            Helpers.InterCompanyId, paymentRequestId, "", 1, 3));

                _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPWF00201SI '{0}','{1}','{2}','{3}','{4}'",
                    Helpers.InterCompanyId, paymentRequestId, Account.GetAccount(User.Identity.GetUserName()).UserId, "", 1));

                status = true;
            }
            catch (Exception)
            {
                status = false;
            }

            return new JsonResult { Data = new { status } };
        }

        public class AttachmentViewModel
        {
            public HttpPostedFileBase FileData { get; set; }
        }

        [HttpPost]
        public ActionResult LoadAttachmentFiles(string paymentRequestId)
        {
            try
            {
                List<string> files = new List<string>();
                string sqlQuery = "SELECT RTRIM(fileName) FileName FROM " + Helpers.InterCompanyId + ".dbo.CO00105 WHERE DOCNUMBR = '" + paymentRequestId + "' AND DELETE1 = 0";
                files = _repository.ExecuteQuery<string>(sqlQuery).ToList();
                return Json(files, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        public ActionResult Download(string documentId, string FileName)
        {
            string sqlQuery = "SELECT BinaryBlob, FileType, A.fileName "
                + "FROM " + Helpers.InterCompanyId + ".dbo.CO00105 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.coAttachmentItems B ON A.Attachment_ID = B.Attachment_ID "
                + "WHERE A.DOCNUMBR = '" + documentId + "' AND A.DELETE1 = 0 AND RTRIM(A.fileName) = '" + FileName + "'";

            DataTable adjunto = ConnectionDb.GetDt(sqlQuery);
            byte[] contents = null;
            string fileType = "";
            string fileName = "";

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

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult Print(string id)
        {
            string status;
            bool isTwoAprovers = false;
            List<short> solicitudes = new List<short>();

            try
            {
                status = "OK";
                string sqlQuery = "SELECT A.WFSTS FROM " + Helpers.InterCompanyId + ".dbo.LPWF00201 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPWF00101 B "
                + "ON A.DOCNUM = B.DOCNUM "
                + "WHERE A.DOCNUM = '" + id + "' AND TYPE = 3";

                solicitudes = _repository.ExecuteQuery<short>(sqlQuery).ToList();

                #region Imagen

                if (solicitudes != null)
                {
                    foreach (var item in solicitudes)
                    {
                        if (item == 3)
                        {
                            isTwoAprovers = true;
                            break;
                        }
                    }
                }


                if (isTwoAprovers)
                {
                    HelperLogic.InsertSignature(HelperLogic.GetAproverPayment());
                    HelperLogic.InsertSignaturePayment(HelperLogic.GetSecondAproverPayment());
                }
                else
                {
                    if (solicitudes != null)
                    {
                        foreach (var item in solicitudes)
                        {
                            if (item == 4)
                            {
                                isTwoAprovers = true;
                                break;
                            }
                        }
                    }

                    if (isTwoAprovers)
                    {
                        HelperLogic.InsertSignature(HelperLogic.GetAproverPayment());
                        HelperLogic.InsertSignaturePayment("");
                    }
                    else
                    {
                        HelperLogic.InsertSignature("");
                        HelperLogic.InsertSignaturePayment("");
                    }
                }

                #endregion

                ReportHelper.Export(Helpers.ReportPath + "Pagos", Server.MapPath("~/PDF/Pago/") + id + ".pdf",
                    String.Format("LODYNDEV.dbo.LPPOP10300R1 '{0}','{1}'", Helpers.InterCompanyId, id), 2, ref status);
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, path = Helpers.ReportPath + @"Requisicion\" + id + ".pdf" } };
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult PrintDepartment(string id)
        {
            string status;
            List<short> solicitudes = new List<short>();

            try
            {
                status = "OK";
                string sqlQuery = "SELECT ISNULL(DEPTMTID,'') DEPARTAMENTO "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30900  "
                    + "WHERE PMNTNMBR = '" + id.Trim() + "'";

                string departamento = _repository.ExecuteScalarQuery<string>(sqlQuery);

                sqlQuery = "SELECT WFSTS FROM " + Helpers.InterCompanyId + ".dbo.LPWF00101 "
                + "WHERE DOCNUM = '" + id + "' AND TYPE = 3";

                short Status = _repository.ExecuteScalarQuery<short>(sqlQuery);

                if (Status == 4 || Status == 5)
                    HelperLogic.InsertSignature(HelperLogic.GetAproverByDepartmentDescription(departamento));
                else
                    HelperLogic.InsertSignature("");
                
                ReportHelper.Export(Helpers.ReportPath + "Pagos", Server.MapPath("~/PDF/Pago/") + id + ".pdf",
                    string.Format("LODYNDEV.dbo.LPPOP10300R2 '{0}','{1}'",
                                Helpers.InterCompanyId, id), 4, ref status);
                
            }
            catch(Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, path = Helpers.ReportPath + @"Requisicion\" + id + ".pdf" } };
        }

        [OutputCache(Duration =0)]
        [HttpPost]
        public ActionResult PrintSales(string id)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Pagos", Server.MapPath("~/PDF/Pago/") + id + ".pdf",
                    String.Format("LODYNDEV.dbo.LPPOP10400R1 '{0}','{1}'",
                        Helpers.InterCompanyId, id), 23, ref xStatus);
            }
            catch (Exception e)
            {
                xStatus = e.Message;
            }

            return new JsonResult {Data = new {status = xStatus}};
        }

        [HttpPost]
        public ActionResult CreateVendor(string rnc, string vendName, string checkName)
        {
            string status = "";
            try
            {
                ServiceContract service = new ServiceContract();
                service.CreateVendor(new GpVendor { Name = vendName, Rnc = rnc, CheckName = checkName },
                    Request.Cookies["UserAccount"]["username"], Request.Cookies["UserAccount"]["password"]);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult GetFiscalData(string rnc)
        {
            try
            {
                string name = "";
                int count = 0;
                string sqlQuery = "SELECT TOP 1 RTRIM(CMPNAME) NAME FROM LODYNDEV.dbo.DGIN50100 WHERE TAXREGTN = '" + rnc + "'";
                count = _repository.ExecuteQuery<string>(sqlQuery).Count();
                if (count > 0)
                {
                    name = _repository.ExecuteScalarQuery<string>(sqlQuery);
                }
                else
                {
                    name = "";
                }
                return Json(name, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }
    }
}