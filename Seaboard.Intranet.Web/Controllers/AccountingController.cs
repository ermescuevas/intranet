using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Data.Repository;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class AccountingController : Controller
    {
        private readonly GenericRepository _repository;

        public AccountingController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "Index"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = $"SELECT A.DocumentNumber Id, A.DocumentDescription Descripción, CONVERT(nvarchar(20), (B.ToNumber - B.NextNumber) + 1) DataExtended " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECNCF40101 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.ECNCF40102 B ON A.DocumentNumber = B.HeaderDocumentNumber " +
                $"WHERE(B.ToNumber - B.NextNumber) <= B.AlertNumber AND (B.ToNumber - B.NextNumber) >= 0 ";
            var list = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();
            string ncfAlert = "";
            if (list.Count > 0)
            {
                ncfAlert = "Existen secuencia de comprobantes fiscales que se estan terminando estos son: " + Environment.NewLine;
                foreach (var item in list)
                    ncfAlert += item.Id + " - " + item.Descripción + " - Disponible: " + item.DataExtended + Environment.NewLine;
                ncfAlert += "Debe de solicitar en la DGII mas secuencias de comprobantes ";
            }
            ViewBag.NcfAlert = ncfAlert;
            ViewBag.DailyAmount = _repository.ExecuteScalarQuery<double?>($"SELECT CONVERT(FLOAT, SUM(ISNULL(Total, 0))) FROM {Helpers.InterCompanyId}.dbo.ECPM20100 " +
                $"WHERE CONVERT(DATE, DocumentDate) = CONVERT(DATE, GETDATE()) AND Status = 2") ?? 0;
            ViewBag.MonthlyAmount = _repository.ExecuteScalarQuery<double?>($"SELECT CONVERT(FLOAT, SUM(ISNULL(Total, 0))) FROM {Helpers.InterCompanyId}.dbo.ECPM20100 " +
                $"WHERE MONTH(CONVERT(DATE, DocumentDate)) = MONTH(CONVERT(DATE, GETDATE())) AND YEAR(CONVERT(DATE, DocumentDate)) = YEAR(CONVERT(DATE, GETDATE())) AND Status = 2") ?? 0;
            ViewBag.Suppliers = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.ECPM00200");
            ViewBag.LastInvoices = _repository.ExecuteQuery<AccountingInvoiceHeader>($"SELECT TOP 10 A.SopNumber DocumentNumber, A.Ncf, A.DocumentDate, A.SupplierNumber, A.SupplierName, A.Subtotal, " +
                $"A.TaxAmount, A.Note, A.Status, CASE A.DocumentType WHEN 11 THEN 'Proveedor Informal' ELSE 'Gastos menores' END PhoneNumber, A.Total TotalAmount, A.CurrencyId, A.ExchangeRate " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.ECPM20200 B ON A.SopNumber = B.SopNumber " +
                $"WHERE A.Status = 2 " +
                $"ORDER BY A.SopNumber DESC ");
            ViewBag.LastInvoices = _repository.ExecuteQuery<AccountingInvoiceHeader>($"SELECT TOP 10 A.SopNumber DocumentNumber, A.Ncf, A.DocumentDate, A.SupplierNumber, A.SupplierName, A.Subtotal, " +
                $"A.TaxAmount, A.Note, A.Status, CASE A.DocumentType WHEN 11 THEN 'Proveedor Informal' ELSE 'Gastos menores' END PhoneNumber, A.Total TotalAmount, A.CurrencyId, A.ExchangeRate " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 A " +
                $"WHERE A.Status = 2 " +
                $"ORDER BY A.SopNumber DESC ");
            ViewBag.TotalAmount = _repository.ExecuteScalarQuery<double?>($"SELECT CONVERT(FLOAT, SUM(ISNULL(Total, 0))) FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE Status = 2") ?? 0;
            ViewBag.DailyInvoices = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.ECPM20100 " +
                $"WHERE CONVERT(DATE, DocumentDate) = CONVERT(DATE, DATEADD(MONTH, -1, GETDATE())) AND Status = 2");
            ViewBag.MonthlyInvoices = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.ECPM20100 " +
                $"WHERE MONTH(CONVERT(DATE, DocumentDate)) = MONTH(CONVERT(DATE, DATEADD(MONTH, -1, GETDATE()))) AND YEAR(CONVERT(DATE, DocumentDate)) = YEAR(CONVERT(DATE, DATEADD(MONTH, -1, GETDATE()))) AND Status = 2");

            ViewBag.Series = _repository.ExecuteQuery<string>($"SELECT A.InvMonth + ' ' + CONVERT(NVARCHAR(20), A.InvYear) FROM (SELECT TOP 12 SUM(ISNULL(Total, 0)) AS Spent, " +
                $"CASE MONTH(DocumentDate) WHEN 1 THEN 'Enero' WHEN 2 THEN 'Febrero' WHEN 3 THEN 'Marzo' WHEN 4 THEN 'Abril' WHEN 5 THEN 'Mayo' WHEN 6 THEN 'Junio' WHEN 7 THEN 'Julio' " +
                $"WHEN 8 THEN 'Agosto' WHEN 9 THEN 'Septiembre' " +
                $"WHEN 10 THEN 'Octubre' WHEN 11 THEN 'Noviembre' WHEN 12 THEN 'Diciembre' ELSE 'Enero' END AS InvMonth, YEAR(DocumentDate) InvYear " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE DocumentDate BETWEEN DATEADD(MONTH, -24, GETDATE()) AND CONVERT(DATE, GETDATE()) AND Status = 2 " +
                $"GROUP BY YEAR(DocumentDate), MONTH(DocumentDate) " +
                $"ORDER BY YEAR(DocumentDate) DESC, MONTH(DocumentDate) DESC) A");

            ViewBag.ValuesSupplier = _repository.ExecuteQuery<double?>($"SELECT CONVERT(FLOAT, (CASE A.DocumentType WHEN 11 THEN A.Spent ELSE 0 END)) FROM (SELECT TOP 12 SUM(ISNULL(Total, 0)) AS Spent, " +
                $"CASE MONTH(DocumentDate) WHEN 1 THEN 'Enero' WHEN 2 THEN 'Febrero' WHEN 3 THEN 'Marzo' WHEN 4 THEN 'Abril' WHEN 5 THEN 'Mayo' WHEN 6 THEN 'Junio' WHEN 7 THEN 'Julio' " +
                $"WHEN 8 THEN 'Agosto' WHEN 9 THEN 'Septiembre' " +
                $"WHEN 10 THEN 'Octubre' WHEN 11 THEN 'Noviembre' WHEN 12 THEN 'Diciembre' ELSE 'Enero' END AS InvMonth, DocumentType " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE DocumentDate BETWEEN DATEADD(MONTH, -24, GETDATE()) AND CONVERT(DATE, GETDATE()) AND Status = 2 " +
                $"GROUP BY YEAR(DocumentDate), MONTH(DocumentDate), DocumentType " +
                $"ORDER BY YEAR(DocumentDate) DESC, MONTH(DocumentDate) DESC) A");

            ViewBag.ValuesMinor = _repository.ExecuteQuery<double?>($"SELECT CONVERT(FLOAT, (CASE A.DocumentType WHEN 13 THEN A.Spent ELSE 0 END)) FROM (SELECT TOP 12 SUM(ISNULL(Total, 0)) AS Spent, " +
                $"CASE MONTH(DocumentDate) WHEN 1 THEN 'Enero' WHEN 2 THEN 'Febrero' WHEN 3 THEN 'Marzo' WHEN 4 THEN 'Abril' WHEN 5 THEN 'Mayo' WHEN 6 THEN 'Junio' WHEN 7 THEN 'Julio' " +
                $"WHEN 8 THEN 'Agosto' WHEN 9 THEN 'Septiembre' " +
                $"WHEN 10 THEN 'Octubre' WHEN 11 THEN 'Noviembre' WHEN 12 THEN 'Diciembre' ELSE 'Enero' END AS InvMonth, DocumentType " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE DocumentDate BETWEEN DATEADD(MONTH, -24, GETDATE()) AND CONVERT(DATE, GETDATE()) AND Status = 2 " +
                $"GROUP BY YEAR(DocumentDate), MONTH(DocumentDate), DocumentType " +
                $"ORDER BY YEAR(DocumentDate) DESC, MONTH(DocumentDate) DESC) A");
            return View();
        }

        #region Informal Invoice

        public ActionResult InformalSupplierInvoiceIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "InformalSupplierInvoice"))
                return RedirectToAction("NotPermission", "Home");
            var invoices = _repository.ExecuteQuery<AccountingInvoiceHeader>($"SELECT TOP 500 SopNumber DocumentNumber, Ncf, NcfDueDate, NcfType, DocumentDate, SupplierNumber, SupplierName, TaxRegistrationNumber, " +
                $"Address, City, State, Country, PhoneNumber, Subtotal, TaxAmount, Total TotalAmount, Note, CurrencyId, ExchangeRate, Status " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE DocumentType = 11 ORDER BY SopNumber DESC");
            return View(invoices);
        }

        public ActionResult InformalSupplierInvoice(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "InformalSupplierInvoice"))
                return RedirectToAction("NotPermission", "Home");
            AccountingInvoiceHeader invoice;
            invoice = _repository.ExecuteScalarQuery<AccountingInvoiceHeader>($"SELECT SopNumber DocumentNumber, Ncf, NcfDueDate, NcfType, DocumentDate, SupplierNumber, SupplierName, TaxRegistrationNumber, " +
                $"Address, City, State, Country, PhoneNumber, Subtotal, TaxAmount, Total, Note, CurrencyId, ExchangeRate, Status " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE SopNumber = '{id}'");

            if (invoice == null)
            {
                var fiscalData = GetNextNcfNumber(11);
                invoice = new AccountingInvoiceHeader
                {
                    DocumentDate = DateTime.Now,
                    DocumentNumber = HelperLogic.AsignaciónSecuencia("Accounting", Account.GetAccount(User.Identity.GetUserName()).UserId),
                    NcfType = fiscalData.IncomeType,
                    CurrencyId = "RDPESO",
                    Ncf = fiscalData.Ncf,
                    NcfDueDate = fiscalData.DocumentDate,
                    Note = "",
                    Subtotal = 0,
                    SupplierNumber = "",
                    SupplierName = "",
                    TaxAmount = 0,
                    TotalAmount = 0,
                    DocumentType = 11,
                    Status = 0,
                    Details = new List<AccountingInvoiceDetail>()
                };
            }
            else
                invoice.Details = _repository.ExecuteQuery<AccountingInvoiceDetail>($"SELECT SopNumber DocumentNumber, LineNumber, ItemNumber, ItemDescription, Quantity, Price, TaxAmount, Total " +
                    $"FROM {Helpers.InterCompanyId}.dbo.ECPM20200 WHERE SopNumber = '{id}'").ToList();
            ViewBag.Taxes = _repository.ExecuteQuery<Lookup>($"SELECT TaxPlanId Id, TaxPlanDescription Descripción, CONVERT(NVARCHAR(20), CONVERT(INT, TaxPercent)) DataExtended, '' DataPlus FROM {Helpers.InterCompanyId}.dbo.ECPM40301").ToList();
            ViewBag.VoidTypes = _repository.ExecuteQuery<Lookup>($"SELECT RTRIM(VoidType) Id, RTRIM(VoidTypeDescription) Descripción, '' DataExtended, '' DataPlus FROM {Helpers.InterCompanyId}.dbo.ECPM40201 ORDER BY VoidType").ToList();
            if (invoice.Status == 1)
            {
                var fiscalData = GetNextNcfNumber(11);
                invoice.Ncf = fiscalData.Ncf;
                invoice.NcfDueDate = fiscalData.DocumentDate;
            }

            return View(invoice);
        }

        [OutputCache(Duration = 0)]
        public JsonResult GetSupplierLookup(string consulta)
        {
            var sqlQuery = $"SELECT SupplierNumber [Id], SupplierName [Descripción] " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM00200 WITH (NOLOCK, READUNCOMMITTED) " +
                $"WHERE SupplierNumber LIKE '%{consulta}%' OR SupplierName LIKE '%{consulta}%' " +
                $"ORDER BY SupplierNumber";

            var lookup = _repository.ExecuteQuery<Lookup>(sqlQuery);
            return Json(lookup, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        public JsonResult GetSupplierData(string vendorId)
        {
            string status;
            CustomerData lookup = null;
            try
            {
                var sqlQuery = $"SELECT SupplierNumber CustomerId, SupplierName CustomerName, RTRIM(Address) CustomerAddress, City, Country Contact, '' CurrencyId " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM00200 WITH (NOLOCK, READUNCOMMITTED) " +
                $"WHERE SupplierNumber = '{vendorId}' ";
                lookup = _repository.ExecuteScalarQuery<CustomerData>(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return Json(new { lookup, status }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public JsonResult InformalSupplierInvoiceReport(string id)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "InformalSupplierInvoiceReport.pdf",
                    $"INTRANET.dbo.InformalSupplierMinorExpenseInvoiceReport '{Helpers.InterCompanyId}','{id}'", 48, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        #endregion

        #region Minor Expenses

        public ActionResult MinorExpensesInvoiceIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "MinorExpenseInvoice"))
                return RedirectToAction("NotPermission", "Home");
            var invoices = _repository.ExecuteQuery<AccountingInvoiceHeader>($"SELECT TOP 500 SopNumber DocumentNumber, Ncf, NcfDueDate, NcfType, DocumentDate, SupplierNumber, SupplierName, TaxRegistrationNumber, " +
                $"Address, City, State, Country, PhoneNumber, Subtotal, TaxAmount, Total TotalAmount, Note, CurrencyId, ExchangeRate, Status, ExpenseType " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE DocumentType = 13 ORDER BY SopNumber DESC");
            return View(invoices);
        }

        public ActionResult MinorExpensesInvoice(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "MinorExpenseInvoice"))
                return RedirectToAction("NotPermission", "Home");
            AccountingInvoiceHeader invoice;
            invoice = _repository.ExecuteScalarQuery<AccountingInvoiceHeader>($"SELECT SopNumber DocumentNumber, Ncf, NcfDueDate, NcfType, DocumentDate, SupplierNumber, SupplierName, TaxRegistrationNumber, " +
                $"Address, City, State, Country, PhoneNumber, Subtotal, TaxAmount, Total, Note, CurrencyId, ExchangeRate, Status, ExpenseType " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE SopNumber = '{id}'");

            if (invoice == null)
            {
                var fiscalData = GetNextNcfNumber(13);
                invoice = new AccountingInvoiceHeader
                {
                    DocumentDate = DateTime.Now,
                    DocumentNumber = HelperLogic.AsignaciónSecuencia("Accounting", Account.GetAccount(User.Identity.GetUserName()).UserId),
                    NcfType = fiscalData.IncomeType,
                    CurrencyId = "RDPESO",
                    Ncf = fiscalData.Ncf,
                    NcfDueDate = fiscalData.DocumentDate,
                    Note = "",
                    Subtotal = 0,
                    SupplierNumber = "",
                    SupplierName = "",
                    TaxAmount = 0,
                    TotalAmount = 0,
                    DocumentType = 13,
                    ExpenseType = 0,
                    Details = new List<AccountingInvoiceDetail>()
                };
            }
            else
                invoice.Details = _repository.ExecuteQuery<AccountingInvoiceDetail>($"SELECT SopNumber DocumentNumber, LineNumber, ItemNumber, ItemDescription, Quantity, Price, TaxAmount, Total " +
                    $"FROM {Helpers.InterCompanyId}.dbo.ECPM20200 WHERE SopNumber = '{id}'").ToList();
            ViewBag.Accounts = _repository.ExecuteQuery<Lookup>($"SELECT AccountNumber [Id], AccountDescription [Descripción], '' [DataExtended], '' DataPlus " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECGL00100 WITH (NOLOCK, READUNCOMMITTED) ORDER BY AccountNumber").ToList();
            ViewBag.VoidTypes = _repository.ExecuteQuery<Lookup>($"SELECT RTRIM(VoidType) Id, RTRIM(VoidTypeDescription) Descripción, '' DataExtended, '' DataPlus " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM40201 ORDER BY VoidType").ToList();
            if (invoice.Status == 1)
            {
                var fiscalData = GetNextNcfNumber(13);
                invoice.Ncf = fiscalData.Ncf;
                invoice.NcfDueDate = fiscalData.DocumentDate;
            }
            return View(invoice);
        }

        #endregion

        #region Shared Methods

        [HttpPost]
        public JsonResult SaveInvoice(AccountingInvoiceHeader invoice)
        {
            string xStatus;
            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE SopNumber = '{invoice.DocumentNumber}'");
                var status = 0;
                FiscalSalesTransaction fiscalTrans = null;
                if (count > 0)
                {
                    status = _repository.ExecuteScalarQuery<int>($"SELECT Status FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE SopNumber = '{invoice.DocumentNumber}'");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE SopNumber = '{invoice.DocumentNumber}'");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.ECPM20200 WHERE SopNumber = '{invoice.DocumentNumber}'");
                }
                if (invoice.Status != 2) invoice.Ncf = "";

                var supplier = _repository.ExecuteScalarQuery<Supplier>($"SELECT SupplierNumber, SupplierName, Contact, Address, City, State, Country, TaxRegistrationNumber RNC, PhoneNumber, FaxNumber " +
                    $"FROM {Helpers.InterCompanyId}.dbo.ECPM00200 WHERE SupplierNumber = '{invoice.SupplierNumber}'");

                _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.ECPM20100 (SopNumber, Ncf, NcfDueDate, NcfType, ExpenseType, DocumentDate, DueDate, SupplierNumber, SupplierName, " +
                    $"TaxRegistrationNumber, Address, City, State, Country, PhoneNumber, Subtotal, TaxAmount, Total, Note, CurrencyId, ExchangeRate, DocumentType, Status, CreatedDate, ModifiedDate, LastUserId) " +
                    $"VALUES ('{invoice.DocumentNumber}', '{invoice.Ncf}', '{invoice.NcfDueDate}', '{invoice.NcfType}', '{invoice.ExpenseType}', '{invoice.DocumentDate}', '{invoice.DocumentDate}', '{invoice.SupplierNumber}', " +
                    $"'{(invoice.DocumentType == 13 ? invoice.SupplierName : (supplier?.SupplierName ?? ""))}', '{supplier?.RNC ?? ""}', '{supplier?.Address ?? ""}', '{supplier?.City ?? ""}', '{supplier?.State ?? ""}', '{supplier?.Country ?? ""}', '{supplier?.PhoneNumber ?? ""}','{invoice.Subtotal}', " +
                    $"'{invoice.TaxAmount}', '{invoice.TotalAmount}', '{invoice.Note}', '{invoice.CurrencyId}', 0, '{invoice.DocumentType}', '{invoice.Status}', GETDATE(), GETDATE(), " +
                    $"'{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
                int lineNumber = 1;
                foreach (var item in invoice.Details)
                {
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.ECPM20200 (SopNumber, LineNumber, ItemNumber, ItemDescription, Quantity, Price, TaxAmount, Total) " +
                        $"VALUES ('{invoice.DocumentNumber}', {lineNumber}, '{item.ItemNumber}', '{item.ItemDescription}', '{item.Quantity}', '{item.Price}', '{item.TaxAmount}', '{item.Total}')");
                    lineNumber++;
                }
                if (invoice.Status == 2 && status != 2)
                {
                    string sqlQuery = $"SELECT A.HeaderDocumentNumber Rnc, CONVERT(NVARCHAR(20), A.DocumentNumber) ApplyNcf, HeaderDocumentNumber + REPLICATE('0', 8 - LEN(A.NextNumber)) + CONVERT(NVARCHAR(20), A.NextNumber) Ncf, " +
                    $"A.DueDate DocumentDate, B.DocumentDescription IncomeType " +
                    $"FROM {Helpers.InterCompanyId}.dbo.ECNCF40102 A " +
                    $"INNER JOIN {Helpers.InterCompanyId}.dbo.ECNCF40101 B ON A.HeaderDocumentNumber = B.DocumentNumber WHERE B.DocumentType = {invoice.DocumentType} AND A.NextNumber <= A.ToNumber";
                    fiscalTrans = _repository.ExecuteScalarQuery<FiscalSalesTransaction>(sqlQuery);
                    invoice.Ncf = fiscalTrans.Ncf;
                    invoice.NcfDueDate = fiscalTrans.DocumentDate;
                    invoice.NcfType = fiscalTrans.IncomeType;

                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.ECNCF40102 SET NextNumber += 1, LeftNumber -= 1 " +
                            $"WHERE HeaderDocumentNumber = '{fiscalTrans.Rnc}' AND DocumentNumber = '{fiscalTrans.ApplyNcf}'");
                    SendMailNcfNotification();
                }
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult VoidInvoice(string id, string voidReason = "", string reason = "")
        {
            string xStatus;
            try
            {
                var invoice = _repository.ExecuteScalarQuery<AccountingInvoiceHeader>($"SELECT SopNumber DocumentNumber, Ncf, NcfDueDate, NcfType, DocumentDate, SupplierNumber, SupplierName, TaxRegistrationNumber, " +
                $"Address, City, State, Country, PhoneNumber, Subtotal, TaxAmount, Total TotalAmount, Note, CurrencyId, ExchangeRate, Status, ExpenseType " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE SopNumber = '{id}'");
                if (invoice.Status == 1)
                {
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE SopNumber = '{id}'");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.ECPM20200 WHERE SopNumber = '{id}'");
                }
                else
                {
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.ECPM20100 SET Status = 3 WHERE SopNumber = '{id}'");
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.ECPM20110 (SopNumber, ReasonType, Reason, Ncf, VoidDate, CreatedDate, ModifiedDate, LastUserId) " +
                        $"VALUES ('{id}','{voidReason}','{reason}','{invoice.Ncf}','{DateTime.Now.ToString("yyyyMMdd")}', GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
                }
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        public JsonResult GetVoidReasonInfo(string id)
        {
            string status;
            Lookup lookup = null;
            try
            {
                var sqlQuery = $"SELECT ReasonType Id, Reason Descripción, LastUserId DataExtended " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20110 WITH (NOLOCK, READUNCOMMITTED) " +
                $"WHERE SopNumber = '{id}' ";
                lookup = _repository.ExecuteScalarQuery<Lookup>(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return Json(new { lookup, status }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        public JsonResult UnblockSecuence(string id)
        {
            HelperLogic.DesbloqueoSecuencia(id, "Accounting", Account.GetAccount(User.Identity.GetUserName()).UserId);
            return Json("");
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public JsonResult MinorExpensesInvoiceReport(string id)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "MinorExpensesInvoiceReport.pdf",
                    $"INTRANET.dbo.InformalSupplierMinorExpenseInvoiceReport '{Helpers.InterCompanyId}','{id}'", 49, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [OutputCache(Duration = 0)]
        public JsonResult CheckSequenceNcf(int type)
        {
            string status;
            try
            {
                var fiscalData = GetNextNcfNumber(type);
                if (fiscalData == null)
                    status = "No hay secuencias de comprobantes disponibles para este tipo de documento, por favor vaya a la configuracion de NCF para asignarla.";
                else
                    status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return Json(new { status }, JsonRequestBehavior.AllowGet);
        }

        private FiscalSalesTransaction GetNextNcfNumber(int ncfType)
        {
            string sqlQuery = $"SELECT A.HeaderDocumentNumber Rnc, CONVERT(NVARCHAR(20), A.DocumentNumber) ApplyNcf, HeaderDocumentNumber + REPLICATE('0', 8 - LEN(A.NextNumber)) + CONVERT(NVARCHAR(20), A.NextNumber) Ncf, " +
                $"A.DueDate DocumentDate, B.DocumentDescription IncomeType " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECNCF40102 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.ECNCF40101 B ON A.HeaderDocumentNumber = B.DocumentNumber WHERE B.DocumentType = {ncfType} AND A.NextNumber <= A.ToNumber";

            return _repository.ExecuteScalarQuery<FiscalSalesTransaction>(sqlQuery);
        }

        #endregion

        #region Supplier

        public ActionResult SupplierIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "Supplier"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = $"SELECT SupplierNumber, SupplierName, Email, Contact, Address, City, State, Country, ExpenseType, TaxRegistrationNumber RNC, PhoneNumber, FaxNumber " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM00200";

            var registros = _repository.ExecuteQuery<Supplier>(sqlQuery);
            return View(registros);
        }

        public ActionResult SupplierCreate()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "Supplier"))
                return RedirectToAction("NotPermission", "Home");
            return View(new Supplier { Country = "Republica Dominicana" });
        }

        public ActionResult SupplierEdit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "Supplier"))
                return RedirectToAction("NotPermission", "Home");
            var supplier = _repository.ExecuteScalarQuery<Supplier>($"SELECT SupplierNumber, SupplierName, Email, Contact, Address, City, State, Country, ExpenseType, TaxRegistrationNumber RNC, PhoneNumber, FaxNumber " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM00200 WHERE SupplierNumber = '{id}'");
            return View(supplier);
        }

        [HttpPost]
        public JsonResult SaveSupplier(Supplier supplier)
        {
            string xStatus;
            try
            {
                if (!string.IsNullOrEmpty(supplier.PhoneNumber)) supplier.PhoneNumber = supplier.PhoneNumber.Replace("(", "").Replace(")", "").Replace("-", "").Trim();
                if (!string.IsNullOrEmpty(supplier.FaxNumber)) supplier.FaxNumber = supplier.FaxNumber.Replace("(", "").Replace(")", "").Replace("-", "");
                var service = new ServiceContract();
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.ECPM00200 WHERE SupplierNumber = '{supplier.SupplierNumber}'");
                if (count == 0)
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.ECPM00200 (SupplierNumber, SupplierName, TaxRegistrationNumber, Email, Contact, Address, Country, " +
                        $"State, City, PhoneNumber, FaxNumber, ExpenseType, CreatedDate, ModifiedDate, LastUserId) " +
                    $"VALUES ('{supplier.SupplierNumber}', '{supplier.SupplierName}', '{supplier.RNC}', '{supplier.Email}', '{supplier.Contact}', '{supplier.Address}', '{supplier.Country}', '{supplier.State}', " +
                    $"'{supplier.City}', '{supplier.PhoneNumber}', '{supplier.FaxNumber}', '{supplier.ExpenseType}', GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
                else
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.ECPM00200 SET SupplierName = '{supplier.SupplierName}', TaxRegistrationNumber = '{supplier.RNC}', Email = '{supplier.Email}', " +
                        $"Contact = '{supplier.Contact}', Address = '{supplier.Address}', Country = '{supplier.Country}', State = '{supplier.State}', City = '{supplier.City}', FaxNumber = '{supplier.FaxNumber}', " +
                        $"PhoneNumber = '{supplier.PhoneNumber}', ExpenseType = '{supplier.ExpenseType}', ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                        $"WHERE SupplierNumber = '{supplier.SupplierNumber}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult DeleteSupplier(string id)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.ECPM00200 WHERE SupplierNumber = '{id}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region NCF Configuration

        public ActionResult NCFConfigurationIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "NCFConfiguration"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        public ActionResult GetNCFHeaders()
        {
            var sqlQuery = "SELECT DocumentNumber Code, DocumentDescription Description, DocumentType DocumentType " +
                "FROM " + Helpers.InterCompanyId + ".dbo.ECNCF40101 ORDER BY DocumentNumber";
            var ncfHeaderConfigurations = _repository.ExecuteQuery<NcfHeaderConfiguration>(sqlQuery).ToList();
            return Json(ncfHeaderConfigurations, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetNCFDetails(string code)
        {
            var sqlQuery = $"SELECT HeaderDocumentNumber HeaderCode, DocumentNumber DetailCode, FromNumber, ToNumber, NextNumber, LeftNumber, AlertNumber, DueDate, Status " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECNCF40102 WHERE HeaderDocumentNumber = '{code}' ORDER BY DocumentNumber";
            var products = _repository.ExecuteQuery<NcfDetailConfiguration>(sqlQuery).ToList();
            return Json(products, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveNcfHeaderConfiguration(NcfHeaderConfiguration header)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.ECNCF40101 WHERE DocumentNumber = '" + header.Code + "'");
                if (count == 0)
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.ECNCF40101 (DocumentNumber, DocumentDescription, DocumentType, LastUserId)" +
                        $"VALUES ('{header.Code}', '{header.Description}', '{header.DocumentType}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
                else
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.ECNCF40101 SET DocumentDescription = '{header.Description}', DocumentType = '{header.DocumentType}', " +
                        $"LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' WHERE DocumentNumber = '{header.Code}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult SaveNcfDetailConfiguration(NcfDetailConfiguration header)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.ECNCF40102 " +
                $"WHERE HeaderDocumentNumber = '{header.HeaderCode}' AND DocumentNumber = '" + header.DetailCode + "'");

                if (count == 0)
                {
                    count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.ECNCF40102 " +
                    $"WHERE {header.NextNumber} BETWEEN FromNumber AND ToNumber AND HeaderDocumentNumber = '{header.HeaderCode}' ");
                    if (count == 0)
                    {
                        _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.ECNCF40102 ([HeaderDocumentNumber], [FromNumber], [ToNumber], [NextNumber], [LeftNumber], [AlertNumber], [DueDate], [Status])" +
                        $"VALUES ('{header.HeaderCode}', '{header.FromNumber}', '{header.ToNumber}', '{header.NextNumber}', '{header.LeftNumber}', '{header.AlertNumber}', '{header.DueDate.ToString("yyyyMMdd")}', '{header.Status}')");
                        xStatus = "OK";
                    }
                    else
                        xStatus = "Ya existe una secuencia de NCF con un rango que contiene el mismo rango que ha especificado, por favor corregir";
                }
                else
                {
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.ECNCF40102 SET FromNumber = '{header.FromNumber}', ToNumber = '{header.ToNumber}', " +
                        $"NextNumber = '{header.NextNumber}', DueDate = '{header.DueDate.ToString("yyyyMMdd")}', Status = '{header.Status}', LeftNumber = '{header.LeftNumber}', AlertNumber = '{header.AlertNumber}' " +
                        $"WHERE HeaderDocumentNumber = '{header.HeaderCode}' AND DocumentNumber = '{header.DetailCode}'");
                    xStatus = "OK";
                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult DeleteNcfHeaderConfiguration(string code)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.ECNCF40102 WHERE HeaderDocumentNumber = '{code}'");
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.ECNCF40101 WHERE DocumentNumber = '{code}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteNcfDetailConfiguration(string codeHeader, string codeDetail)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.ECNCF40102 WHERE HeaderDocumentNumber = '{codeHeader}' AND DocumentNumber = '{codeDetail}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Accounting Configuration

        public ActionResult ConfigurationIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "Configuration"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = $"SELECT TaxRegistrationNumber, CompanyName, CompanyAddress, CompanyPhoneNumber, CompanyFaxNumber, SqlEmailProfile FROM {Helpers.InterCompanyId}.dbo.ECPM40101";

            var configuration = _repository.ExecuteScalarQuery<AccountingConfiguration>(sqlQuery) ?? new AccountingConfiguration();
            sqlQuery = $"SELECT PREFIX Id, CONVERT(NVARCHAR(20), SQNCLONG) Descripción, CONVERT(NVARCHAR(20), SQNCNMBR) DataExtended " +
                $"FROM {Helpers.InterCompanyId}.dbo.LPSY40000 WHERE WINRPTID = 'Accounting'";
            var invoiceNumbering = _repository.ExecuteScalarQuery<Lookup>(sqlQuery);
            configuration.InvoiceNextNumber = Convert.ToInt32(invoiceNumbering?.DataExtended ?? "0");
            configuration.InvoiceNumberLength = Convert.ToInt32(invoiceNumbering?.Descripción ?? "0");
            configuration.PrefixInvoiceNumber = invoiceNumbering?.Id ?? "";
            return View(configuration);
        }

        [HttpPost]
        public JsonResult SaveConfiguration(AccountingConfiguration configuration)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.ECPM40101");
                var sqlQuery = "";

                if (count == 0)
                    _repository.ExecuteCommand("INSERT INTO " + Helpers.InterCompanyId + ".dbo.ECPM40101 ([TaxRegistrationNumber],[CompanyName],[CompanyAddress],[CompanyPhoneNumber]," +
                        "[CompanyFaxNumber],[SqlEmailProfile],[CreatedDate],[ModifiedDate],[LastUserId])" +
                        $"VALUES ('{configuration.TaxRegistrationNumber}', '{configuration.CompanyName}', '{configuration.CompanyAddress}', '{configuration.CompanyPhoneNumber}'," +
                        $"'{configuration.CompanyFaxNumber}', '{configuration.SqlEmailProfile}', GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
                else
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.ECPM40101 SET TaxRegistrationNumber = '{configuration.TaxRegistrationNumber}', CompanyName = '{configuration.CompanyName}'," +
                        $"CompanyAddress = '{configuration.CompanyAddress}', CompanyPhoneNumber = '{configuration.CompanyPhoneNumber}', CompanyFaxNumber = '{configuration.CompanyFaxNumber}', " +
                        $"SqlEmailProfile = '{configuration.SqlEmailProfile}', ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}'");

                count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.LPSY40000 WHERE WINRPTID = 'Accounting'");
                if (count == 0)
                {
                    sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.LPSY40000 ([OBJCTYPE],[WINRPTID],[WINRPTDS],[SEQASSIG],[PREFIX],[SQNCLONG],[SQNCNMBR],[CREATDDT],[MODIFDTE],[LSTUSRID])" +
                       $"VALUES (1, 'Accounting','Secuencia de facturas de contabilidad',1, '{configuration.PrefixInvoiceNumber}', '{configuration.InvoiceNumberLength}', " +
                       $"'{configuration.InvoiceNextNumber}', GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')";
                    _repository.ExecuteCommand(sqlQuery);
                }
                else
                {
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.LPSY40000 SET PREFIX = '{configuration.PrefixInvoiceNumber}', SQNCLONG = '{configuration.InvoiceNumberLength}', SQNCNMBR = '{configuration.InvoiceNextNumber}' " +
                        $"WHERE WINRPTID = 'Accounting' ";
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

        public ActionResult BillingConfigurationIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "BillingConfiguration"))
                return RedirectToAction("NotPermission", "Home");
            ViewBag.Customers = _repository.ExecuteQuery<Lookup>($"SELECT RTRIM(CUSTNMBR) Id, RTRIM(CUSTNAME) Descripción, '' DataExtended FROM {Helpers.InterCompanyId}.dbo.RM00101 WHERE INACTIVE = 0").ToList();
            ViewBag.Accounts = _repository.ExecuteQuery<Lookup>($"SELECT B.ACTNUMST Id, RTRIM(A.ACTDESCR) Descripción, CONVERT(NVARCHAR(20), A.ACTINDX) DataExtended " +
                $"FROM {Helpers.InterCompanyId}.dbo.GL00100 A INNER JOIN {Helpers.InterCompanyId}.dbo.GL00105 B ON A.ACTINDX = B.ACTINDX ORDER BY A.ACTINDX").ToList();
            return View();
        }

        #endregion

        #region Tipo de anulaciones

        public ActionResult VoidType()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "VoidType"))
                return RedirectToAction("NotPermission", "Home");
            var list = _repository.ExecuteQuery<Lookup>($"SELECT VoidType Id, VoidTypeDescription Descripción, '' DataExtended, '' DataPlus " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM40201 ORDER BY VoidType").ToList();
            return View(list);
        }

        public JsonResult SaveVoidType(string description)
        {
            string xStatus;

            try
            {
                var voidTypeId = _repository.ExecuteScalarQuery<int?>($"SELECT MAX(CONVERT(INT, VoidType)) FROM {Helpers.InterCompanyId}.dbo.ECPM40201") ?? 0;
                var sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.ECPM40201 (VoidType, VoidTypeDescription, LastUserId, CreatedDate, ModifiedDate) " +
                    $"VALUES ('{(voidTypeId + 1).ToString().PadLeft(2, '0')}', '{description}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}', GETDATE(), GETDATE()) ";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public JsonResult DeleteVoidType(string id)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.ECPM40201 WHERE VoidType = '{id}'";
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

        #region Tipo de impuestos

        public ActionResult Tax()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "Tax"))
                return RedirectToAction("NotPermission", "Home");
            var list = _repository.ExecuteQuery<Lookup>($"SELECT TaxPlanId Id, TaxPlanDescription Descripción, LTRIM(STR(TaxPercent, 25, 2)) DataExtended, '' DataPlus " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM40301 ORDER BY TaxPlanId").ToList();
            return View(list);
        }

        public JsonResult SaveTax(string description, double percent)
        {
            string xStatus;

            try
            {
                var taxTypeId = _repository.ExecuteScalarQuery<int?>($"SELECT MAX(CONVERT(INT, TaxPlanId)) FROM {Helpers.InterCompanyId}.dbo.ECPM40301") ?? 0;
                var sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.ECPM40301 (TaxPlanId, TaxPlanDescription, TaxPercent, LastUserId, CreatedDate, ModifiedDate) " +
                    $"VALUES ('{(taxTypeId + 1).ToString().PadLeft(2, '0')}', '{description}', '{percent}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}', GETDATE(), GETDATE()) ";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public JsonResult DeleteTax(string id)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.ECPM40301 WHERE TaxPlanId = '{id}'";
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

        #region Email de alerta

        public ActionResult EmailAlert()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "EmailAlert"))
                return RedirectToAction("NotPermission", "Home");
            var list = _repository.ExecuteQuery<Lookup>($"SELECT Email Id, Name Descripción, '' DataExtended, '' DataPlus " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM40401 ORDER BY Email").ToList();
            return View(list);
        }

        public JsonResult SaveEmail(string email, string name)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.ECPM40401 (Email, Name, LastUserId, CreatedDate, ModifiedDate) " +
                    $"VALUES ('{email}', '{name}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}', GETDATE(), GETDATE()) ";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public JsonResult DeleteEmail(string id)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.ECPM40401 WHERE Email = '{id}'";
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

        #region Cuentas Contables

        public ActionResult FinancialAccount()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "Account"))
                return RedirectToAction("NotPermission", "Home");
            var list = _repository.ExecuteQuery<Lookup>($"SELECT AccountNumber Id, AccountDescription Descripción, '' DataExtended, '' DataPlus " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECGL00100 ORDER BY AccountNumber").ToList();
            return View(list);
        }

        public JsonResult SaveAccount(string accountNumber, string accountDescription)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.ECGL00100 WHERE AccountNumber = '{accountNumber}'");
                if (count > 0) _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.ECGL00100 WHERE AccountNumber = '{accountNumber}'");
                var sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.ECGL00100 (AccountNumber, AccountDescription, LastUserId, CreatedDate, ModifiedDate) " +
                    $"VALUES ('{accountNumber}', '{accountDescription}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}', GETDATE(), GETDATE()) ";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public JsonResult DeleteAccount(string id)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.ECGL00100 WHERE AccountNumber = '{id}'";
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

        #region Reports

        public ActionResult TransGeneralLedgerReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "SalesTransGeneralLedger"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult TransGeneralLedgerReport(string fromDate, string toDate, string fromCustomer, string toCustomer, string fromBatch, string toBatch, string fromClass, string toClass)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                if (string.IsNullOrEmpty(fromDate))
                    fromDate = new DateTime(1900, 1, 1).ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(toDate))
                    toDate = new DateTime(2999, 12, 31).ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(fromCustomer))
                    fromCustomer = "";
                if (string.IsNullOrEmpty(toCustomer))
                    toCustomer = "";
                if (string.IsNullOrEmpty(fromBatch))
                    fromBatch = "";
                if (string.IsNullOrEmpty(toBatch))
                    toBatch = "";
                if (string.IsNullOrEmpty(fromClass))
                    fromClass = "";
                if (string.IsNullOrEmpty(toClass))
                    toClass = "";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "SalesTransGeneralLedgerReport.pdf",
                    string.Format("INTRANET.dbo.SalesTransGeneralLedgerReport '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}'", Helpers.InterCompanyId, fromDate, toDate, fromCustomer, toCustomer, fromBatch, toBatch, fromClass, toClass),
                    32, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public ActionResult FiscalSalesReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "FiscalSales"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult FiscalSalesReport(string fromDate, string toDate, decimal exchangeRate, int printCurrency)
        {
            string xStatus;
            var list = new List<FiscalSalesTransaction>();
            try
            {
                xStatus = "OK";
                list = _repository.ExecuteQuery<FiscalSalesTransaction>($"INTRANET.dbo.FiscalSalesReport '{Helpers.InterCompanyId}','{fromDate}','{toDate}','{printCurrency}','{exchangeRate}'").ToList();
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = list } };
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult ExportFiscalSalesReport(string fromDate, string toDate, decimal exchangeRate, int printCurrency, string period)
        {
            string xStatus;
            try
            {
                var taxRegisterNumber = _repository.ExecuteScalarQuery<string>($"SELECT TaxRegistrationNumber FROM {Helpers.InterCompanyId}.dbo.ECPM40101");
                var list = _repository.ExecuteQuery<FiscalSalesTransaction>($"INTRANET.dbo.FiscalSalesReport '{Helpers.InterCompanyId}','{fromDate}','{toDate}','{printCurrency}','{exchangeRate}'").ToList();
                OfficeLogic.WriteDataFiscalSalesFile(list, Server.MapPath("~/PDF/Excel/") + "Planilla.xls", Server.MapPath("~/PDF/Excel/") + "607.xls", period.Replace("-", ""), taxRegisterNumber);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public virtual ActionResult Download()
        {
            return File(Server.MapPath("~/PDF/Excel/") + "607.xls", "application/vnd.ms-excel", "607.xls");
        }

        public ActionResult AccountingTransDetailReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "AccountingTransDetailReport"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult AccountingTransDetailReport(int documentType, int documentStatus, string fromDate, string toDate, string fromSupplier, string toSupplier)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                if (string.IsNullOrEmpty(fromDate))
                    fromDate = new DateTime(1900, 1, 1).ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(toDate))
                    toDate = new DateTime(2999, 12, 31).ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(fromSupplier))
                    fromSupplier = "";
                if (string.IsNullOrEmpty(toSupplier))
                    toSupplier = "";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "AccountingTransDetailReport.pdf",
                    string.Format("INTRANET.dbo.AccountingTransDetailReport '{0}','{1}','{2}','{3}','{4}','{5}','{6}'", Helpers.InterCompanyId, documentType, documentStatus, fromDate, toDate, fromSupplier, toSupplier),
                    documentType == 11 ? 50 : 51, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public ActionResult AccountingTransSummaryReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "AccountingTransSummaryReport"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult AccountingTransSummaryReport(int documentType, int documentStatus, string fromDate, string toDate, string fromSupplier, string toSupplier)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                if (string.IsNullOrEmpty(fromDate))
                    fromDate = new DateTime(1900, 1, 1).ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(toDate))
                    toDate = new DateTime(2999, 12, 31).ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(fromSupplier))
                    fromSupplier = "";
                if (string.IsNullOrEmpty(toSupplier))
                    toSupplier = "";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "AccountingTransSummaryReport.pdf",
                    string.Format("INTRANET.dbo.AccountingTransSummaryReport '{0}','{1}','{2}','{3}','{4}','{5}','{6}'", Helpers.InterCompanyId, documentType, documentStatus, fromDate, toDate, fromSupplier, toSupplier),
                    documentType == 11 ? 52 : 53, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        #endregion

        #region Consultas

        public ActionResult AccountingTransactionInquiry()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "AccountTransactionInquiry"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult AccountingTransactionInquiry(int documentType, int documentStatus, string supplierNumber, string ncf, string accountNumber, string dateRange, string currencyId)
        {
            string xStatus;
            var list = new List<AccountingInvoiceHeader>();
            try
            {
                xStatus = "OK";
                string sqlQuery = $"SELECT DISTINCT A.SopNumber DocumentNumber, A.Ncf, A.DocumentDate, A.SupplierNumber, A.SupplierName, A.Subtotal, A.TaxAmount, A.Note, A.Status, " +
                $"CASE A.DocumentType WHEN 11 THEN 'Proveedor Informal' ELSE 'Gastos menores' END PhoneNumber, A.Total TotalAmount, A.CurrencyId, A.ExchangeRate " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.ECPM20200 B ON A.SopNumber = B.SopNumber ";

                var filters = new List<string>();

                if (!string.IsNullOrEmpty(supplierNumber))
                    filters.Add($"A.SupplierNumber = '{supplierNumber}' ");
                if (documentType != 0)
                    filters.Add($"A.DocumentType = '{documentType}' ");
                if (documentStatus != 0)
                    filters.Add($"A.Status = '{documentStatus}' ");
                if (!string.IsNullOrEmpty(ncf))
                    filters.Add($"A.Ncf = '{ncf}' ");
                if (!string.IsNullOrEmpty(dateRange))
                    filters.Add($"A.DocumentDate BETWEEN '{DateTime.ParseExact(dateRange.Split('-')[0].Trim(), "MM/dd/yyyy", null)}' AND '{DateTime.ParseExact(dateRange.Split('-')[1].Trim(), "MM/dd/yyyy", null)}' ");
                if (!string.IsNullOrEmpty(accountNumber))
                    filters.Add($"B.ItemNumber = '{accountNumber}' ");
                if (!string.IsNullOrEmpty(currencyId))
                    filters.Add($"A.CurrencyId = '{currencyId}' ");

                if (filters.Count > 0)
                    sqlQuery += "WHERE " + filters.FirstOrDefault();
                foreach (var item in filters)
                    sqlQuery += " AND " + item;
                list = _repository.ExecuteQuery<AccountingInvoiceHeader>(sqlQuery).ToList();
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = list }, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = int.MaxValue };
        }

        #endregion

        [OutputCache(Duration = 0)]
        public JsonResult LookupData(int tipo = 0, string consulta = "")
        {
            string sqlQuery;
            switch (tipo)
            {
                case 1:
                    sqlQuery = $"SELECT SupplierNumber [Id], SupplierName [Descripción], '' DataExtended, '' DataPlus " +
                        $"FROM {Helpers.InterCompanyId}.dbo.ECPM00200 WITH (NOLOCK, READUNCOMMITTED) " +
                        $"WHERE SupplierNumber LIKE '%{consulta}%' OR SupplierName LIKE '%{consulta}%' " +
                        $"ORDER BY SupplierNumber";
                    var suppliers = _repository.ExecuteQuery<Lookup>(sqlQuery);
                    return Json(suppliers, JsonRequestBehavior.AllowGet);
                case 2:
                    sqlQuery = $"SELECT AccountNumber [Id], AccountDescription [Descripción], '' [DataExtended], '' DataPlus " +
                        $"FROM {Helpers.InterCompanyId}.dbo.ECGL00100 WITH (NOLOCK, READUNCOMMITTED) " +
                        $"WHERE AccountNumber LIKE '%{consulta}%' OR AccountDescription LIKE '%{consulta}%' " +
                        $"ORDER BY AccountNumber";
                    var accounts = _repository.ExecuteQuery<Lookup>(sqlQuery);
                    return Json(accounts, JsonRequestBehavior.AllowGet);
            }
            return Json("");
        }

        private void SendMailNcfNotification()
        {
            try
            {
                var sqlQuery = $"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.ECNCF40101 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.ECNCF40102 B ON A.DocumentNumber = B.HeaderDocumentNumber " +
                $"WHERE (B.ToNumber - B.NextNumber) <= B.AlertNumber AND (B.ToNumber - B.NextNumber) >= 0";
                var count = _repository.ExecuteScalarQuery<int?>(sqlQuery) ?? 0;

                if (count > 0)
                {
                    string emails = "";
                    _repository.ExecuteQuery<string>($"SELECT Email FROM {Helpers.InterCompanyId}.dbo.ECPM40401").ToList().ForEach(p => { emails += p + ";"; });
                    if (!string.IsNullOrEmpty(emails))
                        _repository.ExecuteCommand($"INTRANET.dbo.AccountingSendMailNcfNotification '{Helpers.InterCompanyId}', '{emails}'");
                }
            }
            catch { }
        }
    }
}