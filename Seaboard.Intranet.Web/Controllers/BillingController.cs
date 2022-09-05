using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Data.Repository;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class BillingController : Controller
    {
        private readonly GenericRepository _repository;

        public BillingController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "Index"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = $"SELECT A.DOCNUMBR Id, A.DOCDESCR Descripción, CONVERT(nvarchar(20), B.TONUMBER - B.NEXTNUMB) DataExtended " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFNCF40101 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.EFNCF40102 B ON A.DOCNUMBR = B.HDDOCNUM " +
                $"WHERE(B.TONUMBER - B.NEXTNUMB) <= B.ALERTNUM AND(B.TONUMBER - B.NEXTNUMB) > 0 ";
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
            return View();
        }

        public ActionResult MeterDashboard()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "Index"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        #region Customers

        public ActionResult CustomerIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "Customer"))
                return RedirectToAction("NotPermission", "Home");

            string sqlQuery = $"SELECT RTRIM(CUSTNMBR) CustomerId, RTRIM(CUSTNAME) CustomerName, RTRIM(CUSTCLAS) ClassId, " +
                $"RTRIM(CNTCPRSN) Contact, RTRIM(SHRTNAME) ShortName, RTRIM(ADDRESS1) Address1, RTRIM(ADDRESS2) Address2, RTRIM(ADDRESS3) Address3, RTRIM(CITY) City, " +
                $"RTRIM(STATE) State, RTRIM(COUNTRY) Country, RTRIM(COMMENT1) NCFType, RTRIM(TXRGNNUM) RNC, SUBSTRING(PHONE1,1,10) Phone1, SUBSTRING(PHONE2,1,10) Phone2, " +
                $"SUBSTRING(PHONE3,1,10) Phone3, SUBSTRING(FAX,1,10) Fax " +
                $"FROM {Helpers.InterCompanyId}.dbo.RM00101 WHERE INACTIVE = 0";

            var registros = _repository.ExecuteQuery<Customer>(sqlQuery);
            return View(registros);
        }
        public ActionResult CustomerCreate()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "Customer"))
                return RedirectToAction("NotPermission", "Home");
            var customer = new Customer
            {
                Country = "Republica Dominicana"
            };
            return View(customer);
        }
        public ActionResult CustomerEdit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "Customer"))
                return RedirectToAction("NotPermission", "Home");
            var customer = _repository.ExecuteScalarQuery<Customer>($"SELECT RTRIM(CUSTNMBR) CustomerId, RTRIM(CUSTNAME) CustomerName, RTRIM(CUSTCLAS) ClassId, " +
                $"RTRIM(CNTCPRSN) Contact, RTRIM(SHRTNAME) ShortName, RTRIM(ADDRESS1) Address1, RTRIM(ADDRESS2) Address2, RTRIM(ADDRESS3) Address3, RTRIM(CITY) City, " +
                $"RTRIM(STATE) State, RTRIM(COUNTRY) Country, RTRIM(COMMENT1) NCFType, RTRIM(TXRGNNUM) RNC, SUBSTRING(PHONE1,1,10) Phone1, SUBSTRING(PHONE2,1,10) Phone2, " +
                $"SUBSTRING(PHONE3,1,10) Phone3, SUBSTRING(FAX,1,10) Fax " +
                $"FROM {Helpers.InterCompanyId}.dbo.RM00101 WHERE CUSTNMBR = '{id}'");
            return View(customer);
        }

        [HttpPost]
        public JsonResult SaveCustomer(Customer customer)
        {
            string xStatus = "";

            try
            {
                if (!string.IsNullOrEmpty(customer.Phone1))
                    customer.Phone1 = customer.Phone1.Replace("(", "").Replace(")", "").Replace("-", "").Trim();
                if (!string.IsNullOrEmpty(customer.Phone2))
                    customer.Phone2 = customer.Phone2.Replace("(", "").Replace(")", "").Replace("-", "").Trim();
                if (!string.IsNullOrEmpty(customer.Phone3))
                    customer.Phone3 = customer.Phone3.Replace("(", "").Replace(")", "").Replace("-", "").Trim();
                if (!string.IsNullOrEmpty(customer.Fax))
                    customer.Fax = customer.Fax.Replace("(", "").Replace(")", "").Replace("-", "");
                var service = new ServiceContract();
                var count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                                ".dbo.RM00101 WHERE CUSTNMBR = '" + customer.CustomerId + "'");
                if (count > 0)
                    service.UpdateCustomer(customer, ref xStatus);
                else
                    service.CreateCustomer(customer, ref xStatus);

                count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                                ".dbo.PM00200 WHERE VENDORID = '" + customer.CustomerId + "'");
                if (count > 0)
                    service.UpdateVendor(customer, ref xStatus);
                else
                    service.CreateVendor(customer, ref xStatus);

                count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                                ".dbo.PA00001 WHERE CUSTNMBR = '" + customer.CustomerId + "'");
                var currency = _repository.ExecuteScalarQuery<string>("SELECT CURNCYID FROM " + Helpers.InterCompanyId +
                                                                ".dbo.RM00101 WHERE CUSTNMBR = '" + customer.CustomerId + "'");
                if (count == 0)
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.PA00001(CUSTNMBR, VENDORID, CURNCYID, RATETPID) " +
                        $"VALUES ('{customer.CustomerId}','{customer.CustomerId}','{currency}','{(currency == "Z-US$" ? "" : "VENDER")}')");
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }
        [HttpPost]
        public JsonResult DeleteCustomer(string customerId)
        {
            string xStatus = "";
            try
            {
                ServiceContract service = new ServiceContract();
                service.DeleteCustomer(customerId, ref xStatus);
                if (xStatus == "OK")
                {
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.PM00200 WHERE VENDORID = '{customerId}'");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.PM00201 WHERE VENDORID = '{customerId}'");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.PM00202 WHERE VENDORID = '{customerId}'");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.PM00203 WHERE VENDORID = '{customerId}'");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.PM00204 WHERE VENDORID = '{customerId}'");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.PA00001 WHERE CUSTNMBR = '{customerId}'");
                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult GetFiscalData(string rnc)
        {
            string xStatus = "";
            string razonSocial = "";
            try
            {
                ServiceContract service = new ServiceContract();
                var contribuyente = service.GetContribuyente(PatronBusqueda.RNC, rnc, ref xStatus);
                if (contribuyente != null)
                    razonSocial = contribuyente.RazonSocial.Trim();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, razonSocial }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Mem Billing

        public ActionResult MemBillingTransactionEntryIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "MemBillingTransaction"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = $"SELECT A.BillingMonth, A.BatchNumber, A.BatchDescription, A.ResolutionNumber, A.Note, SUM(B.Amount) TotalAmount " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFSOP10100 A INNER JOIN {Helpers.InterCompanyId}.dbo.EFSOP10120 B ON A.BatchNumber = B.BatchNumber " +
                $"WHERE A.Posted = 0 AND A.BillingType = 10 " +
                $"GROUP BY A.BillingMonth, A.BatchNumber, A.BatchDescription, A.ResolutionNumber, A.Note";
            var list = _repository.ExecuteQuery<MemBillingHead>(sqlQuery).ToList();
            return View(list);
        }

        public ActionResult MemBillingTransactionEntry(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "MemBillingTransaction"))
                return RedirectToAction("NotPermission", "Home");
            MemBillingHead billingHead;

            billingHead = _repository.ExecuteScalarQuery<MemBillingHead>($"SELECT BillingMonth, BatchNumber, BatchDescription, DocumentDate, DueDate, ResolutionNumber, Note, FilePath " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFSOP10100 WHERE BatchNumber = '{id}' AND BillingType = 10");
            var total = 0m;
            var cantidad = 0;
            if (billingHead == null)
                billingHead = new MemBillingHead
                {
                    BatchNumber = "MEM" + DateTime.Now.AddMonths(-1).ToString("MMyy"),
                    BillingMonth = DateTime.Now.AddMonths(-1),
                    FilePath = "",
                    DocumentDate = DateTime.Now,
                    DueDate = DateTime.Now,
                    Note = $"El total facturado se corresponde con los valores aprobados por el Consejo de Coordinación del OC-SENI en la resolución según " +
                    $"Transacciones Económicas de {Helpers.DateParseDescription(DateTime.Now.AddMonths(-1))} {DateTime.Now.AddMonths(-1).Year}, la cual anexamos."
                };
            else
            {
                total = _repository.ExecuteScalarQuery<decimal>($"SELECT SUM(TotalAmount) FROM {Helpers.InterCompanyId}.dbo.EFSOP10110 WHERE BatchNumber = '{id}' AND BillingType = 10");
                cantidad = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EFSOP10110 WHERE BatchNumber = '{id}' AND BillingType = 10");
            }
            ViewBag.Total = total;
            ViewBag.QuantityTrans = cantidad;

            return View(billingHead);
        }

        [HttpPost]
        public JsonResult GetMemBillingTransactionFile(HttpPostedFileBase fileData, string mes)
        {
            var xStatus = "";
            var xRegistros = new List<MEMData>();
            var xTotal = 0m;
            var xQuantity = 0;
            var lista = new List<MEMData>();
            try
            {
                var lotes = _repository.ExecuteScalarQuery<int>(
                    "SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.EFSOP10100 WHERE MONTH(BillingMonth) = MONTH('" +
                    DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd") + "') AND YEAR(BillingMonth) = YEAR('" +
                    DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd") + "') AND BillingType = '10'");
                if (lotes > 0)
                    xStatus = "Ya se ha procesado el mes indicado de facturación";
                else
                {
                    var filePath = "";
                    if (fileData != null)
                    {
                        filePath = Path.Combine(Server.MapPath("~/Content/File/"), fileData.FileName);
                        if (!Directory.Exists(Server.MapPath("~/Content/File/")))
                            Directory.CreateDirectory(Server.MapPath("~/Content/File/"));
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                        fileData.SaveAs(filePath);
                        lista = OfficeLogic.GetTransactionsFile(filePath, 0, BillingType.MEM, ref xStatus);
                    }
                    else
                        lista = OfficeLogic.MEMBillingProcess(DateTime.ParseExact(mes, "dd/MM/yyyy", null), filePath, ref xStatus);

                    if (lista != null && lista.Count > 0)
                    {
                        _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00110 " +
                            $"WHERE MONTH(DocumentDate) = MONTH('{DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                            $"AND YEAR(DocumentDate) = YEAR('{DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') AND BillingType = 10");

                        _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00120 " +
                            $"WHERE MONTH(DocumentDate) = MONTH('{DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                            $"AND YEAR(DocumentDate) = YEAR('{DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') AND BillingType = 10");
                        foreach (var item in lista)
                        {
                            var customerId = _repository.ExecuteScalarQuery<string>($"SELECT CUSTNMBR FROM {Helpers.InterCompanyId}.dbo.EFRM40201 WHERE CUSTIDEXT = '{item.Debtor}'") ?? "";
                            _repository.ExecuteCommand(string.Format("INTRANET.dbo.InsertBillingMemTempDataHeader '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}'",
                                Helpers.InterCompanyId, DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd"), item.Debtor, customerId,
                                item.TotalAmount.ToString(new CultureInfo("en-US")), 0, item.CurrencyId, Convert.ToInt32(item.BillingType), Account.GetAccount(User.Identity.GetUserName()).UserId));
                            item.CustomerId = customerId;
                            foreach (var detail in item.Details)
                            {
                                _repository.ExecuteCommand(string.Format("INTRANET.dbo.InsertBillingMemTempDataDetail '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}'",
                                Helpers.InterCompanyId, DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd"), item.Debtor, detail.ProductId,
                                detail.ProductName, detail.Amount.ToString(new CultureInfo("en-US")), 0, Convert.ToInt32(item.BillingType)));
                            }
                        }
                        xRegistros = lista;
                        xStatus = "OK";
                    }
                    else
                    {
                        xStatus += "No se encontraron transacciones para este periodo";
                    }

                    xTotal = lista.Sum(x => x.TotalAmount);
                    xQuantity = lista.Count();
                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = xRegistros, total = xTotal, cantidad = xQuantity } };
        }

        public ActionResult GetMemBillingTransactionDetail(string mes, string customerId, int billingType = 10)
        {
            string xStatus;
            List<MemDataDetail> registros = null;
            try
            {
                registros = _repository.ExecuteQuery<MemDataDetail>($"INTRANET.dbo.GetBillingMemTransactionDetail '{Helpers.InterCompanyId}', " +
                    $"'{DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd")}', '{customerId}', {billingType}").ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, registros }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMemBillingTransactionHeader(string mes, int billingType = 10)
        {
            string xStatus;
            List<MEMData> registros = null;
            try
            {
                registros = _repository.ExecuteQuery<MEMData>($"INTRANET.dbo.GetBillingMemTransactionHeader " +
                    $"'{Helpers.InterCompanyId}', '{DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd")}', {billingType}").ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, registros }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveMemBillingTransaction(MemBillingHead billing, string billingMonth, int billingType = 10)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"INTRANET.dbo.InsertBillingMemTransaction '{Helpers.InterCompanyId}','{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}','{billing.BatchNumber}'," +
                    $"'{billing.BatchDescription}','{billing.DocumentDate.ToString("yyyyMMdd")}','{billing.DueDate.ToString("yyyyMMdd")}','{billing.ResolutionNumber}'," +
                    $"'{billing.Note}','{billing.FilePath}', {billingType}, '{billing.ExchangeRate}','{Account.GetAccount(User.Identity.GetUserName()).UserId}','{billing.NoteSecondary}','{billing.RangeCorresponding}'";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult DeleteMemBillingTransaction(string billingMonth, int billingType = 10)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP10100 WHERE MONTH(BillingMonth) = MONTH('" +
                    $"{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') AND YEAR(BillingMonth) = YEAR('" +
                    $"{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd") }') AND BillingType = '{billingType}'");
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP10110 WHERE MONTH(DocumentDate) = MONTH('" +
                    $"{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') AND YEAR(DocumentDate) = YEAR('" +
                    $"{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') AND BillingType = '{billingType}'");
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP10120 WHERE MONTH(DocumentDate) = MONTH('" +
                   $"{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') AND YEAR(DocumentDate) = YEAR('" +
                    $"{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') AND BillingType = '{billingType}'");
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00110 WHERE MONTH(DocumentDate) = MONTH('" +
                    $"{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') AND YEAR(DocumentDate) = YEAR('" +
                    $"{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') AND BillingType = '{billingType}'");
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00120 WHERE MONTH(DocumentDate) = MONTH('" +
                    $"{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') AND YEAR(DocumentDate) = YEAR('" +
                    $"{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') AND BillingType = '{billingType}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateMemBillingTransactionProductExclusion(string billingMonth, string customerId, string productId, bool exclude)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP10120 SET Exclude = '{exclude}' " +
                    $"WHERE MONTH(DocumentDate) = MONTH('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND YEAR(DocumentDate) = YEAR('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND CustomerId = '{customerId}'  AND ItemNumber = '{productId}' AND BillingType = '10'");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP00120 SET Exclude = '{exclude}' " +
                    $"WHERE MONTH(DocumentDate) = MONTH('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND YEAR(DocumentDate) = YEAR('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND CustomerId = '{customerId}' AND ItemNumber = '{productId}' AND BillingType = '10'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateMemBillingTransactionCustomerExclusion(string billingMonth, string customerId, bool exclude)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP10110 SET Exclude = '{exclude}' " +
                    $"WHERE MONTH(DocumentDate) = MONTH('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND YEAR(DocumentDate) = YEAR('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND CustomerExternalId = '{customerId}' AND BillingType = '10'");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP00110 SET Exclude = '{exclude}' " +
                    $"WHERE MONTH(DocumentDate) = MONTH('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND YEAR(DocumentDate) = YEAR('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND CustomerExternalId = '{customerId}' AND BillingType = '10'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult PostMemBillingTransactionEntry(string batchNumber, int billingType = 10)
        {
            string xStatus = "";
            try
            {
                var sqlQuery = $"INTRANET.dbo.PostBillingMemTransaction '{Helpers.InterCompanyId}','{batchNumber}', {billingType}, '{Account.GetAccount(User.Identity.GetUserName()).UserId}'";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult VerifyBatchMemBillingTransaction(string batchNumber)
        {
            string xStatus;
            List<Lookup> registros = null;
            List<string> messages = null;
            try
            {
                registros = _repository.ExecuteQuery<Lookup>($"SELECT A.CustomerExternalId Id, B.CUSTNMBR Descripción " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFSOP10110 A LEFT JOIN {Helpers.InterCompanyId}.dbo.EFRM40201 B ON A.CustomerExternalId = B.CUSTIDEXT " +
                    $"WHERE A.BatchNumber = '{batchNumber}' AND LEN(ISNULL(B.CUSTNMBR, '')) = 0 AND A.Exclude = 0 AND BillingType = 10").ToList();
                if (registros.Count > 0)
                {
                    messages = new List<string>
                    {
                        "Existen registros en esta facturación sin asignación de Id. de cliente de GP los cuales son: ",
                        ""
                    };
                    var count = 1;
                    foreach (var item in registros)
                    {
                        messages.Add(count + ": " + item.Id);
                        count++;
                    }
                    messages.Add("");
                    messages.Add("Debe de ir a la linea indicada y asignar un Id. de cliente de GP para poder continuar con el proceso, Gracias");
                    messages.Add("");
                    messages.Add("Si desea hacer caso omiso de esto puede darle al boton Aceptar pero esto excluira de la facturación a estos clientes, " +
                        "si desea completar la configuración puede darle al boton Cancelar y hacer el proceso");
                    xStatus = "ERROR";
                }
                else
                    xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, messages }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateCustomerId(string billingMonth, string customerId, string customerExternalId)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP10110 SET CustomerId = '{customerId}' " +
                    $"WHERE MONTH(DocumentDate) = MONTH('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND YEAR(DocumentDate) = YEAR('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND CustomerExternalId = '{customerExternalId}' AND BillingType = '10'");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP00110 SET CustomerId = '{customerId}' " +
                    $"WHERE MONTH(DocumentDate) = MONTH('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND YEAR(DocumentDate) = YEAR('{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}') " +
                    $"AND CustomerExternalId = '{customerExternalId}' AND BillingType = '10'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Miscellaneous Invoices

        public ActionResult MiscellaneousInvoiceEntryIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "MiscellaneousInvoice"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = $"SELECT A.BillingMonth, A.BatchNumber, A.BatchDescription, A.ResolutionNumber, A.Note, SUM(B.Amount) TotalAmount " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFSOP10100 A INNER JOIN {Helpers.InterCompanyId}.dbo.EFSOP10120 B ON A.BatchNumber = B.BatchNumber " +
                $"WHERE A.Posted = 0 AND A.BillingType = 20 " +
                $"GROUP BY A.BillingMonth, A.BatchNumber, A.BatchDescription, A.ResolutionNumber, A.Note";
            var list = _repository.ExecuteQuery<MemBillingHead>(sqlQuery).ToList();
            return View(list);
        }

        public ActionResult MiscellaneousInvoiceEntry(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "MiscellaneousInvoice"))
                return RedirectToAction("NotPermission", "Home");
            MemBillingHead billingHead;
            billingHead = _repository.ExecuteScalarQuery<MemBillingHead>($"SELECT BillingMonth, BatchNumber, BatchDescription, DocumentDate, DueDate, " +
                $"ResolutionNumber, Note, FilePath, ExchangeRate FROM {Helpers.InterCompanyId}.dbo.EFSOP10100 WHERE BatchNumber = '{id}' AND BillingType = 20");
            var totalPesos = 0m;
            var totalDolar = 0m;
            var cantidad = 0;
            if (billingHead == null)
                billingHead = new MemBillingHead
                {
                    BatchNumber = "",
                    BillingMonth = DateTime.Now.AddMonths(-1),
                    FilePath = "",
                    DocumentDate = DateTime.Now,
                    DueDate = DateTime.Now,
                    Note = ""
                };
            else
            {
                totalPesos = _repository.ExecuteScalarQuery<decimal?>($"SELECT SUM(TotalAmount) FROM {Helpers.InterCompanyId}.dbo.EFSOP10110 WHERE BatchNumber = '{id}' AND BillingType = 20 AND CurrencyId = 'RDPESO'") ?? 0;
                totalDolar = _repository.ExecuteScalarQuery<decimal?>($"SELECT SUM(TotalAmount) FROM {Helpers.InterCompanyId}.dbo.EFSOP10110 WHERE BatchNumber = '{id}' AND BillingType = 20 AND CurrencyId = 'Z-US$'") ?? 0;
                cantidad = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EFSOP10110 WHERE BatchNumber = '{id}' AND BillingType = 20");
            }
            ViewBag.QuantityTrans = cantidad;
            ViewBag.TotalPesos = totalPesos;
            ViewBag.TotalDolar = totalDolar;

            return View(billingHead);
        }

        [HttpPost]
        public JsonResult DeleteMiscellaneousInvoiceTransaction(string batchNumber, int billingType = 20)
        {
            string xStatus = "";
            try
            {
                DeleteMiscellaneousInvoice(batchNumber, billingType);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetMiscellaneousInvoiceTransactionFile(HttpPostedFileBase fileData, string batchNumber)
        {
            var xStatus = "";
            var xRegistros = new List<MEMData>();
            var xTotalPesos = 0m;
            var xTotalDolar = 0m;
            var xQuantity = 0;
            var lista = new List<MEMData>();
            var listCustomerNotFound = new List<string>();
            var listItemNotFound = new List<string>();
            var listItemLength = new List<string>();
            var listItemAmountZero = new List<string>();
            var listItemCurrencyDifferent = new List<string>();
            var listCustomerWithoutCurrency = new List<string>();
            var currency = "";
            try
            {
                var lotes = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EFSOP10100 WHERE BatchNumber = '{batchNumber}' AND BillingType = '20'");
                if (lotes > 0)
                    xStatus = "Ya se ha procesado un lote de facturación con el mismo id. de lote";
                else
                {
                    var filePath = "";
                    if (fileData != null)
                    {
                        filePath = Path.Combine(Server.MapPath("~/Content/File/"), fileData.FileName);
                        if (!Directory.Exists(Server.MapPath("~/Content/File/")))
                            Directory.CreateDirectory(Server.MapPath("~/Content/File/"));
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                        fileData.SaveAs(filePath);
                        lista = OfficeLogic.GetTransactionsFile(filePath, 0, BillingType.Miscellaneous, ref xStatus);
                    }
                    else
                        xStatus = "No se encontro el archivo especificado por favor verificar si existe o si lo ha especificado";

                    if (lista != null && lista.Count > 0)
                    {
                        _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00110 WHERE BatchNumber = '{batchNumber}' AND BillingType = 20");
                        _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00120 WHERE BatchNumber = '{batchNumber}' AND BillingType = 20");

                        foreach (var item in lista)
                        {
                            if (!string.IsNullOrEmpty(item.Debtor))
                            {
                                currency = "";
                                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.RM00101 WHERE CUSTNMBR = '{item.Debtor}'");
                                if (count == 0)
                                    listCustomerNotFound.Add(item.Debtor);
                                else if (string.IsNullOrEmpty(item.CurrencyId))
                                    listCustomerWithoutCurrency.Add(item.Debtor);
                                else
                                {
                                    _repository.ExecuteCommand(string.Format("INTRANET.dbo.InsertBillingMemTempDataHeader '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}'",
                                        Helpers.InterCompanyId, DateTime.Now.ToString("yyyyMMdd"), item.Debtor, item.Debtor,
                                        item.TotalAmount.ToString(new CultureInfo("en-US")), 0, item.CurrencyId, Convert.ToInt32(item.BillingType),
                                        Account.GetAccount(User.Identity.GetUserName()).UserId, batchNumber, item.Marker, item.Note));
                                    foreach (var detail in item.Details)
                                    {
                                        count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.IV00101 WHERE ITEMNMBR = '{detail.ProductId}'");
                                        if (count == 0)
                                            listItemNotFound.Add(item.Debtor + " - " + detail.ProductId);
                                        else if (detail.ProductName.Trim().Length > 99)
                                            listItemLength.Add(item.Debtor + " - " + detail.ProductId);
                                        else if (detail.Amount == 0)
                                            listItemAmountZero.Add(item.Debtor + " - " + detail.ProductId);
                                        else if (!string.IsNullOrEmpty(currency) && currency != detail.CurrencyId)
                                            listItemCurrencyDifferent.Add(item.Debtor + " - " + detail.ProductId);
                                        else
                                            _repository.ExecuteCommand(string.Format("INTRANET.dbo.InsertBillingMemTempDataDetail '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}'",
                                            Helpers.InterCompanyId, DateTime.Now.ToString("yyyyMMdd"), item.Debtor, detail.ProductId,
                                            detail.ProductName, detail.Amount.ToString(new CultureInfo("en-US")), 0, Convert.ToInt32(item.BillingType), batchNumber, item.Marker, detail.Quantity, detail.LineTotalAmount));

                                        currency = detail.CurrencyId;
                                    }
                                }
                            }
                        }
                        if (listCustomerNotFound.Count > 0 || listItemNotFound.Count > 0 || listItemLength.Count > 0 || listCustomerWithoutCurrency.Count > 0
                            || listItemAmountZero.Count > 0 || listItemCurrencyDifferent.Count > 0)
                        {
                            if (listCustomerNotFound.Count > 0)
                            {
                                xStatus += "Existen lineas en el archivo que tienen id. de cliente que no se encuentran en Dynamics GP estos clientes son: " + Environment.NewLine;
                                foreach (var item in listCustomerNotFound.Distinct())
                                    xStatus += item + Environment.NewLine;
                            }
                            if (listCustomerWithoutCurrency.Count > 0)
                            {
                                xStatus += "Existen lineas en el archivo que tienen documentos sin una moneda asignada estos clientes son: " + Environment.NewLine;
                                foreach (var item in listCustomerWithoutCurrency.Distinct())
                                    xStatus += item + Environment.NewLine;
                            }
                            if (listItemNotFound.Count > 0)
                            {
                                xStatus += "Existen lineas en el archivo que tienen id. de articulo que no se encuentran en Dynamics GP estos articulos son: " + Environment.NewLine;
                                foreach (var item in listItemNotFound)
                                    xStatus += item + Environment.NewLine;
                            }
                            if (listItemLength.Count > 0)
                            {
                                xStatus += "Existen lineas de articulos en el archivo con mas de 99 caracteres debe de modificarlas para poder continuar el maximo de caracteres son 99 estos articulos son: " + Environment.NewLine;
                                foreach (var item in listItemLength)
                                    xStatus += item + Environment.NewLine;
                            }
                            if (listItemAmountZero.Count > 0)
                            {
                                xStatus += "Existen lineas de articulos en el archivo con monto igual a 0 o sin monto debe de modificarlas para poder continuar estos articulos son: " + Environment.NewLine;
                                foreach (var item in listItemAmountZero)
                                    xStatus += item + Environment.NewLine;
                            }
                            if (listItemCurrencyDifferent.Count > 0)
                            {
                                xStatus += "Existen lineas de articulos en el archivo con diferentes monedas en una mismo documento debe de modificarlas para poder continuar estos articulos son: " + Environment.NewLine;
                                foreach (var item in listItemCurrencyDifferent)
                                    xStatus += item + Environment.NewLine;
                            }

                            DeleteMiscellaneousInvoice(batchNumber, 20);

                            xStatus += "Por favor verificar";
                            xRegistros = new List<MEMData>();
                        }
                        else
                        {
                            xRegistros = lista;
                            xStatus = "OK";
                            xTotalPesos = lista.Where(x => x.CurrencyId == "RDPESO").Sum(x => x.TotalAmount);
                            xTotalDolar = lista.Where(x => x.CurrencyId == "Z-US$").Sum(x => x.TotalAmount);
                            xQuantity = lista.Count();
                        }
                    }
                    else
                        if (string.IsNullOrEmpty(xStatus))
                        xStatus += "No se encontraron transacciones para este periodo";


                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = xRegistros, totalPesos = xTotalPesos, totalDolar = xTotalDolar, cantidad = xQuantity } };
        }

        public ActionResult GetMiscellaneousInvoiceTransactionDetail(string batchNumber, string customerId, int flag, int billingType = 20)
        {
            string xStatus;
            List<MemDataDetail> registros = null;
            try
            {
                registros = _repository.ExecuteQuery<MemDataDetail>($"INTRANET.dbo.GetBillingMemTransactionDetail '{Helpers.InterCompanyId}','','{customerId}', {billingType}, '{batchNumber}', {flag}").ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, registros }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMiscellaneousInvoiceTransactionHeader(string batchNumber, int billingType = 20)
        {
            string xStatus;
            List<MEMData> registros = null;
            try
            {
                registros = _repository.ExecuteQuery<MEMData>($"INTRANET.dbo.GetBillingMemTransactionHeader '{Helpers.InterCompanyId}', '', {billingType}, '{batchNumber}'").ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, registros }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateMiscellaneousInvoiceTransactionProductExclusion(string batchNumber, string customerId, string productId, bool exclude)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP10120 SET Exclude = '{exclude}' " +
                    $"WHERE BatchNumber = '{batchNumber}' AND BillingType = '20'");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP00120 SET Exclude = '{exclude}' " +
                    $"WHERE BatchNumber = '{batchNumber}' AND BillingType = '20'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateMiscellaneousInvoiceTransactionCustomerExclusion(string batchNumber, string customerId, bool exclude)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP10110 SET Exclude = '{exclude}' " +
                    $"WHERE BatchNumber = '{batchNumber}' AND CustomerExternalId = '{customerId}' AND BillingType = '20'");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP00110 SET Exclude = '{exclude}' " +
                    $"WHERE BatchNumber = '{batchNumber}' AND CustomerExternalId = '{customerId}' AND BillingType = '20'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        private void DeleteMiscellaneousInvoice(string batchNumber, int billingType = 20)
        {
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP10100 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP10110 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP10120 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00110 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00120 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
        }

        #endregion

        #region Reliquidation

        public ActionResult ReliquidationEntryIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "Reliquidation"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = $"SELECT A.BillingMonth, A.BatchNumber, A.BatchDescription, A.ResolutionNumber, A.Note, SUM(B.Amount) TotalAmount " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFSOP10100 A INNER JOIN {Helpers.InterCompanyId}.dbo.EFSOP10120 B ON A.BatchNumber = B.BatchNumber " +
                $"WHERE A.Posted = 0 AND A.BillingType = 30 " +
                $"GROUP BY A.BillingMonth, A.BatchNumber, A.BatchDescription, A.ResolutionNumber, A.Note";
            var list = _repository.ExecuteQuery<MemBillingHead>(sqlQuery).ToList();
            return View(list);
        }

        public ActionResult ReliquidationEntry(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "Reliquidation"))
                return RedirectToAction("NotPermission", "Home");

            MemBillingHead billingHead;

            billingHead = _repository.ExecuteScalarQuery<MemBillingHead>($"SELECT BillingMonth, BatchNumber, BatchDescription, DocumentDate, DueDate,  " +
                $"ResolutionNumber, Note, FilePath, ExchangeRate, NoteSecondary FROM {Helpers.InterCompanyId}.dbo.EFSOP10100 WHERE BatchNumber = '{id}' AND BillingType = 30");
            var totalPesos = 0m;
            var totalDolar = 0m;
            var configuration = _repository.ExecuteScalarQuery<ConfigurationModel>($"SELECT FILEPATH FilePath, SHEET Sheet, RWBEGDAT RowBeginningData, RWNAMCOL RowNameColumn, CREDEXCCOL CréditorExcelColumn, " +
                $"DEBTEXCCOL DebtorExcelColumn, SUMRWBEGDAT SummaryRowBeginningData, SUMTOTCOL SummaryTotalColumn, SQLEMAPRF SqlEmailProfile, DEFNOTNC DefaultNoteNC, DEFNOTND DefaultNoteND " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFRM40101");
            if (billingHead == null)
            {
                billingHead = new MemBillingHead
                {
                    BatchNumber = "",
                    BillingMonth = DateTime.Now.AddMonths(-1),
                    FilePath = "",
                    DocumentDate = DateTime.Now,
                    DueDate = DateTime.Now,
                    Note = configuration.DefaultNoteNC,
                    NoteSecondary = configuration.DefaultNoteND,
                };
            }
            else
            {
                totalPesos = _repository.ExecuteScalarQuery<decimal?>($"SELECT SUM(TotalAmount) FROM {Helpers.InterCompanyId}.dbo.EFSOP10110 WHERE BatchNumber = '{id}' AND BillingType = 30 AND CurrencyId = 'RDPESO'") ?? 0;
                totalDolar = _repository.ExecuteScalarQuery<decimal?>($"SELECT SUM(TotalAmount) FROM {Helpers.InterCompanyId}.dbo.EFSOP10110 WHERE BatchNumber = '{id}' AND BillingType = 30 AND CurrencyId = 'Z-US$'") ?? 0;
            }
            ViewBag.TotalPesos = totalPesos;
            ViewBag.TotalDolar = totalDolar;
            ViewBag.NoteNC = configuration.DefaultNoteNC;
            ViewBag.NoteND = configuration.DefaultNoteND;

            return View(billingHead);
        }

        [HttpPost]
        public JsonResult GetReliquidationTransactionFile(HttpPostedFileBase fileData, string batchNumber)
        {
            var xStatus = "";
            var xRegistros = new List<MEMData>();
            var xTotalPesos = 0m;
            var xTotalDolar = 0m;
            var lista = new List<MEMData>();
            var listCustomerNotFound = new List<string>();
            var listInvoiceNotFound = new List<string>();
            var listItemNotFound = new List<string>();
            var listItemLength = new List<string>();
            var listItemAmountZero = new List<string>();
            var listItemCurrencyDifferent = new List<string>();
            var listCustomerWithoutCurrency = new List<string>();
            var currency = "";
            try
            {
                var lotes = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EFSOP10100 WHERE BatchNumber = '{batchNumber}' AND BillingType = '30'");
                if (lotes > 0)
                    xStatus = "Ya se ha procesado un lote de de reliquidacion con el mismo id. de lote";
                else
                {
                    var filePath = "";
                    if (fileData != null)
                    {
                        filePath = Path.Combine(Server.MapPath("~/Content/File/"), fileData.FileName);
                        if (!Directory.Exists(Server.MapPath("~/Content/File/")))
                            Directory.CreateDirectory(Server.MapPath("~/Content/File/"));
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                        fileData.SaveAs(filePath);
                        lista = OfficeLogic.GetTransactionsFile(filePath, BillingType.Reliquidation, ref xStatus);
                    }
                    else
                        xStatus = "No se encontro el archivo especificado por favor verificar si existe o si lo ha especificado";

                    if (lista != null && lista.Count > 0)
                    {
                        _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00110 WHERE BatchNumber = '{batchNumber}' AND BillingType = 30");
                        _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00120 WHERE BatchNumber = '{batchNumber}' AND BillingType = 30");

                        foreach (var item in lista)
                        {
                            if (!string.IsNullOrEmpty(item.Debtor))
                            {
                                currency = "";
                                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.RM00101 WHERE CUSTNMBR = '{item.Debtor}'");
                                if (count == 0)
                                    listCustomerNotFound.Add(item.Debtor);
                                else if (string.IsNullOrEmpty(item.CurrencyId))
                                    listCustomerWithoutCurrency.Add(item.Debtor);
                                else
                                {
                                    _repository.ExecuteCommand(string.Format("INTRANET.dbo.InsertBillingMemTempDataHeader '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}'",
                                        Helpers.InterCompanyId, DateTime.Now.ToString("yyyyMMdd"), item.Debtor, item.Debtor,
                                        item.TotalAmount.ToString(new CultureInfo("en-US")), 0, item.CurrencyId, Convert.ToInt32(item.BillingType),
                                        Account.GetAccount(User.Identity.GetUserName()).UserId, batchNumber, item.Marker, item.Note, item.DocumentType));
                                    foreach (var detail in item.Details)
                                    {
                                        count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.IV00101 WHERE ITEMNMBR = '{detail.ProductId}'");
                                        if (count == 0)
                                            listItemNotFound.Add(item.Debtor + " - " + detail.ProductId);
                                        else if (detail.ProductName.Trim().Length > 99)
                                            listItemLength.Add(item.Debtor + " - " + detail.ProductId);
                                        else if (detail.Amount == 0)
                                            listItemAmountZero.Add(item.Debtor + " - " + detail.ProductId);
                                        else if (!string.IsNullOrEmpty(currency) && currency != detail.CurrencyId)
                                            listItemCurrencyDifferent.Add(item.Debtor + " - " + detail.ProductId);
                                        else
                                        {
                                            _repository.ExecuteCommand(string.Format("INTRANET.dbo.InsertBillingMemTempDataDetail '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'",
                                            Helpers.InterCompanyId, DateTime.Now.ToString("yyyyMMdd"), item.Debtor, detail.ProductId,
                                            detail.ProductName, detail.Amount.ToString(new CultureInfo("en-US")), 0, Convert.ToInt32(item.BillingType), batchNumber, item.Marker));
                                            if (!string.IsNullOrEmpty(detail.ReferenceInvoice))
                                                _repository.ExecuteCommand(string.Format("INTRANET.dbo.InsertBillingMemTempDataInvoice '{0}','{1}','{2}','{3}','{4}','{5}'",
                                                Helpers.InterCompanyId, batchNumber, item.Debtor, detail.ReferenceInvoice, Convert.ToInt32(item.BillingType), item.Marker));
                                        }
                                        currency = detail.CurrencyId;
                                    }
                                }
                            }
                        }
                        if (listCustomerNotFound.Count > 0 || listItemNotFound.Count > 0 || listItemLength.Count > 0 || listCustomerWithoutCurrency.Count > 0
                            || listItemAmountZero.Count > 0 || listItemCurrencyDifferent.Count > 0 || listInvoiceNotFound.Count > 0)
                        {
                            if (listCustomerNotFound.Count > 0)
                            {
                                xStatus += "Existen lineas en el archivo que tienen id. de cliente que no se encuentran en Dynamics GP estos clientes son: " + Environment.NewLine;
                                foreach (var item in listCustomerNotFound.Distinct())
                                    xStatus += item + Environment.NewLine;
                            }
                            if (listCustomerWithoutCurrency.Count > 0)
                            {
                                xStatus += "Existen lineas en el archivo que tienen documentos sin una moneda asignada estos clientes son: " + Environment.NewLine;
                                foreach (var item in listCustomerWithoutCurrency.Distinct())
                                    xStatus += item + Environment.NewLine;
                            }
                            if (listItemNotFound.Count > 0)
                            {
                                xStatus += "Existen lineas en el archivo que tienen id. de articulo que no se encuentran en Dynamics GP estos articulos son: " + Environment.NewLine;
                                foreach (var item in listItemNotFound)
                                    xStatus += item + Environment.NewLine;
                            }
                            if (listItemLength.Count > 0)
                            {
                                xStatus += "Existen lineas de articulos en el archivo con mas de 99 caracteres debe de modificarlas para poder continuar el maximo de caracteres son 99 estos articulos son: " + Environment.NewLine;
                                foreach (var item in listItemLength)
                                    xStatus += item + Environment.NewLine;
                            }
                            if (listItemAmountZero.Count > 0)
                            {
                                xStatus += "Existen lineas de articulos en el archivo con monto igual a 0 o sin monto debe de modificarlas para poder continuar estos articulos son: " + Environment.NewLine;
                                foreach (var item in listItemAmountZero)
                                    xStatus += item + Environment.NewLine;
                            }
                            if (listItemCurrencyDifferent.Count > 0)
                            {
                                xStatus += "Existen lineas de articulos en el archivo con diferentes monedas en una mismo documento debe de modificarlas para poder continuar estos articulos son: " + Environment.NewLine;
                                foreach (var item in listItemCurrencyDifferent)
                                    xStatus += item + Environment.NewLine;
                            }
                            if (listInvoiceNotFound.Count > 0)
                            {
                                xStatus += "Existen lineas en el archivo que tienen numero de factura que no se encuentran en Dynamics GP estos clientes son: " + Environment.NewLine;
                                foreach (var item in listItemCurrencyDifferent)
                                    xStatus += item + Environment.NewLine;
                            }

                            DeleteMiscellaneousInvoice(batchNumber, 20);

                            xStatus += "Por favor verificar";
                            xRegistros = new List<MEMData>();
                        }
                        else
                        {
                            xRegistros = lista;
                            xStatus = "OK";
                            xTotalPesos = lista.Where(x => x.CurrencyId == "RDPESO").Sum(x => x.TotalAmount);
                            xTotalDolar = lista.Where(x => x.CurrencyId == "Z-US$").Sum(x => x.TotalAmount);
                        }
                    }
                    else
                        if (string.IsNullOrEmpty(xStatus))
                        xStatus += "No se encontraron transacciones para este periodo";
                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = xRegistros, totalPesos = xTotalPesos, totalDolar = xTotalDolar } };
        }

        public ActionResult GetReliquidationTransactionDetail(string batchNumber, string customerId, int flag, int billingType = 30)
        {
            string xStatus;
            List<MemDataDetail> registros = null;
            try
            {
                registros = _repository.ExecuteQuery<MemDataDetail>($"INTRANET.dbo.GetBillingMemTransactionDetail '{Helpers.InterCompanyId}','','{customerId}', {billingType}, '{batchNumber}', {flag}").ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, registros }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetReliquidationTransactionHeader(string batchNumber, int billingType = 30)
        {
            string xStatus;
            List<MEMData> registros = null;
            try
            {
                registros = _repository.ExecuteQuery<MEMData>($"INTRANET.dbo.GetBillingMemTransactionHeader '{Helpers.InterCompanyId}', '', {billingType}, '{batchNumber}'").ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, registros }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetReliquidationTransactionInvoice(string batchNumber, string customerId, int flag, int billingType = 30)
        {
            string xStatus;
            List<MemNetTransDetail> registros = null;
            try
            {
                registros = _repository.ExecuteQuery<MemNetTransDetail>($"INTRANET.dbo.GetBillingMemTransactionInvoice '{Helpers.InterCompanyId}','{customerId}', {billingType}, '{batchNumber}', {flag}").ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, registros }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateReliquidationTransactionProductExclusion(string batchNumber, string customerId, string productId, bool exclude)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP10120 SET Exclude = '{exclude}' " +
                    $"WHERE BatchNumber = '{batchNumber}' AND BillingType = '30'");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP00120 SET Exclude = '{exclude}' " +
                    $"WHERE BatchNumber = '{batchNumber}' AND BillingType = '30'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateReliquidationTransactionCustomerExclusion(string batchNumber, string customerId, bool exclude)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP10110 SET Exclude = '{exclude}' " +
                    $"WHERE BatchNumber = '{batchNumber}' AND CustomerExternalId = '{customerId}' AND BillingType = '30'");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP00110 SET Exclude = '{exclude}' " +
                    $"WHERE BatchNumber = '{batchNumber}' AND CustomerExternalId = '{customerId}' AND BillingType = '30'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteReliquidationTransaction(string batchNumber, int billingType = 30)
        {
            string xStatus = "";
            try
            {
                DeleteReliquidation(batchNumber, billingType);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        private void DeleteReliquidation(string batchNumber, int billingType = 30)
        {
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP10100 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP10110 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP10120 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00110 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00120 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00130 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
            _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP10130 WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
        }

        public JsonResult GetReliquidationDocumentsForApply(string customerId, string batchNumber, string flag, string currencyId, decimal amount)
        {
            string xStatus;
            var xRegistros = new List<MemNetTransDetail>();
            try
            {
                var startDateInvoice = _repository.ExecuteScalarQuery<DateTime>($"SELECT SRTDATINV FROM {Helpers.InterCompanyId}.dbo.EFRM40101");
                string sqlQuery;
                if (currencyId != "RDPESO")
                {
                    sqlQuery = $"SELECT DISTINCT RTRIM(A.DOCNUMBR) DocumentNumber, CONVERT(NUMERIC(32,2), (A.ORORGTRX - ISNULL(C.DocumentApply, 0)) - ISNULL(D.ApplyAmount, 0)) CurrentAmount, " +
                       $"CONVERT(nvarchar(20), CONVERT(DATE, A.DOCDATE, 112)) DocumentDate, CONVERT(nvarchar(20), CONVERT(DATE, B.DUEDATE, 112)) DueDate, " +
                       $"CONVERT(NUMERIC(32,2), ISNULL(D.ApplyAmount, 0)) AppliedAmount, ISNULL(E.USRDEF05, '') Ncf, RTRIM(A.CURNCYID) CurrencyId,1DocumentType, " +
                       $"CASE (SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.Seabo_SOP_Line_Items AA WHERE AA.SOP_Number = A.DOCNUMBR AND F.Item_Number IN ('INTERESES')) WHEN 0 THEN 'ENERGIA' ELSE 'INTERESES' END Module " +
                       $"FROM {Helpers.InterCompanyId}.dbo.MC020102 A " +
                       $"INNER JOIN {Helpers.InterCompanyId}.dbo.SOP30200 B ON A.DOCNUMBR = B.SOPNUMBE " +
                       $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EFRM30200 C ON A.DOCNUMBR = C.SopNumber " +
                       $"LEFT JOIN ( " +
                       $"SELECT CustomerId, SopNumber, ApplyAmount, BatchNumber, BillingType, Flag FROM {Helpers.InterCompanyId}.dbo.EFSOP00130 " +
                       $"WHERE BatchNumber = '{batchNumber}' AND CustomerId = '{customerId}' AND Flag = '{flag}' " +
                       $"UNION ALL " +
                       $"SELECT CustomerId, SopNumber, ApplyAmount, BatchNumber, BillingType, Flag FROM {Helpers.InterCompanyId}.dbo.EFSOP10130 " +
                       $"WHERE BatchNumber = '{batchNumber}' AND CustomerId = '{customerId}' AND Flag = '{flag}' " +
                       $") AS D ON A.DOCNUMBR = D.SopNumber " +
                       $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SOP10106 E ON A.DOCNUMBR = E.SOPNUMBE " +
                       $"INNER JOIN {Helpers.InterCompanyId}.dbo.Seabo_SOP_Line_Items F ON A.DOCNUMBR = F.SOP_Number " +
                       $"WHERE A.CUSTNMBR = '{customerId}' AND RTRIM(A.CURNCYID) = '{currencyId}' AND (A.ORORGTRX - ISNULL(C.DocumentApply, 0)) > 0 AND A.RMDTYPAL = 1 AND SUBSTRING(A.DOCNUMBR, 1, 3) = 'FAC' " +
                       $"AND (A.ORORGTRX - ISNULL(C.DocumentApply, 0)) - ISNULL(D.ApplyAmount, 0) >= {amount} AND CONVERT(DATE, A.DOCDATE) >= CONVERT(DATE, '{startDateInvoice.ToString("yyyyMMdd")}') " +
                       $"/*AND F.Item_Number NOT IN ('INTERESES') */ ";

                    sqlQuery += "UNION ALL ";

                    sqlQuery += $"SELECT DISTINCT RTRIM(A.DOCNUMBR) DocumentNumber, CONVERT(NUMERIC(32,2), (A.ORORGTRX - ISNULL(C.DocumentApply, 0)) - ISNULL(D.ApplyAmount, 0)) CurrentAmount, " +
                        $"CONVERT(nvarchar(20), CONVERT(DATE, A.DOCDATE, 112)) DocumentDate, CONVERT(nvarchar(20), CONVERT(DATE, B.DUEDATE, 112)) DueDate, " +
                        $"CONVERT(NUMERIC(32,2), ISNULL(D.ApplyAmount, 0)) AppliedAmount, ISNULL(E.USRDEF05, '') Ncf, RTRIM(A.CURNCYID) CurrencyId, 0 DocumentType, " +
                        $"CASE (SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.Seabo_SOP_Line_Items AA WHERE AA.SOP_Number = A.DOCNUMBR AND F.Item_Number IN ('INTERESES')) WHEN 0 THEN 'ENERGIA' ELSE 'INTERESES' END Module " +
                        $"FROM {Helpers.InterCompanyId}.dbo.MC020102 A " +
                        $"INNER JOIN {Helpers.InterCompanyId}.dbo.SOP30200 B ON A.DOCNUMBR = B.SOPNUMBE " +
                        $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EFRM30200 C ON A.DOCNUMBR = C.SopNumber " +
                        $"LEFT JOIN ( " +
                        $"SELECT CustomerId, SopNumber, ApplyAmount, BatchNumber, BillingType, Flag FROM {Helpers.InterCompanyId}.dbo.EFSOP00130 " +
                        $"WHERE BatchNumber = '{batchNumber}' AND CustomerId = '{customerId}' AND Flag = '{flag}' " +
                        $"UNION ALL " +
                        $"SELECT CustomerId, SopNumber, ApplyAmount, BatchNumber, BillingType, Flag FROM {Helpers.InterCompanyId}.dbo.EFSOP10130 " +
                        $"WHERE BatchNumber = '{batchNumber}' AND CustomerId = '{customerId}' AND Flag = '{flag}' " +
                        $") AS D ON A.DOCNUMBR = D.SopNumber " +
                        $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SOP10106 E ON A.DOCNUMBR = E.SOPNUMBE " +
                        $"INNER JOIN {Helpers.InterCompanyId}.dbo.Seabo_SOP_Line_Items F ON A.DOCNUMBR = F.SOP_Number " +
                        $"WHERE A.CUSTNMBR = '{customerId}' AND RTRIM(A.CURNCYID) = '{currencyId}' AND (A.ORORGTRX - ISNULL(C.DocumentApply, 0)) > 0 AND A.RMDTYPAL = 1 AND SUBSTRING(A.DOCNUMBR, 1, 3) = 'FAC' " +
                        $"AND (A.ORORGTRX - ISNULL(C.DocumentApply, 0)) - ISNULL(D.ApplyAmount, 0) < {amount} AND CONVERT(DATE, A.DOCDATE) >= CONVERT(DATE, '{startDateInvoice.ToString("yyyyMMdd")}') " +
                        $"/*AND F.Item_Number NOT IN ('INTERESES') */ ";
                }
                else
                {
                    sqlQuery = $"SELECT DISTINCT RTRIM(A.DOCNUMBR) DocumentNumber, CONVERT(NUMERIC(32,2), (A.ORTRXAMT - ISNULL(C.DocumentApply, 0)) - ISNULL(D.ApplyAmount, 0)) CurrentAmount, " +
                       $"CONVERT(nvarchar(20), CONVERT(DATE, A.DOCDATE, 112)) DocumentDate, CONVERT(nvarchar(20), CONVERT(DATE, B.DUEDATE, 112)) DueDate, " +
                       $"CONVERT(NUMERIC(32,2), ISNULL(D.ApplyAmount, 0)) AppliedAmount, ISNULL(E.USRDEF05, '') Ncf, RTRIM(A.CURNCYID) CurrencyId, 1 DocumentType, " +
                       $"CASE (SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.Seabo_SOP_Line_Items AA WHERE AA.SOP_Number = A.DOCNUMBR AND F.Item_Number IN ('INTERESES')) WHEN 0 THEN 'ENERGIA' ELSE 'INTERESES' END Module " +
                       $"FROM {Helpers.InterCompanyId}.dbo.RM20101 A " +
                       $"INNER JOIN {Helpers.InterCompanyId}.dbo.SOP30200 B ON A.DOCNUMBR = B.SOPNUMBE " +
                       $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EFRM30200 C ON A.DOCNUMBR = C.SopNumber " +
                       $"LEFT JOIN ( " +
                       $"SELECT CustomerId, SopNumber, ApplyAmount, BatchNumber, BillingType, Flag FROM {Helpers.InterCompanyId}.dbo.EFSOP00130 " +
                       $"WHERE BatchNumber = '{batchNumber}' AND CustomerId = '{customerId}' AND Flag = '{flag}' " +
                       $"UNION ALL " +
                       $"SELECT CustomerId, SopNumber, ApplyAmount, BatchNumber, BillingType, Flag FROM {Helpers.InterCompanyId}.dbo.EFSOP10130 " +
                       $"WHERE BatchNumber = '{batchNumber}' AND CustomerId = '{customerId}' AND Flag = '{flag}' " +
                       $") AS D ON A.DOCNUMBR = D.SopNumber " +
                       $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SOP10106 E ON A.DOCNUMBR = E.SOPNUMBE " +
                       $"INNER JOIN {Helpers.InterCompanyId}.dbo.Seabo_SOP_Line_Items F ON A.DOCNUMBR = F.SOP_Number " +
                       $"WHERE A.CUSTNMBR = '{customerId}' AND RTRIM(A.CURNCYID) = '{currencyId}' AND (A.ORTRXAMT - ISNULL(C.DocumentApply, 0)) > 0 AND A.RMDTYPAL = 1 AND SUBSTRING(A.DOCNUMBR, 1, 3) = 'FAC' " +
                       $"AND (A.ORTRXAMT - ISNULL(C.DocumentApply, 0)) - ISNULL(D.ApplyAmount, 0) >= {amount} AND CONVERT(DATE, A.DOCDATE) >= CONVERT(DATE, '{startDateInvoice.ToString("yyyyMMdd")}') " +
                       $"/*AND F.Item_Number NOT IN ('INTERESES') */ ";

                    sqlQuery += "UNION ALL ";

                    sqlQuery += $"SELECT DISTINCT RTRIM(A.DOCNUMBR) DocumentNumber, CONVERT(NUMERIC(32,2), (A.ORTRXAMT - ISNULL(C.DocumentApply, 0)) - ISNULL(D.ApplyAmount, 0)) CurrentAmount, " +
                       $"CONVERT(nvarchar(20), CONVERT(DATE, A.DOCDATE, 112)) DocumentDate, CONVERT(nvarchar(20), CONVERT(DATE, B.DUEDATE, 112)) DueDate, " +
                       $"CONVERT(NUMERIC(32,2), ISNULL(D.ApplyAmount, 0)) AppliedAmount, ISNULL(E.USRDEF05, '') Ncf, RTRIM(A.CURNCYID) CurrencyId, 0 DocumentType, " +
                       $"CASE (SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.Seabo_SOP_Line_Items AA WHERE AA.SOP_Number = A.DOCNUMBR AND F.Item_Number IN ('INTERESES')) WHEN 0 THEN 'ENERGIA' ELSE 'INTERESES' END Module " +
                       $"FROM {Helpers.InterCompanyId}.dbo.RM20101 A " +
                       $"INNER JOIN {Helpers.InterCompanyId}.dbo.SOP30200 B ON A.DOCNUMBR = B.SOPNUMBE " +
                       $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EFRM30200 C ON A.DOCNUMBR = C.SopNumber " +
                       $"LEFT JOIN ( " +
                       $"SELECT CustomerId, SopNumber, ApplyAmount, BatchNumber, BillingType, Flag FROM {Helpers.InterCompanyId}.dbo.EFSOP00130 " +
                       $"WHERE BatchNumber = '{batchNumber}' AND CustomerId = '{customerId}' AND Flag = '{flag}' " +
                       $"UNION ALL " +
                       $"SELECT CustomerId, SopNumber, ApplyAmount, BatchNumber, BillingType, Flag FROM {Helpers.InterCompanyId}.dbo.EFSOP10130 " +
                       $"WHERE BatchNumber = '{batchNumber}' AND CustomerId = '{customerId}' AND Flag = '{flag}' " +
                       $") AS D ON A.DOCNUMBR = D.SopNumber " +
                       $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SOP10106 E ON A.DOCNUMBR = E.SOPNUMBE " +
                       $"INNER JOIN {Helpers.InterCompanyId}.dbo.Seabo_SOP_Line_Items F ON A.DOCNUMBR = F.SOP_Number " +
                       $"WHERE A.CUSTNMBR = '{customerId}' AND RTRIM(A.CURNCYID) = '{currencyId}' AND (A.ORTRXAMT - ISNULL(C.DocumentApply, 0)) > 0 AND A.RMDTYPAL = 1 AND SUBSTRING(A.DOCNUMBR, 1, 3) = 'FAC' " +
                       $"AND (A.ORTRXAMT - ISNULL(C.DocumentApply, 0)) - ISNULL(D.ApplyAmount, 0) < {amount} AND CONVERT(DATE, A.DOCDATE) >= CONVERT(DATE, '{startDateInvoice.ToString("yyyyMMdd")}') " +
                       $"/*AND F.Item_Number NOT IN ('INTERESES') */ ";
                }

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
        public JsonResult RemoveApplyReliquidationTransaction(string batchNumber, string customerId, string marker)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP00130 " +
                    $"WHERE BatchNumber = '{batchNumber}' AND Flag = '{marker}' AND CustomerId = '{customerId}' AND BillingType = '30'");

                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP10130 " +
                     $"WHERE BatchNumber = '{batchNumber}' AND Flag = '{marker}' AND CustomerId = '{customerId}' AND BillingType = '30'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveReliquidationTransactionInvoiceApply(string batchNumber, string customerId, string sopNumber, decimal applyAmount,
            string ncf, string flag, string documentDate, string dueDate, int billingType = 30)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"INTRANET.dbo.InsertBillingMemInvoice '{Helpers.InterCompanyId}','{batchNumber}','{customerId}','{sopNumber}','{applyAmount}'," + $"'{ncf}','{billingType}'," +
                    $"'{flag}'," + $"'{DateTime.ParseExact(documentDate, "yyyy-MM-dd", null).ToString("yyyyMMdd")}','{DateTime.ParseExact(dueDate, "yyyy-MM-dd", null).ToString("yyyyMMdd")}'";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public JsonResult PostReliquidationTransactionEntry(string batchNumber, int billingType = 10)
        {
            string xStatus = "";
            try
            {
                var sqlQuery = $"INTRANET.dbo.PostReliquidationTransaction '{Helpers.InterCompanyId}','{batchNumber}', {billingType},'{Account.GetAccount(User.Identity.GetUserName()).UserId}'";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult VerifyReliquidationNcfAvailability(string batchNumber, int preInvoice, int batch, string sopNumber = "", int billingType = 10)
        {
            string xStatus = "";
            List<Lookup> registros = null;
            try
            {
                var customerNumber = "";
                if (!string.IsNullOrEmpty(sopNumber))
                    customerNumber = _repository.ExecuteScalarQuery<string>($"SELECT CustomerNumber FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 " +
                        $"WHERE BatchNumber = '{batchNumber}' AND SopNumber = '{sopNumber}' AND BillingType = {billingType}");
                registros = _repository.ExecuteQuery<Lookup>(
                    $"INTRANET.dbo.VerifyNcfAvailabilityReliquidationTransaction '{Helpers.InterCompanyId}','{batchNumber}','{preInvoice}','{batch}','{customerNumber}',{billingType}").ToList();
                if (registros.Count > 0)
                {
                    xStatus += "No existen suficientes numeros de comprobantes fiscal para la transacción o transacciones a procesar por favor verificar ";
                    xStatus += "\n";
                    xStatus += "Las transacciones y el tipo de NCF sin numero de comprobante fiscal son: ";
                    var count = 1;
                    foreach (var item in registros)
                    {
                        xStatus += "\n";
                        xStatus += count + ": " + item.Id + " - " + item.DataExtended;
                        count++;
                    }
                    xStatus += "\n";
                    xStatus += "Debe de ir a la configuración de comprobantes fiscales y agregar una secuencia valida para el tipo de NCF para poder continuar con el proceso, Gracias";
                    xStatus += "\n";
                    if (preInvoice == 1)
                        xStatus += "Desea continuar con el proceso obviando este mensaje, esto no le asignara NCF a las transacciones arriba indicadas ?";
                }
                else
                    xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Pre-Invoice

        public ActionResult MemBillingTransactionIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "PreInvoice"))
                return RedirectToAction("NotPermission", "Home");
            var query = $"SELECT BatchNumber, BatchDescription, CONVERT(NUMERIC(19,5), 0) TotalAmount, NoOfTransactions NumberOfTransactions, BillingType " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20500 WHERE Posted = 0";
            var lista = _repository.ExecuteQuery<MemBillingHead>(query).ToList();
            lista.ForEach(p =>
            {
                var totalPesos = _repository.ExecuteScalarQuery<decimal?>($"SELECT ISNULL(SUM(ISNULL(Total,0)), 0) FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 WHERE BatchNumber = '{p.BatchNumber}' AND CurrencyId = 'RDPESO'") ?? 0;
                var totalDolares = _repository.ExecuteScalarQuery<decimal?>($"SELECT ISNULL(SUM(ISNULL(Total,0)), 0) FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 WHERE BatchNumber = '{p.BatchNumber}' AND CurrencyId = 'Z-US$'") ?? 0;
                p.TotalAmount = totalPesos;
                p.TotalForeignAmount = totalDolares;
            });
            return View(lista);
        }

        public ActionResult MemBillingTransaction(string id, int billingType = 10)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "PreInvoice"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = $"SELECT BatchNumber, BatchDescription, CONVERT(NUMERIC(19,5), 0)  TotalAmount, NoOfTransactions NumberOfTransactions, LastUserId UserId " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20500 WHERE BatchNumber = '{id}' AND BillingType = {billingType}";
            var batch = _repository.ExecuteScalarQuery<MemBillingHead>(sqlQuery);
            var totalPesos = _repository.ExecuteScalarQuery<decimal?>($"SELECT ISNULL(SUM(ISNULL(Total,0)), 0) FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 WHERE BatchNumber = '{batch.BatchNumber}' AND CurrencyId = 'RDPESO'") ?? 0;
            var totalDolares = _repository.ExecuteScalarQuery<decimal?>($"SELECT ISNULL(SUM(ISNULL(Total,0)), 0) FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 WHERE BatchNumber = '{batch.BatchNumber}' AND CurrencyId = 'Z-US$'") ?? 0;
            batch.TotalAmount = totalPesos;
            batch.TotalForeignAmount = totalDolares;
            sqlQuery = $"SELECT Exclude, BatchNumber, SopNumber, SopNumber DocumentNumber, Ncf, DueDate, DocumentDate, CustomerNumber, CustomerName, ContactPerson, TaxRegistrationNumber, " +
                $"Address, City, State, Country, Subtotal, TaxAmount, Total, CurrencyId, ExchangeRate, CreatedDate, DocumentType " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 WHERE BatchNumber = '{id}' AND Posted = 0 AND BillingType = {billingType} ORDER BY SopNumber";
            var invoices = _repository.ExecuteQuery<MemInvoiceHead>(sqlQuery).ToList();
            ViewBag.Invoices = invoices;
            ViewBag.Products = _repository.ExecuteQuery<Lookup>("SELECT ITEMNMBR Id, ITEMDESC Descripción, '' DataExtended FROM " + "" + Helpers.InterCompanyId + ".dbo.IV00101 WHERE ITMCLSCD = 'FACT'").ToList();
            ViewBag.BillingType = billingType;
            return View(batch);
        }

        public ActionResult GetInvoice(string batchNumber, string sopNumber, int billingType = 10)
        {
            string xStatus;
            MemInvoiceHead invoice = null;
            try
            {
                var sqlQuery = $"SELECT Exclude, BatchNumber, SopNumber, Ncf, DueDate, DocumentDate, CustomerNumber, CustomerName, ContactPerson, TaxRegistrationNumber, " +
                $"Address, City, State, Country, Subtotal, TaxAmount, Total, CurrencyId, ExchangeRate, Note, NcfDueDate, NcfType, DocumentType, ReferenceInvoice, ReferenceNcf, ContactPerson " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 WHERE BatchNumber = '{batchNumber}' AND SopNumber = '{sopNumber}'";
                invoice = _repository.ExecuteScalarQuery<MemInvoiceHead>(sqlQuery);

                sqlQuery = $"SELECT Exclude, LineNumber, ItemNumber, ItemDescription, Quantity, Price, TaxAmount, Total " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20200 WHERE SopNumber = '{sopNumber}'";
                invoice.Details = _repository.ExecuteQuery<MemInvoiceDetail>(sqlQuery).ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, invoice }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetBatchMemBilling(string batchNumber, int billingType = 10)
        {
            string xStatus;
            MemBillingHead batch = null;
            try
            {
                var sqlQuery = $"SELECT BatchNumber, BatchDescription, BatchTotal, NoOfTransactions, Note, DocumentDate, DueDate, ExchangeRate " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20500 WHERE BatchNumber = '{batchNumber}' AND BillingType = {billingType} ";
                batch = _repository.ExecuteScalarQuery<MemBillingHead>(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, batch }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateBatchMemBilling(string batchNumber, string batchDescription, DateTime documentDate, DateTime dueDate, string note, decimal exchangeRate, int billingType = 10)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20100 SET " +
                    $"DocumentDate = '{documentDate.ToString("yyyyMMdd")}', DueDate = '{dueDate.ToString("yyyyMMdd")}', Note = '{note}', ExchangeRate = {exchangeRate} " +
                   $"WHERE BatchNumber = '{batchNumber}' AND BillingType = {billingType}");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20500 SET BatchDescription = '{batchDescription}', ExchangeRate = {exchangeRate}, " +
                    $"DocumentDate = '{documentDate.ToString("yyyyMMdd")}', DueDate = '{dueDate.ToString("yyyyMMdd")}', Note = '{note}' " +
                    $"WHERE BatchNumber = '{batchNumber}' AND BillingType = {billingType}");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveInvoiceMemBilling(string sopNumber, DateTime documentDate, DateTime dueDate, string note, decimal exchangeRate, string contactPerson, List<MemInvoiceDetail> lines)
        {
            string xStatus = "";
            try
            {
                foreach (var item in lines)
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20200 SET ItemDescription = '{item.ItemDescription}', " +
                   $"Price = '{item.Price}', TaxAmount = '{item.TaxAmount}', Total = '{item.Total}', Quantity = '{item.Quantity}' " +
                   $"WHERE SopNumber = '{sopNumber}' AND LineNumber = '{item.LineNumber}'");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20100 SET " +
                    $"DocumentDate = '{documentDate.ToString("yyyyMMdd")}', DueDate = '{dueDate.ToString("yyyyMMdd")}', Note = '{note}', ExchangeRate = {exchangeRate}, " +
                    $"Subtotal = '{lines.Sum(x => x.Price * x.Quantity)}', TaxAmount = '{lines.Sum(x => x.TaxAmount)}', Total = '{lines.Sum(x => x.Total)}', ContactPerson = '{contactPerson}'" +
                    $"WHERE SopNumber = '{sopNumber}'");

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ExcludeInvoiceMem(string sopNumber, bool exclude)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20100 SET Exclude = '{exclude}' " +
                   $"WHERE SopNumber = '{sopNumber}'");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20200 SET Exclude = '{exclude}' " +
                    $"WHERE SopNumber = '{sopNumber}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ExcludeProductInvoiceMem(string sopNumber, string itemNumber, int lineNumber, bool exclude)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20200 SET Exclude = '{exclude}' " +
                    $"WHERE SopNumber = '{sopNumber}' AND ItemNumber = '{itemNumber}' AND LineNumber = {lineNumber} ");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteBatchMemBilling(string batchNumber, int billingType = 10)
        {
            string xStatus = "";
            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 " +
                    $"WHERE BatchNumber = '{batchNumber}' AND Posted = 1 AND BillingType = {billingType}");
                if (count > 0)
                    xStatus = "No se puede eliminar este lote por que ya hay facturas contabilizadas";
                else
                {
                    var listInvoice = _repository.ExecuteQuery<string>($"SELECT SopNumber FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 WHERE BatchNumber = '{batchNumber}' AND BillingType = {billingType}");
                    foreach (var item in listInvoice)
                        _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP20200 WHERE SopNumber = '{item}'");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP20100 WHERE BatchNumber = '{batchNumber}' AND BillingType = {billingType}");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFSOP20500 WHERE BatchNumber = '{batchNumber}' AND BillingType = {billingType}");
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP10100 SET Posted = 0 WHERE BatchNumber = '{batchNumber}' AND BillingType = {billingType}");
                    xStatus = "OK";
                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CloseBatchMemBilling(string batchNumber, int billingType = 10)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20500 SET Posted = 1 WHERE BatchNumber = '{batchNumber}' AND BillingType = {billingType}");
                SendMailNcfNotification();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult OpenBatchTransaction(string batchNumber)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20500 SET Posted = 0 WHERE BatchNumber = '{batchNumber}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AddItemTransaction(string sopNumber, string itemNumber, string itemDescription, string quantity, string price)
        {
            string xStatus = "";
            try
            {
                var lineNumber = _repository.ExecuteScalarQuery<int?>($"SELECT MAX(LineNumber) FROM {Helpers.InterCompanyId}.dbo.EFSOP20200 WHERE SopNumber = '{sopNumber}'") ?? 0;
                string sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFSOP20200 (SopNumber, LineNumber, ItemNumber, ItemDescription, Quantity, Price, TaxAmount, Total, Exclude) " +
                    $"VALUES ('{sopNumber}', {lineNumber + 1}, '{itemNumber}','{itemDescription}','{Convert.ToDecimal(quantity)}','{Convert.ToDecimal(price)}', " +
                    $"{Helpers.InterCompanyId}.dbo.GetTaxAmount('{itemNumber}', {Convert.ToDecimal(price)}), " +
                    $"({Convert.ToDecimal(price)} + {Helpers.InterCompanyId}.dbo.GetTaxAmount('{itemNumber}', {Convert.ToDecimal(price)})) * {Convert.ToDecimal(quantity)}, 0)";
                _repository.ExecuteCommand(sqlQuery);

                var subTotal = _repository.ExecuteScalarQuery<decimal?>($"SELECT SUM(Price * Quantity) FROM {Helpers.InterCompanyId}.dbo.EFSOP20200 WHERE SopNumber = '{sopNumber}' AND Exclude = 0") ?? 0;
                var taxAmount = _repository.ExecuteScalarQuery<decimal?>($"SELECT SUM(TaxAmount * Quantity) FROM {Helpers.InterCompanyId}.dbo.EFSOP20200 WHERE SopNumber = '{sopNumber}' AND Exclude = 0") ?? 0;
                var total = _repository.ExecuteScalarQuery<decimal?>($"SELECT SUM(Total * Quantity) FROM {Helpers.InterCompanyId}.dbo.EFSOP20200 WHERE SopNumber = '{sopNumber}' AND Exclude = 0") ?? 0;

                sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20100 SET Subtotal = {subTotal}, TaxAmount = {taxAmount}, Total = {total} WHERE SopNumber = '{sopNumber}'";
                _repository.ExecuteCommand(sqlQuery);

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult PostInvoiceMem(string batchNumber, string sopNumber, int billingType = 10)
        {
            string xStatus = "";
            string sucessMessage = "";
            string errorMessage = "";
            var invoiceNumbers = new List<string[]>();
            try
            {
                var sqlQuery = $"SELECT A.BatchNumber, '' SopNumber, A.SopNumber DocumentNumber,  '' Ncf, GETDATE() NcfDueDate, " +
                    $"(CASE DocumentType WHEN 3 THEN 'B03' WHEN 4 THEN 'B04' ELSE B.COMMENT1 END) NcfType, A.CustomerNumber CustomerId, " +
                    $"A.CurrencyId, A.ExchangeRate, A.DocumentDate, A.DueDate, A.Note, A.ReferenceInvoice, A.ReferenceNcf, A.ContactPerson " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 A " +
                    $"INNER JOIN {Helpers.InterCompanyId}.dbo.RM00101 B ON A.CustomerNumber = B.CUSTNMBR " +
                    $"WHERE A.BatchNumber = '{batchNumber}' AND A.SopNumber = '{sopNumber}' AND Exclude = 0 AND BillingType = {billingType}";
                var invoice = _repository.ExecuteScalarQuery<GPInvoice>(sqlQuery);

                sqlQuery = $"SELECT SopNumber, LineNumber, ItemNumber, ItemDescription, Quantity, Price, TaxAmount, Total " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20200 " +
                    $"WHERE Exclude = 0 AND SopNumber = '{invoice.DocumentNumber}'";
                invoice.Lines = _repository.ExecuteQuery<GPInvoiceLine>(sqlQuery).ToList();

                var service = new ServiceContract();
                var list = new List<GPInvoice>
                {
                    invoice
                };

                if (billingType == 30)
                {
                    service.CreateInvoice(list.Where(x => x.NcfType == "B03").ToList(), _repository, Account.GetAccount(User.Identity.GetUserName()).UserId, ref invoiceNumbers, ref errorMessage);
                    service.CreateReturn(list.Where(x => x.NcfType == "B04").ToList(), _repository, Account.GetAccount(User.Identity.GetUserName()).UserId, ref invoiceNumbers, ref errorMessage);
                }
                else
                    service.CreateInvoice(list, _repository, Account.GetAccount(User.Identity.GetUserName()).UserId, ref invoiceNumbers, ref errorMessage);

                if (errorMessage.Length > 0)
                {
                    xStatus = "Ha ocurrido un error al momento de generar los documentos por favor verificar los siguientes: " + Environment.NewLine + errorMessage;
                }
                else
                {
                    sucessMessage = "Se han creado en el sistema GP los siguientes documentos: " + Environment.NewLine;
                    foreach (var item in invoiceNumbers)
                        sucessMessage += item[0] + "  -  " + item[1] + Environment.NewLine;
                    sqlQuery = $"SELECT COUNT(*) " +
                   $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 " +
                   $"WHERE BatchNumber = '{batchNumber}' AND Posted = 0 AND Exclude = 0 AND BillingType = {billingType}";
                    var count = _repository.ExecuteScalarQuery<int>(sqlQuery);
                    if (count == 0)
                    {
                        sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20500 SET Posted = 1 WHERE BatchNumber = '{batchNumber}' AND BillingType = {billingType}";
                        _repository.ExecuteCommand(sqlQuery);
                        SendMailNcfNotification();
                    }
                    var batchDescription = _repository.ExecuteScalarQuery<string>($"SELECT BatchDescription FROM {Helpers.InterCompanyId}.dbo.EFSOP20500 " +
                    $"WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.SY00500 SET BCHCOMNT = '{batchDescription}' WHERE BACHNUMB = '{batchNumber}'");

                    xStatus = "OK";
                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, sucessMessage, invoiceNumber = (invoiceNumbers?.FirstOrDefault()?[0] ?? "") }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult PostBatchMem(string batchNumber, int billingType = 10)
        {
            string xStatus = "";
            string sucessMessage = "";
            string errorMessage = "";
            string xExclusion = "";
            try
            {
                var sqlQuery = $"SELECT A.BatchNumber, '' SopNumber, A.SopNumber DocumentNumber,  '' Ncf, GETDATE() NcfDueDate, " +
                    $"(CASE DocumentType WHEN 3 THEN 'B03' WHEN 4 THEN 'B04' ELSE B.COMMENT1 END) NcfType, A.CustomerNumber CustomerId, " +
                    $"A.CurrencyId, A.ExchangeRate, A.DocumentDate, A.DueDate, A.Note, A.ReferenceInvoice, A.ReferenceNcf, A.ContactPerson " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 A " +
                    $"INNER JOIN {Helpers.InterCompanyId}.dbo.RM00101 B ON A.CustomerNumber = B.CUSTNMBR " +
                    $"WHERE A.BatchNumber = '{batchNumber}' AND A.Posted = 0 AND A.Exclude = 0 AND BillingType = {billingType}";
                var invoices = _repository.ExecuteQuery<GPInvoice>(sqlQuery).ToList();

                invoices.ForEach(p =>
                {
                    sqlQuery = $"SELECT SopNumber, ItemNumber, ItemDescription, Quantity, Price, TaxAmount, Total " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20200 " +
                    $"WHERE Exclude = 0 AND SopNumber = '{p.DocumentNumber}'";
                    p.Lines = _repository.ExecuteQuery<GPInvoiceLine>(sqlQuery).ToList();
                });
                var service = new ServiceContract();
                var invoiceNumbers = new List<string[]>();
                if (billingType == 30)
                {
                    service.CreateInvoice(invoices.Where(x => x.NcfType == "B03").ToList(), _repository, Account.GetAccount(User.Identity.GetUserName()).UserId, ref invoiceNumbers, ref errorMessage);
                    service.CreateReturn(invoices.Where(x => x.NcfType == "B04").ToList(), _repository, Account.GetAccount(User.Identity.GetUserName()).UserId, ref invoiceNumbers, ref errorMessage);
                }
                else
                    service.CreateInvoice(invoices, _repository, Account.GetAccount(User.Identity.GetUserName()).UserId, ref invoiceNumbers, ref errorMessage);

                if (errorMessage.Length > 0)
                {
                    xStatus = "Ha ocurrido un error al momento de generar los documentos por favor verificar los siguientes: " + Environment.NewLine + errorMessage;
                }
                else
                {
                    sucessMessage += "Se han creado en el sistema GP los siguientes documentos: " + Environment.NewLine;
                    foreach (var item in invoiceNumbers)
                        sucessMessage += item[0] + " - " + item[1] + " - " + item[2] + Environment.NewLine;

                    sqlQuery = $"SELECT COUNT(*) " +
                      $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 " +
                      $"WHERE BatchNumber = '{batchNumber}' AND Exclude = 1 AND BillingType = {billingType}";
                    var count = _repository.ExecuteScalarQuery<int>(sqlQuery);
                    if (count == 0)
                    {
                        sqlQuery = $"SELECT COUNT(*) " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 " +
                        $"WHERE BatchNumber = '{batchNumber}' AND Posted = 0 AND Exclude = 0 AND BillingType = {billingType}";
                        count = _repository.ExecuteScalarQuery<int>(sqlQuery);
                        if (count == 0)
                        {
                            sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20500 SET Posted = 1 WHERE BatchNumber = '{batchNumber}' AND BillingType = {billingType}";
                            _repository.ExecuteCommand(sqlQuery);
                            SendMailNcfNotification();
                        }
                    }
                    else
                        xExclusion = "OK";
                    xStatus = "OK";
                    var batchDescription = _repository.ExecuteScalarQuery<string>($"SELECT BatchDescription FROM {Helpers.InterCompanyId}.dbo.EFSOP20500 " +
                        $"WHERE BatchNumber = '{batchNumber}' AND BillingType = '{billingType}'");
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.SY00500 SET BCHCOMNT = '{batchDescription}' WHERE BACHNUMB = '{batchNumber}'");

                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, sucessMessage, exclusion = xExclusion }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public JsonResult BillingInvoiceReport(string sopNumber, string choice, bool duplicate, string preInvoice, int billingType)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                var reportId = 24;
                if (choice == "10" && !duplicate)
                    reportId = 24;
                else if (choice == "20" && !duplicate)
                    reportId = 25;
                else if (choice == "10" && duplicate)
                    reportId = 26;
                else if (choice == "20" && !duplicate)
                    reportId = 27;

                if (preInvoice == "0")
                    reportId = 33;

                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "BillingInvoice.pdf",
                    $"INTRANET.dbo.SopDocumentReport '{Helpers.InterCompanyId}','{sopNumber}', {preInvoice}, {billingType}", reportId, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public JsonResult BillingInvoiceBatchReport(string batchNumber, string choice, string preInvoice, int billingType = 10)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                var reportId = 24;
                if (choice == "10")
                    reportId = 28;
                else
                    reportId = 29;

                if (preInvoice == "0")
                    reportId = 34;
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "BillingInvoiceBatch.pdf",
                    $"INTRANET.dbo.SopBatchReport '{Helpers.InterCompanyId}','{batchNumber}', {preInvoice}, {billingType}", reportId, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        #endregion

        #region NCF Configuration

        public ActionResult NCFConfigurationIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "NCFConfiguration"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        public ActionResult GetNCFHeaders()
        {
            var sqlQuery = "SELECT DOCNUMBR Code, DOCDESCR Description, DOCTYPE DocumentType " +
                "FROM " + Helpers.InterCompanyId + ".dbo.EFNCF40101 ORDER BY DOCNUMBR";
            var ncfHeaderConfigurations = _repository.ExecuteQuery<NcfHeaderConfiguration>(sqlQuery).ToList();
            return Json(ncfHeaderConfigurations, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetNCFDetails(string code)
        {
            var sqlQuery = $"SELECT HDDOCNUM HeaderCode, DOCNUMBR DetailCode, FROMNUMB FromNumber, TONUMBER ToNumber, NEXTNUMB NextNumber, LEFTNUMB LeftNumber, ALERTNUM AlertNumber, DUEDATE DueDate, STATUS Status " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFNCF40102 WHERE HDDOCNUM = '{code}' ORDER BY DOCNUMBR";
            var products = _repository.ExecuteQuery<NcfDetailConfiguration>(sqlQuery).ToList();
            return Json(products, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveNcfHeaderConfiguration(NcfHeaderConfiguration header)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.EFNCF40101 WHERE DOCNUMBR = '" + header.Code + "'");
                var sqlQuery = "";

                if (count == 0)
                {
                    sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.EFNCF40101 (DOCNUMBR, DOCDESCR, DOCTYPE, LSTUSRID)" +
                        $"VALUES ('{header.Code}', '{header.Description}', '{header.DocumentType}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}')";
                    _repository.ExecuteCommand(sqlQuery);
                }
                else
                {
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFNCF40101 SET DOCDESCR = '{header.Description}', DOCTYPE = '{header.DocumentType}', " +
                        $"LSTUSRID = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' WHERE DOCNUMBR = '{header.Code}'";
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
        public JsonResult SaveNcfDetailConfiguration(NcfDetailConfiguration header)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EFNCF40102 " +
                $"WHERE HDDOCNUM = '{header.HeaderCode}' AND DOCNUMBR = '" + header.DetailCode + "'");
                var sqlQuery = "";

                if (count == 0)
                {
                    count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EFNCF40102 " +
                    $"WHERE {header.NextNumber} BETWEEN FROMNUMB AND TONUMBER AND HDDOCNUM = '{header.HeaderCode}' ");
                    if (count == 0)
                    {
                        sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.EFNCF40102 (HDDOCNUM, FROMNUMB, TONUMBER, NEXTNUMB, LEFTNUMB, ALERTNUM, DUEDATE, STATUS)" +
                        $"VALUES ('{header.HeaderCode}', '{header.FromNumber}', '{header.ToNumber}', '{header.NextNumber}', '{header.LeftNumber}', '{header.AlertNumber}', '{header.DueDate.ToString("yyyyMMdd")}', '{header.Status}')";
                        _repository.ExecuteCommand(sqlQuery);
                        xStatus = "OK";
                    }
                    else
                        xStatus = "Ya existe una secuencia de NCF con un rango que contiene el mismo rango que ha especificado, por favor corregir";
                }
                else
                {
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFNCF40102 SET FROMNUMB = '{header.FromNumber}', TONUMBER = '{header.ToNumber}', " +
                        $"NEXTNUMB = '{header.NextNumber}', DUEDATE = '{header.DueDate.ToString("yyyyMMdd")}', STATUS = '{header.Status}', LEFTNUMB = '{header.LeftNumber}', ALERTNUM = '{header.AlertNumber}' " +
                        $"WHERE HDDOCNUM = '{header.HeaderCode}' AND DOCNUMBR = '{header.DetailCode}'";
                    _repository.ExecuteCommand(sqlQuery);
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
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFNCF40102 WHERE HDDOCNUM = '{code}'");
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFNCF40101 WHERE DOCNUMBR = '{code}'");
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
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFNCF40102 WHERE HDDOCNUM = '{codeHeader}' AND DOCNUMBR = '{codeDetail}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Billing Configuration

        public ActionResult ConfigurationIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "Configuration"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = $"SELECT FILEPATH FilePath, SHEET Sheet, RWBEGDAT RowBeginningData, RWNAMCOL RowNameColumn, CREDEXCCOL CréditorExcelColumn, " +
                $"DEBTEXCCOL DebtorExcelColumn, SUMRWBEGDAT SummaryRowBeginningData, SUMTOTCOL SummaryTotalColumn, SQLEMAPRF SqlEmailProfile, " +
                $"DEFNOTNC DefaultNoteNC, DEFNOTND DefaultNoteND, SRTDATINV InvoiceStartDate, TAXREGTN TaxRegistrationNumber " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFRM40101";

            var configuration = _repository.ExecuteScalarQuery<ConfigurationModel>(sqlQuery);
            ViewBag.Customers = _repository.ExecuteQuery<Lookup>("SELECT RTRIM(CUSTNMBR) Id, RTRIM(CUSTNAME) Descripción, '' DataExtended FROM " +
                "" + Helpers.InterCompanyId + ".dbo.RM00101 WHERE INACTIVE = 0").ToList();
            ViewBag.Products = _repository.ExecuteQuery<Lookup>("SELECT ITEMNMBR Id, ITEMDESC Descripción, '' DataExtended FROM " +
                "" + Helpers.InterCompanyId + ".dbo.IV00101 WHERE ITMCLSCD = 'FACT'").ToList();
            return View(configuration);
        }

        public ActionResult GetCustomerConfigurations()
        {
            var sqlQuery = "SELECT A.CUSTNMBR CustomerId, RTRIM(B.CUSTNAME) CustomerName, A.CUSTIDEXT CustomerExternalId, RTRIM(B.CUSTCLAS) CustomerClass " +
                "FROM " + Helpers.InterCompanyId + ".dbo.EFRM40201 A INNER JOIN " + Helpers.InterCompanyId + ".dbo.RM00101 B ON A.CUSTNMBR = B.CUSTNMBR " +
                "ORDER BY A.CUSTNMBR";

            var customers = _repository.ExecuteQuery<CustomerConfiguration>(sqlQuery).ToList();
            return Json(customers, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetProductConfigurations()
        {
            var sqlQuery = "SELECT A.ITEMNMBR ProductId, RTRIM(B.ITEMDESC) ProductName, A.COLNINDX ColumnIndex, A.EXTNLABL ExternalLabel, LINEORDER LineOrder  " +
                "FROM " + Helpers.InterCompanyId + ".dbo.EFRM40102 A INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B ON A.ITEMNMBR = B.ITEMNMBR " +
                "ORDER BY A.ITEMNMBR";

            var products = _repository.ExecuteQuery<ProductConfiguration>(sqlQuery).ToList();
            return Json(products, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetEmailAccountConfigurations()
        {
            var sqlQuery = "SELECT Email Id, RTRIM(Name) Descripción, '' DataExtended " +
                "FROM " + Helpers.InterCompanyId + ".dbo.EFRM40301 " +
                "ORDER BY Email";

            var products = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();
            return Json(products, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveConfiguration(ConfigurationModel configuration)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.EFRM40101");
                var sqlQuery = "";

                if (count == 0)
                {
                    sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.EFRM40101 (FILEPATH, SHEET, RWBEGDAT, RWNAMCOL, CREDEXCCOL, DEBTEXCCOL, SUMRWBEGDAT, SUMTOTCOL, SQLEMAPRF, DEFNOTNC, DEFNOTND, SRTDATINV, TAXREGTN)" +
                        $"VALUES ('{configuration.FilePath}', '{configuration.Sheet}', '{configuration.RowBeginningData}', '{configuration.RowNameColumn}'," +
                        $"'{configuration.CréditorExcelColumn}','{configuration.DebtorExcelColumn}', '{configuration.SummaryRowBeginningData}', '{configuration.SummaryTotalColumn}', " +
                        $"'{configuration.SqlEmailProfile}', '{configuration.DefaultNoteNC}', '{configuration.DefaultNoteND}'," +
                        $"'{configuration.InvoiceStartDate.ToString("yyyyMMdd")}', '{configuration.TaxRegistrationNumber}')";
                    _repository.ExecuteCommand(sqlQuery);
                }
                else
                {
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFRM40101 SET TAXREGTN = '{configuration.TaxRegistrationNumber}', FILEPATH = '{configuration.FilePath}', SHEET = '{configuration.Sheet}', " +
                        $"RWBEGDAT = '{configuration.RowBeginningData}', RWNAMCOL = '{configuration.RowNameColumn}', CREDEXCCOL = '{configuration.CréditorExcelColumn}', " +
                        $"DEBTEXCCOL = '{configuration.DebtorExcelColumn}', SUMRWBEGDAT = '{configuration.SummaryRowBeginningData}', SUMTOTCOL = '{configuration.SummaryTotalColumn}', " +
                        $"SQLEMAPRF = '{configuration.SqlEmailProfile}', DEFNOTNC = '{configuration.DefaultNoteNC}', DEFNOTND = '{configuration.DefaultNoteND}'," +
                        $"SRTDATINV = '{configuration.InvoiceStartDate.ToString("yyyyMMdd")}'";
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
        public JsonResult SaveProductConfiguration(ProductConfiguration product)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.EFRM40102 WHERE ITEMNMBR = '" + product.ProductId + "'");
                var sqlQuery = "";

                if (count == 0)
                {
                    sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.EFRM40102 (ITEMNMBR, ITEMDESC, COLNINDX, EXTNLABL, LINEORDER)" +
                        $"VALUES ('{product.ProductId}', '{product.ProductName}', '{product.ColumnIndex}', '{product.ExternalLabel}', '{product.LineOrder}')";
                    _repository.ExecuteCommand(sqlQuery);
                }
                else
                {
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFRM40102 SET LINEORDER = '{product.LineOrder}', COLNINDX = '{product.ColumnIndex}', EXTNLABL = '{product.ExternalLabel}' " +
                        $"WHERE ITEMNMBR = '{product.ProductId}'";
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
        public JsonResult SaveCustomerConfiguration(CustomerConfiguration customer)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                                ".dbo.EFRM40201 WHERE CUSTIDEXT = '" + customer.CustomerExternalId + "'");
                var sqlQuery = "";

                if (count == 0)
                {
                    sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.EFRM40201 (CUSTNMBR, CUSTNAME, CUSTIDEXT, LSTUSRID)" +
                        $"VALUES ('{customer.CustomerId}', '{customer.CustomerName}', '{customer.CustomerExternalId}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}')";
                    _repository.ExecuteCommand(sqlQuery);
                }
                else
                {
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFRM40201 SET CUSTNMBR = '{customer.CustomerId}', LSTUSRID = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                        $"WHERE CUSTIDEXT = '{customer.CustomerExternalId}'";
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
        public JsonResult SaveEmailAccountConfiguration(string email, string name)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EFRM40301 WHERE Email = '{email}'");
                var sqlQuery = "";

                if (count == 0)
                {
                    sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFRM40301 (Email, Name) VALUES ('{email}', '{name}')";
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
        public JsonResult DeleteCustomerConfiguration(string id)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFRM40201 WHERE CUSTIDEXT = '{id}'";
                _repository.ExecuteCommand(sqlQuery);

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult DeleteProductConfiguration(string id)
        {
            string xStatus;

            try
            {

                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFRM40102 WHERE ITEMNMBR = '{id}'";
                _repository.ExecuteCommand(sqlQuery);

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult DeleteEmailAccountConfiguration(string id)
        {
            string xStatus;

            try
            {

                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFRM40301 WHERE Email = '{id}'";
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

        #region Account Configuration

        public ActionResult AccountConfigurationIndex()
        {
            //if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "AccountConfiguration"))
            //    return RedirectToAction("NotPermission", "Home");
            ViewBag.Customers = _repository.ExecuteQuery<Lookup>("SELECT RTRIM(CUSTNMBR) Id, RTRIM(CUSTNAME) Descripción, '' DataExtended FROM " +
                "" + Helpers.InterCompanyId + ".dbo.RM00101 WHERE INACTIVE = 0").ToList();
            ViewBag.Accounts = _repository.ExecuteQuery<Lookup>("SELECT B.ACTNUMST Id, RTRIM(A.ACTDESCR) Descripción, CONVERT(NVARCHAR(20), A.ACTINDX) DataExtended " +
                $"FROM {Helpers.InterCompanyId}.dbo.GL00100 A INNER JOIN {Helpers.InterCompanyId}.dbo.GL00105 B ON A.ACTINDX = B.ACTINDX ORDER BY A.ACTINDX").ToList();
            return View();
        }

        [HttpPost]
        public ActionResult GetIncomeTypes(string id)
        {
            try
            {
                var sqlQuery = $"SELECT DISTINCT A.ITEMNMBR Id, A.ITEMDESC Descripción, "
                               + $"CASE LEN(ISNULL(B.ItemCode, '')) WHEN '0' THEN '0' ELSE '1' END DataExtended "
                               + $"FROM {Helpers.InterCompanyId}.dbo.IV00101 A "
                               + $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EFRM40401 B ON A.ITEMNMBR = B.ItemCode "
                               + $"AND B.IncomeType = '{id}' " +
                               $"WHERE A.ITMCLSCD = 'FACT'";

                var permissions = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();
                return Json(permissions, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public JsonResult SaveIncomeType(string incomeType, string[] products)
        {
            string xStatus;

            try
            {
                var sqlQuery = "";
                _repository.ExecuteCommand($"DELETE FROM {Helpers.InterCompanyId}.dbo.EFRM40401 WHERE IncomeType = '{incomeType}'");
                foreach (var item in products)
                {
                    sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFRM40401 ([IncomeType],[ItemCode]) "
                               + $"VALUES ('{incomeType}','{item}')";
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

        public ActionResult GetWithHoldingCustomers()
        {
            var sqlQuery = "SELECT A.CustomerId, RTRIM(B.CUSTNAME) CustomerName, CONVERT(NVARCHAR(20), A.[Percent]) CustomerClass " +
                "FROM " + Helpers.InterCompanyId + ".dbo.EFRM40501 A INNER JOIN " + Helpers.InterCompanyId + ".dbo.RM00101 B ON A.CustomerId = B.CUSTNMBR " +
                "ORDER BY A.CustomerId";

            var customers = _repository.ExecuteQuery<CustomerConfiguration>(sqlQuery).ToList();
            return Json(customers, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveWithHoldingCustomer(string customerId, int percent)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                                ".dbo.EFRM40501 WHERE CustomerId = '" + customerId + "'");
                var sqlQuery = "";

                if (count == 0)
                {
                    sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.EFRM40501 (CustomerId, [Percent], LastUserId) " +
                        $"VALUES ('{customerId}', '{percent}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}')";
                    _repository.ExecuteCommand(sqlQuery);
                }
                else
                {
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFRM40501 SET [Percent] = '{percent}', LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                        $"WHERE CustomerId = '{customerId}'";
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
        public JsonResult DeleteWithHoldingCustomer(string id)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFRM40501 WHERE CustomerId = '{id}'";
                _repository.ExecuteCommand(sqlQuery);

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public ActionResult GetAccountProducts(string id)
        {
            try
            {
                var sqlQuery = $"SELECT DISTINCT A.ITEMNMBR Id, A.ITEMDESC Descripción, "
                               + $"CASE LEN(ISNULL(B.ItemCode, '')) WHEN 0 THEN 'Sin Asignar' ELSE 'Asignado' END DataExtended "
                               + $"FROM {Helpers.InterCompanyId}.dbo.IV00101 A "
                               + $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EFRM40601 B ON A.ITEMNMBR = B.ItemCode "
                               + $"AND B.CustomerId = '{id}' " +
                               $"WHERE A.ITMCLSCD = 'FACT'";

                var permissions = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();
                return Json(permissions, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public ActionResult GetAccounts(string customerId, string productId)
        {
            try
            {
                var sqlQuery = $"SELECT A.AccountType ItemId, RTRIM(ISNULL(D.ACTDESCR, '')) ItemNam, RTRIM(ISNULL(C.ACTNUMST, '')) ItemDesc, " +
                    $"CONVERT(nvarchar(20), A.Account) DataExtended, CONVERT(nvarchar(20), ISNULL(C.ACTINDX, '')) DataPlus FROM (" +
                    $"SELECT 'Cuentas por cobrar' AccountType, 1 Account " +
                    $"UNION ALL " +
                    $"SELECT 'Ventas' AccountType, 2 Account " +
                    $"UNION ALL " +
                    $"SELECT 'Costo de ventas' AccountType, 3 Account " +
                    $"UNION ALL " +
                    $"SELECT 'Descuentos' AccountType, 4 Account " +
                    $"UNION ALL " +
                    $"SELECT 'Devoluciones' AccountType, 5 Account ) A " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EFRM40601 B ON A.Account = B.AccountType AND B.CustomerId = '{customerId}' AND B.ItemCode = '{productId}' " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.GL00105 C ON B.AccountIndex = C.ACTINDX " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.GL00100 D ON C.ACTINDX = D.ACTINDX ";

                var permissions = _repository.ExecuteQuery<ItemLookup>(sqlQuery).ToList();
                return Json(permissions, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public JsonResult SaveAccount(string customerCode, string productCode, string accountType, string account)
        {
            string xStatus;

            try
            {
                var count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                                $".dbo.EFRM40601 WHERE CustomerId = '{customerCode.Trim()}' AND ItemCode = '{productCode.Trim()}' AND AccountType = '{accountType}'");
                var sqlQuery = "";
                if (count == 0)
                {
                    sqlQuery = "INSERT INTO " + Helpers.InterCompanyId + ".dbo.EFRM40601 (CustomerId, ItemCode, AccountType, AccountIndex, LastUserId) " +
                        $"VALUES ('{customerCode.Trim()}', '{productCode.Trim()}', '{accountType}', '{account}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}')";
                    _repository.ExecuteCommand(sqlQuery);
                }
                else
                {
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFRM40601 SET AccountIndex = '{account}', LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                        $"WHERE CustomerId = '{customerCode.Trim()}' AND ItemCode = '{productCode.Trim()}' AND AccountType = '{accountType}'";
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

        #region Batch Inquiry

        public ActionResult BatchInquiryIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "BatchInquiry"))
                return RedirectToAction("NotPermission", "Home");
            var query = $"SELECT A.BatchNumber, A.BatchDescription, A.TotalAmount, A.NumberOfTransactions, A.BillingType, A.DocumentDate " +
                $"FROM ( " +
                $"SELECT DISTINCT RTRIM(A.BACHNUMB) BatchNumber, RTRIM(A.BCHCOMNT) BatchDescription, A.BCHTOTAL TotalAmount, A.NUMOFTRX NumberOfTransactions, " +
                $"10 BillingType, ISNULL((SELECT TOP 1 DOCDATE FROM {Helpers.InterCompanyId}.dbo.SOP10100 AA WHERE AA.BACHNUMB = A.BACHNUMB), GETDATE()) DocumentDate " +
                $"FROM {Helpers.InterCompanyId}.dbo.SY00500 A " +
                $"WHERE A.BCHSOURC = 'Sales Entry'" +
                $"UNION ALL " +
                $"SELECT DISTINCT RTRIM(A.BACHNUMB) BatchNumber, RTRIM(A.BCHCOMNT) BatchDescription, A.BCHTOTAL TotalAmount, A.NUMOFTRX NumberOfTransactions, " +
                $"20 BillingType, ISNULL((SELECT TOP 1 DOCDATE FROM {Helpers.InterCompanyId}.dbo.SOP30200 AA WHERE AA.BACHNUMB = A.BACHNUMB), GETDATE()) DocumentDate " +
                $"FROM {Helpers.InterCompanyId}.dbo.SOP30100 A " +
                $"WHERE A.BCHSOURC = 'Sales Entry') A " +
                $"ORDER BY A.DocumentDate";
            var lista = _repository.ExecuteQuery<MemBillingHead>(query).ToList().OrderByDescending(x => x.DocumentDate);
            return View(lista);
        }

        public ActionResult BatchInquiryTransaction(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "BatchInquiry"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = $"SELECT RTRIM(BACHNUMB) BatchNumber, RTRIM(BCHCOMNT) BatchDescription, BCHTOTAL TotalAmount, NUMOFTRX NumberOfTransactions, 10 BillingType " +
                $"FROM {Helpers.InterCompanyId}.dbo.SY00500 WHERE BACHNUMB = '{id}'" +
                $"UNION ALL " +
                $"SELECT RTRIM(BACHNUMB) BatchNumber, RTRIM(BCHCOMNT) BatchDescription, BCHTOTAL TotalAmount, NUMOFTRX NumberOfTransactions, 20 BillingType " +
                $"FROM {Helpers.InterCompanyId}.dbo.SOP30100 WHERE BACHNUMB = '{id}'";
            var batch = _repository.ExecuteScalarQuery<MemBillingHead>(sqlQuery);
            int count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.SOP10100 WHERE BACHNUMB = '{id}'");
            if (count > 0)
                sqlQuery = $"SELECT RTRIM(A.BACHNUMB) BatchNumber, RTRIM(A.SOPNUMBE) SopNumber, RTRIM(A.SOPNUMBE) DocumentNumber, ISNULL(B.USRDEF05, '') Ncf, A.DUEDATE DueDate, A.DOCDATE DocumentDate, RTRIM(A.CUSTNMBR) CustomerNumber, " +
                    $"RTRIM(A.CUSTNAME) CustomerName, RTRIM(A.CNTCPRSN) ContactPerson, RTRIM(A.TXRGNNUM) TaxRegistrationNumber, " +
                    $"RTRIM(A.ADDRESS1) + RTRIM(A.ADDRESS2) Address, RTRIM(A.CITY) City, RTRIM(A.STATE) State, RTRIM(A.COUNTRY) Country, A.SUBTOTAL Subtotal, A.TAXAMNT TaxAmount, A.DOCAMNT Total, " +
                    $"RTRIM(A.CURNCYID) CurrencyId, A.XCHGRATE ExchangeRate, A.CREATDDT CreatedDate, CASE A.DOCID WHEN 'NOTA DE CREDITO' THEN 4 WHEN 'ND' THEN 3 ELSE 1 END DocumentType " +
                    $"FROM {Helpers.InterCompanyId}.dbo.SOP10100 A " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SOP10106 B ON A.SOPNUMBE = B.SOPNUMBE AND A.SOPTYPE = B.SOPTYPE " +
                    $"WHERE A.BACHNUMB = '{id}'";
            else
                sqlQuery = $"SELECT RTRIM(A.BACHNUMB) BatchNumber, RTRIM(A.SOPNUMBE) SopNumber, RTRIM(A.SOPNUMBE) DocumentNumber, ISNULL(B.USRDEF05, '') Ncf, A.DUEDATE DueDate, A.DOCDATE DocumentDate, RTRIM(A.CUSTNMBR) CustomerNumber, " +
                    $"RTRIM(A.CUSTNAME) CustomerName, RTRIM(A.CNTCPRSN) ContactPerson, RTRIM(A.TXRGNNUM) TaxRegistrationNumber, " +
                    $"RTRIM(A.ADDRESS1) + RTRIM(A.ADDRESS2) Address, RTRIM(A.CITY) City, RTRIM(A.STATE) State, RTRIM(A.COUNTRY) Country, A.SUBTOTAL Subtotal, A.TAXAMNT TaxAmount, A.DOCAMNT Total, " +
                    $"RTRIM(A.CURNCYID) CurrencyId, A.XCHGRATE ExchangeRate, A.CREATDDT CreatedDate, CASE A.DOCID WHEN 'NOTA DE CREDITO' THEN 4 WHEN 'ND' THEN 3 ELSE 1 END DocumentType " +
                    $"FROM {Helpers.InterCompanyId}.dbo.SOP30200 A " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SOP10106 B ON A.SOPNUMBE = B.SOPNUMBE AND A.SOPTYPE = B.SOPTYPE " +
                    $"WHERE A.BACHNUMB = '{id}'";
            var invoices = _repository.ExecuteQuery<MemInvoiceHead>(sqlQuery).ToList();
            ViewBag.Invoices = invoices;
            return View(batch);
        }

        [HttpPost]
        public JsonResult SaveInvoiceBatchInquiry(string sopNumber, DateTime dueDate, DateTime documentDate, string note, List<MemInvoiceDetail> lines)
        {
            string xStatus = "";
            try
            {
                int count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.SOP10100 WHERE SOPNUMBE = '{sopNumber}'");
                if (count > 0)
                {
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.SOP10100 SET DUEDATE = '{dueDate.ToString("yyyyMMdd")}', DOCDATE = '{documentDate.ToString("yyyyMMdd")}' WHERE SOPNUMBE = '{sopNumber}'");
                    foreach (var item in lines)
                        _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.SOP10200 SET ITEMDESC = '{item.ItemDescription}' " +
                       $"WHERE SOPNUMBE = '{sopNumber}' AND LNITMSEQ = '{item.LineNumber * 16384} '");

                    _repository.ExecuteCommand($"UPDATE A SET A.TXTFIELD = '{note}' FROM {Helpers.InterCompanyId}.dbo.SY03900 A INNER JOIN {Helpers.InterCompanyId}.dbo.SOP10100 B " +
                        $"ON A.NOTEINDX = B.NOTEINDX WHERE B.SOPNUMBE = '{sopNumber}'");
                }
                else
                {
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.SOP30200 SET DUEDATE = '{dueDate.ToString("yyyyMMdd")}', DOCDATE = '{documentDate.ToString("yyyyMMdd")}' WHERE SOPNUMBE = '{sopNumber}'");
                    foreach (var item in lines)
                        _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.SOP30300 SET ITEMDESC = '{item.ItemDescription}' " +
                       $"WHERE SOPNUMBE = '{sopNumber}' AND LNITMSEQ = '{item.LineNumber * 16384} '");

                    _repository.ExecuteCommand($"UPDATE A SET A.TXTFIELD = '{note}' FROM {Helpers.InterCompanyId}.dbo.SY03900 A INNER JOIN {Helpers.InterCompanyId}.dbo.SOP10100 B " +
                        $"ON A.NOTEINDX = B.NOTEINDX WHERE B.SOPNUMBE = '{sopNumber}'");
                }
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetBatchInquiryInvoice(string batchNumber, string sopNumber)
        {
            string xStatus;
            MemInvoiceHead invoice = null;
            try
            {
                int count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.SOP10100 WHERE SOPNUMBE = '{sopNumber}'");
                if (count > 0)
                {
                    var sqlQuery = $"SELECT RTRIM(A.BACHNUMB) BatchNumber, RTRIM(A.SOPNUMBE) SopNumber, ISNULL(D.USRDEF05, '') Ncf, A.DUEDATE DueDate, A.DOCDATE DocumentDate, RTRIM(A.CUSTNMBR) CustomerNumber, " +
                    $"RTRIM(A.CUSTNAME) CustomerName, RTRIM(A.CNTCPRSN) ContactPerson, RTRIM(A.TXRGNNUM) TaxRegistrationNumber, RTRIM(A.ADDRESS1) + RTRIM(A.ADDRESS2) Address, RTRIM(A.CITY) City, RTRIM(A.STATE) State, " +
                    $"RTRIM(A.COUNTRY) Country, A.SUBTOTAL Subtotal, A.TAXAMNT TaxAmount, A.DOCAMNT Total, RTRIM(A.CURNCYID) CurrencyId, A.XCHGRATE ExchangeRate, TXTFIELD Note, " +
                    $"ISNULL(C.DueDate, '1900-01-01') NcfDueDate, ISNULL(C.NcfTypeDesc, '') NcfType, CASE A.DOCID WHEN 'NOTA DE CREDITO' THEN 4 WHEN 'ND' THEN 3 ELSE 1 END DocumentType, " +
                    $"RTRIM(D.USERDEF2) ReferenceInvoice, RTRIM(D.USRDEF03) ReferenceNcf " +
                    $"FROM {Helpers.InterCompanyId}.dbo.SOP10100 A " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SY03900 B ON A.NOTEINDX = B.NOTEINDX " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EFRM30100 C ON A.SOPNUMBE = C.DocumentNumber " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SOP10106 D ON A.SOPNUMBE = D.SOPNUMBE AND A.SOPTYPE = D.SOPTYPE " +
                    $"WHERE A.BACHNUMB = '{batchNumber}' AND A.SOPNUMBE = '{sopNumber}'";
                    invoice = _repository.ExecuteScalarQuery<MemInvoiceHead>(sqlQuery);

                    sqlQuery = $"SELECT (LNITMSEQ / 16384) LineNumber, RTRIM(ITEMNMBR) ItemNumber, RTRIM(ITEMDESC) ItemDescription, ROUND(QUANTITY, 2) Quantity, UNITPRCE Price, TAXAMNT TaxAmount, XTNDPRCE Total " +
                    $"FROM {Helpers.InterCompanyId}.dbo.SOP10200 WHERE SOPNUMBE = '{sopNumber}'";
                    invoice.Details = _repository.ExecuteQuery<MemInvoiceDetail>(sqlQuery).ToList();
                }
                else
                {
                    var sqlQuery = $"SELECT RTRIM(A.BACHNUMB) BatchNumber, RTRIM(A.SOPNUMBE) SopNumber, ISNULL(D.USRDEF05, '') Ncf, A.DUEDATE DueDate, A.DOCDATE DocumentDate, RTRIM(A.CUSTNMBR) CustomerNumber, " +
                    $"RTRIM(A.CUSTNAME) CustomerName, RTRIM(A.CNTCPRSN) ContactPerson, RTRIM(A.TXRGNNUM) TaxRegistrationNumber, RTRIM(A.ADDRESS1) + RTRIM(A.ADDRESS2) Address, RTRIM(A.CITY) City, RTRIM(A.STATE) State, " +
                    $"RTRIM(A.COUNTRY) Country, A.SUBTOTAL Subtotal, A.TAXAMNT TaxAmount, A.DOCAMNT Total, RTRIM(A.CURNCYID) CurrencyId, A.XCHGRATE ExchangeRate, TXTFIELD Note, " +
                    $"ISNULL(C.DueDate, '1900-01-01') NcfDueDate, ISNULL(C.NcfTypeDesc, '') NcfType, CASE A.DOCID WHEN 'NOTA DE CREDITO' THEN 4 WHEN 'ND' THEN 3 ELSE 1 END DocumentType, " +
                    $"RTRIM(D.USERDEF2) ReferenceInvoice, RTRIM(D.USRDEF03) ReferenceNcf " +
                    $"FROM {Helpers.InterCompanyId}.dbo.SOP30200 A " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SY03900 B ON A.NOTEINDX = B.NOTEINDX " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EFRM30100 C ON A.SOPNUMBE = C.DocumentNumber " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SOP10106 D ON A.SOPNUMBE = D.SOPNUMBE AND A.SOPTYPE = D.SOPTYPE " +
                    $"WHERE A.BACHNUMB = '{batchNumber}' AND A.SOPNUMBE = '{sopNumber}'";
                    invoice = _repository.ExecuteScalarQuery<MemInvoiceHead>(sqlQuery);

                    sqlQuery = $"SELECT (LNITMSEQ / 16384) LineNumber, RTRIM(ITEMNMBR) ItemNumber, RTRIM(ITEMDESC) ItemDescription, ROUND(QUANTITY, 2) Quantity, UNITPRCE Price, TAXAMNT TaxAmount, XTNDPRCE Total " +
                    $"FROM {Helpers.InterCompanyId}.dbo.SOP30300 WHERE SOPNUMBE = '{sopNumber}'";
                    invoice.Details = _repository.ExecuteQuery<MemInvoiceDetail>(sqlQuery).ToList();
                }

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, invoice }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateBatchInquiry(string batchNumber, DateTime documentDate, DateTime dueDate, string note)
        {
            string xStatus = "";
            try
            {
                int count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.SOP10100 WHERE BACHNUMB = '{batchNumber}'");
                if (count > 0)
                {
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.SOP10100 SET DUEDATE = '{dueDate.ToString("yyyyMMdd")}', DOCDATE = '{documentDate.ToString("yyyyMMdd")}' " +
                        $"WHERE BACHNUMB = '{batchNumber}'");

                    _repository.ExecuteCommand($"UPDATE A SET A.TXTFIELD = '{note}' FROM {Helpers.InterCompanyId}.dbo.SY03900 A INNER JOIN {Helpers.InterCompanyId}.dbo.SOP10100 B " +
                        $"ON A.NOTEINDX = B.NOTEINDX WHERE B.BACHNUMB = '{batchNumber}'");
                }
                else
                {
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.SOP30200 SET DUEDATE = '{dueDate.ToString("yyyyMMdd")}', DOCDATE = '{documentDate.ToString("yyyyMMdd")}' " +
                        $"WHERE BACHNUMB = '{batchNumber}'");

                    _repository.ExecuteCommand($"UPDATE A SET A.TXTFIELD = '{note}' FROM {Helpers.InterCompanyId}.dbo.SY03900 A INNER JOIN {Helpers.InterCompanyId}.dbo.SOP30200 B " +
                        $"ON A.NOTEINDX = B.NOTEINDX WHERE B.BACHNUMB = '{batchNumber}'");
                }
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetBatchMemInquiry(string batchNumber)
        {
            string xStatus;
            MemBillingHead batch = null;
            try
            {
                int count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.SOP10100 WHERE BACHNUMB = '{batchNumber}'");
                string sqlQuery;
                if (count > 0)
                {
                    sqlQuery = $"SELECT RTRIM(A.BACHNUMB) BatchNumber, '' BatchDescription, CONVERT(FLOAT, 0) BatchTotal, 0 NoOfTransactions, A.DUEDATE DueDate, A.DOCDATE DocumentDate, B.TXTFIELD Note " +
                    $"FROM {Helpers.InterCompanyId}.dbo.SOP10100 A " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SY03900 B ON A.NOTEINDX = B.NOTEINDX " +
                    $"WHERE A.BACHNUMB = '{batchNumber}'";
                }
                else
                {
                    sqlQuery = $"SELECT RTRIM(A.BACHNUMB) BatchNumber, '' BatchDescription, CONVERT(FLOAT, 0) BatchTotal, 0 NoOfTransactions, A.DUEDATE DueDate, A.DOCDATE DocumentDate, B.TXTFIELD Note " +
                     $"FROM {Helpers.InterCompanyId}.dbo.SOP30200 A " +
                     $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SY03900 B ON A.NOTEINDX = B.NOTEINDX " +
                     $"WHERE A.BACHNUMB = '{batchNumber}'";
                }
                batch = _repository.ExecuteScalarQuery<MemBillingHead>(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, batch }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region País

        public ActionResult Country()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "Country"))
                return RedirectToAction("NotPermission", "Home");
            var list = _repository.ExecuteQuery<Lookup>($"SELECT RTRIM(CTRYCODE) Id, RTRIM(CTRYDESC) Descripción, '' DataExtended " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFLC00100 ORDER BY CTRYCODE").ToList();
            return View(list);
        }

        public JsonResult SaveCountry(string CountryDesc)
        {
            string xStatus;

            try
            {
                var countryId = _repository.ExecuteScalarQuery<int?>($"SELECT MAX(CONVERT(INT, CTRYCODE)) FROM {Helpers.InterCompanyId}.dbo.EFLC00100") ?? 0;

                var sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFLC00100 (CTRYCODE, CTRYDESC, CTRYSTTS, LSTUSRID, CREATDDT, MODIFDTE) " +
                    $"VALUES ('{(countryId + 1).ToString().PadLeft(2, '0')}', '{CountryDesc}', 0, '{Account.GetAccount(User.Identity.GetUserName()).UserId}', GETDATE(), GETDATE()) ";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public JsonResult DeleteCountry(string id)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFLC00100 WHERE CTRYCODE = '{id}'";
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

        #region Provincia

        public ActionResult State()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "State"))
                return RedirectToAction("NotPermission", "Home");
            var list = _repository.ExecuteQuery<ItemLookup>($"SELECT RTRIM(A.CTRYCODE) ItemId, RTRIM(B.CTRYDESC) DataExtended, RTRIM(A.PROVCODE) ItemNam, RTRIM(A.PROVDESC) ItemDesc " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFLC00200 A INNER JOIN {Helpers.InterCompanyId}.dbo.EFLC00100 B ON A.CTRYCODE = B.CTRYCODE ORDER BY A.PROVCODE").ToList();

            ViewBag.Countries = _repository.ExecuteQuery<Lookup>($"SELECT RTRIM(CTRYCODE) Id, RTRIM(CTRYDESC) Descripción, '' DataExtended " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFLC00100 ORDER BY CTRYCODE").ToList();
            return View(list);
        }

        public JsonResult SaveState(string CountryId, string StateDesc)
        {
            string xStatus;

            try
            {
                var stateId = _repository.ExecuteScalarQuery<int?>($"SELECT MAX(CONVERT(INT, PROVCODE)) FROM {Helpers.InterCompanyId}.dbo.EFLC00200") ?? 0;

                var sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFLC00200 (CTRYCODE, PROVCODE, PROVDESC, PROVSTTS, LSTUSRID, CREATDDT, MODIFDTE) " +
                    $"VALUES ('{CountryId}', '{(stateId + 1).ToString().PadLeft(2, '0')}', '{StateDesc}', 0, '{Account.GetAccount(User.Identity.GetUserName()).UserId}', GETDATE(), GETDATE()) ";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public JsonResult DeleteState(string countryId, string stateId)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFLC00200 WHERE CTRYCODE = '{countryId}' AND PROVCODE = '{stateId}'";
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

        #region Ciudad

        public ActionResult City()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "City"))
                return RedirectToAction("NotPermission", "Home");
            var list = _repository.ExecuteQuery<ItemLookup>($"SELECT RTRIM(A.CTRYCODE) ItemNam, RTRIM(C.CTRYDESC) DataExtended, RTRIM(A.PROVCODE) Posted, RTRIM(B.PROVDESC) DataPlus, " +
               $"RTRIM(A.CITYCODE) ItemId, RTRIM(A.CITYDESC) ItemDesc " +
               $"FROM {Helpers.InterCompanyId}.dbo.EFLC00300 A INNER JOIN {Helpers.InterCompanyId}.dbo.EFLC00200 B ON A.CTRYCODE = B.CTRYCODE AND A.PROVCODE = B.PROVCODE " +
               $"INNER JOIN {Helpers.InterCompanyId}.dbo.EFLC00100 C ON B.CTRYCODE = C.CTRYCODE ORDER BY A.PROVCODE").ToList();

            ViewBag.Countries = _repository.ExecuteQuery<Lookup>($"SELECT RTRIM(CTRYCODE) Id, RTRIM(CTRYDESC) Descripción, '' DataExtended " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFLC00100 ORDER BY CTRYCODE").ToList();
            return View(list);
        }

        public JsonResult SaveCity(string CountryId, string StateId, string CityDesc)
        {
            string xStatus;

            try
            {
                var cityId = _repository.ExecuteScalarQuery<int?>($"SELECT MAX(CONVERT(INT, CITYCODE)) FROM {Helpers.InterCompanyId}.dbo.EFLC00300") ?? 0;
                var sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFLC00300 (CTRYCODE, PROVCODE, CITYCODE, CITYDESC, CITYSTTS, LSTUSRID, CREATDDT, MODIFDTE) " +
                    $"VALUES ('{CountryId}', '{StateId}', '{(cityId + 1).ToString().PadLeft(2, '0')}', '{CityDesc}', 0, '{Account.GetAccount(User.Identity.GetUserName()).UserId}', GETDATE(), GETDATE()) ";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public JsonResult DeleteCity(string countryId, string stateId, string cityId)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFLC00300 WHERE CTRYCODE = '{countryId}' AND PROVCODE = '{stateId}' AND CITYCODE = '{cityId}'";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public JsonResult GetStates(string countryId)
        {
            string xStatus;
            List<Lookup> registros = null;
            try
            {
                registros = _repository.ExecuteQuery<Lookup>($"SELECT PROVCODE Id, PROVDESC Descripción, '' DataExtended FROM {Helpers.InterCompanyId}.dbo.EFLC00200 " +
                    $"WHERE CTRYCODE = '{countryId}' ORDER BY PROVCODE").ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(registros, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Reports

        public ActionResult BillingReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "BillingReport"))
                return RedirectToAction("NotPermission", "Home");
            return Redirect("http://tccsvr02/Reports_GP/Pages/Report.aspx?ItemPath=%2fSEABO%2fSales%2fControl+facturas+tcc");
        }

        public ActionResult CustomerDataReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "CustomerDataReport"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult CustomerDataReport(string filterFrom, string filterTo)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "CustomerDataReport.pdf",
                    string.Format("INTRANET.dbo.CustomerDataReport '{0}','{1}','{2}'", Helpers.InterCompanyId, filterFrom, filterTo), 30, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult CustomerDataInquiry(string filterFrom, string filterTo)
        {
            string xStatus;
            var list = new List<CustomerData>();
            try
            {
                xStatus = "OK";
                var sqlQuery = "SELECT CUSTNMBR CustomerId, CUSTNAME CustomerName, CUSTCLAS CustomerClass, CNTCPRSN Contact, RTRIM(ADDRESS1) + ' ' + RTRIM(ADDRESS2) CustomerAddress, " +
                    "CITY City,  ISNULL(B.DOCDESCR, '') NcfType, TXRGNNUM RNC, CURNCYID CurrencyId " +
                    $"FROM {Helpers.InterCompanyId}.dbo.RM00101 A LEFT JOIN {Helpers.InterCompanyId}.dbo.EFNCF40101 B " +
                    $"ON A.COMMENT1 = B.DOCNUMBR ";
                if (!string.IsNullOrEmpty(filterFrom))
                    sqlQuery += $"WHERE A.CUSTCLAS BETWEEN '{filterFrom}' AND '{filterTo}'";

                list = _repository.ExecuteQuery<CustomerData>(sqlQuery).ToList();
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = list } };
        }

        public ActionResult CustomerTransactionInquiry()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "CustomerDataReport"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult CustomerTransactionInquiry(string filterFrom)
        {
            string xStatus;
            var list = new List<MemInvoiceHead>();
            try
            {
                xStatus = "OK";
                var sqlQuery = $"SELECT RTRIM(A.BACHNUMB) BatchNumber, RTRIM(A.SOPNUMBE) SopNumber, RTRIM(A.SOPNUMBE) DocumentNumber, ISNULL(B.USRDEF05, '') Ncf, A.DUEDATE DueDate, A.DOCDATE DocumentDate, RTRIM(A.CUSTNMBR) CustomerNumber, " +
                    $"RTRIM(A.CUSTNAME) CustomerName, RTRIM(A.CNTCPRSN) ContactPerson, RTRIM(A.TXRGNNUM) TaxRegistrationNumber, " +
                    $"RTRIM(A.ADDRESS1) + RTRIM(A.ADDRESS2) Address, RTRIM(A.CITY) City, RTRIM(A.STATE) State, RTRIM(A.COUNTRY) Country, A.SUBTOTAL Subtotal, A.TAXAMNT TaxAmount, A.DOCAMNT Total, " +
                    $"RTRIM(A.CURNCYID) CurrencyId, A.XCHGRATE ExchangeRate, A.CREATDDT CreatedDate, CASE A.DOCID WHEN 'NOTA DE CREDITO' THEN 4 WHEN 'ND' THEN 3 ELSE 1 END DocumentType " +
                    $"FROM {Helpers.InterCompanyId}.dbo.SOP10100 A " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SOP10106 B ON A.SOPNUMBE = B.SOPNUMBE AND A.SOPTYPE = B.SOPTYPE ";
                if (!string.IsNullOrEmpty(filterFrom))
                    sqlQuery += $"WHERE A.CUSTNMBR = '{filterFrom}' ";
                sqlQuery += $"UNION ALL ";
                sqlQuery += $"SELECT RTRIM(A.BACHNUMB) BatchNumber, RTRIM(A.SOPNUMBE) SopNumber, RTRIM(A.SOPNUMBE) DocumentNumber, ISNULL(B.USRDEF05, '') Ncf, A.DUEDATE DueDate, A.DOCDATE DocumentDate, RTRIM(A.CUSTNMBR) CustomerNumber, " +
                $"RTRIM(A.CUSTNAME) CustomerName, RTRIM(A.CNTCPRSN) ContactPerson, RTRIM(A.TXRGNNUM) TaxRegistrationNumber, " +
                $"RTRIM(A.ADDRESS1) + RTRIM(A.ADDRESS2) Address, RTRIM(A.CITY) City, RTRIM(A.STATE) State, RTRIM(A.COUNTRY) Country, A.SUBTOTAL Subtotal, A.TAXAMNT TaxAmount, A.DOCAMNT Total, " +
                $"RTRIM(A.CURNCYID) CurrencyId, A.XCHGRATE ExchangeRate, A.CREATDDT CreatedDate, CASE A.DOCID WHEN 'NOTA DE CREDITO' THEN 4 WHEN 'ND' THEN 3 ELSE 1 END DocumentType " +
                $"FROM {Helpers.InterCompanyId}.dbo.SOP30200 A " +
                $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SOP10106 B ON A.SOPNUMBE = B.SOPNUMBE AND A.SOPTYPE = B.SOPTYPE ";
                if (!string.IsNullOrEmpty(filterFrom))
                    sqlQuery += $"WHERE A.CUSTNMBR = '{filterFrom}' ";
                list = _repository.ExecuteQuery<MemInvoiceHead>(sqlQuery).ToList();
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = list } };
        }

        public ActionResult SalesTransDetailReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "SalesTransDetail"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult SalesTransDetailReport(string fromDate, string toDate, string fromCustomer, string toCustomer, string fromBatch, string toBatch, string fromClass, string toClass)
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
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "SalesTransDetailReport.pdf",
                    string.Format("INTRANET.dbo.SalesTransDetailReport '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}'", Helpers.InterCompanyId, fromDate, toDate, fromCustomer, toCustomer, fromBatch, toBatch, fromClass, toClass),
                    31, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public ActionResult SalesTransGeneralLedgerReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "SalesTransGeneralLedger"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult SalesTransGeneralLedgerReport(string fromDate, string toDate, string fromCustomer, string toCustomer, string fromBatch, string toBatch, string fromClass, string toClass)
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
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Billing", "FiscalSales"))
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
                var taxRegisterNumber = _repository.ExecuteScalarQuery<string>($"SELECT TAXREGTN FROM {Helpers.InterCompanyId}.dbo.EFRM40101");
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

        #endregion

        public JsonResult VerifyMemBillingNcfAvailability(string batchNumber, int preInvoice, int batch, string sopNumber = "", int billingType = 10)
        {
            string xStatus = "";
            List<Lookup> registros = null;
            try
            {
                var customerNumber = "";
                if (!string.IsNullOrEmpty(sopNumber))
                    customerNumber = _repository.ExecuteScalarQuery<string>($"SELECT CustomerNumber FROM {Helpers.InterCompanyId}.dbo.EFSOP20100 " +
                        $"WHERE BatchNumber = '{batchNumber}' AND SopNumber = '{sopNumber}' AND BillingType = {billingType}");
                registros = _repository.ExecuteQuery<Lookup>(
                    $"INTRANET.dbo.VerifyNcfAvailabilityMemBillingTransaction '{Helpers.InterCompanyId}','{batchNumber}','{preInvoice}','{batch}','{customerNumber}',{billingType}").ToList();
                if (registros.Count > 0)
                {
                    xStatus += "No existen suficientes numeros de comprobantes fiscal para la transacción o transacciones a procesar por favor verificar ";
                    xStatus += "\n";
                    xStatus += "Las transacciones y el tipo de NCF sin numero de comprobante fiscal son: ";
                    var count = 1;
                    foreach (var item in registros)
                    {
                        xStatus += "\n";
                        xStatus += count + ": " + item.Id + " - " + item.DataExtended;
                        count++;
                    }
                    xStatus += "\n";
                    xStatus += "Debe de ir a la configuración de comprobantes fiscales y agregar una secuencia valida para el tipo de NCF para poder continuar con el proceso, Gracias";
                    xStatus += "\n";
                    if (preInvoice == 1)
                        xStatus += "Desea continuar con el proceso obviando este mensaje, esto no le asignara NCF a las transacciones arriba indicadas ?";
                }
                else
                    xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }
        private void SendMailNcfNotification()
        {
            var sqlQuery = $"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EFNCF40101 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.EFNCF40102 B ON A.DOCNUMBR = B.HDDOCNUM " +
                $"WHERE (B.TONUMBER - B.NEXTNUMB) <= B.ALERTNUM AND (B.TONUMBER - B.NEXTNUMB) > 0";
            var count = _repository.ExecuteScalarQuery<int?>(sqlQuery) ?? 0;

            if (count > 0)
            {
                string emails = "";
                _repository.ExecuteQuery<string>($"SELECT Email FROM {Helpers.InterCompanyId}.dbo.EFRM40301").ToList().ForEach(p => { emails += p + ";"; });
                if (!string.IsNullOrEmpty(emails))
                    _repository.ExecuteCommand($"INTRANET.dbo.SendMailNcfNotification '{Helpers.InterCompanyId}', '{emails}'");
            }
        }
    }
}