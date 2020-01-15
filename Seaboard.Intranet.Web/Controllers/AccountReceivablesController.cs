using Seaboard.Intranet.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data.Repository;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;
using System.Threading;


namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class AccountReceivablesController : Controller
    {
        private readonly GenericRepository _repository;

        public AccountReceivablesController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        #region Transaction Entry

        public ActionResult TransIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables", "TransEntry"))
                return RedirectToAction("NotPermission", "Home");

            var registros = _repository.ExecuteQuery<ItemLookup>($"INTRANET.dbo.GetAccountReceivablesBatchTransEntry '{Helpers.InterCompanyId}'").ToList();
            return View(registros);
        }

        public ActionResult TransEntry(string batchNumber = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables", "TransEntry"))
                return RedirectToAction("NotPermission", "Home");

            ViewBag.BatchNumber = batchNumber;
            return View();
        }

        public ActionResult TransInquiry(string batchNumber)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables","TransEntry"))
                return RedirectToAction("NotPermission", "Home");
            ViewBag.BatchNumber = batchNumber;
            return View();
        }

        public JsonResult GetTransactionsFile(string mes, string lote, string descripción, string resolución)
        {
            var xStatus = "";
            var xRegistros = new List<MemDebtData>();
            try
            {
                var lotes = _repository.ExecuteScalarQuery<int>(
                    "SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.SESOP30302 WHERE MONTH(DOCDATE) = MONTH('" +
                    DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd") + "') AND YEAR(DOCDATE) = YEAR('" +
                    DateTime.ParseExact(mes, "dd/MM/yyyy", null).ToString("yyyyMMdd") + "')");
                if (lotes > 0)
                    xStatus = "Ya se ha procesado este mes por favor buscar los datos por id de lote";
                else
                {
                    var lista = OfficeLogic.MemDebtProcess(DateTime.ParseExact(mes, "dd/MM/yyyy", null), ref xStatus);

                    if (lista.Count > 0)
                    {
                        foreach (var item in lista)
                        {
                            _repository.ExecuteCommand(
                                string.Format(
                                    "INTRANET.dbo.AccountReceivablesTransEntry '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10:yyyyMMdd}','{11}','{12}','{13}','{14}'",
                                    Helpers.InterCompanyId, "SEABOARD", item.Suplidor,
                                    item.Producto1.ToString(new CultureInfo("en-US")),
                                    item.Producto2.ToString(new CultureInfo("en-US")),
                                    item.Producto3.ToString(new CultureInfo("en-US")),
                                    item.Producto4.ToString(new CultureInfo("en-US")),
                                    item.Producto5.ToString(new CultureInfo("en-US")),
                                    item.Producto6.ToString(new CultureInfo("en-US")),
                                    item.Producto7.ToString(new CultureInfo("en-US")),
                                    new DateTime(DateTime.ParseExact(mes, "dd/MM/yyyy", null).Year, DateTime.ParseExact(mes, "dd/MM/yyyy", null).Month, 1),
                                    lote, descripción, resolución, Account.GetAccount(User.Identity.GetUserName()).UserId));
                        }

                        xRegistros = lista;
                        xStatus = "OK";
                    }
                    else
                        xStatus = "No se encontraron transacciones para este periodo";
                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBatchTransEntryDetail(string batchId)
        {
            string xStatus;
            var xRegistros = new List<MemDebtData>();
            try
            {
                xRegistros =_repository.ExecuteQuery<MemDebtData>($"INTRANET.dbo.GetAccountReceivablesBatchTransEntryDetail '{Helpers.InterCompanyId}','{batchId}'").ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBatchTransEntryPostedDetail(string batchId)
        {
            string xStatus;
            var xRegistros = new List<MemDebtData>();
            try
            {
                xRegistros =_repository.ExecuteQuery<MemDebtData>(
                    "SELECT [ACREEDOR] Acreedor, [SUPLIDOR] Suplidor, CONVERT(FLOAT, [ENERGIA]) Producto1, CONVERT(FLOAT, [POTENCIA]) Producto2, " +
                    "CONVERT(FLOAT, [DC]) Producto3, CONVERT(FLOAT, [PRODUCTO4]) Producto4, CONVERT(FLOAT, [PRODUCTO5]) Producto5, " +
                    "CONVERT(FLOAT, [PRODUCTO6]) Producto6, CONVERT(FLOAT, [PRODUCTO7]) Producto7 " +
                    "FROM " + Helpers.InterCompanyId + ".dbo.SESOP30302 " +
                    "WHERE BACHNMBR = '" + batchId + "'").ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetTransEntry(string batchId)
        {
            string xStatus;
            var xRegistros = new ItemLookup();
            try
            {
                xRegistros = _repository.ExecuteScalarQuery<ItemLookup>(
                    "SELECT TOP 1 FORMAT(DOCDATE, 'MM/yyyy') ItemDesc , BACHNMBR ItemId, BACHCMNT ItemNam, RESOLUID DataExtended, '' Posted " +
                    "FROM " + Helpers.InterCompanyId + ".dbo.SESOP30302 " +
                    "WHERE BACHNMBR = '" + batchId + "'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult GetCustomerStatus(string customerId = "", string itemNumber = "")
        {
            try
            {
                var sqlQuery = "SELECT COUNT(*) " + "FROM " + Helpers.InterCompanyId + ".dbo.SESY01500 " + "WHERE CLIENTEID = '" + customerId + "'";
                var xCount = _repository.ExecuteScalarQuery<int>(sqlQuery);
                return Json(xCount, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(0);
            }
        }

        [HttpPost]
        public JsonResult PostBatchTransEntryDetail(string batchId, string customerId, string fechaDocumento, string resolución)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand(string.Format(
                    "INTRANET.dbo.PostAccountReceivablesBatchTransEntry '{0}','{1}','{2}','{3:yyyyMMdd}','{4}','{5}'",
                    Helpers.InterCompanyId, batchId, customerId,
                    DateTime.ParseExact(fechaDocumento, "dd/MM/yyyy", null), resolución,
                    Account.GetAccount(User.Identity.GetUserName()).UserId));
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(xStatus, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CloseBatchTransEntry(string batchId, string customerId)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand(
                    string.Format("INTRANET.dbo.CloseAccountReceivablesBatchTransEntry '{0}','{1}','{2}','{3}'",
                        Helpers.InterCompanyId, batchId, customerId,
                        Account.GetAccount(User.Identity.GetUserName()).UserId));
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(xStatus, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteBatchTransEntry(string batchId)
        {
            var xStatus = "";
            var xRegistros = new List<MemDebtData>();
            try
            {
                var lotes = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                                ".dbo.SESOP30301 WHERE BACHNMBR = '" + batchId + "'");
                if (lotes > 0)
                {
                    xStatus = "Ya existen transacciones sometidas de este lote no se puede eliminar";
                }
                else
                {
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId +
                                               ".dbo.SESOP30302 WHERE BACHNMBR = '" + batchId + "'");
                    _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId +
                                               ".dbo.SESOP30301 WHERE BACHNMBR = '" + batchId + "'");
                    xStatus = "OK";
                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult PrintTransEntry(string date)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "AccountReceivablesMemTrans.pdf",
                    $"INTRANET.dbo.AccountReceivablesMemTransReport '{Helpers.InterCompanyId}','{DateTime.ParseExact(date, "dd/MM/yyyy", null):yyyyMMdd}'", 16, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        #endregion

        #region Transaction Create

        public ActionResult TransCreateIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "TransCreate"))
            {
                return RedirectToAction("NotPermission", "Home");
            }


            var registros = _repository
                .ExecuteQuery<ItemLookup>(
                    $"INTRANET.dbo.GetAccountReceivablesBatchTransCreate '{Helpers.InterCompanyId}'").ToList();

            return View(registros);
        }

        public ActionResult TransCreate(string batchNumber)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "TransCreate"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            ViewBag.BatchNumber = batchNumber;
            return View();
        }

        public ActionResult TransCreateInquiry(string batchNumber)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "TransCreate"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            ViewBag.BatchNumber = batchNumber;
            return View();
        }

        [OutputCache(Duration = 0)]
        public JsonResult GetBatchTransCreateDetail(string batchId)
        {
            string xStatus;
            var xRegistros = new List<MemDebtDataTrans>();
            try
            {
                xRegistros =
                    _repository.ExecuteQuery<MemDebtDataTrans>(
                            $"INTRANET.dbo.GetAccountReceivablesBatchTransCreateDetail '{Helpers.InterCompanyId}','{batchId}'")
                        .ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveBatchTransCreateDetail(string batchId = "", string customerId = "", string vendorInvoiceNumber = "",
            string ncf = "", int tipoGastos = 8, string fechaDocumento = "")
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand(
                    $"INTRANET.dbo.SaveAccountReceivablesBatchTransCreate '{Helpers.InterCompanyId}','{batchId}','{customerId}','{vendorInvoiceNumber}','{ncf}','{tipoGastos}','{DateTime.ParseExact(fechaDocumento, "dd/MM/yyyy", null):yyyyMMdd}','{Account.GetAccount(User.Identity.GetUserName()).UserId}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(xStatus, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult PostBatchTransCreateDetail(Lookup[] records)
        {
            string xStatus;
            var message = "";
            try
            {
                ProcessReceipt(records);

                foreach (var item in records)
                {
                    UpdateDataTransaction(item.Id, item.Descripción);
                }

                ServiceContract service = new ServiceContract();
                service.PostBatch("Rcvg Trx Entry", records.FirstOrDefault()?.Id, ref message);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(xStatus, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetTransCreate(string batchId)
        {
            string xStatus;
            var xRegistros = new ItemLookup();
            try
            {
                xRegistros = _repository.ExecuteScalarQuery<ItemLookup>(
                    "SELECT TOP 1 FORMAT(CASE CONVERT(DATE, B.TRANSDATE) WHEN '1900-01-01' THEN B.DOCDATE ELSE B.TRANSDATE END, 'dd/MM/yyyy') ItemDesc, " +
                    "A.BACHNMBR ItemId, B.BACHCMNT ItemNam, A.RESOLUID DataExtended, '' Posted " +
                    "FROM " + Helpers.InterCompanyId + ".dbo.SESOP30301 A " +
                    "INNER JOIN " + Helpers.InterCompanyId + ".dbo.SESOP30302 B " +
                    "ON A.BACHNMBR = B.BACHNMBR AND A.VENDORID = B.SUPLIDOR " +
                    "WHERE A.BACHNMBR = '" + batchId + "' AND LEN(A.DOCNUMBE) > 0");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBatchTransCreatePostedDetail(string batchId)
        {
            string xStatus;
            var xRegistros = new List<MemDebtDataTrans>();
            try
            {
                xRegistros = _repository.ExecuteQuery<MemDebtDataTrans>(
                        "SELECT [A.SUPLIDOR] Suplidor, CONVERT(FLOAT, [A.ENERGIA] + [A.POTENCIA] + [A.DC] + [A.PRODUCTO4] + [A.PRODUCTO5] + [A.PRODUCTO6] + [A.PRODUCTO7]) Monto, " +
                        "CASE LEN(A.VENDINVC) WHEN 0 THEN '''' ELSE A.VENDINVC END NumeroProveedor, CASE LEN(A.NCF) WHEN 0 THEN '''' ELSE A.NCF END Ncf, A.TIPOGASTOS TipoGastos" +
                        "FROM " + Helpers.InterCompanyId + ".dbo.SESOP30302 A " +
                        "INNER JOIN " + Helpers.InterCompanyId + ".dbo.SESOP30301 B " +
                        "ON A.BACHNMBR = B.BACHNMBR AND A.SUPLIDOR = B.SUPLIDOR " +
                        "WHERE BACHNMBR = '" + batchId + "' AND LEN(B.DOCNUMBE) > 0").ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        private void ProcessReceipt(Lookup[] records)
        {
            var billing = new ServiceContract();
            var receiptArray = new List<GpPurchaseReceipt>();

            var aConfiguration = _repository.ExecuteScalarQuery<MemConfiguration>(
                "SELECT RUTA, ACREEDOR, CONVERT(INT, HOJA) Hoja, CLIENTEID Suplidor, PRODUCTO1GP Producto1, PRODUCTO2GP Producto2, PRODUCTO3GP Producto3, " +
                "PRODUCTO4GP Producto4, PRODUCTO5GP Producto5, PRODUCTO6GP Producto6, PRODUCTO7GP Producto7 FROM " +
                Helpers.InterCompanyId + ".dbo.SESY01502 WHERE INTERID = '" + Helpers.InterCompanyId + "'");

            foreach (var item in records)
            {
                var receipt = new GpPurchaseReceipt { Lines = new List<GpPurchaseReceiptLine>() };
                var aHeader = _repository.ExecuteScalarQuery<Lookup>(string.Format(
                            "SELECT VENDORGP Id, DOCNUMBE Descripción, CONVERT(NVARCHAR(10), DOCDATE, 103) DataExtended FROM " +
                            Helpers.InterCompanyId + ".dbo.SESOP30301 WHERE BACHNMBR = '{0}' AND VENDORID = '{1}'", item.Id, item.Descripción));

                var aDetail = _repository.ExecuteScalarQuery<MemDebtData>(string.Format(
                            "SELECT '' Acreedor, VENDINVC Suplidor, CONVERT(FLOAT, ENERGIA) Producto1, CONVERT(FLOAT, POTENCIA) Producto2, CONVERT(FLOAT, DC) Producto3, CONVERT(FLOAT, PRODUCTO4) Producto4, " +
                            "CONVERT(FLOAT, PRODUCTO5) Producto5, CONVERT(FLOAT, PRODUCTO6) Producto6, CONVERT(FLOAT, PRODUCTO7) Producto7 FROM " +
                            Helpers.InterCompanyId + ".dbo.SESOP30302 WHERE BACHNMBR = '{0}' AND SUPLIDOR = '{1}'", item.Id, item.Descripción));

                receipt.VendorId = aHeader.Id;
                receipt.BatchNumber = item.Id;
                receipt.DocumentNumber = aHeader.Descripción;
                receipt.CurrencyId = "RD$";
                receipt.DocumentDate = DateTime.ParseExact(item.DataExtended, "dd/MM/yyyy", null);
                receipt.InvoiceId = aDetail.Suplidor;

                var producto1 = Convert.ToDecimal(aDetail.Producto1);
                var producto2 = Convert.ToDecimal(aDetail.Producto2);
                var producto3 = Convert.ToDecimal(aDetail.Producto3);
                var producto4 = Convert.ToDecimal(aDetail.Producto4);
                var producto5 = Convert.ToDecimal(aDetail.Producto5);
                var producto6 = Convert.ToDecimal(aDetail.Producto6);
                var producto7 = Convert.ToDecimal(aDetail.Producto7);

                if (producto1 > 0)
                {
                    receipt.Lines.Add(new GpPurchaseReceiptLine
                    {
                        ItemDescription = "",
                        ItemId = aConfiguration.Producto1,
                        Quantity = 1,
                        UnitId = "UND",
                        UnitPrice = producto1,
                        Warehouse = "COMPRAS"
                    });
                }
                if (producto2 > 0)
                {
                    receipt.Lines.Add(new GpPurchaseReceiptLine
                    {
                        ItemDescription = "",
                        ItemId = aConfiguration.Producto2,
                        Quantity = 1,
                        UnitId = "UND",
                        UnitPrice = producto2,
                        Warehouse = "COMPRAS"
                    });
                }
                if (producto3 > 0)
                {
                    receipt.Lines.Add(new GpPurchaseReceiptLine
                    {
                        ItemDescription = "",
                        ItemId = aConfiguration.Producto3,
                        Quantity = 1,
                        UnitId = "UND",
                        UnitPrice = producto3,
                        Warehouse = "COMPRAS"
                    });
                }
                if (producto4 > 0)
                {
                    receipt.Lines.Add(new GpPurchaseReceiptLine
                    {
                        ItemDescription = "",
                        ItemId = aConfiguration.Producto4,
                        Quantity = 1,
                        UnitId = "UND",
                        UnitPrice = producto4,
                        Warehouse = "COMPRAS"
                    });
                }
                if (producto5 > 0)
                {
                    receipt.Lines.Add(new GpPurchaseReceiptLine
                    {
                        ItemDescription = "",
                        ItemId = aConfiguration.Producto5,
                        Quantity = 1,
                        UnitId = "UND",
                        UnitPrice = producto5,
                        Warehouse = "COMPRAS"
                    });
                }
                if (producto6 > 0)
                {
                    receipt.Lines.Add(new GpPurchaseReceiptLine
                    {
                        ItemDescription = "",
                        ItemId = aConfiguration.Producto6,
                        Quantity = 1,
                        UnitId = "UND",
                        UnitPrice = producto6,
                        Warehouse = "COMPRAS"
                    });
                }
                if (producto7 > 0)
                {
                    receipt.Lines.Add(new GpPurchaseReceiptLine
                    {
                        ItemDescription = "",
                        ItemId = aConfiguration.Producto7,
                        Quantity = 1,
                        UnitId = "UND",
                        UnitPrice = producto7,
                        Warehouse = "COMPRAS"
                    });
                }

                receiptArray.Add(receipt);
            }

            billing.ProcessPurchaseReceipt(receiptArray, Request.Cookies["UserAccount"]?["username"],
                Request.Cookies["UserAccount"]?["password"]);

            var bachComment = _repository.ExecuteScalarQuery<string>(
                string.Format(
                    "SELECT BACHCMNT FROM " + Helpers.InterCompanyId +
                    ".dbo.SESOP30302 WHERE BACHNMBR = '{0}'", records.FirstOrDefault()?.Id));

            _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHCOMNT = '" +
                                       bachComment + "' WHERE BACHNUMB = '" + records.FirstOrDefault()?.Id +
                                       "' AND SERIES = 4 AND BCHSOURC = 'Rcvg Trx Entry'");
        }

        private void UpdateDataTransaction(string batchId, string customerId)
        {
            var aHeader =
                _repository.ExecuteScalarQuery<Lookup>(
                    string.Format(
                        "SELECT VENDORGP Id, DOCNUMBE Descripción, CONVERT(NVARCHAR(10), DOCDATE) DataExtended FROM " +
                        Helpers.InterCompanyId + ".dbo.SESOP30301 WHERE BACHNMBR = '{0}' AND VENDORID = '{1}'", batchId,
                        customerId));

            var aDetail =
                _repository.ExecuteScalarQuery<MemDebtDataTrans>(
                    string.Format(
                        "SELECT '' Acreedor, '' Suplidor, CONVERT(float, 0) Monto, '' NumeroProveedor, NCF Ncf, TIPOGASTOS TipoGastos " +
                        "FROM " + Helpers.InterCompanyId +
                        ".dbo.SESOP30302 WHERE BACHNMBR = '{0}' AND SUPLIDOR = '{1}'", batchId,
                        customerId));

            _repository.ExecuteCommand(
                string.Format(
                    "INTRANET.dbo.CloseAccountReceivablesBatchTransEntry '{0}','{1}','{2}','{3}'",
                    Helpers.InterCompanyId, batchId, customerId,
                    Account.GetAccount(User.Identity.GetUserName()).UserId));

            _repository.ExecuteCommand(
                string.Format(
                    "INTRANET.dbo.UpdatePurchaseReceipt '{0}','{1}'",
                    Helpers.InterCompanyId, aHeader.Descripción));

            _repository.ExecuteCommand(
                string.Format(
                    "INTRANET.dbo.InsertNCFSystemTable '{0}','{1}','{2}','{3}','{4}'",
                    Helpers.InterCompanyId, aHeader.Id, aHeader.Descripción, aDetail.Ncf, aDetail.TipoGastos));
        }

        #endregion

        #region Net Trans

        public ActionResult NetIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables", "NetTrans"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            var sqlQuery =
                "SELECT BACHNMBR BatchNumber, BACHDESC Description, CUSTNMBR CustomerId, CUSTNAME CustomerName, VENDORID VendorId, " +
                "VENDNAME VendorName, CURRCYID CurrencyId, CONVERT(NVARCHAR(10), DOCDATE, 103) DocumentDate, CONVERT(NVARCHAR(10), CLOSDATE, 103) CloseDate, NOTE Note, POSTED Posted " +
                "FROM " + Helpers.InterCompanyId + ".dbo.SESOP30501 ORDER BY BACHNMBR";

            var netTrans = _repository.ExecuteQuery<MemNetTrans>(sqlQuery).ToList();
            return View(netTrans);
        }

        public ActionResult NetTrans()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "NetTrans"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        public ActionResult NetEdit(string batchNumber)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "NetTrans"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            ViewBag.BatchNumber = batchNumber;
            return View();
        }

        public ActionResult NetInquiry(string batchNumber)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "NetTrans"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            ViewBag.BatchNumber = batchNumber;
            return View();
        }

        public JsonResult GetNetTransDetail(string batchId)
        {
            string xStatus;
            var xRegistros = new List<MemNetTransDetail>();
            try
            {
                xRegistros =
                    _repository.ExecuteQuery<MemNetTransDetail>(string.Format(
                        "SELECT BACHNMBR BatchNumber, [CHECK] [Check], MODULE Module, CUSTVEND CustomerVendor, " +
                        "DOCNUMBE DocumentNumber, CONVERT(NVARCHAR(10), DOCDATE, 103) DocumentDate, CONVERT(INT, DOCTYPE) DocumentType, " +
                        "CURRCYID CurrencyId, CURRAMNT CurrentAmount, APPLAMNT AppliedAmount " +
                        "FROM " + Helpers.InterCompanyId + ".dbo.SESOP30502 " +
                        "WHERE BACHNMBR = '{0}'", batchId)).ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRelationtCustVend(int tipo, string id)
        {
            string xStatus;
            var xRegistros = new Lookup();
            var xCount = 0;
            var xCurrency = "";
            try
            {
                string alterId;
                if (tipo == 0)
                {
                    xCount =
                        _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                            ".dbo.PA00001 WHERE VENDORID = '" + id + "'");
                    if (xCount > 0)
                    {
                        alterId = _repository.ExecuteScalarQuery<string>(
                            "SELECT RTRIM(CUSTNMBR) FROM " + Helpers.InterCompanyId +
                            ".dbo.PA00001 WHERE VENDORID = '" + id + "'");
                        xCurrency = _repository.ExecuteScalarQuery<string>(
                            "SELECT RTRIM(CURNCYID) FROM " + Helpers.InterCompanyId +
                            ".dbo.PA00001 WHERE VENDORID = '" + id + "'");
                        xRegistros = _repository.ExecuteScalarQuery<Lookup>(
                            "SELECT RTRIM(CUSTNMBR) Id, RTRIM(CUSTNAME) Descripción FROM " + Helpers.InterCompanyId +
                            ".dbo.RM00101 WHERE CUSTNMBR = '" + alterId + "'");
                    }
                }
                else
                {
                    xCount =
                        _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                            ".dbo.PA00001 WHERE CUSTNMBR = '" + id + "'");
                    if (xCount > 0)
                    {
                        alterId = _repository.ExecuteScalarQuery<string>(
                            "SELECT RTRIM(VENDORID) FROM " + Helpers.InterCompanyId +
                            ".dbo.PA00001 WHERE CUSTNMBR = '" + id + "'");
                        xCurrency = _repository.ExecuteScalarQuery<string>(
                            "SELECT RTRIM(CURNCYID) FROM " + Helpers.InterCompanyId +
                            ".dbo.PA00001 WHERE CUSTNMBR = '" + id + "'");
                        xRegistros = _repository.ExecuteScalarQuery<Lookup>(
                            "SELECT RTRIM(VENDORID) Id, RTRIM(VENDNAME) Descripción FROM " + Helpers.InterCompanyId +
                            ".dbo.PM00200 WHERE VENDORID = '" + alterId + "'");
                    }
                }

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros, count = xCount, currency = xCurrency },
                JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetNetTransactions(string customerId, string vendorId, string date)
        {
            string xStatus;
            var xRegistros = new List<MemNetTransDetail>();
            try
            {
                xRegistros =
                    _repository.ExecuteQuery<MemNetTransDetail>(
                            $"INTRANET.dbo.GetNetTransactions '{Helpers.InterCompanyId}','{Account.GetAccount(User.Identity.GetUserName()).UserId}'," +
                            $"'{customerId}','','{vendorId}','{DateTime.ParseExact(date, "dd/MM/yyyy", null):yyyyMMdd}'")
                        .ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetNetTrans(string batchId)
        {
            string xStatus;
            var xRegistro = new MemNetTrans();
            try
            {
                var sqlQuery =
                    "SELECT BACHNMBR BatchNumber, BACHDESC Description, CUSTNMBR CustomerId, CUSTNAME CustomerName, VENDORID VendorId, " +
                    "VENDNAME VendorName, CURRCYID CurrencyId, CONVERT(NVARCHAR(10), DOCDATE, 103) DocumentDate, CONVERT(NVARCHAR(10), CLOSDATE, 103) CloseDate, NOTE Note, RESOLUID Resolution, POSTED Posted " +
                    "FROM " + Helpers.InterCompanyId + ".dbo.SESOP30501 " +
                    "WHERE BACHNMBR = '" + batchId + "'";
                xRegistro = _repository.ExecuteScalarQuery<MemNetTrans>(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = xRegistro }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteNetTrans(string batchId)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand("DELETE FROM " + Helpers.InterCompanyId +
                                           ".dbo.SESOP30501 WHERE BACHNMBR = '" + batchId + "'");
                _repository.ExecuteCommand("DELETE FROM " + Helpers.InterCompanyId +
                                           ".dbo.SESOP30502 WHERE BACHNMBR = '" + batchId + "'");

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(xStatus, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveNetTrans(string batchId, string descripción, string customerId, string customerName,
            string vendorId, string vendorName, string currency, string fechaDocumento, string fechaCorte, string note, string resolución)
        {
            string xStatus;
            try
            {
                var aCustomerName = _repository.ExecuteScalarQuery<string>(
                    "SELECT CUSTNAME FROM " + Helpers.InterCompanyId + ".dbo.RM00101 WHERE CUSTNMBR = '" + customerId +
                    "'");

                var aVendorName = _repository.ExecuteScalarQuery<string>(
                    "SELECT VENDNAME FROM " + Helpers.InterCompanyId + ".dbo.PM00200 WHERE VENDORID = '" + vendorId +
                    "'");

                _repository.ExecuteCommand(
                    string.Format(
                        "INTRANET.dbo.SaveAccountReceivablesNetTrans '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8:yyyyMMdd}','{9:yyyyMMdd}','{10}','{11}',{12}",
                        Helpers.InterCompanyId, batchId, descripción, customerId, aCustomerName, vendorId, aVendorName,
                        currency, DateTime.ParseExact(fechaDocumento, "dd/MM/yyyy", null),
                        DateTime.ParseExact(fechaCorte, "dd/MM/yyyy", null),
                        note, resolución, Account.GetAccount(User.Identity.GetUserName()).UserId));
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(xStatus, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveNetTransDetail(string batchId, int check, string module, string customerVendor,
            string documentNumber, string documentDate, string currencyId, decimal currentAmount, decimal appliedAmount, int documentType)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand(
                    string.Format(
                        "INTRANET.dbo.SaveAccountReceivablesNetTransDetail '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9:yyyyMMdd}',{10},'{11}'",
                        Helpers.InterCompanyId, batchId, check, module, customerVendor,
                        documentNumber, currencyId, currentAmount.ToString(new CultureInfo("en-US")),
                        appliedAmount.ToString(new CultureInfo("en-US")),
                        DateTime.ParseExact(documentDate, "dd/MM/yyyy", null),
                        documentType, Account.GetAccount(User.Identity.GetUserName()).UserId));

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(xStatus, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult PostNetTrans(string batchId, string descripción, string customerId, decimal customerAmount,
            string vendorId, decimal vendorAmount, decimal netDifference, string currency, string date, bool generateDocuments, string note)
        {
            string xStatus;
            var message = "";
            try
            {
                var cutomerApplied = customerAmount > vendorAmount ? vendorAmount : customerAmount;
                var vendorApplied = customerAmount > vendorAmount ? vendorAmount : customerAmount;
                var aRegistros = _repository.ExecuteQuery<MemNetTransDetail>(string.Format(
                    "SELECT BACHNMBR BatchNumber, [CHECK] [Check], MODULE Module, CUSTVEND CustomerVendor, " +
                    "DOCNUMBE DocumentNumber, CONVERT(NVARCHAR(10), DOCDATE, 103) DocumentDate, CONVERT(INT, DOCTYPE) DocumentType, CURRCYID CurrencyId, " +
                    "CURRAMNT CurrentAmount, APPLAMNT AppliedAmount " +
                    "FROM " + Helpers.InterCompanyId + ".dbo.SESOP30502 " +
                    "WHERE BACHNMBR = '{0}'", batchId)).ToList();

                var codigo = _repository.ExecuteScalarQuery<string>($"INTRANET.dbo.GetNextNumberAccountReceivables '{Helpers.InterCompanyId}','{7}'");
                var credit = new GpCreditNote
                {
                    Cliente = customerId,
                    CompanyId = Helpers.CompanyIdWebServices,
                    Codigo = codigo,
                    Descripción = descripción,
                    Fecha = DateTime.ParseExact(date, "dd/MM/yyyy", null),
                    Moneda = currency,
                    Lote = batchId,
                    Monto = cutomerApplied
                };

                var service = new ServiceContract();
                service.ProcessReceivablesCreditNote(credit, Request.Cookies["UserAccount"]?["username"], Request.Cookies["UserAccount"]?["password"]);
                _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHCOMNT = '" +
                                           descripción + "' WHERE BACHNUMB = '" + batchId + "' AND SERIES = 3 AND BCHSOURC = 'RM_Sales'");

                foreach (var item in aRegistros)
                {
                    if (item.Module != "CC" || item.AppliedAmount <= 0 || cutomerApplied <= 0) continue;
                    if (item.AppliedAmount > cutomerApplied)
                    {
                        _repository.ExecuteCommand(
                            string.Format(
                                "INTRANET.dbo.ApplyReceivablesDocuments '{0}','{1}','{2}','{3}','{4}','{5}','{6:yyyyMMdd}'",
                                Helpers.InterCompanyId, item.DocumentNumber, codigo,
                                cutomerApplied.ToString(new CultureInfo("en-US")),
                                7, item.DocumentType, DateTime.ParseExact(date, "dd/MM/yyyy", null)));
                        cutomerApplied = 0;
                    }
                    else
                    {
                        var docType = _repository.ExecuteScalarQuery<short>(
                            "SELECT DOCTYPE FROM " + Helpers.InterCompanyId + ".dbo.PA50105 " +
                            "WHERE USERID = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "' " +
                            "AND RTRIM(DOCNUMBR) = '" + item.DocumentNumber + "'");
                        _repository.ExecuteCommand(
                            string.Format(
                                "INTRANET.dbo.ApplyReceivablesDocuments '{0}','{1}','{2}','{3}','{4}','{5}','{6:yyyyMMdd}'",
                                Helpers.InterCompanyId, item.DocumentNumber, codigo,
                                item.AppliedAmount.ToString(new CultureInfo("en-US")),
                                7, docType, DateTime.ParseExact(date, "dd/MM/yyyy", null)));

                        cutomerApplied -= Convert.ToDecimal(item.AppliedAmount);
                    }
                }

                service = new ServiceContract();
                service.PostBatch("RM_Sales", batchId, ref message);

                codigo = _repository.ExecuteScalarQuery<string>($"INTRANET.dbo.GetNextNumberAccountPayables '{Helpers.InterCompanyId}'");

                credit = new GpCreditNote
                {
                    Cliente = vendorId,
                    CompanyId = Helpers.CompanyIdWebServices,
                    Codigo = codigo,
                    Descripción = descripción,
                    Fecha = DateTime.ParseExact(date, "dd/MM/yyyy", null),
                    Moneda = currency,
                    Lote = batchId,
                    Monto = vendorApplied,
                    Ncf = HelperLogic.AsignaciónSecuencia("SESOP30401", Account.GetAccount(User.Identity.GetUserName()).UserId)
                };

                service = new ServiceContract();
                service.ProcessPayablesCreditNote(credit, Request.Cookies["UserAccount"]?["username"], Request.Cookies["UserAccount"]?["password"]);
                _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHCOMNT = '" + descripción + "' WHERE BACHNUMB = '" + batchId +
                                           "' AND SERIES = 4 AND BCHSOURC = 'PM_Trxent'");

                foreach (var item in aRegistros)
                {
                    if (item.Module != "CPP" || item.AppliedAmount <= 0 || vendorApplied <= 0) continue;
                    {
                        if (item.AppliedAmount > vendorApplied)
                        {
                            _repository.ExecuteCommand(String.Format(
                                "INTRANET.dbo.ApplyAccountPayablesDocuments '{0}','{1}','{2:yyyyMMdd}','{3}','{4}','{5}','{6}','{7}','{8}'",
                                    Helpers.InterCompanyId, vendorId, DateTime.ParseExact(date, "dd/MM/yyyy", null),
                                    codigo, item.DocumentNumber, vendorApplied.ToString(new CultureInfo("en-US")),
                                    currency, credit.Ncf, Account.GetAccount(User.Identity.GetUserName()).UserId));
                            vendorApplied = 0;
                        }
                        else
                        {

                            _repository.ExecuteCommand(String.Format(
                                    "INTRANET.dbo.ApplyAccountPayablesDocuments '{0}','{1}','{2:yyyyMMdd}','{3}','{4}','{5}','{6}','{7}','{8}'",
                                    Helpers.InterCompanyId, vendorId, DateTime.ParseExact(date, "dd/MM/yyyy", null),
                                    codigo, item.DocumentNumber, item.AppliedAmount.ToString(new CultureInfo("en-US")),
                                    currency, credit.Ncf, Account.GetAccount(User.Identity.GetUserName()).UserId));

                            vendorApplied -= Convert.ToDecimal(item.AppliedAmount);
                        }
                    }
                }

                service = new ServiceContract();
                service.PostBatch("PM_Trxent", batchId, ref message);

                if (generateDocuments)
                {
                    GenerateDocuments(customerId, customerAmount, vendorId, vendorAmount, netDifference, batchId, date, currency, note, descripción);
                }

                _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SESOP30501 SET POSTED = 1 WHERE BACHNMBR = '" + batchId + "'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message + "\n" + message;
            }

            return Json(xStatus, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult PrintNetTrans(string batchId)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "NetTrans.pdf",
                    $"INTRANET.dbo.NetTransReport '{Helpers.InterCompanyId}','{batchId}'", 19, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        private void GenerateDocuments(string customerId, decimal customerAmount, string vendorId, decimal vendorAmount,
            decimal netDifference, string batchId, string date, string currency, string note, string descripción)
        {
            string secuencia;
            if (customerAmount > vendorAmount)
            {
                secuencia = HelperLogic.AsignaciónSecuencia("RM10201",
                    Account.GetAccount(User.Identity.GetUserName()).UserId);

                var service = new ServiceContract();
                var receipt = new GpCashReceipt
                {
                    CustomerId = customerId,
                    Amount = netDifference,
                    BatchNumber = batchId,
                    DocumentDate = DateTime.ParseExact(date, "dd/MM/yyyy", null),
                    CurrencyId = currency,
                    DocumentNumber = secuencia,
                    Description = descripción
                };
                service.CreateCashReceipt(receipt, Request.Cookies["UserAccount"]?["username"],
                    Request.Cookies["UserAccount"]?["password"]);
                _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHCOMNT = '" +
                                           descripción + "' WHERE BACHNUMB = '" + batchId +
                                           "' AND SERIES = 3 AND BCHSOURC = 'RM_Cash'");

                _repository.ExecuteCommand(
                    $"INTRANET.dbo.AttachCashReceiptNote '{Helpers.InterCompanyId}','{secuencia}','{note}'");
            }
            else
            {
                var order = 16384;

                secuencia = _repository.ExecuteScalarQuery<string>(
                    $"INTRANET.dbo.GetNextNumberPaymentRequest '{Helpers.InterCompanyId}'");

                _repository.ExecuteCommand(string.Format("INTRANET.dbo.BatchInsert '{0}','{1}','{2}','{3}','{4}','{5}'",
                    Helpers.InterCompanyId, batchId, netDifference.ToString(new CultureInfo("en-US")),
                    currency, Account.GetAccount(User.Identity.GetUserName()).UserId, "PM_Payment"));

                var numeroCheque = HelperLogic.AsignaciónSecuencia("PM10400",
                    Account.GetAccount(User.Identity.GetUserName()).UserId);

                _repository.ExecuteCommand(
                    String.Format(
                        "INTRANET.dbo.PaymentInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9:yyyyMMdd}','{10}', '{11}'",
                        Helpers.InterCompanyId, batchId, secuencia, note,
                        netDifference.ToString(new CultureInfo("en-US")), 0, vendorId, descripción, currency,
                        DateTime.ParseExact(date, "dd/MM/yyyy", null), numeroCheque,
                        Account.GetAccount(User.Identity.GetUserName()).UserId));

                _repository.ExecuteCommand(string.Format(
                    "INTRANET.dbo.PaymentGeneralLedgerInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'",
                    Helpers.InterCompanyId, secuencia, "", order, vendorId,
                    netDifference.ToString(new CultureInfo("en-US")), 0, 1,
                    currency, Account.GetAccount(User.Identity.GetUserName()).UserId));

                order += 16384;

                _repository.ExecuteCommand(string.Format(
                    "INTRANET.dbo.PaymentGeneralLedgerInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'",
                    Helpers.InterCompanyId, secuencia, "", order, vendorId, 0,
                    netDifference.ToString(new CultureInfo("en-US")), 2,
                    currency, Account.GetAccount(User.Identity.GetUserName()).UserId));

                _repository.ExecuteCommand(string.Format(
                    "INTRANET.dbo.PaymentControlInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}'",
                    Helpers.InterCompanyId, secuencia, vendorId,
                    netDifference.ToString(new CultureInfo("en-US")),
                    currency, Account.GetAccount(User.Identity.GetUserName()).UserId, "PM_Payment"));

                _repository.ExecuteCommand(String.Format(
                    "LODYNDEV.dbo.LPPOP30900SI '{0}','{1}','{2}','{3}','{4}','{5}','{6}'",
                    Helpers.InterCompanyId, secuencia, "NEGOCIOS", "Baja", "CHEQUE",
                    Account.GetAccount(User.Identity.GetUserName()).UserId,
                    Account.GetAccount(User.Identity.GetUserName()).UserId));

                _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHCOMNT = '" +
                                           descripción + "' WHERE BACHNUMB = '" + batchId +
                                           "' AND SERIES = 4 AND BCHSOURC = 'PM_Payment'");
            }
        }

        #endregion

        #region Cash Receipts

        public ActionResult CashReceiptIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "CashReceipt"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            var sqlQuery =
                "SELECT A.DOCNUMBR CashReceiptId, A.BACHNUMB BatchNumber, A.TRXDSCRN Description, A.CUSTNMBR CustomerId, B.CUSTNAME CustomerName, " +
                "A.CURNCYID Currency, A.DOCDATE DocumentDate, CONVERT(INT, A.CSHRCTYP) CashReceiptType, A.ORTRXAMT Amount, CONVERT(BIT, A.POSTED) Posted, " +
                "CONVERT(BIT, A.VOIDSTTS) Voided FROM (" +
                "SELECT DOCNUMBR, BACHNUMB, TRXDSCRN, CUSTNMBR, CURNCYID, DOCDATE, CSHRCTYP, ORTRXAMT, 0 POSTED, 0 VOIDSTTS " +
                "FROM " + Helpers.InterCompanyId + ".dbo.RM10201 " +
                "UNION ALL " +
                "SELECT DOCNUMBR, BACHNUMB, TRXDSCRN, CUSTNMBR, CURNCYID, DOCDATE, CSHRCTYP, ORTRXAMT, 1 POSTED, VOIDSTTS " +
                "FROM " + Helpers.InterCompanyId + ".dbo.RM20101 WHERE RMDTYPAL = 9) A " +
                "INNER JOIN " + Helpers.InterCompanyId + ".dbo.RM00101 B " +
                "ON A.CUSTNMBR = B.CUSTNMBR " +
                "ORDER BY DOCNUMBR";

            var trans = _repository.ExecuteQuery<CashReceipt>(sqlQuery).ToList();
            return View(trans);
        }

        public ActionResult CashReceipt()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "CashReceipt"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            if (ViewBag.DocumentNumber == null)
            {
                ViewBag.DocumentNumber =
                    HelperLogic.AsignaciónSecuencia("RM10201", Account.GetAccount(User.Identity.GetUserName()).UserId);
            }
            return View();
        }

        public ActionResult CashReceiptEdit(string documentNumber)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables", "CashReceipt"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            var sqlQuery = "SELECT A.DOCNUMBR CashReceiptId, A.BACHNUMB BatchNumber, A.TRXDSCRN Description, A.CUSTNMBR CustomerId, B.CUSTNAME CustomerName, " +
                "A.CURNCYID Currency, A.DOCDATE DocumentDate, CONVERT(INT, A.CSHRCTYP) CashReceiptType, A.ORTRXAMT Amount, CONVERT(BIT, A.POSTED) Posted, ISNULL(C.TXTFIELD, '') Note FROM (" +
                "SELECT DOCNUMBR, BACHNUMB, TRXDSCRN, CUSTNMBR, CURNCYID, DOCDATE, CSHRCTYP, ORTRXAMT, 0 POSTED, NOTEINDX " +
                "FROM " + Helpers.InterCompanyId + ".dbo.RM10201 " +
                "UNION ALL " +
                "SELECT DOCNUMBR, BACHNUMB, TRXDSCRN, CUSTNMBR, CURNCYID, DOCDATE, CSHRCTYP, ORTRXAMT, 1 POSTED, NOTEINDX " +
                "FROM " + Helpers.InterCompanyId + ".dbo.RM20101 WHERE RMDTYPAL = 9) A " +
                "INNER JOIN " + Helpers.InterCompanyId + ".dbo.RM00101 B ON A.CUSTNMBR = B.CUSTNMBR " +
                "LEFT  JOIN " + Helpers.InterCompanyId + ".dbo.SY03900 C ON A.NOTEINDX = C.NOTEINDX " +
                "WHERE A.DOCNUMBR = '" + documentNumber + "'";
            var trans = _repository.ExecuteScalarQuery<CashReceipt>(sqlQuery);

            sqlQuery = "SELECT RTRIM(APTODCNM) DocumentNumber, CONVERT(NUMERIC(32,2), APPTOAMT) DocumentAmount "
                       + "FROM " + Helpers.InterCompanyId + ".dbo.RM20201 "
                       + "WHERE APFRDCNM = '" + documentNumber + "'";

            ViewBag.InvoicesApplied = _repository.ExecuteQuery<PaymentRequestInvoiceViewModel>(sqlQuery).ToList();

            return View(trans.Posted ? "CashReceiptInquiry" : "CashReceiptEdit", trans);
        }

        public JsonResult UnblockSecuence(string secuencia, string formulario, string usuario)
        {
            HelperLogic.DesbloqueoSecuencia(secuencia, "RM10201",
                Account.GetAccount(User.Identity.GetUserName()).UserId);
            return Json("");
        }

        [HttpPost]
        public ActionResult InvoiceApply(string customerId = "", string currency = "")
        {
            try
            {
                string sqlQuery = "";
                if (currency != "RDPESO")
                {
                    sqlQuery =
                    "SELECT RTRIM(DOCNUMBR) Id, CONVERT(NVARCHAR(20), CONVERT(NUMERIC(32,2), ORCTRXAM)) Descripción, "
                    + "CONVERT(nvarchar(20), CONVERT(DATE, DOCDATE, 112)) DataExtended FROM " + Helpers.InterCompanyId +
                    ".dbo.MC020102 " + "WHERE CUSTNMBR = '" + customerId + "' AND RTRIM(CURNCYID) = '" + currency +
                    "' AND ORCTRXAM > 0 AND RMDTYPAL IN(1,3) ";
                }
                else
                {
                    sqlQuery =
                    "SELECT RTRIM(DOCNUMBR) Id, CONVERT(NVARCHAR(20), CONVERT(NUMERIC(32,2), CURTRXAM)) Descripción, "
                    + "CONVERT(nvarchar(20), CONVERT(DATE, DOCDATE, 112)) DataExtended FROM " + Helpers.InterCompanyId +
                    ".dbo.RM20101 " + "WHERE CUSTNMBR = '" + customerId + "' AND RTRIM(CURNCYID) = '" + currency +
                    "' AND CURTRXAM > 0 AND RMDTYPAL IN(1,3) ";
                }

                var lookup = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();
                return Json(lookup, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public JsonResult SaveCashReceipt(CashReceipt cashReceipt)
        {
            string xStatus;

            try
            {
                var service = new ServiceContract();
                var receipt = new GpCashReceipt
                {
                    CustomerId = cashReceipt.CustomerId,
                    Amount = cashReceipt.Amount,
                    BatchNumber = cashReceipt.BatchNumber,
                    DocumentDate = cashReceipt.DocumentDate,
                    CurrencyId = cashReceipt.Currency,
                    DocumentNumber = cashReceipt.CashReceiptId,
                    Type = cashReceipt.CashReceiptType,
                    Description = cashReceipt.Description
                };

                var count = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                                ".dbo.RM10201 WHERE DOCNUMBR = '" +
                                                                cashReceipt.CashReceiptId + "'");

                if (count > 0)
                {
                    var sqlQuery =
                        "SELECT DOCNUMBR CashReceiptId, BACHNUMB BatchNumber, TRXDSCRN Description, CUSTNMBR CustomerId, " +
                        "CURNCYID CurrencyId, DOCDATE DocumentDate, CONVERT(INT, CSHRCTYP) CashReceiptType, ORTRXAMT Amount, CONVERT(BIT, POSTED) Posted " +
                        "FROM " + Helpers.InterCompanyId + ".dbo.RM10201 " +
                        "WHERE DOCNUMBR = '" + cashReceipt.CashReceiptId + "'";

                    var trans = _repository.ExecuteScalarQuery<CashReceipt>(sqlQuery);

                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHTOTAL -= '" +
                                               trans.Amount + "', NUMOFTRX -= 1 WHERE BACHNUMB = '" +
                                               trans.BatchNumber + "'");

                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.RM10201 " +
                        "SET TRXDSCRN = '" + cashReceipt.Description + "', CURNCYID = '" + cashReceipt.Currency + "', DOCDATE = '" + cashReceipt.DocumentDate.ToString("yyyyMMdd") + "'," +
                        "CSHRCTYP = '" + Convert.ToInt16(cashReceipt.CashReceiptType) + "', ORTRXAMT = '" + cashReceipt.Amount + "' WHERE DOCNUMBR = '" + cashReceipt.CashReceiptId + "'");

                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.RM10201 " +
                        "SET TRXDSCRN = '" + cashReceipt.Description + "', CURNCYID = '" + cashReceipt.Currency + "', DOCDATE = '" + cashReceipt.DocumentDate.ToString("yyyyMMdd") + "'," +
                        "CSHRCTYP = '" + Convert.ToInt16(cashReceipt.CashReceiptType) + "', ORTRXAMT = '" + cashReceipt.Amount + "'," +
                        "GLPOSTDT = '" + cashReceipt.DocumentDate.ToString("yyyyMMdd") + "' WHERE DOCNUMBR = '" + cashReceipt.CashReceiptId + "' AND CUSTNMBR = '" + cashReceipt.CustomerId + "'");
                    if (receipt.CurrencyId != "RDPESO")
                    {
                        _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.MC020102 " +
                          "SET DOCDATE = '" + cashReceipt.DocumentDate.ToString("yyyyMMdd") + "', ORCTRXAM = '" + cashReceipt.Amount + "' " +
                          "WHERE DOCNUMBR = '" + cashReceipt.CashReceiptId + "' AND CUSTNMBR = '" + cashReceipt.CustomerId + "'");
                    }

                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHTOTAL += '" + cashReceipt.Amount + "', " +
                        "NUMOFTRX += 1 WHERE BACHNUMB = '" + cashReceipt.BatchNumber + "'");
                }
                else
                {
                    service.CreateCashReceipt(receipt, Request.Cookies["UserAccount"]?["username"], Request.Cookies["UserAccount"]?["password"]);

                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.SY00500 SET BCHCOMNT = '" +
                                               cashReceipt.Description + "' WHERE BACHNUMB = '" + cashReceipt.BatchNumber +
                                               "' AND SERIES = 3 AND BCHSOURC = 'RM_Cash'");
                }

                _repository.ExecuteCommand($"INTRANET.dbo.AttachCashReceiptNote '{Helpers.InterCompanyId}','{cashReceipt.CashReceiptId}','{cashReceipt.Note}'");

                if (cashReceipt.InvoiceLines != null)
                {
                    foreach (var item in cashReceipt.InvoiceLines)
                    {
                        if (cashReceipt.Currency != "RDPESO")
                        {
                            _repository.ExecuteCommand(string.Format(
                                    "INTRANET.dbo.ApplyMulticurrencyReceivablesDocument '{0}','{1}','{2}','{3}','{4}','{5:yyyyMMdd}'",
                                    Helpers.InterCompanyId, cashReceipt.CustomerId, cashReceipt.CashReceiptId, item.DocumentNumber,
                                    Convert.ToDecimal(item.DocumentAmount).ToString(new CultureInfo("en-US")), cashReceipt.DocumentDate));
                        }
                        else
                        {
                            var docType = _repository.ExecuteScalarQuery<short>(
                            "SELECT RMDTYPAL FROM " + Helpers.InterCompanyId + ".dbo.RM20101 " +
                            "WHERE RTRIM(DOCNUMBR) = '" + item.DocumentNumber + "' " +
                            "AND RTRIM(CUSTNMBR) = '" + cashReceipt.CustomerId + "'");
                            _repository.ExecuteCommand(string.Format(
                                    "INTRANET.dbo.ApplyReceivablesDocuments '{0}','{1}','{2}','{3}','{4}','{5}','{6:yyyyMMdd}'",
                                    Helpers.InterCompanyId, item.DocumentNumber, cashReceipt.CashReceiptId,
                                    Convert.ToDecimal(item.DocumentAmount).ToString(new CultureInfo("en-US")),
                                    9, docType, cashReceipt.DocumentDate));
                        }
                    }
                }

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult PrintCashReceipt(string cashReceiptId)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "CashReceiptReport.pdf",
                    $"INTRANET.dbo.CashReceiptReport '{Helpers.InterCompanyId}','{cashReceiptId}'", 22, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        #endregion

        #region Reports

        public ActionResult AgingReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "AgingReport"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult AgingReport(bool detailReport, string filterFrom, string filterTo, int typeFilter,
            int methodCalc, decimal exchangeRate, int printCurrency, string date, int excludeNoActivity, int excludeZeroBalance, 
            int excludeFullyPaidTrx, int excludeCreditBalance, int excludeUnpostedAppldCrDocs)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                if (detailReport)
                {
                    ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "AgingAccountReceivablesDetail.pdf",
                        string.Format("INTRANET.dbo.AccountReceivablesAgingReportDetail '{0}','{1}','{2}','{3}','{4}','{5}',{6},{7},'{8}','{9}','{10}','{11}','{12}','{13}'",
                            Helpers.InterCompanyId, date, typeFilter == 0 ? filterFrom : "",
                            typeFilter == 0 ? filterTo : "", typeFilter == 1 ? filterFrom : "",
                            typeFilter == 1 ? filterTo : "", printCurrency,
                            exchangeRate.ToString(new CultureInfo("en-US")), methodCalc, excludeNoActivity, 
                            excludeZeroBalance, excludeFullyPaidTrx, excludeCreditBalance, excludeUnpostedAppldCrDocs), 15, ref xStatus);
                }
                else
                {
                    ReportHelper.Export(Helpers.ReportPath + "Reportes",
                        Server.MapPath("~/PDF/Reportes/") + "AgingAccountReceivablesSummary.pdf",
                        string.Format("INTRANET.dbo.AccountReceivablesAgingReportSummary '{0}','{1}','{2}','{3}','{4}','{5}',{6},{7},{8},'{9}','{10}','{11}','{12}','{13}'",
                            Helpers.InterCompanyId, date, typeFilter == 0 ? filterFrom : "",
                            typeFilter == 0 ? filterTo : "", typeFilter == 1 ? filterFrom : "",
                            typeFilter == 1 ? filterTo : "", printCurrency,
                            exchangeRate.ToString(new CultureInfo("en-US")), methodCalc, excludeNoActivity,
                            excludeZeroBalance, excludeFullyPaidTrx, excludeCreditBalance, excludeUnpostedAppldCrDocs), 14, ref xStatus);
                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public ActionResult TransAnalysisReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "TransAnalysisReport"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult TransAnalysisReport(string date)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "AccountReceivablesTransAnalysisReport.pdf",
                    $"INTRANET.dbo.AccountReceivablesTransAnalysisReport '{Helpers.InterCompanyId}','{DateTime.ParseExact(date, "dd/MM/yyyy", null):yyyyMMdd}'", 17, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public ActionResult RelationInvoicePaymentReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "RelationInvoicePaymentReport"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult RelationInvoicePaymentReport(string filterFrom, string filterTo, int typeFilter, string fromDate, string toDate)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "AccountReceivablesRelationReport.pdf",
                    string.Format("INTRANET.dbo.AccountReceivablesRelationReport '{0}','{1}','{2}','{3}','{4}','{5}'",
                        Helpers.InterCompanyId, fromDate, toDate, typeFilter, filterFrom, filterTo), 18, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        #endregion

        #region Attachments

        [HttpPost]
        public JsonResult AttachFile(HttpPostedFileBase fileData, string batchId)
        {
            bool aStatus;

            try
            {
                byte[] fileStream = null;
                using (var binaryReader = new BinaryReader(fileData.InputStream))
                {
                    fileStream = binaryReader.ReadBytes(fileData.ContentLength);
                }

                var fileName = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1];
                var fileType = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].Split('.')[1];

                _repository.ExecuteCommand(String.Format(
                    "INTRANET.dbo.AttachmentInsert '{0}','{1}','{2}',{3},'{4}','{5}','{6}'",
                    Helpers.InterCompanyId, batchId, fileName,
                    "0x" + BitConverter.ToString(fileStream).Replace("-", String.Empty),
                    fileType, Account.GetAccount(User.Identity.GetUserName()).UserId, "REQ"));
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
        public ActionResult LoadAttachmentFiles(string batchId)
        {
            try
            {
                var files = new List<string>();
                var sqlQuery = "SELECT RTRIM(fileName) FileName FROM " + Helpers.InterCompanyId +
                               ".dbo.CO00105 WHERE DOCNUMBR = '" + batchId + "' AND DELETE1 = 0";
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
            string status;
            try
            {
                var sqlQuery = "UPDATE " + Helpers.InterCompanyId +
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

        public ActionResult Download(string batchId, string fileName)
        {
            var sqlQuery = "SELECT BinaryBlob, FileType, A.fileName "
                           + "FROM " + Helpers.InterCompanyId + ".dbo.CO00105 A "
                           + "INNER JOIN " + Helpers.InterCompanyId +
                           ".dbo.coAttachmentItems B ON A.Attachment_ID = B.Attachment_ID "
                           + "WHERE A.DOCNUMBR = '" + batchId +
                           "' AND A.DELETE1 = 0 AND RTRIM(A.fileName) = '" + fileName + "'";

            var adjunto = ConnectionDb.GetDt(sqlQuery);
            var fileType = "";
            var aFileName = "";

            if (adjunto.Rows.Count <= 0) return File((byte[])null, fileType.Trim(), aFileName.Trim());
            var contents = (byte[])adjunto.Rows[0][0];
            fileType = adjunto.Rows[0][1].ToString();
            aFileName = adjunto.Rows[0][2].ToString();

            return File(contents, fileType.Trim(), aFileName.Trim());
        }

        #endregion

        #region Apply Documents

        public ActionResult ApplyDocuments()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables", "ApplyDocuments"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        public JsonResult GetDocumentsForApply(string customerId, string documentNumber)
        {
            string xStatus;
            var xRegistros = new List<MemNetTransDetail>();
            try
            {
                var sqlQuery = "SELECT TOP 1 RTRIM(CURNCYID) FROM " +
                               Helpers.InterCompanyId +
                               ".dbo.RM20101 " + "WHERE CUSTNMBR = '" + customerId + "' AND CURTRXAM > 0 AND RMDTYPAL IN(1, 3) ";

                var currency = _repository.ExecuteScalarQuery<string>(sqlQuery);

                if (currency != "RDPESO")
                {
                    sqlQuery = "SELECT RTRIM(DOCNUMBR) Id, CONVERT(NVARCHAR(20), CONVERT(NUMERIC(32,2), ORCTRXAM)) Descripción, "
                       + "CONVERT(nvarchar(20), CONVERT(DATE, DOCDATE, 112)) DataExtended FROM " + Helpers.InterCompanyId +
                       ".dbo.MC020102 " + "WHERE CUSTNMBR = '" + customerId + "' AND RTRIM(CURNCYID) = '" + currency +
                       "' AND ORCTRXAM > 0 AND RMDTYPAL IN(1,3) ";
                }
                else
                {
                    sqlQuery = "SELECT RTRIM(DOCNUMBR) DocumentNumber, CONVERT(NUMERIC(32,2), CURTRXAM) CurrentAmount, "
                               + "CONVERT(nvarchar(20), CONVERT(DATE, DOCDATE, 112)) DocumentDate, RTRIM(CURNCYID) CurrencyId FROM " +
                               Helpers.InterCompanyId +
                               ".dbo.RM20101 " + "WHERE CUSTNMBR = '" + customerId + "' AND CURTRXAM > 0 AND RMDTYPAL IN(1, 3) ";
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
        public JsonResult PostApplyDocument(CashReceipt cashReceipt, string docDate)
        {
            string xStatus;

            try
            {
                var toDocType = _repository.ExecuteScalarQuery<short>(
                    "SELECT RMDTYPAL FROM " + Helpers.InterCompanyId + ".dbo.RM20101 " +
                    "WHERE RTRIM(DOCNUMBR) = '" + cashReceipt.CashReceiptId + "' " +
                    "AND RTRIM(CUSTNMBR) = '" + cashReceipt.CustomerId + "'");

                foreach (var item in cashReceipt.InvoiceLines)
                {
                    var docType = _repository.ExecuteScalarQuery<short>(
                        "SELECT RMDTYPAL FROM " + Helpers.InterCompanyId + ".dbo.RM20101 " +
                        "WHERE RTRIM(DOCNUMBR) = '" + item.DocumentNumber + "' " +
                        "AND RTRIM(CUSTNMBR) = '" + cashReceipt.CustomerId + "'");

                    _repository.ExecuteCommand(string.Format(
                            "INTRANET.dbo.ApplyReceivablesDocuments '{0}','{1}','{2}','{3}','{4}','{5}','{6:yyyyMMdd}'",
                            Helpers.InterCompanyId, item.DocumentNumber, cashReceipt.CashReceiptId,
                            Convert.ToDecimal(item.DocumentAmount).ToString(new CultureInfo("en-US")),
                            toDocType, docType, DateTime.ParseExact(docDate, "dd/MM/yyyy", null)));
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

        #region Post Batch

        public ActionResult PostBatch()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "PostBatch"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            return View();
        }

        [HttpPost]
        public ActionResult PostBatch(string bachNumber, string bachSource)
        {
            var aStatus = "";
            try
            {
                var service = new ServiceContract();
                bachSource = bachSource.Trim().Replace(" ", "%20");

                if (service.PostBatch(bachSource, bachNumber, ref aStatus))
                {
                    aStatus = "OK";
                    if (bachSource == "PM_Payment")
                    {
                        Thread.Sleep(3000);
                        _repository.ExecuteCommand($"INTRANET.dbo.CheckPaymentDuplication '{Helpers.InterCompanyId}','{bachNumber}'");
                    }
                }
            }
            catch (Exception e)
            {
                aStatus = e.Message;
            }

            return new JsonResult { Data = new { status = aStatus } };
        }

        #endregion

        #region Inquiry

        public ActionResult TransactionInquiry()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AccountReceivables",
                "AgingReport"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            return View();
        }

        [HttpPost]
        public ActionResult TransactionInquiry(string custVendId, string custVendName, string fromDate, string toDate,
            int typeFilter, int all)
        {
            string xStatus;
            List<Item> xRegistros = null;
            try
            {
                var sqlQuery = string.Format("INTRANET.dbo.TransactionInquiry '{0}','{1}','{2}','{3}','{4}','{5}'",
                    Helpers.InterCompanyId, custVendId, fromDate, toDate, typeFilter, all);

                xRegistros = _repository.ExecuteQuery<Item>(sqlQuery).ToList();
                xStatus = "OK";
            }
            catch (Exception e)
            {
                xStatus = e.Message;
            }
            return Json(new { status = xStatus, registros = xRegistros }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Interests

        public ActionResult InterestIndex(int type = 0)
        {
            List<InterestHeader> list;
            string sqlQuery;
            if (type == 0)
                sqlQuery = $"SELECT [BatchNumber],[Description],[CutDate],[SearchDate],[BillingMonth],[DocumentDate],[InterestRate],[Charge],[Preliminar],[Posted],[RowId] " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFRM10100 WHERE Preliminar = 0";
            else
                sqlQuery = $"SELECT [BatchNumber],[Description],[CutDate],[SearchDate],[BillingMonth],[DocumentDate],[InterestRate],[Charge],[Preliminar],[Posted],[RowId] " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFRM10100 WHERE Preliminar = 1";
            list = _repository.ExecuteQuery<InterestHeader>(sqlQuery).ToList();
            ViewBag.Type = type;
            return View(list);
        }

        public ActionResult InterestEntry(int type = 0, int preliminar = 0)
        {
            ViewBag.Type = type;
            ViewBag.Preliminar = preliminar;
            ViewBag.Total = 0;
            return View();
        }

        public JsonResult InsertInterestData(string batchNumber, string description, string cutDate, string searchDate, string billingMonth, string documentDate, decimal interestRate, decimal charge, int type)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand($"INTRANET.dbo.InsertInterestTransaction '{Helpers.InterCompanyId}','{batchNumber}','{description}'," +
                    $"'{DateTime.ParseExact(cutDate,"dd/MM/yyyy", null).ToString("yyyyMMdd")}','{DateTime.ParseExact(searchDate, "dd/MM/yyyy", null).ToString("yyyyMMdd")}'," +
                    $"'{DateTime.ParseExact(billingMonth, "dd/MM/yyyy", null).ToString("yyyyMMdd")}','{DateTime.ParseExact(documentDate, "dd/MM/yyyy", null).ToString("yyyyMMdd")}'," +
                    $"'{interestRate}','{charge}','{type}','{Account.GetAccount(User.Identity.GetUserName()).UserId}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            
            return Json(new { status = xStatus}, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetInterestDataCustomer(string batchNumber)
        {
            string xStatus;
            List<InterestCustomer> customers = null;
            try
            {
                var sqlQuery = $"SELECT CustomerId, TotalAmount, Exclude FROM {Helpers.InterCompanyId}.dbo.EFRM00110 WHERE BatchNumber = '{batchNumber}'";
                customers = _repository.ExecuteQuery<InterestCustomer>(sqlQuery).ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = customers }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetInterestDataDetails(string batchNumber, string customerId)
        {
            string xStatus;
            List<InterestDetail> details = null;
            try
            {
                var sqlQuery = $"SELECT CustomerId, DocumentNumber, DocumentAmount, CurrentAmount, DueDate, CutDate, Days, InterestRate, InterestAmount, Charge, ChargeAmount, TotalAmount " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFRM00120 WHERE BatchNumber = '{batchNumber}' AND CustomerId = '{customerId}'";
                details = _repository.ExecuteQuery<InterestDetail>(sqlQuery).ToList();
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new { status = xStatus, registros = details }, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}