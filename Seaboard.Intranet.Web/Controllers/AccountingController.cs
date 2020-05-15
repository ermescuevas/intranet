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
            ViewBag.NcfAlert = "Bienvenidos";
            return View();
        }

        #region Informal Invoice

        public ActionResult InformalSupplierInvoiceIndex()
        {
            var invoices = _repository.ExecuteQuery<AccountingInvoiceHeader>($"SELECT SopNumber DocumentNumber, Ncf, NcfDueDate, NcfType, DocumentDate, SupplierNumber, SupplierName, TaxRegistrationNumber, " +
                $"Address, City, State, Country, PhoneNumber, Subtotal, TaxAmount, Total TotalAmount, Note, CurrencyId, ExchangeRate, Posted " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE DocumentType = 11");
            return View(invoices);
        }

        public ActionResult InformalSupplierInvoice(string id = "")
        {
            AccountingInvoiceHeader invoice;
            invoice = _repository.ExecuteScalarQuery<AccountingInvoiceHeader>($"SELECT SopNumber DocumentNumber, Ncf, NcfDueDate, NcfType, DocumentDate, SupplierNumber, SupplierName, TaxRegistrationNumber, " +
                $"Address, City, State, Country, PhoneNumber, Subtotal, TaxAmount, Total, Note, CurrencyId, ExchangeRate " +
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
                    Details = new List<AccountingInvoiceDetail>()
                };
            }
            else
                invoice.Details = _repository.ExecuteQuery<AccountingInvoiceDetail>($"SELECT SopNumber DocumentNumber, LineNumber, ItemNumber, ItemDescription, Quantity, Price, TaxAmount, Total " +
                    $"FROM {Helpers.InterCompanyId}.dbo.ECPM20200 WHERE SopNumber = '{id}'").ToList();
            ViewBag.Taxes = _repository.ExecuteQuery<Lookup>($"SELECT RTRIM(TXDTLPCT) Id, RTRIM(TXDTLDSC) Descripción, '' DataExtended, '' DataPlus FROM {Helpers.InterCompanyId}.dbo.TX00201 WHERE TXDTLTYP = 2 AND TXDTLBSE = 3").ToList();
            return View(invoice);
        }

        [OutputCache(Duration = 0)]
        public JsonResult GetSupplierLookup(string consulta)
        {
            var configuration = _repository.ExecuteScalarQuery<AccountingConfiguration>($"SELECT VendorClassId, TaxRegistrationNumber FROM {Helpers.InterCompanyId}.dbo.ECPM40101");
            var sqlQuery = $"SELECT VENDORID [Id], VENDNAME [Descripción] " +
                $"FROM {Helpers.InterCompanyId}.dbo.PM00200 WITH (NOLOCK, READUNCOMMITTED) " +
                $"WHERE (VENDORID LIKE '%{consulta}%' OR VENDNAME LIKE '%{consulta}%') AND VNDCLSID = '{configuration.VendorClassId}' ORDER BY VENDORID";

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
                var sqlQuery = $"SELECT VENDORID CustomerId, VENDNAME CustomerName, RTRIM(ADDRESS1) CustomerAddress, RTRIM(CITY) City, RTRIM(COUNTRY) Contact, '' CurrencyId " +
                $"FROM {Helpers.InterCompanyId}.dbo.PM00200 WITH (NOLOCK, READUNCOMMITTED) " +
                $"WHERE VENDORID = '{vendorId}' ";
                lookup = _repository.ExecuteScalarQuery<CustomerData>(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return Json(new { lookup, status }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Minor Expenses

        public ActionResult MinorExpensesInvoiceIndex()
        {
            var invoices = _repository.ExecuteQuery<AccountingInvoiceHeader>($"SELECT SopNumber DocumentNumber, Ncf, NcfDueDate, NcfType, DocumentDate, SupplierNumber, SupplierName, TaxRegistrationNumber, " +
                $"Address, City, State, Country, PhoneNumber, Subtotal, TaxAmount, Total TotalAmount, Note, CurrencyId, ExchangeRate, Posted, ExpenseType " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE DocumentType = 13");
            return View(invoices);
        }

        public ActionResult MinorExpensesInvoice(string id = "")
        {
            AccountingInvoiceHeader invoice;
            invoice = _repository.ExecuteScalarQuery<AccountingInvoiceHeader>($"SELECT SopNumber DocumentNumber, Ncf, NcfDueDate, NcfType, DocumentDate, SupplierNumber, SupplierName, TaxRegistrationNumber, " +
                $"Address, City, State, Country, PhoneNumber, Subtotal, TaxAmount, Total, Note, CurrencyId, ExchangeRate, ExpenseType " +
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
            ViewBag.Accounts = _repository.ExecuteQuery<Lookup>($"SELECT RTRIM(ACTALIAS) [Id], ACTDESCR [Descripción], (RTRIM(LTRIM(ACTNUMBR_1)) + '-' + RTRIM(LTRIM(ACTNUMBR_2)) + '-' + RTRIM(LTRIM(ACTNUMBR_3))) [DataExtended] " +
                $"FROM {Helpers.InterCompanyId}.dbo.GL00100 WITH (NOLOCK, READUNCOMMITTED) WHERE ACTALIAS <> '' ORDER BY [Id] ASC ").ToList();
            return View(invoice);
        }

        #endregion

        [HttpPost]
        public JsonResult SaveInvoice(AccountingInvoiceHeader invoice)
        {
            string xStatus;
            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE SopNumber = '{invoice.DocumentNumber}'");
                FiscalSalesTransaction fiscalTrans = null;
                if (count > 0)
                {
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.ECPM20100 WHERE SopNumber = '{invoice.DocumentNumber}'");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.ECPM20200 WHERE SopNumber = '{invoice.DocumentNumber}'");
                }
                else
                {
                    string sqlQuery = $"SELECT A.HeaderDocumentNumber Rnc, CONVERT(NVARCHAR(20), A.DocumentNumber) ApplyNcf, HeaderDocumentNumber + REPLICATE('0', 8 - LEN(A.NextNumber)) + CONVERT(NVARCHAR(20), A.NextNumber) Ncf, " +
                    $"A.DueDate DocumentDate, B.DocumentDescription IncomeType " +
                    $"FROM {Helpers.InterCompanyId}.dbo.ECNCF40102 A " +
                    $"INNER JOIN {Helpers.InterCompanyId}.dbo.ECNCF40101 B ON A.HeaderDocumentNumber = B.DocumentNumber WHERE B.DocumentType = {invoice.DocumentType} AND A.NextNumber <= A.ToNumber";
                    fiscalTrans = _repository.ExecuteScalarQuery<FiscalSalesTransaction>(sqlQuery);
                    invoice.Ncf = fiscalTrans.Ncf;
                    invoice.NcfDueDate = fiscalTrans.DocumentDate;
                    invoice.NcfType = fiscalTrans.IncomeType;
                }
                var supplier = _repository.ExecuteScalarQuery<Supplier>($"SELECT RTRIM(VENDORID) SupplierId, RTRIM(VENDNAME) SupplierName, RTRIM(PYMTRMID) PaymentCondition, " +
                    $"RTRIM(VNDCNTCT) Contact, RTRIM(VENDSHNM) ShortName, RTRIM(ADDRESS1) Address1, RTRIM(ADDRESS2) Address2, RTRIM(ADDRESS3) Address3, RTRIM(CITY) City, " +
                    $"RTRIM(STATE) State, RTRIM(COUNTRY) Country, RTRIM(TXRGNNUM) RNC, SUBSTRING(PHNUMBR1,1,10) Phone1, SUBSTRING(PHNUMBR2,1,10) Phone2, " +
                    $"SUBSTRING(PHONE3,1,10) Phone3, SUBSTRING(FAXNUMBR,1,10) Fax " +
                    $"FROM {Helpers.InterCompanyId}.dbo.PM00200 WHERE VENDSTTS = 1 AND VENDORID = '{invoice.SupplierNumber}'");

                _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.ECPM20100 (SopNumber, Ncf, NcfDueDate, NcfType, ExpenseType, DocumentDate, DueDate, SupplierNumber, SupplierName, " +
                    $"TaxRegistrationNumber, Address, City, State, Country, PhoneNumber, Subtotal, TaxAmount, Total, Note, CurrencyId, ExchangeRate, DocumentType, Posted, CreatedDate, ModifiedDate, LastUserId) " +
                    $"VALUES ('{invoice.DocumentNumber}', '{invoice.Ncf}', '{invoice.NcfDueDate}', '{invoice.NcfType}', '{invoice.ExpenseType}', '{invoice.DocumentDate}', '{invoice.DocumentDate}', '{invoice.SupplierNumber}', " +
                    $"'{(invoice.DocumentType == 13 ? invoice.SupplierName : (supplier?.SupplierName ?? ""))}', '{supplier?.RNC ?? ""}', '{supplier?.Address1 ?? ""}', '{supplier?.City ?? ""}', '{supplier?.State ?? ""}', '{supplier?.Country ?? ""}', '{supplier?.Phone1 ?? ""}','{invoice.Subtotal}', " +
                    $"'{invoice.TaxAmount}', '{invoice.TotalAmount}', '{invoice.Note}', '{invoice.CurrencyId}', 0, '{invoice.DocumentType}', 0, GETDATE(), GETDATE(), " +
                    $"'{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
                int lineNumber = 1;
                foreach (var item in invoice.Details)
                {
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.ECPM20200 (SopNumber, LineNumber, ItemNumber, ItemDescription, Quantity, Price, TaxAmount, Total) " +
                        $"VALUES ('{invoice.DocumentNumber}', {lineNumber}, '{item.ItemNumber}', '{item.ItemDescription}', '{item.Quantity}', '{item.Price}', '{item.TaxAmount}', '{item.Total}')");
                    lineNumber++;
                }
                if (count > 0)
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.ECNCF40102 SET NextNumber += 1, LeftNumber -= 1 " +
                            $"WHERE HeaderDocumentNumber = '{fiscalTrans.Rnc}' AND DocumentNumber = '{fiscalTrans.ApplyNcf}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        public JsonResult UnblockSecuence(string id)
        {
            HelperLogic.DesbloqueoSecuencia(id, "Accounting", Account.GetAccount(User.Identity.GetUserName()).UserId);
            return Json("");
        }

        #region Supplier

        public ActionResult SupplierIndex()
        {
            //if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "Supplier"))
            //    return RedirectToAction("NotPermission", "Home");

            var configuration = _repository.ExecuteScalarQuery<AccountingConfiguration>($"SELECT VendorClassId, TaxRegistrationNumber FROM {Helpers.InterCompanyId}.dbo.ECPM40101");

            string sqlQuery = $"SELECT RTRIM(VENDORID) SupplierId, RTRIM(VENDNAME) SupplierName, RTRIM(PYMTRMID) PaymentCondition, " +
                $"RTRIM(VNDCNTCT) Contact, RTRIM(VENDSHNM) ShortName, RTRIM(ADDRESS1) Address1, RTRIM(ADDRESS2) Address2, RTRIM(ADDRESS3) Address3, RTRIM(CITY) City, " +
                $"RTRIM(STATE) State, RTRIM(COUNTRY) Country, RTRIM(COMMENT1) NCFType, RTRIM(TXRGNNUM) RNC, SUBSTRING(PHNUMBR1,1,10) Phone1, SUBSTRING(PHNUMBR2,1,10) Phone2, " +
                $"SUBSTRING(PHONE3,1,10) Phone3, SUBSTRING(FAXNUMBR,1,10) Fax " +
                $"FROM {Helpers.InterCompanyId}.dbo.PM00200 WHERE VENDSTTS = 1 AND VNDCLSID = '{configuration.VendorClassId}'";

            var registros = _repository.ExecuteQuery<AccountingConfiguration>(sqlQuery);
            return View(registros);
        }

        public ActionResult SupplierCreate()
        {
            //if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "Supplier"))
            //    return RedirectToAction("NotPermission", "Home");
            var supplier = new Supplier
            {
                Country = "Republica Dominicana"
            };
            return View(supplier);
        }

        public ActionResult SupplierEdit(string id)
        {
            //if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "Supplier"))
            //    return RedirectToAction("NotPermission", "Home");
            var supplier = _repository.ExecuteScalarQuery<Supplier>($"SELECT RTRIM(VENDORID) SupplierId, RTRIM(VENDNAME) SupplierName, RTRIM(PYMTRMID) PaymentCondition, " +
                $"RTRIM(VNDCNTCT) Contact, RTRIM(VENDSHNM) ShortName, RTRIM(ADDRESS1) Address1, RTRIM(ADDRESS2) Address2, RTRIM(ADDRESS3) Address3, RTRIM(CITY) City, " +
                $"RTRIM(STATE) State, RTRIM(COUNTRY) Country, RTRIM(TXRGNNUM) RNC, SUBSTRING(PHNUMBR1,1,10) Phone1, SUBSTRING(PHNUMBR2,1,10) Phone2, " +
                $"SUBSTRING(PHONE3,1,10) Phone3, SUBSTRING(FAXNUMBR,1,10) Fax " +
                $"FROM {Helpers.InterCompanyId}.dbo.PM00200 WHERE VENDSTTS = 1 AND VENDORID = '{id}'");
            return View(supplier);
        }

        [HttpPost]
        public JsonResult SaveSupplier(Supplier supplier)
        {
            string xStatus = "";

            try
            {
                if (!string.IsNullOrEmpty(supplier.Phone1))
                    supplier.Phone1 = supplier.Phone1.Replace("(", "").Replace(")", "").Replace("-", "").Trim();
                if (!string.IsNullOrEmpty(supplier.Phone2))
                    supplier.Phone2 = supplier.Phone2.Replace("(", "").Replace(")", "").Replace("-", "").Trim();
                if (!string.IsNullOrEmpty(supplier.Phone3))
                    supplier.Phone3 = supplier.Phone3.Replace("(", "").Replace(")", "").Replace("-", "").Trim();
                if (!string.IsNullOrEmpty(supplier.Fax))
                    supplier.Fax = supplier.Fax.Replace("(", "").Replace(")", "").Replace("-", "");
                var service = new ServiceContract();
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.PM00200 WHERE VENDORID = '{supplier.SupplierId}'");
                if (count > 0)
                    service.UpdateVendor(supplier, ref xStatus);
                else
                    service.CreateVendor(supplier, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult DeleteSupplier(string customerId)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.PM00200 WHERE VENDORID = '{customerId}'");
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.PM00201 WHERE VENDORID = '{customerId}'");
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.PM00202 WHERE VENDORID = '{customerId}'");
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.PM00203 WHERE VENDORID = '{customerId}'");
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.PM00204 WHERE VENDORID = '{customerId}'");
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
            //if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "NCFConfiguration"))
            //    return RedirectToAction("NotPermission", "Home");
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
            //if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Accounting", "Configuration"))
            //    return RedirectToAction("NotPermission", "Home");
            var sqlQuery = $"SELECT VendorClassId, TaxRegistrationNumber " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM40101";

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
                {
                    sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.ECPM40101 (VendorClassId, TaxRegistrationNumber)" +
                        $"VALUES ('{configuration.VendorClassId}', '{configuration.TaxRegistrationNumber}')";
                    _repository.ExecuteCommand(sqlQuery);
                }
                else
                {
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.ECPM40101 SET VendorClassId = '{configuration.VendorClassId}', TaxRegistrationNumber = '{configuration.TaxRegistrationNumber}'";
                    _repository.ExecuteCommand(sqlQuery);
                }
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

        #endregion

        public ActionResult AccountingTransactionInquiry()
        {
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult AccountingTransactionInquiry(int documentType, string supplierNumber, string ncf, string accountNumber, string dateRange, string currencyId)
        {
            string xStatus;
            var list = new List<AccountingInvoiceHeader>();
            try
            {
                xStatus = "OK";
                string sqlQuery = $"SELECT DISTINCT A.SopNumber DocumentNumber, A.Ncf, A.DocumentDate, A.SupplierNumber, A.SupplierName, A.Subtotal, A.TaxAmount, A.Note, A.Posted, " +
                $"CASE A.DocumentType WHEN 11 THEN 'Proveedor Informal' ELSE 'Gastos menores' END PhoneNumber, A.Total TotalAmount, A.CurrencyId, A.ExchangeRate " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECPM20100 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.ECPM20200 B ON A.SopNumber = B.SopNumber ";

                var filters = new List<string>();
                
                if (!string.IsNullOrEmpty(supplierNumber))
                    filters.Add($"A.SupplierNumber = '{supplierNumber}' ");
                if (documentType != 0)
                    filters.Add($"A.DocumentType = '{documentType}' ");
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

            return new JsonResult { Data = new { status = xStatus, registros = list } };
        }

        private FiscalSalesTransaction GetNextNcfNumber(int ncfType)
        {
            string sqlQuery = $"SELECT A.HeaderDocumentNumber Rnc, CONVERT(NVARCHAR(20), A.DocumentNumber) ApplyNcf, HeaderDocumentNumber + REPLICATE('0', 8 - LEN(A.NextNumber)) + CONVERT(NVARCHAR(20), A.NextNumber) Ncf, " +
                $"A.DueDate DocumentDate, B.DocumentDescription IncomeType " +
                $"FROM {Helpers.InterCompanyId}.dbo.ECNCF40102 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.ECNCF40101 B ON A.HeaderDocumentNumber = B.DocumentNumber WHERE B.DocumentType = {ncfType} AND A.NextNumber <= A.ToNumber";

            return _repository.ExecuteScalarQuery<FiscalSalesTransaction>(sqlQuery);
        }
    }
}