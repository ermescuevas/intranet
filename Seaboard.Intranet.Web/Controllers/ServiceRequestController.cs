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
    public class ServiceRequestController : Controller
    {
        private readonly GenericRepository _repository;

        public ServiceRequestController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "ServiceRequest", "Index"))
                return RedirectToAction("NotPermission", "Home");

            var sqlQuery = "SELECT DISTINCT A.DEPRTMDS FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                              + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                              + "WHERE RTRIM(B.USERID) = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "'";

            var filter = "";
            var departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

            foreach (var item in departments)
                if (filter.Length == 0)
                    filter = "'" + item + "'";
                else
                    filter += ",'" + item + "'";

            sqlQuery = "SELECT TOP 20 LTRIM(RTRIM(A.POPRequisitionNumber)) RequestId, RTRIM(A.RequisitionDescription) Description, RTRIM(A.USERDEF1) Priority, "
                + "ISNULL(B.REQUESID, '') QuoteId, A.DOCDATE DocumentDate, A.REQDATE RequiredDate, '' Status, A.REQSTDBY Requester, RTRIM(ISNULL(C.TXTFIELD, '')) AR "
                + "FROM (SELECT RequisitionDescription, Workflow_Status, POPRequisitionNumber, DOCDATE, REQDATE, USERDEF1, USERDEF2, REQSTDBY FROM " +
                Helpers.InterCompanyId + ".dbo.POP10200 "
                + "UNION ALL "
                + "SELECT RequisitionDescription, Workflow_Status, POPRequisitionNumber, DOCDATE, REQDATE, USERDEF1, USERDEF2, REQSTDBY FROM " +
                Helpers.InterCompanyId + ".dbo.POP30200 "
                + "WHERE VOIDED = 0 AND RequisitionStatus NOT IN (5)) A "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP10100 B ON A.POPRequisitionNumber = B.PURCHREQ "
                + "LEFT JOIN " + Helpers.InterCompanyId +
                ".dbo.LPRFQ10100 C ON A.POPRequisitionNumber = C.RFQNMBR AND C.TYPE = 3"
                + "WHERE LOWER(RTRIM(A.USERDEF2)) IN(" + filter + ")"
                + "ORDER BY A.POPRequisitionNumber DESC";

            var purchaseRequestList = _repository.ExecuteQuery<PurchaseRequestViewModel>(sqlQuery);
            foreach (var item in purchaseRequestList)
                item.Status = GetFollowing(item.RequestId);
            return View(purchaseRequestList);
        }

        public ActionResult List(string fromDate, string toDate)
        {
            var sqlQuery = "SELECT DISTINCT A.DEPRTMDS FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                              + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                              + "WHERE RTRIM(B.USERID) = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "'";

            var filter = "";
            var departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

            foreach (var item in departments)
                if (filter.Length == 0)
                    filter = "'" + item + "'";
                else
                    filter += ",'" + item + "'";

            sqlQuery = "SELECT LTRIM(RTRIM(A.POPRequisitionNumber)) RequestId, RTRIM(A.RequisitionDescription) Description, RTRIM(A.USERDEF1) Priority, "
                + "ISNULL(B.REQUESID, '') QuoteId, A.DOCDATE DocumentDate, A.REQDATE RequiredDate, '' Status, A.REQSTDBY Requester, RTRIM(ISNULL(C.TXTFIELD, '')) AR "
                + "FROM (SELECT RequisitionDescription, Workflow_Status, POPRequisitionNumber, DOCDATE, REQDATE, USERDEF1, USERDEF2, REQSTDBY FROM " +
                Helpers.InterCompanyId + ".dbo.POP10200 "
                + "UNION ALL "
                + "SELECT RequisitionDescription, Workflow_Status, POPRequisitionNumber, DOCDATE, REQDATE, USERDEF1, USERDEF2, REQSTDBY FROM " +
                Helpers.InterCompanyId + ".dbo.POP30200 "
                + "WHERE VOIDED = 0 AND RequisitionStatus NOT IN (5)) A "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP10100 B ON A.POPRequisitionNumber = B.PURCHREQ "
                + "LEFT JOIN " + Helpers.InterCompanyId +
                ".dbo.LPRFQ10100 C ON A.POPRequisitionNumber = C.RFQNMBR AND C.TYPE = 3 "
                + "WHERE LOWER(RTRIM(A.USERDEF2)) IN(" + filter + ") AND "
                + $"A.DOCDATE BETWEEN '{DateTime.ParseExact(fromDate, "MM/dd/yyyy", null).ToString("yyyyMMdd")}' AND '{DateTime.ParseExact(toDate, "MM/dd/yyyy", null).ToString("yyyyMMdd")}' "
                + "ORDER BY A.POPRequisitionNumber DESC";

            var purchaseRequestList = _repository.ExecuteQuery<PurchaseRequestViewModel>(sqlQuery);
            foreach (var item in purchaseRequestList)
                item.Status = GetFollowing(item.RequestId);
            return PartialView("~/Views/ServiceRequest/_List.cshtml", purchaseRequestList);
        }

        public ActionResult IndexWarehouse()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "ServiceRequest", "IndexWarehouse"))
                return RedirectToAction("NotPermission", "Home");

            var sqlQuery ="SELECT TOP 40 RTRIM(A.POPRequisitionNumber) RequestId, RTRIM(ISNULL(D.DOCNUMBR, '')) OriginalRequest, RTRIM(A.RequisitionDescription) Description, RTRIM(A.USERDEF1) Priority, "
                + "ISNULL(B.REQUESID, '') QuoteId, A.DOCDATE DocumentDate, A.REQDATE RequiredDate, '' Status, A.REQSTDBY Requester, RTRIM(ISNULL(C.TXTFIELD, '')) AR "
                + "FROM (SELECT RequisitionDescription, Workflow_Status, POPRequisitionNumber, DOCDATE, REQDATE, USERDEF1, USERDEF2, REQSTDBY FROM " +
                Helpers.InterCompanyId + ".dbo.POP10200 "
                + "UNION ALL "
                + "SELECT RequisitionDescription, Workflow_Status, POPRequisitionNumber, DOCDATE, REQDATE, USERDEF1, USERDEF2, REQSTDBY FROM " +
                Helpers.InterCompanyId + ".dbo.POP30200 "
                + "WHERE VOIDED = 0 AND RequisitionStatus NOT IN (5, 7)) A "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP10100 B ON A.POPRequisitionNumber = B.PURCHREQ "
                + "LEFT JOIN " + Helpers.InterCompanyId +
                ".dbo.LPRFQ10100 C ON A.POPRequisitionNumber = C.RFQNMBR AND C.TYPE = 3 "
                + "LEFT JOIN " + Helpers.InterCompanyId +
                ".dbo.LLIF10100 D ON A.POPRequisitionNumber = D.WORKNUMB AND D.DOCTYPE = 1 "
                + "WHERE LOWER(RTRIM(A.USERDEF2)) = LOWER(RTRIM('" +
                Account.GetAccount(User.Identity.GetUserName()).Department + "')) "
                + "ORDER BY A.POPRequisitionNumber DESC";

            var purchaseRequestList = _repository.ExecuteQuery<PurchaseRequestViewModel>(sqlQuery);

            foreach (var item in purchaseRequestList)
                item.Status = GetFollowing(item.RequestId);
            return View(purchaseRequestList);
        }

        public ActionResult ListWarehouse(string fromDate, string toDate)
        {
            var sqlQuery = "SELECT RTRIM(A.POPRequisitionNumber) RequestId, RTRIM(ISNULL(D.DOCNUMBR, '')) OriginalRequest, RTRIM(A.RequisitionDescription) Description, RTRIM(A.USERDEF1) Priority, "
                + "ISNULL(B.REQUESID, '') QuoteId, A.DOCDATE DocumentDate, A.REQDATE RequiredDate, '' Status, A.REQSTDBY Requester, RTRIM(ISNULL(C.TXTFIELD, '')) AR "
                + "FROM (SELECT RequisitionDescription, Workflow_Status, POPRequisitionNumber, DOCDATE, REQDATE, USERDEF1, USERDEF2, REQSTDBY FROM " +
                Helpers.InterCompanyId + ".dbo.POP10200 "
                + "UNION ALL "
                + "SELECT RequisitionDescription, Workflow_Status, POPRequisitionNumber, DOCDATE, REQDATE, USERDEF1, USERDEF2, REQSTDBY FROM " +
                Helpers.InterCompanyId + ".dbo.POP30200 "
                + "WHERE VOIDED = 0 AND RequisitionStatus NOT IN (5, 7)) A "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP10100 B ON A.POPRequisitionNumber = B.PURCHREQ "
                + "LEFT JOIN " + Helpers.InterCompanyId +
                ".dbo.LPRFQ10100 C ON A.POPRequisitionNumber = C.RFQNMBR AND C.TYPE = 3 "
                + "LEFT JOIN " + Helpers.InterCompanyId +
                ".dbo.LLIF10100 D ON A.POPRequisitionNumber = D.WORKNUMB AND D.DOCTYPE = 1 "
                + "WHERE LOWER(RTRIM(A.USERDEF2)) = LOWER(RTRIM('" +Account.GetAccount(User.Identity.GetUserName()).Department + "')) AND "
                + $"A.DOCDATE BETWEEN '{DateTime.ParseExact(fromDate, "MM/dd/yyyy", null).ToString("yyyyMMdd")}' AND '{DateTime.ParseExact(toDate, "MM/dd/yyyy", null).ToString("yyyyMMdd")}' "
                + "ORDER BY A.POPRequisitionNumber DESC";

            var purchaseRequestList = _repository.ExecuteQuery<PurchaseRequestViewModel>(sqlQuery);
            foreach (var item in purchaseRequestList)
                item.Status = GetFollowing(item.RequestId);
            return PartialView("~/Views/ServiceRequest/_ListWarehouse.cshtml", purchaseRequestList);
        }

        public ActionResult Create()
        {
            if (Account.GetAccount(User.Identity.GetUserName()).Department != "ALMACEN")
                if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "ServiceRequest", "Index"))
                    return RedirectToAction("NotPermission", "Home");

            var departmentCode = _repository.ExecuteScalarQuery<string>
            ("SELECT TOP 1 DEPRTMID FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 WHERE RTRIM(DEPRTMDS) = '" +
             Account.GetAccount(User.Identity.GetUserName()).Department + "'").Trim();

            var aprover = _repository.ExecuteScalarQuery<string>("SELECT TOP 1 RTRIM(USERID) FROM " + Helpers.InterCompanyId +
                 ".dbo.LPPOP40101 WHERE RTRIM(DEPRTMID) =  RTRIM('" + departmentCode +"') AND TYPE = 1 AND ISPRINC = 1").Trim();

            ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;
            if (Account.GetAccount(User.Identity.GetUserName()).Department == "COMPRAS")
                ViewBag.Aprover = "jhermida";
            else
                ViewBag.Aprover = aprover;

            ViewBag.Priority = "Baja";
            ViewBag.PurchaseRequestId = HelperLogic.AsignaciónSecuencia("POP10200", Account.GetAccount(User.Identity.GetUserName()).UserId);

            return View();
        }

        [OutputCache(Duration = 0)]
        public ActionResult Details(string id)
        {
            if (id == null) return PartialView();
            var sqlQuery = "";

            sqlQuery = "SELECT TOP 1 A.RequisitionDescription Description, A.DOCDATE DocumentDate, A.REQDATE RequiredDate, "
                + "A.REQSTDBY Requester, A.USERDEF2 Department, ISNULL(E.TXTFIELD, '') Aprover, "
                + "ISNULL(B.LSTUSRID,  '') UserQuote, ISNULL(D.LSTUSRID, '') UserPreAnalysis,"
                + "ISNULL(C.LSTUSRID, '') UserAnalysis, ISNULL(B.CREATDDT, '') QuoteDate, "
                + "ISNULL(D.CREATDDT, '') PreAnalysisDate, ISNULL(C.CREATDDT, '') AnalysisDate FROM "
                + "(SELECT POPRequisitionNumber, RequisitionDescription, RequisitionStatus, DOCDATE, REQDATE, REQSTDBY, "
                + "USERDEF1, USERDEF2, Workflow_Status "
                + "FROM " + Helpers.InterCompanyId + ".dbo.POP10200 UNION ALL "
                + "SELECT POPRequisitionNumber, RequisitionDescription, RequisitionStatus, DOCDATE, REQDATE, REQSTDBY, "
                + "USERDEF1, USERDEF2, Workflow_Status "
                + "FROM      " + Helpers.InterCompanyId + ".dbo.POP30200)  A "
                + "LEFT JOIN " + Helpers.InterCompanyId +
                ".dbo.LPPOP10100 B ON A.POPRequisitionNumber = B.PURCHREQ "
                + "LEFT JOIN " + Helpers.InterCompanyId +
                ".dbo.LPPOP30100 C ON A.POPRequisitionNumber = C.PURCHREQ AND C.VOIDED = 0 "
                + "LEFT JOIN " + Helpers.InterCompanyId +
                ".dbo.LPPOP20100 D ON A.POPRequisitionNumber = D.PURCHREQ "
                + "LEFT JOIN " + Helpers.InterCompanyId +
                ".dbo.LPRFQ10100 E ON A.POPRequisitionNumber = E.RFQNMBR AND E.TYPE = 1 "
                + "WHERE A.POPRequisitionNumber = '" + id + "' "
                + "ORDER BY A.POPRequisitionNumber ";

            var requestAdditionalInformation = _repository.ExecuteScalarQuery<PurchaseRequestAdditionalInfoViewModel>(sqlQuery);

            sqlQuery = "SELECT DISTINCT A.ITEMNMBR ItemId, A.ITEMDESC ItemDesc, A.QTYORDER Quantity, A.UOFM UnitId, '' Status "
                + "FROM "
                + "(SELECT POPRequisitionNumber, ITEMNMBR, ITEMDESC, QTYORDER, UOFM "
                + "FROM " + Helpers.InterCompanyId + ".dbo.POP10210 UNION ALL "
                + "SELECT POPRequisitionNumber, ITEMNMBR, ITEMDESC, QTYORDER, UOFM "
                + "FROM " + Helpers.InterCompanyId + ".dbo.POP30210) A "
                + "WHERE A.POPRequisitionNumber = '" + id + "'";

            var requestDetail = _repository.ExecuteQuery<PurchaseRequestLineViewModel>(sqlQuery).ToList();

            foreach (var item in requestDetail)
            {
                if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.POP10210 "+ "WHERE POPRequisitionNumber = '" + id + "' AND ITEMNMBR = '" +item.ItemId + "'").FirstOrDefault() > 0)
                    item.Status = "En requisicion";
                if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId + ".dbo.POP30210 "+ "WHERE POPRequisitionNumber = '" + id + "' AND ITEMNMBR = '" +item.ItemId + "'").FirstOrDefault() > 0)
                    item.Status = "En requisicion";
                if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +".dbo.LPPOP10200 "+ "WHERE PURCHREQ = '" + id + "' AND ITEMNMBR = '" + item.ItemId +"'").FirstOrDefault() > 0)
                    item.Status = "En cotizacion";
                if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +".dbo.LPPOP10200 "+ "WHERE PURCHREQ = '" + id + "' AND ITEMNMBR = '" + item.ItemId +"'").FirstOrDefault() > 0)
                    item.Status = "En cotizacion";
                if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +".dbo.LPPOP20200 "+ "WHERE PURCHREQ = '" + id + "' AND ITEMNMBR = '" + item.ItemId +"'").FirstOrDefault() > 0)
                    item.Status = "En pre-analisis";
                if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +".dbo.LPPOP30200 A "+ "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP30100 B "
                    + "ON A.ANLREQUS = B.ANLREQUS "+ "WHERE B.PURCHREQ = '" + id + "' AND A.ITEMNMBR = '" +item.ItemId + "' AND B.VOIDED = 0").FirstOrDefault() > 0)
                    item.Status = "En analisis";
                if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +".dbo.LPPOP30104 A "+ "INNER JOIN " + Helpers.InterCompanyId + ".dbo.POP10100 B "
                    + "ON A.PONUMBE = B.PONUMBER "+ "WHERE A.PURCHREQ = '" + id + "' AND A.ITEMNMBR = '" +item.ItemId + "' AND B.POSTATUS <> 2").FirstOrDefault() > 0)
                    item.Status = "En compras";
                if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +".dbo.POP10310 A "+ "INNER JOIN " + Helpers.InterCompanyId +
                    ".dbo.LPPOP30104 B ON A.PONUMBER = B.PONUMBE AND A.ITEMNMBR = B.ITEMNMBR "+ "WHERE B.PURCHREQ = '" + id + "' AND A.ITEMNMBR = '" +item.ItemId + "'").FirstOrDefault() > 0)
                    item.Status = "Recibido";
                if (_repository.ExecuteQuery<int>("SELECT COUNT(*) FROM " + Helpers.InterCompanyId +".dbo.POP30310 A "+ "INNER JOIN " + Helpers.InterCompanyId +".dbo.LPPOP30104 B ON A.PONUMBER = B.PONUMBE AND A.ITEMNMBR = B.ITEMNMBR "
                    + "WHERE B.PURCHREQ = '" + id + "' AND A.ITEMNMBR = '" +item.ItemId + "'").FirstOrDefault() > 0)
                    item.Status = "Recibido";
            }

            sqlQuery = "SELECT ANLREQUS ViewDataId, REQSDESC ViewDataDescription "
                       + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30100 "
                       + "WHERE PURCHREQ = '" + id + "' AND VOIDED = 0 ORDER BY ANLREQUS";

            var analysisDetail = _repository.ExecuteQuery<GenericViewModel>(sqlQuery);

            sqlQuery = "SELECT A.ANLREQUS ViewDataId, A.PONUMBE ViewDataDescription "
                       + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30102 A "
                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.POP10100 B ON A.PONUMBE = B.PONUMBER "
                       + "WHERE A.PURCHREQ = '" + id + "' ORDER BY ANLREQUS";

            var purchaseOrderDetail = _repository.ExecuteQuery<GenericViewModel>(sqlQuery);
            ViewBag.RequestDetail = requestDetail;
            ViewBag.AdditionalInformation = requestAdditionalInformation;
            ViewBag.AnalysisDetail = analysisDetail;
            ViewBag.PurchaseOrderDetail = purchaseOrderDetail;

            return PartialView();
        }

        [HttpPost]
        public JsonResult SaveServiceRequest(PurchaseRequest request, int postType = 0)
        {
            var status = "";
            var order = 16384;
            var lineNumber = 1;

            try
            {
                status = "OK";
                _repository.ExecuteCommand(String.Format("INTRANET.dbo.PurchRequestInsert '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}'",
                    Helpers.InterCompanyId, request.PurchaseRequestId, request.Description, request.Note, request.DocumentDate, request.RequiredDate,
                    request.Priority, request.DepartmentId, request.AR, request.Approver, request.Requester));
                _repository.ExecuteCommand(String.Format("INTRANET.dbo.PurchRequestLineDelete '{0}','{1}'", Helpers.InterCompanyId, request.PurchaseRequestId));

                foreach (var i in request.PurchaseRequestLines)
                {
                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.PurchRequestLineInsert '{0}','{1}','{2}','{3}','{4}',[{5}],'{6}','{7}','{8}','{9}','{10}'",
                    Helpers.InterCompanyId, request.PurchaseRequestId, order, lineNumber, i.ItemId, i.ItemDescription, i.UnitId, i.Warehouse, i.Quantity, i.AccountNum, i.Charge));

                    order += 16384;
                    lineNumber += 1;
                }

                if (postType == 1)
                {
                    var sqlQuery = $"SELECT TOP 1 RTRIM(ISNULL(DOCNUMBR, '')) FROM {Helpers.InterCompanyId}.dbo.LLIF10100 WHERE WORKNUMB = '{request.PurchaseRequestId}' AND DOCTYPE = 1";
                    var purchaseRequest = _repository.ExecuteScalarQuery<string>(sqlQuery);
                    _repository.ExecuteCommand($"LODYNDEV.dbo.LPWF00101SI '{Helpers.InterCompanyId}','{request.PurchaseRequestId}','{request.Description}','{1}','{1}'");
                    _repository.ExecuteCommand($"LODYNDEV.dbo.LPWF00201SI '{Helpers.InterCompanyId}','{request.PurchaseRequestId}','{Account.GetAccount(User.Identity.GetUserName()).UserId}','','{1}'");
                    ProcessLogic.SendToSharepoint(request.PurchaseRequestId, 1, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
                }
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult {Data = new { status } };
        }

        [HttpPost]
        public JsonResult CheckChange(PurchaseRequest request)
        {
            bool isChange = false;
            try
            {
                var sqlQuery = "SELECT RTRIM(A.POPRequisitionNumber) PurchaseRequestId, RTRIM(A.RequisitionDescription) Description, "
                + "RTRIM(A.REQSTDBY) Requester, A.DOCDATE DocumentDate, A.REQDATE RequiredDate, RTRIM(A.USERDEF1) Priority, "
                + "RTRIM(A.USERDEF2) DepartmentId, ISNULL(RTRIM(B.TXTFIELD), '') Approver, ISNULL(RTRIM(C.TXTFIELD), '') AR, "
                + "ISNULL(D.TXTFIELD, '') Note, '" + Helpers.InterCompanyId + "' INTERID "
                + "FROM " + Helpers.InterCompanyId + ".dbo.POP10200 A "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPRFQ10100 B "
                + "ON A.POPRequisitionNumber = B.RFQNMBR AND B.TYPE = 1 "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPRFQ10100 C "
                + "ON A.POPRequisitionNumber = C.RFQNMBR AND C.TYPE = 3 "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY03900 D "
                + "ON A.Requisition_Note_Index = D.NOTEINDX "
                + "WHERE A.POPRequisitionNumber = '" + request.PurchaseRequestId + "'";

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

                sqlQuery = "SELECT RTRIM(ITEMNMBR) ItemId, RTRIM(ITEMDESC) ItemDescription, QTYORDER Quantity, "
                           + "RTRIM(UOFM) UnitId, RTRIM(LOCNCODE) Warehouse, RTRIM(REQSTDBY) AccountNum, RTRIM(USERDEF2) Charge "
                           + "FROM " + Helpers.InterCompanyId + ".dbo.POP30210 "
                           + "WHERE POPRequisitionNumber = '" + request.PurchaseRequestId + "'";

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

        [HttpPost]
        public JsonResult SendWorkFlow(string purchaseRequestId)
        {
            string xStatus;

            try
            {
                xStatus = "OK";
                var sqlQuery = "SELECT TOP 1 RTRIM(ISNULL(DOCNUMBR, '')) " + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 " + "WHERE WORKNUMB = '" + purchaseRequestId + "' AND DOCTYPE = 1";
                var purchaseRequest = _repository.ExecuteScalarQuery<string>(sqlQuery);
                _repository.ExecuteCommand(string.Format("LODYNDEV.dbo.LPWF00101SI '{0}','{1}','{2}','{3}','{4}'", Helpers.InterCompanyId, purchaseRequestId, "", 1, 1));
                _repository.ExecuteCommand(string.Format("LODYNDEV.dbo.LPWF00201SI '{0}','{1}','{2}','{3}','{4}'", Helpers.InterCompanyId, purchaseRequestId, Account.GetAccount(User.Identity.GetUserName()).UserId, "", 4));

                ProcessLogic.SendToSharepoint(purchaseRequestId, 1, Account.GetAccount(User.Identity.GetUserName()).Email, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public ActionResult Edit(string id)
        {
            if (Account.GetAccount(User.Identity.GetUserName()).Department != "ALMACEN")
            {
                if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "ServiceRequest", "Index"))
                {
                    return RedirectToAction("NotPermission", "Home");
                }
            }

            var sqlQuery = "SELECT RTRIM(ITEMNMBR) ItemId, RTRIM(ITEMDESC) ItemDescription, QTYORDER Quantity, "
                              + "RTRIM(UOFM) UnitId, RTRIM(LOCNCODE) Warehouse, RTRIM(REQSTDBY) AccountNum, RTRIM(USERDEF2) Charge "
                              + "FROM " + Helpers.InterCompanyId + ".dbo.POP10210 "
                              + "WHERE POPRequisitionNumber = '" + id + "'";

            var purchaseRequestItems = _repository.ExecuteQuery<RequisitionLineViewModel>(sqlQuery).ToList();

            sqlQuery =
                "SELECT RTRIM(A.POPRequisitionNumber) PurchaseRequestId, RTRIM(A.RequisitionDescription) Description, "
                + "RTRIM(A.REQSTDBY) Requester, A.DOCDATE DocumentDate, A.REQDATE RequiredDate, RTRIM(A.USERDEF1) Priority, "
                + "RTRIM(A.USERDEF2) DepartmentId, ISNULL(RTRIM(B.TXTFIELD), '') Approver, ISNULL(RTRIM(C.TXTFIELD), '') AR, "
                + "ISNULL(D.TXTFIELD, '') Note, '" + Helpers.InterCompanyId + "' INTERID "
                + "FROM " + Helpers.InterCompanyId + ".dbo.POP10200 A "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPRFQ10100 B "
                + "ON A.POPRequisitionNumber = B.RFQNMBR AND B.TYPE = 1 "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPRFQ10100 C "
                + "ON A.POPRequisitionNumber = C.RFQNMBR AND C.TYPE = 3 "
                + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY03900 D "
                + "ON A.Requisition_Note_Index = D.NOTEINDX "
                + "WHERE A.POPRequisitionNumber = '" + id + "'";

            var purchaseRequest = _repository.ExecuteScalarQuery<PurchaseRequest>(sqlQuery);

            sqlQuery = "SELECT ISNULL(A.Workflow_Status, 0) "
                       + "FROM (SELECT Workflow_Status, POPRequisitionNumber FROM " + Helpers.InterCompanyId +
                       ".dbo.POP10200 "
                       + "UNION ALL "
                       + "SELECT Workflow_Status, POPRequisitionNumber FROM " + Helpers.InterCompanyId +
                       ".dbo.POP30200) A "
                       + "WHERE A.POPRequisitionNumber = '" + id + "'";

            var status = _repository.ExecuteScalarQuery<short>(sqlQuery);

            sqlQuery = "SELECT COUNT(*) "
                       + "FROM " + Helpers.InterCompanyId + ".dbo.POP30200 "
                       + "WHERE POPRequisitionNumber = '" + id + "'";

            var inWork = _repository.ExecuteScalarQuery<int>(sqlQuery) != 0;

            if (purchaseRequest == null)
            {
                sqlQuery = "SELECT RTRIM(ITEMNMBR) ItemId, RTRIM(ITEMDESC) ItemDescription, QTYORDER Quantity, "
                           + "RTRIM(UOFM) UnitId, RTRIM(LOCNCODE) Warehouse, RTRIM(REQSTDBY) AccountNum, RTRIM(USERDEF2) Charge "
                           + "FROM " + Helpers.InterCompanyId + ".dbo.POP30210 "
                           + "WHERE POPRequisitionNumber = '" + id + "'";

                purchaseRequestItems = _repository.ExecuteQuery<RequisitionLineViewModel>(sqlQuery).ToList();

                sqlQuery =
                    "SELECT RTRIM(A.POPRequisitionNumber) PurchaseRequestId, RTRIM(A.RequisitionDescription) Description, "
                    + "RTRIM(A.REQSTDBY) Requester, A.DOCDATE DocumentDate, A.REQDATE RequiredDate, RTRIM(A.USERDEF1) Priority, "
                    + "RTRIM(A.USERDEF2) DepartmentId, ISNULL(RTRIM(B.TXTFIELD), '') Approver, ISNULL(RTRIM(C.TXTFIELD), '') AR, "
                    + "ISNULL(D.TXTFIELD, '') Note, '" + Helpers.InterCompanyId + "' INTERID "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.POP30200 A "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPRFQ10100 B "
                    + "ON A.POPRequisitionNumber = B.RFQNMBR AND B.TYPE = 1 "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPRFQ10100 C "
                    + "ON A.POPRequisitionNumber = C.RFQNMBR AND C.TYPE = 3 "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY03900 D "
                    + "ON A.Requisition_Note_Index = D.NOTEINDX "
                    + "WHERE A.POPRequisitionNumber = '" + id + "'";

                purchaseRequest = _repository.ExecuteScalarQuery<PurchaseRequest>(sqlQuery);

                ViewBag.Status = status.ToString();
                ViewBag.Work = inWork;
                ViewBag.PurchaseRequestItems = purchaseRequestItems;
                ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;
                if (!inWork)
                {
                    return View("Edit", purchaseRequest);
                }
                return View("Inquiry", purchaseRequest);
            }
            else
            {

                ViewBag.Status = status.ToString();
                ViewBag.Work = inWork;
                ViewBag.PurchaseRequestItems = purchaseRequestItems;
                ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;
                if (inWork)
                {
                    return View("Inquiry", purchaseRequest);
                }
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string purchaseRequestId)
        {
            _repository.ExecuteCommand("UPDATE " + Helpers.InterCompanyId + ".dbo.LPWF00101 SET WFSTS = 3 WHERE DOCNUM = '" + purchaseRequestId + "'");
            _repository.ExecuteCommand(String.Format("INTRANET.dbo.PurchRequestDelete '{0}','{1}'", Helpers.InterCompanyId, purchaseRequestId));
            return RedirectToAction("Index");
        }

        public ActionResult UnblockSecuence(string secuencia, string formulario, string usuario)
        {
            HelperLogic.DesbloqueoSecuencia(secuencia, "POP10200",
                Account.GetAccount(User.Identity.GetUserName()).UserId);
            return View();
        }

        [HttpPost]
        public JsonResult AttachFile(HttpPostedFileBase fileData, string requestId)
        {
            var status = false;

            try
            {
                byte[] fileStream = null;
                using (var binaryReader = new BinaryReader(fileData.InputStream))
                {
                    fileStream = binaryReader.ReadBytes(fileData.ContentLength);
                }

                var fileName = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].ToString();
                var fileType = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].Split('.')[1]
                    .ToString();

                _repository.ExecuteCommand(String.Format(
                    "INTRANET.dbo.AttachmentInsert '{0}','{1}','{2}',{3},'{4}','{5}','{6}'",
                    Helpers.InterCompanyId, requestId, fileName,
                    "0x" + BitConverter.ToString(fileStream).Replace("-", String.Empty),
                    fileType, Account.GetAccount(User.Identity.GetUserName()).UserId, "REQ"));
                status = true;
            }
            catch
            {
                status = false;
            }

            return new JsonResult {Data = new { status } };
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
                var sqlQuery = "SELECT RTRIM(fileName) FileName FROM " + Helpers.InterCompanyId +
                                  ".dbo.CO00105 WHERE DOCNUMBR = '" + purchaseRequestId + "' AND DELETE1 = 0";
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
            var sqlQuery = "SELECT BinaryBlob, FileType, A.fileName "
                              + "FROM " + Helpers.InterCompanyId + ".dbo.CO00105 A "
                              + "INNER JOIN " + Helpers.InterCompanyId +
                              ".dbo.coAttachmentItems B ON A.Attachment_ID = B.Attachment_ID "
                              + "WHERE A.DOCNUMBR = '" + documentId + "' AND A.DELETE1 = 0 AND RTRIM(A.fileName) = '" +
                              FileName + "'";

            var adjunto = ConnectionDb.GetDt(sqlQuery);
            byte[] contents = null;
            var fileType = "";
            var fileName = "";

            if (adjunto.Rows.Count > 0)
            {
                contents = (byte[]) adjunto.Rows[0][0];
                fileType = adjunto.Rows[0][1].ToString();
                fileName = adjunto.Rows[0][2].ToString();
            }

            return File(contents, fileType.Trim(), fileName.Trim());
        }

        [HttpPost]
        public ActionResult ListItems(string id)
        {
            try
            {
                var files = new List<RequisitionLineViewModel>();
                var sqlQuery =
                    "SELECT RTRIM(ITEMNMBR) ItemId, RTRIM(ITEMDESC) ItemDescription, CONVERT(NUMERIC(32,2), QTYORDER) Quantity, "
                    + "RTRIM(UOFM) UnitId, RTRIM(REQSTDBY) AccountNum, RTRIM(USERDEF2) Charge, RTRIM(LOCNCODE) Warehouse FROM "
                    + "( "
                    + "SELECT POPRequisitionNumber, ITEMNMBR, ITEMDESC, QTYORDER, UOFM, REQSTDBY, USERDEF2, LOCNCODE FROM " +
                    Helpers.InterCompanyId + ".dbo.POP10210 "
                    + "UNION ALL "
                    + "SELECT POPRequisitionNumber, ITEMNMBR, ITEMDESC, QTYORDER, UOFM, REQSTDBY, USERDEF2, LOCNCODE FROM " +
                    Helpers.InterCompanyId + ".dbo.POP30210 "
                    + ") A "
                    + "WHERE A.POPRequisitionNumber = '" + id + "'"
                    + "ORDER BY A.POPRequisitionNumber ";
                files = _repository.ExecuteQuery<RequisitionLineViewModel>(sqlQuery).ToList();
                return Json(files, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        public ActionResult Print(string id)
        {
            var status = "";
            var isTwoAprovers = false;
            try
            {
                var sqlQuery = "SELECT ISNULL(B.TXTFIELD,'') APROBADOR FROM "
                                  + "(SELECT DISTINCT POPRequisitionNumber FROM " + Helpers.InterCompanyId +
                                  ".dbo.POP10200 "
                                  + "UNION ALL "
                                  + "SELECT DISTINCT POPRequisitionNumber FROM " + Helpers.InterCompanyId +
                                  ".dbo.POP30200) A "
                                  + "LEFT JOIN " + Helpers.InterCompanyId +
                                  ".dbo.LPRFQ10100 B ON A.POPRequisitionNumber = B.RFQNMBR AND B.TYPE = 1 "
                                  + "WHERE A.POPRequisitionNumber = '" + id.Trim() + "'";

                var aprobador = _repository.ExecuteScalarQuery<string>(sqlQuery);

                var solicitudes = new List<short>();

                sqlQuery = "SELECT ISNULL(A.WFSTS, 0) FROM " + Helpers.InterCompanyId + ".dbo.LPWF00201 A "
                           + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPWF00101 B "
                           + "ON A.DOCNUM = B.DOCNUM "
                           + "WHERE A.DOCNUM = '" + id + "' AND TYPE = 1";

                solicitudes = _repository.ExecuteQuery<short>(sqlQuery).ToList();

                #region Imagen

                foreach (var item in solicitudes)
                {
                    if (item == 3)
                    {
                        isTwoAprovers = true;
                        break;
                    }
                }

                if (isTwoAprovers)
                {
                    HelperLogic.InsertSignature(aprobador.Trim());
                    HelperLogic.InsertSignaturePayment(HelperLogic.GetSecondAproverPayment());
                }
                else
                {

                    foreach (var item in solicitudes)
                    {
                        if (item == 4)
                        {
                            isTwoAprovers = true;
                            break;
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

                ReportHelper.Export(Helpers.ReportPath + "Requisicion",
                    Server.MapPath("~/PDF/Requisicion/") + id + ".pdf",
                    String.Format("LODYNDEV.dbo.LPPOP10200R1 '{0}','{1}'",
                        Helpers.InterCompanyId, id), 1, ref status);

                status = id + ".pdf";
            }
            catch
            {
                status = "";
            }

            return new JsonResult {Data = new { status } };
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

        private string GetFollowing(string requisitionId)
        {
            try
            {
                var status = "";
                var sqlQuery =
                    "SELECT DISTINCT RTRIM(A.POPRequisitionNumber) RequestId, ISNULL(RTRIM(B.REQUESID), '') QuoteId, "
                    + "ISNULL(RTRIM(C.ANLREQUS), '') AnalisisNumber, ISNULL(RTRIM(D.REQUESID), '') PreAnalisisNumber, "
                    + "ISNULL(RTRIM(F.PONUMBE), '') PurchaseOrder, ISNULL(RTRIM(G.POPRCTNM), '') ReceiptId, ISNULL(RTRIM(H.DOCNUMBR), '') LogisticId FROM "
                    + "(SELECT Workflow_Status, POPRequisitionNumber FROM " + Helpers.InterCompanyId +
                    ".dbo.POP10200 UNION ALL "
                    + "SELECT Workflow_Status, POPRequisitionNumber FROM  " + Helpers.InterCompanyId +
                    ".dbo.POP30200) A "
                    + "LEFT JOIN " + Helpers.InterCompanyId +
                    ".dbo.LPPOP10100 B ON A.POPRequisitionNumber = B.PURCHREQ "
                    + "LEFT JOIN " + Helpers.InterCompanyId +
                    ".dbo.LPPOP30100 C ON A.POPRequisitionNumber = C.PURCHREQ AND C.VOIDED = 0 "
                    + "LEFT JOIN " + Helpers.InterCompanyId +
                    ".dbo.LPPOP20100 D ON A.POPRequisitionNumber = D.PURCHREQ "
                    + "LEFT JOIN " + Helpers.InterCompanyId +
                    ".dbo.LPRFQ10100 E ON A.POPRequisitionNumber = E.RFQNMBR AND E.TYPE = 1 "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP30102 F ON C.ANLREQUS = F.ANLREQUS "
                    + "LEFT JOIN "
                    + "(SELECT POPRCTNM, PONUMBER FROM " + Helpers.InterCompanyId + ".dbo.POP10310 "
                    + "UNION ALL "
                    + "SELECT POPRCTNM, PONUMBER FROM " + Helpers.InterCompanyId +
                    ".dbo.POP30310) G ON F.PONUMBE = G.PONUMBER "
                    + "LEFT JOIN " + Helpers.InterCompanyId +
                    ".dbo.LLIF10100 H ON A.POPRequisitionNumber = H.WORKNUMB AND H.DOCSTTS = 6 "
                    + "WHERE A.POPRequisitionNumber = '" + requisitionId + "' ";

                var following = _repository.ExecuteScalarQuery<FollowingViewModel>(sqlQuery);

                sqlQuery = "SELECT WFSTS FROM " + Helpers.InterCompanyId + ".dbo.LPWF00101 WHERE DOCNUM = '" +
                           requisitionId + "'";
                var workflowStatus = _repository.ExecuteScalarQuery<short>(sqlQuery);

                if (following != null)
                {
                    if (following.ReceiptId.Length > 0)
                    {
                        status = "Recibido";
                    }
                    else if (following.PurchaseOrder.Length > 0)
                    {
                        status = "En compras";
                    }
                    else if (following.QuoteId.Length > 0 && following.PreAnalisisNumber.Length > 0
                             && following.AnalisisNumber.Length > 0 && following.PurchaseOrder.Length == 0)
                    {
                        status = "En analisis";
                    }
                    else if (following.QuoteId.Length > 0 && following.PreAnalisisNumber.Length == 0
                             && following.AnalisisNumber.Length == 0)
                    {
                        status = "En cotizacion";
                    }
                    else if (following.QuoteId.Length > 0 && following.PreAnalisisNumber.Length > 0
                             && following.AnalisisNumber.Length == 0)
                    {
                        status = "En pre-analisis";
                    }
                    else if (following.QuoteId.Length == 0 && following.PreAnalisisNumber.Length == 0
                             && following.AnalisisNumber.Length == 0)
                    {
                        switch (workflowStatus)
                        {
                            case 1:
                                status = "Enviado";
                                break;
                            case 2:
                                status = "Rechazado";
                                break;
                            case 3:
                                status = "Anulado";
                                break;
                            case 4:
                                status = "Aprobado";
                                break;
                            default:
                                status = "No enviado";
                                break;
                        }
                    }
                    else
                    {
                        status = "En requisicion";
                    }
                }

                return status;
            }
            catch
            {
                return "En requisicion";
            }
        }
    }
}