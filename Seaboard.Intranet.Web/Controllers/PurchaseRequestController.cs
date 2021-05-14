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
using System.Threading.Tasks;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class PurchaseRequestController : Controller
    {
        readonly GenericRepository _repository;

        public PurchaseRequestController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseRequest", "Index"))
                return RedirectToAction("NotPermission", "Home");

            var sqlQuery = "SELECT DISTINCT A.DEPRTMID FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                              + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                              + "WHERE RTRIM(B.USERID) = '" + Account.GetAccount(User.Identity.GetUserName()).UserId +
                              "'";

            var filter = "";

            var departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

            foreach (var item in departments)
                if (filter.Length == 0)
                    filter = "'" + item + "'";
                else
                    filter += ",'" + item + "'";

            sqlQuery = "SELECT TOP 20 LTRIM(RTRIM(A.DOCNUMBR)) RequestId, LTRIM(RTRIM(A.WORKNUMB)) WorkNumber, RTRIM(A.PTDUSRID) Priority, RTRIM(A.SRCDOCNUM) Description, "
                + "A.POSTEDDT RequiredDate, A.DOCDATE DocumentDate, RTRIM(ISNULL(A.TRNSTLOC, '')) AR, ISNULL(A.USERID, '') Requester, "
                + "CASE A.DOCSTTS WHEN 1 THEN 'No enviado' WHEN 2 THEN 'Enviada' WHEN 3 THEN 'Aprobado' WHEN 4 THEN 'En Proceso' WHEN 5 THEN 'Anulado' WHEN 7 THEN 'Rechazado' ELSE 'Cerrada' END Status "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                + "WHERE A.DOCTYPE = 1 AND LOWER(RTRIM(A.DEPTMTID)) IN (" + filter + ") AND LEN(RTRIM(A.SRCDOCNUM)) > 0 "
                + "ORDER BY A.DEX_ROW_ID DESC";

            var purchaseRequestList = _repository.ExecuteQuery<PurchaseRequestViewModel>(sqlQuery);

            return View(purchaseRequestList);
        }

        public ActionResult List(string fromDate, string toDate)
        {
            var sqlQuery = "SELECT DISTINCT A.DEPRTMID FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                              + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                              + "WHERE RTRIM(B.USERID) = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "'";

            var filter = "";

            var departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

            foreach (var item in departments)
                if (filter.Length == 0)
                    filter = "'" + item + "'";
                else
                    filter += ",'" + item + "'";

            sqlQuery = "SELECT LTRIM(RTRIM(A.DOCNUMBR)) RequestId, LTRIM(RTRIM(A.WORKNUMB)) WorkNumber, RTRIM(A.PTDUSRID) Priority, RTRIM(A.SRCDOCNUM) Description, "
                + "A.POSTEDDT RequiredDate, A.DOCDATE DocumentDate, RTRIM(ISNULL(A.TRNSTLOC, '')) AR, ISNULL(A.USERID, '') Requester, "
                + "CASE A.DOCSTTS WHEN 1 THEN 'No enviado' WHEN 2 THEN 'Enviada' WHEN 3 THEN 'Aprobado' WHEN 4 THEN 'En Proceso' WHEN 5 THEN 'Anulado' WHEN 7 THEN 'Rechazado' ELSE 'Cerrada' END Status "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                + $"WHERE A.DOCTYPE = 1 AND LOWER(RTRIM(A.DEPTMTID)) IN ({filter}) AND LEN(RTRIM(A.SRCDOCNUM)) > 0 AND " +
                $"DOCDATE BETWEEN '{DateTime.ParseExact(fromDate, "MM/dd/yyyy", null).ToString("yyyyMMdd")}' AND '{DateTime.ParseExact(toDate, "MM/dd/yyyy", null).ToString("yyyyMMdd")}' "
                + "ORDER BY A.DEX_ROW_ID DESC";

            var purchaseRequestList = _repository.ExecuteQuery<PurchaseRequestViewModel>(sqlQuery);
            return PartialView("~/Views/PurchaseRequest/_List.cshtml", purchaseRequestList);
        }

        public ActionResult IndexWarehouse()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseRequest", "IndexWarehouse"))
                return RedirectToAction("NotPermission", "Home");

            var sqlQuery = "SELECT TOP 30 LTRIM(RTRIM(A.DOCNUMBR)) RequestId, RTRIM(A.PTDUSRID) Priority, RTRIM(A.SRCDOCNUM) Description, "
                + "A.POSTEDDT RequiredDate, A.DOCDATE DocumentDate, RTRIM(ISNULL(A.TRNSTLOC, '')) AR, ISNULL(A.USERID, '') Requester, "
                + "CASE A.DOCSTTS WHEN 1 THEN 'No enviado' WHEN 2 THEN 'Enviada' WHEN 3 THEN 'Aprobado' WHEN 4 THEN 'En Proceso' WHEN 5 THEN 'Anulado' WHEN 7 THEN 'Rechazado' ELSE 'Cerrada' END Status "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                + "WHERE A.DOCTYPE = 1 AND LEN(RTRIM(A.SRCDOCNUM)) > 0 AND LEN(RTRIM(WORKNUMB)) > 0 "
                + "ORDER BY A.DEX_ROW_ID DESC";

            var purchaseRequestList = _repository.ExecuteQuery<PurchaseRequestViewModel>(sqlQuery);

            return View(purchaseRequestList);
        }

        public ActionResult ListWarehouse(string fromDate, string toDate)
        {
            var sqlQuery = "SELECT LTRIM(RTRIM(A.DOCNUMBR)) RequestId, RTRIM(A.PTDUSRID) Priority, RTRIM(A.SRCDOCNUM) Description, "
                + "A.POSTEDDT RequiredDate, A.DOCDATE DocumentDate, RTRIM(ISNULL(A.TRNSTLOC, '')) AR, ISNULL(A.USERID, '') Requester, "
                + "CASE A.DOCSTTS WHEN 1 THEN 'No enviado' WHEN 2 THEN 'Enviada' WHEN 3 THEN 'Aprobado' WHEN 4 THEN 'En Proceso' WHEN 5 THEN 'Anulado' WHEN 7 THEN 'Rechazado' ELSE 'Cerrada' END Status "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                + "WHERE A.DOCTYPE = 1 AND LEN(RTRIM(A.SRCDOCNUM)) > 0 AND LEN(RTRIM(WORKNUMB)) > 0 AND "
                + $"DOCDATE BETWEEN '{DateTime.ParseExact(fromDate, "MM/dd/yyyy", null).ToString("yyyyMMdd")}' AND '{DateTime.ParseExact(toDate, "MM/dd/yyyy", null).ToString("yyyyMMdd")}' "
                + "ORDER BY A.DEX_ROW_ID DESC";

            var purchaseRequestList = _repository.ExecuteQuery<PurchaseRequestViewModel>(sqlQuery);

            return PartialView("~/Views/PurchaseRequest/_ListWarehouse.cshtml", purchaseRequestList);
        }

        public ActionResult Create()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseRequest",
                "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            var departmentCode = _repository.ExecuteScalarQuery<string>
            ("SELECT TOP 1 DEPRTMID FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 WHERE RTRIM(DEPRTMDS) = '" +
             Account.GetAccount(User.Identity.GetUserName()).Department + "'").Trim();

            var aprover = _repository.ExecuteScalarQuery<string>
                ("SELECT TOP 1 RTRIM(USERID) FROM " + Helpers.InterCompanyId +
                 ".dbo.LPPOP40101 WHERE RTRIM(DEPRTMID) = RTRIM('" + departmentCode + "') AND TYPE = 1 AND ISPRINC = 1")
                .Trim();

            ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;

            if (Account.GetAccount(User.Identity.GetUserName()).Department == "COMPRAS")
            {
                ViewBag.Aprover = "jhermida";
            }
            else
            {
                ViewBag.Aprover = aprover;
            }

            ViewBag.Priority = "Baja";
            ViewBag.PurchaseRequestId = HelperLogic.AsignaciónSecuencia("LLIF10100", Account.GetAccount(User.Identity.GetUserName()).UserId);

            return View();
        }

        [OutputCache(Duration = 0)]
        public ActionResult Details(string id)
        {
            if (id != null)
            {
                var sqlQuery = "SELECT TOP 1 ISNULL(WORKNUMB, '') FROM " + Helpers.InterCompanyId +
                                  ".dbo.LLIF10100 WHERE DOCNUMBR = '" + id + "'";

                var workNumber = _repository.ExecuteScalarQuery<string>(sqlQuery);

                sqlQuery = "SELECT TOP 1 A.SRCDOCNUM Description, A.DOCDATE DocumentDate, A.POSTEDDT RequiredDate, "
                           + "A.USERID Requester, B.DEPTDESC Department, A.APPRVDBY Aprover "
                           + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                           + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100  B "
                           + "ON A.DEPTMTID = B.DEPTMTID "
                           + "WHERE A.DOCNUMBR = '" + id + "' ";

                var requestAdditionalInformation =
                    _repository.ExecuteScalarQuery<PurchaseRequestAdditionalInfoViewModel>(sqlQuery);

                sqlQuery = "SELECT DISTINCT A.ITEMNMBR ItemId, B.ITEMDESC ItemDesc, A.TRXQTY Quantity, A.UOFM UnitId, "
                           + "(CASE A.ITEMSTTS WHEN 1 THEN 'Nuevo' WHEN 2 THEN 'Enviado' WHEN 3 THEN 'Aprobado' WHEN 4 THEN 'Despachado' WHEN 5 THEN 'Anulado' ELSE 'Cerrado' END) Status "
                           + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                           + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B "
                           + "ON A.ITEMNMBR = B.ITEMNMBR "
                           + "WHERE A.DOCNUMBR = '" + id + "'";

                var requestDetail = _repository.ExecuteQuery<PurchaseRequestLineViewModel>(sqlQuery).ToList();

                foreach (var item in requestDetail)
                {
                    if (item.Status != "Despachado")
                    {
                        if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                         ".dbo.POP10210 "
                                                         + "WHERE POPRequisitionNumber = '" + workNumber +
                                                         "' AND ITEMNMBR = '" + item.ItemId + "'").FirstOrDefault() > 0)
                        {
                            item.Status = "En requisicion";
                        }
                        if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                         ".dbo.POP30210 "
                                                         + "WHERE POPRequisitionNumber = '" + workNumber +
                                                         "' AND ITEMNMBR = '" + item.ItemId + "'").FirstOrDefault() > 0)
                        {
                            item.Status = "En requisicion";
                        }
                        if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                         ".dbo.LPPOP10200 "
                                                         + "WHERE PURCHREQ = '" + workNumber + "' AND ITEMNMBR = '" +
                                                         item.ItemId + "'").FirstOrDefault() > 0)
                        {
                            item.Status = "En cotizacion";
                        }
                        if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                         ".dbo.LPPOP10200 "
                                                         + "WHERE PURCHREQ = '" + workNumber + "' AND ITEMNMBR = '" +
                                                         item.ItemId + "'").FirstOrDefault() > 0)
                        {
                            item.Status = "En cotizacion";
                        }
                        if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                         ".dbo.LPPOP20200 "
                                                         + "WHERE PURCHREQ = '" + workNumber + "' AND ITEMNMBR = '" +
                                                         item.ItemId + "'").FirstOrDefault() > 0)
                        {
                            item.Status = "En pre-analisis";
                        }
                        if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                         ".dbo.LPPOP30200 A "
                                                         + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP30100 B "
                                                         + "ON A.ANLREQUS = B.ANLREQUS "
                                                         + "WHERE B.PURCHREQ = '" + workNumber +
                                                         "' AND A.ITEMNMBR = '" + item.ItemId + "' AND B.VOIDED = 0")
                                .FirstOrDefault() > 0)
                        {
                            item.Status = "En analisis";
                        }

                        if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                         ".dbo.LPPOP30104 A "
                                                         + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.POP10100 B "
                                                         + "ON A.PONUMBE = B.PONUMBER "
                                                         + "WHERE A.PURCHREQ = '" + workNumber +
                                                         "' AND A.ITEMNMBR = '" + item.ItemId + "' AND B.POSTATUS <> 2")
                                .FirstOrDefault() > 0)
                        {
                            item.Status = "En compras";
                        }
                        if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                         ".dbo.POP10310 A "
                                                         + "INNER JOIN " + Helpers.InterCompanyId +
                                                         ".dbo.LPPOP30104 B ON A.PONUMBER = B.PONUMBE AND A.ITEMNMBR = B.ITEMNMBR "
                                                         + "WHERE B.PURCHREQ = '" + workNumber +
                                                         "' AND A.ITEMNMBR = '" + item.ItemId + "'").FirstOrDefault() >
                            0)
                        {
                            item.Status = "Recibido";
                        }
                        if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +
                                                         ".dbo.POP30310 A "
                                                         + "INNER JOIN " + Helpers.InterCompanyId +
                                                         ".dbo.LPPOP30104 B ON A.PONUMBER = B.PONUMBE AND A.ITEMNMBR = B.ITEMNMBR "
                                                         + "WHERE B.PURCHREQ = '" + workNumber +
                                                         "' AND A.ITEMNMBR = '" + item.ItemId + "'").FirstOrDefault() >
                            0)
                        {
                            item.Status = "Recibido";
                        }
                    }

                    ViewBag.RequestDetail = requestDetail;
                    ViewBag.AdditionalInformation = requestAdditionalInformation;
                }
            }

            return PartialView();
        }

        [HttpPost]
        public JsonResult SavePurchaseRequest(PurchaseRequest request, int postType = 0)
        {
            var status = "";

            try
            {
                status = "OK";
                if (Account.GetAccount(User.Identity.GetUserName()).Department == "ALMACEN")
                {
                    var order = 16384;
                    var lineNumber = 1;
                    _repository.ExecuteCommand(String.Format(
                        "INTRANET.dbo.PurchRequestInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}'",
                        Helpers.InterCompanyId, request.PurchaseRequestId, request.Description, request.Note,
                        request.DocumentDate, request.RequiredDate,
                        request.Priority, request.DepartmentId, request.AR, request.Approver, request.Requester));

                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.PurchRequestLineDelete '{0}','{1}'",
                        Helpers.InterCompanyId, request.PurchaseRequestId));

                    foreach (var i in request.PurchaseRequestLines)
                    {
                        _repository.ExecuteCommand(
                            String.Format("INTRANET.dbo.PurchRequestLineInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}'",
                                Helpers.InterCompanyId, request.PurchaseRequestId, order, lineNumber, i.ItemId,
                                i.ItemDescription, i.UnitId, i.Warehouse, i.Quantity, i.AccountNum, i.Charge));

                        order += 16384;
                        lineNumber += 1;
                    }

                    if (postType == 1)
                    {
                        _repository.ExecuteCommand(string.Format("LODYNDEV.dbo.LPWF00101SI '{0}','{1}','{2}','{3}','{4}'",
                                Helpers.InterCompanyId, request.PurchaseRequestId, request.Description, 1, 1));

                        _repository.ExecuteCommand(string.Format("LODYNDEV.dbo.LPWF00201SI '{0}','{1}','{2}','{3}','{4}'",
                                Helpers.InterCompanyId, request.PurchaseRequestId,
                                Account.GetAccount(User.Identity.GetUserName()).UserId, "", 4));

                        Task.Run(() => ProcessLogic.SendToSharepointAsync(request.PurchaseRequestId, 1, Account.GetAccount(User.Identity.GetUserName()).Email));
                        //ProcessLogic.SendToSharepoint(request.PurchaseRequestId, 1, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
                    }
                }
                else
                {
                    var departmentCode = _repository.ExecuteScalarQuery<string>
                        ("SELECT TOP 1 DEPRTMID FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 WHERE RTRIM(DEPRTMDS) = '" + request.DepartmentId + "'").Trim();

                    _repository.ExecuteCommand("DELETE FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 WHERE DOCNUMBR = '" + request.PurchaseRequestId + "'");
                    _repository.ExecuteCommand("DELETE FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 WHERE DOCNUMBR = '" + request.PurchaseRequestId + "'");

                    _repository.ExecuteCommand("LODYNDEV.dbo.LLIF10100SI @INTERID = '" + Helpers.InterCompanyId + "', "
                                               + "@DOCID = 'Requisición', @DOCNUMBR = '" + request.PurchaseRequestId +
                                               "', @DOCDATE = '" + request.DocumentDate.ToString("yyyyMMdd") + "', "
                                               + "@DEPTMTID = '" + departmentCode + "', @TRXLOCTN = '" +
                                               request.PurchaseRequestLines.First().Warehouse + "', "
                                               + "@WORKNUMB = '', @USERID = '" + request.Requester +
                                               "', @DYNDVFRM = 'LLIF10100'");

                    var sqlQuery = "UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10100 SET POSTEDDT = '" +
                                   request.RequiredDate.ToString("yyyyMMdd") + "', "
                                   + "PTDUSRID = '" + request.Priority + "', TRNSTLOC = '" + request.AR +
                                   "', APPRVDBY = '" + request.Approver + "', "
                                   + "SRCDOCNUM = '" + request.Description + "' "
                                   + "WHERE DOCNUMBR = '" + request.PurchaseRequestId + "'";

                    _repository.ExecuteCommand(sqlQuery);

                    if (request.Note?.Length > 0)
                    {
                        _repository.ExecuteCommand(string.Format("LODYNDEV.dbo.LLIF00140SI '{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}'",
                            Helpers.InterCompanyId, request.PurchaseRequestId, request.Note, "", "", "", "", "", "", "INIF00140"));
                    }
                    else
                        _repository.ExecuteCommand("DELETE " + Helpers.InterCompanyId + ".dbo.LLIF00140 WHERE DOCNUMBR = '" + request.PurchaseRequestId + "'");

                    foreach (var item in request.PurchaseRequestLines)
                    {
                        _repository.ExecuteCommand("LODYNDEV.dbo.LLIF10110SI '" + Helpers.InterCompanyId +
                                                   "', 'Requisición', "
                                                   + "'" + request.PurchaseRequestId + "', -1, '" + item.ItemId +
                                                   "', '" +
                                                   item.UnitId + "', "
                                                   + "" + item.Quantity + ", 0, '" + departmentCode + "', '" +
                                                   item.Warehouse +
                                                   "', '', 0, 0, 0, 0, 1, 0, "
                                                   + "'" + Account.GetAccount(User.Identity.GetUserName()).UserId +
                                                   "', 'LLIF10100'");

                        sqlQuery = "UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10110 SET TRNSTLOC = '" + item.Charge + "', " + "ORIGNUMB = '" + item.AccountNum + "' "
                                   + "WHERE DOCNUMBR = '" + request.PurchaseRequestId + "' AND ITEMNMBR = '" + item.ItemId + "'";
                        _repository.ExecuteCommand(sqlQuery);
                    }

                    if (postType == 1)
                    {
                        _repository.ExecuteCommand(string.Format("LODYNDEV.dbo.LPWF00101SI '{0}','{1}','{2}','{3}','{4}'",
                            Helpers.InterCompanyId, request.PurchaseRequestId, request.Description, 1, 4));

                        _repository.ExecuteCommand(string.Format("LODYNDEV.dbo.LPWF00201SI '{0}','{1}','{2}','{3}','{4}'",
                            Helpers.InterCompanyId, request.PurchaseRequestId, Account.GetAccount(User.Identity.GetUserName()).UserId, "", 1));

                        _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10100 SET DOCSTTS = 2 WHERE DOCNUMBR = '" + request.PurchaseRequestId + "'");
                        _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10110 SET ITEMSTTS = 2 WHERE DOCNUMBR = '" + request.PurchaseRequestId + "'");
                        Task.Run(() => ProcessLogic.SendToSharepointAsync(request.PurchaseRequestId, 4, Account.GetAccount(User.Identity.GetUserName()).Email));
                        //ProcessLogic.SendToSharepoint(request.PurchaseRequestId, 4, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
                    }
                }
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult CheckChange(PurchaseRequest request)
        {
            bool isChange = false;
            try
            {
                var sqlQuery = "SELECT RTRIM(A.DOCNUMBR) PurchaseRequestId, RTRIM(A.SRCDOCNUM) Description, "
                       + "RTRIM(A.USERID) Requester, A.DOCDATE DocumentDate, A.POSTEDDT RequiredDate, RTRIM(A.PTDUSRID) Priority, "
                       + "RTRIM(D.DEPTDESC) DepartmentId, RTRIM(A.APPRVDBY) Approver, RTRIM(A.TRNSTLOC) AR, "
                       + "ISNULL(C.TEXT1, '') Note, '" + Helpers.InterCompanyId + "' INTERID "
                       + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 B "
                       + "ON A.DEPTMTID = B.DEPTMTID "
                       + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00140 C "
                       + "ON A.DOCNUMBR = C.DOCNUMBR "
                       + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 D "
                       + "ON A.DEPTMTID = D.DEPTMTID "
                       + "WHERE A.DOCNUMBR = '" + request.PurchaseRequestId + "'";

                var purchaseRequest = _repository.ExecuteScalarQuery<PurchaseRequest>(sqlQuery);

                if (purchaseRequest.Description != request.Description)
                    isChange = true;
                if (purchaseRequest.DepartmentId != request.DepartmentId)
                    isChange = true;
                if (purchaseRequest.Priority != request.Priority)
                    isChange = true;
                if (purchaseRequest.AR != request.AR)
                    isChange = true;
                if (purchaseRequest.Note != request.Note)
                    isChange = true;
                sqlQuery = "SELECT RTRIM(A.ITEMNMBR) ItemId, RTRIM(B.ITEMDESC) ItemDescription, A.TRXQTY Quantity, "
                              + "RTRIM(A.UOFM) UnitId, RTRIM(A.TRXLOCTN) Warehouse, RTRIM(A.ORIGNUMB) AccountNum, RTRIM(A.TRNSTLOC) Charge "
                              + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                              + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B "
                              + "ON A.ITEMNMBR = B.ITEMNMBR "
                              + "WHERE A.DOCNUMBR = '" + request.PurchaseRequestId + "'";

                var purchaseRequestItems = _repository.ExecuteQuery<RequisitionLineViewModel>(sqlQuery).ToList();
                if (purchaseRequestItems.Count != request.PurchaseRequestLines.Count)
                    isChange = true;
            }
            catch
            {
                isChange = false;
            }

            return new JsonResult { Data = new { change = isChange } };
        }

        public ActionResult Edit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseRequest", "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            var sqlQuery = "SELECT RTRIM(A.ITEMNMBR) ItemId, RTRIM(B.ITEMDESC) ItemDescription, A.TRXQTY Quantity, "
                              + "RTRIM(A.UOFM) UnitId, RTRIM(A.TRXLOCTN) Warehouse, RTRIM(A.ORIGNUMB) AccountNum, RTRIM(A.TRNSTLOC) Charge "
                              + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                              + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B "
                              + "ON A.ITEMNMBR = B.ITEMNMBR "
                              + "WHERE A.DOCNUMBR = '" + id + "'";

            var purchaseRequestItems = _repository.ExecuteQuery<RequisitionLineViewModel>(sqlQuery).ToList();

            sqlQuery = "SELECT DOCSTTS FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 WHERE DOCNUMBR = '" + id + "'";
            var status = _repository.ExecuteScalarQuery<short>(sqlQuery);

            sqlQuery = "SELECT RTRIM(A.DOCNUMBR) PurchaseRequestId, RTRIM(A.SRCDOCNUM) Description, "
                       + "RTRIM(A.USERID) Requester, A.DOCDATE DocumentDate, A.POSTEDDT RequiredDate, RTRIM(A.PTDUSRID) Priority, "
                       + "RTRIM(D.DEPTDESC) DepartmentId, RTRIM(A.APPRVDBY) Approver, RTRIM(A.TRNSTLOC) AR, "
                       + "ISNULL(C.TEXT1, '') Note, '" + Helpers.InterCompanyId + "' INTERID "
                       + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 B "
                       + "ON A.DEPTMTID = B.DEPTMTID "
                       + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00140 C "
                       + "ON A.DOCNUMBR = C.DOCNUMBR "
                       + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 D "
                       + "ON A.DEPTMTID = D.DEPTMTID "
                       + "WHERE A.DOCNUMBR = '" + id + "'";

            var purchaseRequest = _repository.ExecuteScalarQuery<PurchaseRequest>(sqlQuery);

            var inWork = false;
            var voided = _repository.ExecuteScalarQuery<short>("SELECT DOCSTTS FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 WHERE DOCNUMBR = '" + id + "'") == 5;
            if (voided)
                inWork = true;
            else
                inWork = _repository.ExecuteScalarQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 WHERE DOCNUMBR = '" + id + "' AND LEN(WORKNUMB) > 0") != 0;

            if (inWork)
            {
                ViewBag.PurchaseRequestItems = purchaseRequestItems;
                ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;
                ViewBag.Status = status.ToString();
                ViewBag.PurchaseRequestId = purchaseRequest.PurchaseRequestId;
                return View("Inquiry", purchaseRequest);
            }
            else
            {

                ViewBag.PurchaseRequestItems = purchaseRequestItems;
                ViewBag.Status = status.ToString();
                ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;
                ViewBag.PurchaseRequestId = purchaseRequest.PurchaseRequestId;
                return View(purchaseRequest);
            }
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var purchaseRequest = _repository.GetBy<PurchaseRequest>(id);
            if (purchaseRequest == null)
            {
                return HttpNotFound();
            }
            return View(purchaseRequest);
        }

        public ActionResult DispatchRequest(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseRequest", "Dispatch"))
                return RedirectToAction("NotPermission", "Home");

            var sqlQuery = "SELECT RTRIM(A.DOCNUMBR) PurchaseRequestId, RTRIM(A.SRCDOCNUM) Description, "
                       + "RTRIM(A.USERID) Requester, A.DOCDATE DocumentDate, A.POSTEDDT RequiredDate, RTRIM(A.PTDUSRID) Priority, "
                       + "RTRIM(D.DEPTDESC) DepartmentId, RTRIM(A.APPRVDBY) Approver, RTRIM(A.TRNSTLOC) AR, "
                       + "ISNULL(C.TEXT1, '') Note, '" + Helpers.InterCompanyId + "' INTERID "
                       + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 B "
                       + "ON A.DEPTMTID = B.DEPTMTID "
                       + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00140 C "
                       + "ON A.DOCNUMBR = C.DOCNUMBR "
                       + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 D "
                       + "ON A.DEPTMTID = D.DEPTMTID "
                       + "WHERE A.DOCNUMBR = '" + id + "'";

            var purchaseRequest = _repository.ExecuteScalarQuery<PurchaseRequest>(sqlQuery);

            sqlQuery = "SELECT RTRIM(A.ITEMNMBR) ItemId, RTRIM(B.ITEMDESC) ItemDesc, RTRIM(A.UOFM) UnitId, RTRIM(A.TRXLOCTN) Warehouse, ABS((A.TRXQTY - A.QTYDSPT)) Quantity "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B "
                + "ON A.ITEMNMBR = B.ITEMNMBR "
                + "WHERE A.DOCNUMBR = '" + id + "' AND A.ITEMSTTS = 3 ";
            ViewBag.Items = _repository.ExecuteQuery<Item>(sqlQuery);

            return View(purchaseRequest);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string purchaseRequestId)
        {
            _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId +
                                      ".dbo.LLIF10100 SET DOCSTTS = 5 WHERE DOCNUMBR = '" + purchaseRequestId + "'");
            _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId +
                                      ".dbo.LLIF10110 SET ITEMSTTS = 5 WHERE DOCNUMBR = '" + purchaseRequestId + "'");

            return RedirectToAction("Index");
        }

        public JsonResult UnblockSecuence(string secuencia, string formulario, string usuario)
        {
            HelperLogic.DesbloqueoSecuencia(secuencia, "LLIF10100",Account.GetAccount(User.Identity.GetUserName()).UserId);
            return Json("");
        }

        public ActionResult UnDispachtRequest()
        {

            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseRequest",
                "Dispatch"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            var sqlQuery =
                "SELECT RTRIM(A.DOCNUMBR) RequestId, A.DOCDATE DocumentDate, A.POSTEDDT RequiredDate, RTRIM(B.DEPTDESC) DepartmentId, "
                + "RTRIM(A.USERID) Requester, RTRIM(A.SRCDOCNUM) Description, A.PTDUSRID Priority "
                + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 B "
                + "ON A.DEPTMTID = B.DEPTMTID "
                + "WHERE DOCNUMBR IN (SELECT DOCNUMBR FROM " + Helpers.InterCompanyId +
                ".dbo.LLIF10110 WHERE ITEMSTTS = 3) AND LEN(RTRIM(A.WORKNUMB)) = 0 ";

            var undispachtRequest = _repository.ExecuteQuery<UnDispachtRequestViewModel>(sqlQuery);

            return View(undispachtRequest);
        }

        [HttpPost]
        public JsonResult ProcessPurchaseRequest(DispatchRequestViewModel[] items)
        {
            string status;

            try
            {
                if (items != null)
                {
                    var secuencia =HelperLogic.AsignaciónSecuencia("POP10200",Account.GetAccount(User.Identity.GetUserName()).UserId);
                    _repository.ExecuteCommand(string.Format("INTRANET.dbo.PurchRequestCreateHeader '{0}','{1}','{2}','{3}'",
                        Helpers.InterCompanyId, items.First().PurchaseRequestId,Account.GetAccount(User.Identity.GetUserName()).UserId, secuencia));
                    HelperLogic.DesbloqueoSecuencia(secuencia, "POP10200",Account.GetAccount(User.Identity.GetUserName()).UserId);

                    var contador = 1;
                    var orden = 16384;
                    foreach (var item in items)
                    {
                        _repository.ExecuteCommand(string.Format("INTRANET.dbo.PurchRequestCreateDetail '{0}','{1}','{2}','{3}','{4}','{5}'",
                            Helpers.InterCompanyId, item.PurchaseRequestId, secuencia, item.ItemId,contador, orden));
                        contador++;
                        orden += 16384;
                    }
                    status = "OK";
                }
                else
                    status = "ERROR";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult ClosePurchaseRequest(string id)
        {
            string xStatus;
            try
            {
                if (id != null)
                {
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10100 SET DOCSTTS = 6, WORKNUMB = 'N/A' WHERE DOCNUMBR = '" + id + "'");
                    _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10110 SET ITEMSTTS = 6 WHERE DOCNUMBR = '" + id + "'");
                    xStatus = "OK";
                }
                else
                    xStatus = "ERROR";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult SendWorkFlow(string purchaseRequestId)
        {
            var status = "";

            try
            {
                status = "OK";
                _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPWF00101SI '{0}','{1}','{2}','{3}','{4}'", Helpers.InterCompanyId, purchaseRequestId, "", 1, 4));
                _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPWF00201SI '{0}','{1}','{2}','{3}','{4}'",
                    Helpers.InterCompanyId, purchaseRequestId, Account.GetAccount(User.Identity.GetUserName()).UserId, "", 1));

                _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10100 SET DOCSTTS = 2 WHERE DOCNUMBR = '" + purchaseRequestId + "'");
                _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.LLIF10110 SET ITEMSTTS = 2 WHERE DOCNUMBR = '" + purchaseRequestId + "'");

                Task.Run(() => ProcessLogic.SendToSharepointAsync(purchaseRequestId, 4, Account.GetAccount(User.Identity.GetUserName()).Email));
                //ProcessLogic.SendToSharepoint(purchaseRequestId, 4, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult AttachFile(HttpPostedFileBase fileData, string requestId)
        {
            var status = false;

            try
            {
                byte[] fileStream = null;
                using (var binaryReader = new BinaryReader(fileData.InputStream))
                    fileStream = binaryReader.ReadBytes(fileData.ContentLength);

                var fileName = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1];
                var fileType = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].Split('.')[1];

                _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachmentInsert '{0}','{1}','{2}',{3},'{4}','{5}','{6}'",
                    Helpers.InterCompanyId, requestId, fileName, "0x" + BitConverter.ToString(fileStream).Replace("-", String.Empty),
                    fileType, Account.GetAccount(User.Identity.GetUserName()).UserId, "REQ"));
                status = true;
            }
            catch
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
        public ActionResult LoadAttachmentFiles(string purchaseRequestId)
        {
            try
            {
                var files = new List<string>();
                var sqlQuery = "SELECT RTRIM(fileName) FileName FROM " + Helpers.InterCompanyId + ".dbo.CO00105 WHERE DOCNUMBR = '" + purchaseRequestId + "' AND DELETE1 = 0";
                files = _repository.ExecuteQuery<string>(sqlQuery).ToList();
                return Json(files, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public ActionResult ListRequests(string purchaseRequestId)
        {
            try
            {
                var files = new List<Lookup>();
                var sqlQuery =
                    "SELECT RTRIM(A.POPRequisitionNumber) Id, RTRIM(A.RequisitionDescription) Descripción, '' DataExtended FROM "
                    + "( "
                    + "SELECT POPRequisitionNumber, RequisitionDescription, USERDEF2 FROM " + Helpers.InterCompanyId +
                    ".dbo.POP10200 "
                    + "UNION ALL "
                    + "SELECT POPRequisitionNumber, RequisitionDescription, USERDEF2 FROM " + Helpers.InterCompanyId +
                    ".dbo.POP30200 "
                    + ") A "
                    + "WHERE A.USERDEF2 = '" + Account.GetAccount(User.Identity.GetUserName()).Department + "'"
                    + "ORDER BY A.POPRequisitionNumber ";
                files = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();
                return Json(files, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public ActionResult ListItems(string id)
        {
            try
            {
                var files = new List<RequisitionLineViewModel>();
                var sqlQuery =
                    "SELECT RTRIM(A.ITEMNMBR) ItemId, RTRIM(B.ITEMDESC) ItemDescription, CONVERT(NUMERIC(32,2), A.TRXQTY) Quantity, "
                    + "RTRIM(A.UOFM) UnitId, RTRIM(A.ORIGNUMB) AccountNum, RTRIM(A.TRNSTLOC) Charge, RTRIM(A.TRXLOCTN) Warehouse "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10110 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV00101 B "
                    + "ON A.ITEMNMBR = B.ITEMNMBR "
                    + "WHERE A.DOCNUMBR = '" + id + "' "
                    + "ORDER BY A.DOCNUMBR ";
                files = _repository.ExecuteQuery<RequisitionLineViewModel>(sqlQuery).ToList();
                return Json(files, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public ActionResult GetAccountNum(string itemId)
        {
            try
            {
                var accountNum = "";

                var itemClass =
                    _repository.ExecuteScalarQuery<string>("SELECT TOP 1 ISNULL(ITMCLSCD, '') FROM " +
                                                          Helpers.InterCompanyId + ".dbo.IV00101 WHERE ITEMNMBR = '" +
                                                          itemId + "'");
                var count = 0;
                var departmentCode =
                    _repository.ExecuteScalarQuery<string>("SELECT TOP 1 ISNULL(DEPRTMID, '') FROM " +
                                                          Helpers.InterCompanyId +
                                                          ".dbo.LPPOP40100 WHERE DEPRTMDS = '" +
                                                          Account.GetAccount(User.Identity.GetUserName()).Department +
                                                          "'");
                var sqlQuery = "SELECT COUNT(*) "
                                  + "FROM " + Helpers.InterCompanyId + ".dbo.GL00100 A "
                                  + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00102 B "
                                  + "ON A.ACTINDX = B.ACTINDX "
                                  + "WHERE B.DEPTMTID = '" + departmentCode + "' AND ITMCLSCD = '" + itemClass + "'";
                count = _repository.ExecuteScalarQuery<int>(sqlQuery);

                if (count > 0)
                {
                    sqlQuery =
                        "SELECT TOP 1 RTRIM(A.ACTNUMBR_1) + '-' + RTRIM(A.ACTNUMBR_2) + '-' + RTRIM(A.ACTNUMBR_3) "
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

                return Json(accountNum, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public ActionResult Print(string id)
        {
            var status = "";
            var isTwoAprovers = false;
            try
            {
                var solicitudes = new List<short>();

                var sqlQuery = "SELECT ISNULL(A.WFSTS, 0) FROM " + Helpers.InterCompanyId + ".dbo.LPWF00201 A "
                                  + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPWF00101 B "
                                  + "ON A.DOCNUM = B.DOCNUM "
                                  + "WHERE A.DOCNUM = '" + id + "' AND TYPE = 4";

                solicitudes = _repository.ExecuteQuery<short>(sqlQuery).ToList();

                sqlQuery = "SELECT APPRVDBY APROBADOR "
                           + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 "
                           + "WHERE DOCNUMBR = '" + id.Trim() + "'";

                var aprobador = _repository.ExecuteScalarQuery<string>(sqlQuery);

                #region Imagen

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
                    HelperLogic.InsertSignature(aprobador.Trim());
                    HelperLogic.InsertSignaturePayment(HelperLogic.GetSecondAproverPayment());
                }
                else
                {
                    if (solicitudes != null)
                    {
                        foreach (var item in solicitudes)
                        {
                            if (item == 2)
                            {
                                isTwoAprovers = true;
                                break;
                            }
                        }
                    }

                    if (isTwoAprovers)
                    {
                        HelperLogic.InsertSignature(aprobador.Trim());
                        HelperLogic.InsertSignaturePayment("");
                    }
                    else
                    {
                        HelperLogic.InsertSignature("");
                        HelperLogic.InsertSignaturePayment("");
                    }
                }

                #endregion

                status = "OK";

                ReportHelper.Export(Helpers.ReportPath + "Requisicion",
                    Server.MapPath("~/PDF/Requisicion/") + id + ".pdf",
                    String.Format("LODYNDEV.dbo.LPPOP10200R2 '{0}','{1}'",
                        Helpers.InterCompanyId, id), 1, ref status);


            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public ActionResult DeleteFile(string id, string fileName)
        {
            var status = "";
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

        public ActionResult Download(string purchaseRequestId, string FileName)
        {
            var sqlQuery = "SELECT BinaryBlob, FileType, A.fileName "
                              + "FROM " + Helpers.InterCompanyId + ".dbo.CO00105 A "
                              + "INNER JOIN " + Helpers.InterCompanyId +
                              ".dbo.coAttachmentItems B ON A.Attachment_ID = B.Attachment_ID "
                              + "WHERE A.DOCNUMBR = '" + purchaseRequestId +
                              "' AND A.DELETE1 = 0 AND RTRIM(A.fileName) = '" + FileName + "'";

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

        public ActionResult AmountItem()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "PurchaseRequest",
                "AmountItem"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            var sqlQuery = "SELECT IV1.[ITEMNMBR] AS [ArticleNum],"
                              + " IV3.[ITEMDESC] AS [ArticleDes], "
                              + " IV1.[ORDRPNTQTY] AS [OrderPointquant],"
                              + " IV1.[QTYONORD] AS [OrderQuantity],"
                              + " IV1.[QTYONHND] AS [ExistingAmount],"
                              + " IV1.[ORDRUPTOLVL] AS [AskUptolevel] "
                              + " FROM " + Helpers.InterCompanyId + ".dbo.[IV00102] IV1 "
                              + " LEFT JOIN " + Helpers.InterCompanyId +
                              ".dbo.[IV00103] IV2 ON IV1.[PRIMVNDR] = IV2.[VENDORID] and IV1.[ITEMNMBR] = IV2.[ITEMNMBR] "
                              + " LEFT JOIN " + Helpers.InterCompanyId +
                              ".dbo.[PM00200] PM  ON IV1.[PRIMVNDR] = PM.[VENDORID] "
                              + " LEFT JOIN " + Helpers.InterCompanyId +
                              ".dbo.[IV00101] IV3 ON IV1.[ITEMNMBR] = IV3.[ITEMNMBR] "
                              + " LEFT JOIN " + Helpers.InterCompanyId +
                              ".dbo.[IV40201] IV4 ON IV3.[UOMSCHDL] = IV4.[UOMSCHDL]"
                              + " WHERE IV1.[ORDRPNTQTY] > 0 ";

            var amountItem = _repository.ExecuteQuery<AmountOfItemViewModel>(sqlQuery);

            return View(amountItem);
        }
    }
}