using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Seaboard.Intranet.Data.Repository;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class ArController : Controller
    {
        private readonly GenericRepository _repository;

        public ArController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AR", "Index"))
                return RedirectToAction("NotPermission", "Home");

            string sqlQuery = "SELECT DISTINCT LTRIM(RTRIM(A.PROYCTID)) ProjectId, RTRIM(A.PROYCTDS) ProjectDesc, RTRIM(A.ACCNTNUM) AccountNum "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40200 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40400 B "
                    + "ON A.PROYCTID = B.PROYCTID "
                    + "WHERE LOWER(RTRIM(B.DEPRTMID)) = LOWER(RTRIM('" + Account.GetAccount(User.Identity.GetUserName()).Department + "')) ";

            var projects = _repository.ExecuteQuery<Project>(sqlQuery);
            foreach (var item in projects)
            {
                sqlQuery = "SELECT DISTINCT RTRIM(ARNUMBER) ARNumber, RTRIM(ARDESC) ARDescription, RTRIM(ACCNTNUM) AccountNum, "
                   + "RTRIM(DEPRTMID) DepartmentId, DOCAMNT Amount "
                   + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40400 "
                   + "WHERE PROYCTID = '" + item.ProjectId + "'";
                item.ProjectLines = _repository.ExecuteQuery<Ar>(sqlQuery).ToList();
            }
            return View(projects);
        }

        public ActionResult Create()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AR", "Index"))
                return RedirectToAction("NotPermission", "Home");
            ViewBag.DepartmentId = Account.GetAccount(User.Identity.GetUserName()).Department;
            return View();
        }

        public ActionResult Edit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AR", "Index"))
                return RedirectToAction("NotPermission", "Home");

            string sqlQuery = "SELECT DISTINCT LTRIM(RTRIM(PROYCTID)) ProjectId, PROYCTDS ProjectDesc, RTRIM(ACCNTNUM) AccountNum "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40200 "
                    + "WHERE RTRIM(PROYCTID) = RTRIM('" + id + "')";

            var project = _repository.ExecuteScalarQuery<Project>(sqlQuery);

            if (project != null)
            {
                sqlQuery = "SELECT DISTINCT RTRIM(ARNUMBER) ARNumber, RTRIM(ARDESC) ARDescription, RTRIM(ACCNTNUM) AccountNum, "
                   + "RTRIM(DEPRTMID) DepartmentId, DOCAMNT Amount "
                   + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40400 "
                   + "WHERE PROYCTID = '" + project.ProjectId + "'";

                project.ProjectLines = _repository.ExecuteQuery<Ar>(sqlQuery).ToList();
            }

            ViewBag.DepartmentId = project.ProjectLines.First().DepartmentId;
            return View(project);
        }

        public ActionResult Inquiry()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AR", "Inquiry"))
                return RedirectToAction("NotPermission", "Home");

            string sqlQuery = "SELECT DISTINCT RTRIM(ARNUMBER) ARNumber, RTRIM(ARDESC) ARDescription, RTRIM(ACCNTNUM) AccountNum, "
                   + "RTRIM(DEPRTMID) DepartmentId, DOCAMNT Amount "
                   + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40400 "
                   + "WHERE DEPRTMID = '" + Account.GetAccount(User.Identity.GetUserName()).Department + "'";

            var ar = _repository.ExecuteQuery<Ar>(sqlQuery);

            foreach (var item in ar)
            {
                sqlQuery = "SELECT TOP 1 LTRIM(RTRIM(A.PROYCTID)) ProjectId, A.PROYCTDS ProjectDesc, RTRIM(A.ACCNTNUM) AccountNum "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40200 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40400 B "
                    + "ON A.PROYCTID = B.PROYCTID "
                    + "WHERE LOWER(RTRIM(B.ARNUMBER)) = LOWER(RTRIM('" + item.ArNumber + "'))";
                item.Project = _repository.ExecuteScalarQuery<Project>(sqlQuery);
                if (item.Project == null)
                    item.Project = new Project();
            }

            return View(ar);
        }

        public ActionResult Details(string id)
        {
            string sqlQuery = "SELECT DISTINCT RTRIM(ARNUMBER) ARNumber, RTRIM(ARDESC) ARDescription, RTRIM(ACCNTNUM) AccountNum, "
                   + "RTRIM(DEPRTMID) DepartmentId, DOCAMNT Amount, PROYCTID ProjectId, CREATDDT CreatedDate, MODIFDTE ModifiedDate, "
                   + "LSTUSRID CreatedBy "
                   + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40400 "
                   + "WHERE ARNUMBER = '" + id + "'";

            List<ArPurchaseRequest> requestAr = new List<ArPurchaseRequest>();

            var ar = _repository.ExecuteScalarQuery<ArViewModel>(sqlQuery);

            if (ar != null)
            {
                sqlQuery = "SELECT DISTINCT A.POPRequisitionNumber RequestId, A.DOCDATE RequestDocumentDate, ISNULL(E.PONUMBER, '') PurchaseOrder, "
                    + "(ISNULL(E.SUBTOTAL, 0) + ISNULL(E.FRTAMNT, 0) + ISNULL(E.MSCCHAMT, 0) + ISNULL(E.TAXAMNT, 0) - ISNULL(E.TRDISAMT, 0)) Amount, "
                    + "ISNULL(E.DOCDATE, '1900-01-01') PurchaseDocumentDate "
                    + "FROM "
                    + "(SELECT POPRequisitionNumber, DOCDATE FROM " + Helpers.InterCompanyId + ".dbo.POP10200 "
                    + "UNION ALL "
                    + "SELECT POPRequisitionNumber, DOCDATE FROM " + Helpers.InterCompanyId + ".dbo.POP30200 WHERE VOIDED = 0) A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPRFQ10100 B "
                    + "ON A.POPRequisitionNumber = B.RFQNMBR AND B.TYPE = 3 "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP30102 D "
                    + "ON A.POPRequisitionNumber = D.PURCHREQ "
                    + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.POP10100 E "
                    + "ON D.PONUMBE = E.PONUMBER AND E.POSTATUS <> 2 "
                    + "WHERE B.TXTFIELD = '" + ar.ArNumber + "'";

                ar.PurchaseList = _repository.ExecuteQuery<ArPurchaseRequest>(sqlQuery).ToList();

                if (ar.PurchaseList == null)
                    ar.PurchaseList = new List<ArPurchaseRequest>();
            }

            return View(ar);
        }

        [HttpPost]
        public JsonResult SaveProject(Project project)
        {
            bool status = false;

            try
            {
                if (project != null)
                {
                    _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPPOP40200SI '{0}','{1}','{2}','{3}','{4}','{5}'",
                        Helpers.InterCompanyId, project.ProjectId, project.ProjectDesc, 0, project.ProjectLines.First().AccountNum, Account.GetAccount(User.Identity.GetUserName()).UserId));

                    _repository.ExecuteCommand(String.Format("DELETE FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40400 WHERE PROYCTID = '{0}'", project.ProjectId));
                    foreach (var item in project.ProjectLines)
                        _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPPOP40400SI '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}'",
                          Helpers.InterCompanyId, item.ArNumber, item.ArDescription, 0, item.Amount, project.ProjectId, item.AccountNum, item.DepartmentId, Account.GetAccount(User.Identity.GetUserName()).UserId));
                }

                status = true;
            }
            catch
            {
                status = false;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult Project()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AR", "Project"))
                return RedirectToAction("NotPermission", "Home");

            string sqlQuery = "SELECT DISTINCT LTRIM(RTRIM(A.PROYCTID)) ProjectId, RTRIM(A.PROYCTDS) ProjectDesc, RTRIM(A.ACCNTNUM) AccountNum "
                    + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40200 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40400 B "
                    + "ON A.PROYCTID = B.PROYCTID "
                    + "WHERE LOWER(RTRIM(B.DEPRTMID)) = LOWER(RTRIM('" + Account.GetAccount(User.Identity.GetUserName()).Department + "')) ";

            var projects = _repository.ExecuteQuery<Project>(sqlQuery);
            foreach (var item in projects)
            {
                sqlQuery = "SELECT DISTINCT RTRIM(ARNUMBER) ARNumber, RTRIM(ARDESC) ARDescription, RTRIM(ACCNTNUM) AccountNum, "
                   + "RTRIM(DEPRTMID) DepartmentId, DOCAMNT Amount "
                   + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40400 "
                   + "WHERE PROYCTID = '" + item.ProjectId + "'";

                item.ProjectLines = _repository.ExecuteQuery<Ar>(sqlQuery).ToList();
            }

            return View(projects);
        }

        public ActionResult AR_Consult()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "AR", "Project"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = "SELECT DISTINCT LTRIM(RTRIM((A.POPRequisitionNumber))) AS[Requisition], LTRIM(RTRIM(F.ARNUMBER)) AS[ARNumber], "
                              + "ISNULL(E.PONUMBER, '') AS [PurchaseOrder], LTRIM(RTRIM(ISNULL(G.PROYCTDS, F.ARNUMBER))) AS[ProjectDesc], "
                              + "LTRIM(RTRIM(F.ARDESC)) AS[ARDescription], LTRIM(RTRIM((F.DEPRTMID))) AS [Department],LTRIM(RTRIM((ISNULL(H.PMNTNMBR,'')))) AS [Payment], "
                              + "LTRIM(RTRIM((ISNULL(E.CURNCYID, '')))) AS[Currency], CONVERT(DECIMAL(20, 2), ISNULL(F.DOCAMNT, 0.00)) AS[Amount], CONVERT(DECIMAL(20, 2), ( "
                              + "ISNULL(E.SUBTOTAL, 0) + ISNULL(E.FRTAMNT, 0) + ISNULL(E.MSCCHAMT, 0) + ISNULL(E.TAXAMNT, 0) - ISNULL(E.TRDISAMT, 0))) AS[UsedAmount] "
                              + "FROM (SELECT POPRequisitionNumber FROM " + Helpers.InterCompanyId + ".dbo.POP10200 UNION ALL SELECT POPRequisitionNumber FROM " + Helpers.InterCompanyId + ".dbo.POP30200 WHERE VOIDED = 0) A "
                              + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPRFQ10100 B ON B.RFQNMBR = A.POPRequisitionNumber AND B.TYPE = 3 LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP30102 D "
                              + "ON A.POPRequisitionNumber = D.PURCHREQ LEFT JOIN (SELECT PONUMBER, POSTATUS, SUBTOTAL, FRTAMNT, MSCCHAMT, TAXAMNT, TRDISAMT, CURNCYID "
                              + "FROM " + Helpers.InterCompanyId + ".dbo.POP10100 UNION ALL SELECT PONUMBER, POSTATUS, SUBTOTAL, FRTAMNT, MSCCHAMT, TAXAMNT, TRDISAMT, CURNCYID FROM " + Helpers.InterCompanyId + ".dbo.POP30100) E "
                              + "ON D.PONUMBE = E.PONUMBER AND E.POSTATUS <> 2 LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40400 F ON F.ARNUMBER = B.TXTFIELD LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40200 G "
                              + "ON F.PROYCTID = G.PROYCTID LEFT JOIN (SELECT DISTINCT A.PMNTNMBR, D.PONUMBER FROM " + Helpers.InterCompanyId + ".dbo.PM10300  A INNER JOIN " + Helpers.InterCompanyId + ".dbo.PM10200  B ON "
                              + "A.PMNTNMBR = B.VCHRNMBR AND A.VENDORID = B.VENDORID INNER JOIN " + Helpers.InterCompanyId + ".dbo.POP30300  C ON B.APTODCNM = C.VNDDOCNM INNER JOIN " + Helpers.InterCompanyId + ".dbo.POP30310  D ON C.POPRCTNM = D.POPRCTNM "
                              + "LEFT  JOIN " + Helpers.InterCompanyId + ".dbo.LPWF00201 F ON F.DOCNUM = A.PMNTNMBR AND F.WFSTS = 1) H ON H.PONUMBER = E.PONUMBER WHERE F.ARNUMBER <> 'NO' AND F.ARNUMBER IS NOT NULL ";

            var aRconsult = _repository.ExecuteQuery<ArConsultViewModel>(sqlQuery);
            return View(aRconsult);
        }

        public ActionResult Print()
        {
            try
            {
                string status = "OK";
                ReportHelper.Export(Helpers.ReportPath + "AR", Server.MapPath("~/PDF/AR/") + "AR000000Rpt" + ".pdf",String.Format("INTRANET.dbo.AR000000Rpt '{0}'", Helpers.InterCompanyId), 12, ref status); ;
            }
            catch (Exception ex)
            {
                return View(ex.Message);
            }
            return File("~/PDF/AR/AR000000Rpt.pdf", "application/pdf");
        }
    }
}