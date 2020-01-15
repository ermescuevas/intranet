using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Seaboard.Intranet.Data.Repository;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly GenericRepository _repository;

        public InventoryController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Inventory", "Create"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        public ActionResult Edit()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Inventory", "Edit"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        public ActionResult InventoryInquiry()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Inventory", "Inventory"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult InventoryInquiry(string filterFrom, string filterTo, int typeFilter, string date)
        {
            var sqlQuery = String.Format("INTRANET.dbo.InventoryInquiry '{0}','{1}','{2}','{3}','{4}'",
                Helpers.InterCompanyId, typeFilter, filterFrom, filterTo, date);
            var xRegistros = _repository.ExecuteQuery<InventoryRequestDetail>(sqlQuery).ToList();
            return Json(xRegistros, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DepartmentMovementInquiry()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Inventory", "Department"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult DepartmentMovementInquiry(string filterFrom, string filterTo, string fromDate, string toDate)
        {
            var sqlQuery = String.Format("INTRANET.dbo.DepartmentMovementInquiry '{0}','{1}','{2}','{3}','{4}'",
                Helpers.InterCompanyId, filterFrom, filterTo, fromDate, toDate);
            var xRegistros = _repository.ExecuteQuery<InventoryRequestDetail>(sqlQuery).ToList();
            return Json(xRegistros, JsonRequestBehavior.AllowGet);
        }

        public ActionResult InvoiceMovementInquiry()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Inventory", "Invoice"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult InvoiceMovementInquiry(string fromDate, string toDate)
        {
            string xStatus;
            List<InventoryRequestDetail> xRegistros = null;
            try
            {
                var sqlQuery = String.Format("INTRANET.dbo.InvoiceMovementInquiry '{0}','{1}','{2}'",
                    Helpers.InterCompanyId, fromDate, toDate);
                xRegistros = _repository.ExecuteQuery<InventoryRequestDetail>(sqlQuery).ToList();
                xStatus = "OK";

            }
            catch (Exception e)
            {
                xStatus = e.Message;
            }
            return Json(new {status = xStatus, registros = xRegistros}, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult GetItemReorder(string filter = "")
        {
            try
            {
                var sqlQuery = "";

                sqlQuery = "SELECT RTRIM(A.ITEMNMBR) ItemId, RTRIM(B.ITEMDESC) ItemDescription, RTRIM(B.ITMCLSCD) ItemClass, "
                    + "A.ORDRPNTQTY MinStock, A.ORDRUPTOLVL MaxStock, RTRIM(C.BASEUOFM) UnitId "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.IV00102 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B ON A.ITEMNMBR = B.ITEMNMBR "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV40201 C ON B.UOMSCHDL = C.UOMSCHDL "
                    + "WHERE RTRIM(B.ITMCLSCD) LIKE '%" + filter + "%' AND RCRDTYPE = 1 AND RTRIM(B.ITMCLSCD) <> 'FACT' "
                    + "ORDER BY ItemId ";

                var items = _repository.ExecuteQuery<InventoryRequestDetail>(sqlQuery);


                return Json(items, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public JsonResult ProcessReorder(InventoryPurchase[] items)
        {
            string xStatus;

            try
            {
                if (items != null)
                {
                    foreach (var t in items)
                    {
                        _repository.ExecuteCommand(String.Format("INTRANET.dbo.UpdateItemReorder '{0}','{1}','{2}','{3}','{4}'",
                            Helpers.InterCompanyId, t.ItemId, t.QtyMin, t.QtyMax, Account.GetAccount(User.Identity.GetUserName()).UserId));
                    }
                    xStatus = "OK";
                }
                else { xStatus = "ERROR"; }

            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult GetItemRequest(string filter = "")
        {
            try
            {
                string sqlQuery;

                if (filter.Length == 0)
                {
                    sqlQuery = "SELECT RTRIM(A.ITEMNMBR) ItemId, RTRIM(B.ITEMDESC) ItemDescription, RTRIM(B.ITMCLSCD) ItemClass, "
                    + "SUM(A.ORDRPNTQTY) MinStock, SUM(A.ORDRUPTOLVL) MaxStock, SUM(A.QTYONHND) - SUM(A.ATYALLOC) QtyOnHand, RTRIM(C.BASEUOFM) UnitId, "
                    + "CASE WHEN (SUM(A.ORDRUPTOLVL) - (SUM(A.QTYONHND) - SUM(A.ATYALLOC))) < 0 THEN 0 ELSE SUM(A.ORDRUPTOLVL) - (SUM(A.QTYONHND) - SUM(A.ATYALLOC)) END QtyRequest "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.IV00102 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B ON A.ITEMNMBR = B.ITEMNMBR "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV40201 C ON B.UOMSCHDL = C.UOMSCHDL "
                    + "WHERE A.RCRDTYPE = 1 "
                    + "GROUP BY A.ITEMNMBR, B.ITEMDESC, B.ITMCLSCD, C.BASEUOFM "
                    + "HAVING SUM(A.QTYONHND - A.ATYALLOC) < SUM(A.ORDRPNTQTY) "
                    + "ORDER BY ItemId ";
                }
                else
                {
                    sqlQuery = "SELECT RTRIM(A.ITEMNMBR) ItemId, RTRIM(B.ITEMDESC) ItemDescription, RTRIM(B.ITMCLSCD) ItemClass, "
                    + "SUM(A.ORDRPNTQTY) MinStock, SUM(A.ORDRUPTOLVL) MaxStock, SUM(A.QTYONHND) - SUM(A.ATYALLOC) QtyOnHand, RTRIM(C.BASEUOFM) UnitId, "
                    + "CASE WHEN (SUM(A.ORDRUPTOLVL) - (SUM(A.QTYONHND) - SUM(A.ATYALLOC))) < 0 THEN 0 ELSE SUM(A.ORDRUPTOLVL) - (SUM(A.QTYONHND) - SUM(A.ATYALLOC)) END QtyRequest "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.IV00102 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B ON A.ITEMNMBR = B.ITEMNMBR "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV40201 C ON B.UOMSCHDL = C.UOMSCHDL "
                    + "WHERE A.RCRDTYPE = 1 AND RTRIM(B.ITMCLSCD) LIKE '%" + filter + "%' "
                    + "GROUP BY A.ITEMNMBR, B.ITEMDESC, B.ITMCLSCD, C.BASEUOFM "
                    + "ORDER BY ItemId ";
                }

                var items = _repository.ExecuteQuery<InventoryRequestDetail>(sqlQuery);

                foreach (var item in items)
                {
                    sqlQuery = "SELECT COUNT(*) FROM "
                    + "(SELECT ITEMNMBR, B.RequisitionStatus, B.POPRequisitionNumber FROM " + Helpers.InterCompanyId + ".dbo.POP10210 A INNER JOIN " + Helpers.InterCompanyId + ".dbo.POP10200 B ON A.POPRequisitionNumber = B.POPRequisitionNumber "
                    + "WHERE ITEMNMBR = '" + item.ItemId + "' AND B.Workflow_Status = 6 "
                    + "UNION ALL "
                    + "SELECT ITEMNMBR, B.RequisitionStatus, B.POPRequisitionNumber FROM " + Helpers.InterCompanyId + ".dbo.POP30210 A INNER JOIN " + Helpers.InterCompanyId + ".dbo.POP30200 B ON A.POPRequisitionNumber = B.POPRequisitionNumber "
                    + "WHERE B.RequisitionStatus NOT IN(5, 7) AND B.Workflow_Status = 6 AND A.ITEMNMBR = '" + item.ItemId + "') A ";

                    if (_repository.ExecuteScalarQuery<int>(sqlQuery) > 0)
                    {
                        item.Status = "En requisicion";
                    }
                    else
                    {
                        item.Status = "Disponible";
                    }
                }

                return Json(items, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [OutputCache(Duration = 0)]
        public ActionResult Details(string id)
        {
            if (id == null) return PartialView();

            var sqlQuery = "SELECT DISTINCT POPRequisitionNumber Id, RequisitionDescription Descripción "
                              + "FROM "
                              + "(SELECT B.POPRequisitionNumber, B.RequisitionDescription "
                              + "FROM " + Helpers.InterCompanyId + ".dbo.POP10210 A INNER JOIN " + Helpers.InterCompanyId + ".dbo.POP10200 B ON A.POPRequisitionNumber = B.POPRequisitionNumber "
                              + "WHERE ITEMNMBR = '" + id + "' AND B.Workflow_Status = 6 "
                              + "UNION ALL "
                              + "SELECT B.POPRequisitionNumber, B.RequisitionDescription "
                              + "FROM " + Helpers.InterCompanyId + ".dbo.POP30210 A INNER JOIN " + Helpers.InterCompanyId + ".dbo.POP30200 B ON A.POPRequisitionNumber = B.POPRequisitionNumber "
                              + "WHERE B.RequisitionStatus NOT IN(5, 7) AND B.Workflow_Status = 6 AND A.ITEMNMBR = '" + id + "') A ";

            var requestDetail = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();

            ViewBag.RequestDetail = requestDetail;

            return PartialView();
        }

        [HttpPost]
        public JsonResult ProcessPurchaseRequest(InventoryPurchase[] items)
        {
            string status;

            try
            {
                if (items != null)
                {
                    var departmentCode = _repository.ExecuteScalarQuery<string>
                        ("SELECT TOP 1 DEPRTMID FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 WHERE RTRIM(DEPRTMDS) = '" + Account.GetAccount(User.Identity.GetUserName()).Department + "'").Trim();

                    var aprover = _repository.ExecuteScalarQuery<string>
                        ("SELECT TOP 1 RTRIM(USERID) FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40101 WHERE RTRIM(DEPRTMID) =  RTRIM('" + departmentCode + "') AND TYPE = 1 AND ISPRINC = 1").Trim();

                    var secuencia = HelperLogic.AsignaciónSecuencia("POP10200", Account.GetAccount(User.Identity.GetUserName()).UserId);
                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.PurchRequestInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}'",
                        Helpers.InterCompanyId, secuencia,"REQUISICION AUTOMATICA " + DateTime.Now.ToString("dd-MM-yyyy"), "", DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.AddDays(10).ToString("yyyyMMdd"), 
                        "Baja", Account.GetAccount(User.Identity.GetUserName()).Department, "NO", aprover, Account.GetAccount(User.Identity.GetUserName()).UserId));

                    HelperLogic.DesbloqueoSecuencia(secuencia, "POP10200", Account.GetAccount(User.Identity.GetUserName()).UserId);

                    var contador = 1;
                    var orden = 16384;
                    foreach (var item in items)
                    {
                        _repository.ExecuteCommand(String.Format("INTRANET.dbo.PurchRequestLineInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}'",
                            Helpers.InterCompanyId, secuencia, orden, contador, item.ItemId, item.ItemDescription, item.UnitId, "COMPRAS", item.Qty, GetAccountNum(item.ItemId), 
                            Account.GetAccount(User.Identity.GetUserName()).Department));

                        contador++;
                        orden += 16384;
                    }
                    status = "OK";
                }
                else { status = "ERROR"; }

            }
            catch(Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult DepartmentMovementReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Inventory", "Department"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult DepartmentMovementReport(string filterFrom, string filterTo, string fromDate, string toDate)
        {
            var status = "";
            try
            {
                status = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "DepartmentMovement.pdf",
                String.Format("INTRANET.dbo.DepartmentMovementReport '{0}','{1}','{2}','{3}','{4}'",
                Helpers.InterCompanyId, filterFrom, filterTo, fromDate, toDate), 8, ref status);
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult InventoryReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Inventory", "Inventory"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult InventoryReport(string filterFrom, string filterTo, int typeFilter, string date, bool zeroQuantity)
        {
            string status;
            try
            {
                status = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "Inventory.pdf",
                String.Format("INTRANET.dbo.InventoryReport '{0}','{1}','{2}','{3}','{4}','{5}'",
                Helpers.InterCompanyId, typeFilter, filterFrom, filterTo, date, zeroQuantity == false ? 0 : 1), 7, ref status);
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult InvoiceMovementReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Inventory", "Invoice"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult InvoiceMovementReport(string fromDate, string toDate)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes",Server.MapPath("~/PDF/Reportes/") + "InvoiceMovement.pdf",
                    String.Format("INTRANET.dbo.InvoiceMovementReport '{0}','{1}','{2}'", Helpers.InterCompanyId, fromDate, toDate), 20, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public string GetAccountNum(string itemId)
        {
            try
            {
                var accountNum = "";

                var itemClass = _repository.ExecuteScalarQuery<string>("SELECT TOP 1 ISNULL(ITMCLSCD, '') FROM " + Helpers.InterCompanyId + ".dbo.IV00101 WHERE ITEMNMBR = '" + itemId + "'");
                var count = 0;
                var departmentCode = _repository.ExecuteScalarQuery<string>("SELECT TOP 1 ISNULL(DEPRTMID, '') FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 WHERE DEPRTMDS = '" + Account.GetAccount(User.Identity.GetUserName()).Department + "'");
                var sqlQuery = "SELECT COUNT(*) "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.GL00100 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00102 B "
                    + "ON A.ACTINDX = B.ACTINDX "
                    + "WHERE B.DEPTMTID = '" + departmentCode + "' AND ITMCLSCD = '" + itemClass + "'";
                count = _repository.ExecuteScalarQuery<int>(sqlQuery);

                if (count > 0)
                {
                    sqlQuery = "SELECT TOP 1 RTRIM(A.ACTNUMBR_1) + '-' + RTRIM(A.ACTNUMBR_2) + '-' + RTRIM(A.ACTNUMBR_3) "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.GL00100 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00102 B "
                    + "ON A.ACTINDX = B.ACTINDX "
                    + "WHERE B.DEPTMTID = '" + departmentCode + "' AND ITMCLSCD = '" + itemClass + "'";
                    accountNum = _repository.ExecuteScalarQuery<string>(sqlQuery);
                }
                else
                {
                    accountNum = "";
                }

                return accountNum;
            }
            catch
            {
                return "";
            }
        }

        public class InventoryPurchase
        {
            public string ItemId { get; set; }
            public string ItemDescription { get; set; }
            public string UnitId { get; set; }
            public decimal Qty { get; set; }
            public decimal QtyMin { get; set; }
            public decimal QtyMax { get; set; }
            public string Status { get; set; }
        }
    }
}