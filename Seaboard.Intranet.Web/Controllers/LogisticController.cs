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
    public class LogisticController : Controller
    {
        private readonly GenericRepository _repository;

        public LogisticController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        #region Despacho

        public ActionResult DispachtIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "Dispacht"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            string sqlQuery = "SELECT TOP 300 RTRIM(A.DOCNUMBR) DispachtId, A.SRCDOCNUM Description, A.DOCDATE DocumentDate, A.WORKNUMB RequestId, "
               + "B.DEPTDESC DepartmentId, A.TRXLOCTN WarehouseId, ISNULL(C.TEXT1, '') Note, "
               + "CASE A.DOCSTTS WHEN 1 THEN 'No enviado' WHEN 2 THEN 'Enviada' WHEN 3 THEN 'Aprobado' WHEN 4 THEN 'En Proceso' WHEN 5 THEN 'Anulado' ELSE 'Cerrada' END Status "
               + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
               + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 B "
               + "ON A.DEPTMTID = B.DEPTMTID "
               + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00140 C "
               + "ON A.DOCNUMBR = C.DOCNUMBR "
               + "WHERE A.DOCTYPE = 2 "
               + "ORDER BY A.DEX_ROW_ID DESC";

            var purchaseRequestList = _repository.ExecuteQuery<DispachtHeaderViewModel>(sqlQuery);

            return View(purchaseRequestList);
        }

        public ActionResult DispachtEdit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "Dispacht"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            string originalNumber = "";
            string requester = "";

            string sqlQuery = "SELECT RTRIM(A.ITEMNMBR) ItemNumber, RTRIM(B.ITEMDESC) ItemDescription, A.TRXQTY QtyDispachted, "
                + "RTRIM(A.UOFM) UnitId, RTRIM(A.TRXLOCTN) WarehouseId "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B "
                + "ON A.ITEMNMBR = B.ITEMNMBR "
                + "WHERE A.DOCNUMBR = '" + id + "'";

            var purchaseRequestItems = _repository.ExecuteQuery<DispachtDetailViewModel>(sqlQuery).ToList();

            sqlQuery = "SELECT ISNULL(ORIGNUMB, '') FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 WHERE DOCNUMBR = '" + id + "'";
            originalNumber = _repository.ExecuteScalarQuery<string>(sqlQuery);

            sqlQuery = "SELECT ISNULL(USERID, '') FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 WHERE DOCNUMBR = '" + originalNumber + "'";
            requester = _repository.ExecuteScalarQuery<string>(sqlQuery);

            sqlQuery = "SELECT DOCSTTS FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 WHERE DOCNUMBR = '" + id + "'";
            var status = _repository.ExecuteScalarQuery<short>(sqlQuery);

            sqlQuery = "SELECT RTRIM(A.DOCNUMBR) DispachtId, RTRIM(A.SRCDOCNUM) Description, RTRIM(A.WORKNUMB) RequestId, "
                + "A.DOCDATE DocumentDate, RTRIM(B.DEPTDESC) DepartmentId, RTRIM(A.TRXLOCTN) WarehouseId, '' Requester, ISNULL(C.TEXT1, '') Note "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 B "
                + "ON A.DEPTMTID = B.DEPTMTID "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00140 C "
                + "ON A.DOCNUMBR = C.DOCNUMBR "
                + "WHERE A.DOCNUMBR = '" + id + "'";

            sqlQuery = "SELECT RTRIM(A.DOCNUMBR) DispachtId, RTRIM(A.SRCDOCNUM) Description, RTRIM(A.WORKNUMB) RequestId, "
                + "A.DOCDATE DocumentDate, RTRIM(B.DEPTDESC) DepartmentId, RTRIM(A.TRXLOCTN) WarehouseId, '' Requester, ISNULL(C.TEXT1, '') Note "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 B "
                + "ON A.DEPTMTID = B.DEPTMTID "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00140 C "
                + "ON A.DOCNUMBR = C.DOCNUMBR "
                + "WHERE A.DOCNUMBR = '" + id + "'";

            var purchaseRequest = _repository.ExecuteScalarQuery<DispachtHeaderViewModel>(sqlQuery);
            purchaseRequest.Requester = requester;

            sqlQuery = "SELECT ISNULL(EXCHRATE, 0.00) "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LPMC40100 "
                + "WHERE DOCNUMBR = '" + id + "' "
                + "AND DOCTYPE = 2 ";

            decimal exchangeRate = _repository.ExecuteScalarQuery<decimal>(sqlQuery);

            if (status != 4)
            {
                ViewBag.PurchaseRequestItems = purchaseRequestItems;
                ViewBag.ExchangeRate = exchangeRate;
                return View("DispachtInquiry", purchaseRequest);
            }
            else
            {
                ViewBag.PurchaseRequestItems = purchaseRequestItems;
                ViewBag.ExchangeRate = exchangeRate;
                return View(purchaseRequest);
            }
        }

        public ActionResult Dispacht()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "Dispacht"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            string sqlQuery = "SELECT DISTINCT A.DOCNUMBR DispachtId, B.WORKNUMB RequestId, B.SRCDOCNUM Description, C.DEPTDESC DepartmentId, B.USERID Requester, B.DOCDATE DocumentDate "
               + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A WITH (NOLOCK) "
               + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF10100 B WITH (NOLOCK) "
               + "ON A.DOCTYPE = B.DOCTYPE AND A.DOCNUMBR = B.DOCNUMBR "
               + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 C WITH (NOLOCK) "
               + "ON B.DEPTMTID = C.DEPTMTID "
               + "WHERE A.DOCTYPE = 1 AND A.QTYDSPT < A.QTYALWC AND A.ITEMSTTS IN (3, 4) AND LEN(B.SRCDOCNUM) > 0 ";

            var purchaseRequestList = _repository.ExecuteQuery<DispachtHeaderViewModel>(sqlQuery);

            return View(purchaseRequestList);
        }

        [OutputCache(Duration = 0)]
        public ActionResult DispachtDetails(string id)
        {
            if (id != null)
            {
                string sqlQuery = "";

                sqlQuery = "SELECT DISTINCT A.ITEMNMBR ItemNumber, B.ITEMDESC ItemDescription, A.TRXQTY QtyDispachted, A.UOFM UnitId, "
                         + "(CASE A.ITEMSTTS WHEN 1 THEN 'Nuevo' WHEN 2 THEN 'Enviado' WHEN 3 THEN 'Aprobado' WHEN 4 THEN 'Despachado' WHEN 5 THEN 'Anulado' ELSE 'Cerrado' END) Status "
                         + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                         + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B "
                         + "ON A.ITEMNMBR = B.ITEMNMBR "
                         + "WHERE A.DOCNUMBR = '" + id + "'";

                var requestDetail = _repository.ExecuteQuery<DispachtDetailViewModel>(sqlQuery).ToList();

                ViewBag.RequestDetail = requestDetail;
            }

            return PartialView();
        }

        [HttpPost]
        public JsonResult SaveDispacht(DispachtHeaderViewModel request, int postType = 0, decimal exchangeRate = 0)
        {
            string status = "";

            try
            {
                string originalNumber = _repository.ExecuteScalarQuery<string>
                ("SELECT TOP 1 ISNULL(ORIGNUMB, '') FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 WHERE RTRIM(DOCNUMBR) = '" + request.DispachtId + "' AND DOCTYPE = 2").Trim();

                if (request.Note != null)
                {
                    if (request.Note.Length > 0)
                    {
                        _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LLIF00140SI '{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}'",
                            Helpers.InterCompanyId, request.DispachtId, request.Note, "", "", "", "", "", "", "INIF00140"));
                    }
                }

                foreach (var i in request.DispachtLines)
                {
                    decimal originalLine = _repository.ExecuteScalarQuery<decimal>
                    ("SELECT TOP 1 ISNULL(ORLSQNBR, 0) FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 WHERE RTRIM(DOCNUMBR) = '" + request.DispachtId + "' AND RTRIM(ITEMNMBR)= '" + i.ItemNumber + "' AND DOCTYPE = 2");

                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.DeleteLogisticLine '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}'",
                    Helpers.InterCompanyId, "Despacho", request.DispachtId, i.ItemNumber, "LLIF10200", "LLIF10100", "Requisición", originalNumber));

                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.DispachtCreateDetail '{0}', '{1}', '{2}', {3}, '{4}', '{5}', {6}, {7}, '{8}', '{9}', '{10}', {11}, {12}, {13}, {14}, {15}, {16}, '{17}', '{18}', {19}, {20},'{21}','{22}'",
                    Helpers.InterCompanyId, "Despacho", request.DispachtId, -1, i.ItemNumber, i.UnitId, i.QtyDispachted, 0,
                    request.DepartmentId, request.WarehouseId, "", i.QtyDispachted, i.QtyDispachted, 0, 0, 1, 0,
                    Account.GetAccount(User.Identity.GetUserName()).UserId, "LLIF10200", 1, originalLine, "Requisición", originalNumber));

                    //SaveExchangeRate(request.DispachtId, ExchangeRate, 2, i.ItemNumber);
                }

                if (postType == 1)
                {
                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.TransferDispacht '{0}','{1}','{2}','{3}','{4}','{5}'",
                        Helpers.InterCompanyId, "Despacho", request.DispachtId, 6, Account.GetAccount(User.Identity.GetUserName()).UserId, "LLIF10200"));
                }

                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult DeleteDispacht(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            DispachtHeaderViewModel dispacht = _repository.GetBy<DispachtHeaderViewModel>(id);
            if (dispacht == null)
            {
                return HttpNotFound();
            }
            return View(dispacht);
        }

        [HttpPost, ActionName("DeleteDispacht")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteDispachtConfirmed(string dispachtId)
        {
            _repository.ExecuteCommand(String.Format("INTRANET.dbo.VoidLogisticDocument '{0}','{1}','{2}','{3}','{4}','{5}'",
                Helpers.InterCompanyId, "Despacho", dispachtId, 5, Account.GetAccount(User.Identity.GetUserName()).UserId, "LLIF10200"));

            return RedirectToAction("DispachtIndex");
        }

        [HttpPost]
        public ActionResult GetItemRequisition(string requisitionId = "")
        {
            try
            {
                List<DispachtDetailViewModel> detail = null;
                string sqlQuery = "";

                sqlQuery = "SELECT CONVERT(int, (A.LNSEQNBR / 16384)) LineId, A.ITEMNMBR ItemNumber, B.ITEMDESC ItemDescription, CONVERT(NUMERIC(19,2), A.TRXQTY) QtyOrder,  A.UOFM UnitId, "
                    + "CONVERT(NUMERIC(19,2), (A.TRXQTY - QTYDSPT)) QtyPending, A.UOFM BaseUnitId, CONVERT(NUMERIC(19,2), CONVERT(int, ((D.QTYONHND - D.ATYALLOC) / (CASE ISNULL(E.QTYBSUOM, 0) WHEN 0 THEN 1 ELSE E.QTYBSUOM END)))) QtyOnHand "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV40201 C "
                    + "ON B.UOMSCHDL =  C.UOMSCHDL "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 D "
                    + "ON B.ITEMNMBR =  D.ITEMNMBR "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV40202 E "
                    + "ON B.UOMSCHDL = E.UOMSCHDL AND A.UOFM = E.UOFM "
                    + "WHERE A.DOCNUMBR = '" + requisitionId + "'AND A.QTYDSPT < (A.TRXQTY) AND D.RCRDTYPE = 2 ";

                detail = _repository.ExecuteQuery<DispachtDetailViewModel>(sqlQuery).ToList();
                return Json(detail, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public JsonResult GetQuantitiesDispacht(string dispachtId = "", string itemNumber = "")
        {
            string status = "";
            try
            {
                decimal qtyOnHand = 0m;
                decimal qtyAllocated = 0m;
                decimal qtyPending = 0m;
                decimal qtyDispachted = 0m;

                string originalNumber = _repository.ExecuteScalarQuery<string>
                ("SELECT TOP 1 ISNULL(ORIGNUMB, '') FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 WHERE RTRIM(DOCNUMBR) = '" + dispachtId + "' AND DOCTYPE = 2").Trim();

                string quantity = "";
                string sqlQuery = "";

                sqlQuery = "SELECT CONVERT(NUMERIC(19,2), TRXQTY) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "WHERE A.DOCNUMBR = '" + dispachtId + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyDispachted = _repository.ExecuteScalarQuery<decimal>(sqlQuery);

                sqlQuery = "SELECT CONVERT(NUMERIC(19,2), ((A.TRXQTY) - QTYDSPT)) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "WHERE A.DOCNUMBR = '" + originalNumber + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyPending = _repository.ExecuteScalarQuery<decimal>(sqlQuery);
                qtyPending += qtyDispachted;

                sqlQuery = "SELECT ISNULL(B.QTYONHND, 0) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "WHERE A.DOCNUMBR = '" + dispachtId + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyOnHand = _repository.ExecuteScalarQuery<decimal>(sqlQuery);

                sqlQuery = "SELECT ISNULL(B.ATYALLOC, 0) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "WHERE A.DOCNUMBR = '" + dispachtId + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyAllocated = _repository.ExecuteScalarQuery<decimal>(sqlQuery);

                quantity = qtyOnHand.ToString("N2") + "-" + qtyAllocated.ToString("N2") + "-" + qtyPending.ToString("N2") + "-" + qtyDispachted.ToString("N2");

                status = quantity;
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult GenerateDispacht(DispachtDetailViewModel[] items)
        {
            string status = "";

            try
            {
                if (items != null)
                {
                    string secuencia = HelperLogic.AsignaciónSecuencia("LLIF10200", Account.GetAccount(User.Identity.GetUserName()).UserId);
                    var request = _repository.ExecuteScalarQuery<DispachtDetail>("SELECT ISNULL(WORKNUMB, '') Request, ISNULL(DEPTMTID, '') DepartmentId, ISNULL(TRXLOCTN, '') WarehouseId FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 WHERE DOCNUMBR = '" + items.First().RequestId + "'");

                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.DispachtCreateHeader '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'",
                        Helpers.InterCompanyId, "Despacho", secuencia, DateTime.Now.ToString("yyyyMMdd"), request.DepartmentId, request.WarehouseId,
                        "", request.Request, Account.GetAccount(User.Identity.GetUserName()).UserId, "LLIF10200"));

                    HelperLogic.DesbloqueoSecuencia(secuencia, "LLIF10200", Account.GetAccount(User.Identity.GetUserName()).UserId);

                    DispachtDetail dispacht = null;

                    for (int i = 0; i < items.Length; i++)
                    {
                        dispacht = _repository.ExecuteScalarQuery<DispachtDetail>("SELECT ISNULL(LNSEQNBR, 0) LineId, ISNULL(DEPTMTID, '') DepartmentId, ISNULL(TRXLOCTN, '') WarehouseId FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 WHERE DOCNUMBR = '" + items[i].RequestId + "' AND ITEMNMBR = '" + items[i].ItemNumber + "'");
                        _repository.ExecuteCommand(String.Format("INTRANET.dbo.DispachtCreateDetail '{0}', '{1}', '{2}', {3}, '{4}', '{5}', {6}, {7}, '{8}', '{9}', '{10}', {11}, {12}, {13}, {14}, {15}, {16}, '{17}', '{18}', {19}, {20},'{21}','{22}'",
                        Helpers.InterCompanyId, "Despacho", secuencia, -1, items[i].ItemNumber, items[i].UnitId, items[i].QtyDispachted, 0,
                        dispacht.DepartmentId, dispacht.WarehouseId, "", items[i].QtyDispachted, items[i].QtyDispachted, 0, 0, 1, 0,
                        Account.GetAccount(User.Identity.GetUserName()).UserId, "LLIF10200", 1, dispacht.LineId, "Requisición", items[i].RequestId));
                    }

                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10100 SET DOCSTTS = 4, APPRVDTE = '" + DateTime.Now.ToString("yyyyMMdd") + "', APPRVDBY = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "', STTSDATE = '" + DateTime.Now.ToString("yyyyMMdd") + "', STTSUSRD = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "', USERID = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "', MDFUSRID = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "' WHERE DOCNUMBR = '" + secuencia + "'");
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10110 SET ITEMSTTS = 4 WHERE DOCNUMBR = '" + secuencia + "'");

                    status = "OK";
                }
                else { status = "ERROR"; }

            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        #endregion Despacho

        #region Devolucion

        public ActionResult ReturnIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "Return"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            string sqlQuery = "SELECT RTRIM(A.DOCNUMBR) DispachtId, A.SRCDOCNUM Description, A.DOCDATE DocumentDate, A.WORKNUMB RequestId, "
               + "B.DEPTDESC DepartmentId, A.TRXLOCTN WarehouseId, ISNULL(C.TEXT1, '') Note, "
               + "CASE A.DOCSTTS WHEN 1 THEN 'No enviado' WHEN 2 THEN 'Enviada' WHEN 3 THEN 'Aprobado' WHEN 4 THEN 'En Proceso' WHEN 5 THEN 'Anulado' ELSE 'Cerrada' END Status "
               + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
               + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 B "
               + "ON A.DEPTMTID = B.DEPTMTID "
               + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00140 C "
               + "ON A.DOCNUMBR = C.DOCNUMBR "
               + "WHERE A.DOCTYPE = 3 "
               + "ORDER BY A.DEX_ROW_ID DESC";

            var purchaseRequestList = _repository.ExecuteQuery<DispachtHeaderViewModel>(sqlQuery);

            return View(purchaseRequestList);
        }

        public ActionResult ReturnEdit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "Return"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            string sqlQuery = "SELECT RTRIM(A.ITEMNMBR) ItemNumber, RTRIM(B.ITEMDESC) ItemDescription, A.TRXQTY QtyDispachted, "
                + "RTRIM(A.UOFM) UnitId, RTRIM(A.TRXLOCTN) WarehouseId "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B "
                + "ON A.ITEMNMBR = B.ITEMNMBR "
                + "WHERE A.DOCNUMBR = '" + id + "'";

            var purchaseRequestItems = _repository.ExecuteQuery<DispachtDetailViewModel>(sqlQuery).ToList();

            sqlQuery = "SELECT DOCSTTS FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 WHERE DOCNUMBR = '" + id + "'";
            var status = _repository.ExecuteScalarQuery<short>(sqlQuery);

            sqlQuery = "SELECT RTRIM(A.DOCNUMBR) DispachtId, RTRIM(A.SRCDOCNUM) Description, RTRIM(A.WORKNUMB) RequestId, "
                + "A.DOCDATE DocumentDate, RTRIM(B.DEPTDESC) DepartmentId, RTRIM(A.TRXLOCTN) WarehouseId, A.USERID Requester, ISNULL(C.TEXT1, '') Note "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 B "
                + "ON A.DEPTMTID = B.DEPTMTID "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00140 C "
                + "ON A.DOCNUMBR = C.DOCNUMBR "
                + "WHERE A.DOCNUMBR = '" + id + "'";

            sqlQuery = "SELECT RTRIM(A.DOCNUMBR) DispachtId, RTRIM(A.SRCDOCNUM) Description, RTRIM(A.WORKNUMB) RequestId, "
                + "A.DOCDATE DocumentDate, RTRIM(B.DEPTDESC) DepartmentId, RTRIM(A.TRXLOCTN) WarehouseId, '' Requester, ISNULL(C.TEXT1, '') Note "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 B "
                + "ON A.DEPTMTID = B.DEPTMTID "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00140 C "
                + "ON A.DOCNUMBR = C.DOCNUMBR "
                + "WHERE A.DOCNUMBR = '" + id + "'";

            var purchaseRequest = _repository.ExecuteScalarQuery<DispachtHeaderViewModel>(sqlQuery);

            if (status != 4)
            {
                ViewBag.PurchaseRequestItems = purchaseRequestItems;
                return View("ReturnInquiry", purchaseRequest);
            }
            else
            {
                ViewBag.PurchaseRequestItems = purchaseRequestItems;
                return View(purchaseRequest);
            }
        }

        public ActionResult Return()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "Return"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            string sqlQuery = "SELECT DISTINCT A.DOCNUMBR DispachtId, B.WORKNUMB RequestId, C.DEPTDESC DepartmentId, B.DOCDATE DocumentDate "
               + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A WITH (NOLOCK) "
               + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF10100 B WITH (NOLOCK) "
               + "ON A.DOCTYPE = B.DOCTYPE AND A.DOCNUMBR = B.DOCNUMBR "
               + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 C WITH (NOLOCK) "
               + "ON B.DEPTMTID = C.DEPTMTID "
               + "WHERE A.DOCTYPE = 2 AND A.QTYRETN < A.QTYDSPT AND A.ITEMSTTS IN (4, 6) "
               + "AND A.DEPTMTID IN (SELECT DISTINCT DEPTMTID FROM " + Helpers.InterCompanyId + ".dbo.LLIF00101 WITH (NOLOCK)) ";

            var purchaseRequestList = _repository.ExecuteQuery<DispachtHeaderViewModel>(sqlQuery);

            return View(purchaseRequestList);
        }

        [OutputCache(Duration = 0)]
        public ActionResult ReturnDetails(string id)
        {
            if (id != null)
            {
                string sqlQuery = "";

                sqlQuery = "SELECT DISTINCT A.ITEMNMBR ItemNumber, B.ITEMDESC ItemDescription, A.TRXQTY QtyDispachted, A.UOFM UnitId, "
                         + "(CASE A.ITEMSTTS WHEN 1 THEN 'Nuevo' WHEN 2 THEN 'Enviado' WHEN 3 THEN 'Aprobado' WHEN 4 THEN 'Despachado' WHEN 5 THEN 'Anulado' ELSE 'Cerrado' END) Status "
                         + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                         + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B "
                         + "ON A.ITEMNMBR = B.ITEMNMBR "
                         + "WHERE A.DOCNUMBR = '" + id + "'";

                var requestDetail = _repository.ExecuteQuery<DispachtDetailViewModel>(sqlQuery).ToList();

                ViewBag.RequestDetail = requestDetail;
            }

            return PartialView();
        }

        [HttpPost]
        public JsonResult SaveReturn(DispachtHeaderViewModel request, int postType = 0)
        {
            string status = "";

            try
            {
                string originalNumber = _repository.ExecuteScalarQuery<string>
                ("SELECT TOP 1 ISNULL(ORIGNUMB, '') FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 WHERE RTRIM(DOCNUMBR) = '" + request.DispachtId + "' AND DOCTYPE = 3").Trim();

                if (request.Note != null)
                {
                    if (request.Note.Length > 0)
                    {
                        _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LLIF00140SI '{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}'",
                            Helpers.InterCompanyId, request.DispachtId, request.Note, "", "", "", "", "", "", "INIF00140"));
                    }
                }

                foreach (var i in request.DispachtLines)
                {
                    decimal originalLine = _repository.ExecuteScalarQuery<decimal>
                    ("SELECT TOP 1 ISNULL(ORLSQNBR, 0) FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 WHERE RTRIM(DOCNUMBR) = '" + request.DispachtId + "' AND RTRIM(ITEMNMBR) = '" + i.ItemNumber + "' AND DOCTYPE = 3 ");

                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.DeleteLogisticLine '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}'",
                    Helpers.InterCompanyId, "Devolución", request.DispachtId, i.ItemNumber, "LLIF10300", "LLIF10200", "Despacho", originalNumber));

                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.ReturnCreateDetail '{0}', '{1}', '{2}', {3}, '{4}', '{5}', {6}, {7}, '{8}', '{9}', '{10}', {11}, {12}, {13}, {14}, {15}, {16}, '{17}', '{18}', {19}, {20},'{21}','{22}'",
                    Helpers.InterCompanyId, "Devolución", request.DispachtId, -1, i.ItemNumber, i.UnitId, i.QtyDispachted, 0,
                    request.DepartmentId, request.WarehouseId, "", i.QtyDispachted, i.QtyDispachted, 0, 0, 1, 0,
                    Account.GetAccount(User.Identity.GetUserName()).UserId, "LLIF10300", 1, originalLine, "Despacho", originalNumber));
                }

                if (postType == 1)
                {
                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.TransferReturn '{0}','{1}','{2}','{3}','{4}',{5}",
                        Helpers.InterCompanyId, "Devolución", request.DispachtId, 6, Account.GetAccount(User.Identity.GetUserName()).UserId, "LLIF10300"));
                }
                else if (postType == 2)
                {
                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.TransferReturnDefault '{0}','{1}','{2}','{3}','{4}','{5}'",
                        Helpers.InterCompanyId, "Devolución", request.DispachtId, 6, Account.GetAccount(User.Identity.GetUserName()).UserId, "LLIF10300"));
                }

                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult DeleteReturn(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            DispachtHeaderViewModel dispacht = _repository.GetBy<DispachtHeaderViewModel>(id);
            if (dispacht == null)
            {
                return HttpNotFound();
            }
            return View(dispacht);
        }

        [HttpPost, ActionName("DeleteReturn")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteReturnConfirmed(string dispachtId)
        {
            _repository.ExecuteCommand(String.Format("INTRANET.dbo.VoidLogisticDocument '{0}','{1}','{2}','{3}','{4}','{5}'",
                Helpers.InterCompanyId, "Devolución", dispachtId, 5, Account.GetAccount(User.Identity.GetUserName()).UserId, "LLIF10300"));

            return RedirectToAction("ReturnIndex");
        }

        [HttpPost]
        public ActionResult GetItemDispacht(string dispachtId = "")
        {
            try
            {
                List<DispachtDetailViewModel> detail = null;
                string sqlQuery = "";

                sqlQuery = "SELECT CONVERT(int, (A.LNSEQNBR / 16384)) LineId, A.ITEMNMBR ItemNumber, B.ITEMDESC ItemDescription, CONVERT(NUMERIC(19,2), A.TRXQTY) QtyOrder, "
                    + " A.UOFM UnitId, CONVERT(NUMERIC(19,2), (A.QTYDSPT - A.QTYRETN)) QtyPending, C.BASEUOFM BaseUnitId "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV40201 C "
                    + "ON B.UOMSCHDL =  C.UOMSCHDL "
                    + "WHERE A.DOCNUMBR = '" + dispachtId + "'AND A.QTYRETN < A.TRXQTY ";

                detail = _repository.ExecuteQuery<DispachtDetailViewModel>(sqlQuery).ToList();
                return Json(detail, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public JsonResult GetQuantitiesReturn(string returnId = "", string itemNumber = "")
        {
            string status = "";
            try
            {
                decimal qtyOnHand = 0m;
                decimal qtyAllocated = 0m;
                decimal qtyPending = 0m;
                decimal qtyDispachted = 0m;

                string originalNumber = _repository.ExecuteScalarQuery<string>
                ("SELECT TOP 1 ISNULL(ORIGNUMB, '') FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 WHERE RTRIM(DOCNUMBR) = '" + returnId + "' AND DOCTYPE = 3").Trim();

                string quantity = "";
                string sqlQuery = "";

                sqlQuery = "SELECT CONVERT(NUMERIC(19,2), TRXQTY) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "WHERE A.DOCNUMBR = '" + returnId + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyDispachted = _repository.ExecuteScalarQuery<decimal>(sqlQuery);

                sqlQuery = "SELECT CONVERT(NUMERIC(19,2), ((A.TRXQTY) - QTYDSPT)) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "WHERE A.DOCNUMBR = '" + originalNumber + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyPending = _repository.ExecuteScalarQuery<decimal>(sqlQuery);
                qtyPending += qtyDispachted;

                sqlQuery = "SELECT ISNULL(B.QTYONHND, 0) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "WHERE A.DOCNUMBR = '" + returnId + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyOnHand = _repository.ExecuteScalarQuery<decimal>(sqlQuery);

                sqlQuery = "SELECT ISNULL(B.ATYALLOC, 0) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "WHERE A.DOCNUMBR = '" + returnId + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyAllocated = _repository.ExecuteScalarQuery<decimal>(sqlQuery);

                quantity = qtyOnHand.ToString("N2") + "-" + qtyAllocated.ToString("N2") + "-" + qtyPending.ToString("N2") + "-" + qtyDispachted.ToString("N2");

                status = quantity;
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult GenerateReturn(DispachtDetailViewModel[] items)
        {
            string status = "";

            try
            {
                if (items != null)
                {
                    string secuencia = HelperLogic.AsignaciónSecuencia("LLIF10300", Account.GetAccount(User.Identity.GetUserName()).UserId);
                    var request = _repository.ExecuteScalarQuery<DispachtDetail>("SELECT ISNULL(WORKNUMB, '') Request, ISNULL(DEPTMTID, '') DepartmentId, ISNULL(TRXLOCTN, '') WarehouseId FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 WHERE DOCNUMBR = '" + items.First().RequestId + "'");

                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.ReturnCreateHeader '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'",
                        Helpers.InterCompanyId, "Devolución", secuencia, DateTime.Now.ToString("yyyyMMdd"), request.DepartmentId, request.WarehouseId,
                        "", request.Request, Account.GetAccount(User.Identity.GetUserName()).UserId, "LLIF10300"));

                    HelperLogic.DesbloqueoSecuencia(secuencia, "LLIF10300", Account.GetAccount(User.Identity.GetUserName()).UserId);

                    DispachtDetail dispacht = null;

                    for (int i = 0; i < items.Length; i++)
                    {
                        dispacht = _repository.ExecuteScalarQuery<DispachtDetail>("SELECT ISNULL(LNSEQNBR, 0) LineId, ISNULL(DEPTMTID, '') DepartmentId, ISNULL(TRXLOCTN, '') WarehouseId FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 WHERE DOCNUMBR = '" + items.First().RequestId + "' AND ITEMNMBR = '" + items[i].ItemNumber + "'");
                        _repository.ExecuteCommand(String.Format("INTRANET.dbo.ReturnCreateDetail '{0}', '{1}', '{2}', {3}, '{4}', '{5}', {6}, {7}, '{8}', '{9}', '{10}', {11}, {12}, {13}, {14}, {15}, {16}, '{17}', '{18}', {19}, {20},'{21}','{22}'",
                        Helpers.InterCompanyId, "Devolución", secuencia, -1, items[i].ItemNumber, items[i].UnitId, items[i].QtyDispachted, 0,
                        dispacht.DepartmentId, dispacht.WarehouseId, "", items[i].QtyDispachted, items[i].QtyDispachted, 0, 0, 1, 0,
                        Account.GetAccount(User.Identity.GetUserName()).UserId, "LLIF10300", 1, dispacht.LineId, "Despacho", items[i].RequestId));
                    }

                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10100 SET DOCSTTS = 4, APPRVDTE = '" + DateTime.Now.ToString("yyyyMMdd") + "', APPRVDBY = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "', STTSDATE = '" + DateTime.Now.ToString("yyyyMMdd") + "', STTSUSRD = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "', USERID = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "', MDFUSRID = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "' WHERE DOCNUMBR = '" + secuencia + "'");
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10110 SET ITEMSTTS = 4 WHERE DOCNUMBR = '" + secuencia + "'");

                    status = "OK";
                }
                else { status = "ERROR"; }

            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        #endregion Devolucion

        public ActionResult StockReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "Stock"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult PrintStockReport(string filterFrom, string filterTo, int typeFilter, bool zeroQuantity, string date)
        {
            string status = "";
            try
            {
                status = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "Stock.pdf",
                String.Format("INTRANET.dbo.StockReport '{0}','{1}','{2}','{3}','{4}','{5}'",
                Helpers.InterCompanyId, typeFilter, filterFrom, filterTo, date, zeroQuantity == false ? 0 : 1), 9, ref status);
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult ReorderReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "Reorder"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult PrintReorderReport(string filterFrom, string filterTo, int typeFilter, int reportType)
        {
            string status = "";
            try
            {
                status = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "Reorder.pdf",
                String.Format("INTRANET.dbo.ReorderReport '{0}','{1}','{2}','{3}','{4}'",
                Helpers.InterCompanyId, typeFilter, filterFrom, filterTo, reportType), 10, ref status);
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult AttachFile(HttpPostedFileBase fileData, string documentNumber)
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
                string fileType = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].Split('.')[1].ToString();

                _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachmentInsert '{0}','{1}','{2}',{3},'{4}','{5}','{6}'",
                        Helpers.InterCompanyId, documentNumber, fileName, "0x" + BitConverter.ToString(fileStream).Replace("-", String.Empty),
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
        public ActionResult LoadAttachmentFiles(string documentNumber)
        {
            try
            {
                List<string> files = new List<string>();
                string sqlQuery = "SELECT RTRIM(fileName) FileName FROM " + Helpers.InterCompanyId + ".dbo.CO00105 WHERE DOCNUMBR = '" + documentNumber + "' AND DELETE1 = 0";
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

        [HttpPost]
        public ActionResult Print(string id, int docType)
        {
            string status = "";
            try
            {
                status = "OK";
                if (docType == 2)
                {
                    ReportHelper.Export(Helpers.ReportPath + "Despacho", Server.MapPath("~/PDF/Despacho/") + id + ".pdf",
                    String.Format("INTRANET.dbo.DispachtReport '{0}','{1}'",
                    Helpers.InterCompanyId, id), 5, ref status);
                }
                else
                {
                    ReportHelper.Export(Helpers.ReportPath + "Despacho", Server.MapPath("~/PDF/Despacho/") + id + ".pdf",
                    String.Format("INTRANET.dbo.ReturnReport '{0}','{1}'",
                    Helpers.InterCompanyId, id), 6, ref status);
                }
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult Download(string documentNumber, string FileName)
        {
            string sqlQuery = "SELECT BinaryBlob, FileType, A.fileName "
                + "FROM " + Helpers.InterCompanyId + ".dbo.CO00105 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.coAttachmentItems B ON A.Attachment_ID = B.Attachment_ID "
                + "WHERE A.DOCNUMBR = '" + documentNumber + "' AND A.DELETE1 = 0 AND RTRIM(A.fileName) = '" + FileName + "'";

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

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult GetAvailable(string requisitionId = "", string itemNumber = "")
        {
            try
            {
                decimal qtyPending = 0m;
                decimal qtyOnHand = 0m;

                bool status = true;
                string sqlQuery = "";

                sqlQuery = "SELECT ((A.TRXQTY) - A.QTYDSPT) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "WHERE A.DOCNUMBR = '" + requisitionId + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyPending = _repository.ExecuteScalarQuery<decimal>(sqlQuery);

                sqlQuery = "SELECT CONVERT(NUMERIC(19,2), CONVERT(int, ((B.QTYONHND - B.ATYALLOC) / (CASE ISNULL(D.QTYBSUOM, 0) WHEN 0 THEN 1 ELSE D.QTYBSUOM END)))) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 C "
                    + "ON A.ITEMNMBR = C.ITEMNMBR "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV40202 D "
                    + "ON C.UOMSCHDL = D.UOMSCHDL AND A.UOFM = D.UOFM "
                    + "WHERE A.DOCNUMBR = '" + requisitionId + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyOnHand = _repository.ExecuteScalarQuery<decimal>(sqlQuery);

                if (qtyOnHand < qtyPending)
                {
                    status = false;
                }
                return Json(status, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public JsonResult GetQuantities(string requisitionId = "", string itemNumber = "")
        {
            string status = "";
            try
            {
                decimal qtyOnHand = 0m;
                decimal qtyAllocated = 0m;

                string quantity = "";
                string sqlQuery = "";

                sqlQuery = "SELECT CONVERT(NUMERIC(19,2), CONVERT(int, ((B.QTYONHND) / (CASE ISNULL(D.QTYBSUOM, 0) WHEN 0 THEN 1 ELSE D.QTYBSUOM END)))) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 C "
                    + "ON A.ITEMNMBR = C.ITEMNMBR "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV40202 D "
                    + "ON C.UOMSCHDL = D.UOMSCHDL AND A.UOFM = D.UOFM "
                    + "WHERE A.DOCNUMBR = '" + requisitionId + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyOnHand = _repository.ExecuteScalarQuery<decimal>(sqlQuery);

                sqlQuery = "SELECT CONVERT(NUMERIC(19,2), CONVERT(int, ((B.ATYALLOC) / (CASE ISNULL(D.QTYBSUOM, 0) WHEN 0 THEN 1 ELSE D.QTYBSUOM END)))) "
                    + " FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.IV00102 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR AND A.TRXLOCTN = B.LOCNCODE "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 C "
                    + "ON A.ITEMNMBR = C.ITEMNMBR "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV40202 D "
                    + "ON C.UOMSCHDL = D.UOMSCHDL AND A.UOFM = D.UOFM "
                    + "WHERE A.DOCNUMBR = '" + requisitionId + "' AND A.ITEMNMBR = '" + itemNumber + "'";

                qtyAllocated = _repository.ExecuteScalarQuery<decimal>(sqlQuery);

                quantity = qtyOnHand.ToString("N2") + "-" + qtyAllocated.ToString("N2");

                status = quantity;
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult DepartmentIndex()
        {
            bool flag = !HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "Department");
            ActionResult result;
            if (flag)
            {
                result = base.RedirectToAction("NotPermission", "Home");
            }
            else
            {
                string query = string.Concat(new string[]
                {
                    "SELECT RTRIM(DEPTMTID) Id, RTRIM(DEPTDESC) Descripción, ISNULL(B.ACTALIAS, '000.0000.000') DataExtended FROM ",
                    Helpers.InterCompanyId,
                    ".dbo.LLIF00100 A LEFT JOIN ",
                    Helpers.InterCompanyId,
                    ".dbo.GL00100 B ON A.ACTINDX = B.ACTINDX ORDER BY DEPTMTID"
                });
                IEnumerable<Lookup> enumerable = this._repository.ExecuteQuery<Lookup>(query);
                result = base.View(enumerable);
            }
            return result;
        }

        public ActionResult DepartmentCreate()
        {
            bool flag = !HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "Department");
            ActionResult result;
            if (flag)
            {
                result = base.RedirectToAction("NotPermission", "Home");
            }
            else
            {
                result = base.View();
            }
            return result;
        }

        public ActionResult DepartmentEdit(string id)
        {
            bool flag = !HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "Department");
            ActionResult result;
            if (flag)
            {
                result = base.RedirectToAction("NotPermission", "Home");
            }
            else
            {
                string query = string.Concat(new string[]
                {
                    "SELECT RTRIM(DEPTMTID) Id, RTRIM(DEPTDESC) Descripción, ISNULL(B.ACTALIAS, '000.0000.000') DataExtended FROM ",
                    Helpers.InterCompanyId,
                    ".dbo.LLIF00100 A LEFT JOIN ",
                    Helpers.InterCompanyId,
                    ".dbo.GL00100 B ON A.ACTINDX = B.ACTINDX WHERE DEPTMTID = '",
                    id,
                    "' "
                });
                Lookup lookup = this._repository.ExecuteScalarQuery<Lookup>(query);
                result = base.View(lookup);
            }
            return result;
        }

        [HttpPost]
        public JsonResult SaveDepartment(Lookup department)
        {
            string status = "";
            try
            {
                this._repository.ExecuteCommand(string.Format("INTRANET.dbo.CreateDepartment '{0}', '{1}', '{2}', '{3}', '{4}'", new object[]
                {
                    Helpers.InterCompanyId,
                    department.Id,
                    department.Descripción,
                    department.DataExtended,
                    Account.GetAccount(User.Identity.GetUserName()).UserId
                }));
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }
            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult DeleteDepartment(string id)
        {
            Lookup lookup = new Lookup();
            string status = "";
            try
            {
                lookup = this._repository.ExecuteScalarQuery<Lookup>(string.Format("INTRANET.dbo.DeleteDepartment '{0}', '{1}'", Helpers.InterCompanyId, id));
                bool flag = lookup.Id == "0";
                if (flag)
                {
                    status = "OK";
                }
                else
                {
                    status = lookup.Descripción;
                }
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }
            return new JsonResult { Data = new { status } };
        }

        public ActionResult UserDepartment()
        {
            bool flag = !HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "UserDepartment");
            ActionResult result;
            if (flag)
            {
                result = base.RedirectToAction("NotPermission", "Home");
            }
            else
            {
                result = base.View();
            }
            return result;
        }

        [HttpPost]
        public JsonResult SaveUserDepartment(List<LineRow> lines)
        {
            string status = "";
            try
            {
                foreach (LogisticController.LineRow current in lines)
                {
                    this._repository.ExecuteCommand(string.Format("INTRANET.dbo.CreateUserDepartment '{0}', '{1}', '{2}', '{3}', '{4}'", new object[]
                    {
                        Helpers.InterCompanyId,
                        current.Id,
                        current.Descripción,
                        Convert.ToInt32(current.Estado),
                        Account.GetAccount(User.Identity.GetUserName()).UserId
                    }));
                }
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }
            return new JsonResult { Data = new { status } };
        }

        public ActionResult SiteDepartment()
        {
            bool flag = !HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "SiteDepartment");
            ActionResult result;
            if (flag)
            {
                result = base.RedirectToAction("NotPermission", "Home");
            }
            else
            {
                result = base.View();
            }
            return result;
        }

        [HttpPost]
        public JsonResult SaveSiteDepartment(List<LineRow> lines)
        {
            string status = "";
            try
            {
                foreach (LogisticController.LineRow current in lines)
                {
                    this._repository.ExecuteCommand(string.Format("INTRANET.dbo.CreateSiteDepartment '{0}', '{1}', '{2}', '{3}', '{4}'", new object[]
                    {
                        Helpers.InterCompanyId,
                        current.Id,
                        current.Descripción,
                        Convert.ToInt32(current.Estado),
                        Account.GetAccount(User.Identity.GetUserName()).UserId
                    }));
                }
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }
            return new JsonResult { Data = new { status } };
        }

        public ActionResult AccountDepartment()
        {
            bool flag = !HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Logistic", "AccountDepartment");
            ActionResult result;
            if (flag)
            {
                result = base.RedirectToAction("NotPermission", "Home");
            }
            else
            {
                result = base.View();
            }
            return result;
        }

        [HttpPost]
        public JsonResult SaveAccountDepartment(List<Lookup> lines)
        {
            string status = "";
            try
            {
                foreach (Lookup current in lines)
                {
                    this._repository.ExecuteCommand(string.Format("INTRANET.dbo.CreateAccountDepartment '{0}', '{1}', '{2}', '{3}', '{4}'",
                        Helpers.InterCompanyId, current.Id, current.Descripción, current.DataExtended, Account.GetAccount(User.Identity.GetUserName()).UserId));
                }
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }
            return new JsonResult { Data = new { status } };
        }

        [HttpPost, OutputCache(Duration = 0)]
        public JsonResult GetUserDepartments(string departmentId = "")
        {
            List<LogisticController.LineRow> status;
            try
            {
                string query = string.Concat(new string[]
                {
                    "SELECT RTRIM(A.USERID) Id, RTRIM(A.USERNAME) Descripción, CONVERT(bit, ISNULL(B.USERSTTS, 0)) Estado FROM DYNAMICS.dbo.SY01400 A WITH (NOLOCK) LEFT JOIN ",
                    Helpers.InterCompanyId,
                    ".dbo.LLIF00101 B WITH (NOLOCK) ON A.USERID = B.USERID AND B.DEPTMTID = '",
                    departmentId,
                    "'ORDER BY A.USERID"
                });
                status = this._repository.ExecuteQuery<LogisticController.LineRow>(query).ToList<LogisticController.LineRow>();
            }
            catch
            {
                return Json("");
            }
            return new JsonResult { Data = new { status } };
        }

        [HttpPost, OutputCache(Duration = 0)]
        public JsonResult GetSiteDepartments(string departmentId = "")
        {
            List<LineRow> status;
            try
            {
                var query = string.Concat(new string[]
                {
                    "SELECT RTRIM(A.LOCNCODE) Id, RTRIM(A.LOCNDSCR) Descripción, CONVERT(bit, ISNULL(B.ACCESSCK, 0)) Estado FROM ",
                    Helpers.InterCompanyId,
                    ".dbo.IV40700 A WITH (NOLOCK) LEFT JOIN ",
                    Helpers.InterCompanyId,
                    ".dbo.LLIF00104 B WITH (NOLOCK) ON A.LOCNCODE = B.LOCNCODE AND B.DEPTMTID = '",
                    departmentId,
                    "'ORDER BY A.LOCNCODE"
                });
                status = this._repository.ExecuteQuery<LineRow>(query).ToList<LineRow>();
            }
            catch
            {
                return Json("");
            }
            return new JsonResult { Data = new { status } };
        }

        [HttpPost, OutputCache(Duration = 0)]
        public JsonResult GetAccountDepartments(string departmentId = "")
        {
            List<Lookup> status;
            try
            {
                string query = string.Concat(new string[]
                {
                    "SELECT RTRIM(A.ITMCLSCD) Id, RTRIM(A.ITMCLSDC) Descripción, RTRIM(ISNULL(C.ACTNUMST, '''')) DataExtended FROM ",
                    Helpers.InterCompanyId,
                    ".dbo.IV40400 A WITH (NOLOCK) LEFT JOIN ",
                    Helpers.InterCompanyId,
                    ".dbo.LLIF00102 B WITH (NOLOCK) ON A.ITMCLSCD = B.ITMCLSCD AND B.DEPTMTID = '",
                    departmentId,
                    "'LEFT JOIN ",
                    Helpers.InterCompanyId,
                    ".dbo.GL00105 C WITH (NOLOCK) ON B.ACTINDX = C.ACTINDX ORDER BY A.ITMCLSCD"
                });
                status = this._repository.ExecuteQuery<Lookup>(query).ToList<Lookup>();
            }
            catch
            {
                return Json("");
            }
            return new JsonResult { Data = new { status } };
        }

        internal class DispachtDetail
        {
            public string Request { get; set; }
            public string DepartmentId { get; set; }
            public string WarehouseId { get; set; }
            public decimal LineId { get; set; }
        }

        public class LineRow
        {
            public string Id { get; set; }
            public string Descripción { get; set; }
            public bool Estado { get; set; }
        }
    }
}