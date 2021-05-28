using Microsoft.AspNet.Identity;
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
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = "SELECT A.MODULE Module, A.VCHRNMBR VoucherNumber, A.VENDORID VendorId, A.VENDNAME VendName, A.DOCNUMBR DocumentNumber, " +
                $"A.DOCDATE DocumentDate, A.CURNCYID Currency, A.TRXDSCRN Description, CONVERT(NUMERIC(32, 2), A.PRCHAMNT) PurchaseAmount, CONVERT(NUMERIC(32, 2), A.TRDISAMT) DiscountAmount, " +
                $"CONVERT(NUMERIC(32, 2), A.TAXAMNT) TaxAmount, CONVERT(NUMERIC(32, 2), A.DOCAMNT) Amount, CONVERT(NUMERIC(32, 2), A.MSCCHAMT) MiscellaneousAmount, (CASE A.DOCTYPE WHEN 1 THEN 1 ELSE 2 END) Type, " +
                $"ISNULL(C.TXTFIELD, '') Note, ISNULL(A.TAXDTLID, '') TaxDetailId FROM ( " +
                $"SELECT 1 MODULE, RTRIM(A.VCHNUMWK) VCHRNMBR, A.VENDORID, B.VENDNAME, A.DOCNUMBR, A.DOCAMNT, A.DOCDATE, A.CURNCYID, A.TRXDSCRN, PRCHAMNT, TRDISAMT, A.TAXAMNT, FRTAMNT, MSCCHAMT, A.DOCTYPE, A.NOTEINDX, C.TAXDTLID, B.VNDCLSID " +
                $"FROM {Helpers.InterCompanyId}.dbo.PM10000 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.PM00200 B ON A.VENDORID = B.VENDORID " +
                $"LEFT  JOIN  {Helpers.InterCompanyId}.dbo.PM10500 C ON A.VCHRNMBR = C.VCHRNMBR AND A.VENDORID = C.VENDORID " +
                $"UNION ALL " +
                $"SELECT 2 MODULE, RTRIM(A.RMDNUMWK) VCHRNMBR, A.CUSTNMBR VENDORID, B.CUSTNAME VENDNAME, A.DOCNUMBR, A.DOCAMNT, A.DOCDATE, A.CURNCYID, A.DOCDESCR TRXDSCRN, A.SLSAMNT PRCHAMNT, A.TRDISAMT, " +
                $"A.TAXAMNT, A.FRTAMNT, A.MISCAMNT MSCCHAMT, CASE A.DOCTYPE WHEN 2 THEN 1 ELSE 2 END DOCTYPE, A.NOTEINDX, C.TAXDTLID, B.CUSTCLAS VNDCLSID " +
                $"FROM {Helpers.InterCompanyId}.dbo.RM10301 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.RM00101 B ON A.CUSTNMBR = B.CUSTNMBR " +
                $"LEFT  JOIN {Helpers.InterCompanyId}.dbo.RM10601 C ON A.CUSTNMBR = C.CUSTNMBR AND A.DOCNUMBR = C.DOCNUMBR) A " +
                $"LEFT  JOIN {Helpers.InterCompanyId}.dbo.SY03900 C ON A.NOTEINDX = C.NOTEINDX " +
                $"LEFT  JOIN {Helpers.InterCompanyId}.dbo.PM10500 D ON A.VCHRNMBR = D.VCHRNMBR AND A.VENDORID = D.VENDORID " +
                $"WHERE A.VNDCLSID IN ('LOCALSPOT', 'LOCALUNR')";

            var accountPayablesList = _repository.ExecuteQuery<AccountPayablesViewModel>(sqlQuery);

            return View(accountPayablesList.ToList());
        }

        public ActionResult Create()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountPayables", "Index"))
                return RedirectToAction("NotPermission", "Home");
            if (ViewBag.RequestSecuence == null)
                ViewBag.RequestSecuence = HelperLogic.AsignaciónSecuencia("PM10000", Account.GetAccount(User.Identity.GetUserName()).UserId);
            ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;
            ViewBag.Discount = 0.00;
            ViewBag.Freight = 0.00;
            ViewBag.Miscellaneous = 0.00;
            ViewBag.Tax = 0.00;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Module,Type,Description,VendorId,VendName,DocumentNumber,TaxDetailId,PurchaseAmount,DiscountAmount,FreightAmount,MiscellaneousAmount,TaxAmount,Currency,Note,DocumentDate,DueDate,PostType")] AccountPayables accountPayables)
        {
            var isValid = false;
            string voucherNumber;
            if (accountPayables.Module == AccountPayablesModule.Compras)
                voucherNumber = _repository.ExecuteScalarQuery<string>($"INTRANET.dbo.GetNextNumberAccountPayables '{Helpers.InterCompanyId}'");
            else
            {
                if (accountPayables.Type == AccountPayablesType.NC)
                    voucherNumber = _repository.ExecuteScalarQuery<string>($"INTRANET.dbo.GetNextNumberAccountReceivables '{Helpers.InterCompanyId}','{7}'");
                else
                    voucherNumber = _repository.ExecuteScalarQuery<string>($"INTRANET.dbo.GetNextNumberAccountReceivables '{Helpers.InterCompanyId}','{3}'");
            }
            var service = new ServiceContract();
            if (accountPayables.Module == AccountPayablesModule.Compras)
            {
                var payablesDocument = new GpPayablesDocument
                {
                    Currency = accountPayables.Currency,
                    DocumentDate = accountPayables.DocumentDate,
                    DocumentNumber = accountPayables.DocumentNumber,
                    FreightAmount = accountPayables.FreightAmount,
                    MiscellaneousAmount = accountPayables.MiscellaneousAmount,
                    PurchaseAmount = accountPayables.PurchaseAmount,
                    TaxAmount = accountPayables.TaxAmount,
                    TaxDetail = accountPayables.TaxDetailId,
                    TradeDiscountAmount = accountPayables.DiscountAmount,
                    VendorId = accountPayables.VendorId,
                    VoucherNumber = voucherNumber,
                    Note = accountPayables.Note,
                    Description = accountPayables.Description,
                    DueDate = accountPayables.DueDate
                };
                if (accountPayables.Type == AccountPayablesType.NC)
                {
                    if (service.CreatePayablesCreditNote(payablesDocument))
                        isValid = true;
                }
                else
                {
                    if (service.CreatePayablesInvoice(payablesDocument))
                        isValid = true;
                }
                if (isValid)
                {
                    if (!string.IsNullOrEmpty(payablesDocument.Note))
                        _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachPayableNote '{0}','{1}','{2}'", Helpers.InterCompanyId, payablesDocument.VoucherNumber, payablesDocument.Note));
                }
            }
            else
            {
                var receivablesDocument = new GpCreditNote
                {
                    Cliente = accountPayables.VendorId,
                    Codigo = voucherNumber,
                    Monto = accountPayables.PurchaseAmount,
                    Moneda = accountPayables.Currency,
                    CompanyId = Helpers.CompanyIdWebServices,
                    Fecha = accountPayables.DocumentDate,
                    Descuento = accountPayables.DiscountAmount,
                    Lote = accountPayables.DocumentNumber,
                    DueDate = accountPayables.DueDate
                };
                if (accountPayables.Type == AccountPayablesType.NC)
                    service.CreateReceivablesCreditNote(receivablesDocument);
                else
                    service.CreateReceivablesDebitNote(receivablesDocument);
                isValid = true;
            }

            if (isValid)
                return RedirectToAction("Index");
            
            ViewBag.Discount = accountPayables.DiscountAmount;
            ViewBag.Freight = accountPayables.FreightAmount;
            ViewBag.Miscellaneous = accountPayables.MiscellaneousAmount;
            ViewBag.Tax = accountPayables.TaxAmount;
            return View(accountPayables);
        }

        public ActionResult Edit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountPayables", "Index"))
                return RedirectToAction("NotPermission", "Home");
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var sqlQuery = "SELECT TOP 1 A.MODULE Module, A.VCHRNMBR VoucherNumber, A.VENDORID VendorId, A.VENDNAME VendName, A.DOCNUMBR DocumentNumber, " +
                $"A.DOCDATE DocumentDate, A.CURNCYID Currency, A.TRXDSCRN Description, CONVERT(NUMERIC(32, 2), A.PRCHAMNT) PurchaseAmount, CONVERT(NUMERIC(32, 2), A.TRDISAMT) DiscountAmount, " +
                $"CONVERT(NUMERIC(32, 2), A.TAXAMNT) TaxAmount, CONVERT(NUMERIC(32, 2), A.FRTAMNT) FreightAmount, CONVERT(NUMERIC(32, 2), A.MSCCHAMT) MiscellaneousAmount, (CASE A.DOCTYPE WHEN 1 THEN 1 ELSE 2 END) Type, " +
                $"ISNULL(C.TXTFIELD, '') Note, ISNULL(A.TAXDTLID, '') TaxDetailId, ISNULL(A.DUEDATE, A.DOCDATE) DueDate FROM ( " +
                $"SELECT 1 MODULE, RTRIM(A.VCHNUMWK) VCHRNMBR, A.VENDORID, B.VENDNAME, A.DOCNUMBR, A.DOCAMNT, A.DOCDATE, A.CURNCYID, A.TRXDSCRN, PRCHAMNT, TRDISAMT, A.TAXAMNT, FRTAMNT, MSCCHAMT, A.DOCTYPE, " +
                $"A.NOTEINDX, C.TAXDTLID, B.VNDCLSID, CASE A.DUEDATE WHEN '1900-01-01' THEN A.DOCDATE ELSE A.DUEDATE END DUEDATE " +
                $"FROM {Helpers.InterCompanyId}.dbo.PM10000 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.PM00200 B ON A.VENDORID = B.VENDORID " +
                $"LEFT  JOIN  {Helpers.InterCompanyId}.dbo.PM10500 C ON A.VCHRNMBR = C.VCHRNMBR AND A.VENDORID = C.VENDORID " +
                $"UNION ALL " +
                $"SELECT 2 MODULE, RTRIM(A.RMDNUMWK) VCHRNMBR, A.CUSTNMBR VENDORID, B.CUSTNAME VENDNAME, A.DOCNUMBR, A.DOCAMNT, A.DOCDATE, A.CURNCYID, A.DOCDESCR TRXDSCRN, A.SLSAMNT PRCHAMNT, A.TRDISAMT, " +
                $"A.TAXAMNT, A.FRTAMNT, A.MISCAMNT MSCCHAMT, CASE A.DOCTYPE WHEN 2 THEN 1 ELSE 2 END DOCTYPE, A.NOTEINDX, C.TAXDTLID, B.CUSTCLAS VNDCLSID, CASE A.DUEDATE WHEN '1900-01-01' THEN A.DOCDATE ELSE A.DUEDATE END DUEDATE  " +
                $"FROM {Helpers.InterCompanyId}.dbo.RM10301 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.RM00101 B ON A.CUSTNMBR = B.CUSTNMBR " +
                $"LEFT  JOIN {Helpers.InterCompanyId}.dbo.RM10601 C ON A.CUSTNMBR = C.CUSTNMBR AND A.DOCNUMBR = C.DOCNUMBR) A " +
                $"LEFT  JOIN {Helpers.InterCompanyId}.dbo.SY03900 C ON A.NOTEINDX = C.NOTEINDX " +
                $"LEFT  JOIN {Helpers.InterCompanyId}.dbo.PM10500 D ON A.VCHRNMBR = D.VCHRNMBR AND A.VENDORID = D.VENDORID " +
                $"WHERE A.VCHRNMBR = '{id}'";

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
        public ActionResult Edit([Bind(Include = "Module,Type,VoucherNumber,Description,VendorId,VendName,DocumentNumber,TaxDetailId,PurchaseAmount,DiscountAmount,FreightAmount,MiscellaneousAmount,TaxAmount,Currency,Note,DocumentDate,DueDate,PostType")] AccountPayables accountPayables)
        {
            var isValid = false;

            var service = new ServiceContract();
            if (accountPayables.Module == AccountPayablesModule.Compras)
            {
                var payablesDocument = new GpPayablesDocument
                {
                    Currency = accountPayables.Currency,
                    DocumentDate = accountPayables.DocumentDate,
                    DocumentNumber = accountPayables.DocumentNumber,
                    FreightAmount = accountPayables.FreightAmount,
                    MiscellaneousAmount = accountPayables.MiscellaneousAmount,
                    PurchaseAmount = accountPayables.PurchaseAmount,
                    TaxAmount = accountPayables.TaxAmount,
                    TaxDetail = accountPayables.TaxDetailId,
                    TradeDiscountAmount = accountPayables.DiscountAmount,
                    VendorId = accountPayables.VendorId,
                    VoucherNumber = accountPayables.VoucherNumber,
                    Note = accountPayables.Note,
                    Description = accountPayables.Description,
                    DueDate = accountPayables.DueDate
                };
                if (accountPayables.Type == AccountPayablesType.NC)
                {
                    var amountDebit = _repository.ExecuteScalarQuery<decimal>("SELECT TOP 1 ISNULL(CURTRXAM, 0) FROM " + Helpers.InterCompanyId + ".dbo.PM10000 WHERE VCHNUMWK = '" + accountPayables.VoucherNumber + "' AND DOCTYPE = 5");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10000 WHERE VCHNUMWK = '" + accountPayables.VoucherNumber + "' AND DOCTYPE = 5");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10500 WHERE VCHRNMBR = '" + accountPayables.VoucherNumber + "' AND DOCTYPE = 5");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM00400 WHERE CNTRLNUM = '" + accountPayables.VoucherNumber + "' AND DOCTYPE = 5");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10100 WHERE VCHRNMBR = '" + accountPayables.VoucherNumber + "' AND PSTGSTUS = 0");
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHTOTAL -= '" + amountDebit + "', NUMOFTRX -= 1 WHERE BACHNUMB = '" + accountPayables.DocumentNumber + "'");
                    if (service.CreatePayablesCreditNote(payablesDocument))
                        isValid = true;
                }
                else
                {
                    var amountDebit = _repository.ExecuteScalarQuery<decimal>("SELECT TOP 1 ISNULL(CURTRXAM, 0) FROM " + Helpers.InterCompanyId + ".dbo.PM10000 WHERE VCHNUMWK = '" + accountPayables.VoucherNumber + "' AND DOCTYPE = 1");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10000 WHERE VCHNUMWK = '" + accountPayables.VoucherNumber + "' AND DOCTYPE = 1");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10500 WHERE VCHRNMBR = '" + accountPayables.VoucherNumber + "' AND DOCTYPE = 1");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM00400 WHERE CNTRLNUM = '" + accountPayables.VoucherNumber + "' AND DOCTYPE = 1");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10100 WHERE VCHRNMBR = '" + accountPayables.VoucherNumber + "' AND PSTGSTUS = 0");
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHTOTAL -= '" + amountDebit + "', NUMOFTRX -= 1 WHERE BACHNUMB = '" + accountPayables.DocumentNumber + "'");

                    if (service.CreatePayablesInvoice(payablesDocument))
                        isValid = true;
                }
                if (isValid)
                {
                    if (!string.IsNullOrEmpty(payablesDocument.Note))
                        _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachPayableNote '{0}','{1}','{2}'", Helpers.InterCompanyId, payablesDocument.VoucherNumber, payablesDocument.Note));
                }
            }
            else
            {
                var receivablesDocument = new GpCreditNote
                {
                    Cliente = accountPayables.VendorId,
                    Codigo = accountPayables.VoucherNumber,
                    Monto = accountPayables.PurchaseAmount,
                    Moneda = accountPayables.Currency,
                    CompanyId = Helpers.CompanyIdWebServices,
                    Descripción = accountPayables.Description,
                    Fecha = accountPayables.DocumentDate,
                    Descuento = accountPayables.DiscountAmount,
                    Lote = accountPayables.DocumentNumber,
                    DueDate = accountPayables.DueDate
                };
                if (accountPayables.Type == AccountPayablesType.NC)
                {
                    var amountDebit = _repository.ExecuteScalarQuery<decimal>("SELECT TOP 1 ISNULL(DOCAMNT, 0) FROM " + Helpers.InterCompanyId + ".dbo.RM10301 WHERE DOCNUMBR = '" + accountPayables.DocumentNumber + "' AND RMDTYPAL = 7");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10301 WHERE DOCNUMBR = '" + accountPayables.DocumentNumber + "' AND RMDTYPAL = 7");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10601 WHERE DOCNUMBR = '" + accountPayables.DocumentNumber + "' AND RMDTYPAL = 7");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM00401 WHERE DOCNUMBR = '" + accountPayables.DocumentNumber + "' AND RMDTYPAL = 7");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10101 WHERE DOCNUMBR = '" + accountPayables.DocumentNumber + "' AND DCSTATUS = 1");
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHTOTAL -= '" + amountDebit + "', NUMOFTRX -= 1 WHERE BACHNUMB = '" + accountPayables.DocumentNumber + "'");
                    service.CreateReceivablesCreditNote(receivablesDocument);
                }
                else
                {
                    var amountDebit = _repository.ExecuteScalarQuery<decimal>("SELECT TOP 1 ISNULL(DOCAMNT, 0) FROM " + Helpers.InterCompanyId + ".dbo.RM10301 WHERE DOCNUMBR = '" + accountPayables.DocumentNumber + "' AND RMDTYPAL = 3");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10301 WHERE DOCNUMBR = '" + accountPayables.DocumentNumber + "' AND RMDTYPAL = 3");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10601 WHERE DOCNUMBR = '" + accountPayables.DocumentNumber + "' AND RMDTYPAL = 3");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM00401 WHERE DOCNUMBR = '" + accountPayables.DocumentNumber + "' AND RMDTYPAL = 3");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10101 WHERE DOCNUMBR = '" + accountPayables.DocumentNumber + "' AND DCSTATUS = 0");
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHTOTAL -= '" + amountDebit + "', NUMOFTRX -= 1 WHERE BACHNUMB = '" + accountPayables.DocumentNumber + "'");
                    service.CreateReceivablesDebitNote(receivablesDocument);
                }
                isValid = true;
            }
            if (isValid)
                return RedirectToAction("Index");

            ViewBag.Discount = accountPayables.DiscountAmount;
            ViewBag.Freight = accountPayables.FreightAmount;
            ViewBag.Miscellaneous = accountPayables.MiscellaneousAmount;
            ViewBag.Tax = accountPayables.TaxAmount;
            return View(accountPayables);
        }

        public ActionResult Inquiry(string id)
        {
            var sqlQuery = "SELECT A.VCHRNMBR VoucherNumber, A.VENDORID VendorId, A.VENDNAME VendName, A.DOCNUMBR DocumentNumber, "
                    + "A.DOCDATE DocumentDate, A.CURNCYID Currency, A.TRXDSCRN Description, "
                    + "CONVERT(NUMERIC(32, 2), A.PRCHAMNT) PurchaseAmount, "
                    + "CONVERT(NUMERIC(32, 2), A.TRDISAMT) DiscountAmount, CONVERT(NUMERIC(32, 2), A.TAXAMNT) TaxAmount, "
                    + "CONVERT(NUMERIC(32, 2), A.FRTAMNT) FreightAmount, CONVERT(NUMERIC(32, 2), A.MSCCHAMT) MiscellaneousAmount, "
                    + "(CASE A.DOCTYPE WHEN 1 THEN 1 ELSE 2 END) Type, ISNULL(C.TXTFIELD, '') Note "
                    + "FROM (SELECT RTRIM(A.VCHNUMWK) VCHRNMBR, A.VENDORID, B.VENDNAME, A.DOCNUMBR, A.DOCAMNT, A.DOCDATE, "
                    + "A.CURNCYID, A.TRXDSCRN, PRCHAMNT, TRDISAMT, TAXAMNT, FRTAMNT, MSCCHAMT, DOCTYPE, A.NOTEINDX "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.PM10000 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.PM00200 B ON A.VENDORID = B.VENDORID "
                    + "UNION ALL "
                    + "SELECT RTRIM(A.VCHRNMBR), A.VENDORID, B.VENDNAME, A.DOCNUMBR, A.DOCAMNT, A.DOCDATE, A.CURNCYID, "
                    + "TRXDSCRN, A.PONUMBER, PRCHAMNT, TRDISAMT, TAXAMNT, FRTAMNT, MSCCHAMT, DOCTYPE, A.NOTEINDX "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.PM20000 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.PM00200 B "
                    + "ON A.VENDORID = B.VENDORID) A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY03900 C "
                    + "ON A.NOTEINDX = C.NOTEINDX "
                    + "WHERE A.VCHRNMBR = '" + id + "'";

            var accountPayables = _repository.ExecuteScalarQuery<AccountPayables>(sqlQuery);
            return View(accountPayables);
        }

        [HttpPost]
        public ActionResult Delete(string id, string documentNumber)
        {
            string aStatus;
            try
            {
                var amountDebit = _repository.ExecuteScalarQuery<decimal>("SELECT TOP 1 ISNULL(CURTRXAM, 0) FROM " + Helpers.InterCompanyId + ".dbo.PM10000 WHERE VCHNUMWK = '" + id + "' AND DOCTYPE = 5");
                if (amountDebit > 0)
                {
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10000 WHERE VCHNUMWK = '" + id + "' AND DOCTYPE = 5");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10500 WHERE VCHRNMBR = '" + id + "' AND DOCTYPE = 5");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM00400 WHERE CNTRLNUM = '" + id + "' AND DOCTYPE = 5");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10100 WHERE VCHRNMBR = '" + id + "' AND PSTGSTUS = 0");
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHTOTAL -= '" + amountDebit + "', NUMOFTRX -= 1 WHERE BACHNUMB = '" + documentNumber + "'");
                }

                amountDebit = _repository.ExecuteScalarQuery<decimal>("SELECT TOP 1 ISNULL(CURTRXAM, 0) FROM " + Helpers.InterCompanyId + ".dbo.PM10000 WHERE VCHNUMWK = '" + id + "' AND DOCTYPE = 1");
                if (amountDebit > 0)
                {

                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10000 WHERE VCHNUMWK = '" + id + "' AND DOCTYPE = 1");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10500 WHERE VCHRNMBR = '" + id + "' AND DOCTYPE = 1");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM00400 WHERE CNTRLNUM = '" + id + "' AND DOCTYPE = 1");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.PM10100 WHERE VCHRNMBR = '" + id + "' AND PSTGSTUS = 0");
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHTOTAL -= '" + amountDebit + "', NUMOFTRX -= 1 WHERE BACHNUMB = '" + documentNumber + "'");
                }

                amountDebit = _repository.ExecuteScalarQuery<decimal>("SELECT TOP 1 ISNULL(DOCAMNT, 0) FROM " + Helpers.InterCompanyId + ".dbo.RM10301 WHERE DOCNUMBR = '" + documentNumber + "' AND RMDTYPAL = 7");
                if (amountDebit > 0)
                {
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10301 WHERE DOCNUMBR = '" + documentNumber + "' AND RMDTYPAL = 7");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10601 WHERE DOCNUMBR = '" + documentNumber + "' AND RMDTYPAL = 7");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM00401 WHERE DOCNUMBR = '" + documentNumber + "' AND RMDTYPAL = 7");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10101 WHERE DOCNUMBR = '" + documentNumber + "' AND DCSTATUS = 1");
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHTOTAL -= '" + amountDebit + "', NUMOFTRX -= 1 WHERE BACHNUMB = '" + documentNumber + "'");
                }

                amountDebit = _repository.ExecuteScalarQuery<decimal>("SELECT TOP 1 ISNULL(DOCAMNT, 0) FROM " + Helpers.InterCompanyId + ".dbo.RM10301 WHERE DOCNUMBR = '" + documentNumber + "' AND RMDTYPAL = 3");
                if (amountDebit > 0)
                {
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10301 WHERE DOCNUMBR = '" + documentNumber + "' AND RMDTYPAL = 3");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10601 WHERE DOCNUMBR = '" + documentNumber + "' AND RMDTYPAL = 3");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM00401 WHERE DOCNUMBR = '" + documentNumber + "' AND RMDTYPAL = 3");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.RM10101 WHERE DOCNUMBR = '" + documentNumber + "' AND DCSTATUS = 0");
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHTOTAL -= '" + amountDebit + "', NUMOFTRX -= 1 WHERE BACHNUMB = '" + documentNumber + "'");
                }
                aStatus = "OK";
            }
            catch (Exception ex)
            {
                aStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = aStatus } };
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