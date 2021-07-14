using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Data.Repository;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;

namespace Seaboard.Intranet.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly GenericRepository _repository;

        public HomeController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            var sqlQuery = "SELECT A.FRSTNAME FirstName, A.LASTNAME LastName, DAY(A.STRTDATE) Day, "
                           + "DATEDIFF(YEAR, A.STRTDATE, GETDATE()) Years, B.DSCRIPTN Department "
                           + "FROM " + Helpers.InterCompanyId + ".dbo.UPR00100 A "
                           + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40300 B ON A.DEPRTMNT = B.DEPRTMNT "
                           + "WHERE DAY(STRTDATE) = DAY(GETDATE()) AND MONTH(STRTDATE) = MONTH(GETDATE()) AND (A.INACTIVE = 0 OR UPPER(A.USERDEF2) = 'SI') "
                           + "ORDER BY [Day]";

            var anniversaryList = _repository.ExecuteQuery<HollidayPersonViewModel>(sqlQuery).ToList();

            sqlQuery = "SELECT A.FRSTNAME FirstName, A.LASTNAME LastName, DAY(A.BRTHDATE) Day, "
                       + "DATEDIFF(YEAR, A.BRTHDATE, GETDATE()) Years, B.DSCRIPTN Department "
                       + "FROM " + Helpers.InterCompanyId + ".dbo.UPR00100 A "
                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40300 B ON A.DEPRTMNT = B.DEPRTMNT "
                       + "WHERE DAY(BRTHDATE) = DAY(GETDATE()) AND MONTH(BRTHDATE) = MONTH(GETDATE()) AND (A.INACTIVE = 0 OR UPPER(A.USERDEF2) = 'SI') "
                       + "ORDER BY [Day]";

            var birthdayList = _repository.ExecuteQuery<HollidayPersonViewModel>(sqlQuery).ToList();

            var menu = _repository.GetAll<FoodMenu>(m => m.FoodMenuDate.Day == DateTime.Now.Day
                                                         && m.FoodMenuDate.Month == DateTime.Now.Month
                                                         && m.FoodMenuDate.Year == DateTime.Now.Year).FirstOrDefault();

            IEnumerable<FoodMenuLine> menuLine = null;

            if (menu != null)
                menuLine = _repository.GetAll<FoodMenuLine>(m => m.FoodMenuId == menu.FoodMenuId, includeProperties: "Food");

            var phoneExtensions = _repository.GetAll<EmployeeExtension>(includeProperties: "Department");
            sqlQuery = "SELECT TOP 1 CURRENCY CurrencyId, DAY, MONTH, YEAR, PURCHASE, SALES, ROUND(SALES, 2) TCCRate "
                       + "FROM SEABO.dbo.EXCHRATE ORDER BY YEAR DESC, MONTH DESC, DAY DESC";
            var currencies = _repository.ExecuteQuery<ExchangeRateViewModel>(sqlQuery).FirstOrDefault();

            ViewBag.Menus = menuLine;
            ViewBag.BirthDays = birthdayList;
            ViewBag.Anniversaries = anniversaryList;
            ViewBag.PhoneDirectory = phoneExtensions;
            ViewBag.Files = GetFiles(Helpers.PublicDocumentsPath);
            ViewBag.Images = GetImagesFiles();
            ViewBag.Currency = currencies;
            return View();
        }

        public ActionResult NotPermission()
        {
            return View();
        }

        public ActionResult TestWorkflow()
        {
            return View();
        }

        [Authorize]
        public ActionResult OpenPurchaseOrders()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Home", "OpenPurchaseOrders"))
                return RedirectToAction("NotPermission", "Home");
            var list = _repository.ExecuteQuery<OpenOrdersView>($"SELECT * FROM {Helpers.InterCompanyId}.dbo.Seabo_status_ordenes_abiertas").ToList();
            return View(list);
        }

        [Authorize]
        public ActionResult ApprovalHistory()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Home", "ApprovalHistory"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [HttpPost]
        public JsonResult ApprovalHistory(int modulo, string id)
        {
            string status;
            List<ApprovalHistory> list = null;
            string statusWorkflow = "";
            string pendingApprover = "";
            string documentDate = "";
            try
            {
                list = ProcessLogic.GetListSharepoint(modulo, id, ref statusWorkflow, ref pendingApprover, ref documentDate);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, registros = list, statusWorkflow, pendingApprover } };
        }

        [HttpPost]
        public JsonResult ApprovalHistoryReport(int modulo, string id)
        {
            string xStatus;
            string statusWorkflow = "";
            string pendingApprover = "";
            string module = "";
            string documentDate = "";

            List<ApprovalHistory> list = null;
            try
            {
                xStatus = "OK";
                list = ProcessLogic.GetListSharepoint(modulo, id, ref statusWorkflow, ref pendingApprover, ref documentDate);
                var tableDetail = new DataTable();
                switch (modulo)
                {
                    case 1:
                        module = "Almacen";
                        break;
                    case 2:
                        module = "Articulo";
                        break;
                    case 3:
                        module = "Servicio";
                        break;
                    case 4:
                        module = "Caja Chica";
                        break;
                    case 5:
                        module = "Orden de Compra";
                        break;
                    case 6:
                        module = "Analisis de Compra";
                        break;
                    case 7:
                        module = "Pago";
                        break;
                    case 8:
                        module = "Proveedor";
                        break;
                    case 9:
                        module = "Ausencias";
                        break;
                    case 10:
                        module = "Entrenamiento";
                        break;
                    case 11:
                        module = "Overtime";
                        break;
                    case 12:
                        module = "Usuario";
                        break;
                }
                tableDetail.Columns.Add("DocumentNumber", typeof(string));
                tableDetail.Columns.Add("DocumentDate", typeof(DateTime));
                tableDetail.Columns.Add("PendingApprover", typeof(string));
                tableDetail.Columns.Add("StatusWorkflow", typeof(string));
                tableDetail.Columns.Add("Module", typeof(string));
                tableDetail.Columns.Add("Requester", typeof(string));
                tableDetail.Columns.Add("Approver", typeof(string));
                tableDetail.Columns.Add("WorkflowDate", typeof(string));
                tableDetail.Columns.Add("ApproveStatus", typeof(string));
                tableDetail.Columns.Add("Marca", typeof(short));
                list.ForEach(p =>
                {
                    tableDetail.Rows.Add(id, Convert.ToDateTime(documentDate), pendingApprover, statusWorkflow, module, p.Requester, p.Approver, p.DateApproved, p.Status, 1);
                });
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "ApprovalHistoryReport.pdf", tableDetail, 0, ref xStatus);
            }
            catch (Exception ex)
            {

                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [OutputCache(Duration = 0)]
        public ActionResult ListLookupApproval(int tipo = 1, string consulta = "")
        {
            try
            {
                var sqlQuery = "";

                switch (tipo)
                {
                    case 1:
                        sqlQuery = $"SELECT TOP 100 RTRIM(A.DOCNUMBR) Id, RTRIM(A.SRCDOCNUM) Descripción, RTRIM(B.DEPRTMDS) DataExtended, CONVERT(NVARCHAR(10), DOCDATE, 103) DataPlus " +
                            $"FROM {Helpers.InterCompanyId}.dbo.LLIF10100 A " +
                            $"INNER JOIN {Helpers.InterCompanyId}.dbo.LPPOP40100 B " +
                            $"ON A.DEPTMTID = B.DEPRTMID " +
                            $"WHERE A.DOCTYPE = 1 " +
                            $"AND (A.DOCNUMBR LIKE '%{consulta}%' OR A.SRCDOCNUM LIKE '%{consulta}%' OR B.DEPRTMDS LIKE '%{consulta}%' OR CONVERT(NVARCHAR(10), A.DOCDATE, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.DOCNUMBR DESC";
                        break;
                    case 2:
                        sqlQuery = $"SELECT TOP 100 RTRIM(A.REQUESID) Id, RTRIM(A.ITEMDESC) Descripción, '' DataExtended, CONVERT(NVARCHAR(10), CREATDDT, 103) DataPlus " +
                            $"FROM {Helpers.InterCompanyId}.dbo.LPIV00101 A " +
                            $"WHERE (A.REQUESID LIKE '%{consulta}%' OR A.ITEMDESC LIKE '%{consulta}%' OR CONVERT(NVARCHAR(10), A.CREATDDT, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.REQUESID DESC";
                        break;
                    case 3:
                        sqlQuery = $"SELECT TOP 100 A.POPRequisitionNumber Id, A.RequisitionDescription Descripción, RTRIM(B.DEPRTMDS) DataExtended, CONVERT(NVARCHAR(10), A.DOCDATE, 103) DataPlus " +
                            $"FROM( " +
                            $"SELECT POPRequisitionNumber, RequisitionDescription, DOCDATE, USERDEF2 FROM {Helpers.InterCompanyId}.dbo.POP10200 " +
                            $"UNION ALL " +
                            $"SELECT POPRequisitionNumber, RequisitionDescription, DOCDATE, USERDEF2 FROM {Helpers.InterCompanyId}.dbo.POP30200) A " +
                            $"INNER JOIN {Helpers.InterCompanyId}.dbo.LPPOP40100 B " +
                            $"ON A.USERDEF2 = B.DEPRTMDS " +
                            $"WHERE (A.POPRequisitionNumber LIKE '%{consulta}%' OR A.RequisitionDescription LIKE '%{consulta}%' OR B.DEPRTMDS LIKE '%{consulta}%' " +
                            $"OR CONVERT(NVARCHAR(10), A.DOCDATE, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.POPRequisitionNumber DESC";
                        break;
                    case 4:
                        sqlQuery = $"SELECT TOP 100 RTRIM(A.REQUESID) Id, RTRIM(A.DESCRIPT) Descripción, RTRIM(A.DEPRTMID) DataExtended, CONVERT(NVARCHAR(10), A.CREATDDT, 103) DataPlus " +
                            $"FROM {Helpers.InterCompanyId}.dbo.LPPOP30600 A " +
                            $"WHERE (A.REQUESID LIKE '%{consulta}%' OR A.DESCRIPT LIKE '%{consulta}%' OR A.DEPRTMID LIKE '%{consulta}%' OR CONVERT(NVARCHAR(10), A.CREATDDT, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.REQUESID DESC";
                        break;
                    case 5:
                        sqlQuery = $"SELECT TOP 100 A.PONUMBER Id, RTRIM(A.VENDORID) + ' - ' + RTRIM(A.VENDNAME) Descripción, '' DataExtended, CONVERT(NVARCHAR(10), A.DOCDATE, 103) DataPlus " +
                            $"FROM {Helpers.InterCompanyId}.dbo.POP10100 A " +
                            $"WHERE (A.PONUMBER LIKE '%{consulta}%' OR RTRIM(A.VENDORID) LIKE '%{consulta}%' OR RTRIM(A.VENDNAME) LIKE '%{consulta}%' OR CONVERT(NVARCHAR(10), A.DOCDATE, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.PONUMBER DESC";
                        break;
                    case 6:
                        sqlQuery = $"SELECT TOP 100 A.ANLREQUS Id, A.REQSDESC Descripción, B.USERDEF2 DataExtended, CONVERT(NVARCHAR(10), A.CREATDDT, 103) DataPlus " +
                            $"FROM {Helpers.InterCompanyId}.dbo.LPPOP30100 A " +
                            $"INNER JOIN {Helpers.InterCompanyId}.dbo.POP30200 B ON A.PURCHREQ = B.POPRequisitionNumber " +
                            $"WHERE (A.ANLREQUS LIKE '%{consulta}%' OR RTRIM(A.REQSDESC) LIKE '%{consulta}%' OR B.USERDEF2 LIKE '%{consulta}%' OR CONVERT(NVARCHAR(10), A.CREATDDT, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.ANLREQUS DESC";
                        break;
                    case 7:
                        sqlQuery = $"SELECT TOP 100 PMNTNMBR Id, ISNULL(B.TXTFIELD, '') Descripción, RTRIM(A.VENDNAME) DataExtended, CONVERT(NVARCHAR(10), A.MODIFDT, 103) DataPlus " +
                            $"FROM {Helpers.InterCompanyId}.dbo.PM10300 A " +
                            $"LEFT JOIN {Helpers.InterCompanyId}.dbo.SY03900 B ON A.NOTEINDX = B.NOTEINDX " +
                            $"WHERE (A.PMNTNMBR LIKE '%{consulta}%' OR ISNULL(B.TXTFIELD,'') LIKE '%{consulta}%' OR A.VENDNAME LIKE '%{consulta}%' OR A.VENDORID LIKE '%{consulta}%' OR CONVERT(NVARCHAR(10), A.MODIFDT, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.PMNTNMBR DESC";
                        break;
                    case 8:
                        sqlQuery = $"SELECT TOP 100 A.REQUESID Id, A.VENDORID + ' - ' + A.COMPANY Descripción, A.TAXRGSNM DataExtended, CONVERT(NVARCHAR(10), A.CREATDDT, 103) DataPlus " +
                            $"FROM {Helpers.InterCompanyId}.dbo.LPPM00200 A " +
                            $"WHERE (A.REQUESID LIKE '%{consulta}%' OR RTRIM(A.VENDORID) LIKE '%{consulta}%' OR A.COMPANY LIKE '%{consulta}%' OR A.TAXRGSNM LIKE '%{consulta}%' OR CONVERT(NVARCHAR(10), A.CREATDDT, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.CREATDDT DESC";
                        break;
                    case 9:
                        sqlQuery = $"SELECT TOP 100 A.RequestId Id, A.Note Descripción, A.EmployeeId + ' - ' + (RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME)) DataExtended, CONVERT(NVARCHAR(10), A.CreatedDate, 103) DataPlus " +
                            $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30100 A " +
                            $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                            $"WHERE (A.RequestId LIKE '%{consulta}%' OR RTRIM(A.Note) LIKE '%{consulta}%' OR A.EmployeeId LIKE '%{consulta}%' OR (RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME)) LIKE '%{consulta}%' OR CONVERT(NVARCHAR(10), A.CreatedDate, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.RequestId DESC";
                        break;
                    case 10:
                        sqlQuery = $"SELECT TOP 100 A.RequestId Id, A.Description Descripción, A.Department DataExtended, CONVERT(NVARCHAR(10), A.StartDate, 103) DataPlus " +
                            $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30400 A " +
                            $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                            $"WHERE (A.RequestId LIKE '%{consulta}%' OR RTRIM(A.Description) LIKE '%{consulta}%' OR A.Department LIKE '%{consulta}%' OR CONVERT(NVARCHAR(10), A.StartDate, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.RequestId DESC";
                        break;
                    case 11:
                        sqlQuery = $"SELECT TOP 100 A.BatchNumber Id, A.[Description] Descripción, A.Approver DataExtended, CONVERT(NVARCHAR(10), A.CreatedDate, 103) DataPlus " +
                            $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30300 A " +
                            $"WHERE (A.BatchNumber LIKE '%{consulta}%' OR RTRIM(A.[Description]) LIKE '%{consulta}%' OR A.Approver LIKE '%{consulta}%' OR CONVERT(NVARCHAR(10), A.CreatedDate, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.CreatedDate DESC";
                        break;
                    case 12:
                        sqlQuery = $"SELECT TOP 100 A.RequestId Id, A.EmployeeId + ' - ' + (RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME)) Descripción, A.Department DataExtended, CONVERT(NVARCHAR(10), A.CreatedDate, 103) DataPlus " +
                            $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30500 A " +
                            $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                            $"WHERE (A.RequestId LIKE '%{consulta}%' OR A.EmployeeId LIKE '%{consulta}%' OR (RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME)) LIKE '%{consulta}%' OR CONVERT(NVARCHAR(10), A.CreatedDate, 103) LIKE '%{consulta}%') " +
                            $"ORDER BY A.RequestId DESC";
                        break;
                }

                var lookup = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();
                return Json(lookup, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("", JsonRequestBehavior.AllowGet);
            }
        }

        [OutputCache(Duration = 0)]
        public ActionResult ListLookup(int tipo = 1, string consulta = "", string consultaExtra = "")
        {
            try
            {
                var sqlQuery = "";

                switch (tipo)
                {
                    case 0:
                        sqlQuery = "SELECT TOP 50 RTRIM(A.ITEMNMBR) ItemId, RTRIM(A.ITEMDESC) ItemDesc, RTRIM(ISNULL(C.ITEMAREA, '')) ItemArea, "
                            + "RTRIM(A.LOCNCODE) Warehouse, RTRIM(B.BASEUOFM) UnitId "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.IV00101 A "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV40201 B "
                            + "ON A.UOMSCHDL = B.UOMSCHDL "
                            + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPIV30101 C "
                            + "ON A.ITEMNMBR = C.ITEMNMBR "
                            + "WHERE A.ITEMTYPE NOT IN (5, 2) AND ITMCLSCD <> 'FACT' AND (A.ITEMNMBR LIKE '%" +
                            consulta + "%' OR A.ITEMDESC LIKE '%" + consulta + "%' OR C.ITEMAREA LIKE '%" + consulta +
                            "%') "
                            + "ORDER BY A.ITEMNMBR ";
                        break;
                    case 1:
                        if (Account.GetAccount(User.Identity.GetUserName()).Department == "INFORMATICA" ||
                            Account.GetAccount(User.Identity.GetUserName()).Department == "RECURSOS HUMANOS")
                        {
                            sqlQuery = "SELECT RTRIM(SGMNTID) [Id], RTRIM(DSCRIPTN) [Descripción] FROM " +
                                       Helpers.InterCompanyId + ".dbo.GL40200 "
                                       + "WHERE SGMTNUMB = 2 AND LEN(RTRIM(DSCRIPTN)) > 0 AND (SGMNTID LIKE '%" +
                                       consulta + "%' OR DSCRIPTN LIKE '%" + consulta + "%') ";
                        }
                        else
                        {
                            sqlQuery = "SELECT DISTINCT A.DEPRTMID [Id], A.DEPRTMDS [Descripción] FROM " +
                                       Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                                       + "INNER JOIN " + Helpers.InterCompanyId +
                                       ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                                       + "WHERE RTRIM(B.USERID) = '" +
                                       Account.GetAccount(User.Identity.GetUserName()).UserId +
                                       "' AND (A.DEPRTMID LIKE '%" + consulta + "%' OR A.DEPRTMDS LIKE '%" + consulta + "%') ";
                        }
                        break;
                    case 2:
                        sqlQuery = "SELECT (CASE PRIORITY WHEN 0 THEN 'Baja' WHEN 1 THEN 'Normal' ELSE 'Alta' END) [Id], "
                            + "(CASE PRIORITY WHEN 0 THEN 'Baja' WHEN 1 THEN 'Normal' ELSE 'Alta' END) [Descripción] "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.LPSY00102 ORDER BY PRIORITY ASC ";
                        break;
                    case 3:
                        sqlQuery = "SELECT DISTINCT A.DEPRTMID FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                                   + "INNER JOIN " + Helpers.InterCompanyId +
                                   ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                                   + "WHERE RTRIM(B.USERID) = '" +
                                   Account.GetAccount(User.Identity.GetUserName()).UserId + "'";

                        var filter = "";

                        var departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

                        foreach (var item in departments)
                        {
                            if (filter.Length == 0)
                                filter = "'" + item + "'";
                            else
                                filter += ",'" + item + "'";
                        }

                        sqlQuery = "SELECT LTRIM(RTRIM(USERID)) [Id], LTRIM(RTRIM(NAME)) [Descripción] FROM " +
                                   Helpers.InterCompanyId + ".dbo.LPPOP40101 "
                                   + "WHERE TYPE = 1 AND DEPRTMID IN (" + filter + ") AND (USERID LIKE '%" + consulta +
                                   "%' OR NAME LIKE '%" + consulta + "%') "
                                   + "ORDER BY USERID ASC ";
                        break;
                    case 4:
                        sqlQuery = "SELECT 'NO' [Id], 'NO' [Descripción], CONVERT(nvarchar(20), 0.00) [DataExtended] UNION ALL SELECT LTRIM(RTRIM(ARNUMBER)) [Id], LTRIM(RTRIM(ARDESC)) [Descripción], " +
                            "CONVERT(nvarchar(20), CONVERT(NUMERIC(30,2), DOCAMNT)) [DataExtended] FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40400 "
                            + " WHERE ARNUMBER LIKE '%" + consulta + "%' OR ARDESC LIKE '%" + consulta + "%' ";
                        break;
                    case 5:
                        sqlQuery = "SELECT RTRIM(A.CURNCYID) [Id], RTRIM(A.CRNCYDSC) [Descripción] FROM DYNAMICS.dbo.MC40200 A INNER JOIN DYNAMICS.dbo.MC60100 B "
                            + "ON A.CURNCYID = B.CURNCYID "
                            + "WHERE B.INACTIVE = 0 AND B.CMPANYID = 1 AND (A.CURNCYID LIKE '%" + consulta +
                            "%' OR A.CRNCYDSC LIKE '%" + consulta + "%')";
                        break;
                    case 6:
                        sqlQuery =  "SELECT USERID [Id], USERNAME [Descripción] FROM DYNAMICS..SY01400 WHERE USERID LIKE '%" + consulta + "%' OR USERNAME LIKE '%" + consulta + "%'";
                        break;
                    case 7:
                        if (Account.GetAccount(User.Identity.GetUserName()).Department == "NEGOCIOS")
                            sqlQuery = "SELECT VENDORID [Id], VENDNAME [Descripción] FROM " + Helpers.InterCompanyId +
                                       ".dbo.PM00200 WITH (NOLOCK, READUNCOMMITTED) WHERE (VENDORID LIKE '%" + consulta + "%' OR VENDNAME " +
                                       "LIKE '%" + consulta + "%') AND VNDCLSID IN ('LOCALSPOT', 'LOCSLUNR', 'LOCALGEN') ORDER BY VENDORID";
                        else
                            sqlQuery = "SELECT VENDORID [Id], VENDNAME [Descripción] FROM " + Helpers.InterCompanyId +
                                       ".dbo.PM00200 WITH (NOLOCK, READUNCOMMITTED) WHERE " +
                                       "(VENDORID LIKE '%" + consulta + "%' OR VENDNAME LIKE '%" + consulta + "%') " +
                                       "AND VNDCLSID NOT IN ('LOCALSPOT', 'LOCSLUNR', 'LOCALGEN') ORDER BY VENDORID";
                        break;
                    case 8:
                        sqlQuery = "SELECT 'CHEQUE' [Id], 'CHEQUE' [Descripción] UNION ALL SELECT 'TRANSF' [Id], 'TRANSFERENCIA' [Descripción] ";
                        break;
                    case 9:
                        sqlQuery = "SELECT RTRIM(LOCNCODE) [Id], LOCNDSCR [Descripción] FROM " +
                                   Helpers.InterCompanyId +
                                   ".dbo.IV40700 WITH (NOLOCK, READUNCOMMITTED) WHERE LOCNCODE LIKE '%" + consulta +
                                   "%' OR LOCNDSCR LIKE '%" + consulta + "%' ORDER BY LOCNCODE";
                        break;
                    case 10:
                        sqlQuery = "SELECT DISTINCT RTRIM(UOFM) [Id], UOFM [Descripción] FROM " + Helpers.InterCompanyId + ".dbo.IV00106 WITH (NOLOCK, READUNCOMMITTED) " +
                            "WHERE ITEMNMBR = '" + consultaExtra + "' AND UOFM LIKE '%" + consulta + "%'";
                        break;
                    case 11:
                        sqlQuery = "SELECT RTRIM(ACTALIAS) [Id], ACTDESCR [Descripción], (RTRIM(LTRIM(ACTNUMBR_1)) + '-' + RTRIM(LTRIM(ACTNUMBR_2)) + '-' + RTRIM(LTRIM(ACTNUMBR_3))) [DataExtended] FROM " +
                            Helpers.InterCompanyId +
                            ".dbo.GL00100 WITH (NOLOCK, READUNCOMMITTED) WHERE ACTALIAS <> '' AND ((RTRIM(LTRIM(ACTNUMBR_1)) + '-' + RTRIM(LTRIM(ACTNUMBR_2)) + '-' + RTRIM(LTRIM(ACTNUMBR_3))) LIKE '%" +
                            consulta + "%' OR ACTDESCR LIKE '%" + consulta + "%' OR ACTALIAS LIKE '%" + consulta + "%') ORDER BY [Id] ASC ";
                        break;
                    case 12:
                        if (Account.GetAccount(User.Identity.GetUserName()).Department == "INFORMATICA" ||
                            Account.GetAccount(User.Identity.GetUserName()).Department == "RECURSOS HUMANOS")
                            sqlQuery = "SELECT RTRIM(SGMNTID) [Id], RTRIM(DSCRIPTN) [Descripción] FROM " +
                                       Helpers.InterCompanyId + ".dbo.GL40200 "
                                       + "WHERE SGMTNUMB = 2 AND LEN(RTRIM(DSCRIPTN)) > 0 AND (SGMNTID LIKE '%" +
                                       consulta + "%' OR DSCRIPTN LIKE '%" + consulta + "%') ";
                        else
                            sqlQuery = "SELECT DISTINCT A.DEPRTMID [Id], A.DEPRTMDS [Descripción] FROM " +
                                       Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                                       + "INNER JOIN " + Helpers.InterCompanyId +
                                       ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                                       + "WHERE RTRIM(B.USERID) = '" +
                                       Account.GetAccount(User.Identity.GetUserName()).UserId +
                                       "' AND (A.DEPRTMID LIKE '%" + consulta + "%' OR A.DEPRTMDS LIKE '%" + consulta + "%') ";
                        break;
                    case 13:
                        sqlQuery ="SELECT CONVERT(nvarchar(20), FoodId) [Id], FoodName [Descripción] FROM INTRANET.dbo.FOOD WHERE FoodName LIKE '%" +consulta + "%'";
                        break;
                    case 14:
                        sqlQuery ="SELECT TOP 50 RTRIM(A.ITEMNMBR) ItemId, RTRIM(A.ITEMDESC) ItemDesc, RTRIM(ISNULL(C.ITEMAREA, '')) ItemArea, "
                            + "RTRIM(A.LOCNCODE) Warehouse, RTRIM(B.BASEUOFM) UnitId "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.IV00101 A "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.IV40201 B "
                            + "ON A.UOMSCHDL = B.UOMSCHDL "
                            + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPIV30101 C "
                            + "ON A.ITEMNMBR = C.ITEMNMBR "
                            + "WHERE A.ITEMTYPE = 5 AND ITMCLSCD <> 'FACT' AND (A.ITEMNMBR LIKE '%" + consulta +
                            "%' OR A.ITEMDESC LIKE '%" + consulta + "%' OR C.ITEMAREA LIKE '%" + consulta + "%')";
                        break;
                    case 15:
                        sqlQuery = "SELECT RTRIM(TAXDTLID) [Id], RTRIM(TXDTLDSC) [Descripción] FROM " +
                                   Helpers.InterCompanyId +
                                   ".dbo.TX00201 WHERE TXDTLTYP = 2 AND TXDTLBSE = 3 AND TXDTLPCT >= 0 AND TAXDTLID LIKE '%" +
                                   consulta + "%' AND TXDTLDSC LIKE '%" + consulta + "%'";
                        break;
                    case 16:
                        sqlQuery = "SELECT RTRIM(BACHNUMB) [Id], RTRIM(BACHNUMB) [Descripción] FROM " +
                                   Helpers.InterCompanyId +
                                   ".dbo.SY00500 WHERE BCHSOURC = 'XPM_Cchecks' AND BACHNUMB LIKE '%" + consulta + "%'";
                        break;
                    case 17:
                        sqlQuery = "SELECT [PONUMBER] [Id], [VENDNAME] [Descripción] FROM " + Helpers.InterCompanyId +
                                   ".dbo.POP10100 WHERE PONUMBER LIKE '%" + consulta + "%' OR VENDNAME LIKE '%" +
                                   consulta + "%'";
                        break;
                    case 18:
                        sqlQuery ="SELECT A.EMPLOYID EmployeeId, RTRIM(A.FRSTNAME) + ' ' + RTRIM(A.LASTNAME) Name, RTRIM(A.FRSTNAME) FirstName, "
                            + "RTRIM(A.LASTNAME) LastName, RTRIM(A.USERDEF1) Identification, RTRIM(B.DSCRIPTN) Department, RTRIM(C.DSCRIPTN) JobTitle, RTRIM(ISNULL(D.INET1, '')) Email "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.UPR00100 A "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40300 B "
                            + "ON A.DEPRTMNT = B.DEPRTMNT "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40301 C "
                            + "ON A.JOBTITLE = C.JOBTITLE "
                            + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY01200 D "
                            + "ON A.EMPLOYID = D.Master_ID AND D.Master_Type = 'EMP' "
                            + "WHERE (INACTIVE = 0 OR UPPER(USERDEF2) = 'SI') AND (A.EMPLOYID LIKE '%" + consulta +
                            "%' OR (RTRIM(A.FRSTNAME) + ' ' + RTRIM(A.LASTNAME)) LIKE '%" + consulta + "%') "
                            + "ORDER BY A.EMPLOYID ";
                        break;

                    case 19:
                        sqlQuery = "SELECT DISTINCT A.DEPRTMID FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                                   + "INNER JOIN " + Helpers.InterCompanyId +
                                   ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                                   + "WHERE RTRIM(B.USERID) = '" +
                                   Account.GetAccount(User.Identity.GetUserName()).UserId + "'";

                        filter = "";

                        departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

                        foreach (var item in departments)
                        {
                            if (filter.Length == 0)
                                filter = "'" + item + "'";
                            else
                                filter += ",'" + item + "'";
                        }
                        sqlQuery ="SELECT RTRIM(DOCNUMBR) RequestId, RTRIM(SRCDOCNUM) Description, GETDATE() DocumentDate, "
                            + "GETDATE() RequiredDate, '' QuoteId, '' Priority, '' Status, '' WorkNumber, '' AR, '' Requester "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 "
                            + "WHERE DEPTMTID IN (" + filter + ") AND DOCTYPE = 1 "
                            + "AND (DOCNUMBR LIKE '%" + consulta + "%' OR SRCDOCNUM LIKE '%" + consulta + "%') "
                            + "ORDER BY DOCNUMBR ";
                        break;
                    case 20:
                        sqlQuery = "SELECT DISTINCT A.DEPRTMDS FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                                   + "INNER JOIN " + Helpers.InterCompanyId +
                                   ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                                   + "WHERE RTRIM(B.USERID) = '" +
                                   Account.GetAccount(User.Identity.GetUserName()).UserId + "'";

                        filter = "";

                        departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

                        foreach (var item in departments)
                        {
                            if (filter.Length == 0)
                                filter = "'" + item + "'";
                            else
                                filter += ",'" + item + "'";
                        }
                        sqlQuery ="SELECT RTRIM(A.POPRequisitionNumber) RequestId, RTRIM(A.RequisitionDescription) Description, GETDATE() DocumentDate, "
                            + "GETDATE() RequiredDate, '' QuoteId, '' Priority, '' Status, '' WorkNumber, '' AR, '' Requester FROM "
                            + "( "
                            + "SELECT POPRequisitionNumber, RequisitionDescription, USERDEF2 FROM " +
                            Helpers.InterCompanyId + ".dbo.POP10200 "
                            + "UNION ALL "
                            + "SELECT POPRequisitionNumber, RequisitionDescription, USERDEF2 FROM " +
                            Helpers.InterCompanyId + ".dbo.POP30200 "
                            + ") A "
                            + "WHERE A.USERDEF2 IN (" + filter + ") "
                            + "AND (A.POPRequisitionNumber LIKE '%" + consulta +
                            "%' OR A.RequisitionDescription LIKE '%" + consulta + "%') "
                            + "ORDER BY A.POPRequisitionNumber ";

                        break;

                    case 21:
                        sqlQuery = "SELECT RTRIM(SGMNTID) [Id], RTRIM(DSCRIPTN) [Descripción] "
                                   + "FROM " + Helpers.InterCompanyId + ".dbo.GL40200 "
                                   + "WHERE SGMTNUMB = 2 AND LEN(RTRIM(DSCRIPTN)) > 0 AND (SGMNTID LIKE '%" + consulta +
                                   "%' OR DSCRIPTN LIKE '%" + consulta + "%') "
                                   + "UNION ALL "
                                   + "SELECT RTRIM(EMPLOYID) [Id], RTRIM(FRSTNAME) + ' ' + RTRIM(LASTNAME) [Descripción] "
                                   + "FROM " + Helpers.InterCompanyId + ".dbo.UPR00100 "
                                   + "WHERE (INACTIVE = 0 OR UPPER(USERDEF2) = 'SI') AND (EMPLOYID LIKE '%" + consulta +
                                   "%' OR (RTRIM(FRSTNAME) + ' ' + RTRIM(LASTNAME)) LIKE '%" + consulta + "%')";
                        break;
                    case 22:
                        sqlQuery = "SELECT DISTINCT RTRIM(ITMCLSCD) [Id], RTRIM(ITMCLSDC) [Descripción] FROM " +
                                   Helpers.InterCompanyId +
                                   ".dbo.IV40400 WITH (NOLOCK, READUNCOMMITTED) WHERE ITMCLSCD LIKE '%" + consulta +
                                   "%' OR ITMCLSDC LIKE '%" + consulta + "%'";
                        break;
                    case 23:
                        sqlQuery = "SELECT RTRIM(SGMNTID) [Id], RTRIM(DSCRIPTN) [Descripción] FROM " +
                                   Helpers.InterCompanyId + ".dbo.GL40200 "
                                   + "WHERE SGMTNUMB = 2 AND LEN(RTRIM(DSCRIPTN)) > 0 AND (SGMNTID LIKE '%" + consulta +
                                   "%' OR DSCRIPTN LIKE '%" + consulta + "%') ";
                        break;
                    case 24:
                        sqlQuery = "SELECT RTRIM(DEPTMTID) [Id], RTRIM(DEPTDESC) [Descripción] FROM " +
                                   Helpers.InterCompanyId +
                                   ".dbo.LLIF00100 WITH (NOLOCK, READUNCOMMITTED) WHERE RTRIM(DEPTMTID) LIKE '%" +
                                   consulta + "%' OR RTRIM(DEPTDESC) LIKE '%" + consulta + "%' ORDER BY DEPTMTID";
                        break;
                    case 25:
                        sqlQuery ="SELECT RTRIM(A.ACTNUMST) Id, RTRIM(B.ACTDESCR) Descripción, RTRIM(B.ACTALIAS) DataExtended FROM  " +
                            Helpers.InterCompanyId + ".dbo.GL00100 B INNER JOIN  " + Helpers.InterCompanyId +
                            ".dbo.GL00105 A ON A.ACTINDX = B.ACTINDX WHERE RTRIM(A.ACTNUMST) LIKE '%" + consulta +
                            "%' OR RTRIM(B.ACTALIAS) LIKE '%" + consulta + "%' OR RTRIM(B.ACTDESCR) LIKE '%" +
                            consulta + "%' ORDER BY A.ACTNUMST";
                        break;
                    case 26:
                        sqlQuery = "SELECT DISTINCT(LTRIM(RTRIM(A.supplierId))) AS [ItemId],(LTRIM(RTRIM(A.BankName))) AS [ItemNam],(LTRIM(RTRIM(A.AccountNumber))) AS [ItemDesc]," +
                            "(LTRIM(RTRIM(A.TransferId))) AS [Posted], '' As [DataExtended] " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.TRANSFER_AUTHORIZATION A " +
                            "INNER JOIN (SELECT VENDORID, PMNTNMBR FROM " + Helpers.InterCompanyId + ".dbo.PM10300 " +
                            "UNION ALL SELECT VENDORID, VCHRNMBR AS[PMNTNMBR] FROM " + Helpers.InterCompanyId +
                            ".dbo.PM20000) B ON A.supplierId = B.VENDORID " +
                            "WHERE A.supplierId = '" + consultaExtra + "'";
                        break;
                    case 27:
                        sqlQuery = "SELECT DISTINCT(LTRIM(RTRIM(A.BACHNUMB))) AS [ItemId],(LTRIM(RTRIM(A.BCHCOMNT))) AS [ItemNam], LTRIM(RTRIM([STATUS])) AS [ItemDesc], " +
                            "LTRIM(RTRIM(CONVERT(VARCHAR(20), POSTEDDT, 101))) AS [Posted] " +
                            "FROM (SELECT BACHNUMB, BCHCOMNT, POSTEDDT, STATUS FROM " + Helpers.InterCompanyId +
                            ".dbo.TEMPUPR10301 " +
                            "UNION ALL SELECT BACHNUMB, BCHCOMNT, POSTEDDT,STATUS FROM " + Helpers.InterCompanyId +
                            ".dbo.HISTEMPUPR10301) A " +
                            "WHERE A.BACHNUMB LIKE '%" + consulta + "%'";
                        break;
                    case 28:
                        sqlQuery = $"INTRANET.dbo.GetAccountReceivablesBatchTransEntry '{Helpers.InterCompanyId}'";
                        break;
                    case 29:
                        sqlQuery = $"INTRANET.dbo.GetAccountReceivablesBatchTransCreate '{Helpers.InterCompanyId}'";
                        break;
                    case 30:
                        sqlQuery = "SELECT RTRIM(CUSTNMBR) [Id], RTRIM(CUSTNAME) [Descripción] FROM " +
                                   Helpers.InterCompanyId +
                                   ".dbo.RM00101 WITH (NOLOCK, READUNCOMMITTED) WHERE CUSTNMBR LIKE '%" + consulta +
                                   "%' OR CUSTNAME LIKE '%" + consulta + "%' ORDER BY CUSTNMBR";
                        break;
                    case 31:
                        sqlQuery = "SELECT BACHNMBR BatchNumber, BACHDESC Description, CUSTNMBR CustomerId, CUSTNAME CustomerName, VENDORID VendorId, " +
                            "VENDNAME VendorName, CURRCYID CurrencyId, CONVERT(NVARCHAR(10), DOCDATE, 103) DocumentDate, CONVERT(NVARCHAR(10), CLOSDATE, 103) CloseDate, NOTE Note, POSTED Posted " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.SESOP30501 " +
                            "WHERE (BACHNMBR LIKE '%" + consulta +
                            "%' OR BACHDESC LIKE '%" + consulta + "%') AND POSTED = 0 ORDER BY BACHNMBR";
                        break;
                    case 32:
                        sqlQuery = "SELECT DISTINCT RTRIM(CLASSID) [Id], RTRIM(CLASDSCR) [Descripción] FROM " +
                                   Helpers.InterCompanyId +
                                   ".dbo.RM00201 WITH (NOLOCK, READUNCOMMITTED) WHERE CLASSID LIKE '%" + consulta +
                                   "%' OR CLASDSCR LIKE '%" + consulta + "%'";
                        break;
                    case 33:
                        sqlQuery = "SELECT  RTRIM(DOCNUMBR) CustomerId, CONVERT(VARCHAR, CONVERT(MONEY, ORTRXAMT), 1) Description, CONVERT(VARCHAR, CONVERT(MONEY, CURTRXAM), 1) BatchNumber, '' CustomerName, '' VendorId, " +
                            "'' VendorName, CURNCYID CurrencyId, '' DocumentDate, '' CloseDate, '' Note, 0 Posted " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.RM20101 " +
                            "WHERE DOCNUMBR LIKE '%" + consulta +
                            "%' AND CURTRXAM > 0 AND CUSTNMBR = '" + consultaExtra +
                            "' AND RMDTYPAL IN(7, 8, 9) ORDER BY DOCNUMBR";
                        break;
                    case 34:
                        sqlQuery = "SELECT RTRIM(DOCNUMBR) CustomerId, CONVERT(VARCHAR, CONVERT(MONEY, DOCAMNT), 1) Description, CONVERT(VARCHAR, CONVERT(MONEY, CURTRXAM), 1) BatchNumber, " +
                            "'' CustomerName, '' VendorId, '' VendorName, CURNCYID CurrencyId, '' DocumentDate, '' CloseDate, '' Note, 0 Posted " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.PM20000 " +
                            "WHERE DOCNUMBR LIKE '%" + consulta + "%' AND CURTRXAM > 0 AND VENDORID = '" + consultaExtra + "' AND DOCTYPE IN (4, 5, 6) ORDER BY DOCNUMBR";
                        break;
                    case 35:
                        sqlQuery = "SELECT USERID Id, RTRIM(FirstName) + ' ' + RTRIM(LastName) Descripción, RTRIM(Department) DataExtended " +
                            "FROM INTRANET.dbo.USERS " +
                            "WHERE UserId LIKE '%" + consulta +
                            "%' OR (RTRIM(FirstName) + ' ' + RTRIM(LastName)) LIKE '%" + consulta +
                            "%' ORDER BY UserId";
                        break;
                    case 36:
                        sqlQuery = "SELECT RTRIM(Name) Id, RTRIM(Name) Descripción, '' DataExtended " +
                            "FROM INTRANET.dbo.PERMISSIONS " +
                            "WHERE Name LIKE '%" + consulta + "%' ORDER BY Name";
                        break;
                    case 37:
                        switch (consultaExtra)
                        {
                            case "PM_Trxent":
                            case "PM_Payment":
                            case "Rcvg Trx Entry":
                                sqlQuery = "SELECT RTRIM(BACHNUMB) ItemId, RTRIM(BCHCOMNT) ItemNam, RTRIM(BCHSOURC) ItemDesc, CONVERT(VARCHAR, CONVERT(MONEY, BCHTOTAL), 1) DataExtended, CONVERT(NVARCHAR(20), NUMOFTRX) DataPlus " +
                                    "FROM " + Helpers.InterCompanyId + ".dbo.SY00500 " +
                                    "WHERE (BACHNUMB LIKE '%" + consulta + "%' OR BCHCOMNT LIKE '%" + consulta +
                                    "%') " +
                                    "AND BCHSOURC = '" + consultaExtra + "' AND BCHSTTUS = 0 AND BACHNUMB IN ( " +
                                    "SELECT A.BACHNUMB FROM " + Helpers.InterCompanyId + ".dbo.PM10000 A INNER JOIN " +
                                    Helpers.InterCompanyId + ".dbo.PM00200 B ON A.VENDORID = B.VENDORID " +
                                    "WHERE A.BCHSOURC = '" + consultaExtra +
                                    "' AND B.VNDCLSID IN ('LOCALSPOT', 'LOCSLUNR', 'LOCALGEN') UNION ALL " +
                                    "SELECT A.BACHNUMB FROM " + Helpers.InterCompanyId + ".dbo.POP10300 A INNER JOIN " +
                                    Helpers.InterCompanyId + ".dbo.PM00200 B ON A.VENDORID = B.VENDORID " +
                                    "WHERE A.BCHSOURC = '" + consultaExtra +
                                    "' AND B.VNDCLSID IN ('LOCALSPOT', 'LOCSLUNR', 'LOCALGEN') UNION ALL " +
                                    "SELECT A.BACHNUMB FROM " + Helpers.InterCompanyId + ".dbo.PM10400 A INNER JOIN " +
                                    Helpers.InterCompanyId + ".dbo.PM00200 B ON A.VENDORID = B.VENDORID " +
                                    "WHERE A.BCHSOURC = '" + consultaExtra +
                                    "' AND B.VNDCLSID IN ('LOCALSPOT', 'LOCSLUNR', 'LOCALGEN')) " +
                                    "ORDER BY BACHNUMB";
                                break;
                            default:
                                sqlQuery = "SELECT RTRIM(BACHNUMB) ItemId, RTRIM(BCHCOMNT) ItemNam, RTRIM(BCHSOURC) ItemDesc, CONVERT(VARCHAR, CONVERT(MONEY, BCHTOTAL), 1) DataExtended, CONVERT(NVARCHAR(20), NUMOFTRX) DataPlus  " +
                                    "FROM " + Helpers.InterCompanyId + ".dbo.SY00500 " +
                                    "WHERE (BACHNUMB LIKE '%" + consulta + "%' OR BCHCOMNT LIKE '%" + consulta +
                                    "%') " +
                                    "AND BCHSOURC = '" + consultaExtra + "' AND BCHSTTUS = 0 " +
                                    "ORDER BY BACHNUMB";
                                break;
                        }
                        break;
                    case 38:
                        sqlQuery = "SELECT RTRIM(CTRYCODE) Id, RTRIM(CTRYDESC) Descripción " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.EFLC00100 " +
                            "WHERE CTRYCODE LIKE '%" + consulta + "%' OR CTRYDESC LIKE '%" + consulta + "%' ORDER BY CTRYCODE";
                        break;
                    case 39:
                        sqlQuery = "SELECT RTRIM(PROVCODE) Id, RTRIM(PROVDESC) Descripción " +
                        "FROM " + Helpers.InterCompanyId + ".dbo.EFLC00200 " +
                        "WHERE (PROVCODE LIKE '%" + consulta + "%' OR PROVDESC LIKE '%" + consulta + "%') AND CTRYCODE = '" + consultaExtra + "'  ORDER BY PROVCODE";
                        break;
                    case 40:
                        sqlQuery = "SELECT RTRIM(CITYCODE) Id, RTRIM(CITYDESC) Descripción " +
                        "FROM " + Helpers.InterCompanyId + ".dbo.EFLC00300 " +
                        "WHERE (CITYCODE LIKE '%" + consulta + "%' OR CITYDESC LIKE '%" + consulta + "%') AND CTRYCODE = '" + consultaExtra.Split(',')[0] + "' " +
                        "AND PROVCODE = '" + consultaExtra.Split(',')[1] + "'  ORDER BY CITYCODE";
                        break;
                    case 41:
                        sqlQuery = "SELECT RTRIM(DOCNUMBR) Id, RTRIM(DOCDESCR) Descripción, '' DataExtended " +
                        "FROM " + Helpers.InterCompanyId + ".dbo.EFNCF40101 " +
                        "WHERE (DOCNUMBR LIKE '%" + consulta + "%' OR DOCDESCR LIKE '%" + consulta + "%') ORDER BY DOCNUMBR";
                        break;
                    case 42:
                        sqlQuery = "SELECT RTRIM(ITEMNMBR) Id, RTRIM(ITEMDESC) Descripción, '' DataExtended " +
                        "FROM " + Helpers.InterCompanyId + ".dbo.IV00101 " +
                        "WHERE ITMCLSCD = 'FACT' AND (ITEMNMBR LIKE '%" + consulta + "%' OR ITEMDESC LIKE '%" + consulta + "%') ORDER BY ITEMNMBR";
                        break;
                    case 43:
                        sqlQuery = "SELECT RTRIM(BatchNumber) Id, RTRIM(BatchDescription) Descripción, '' DataExtended " +
                        "FROM " + Helpers.InterCompanyId + ".dbo.EFSOP20500 " +
                        "WHERE (BatchNumber LIKE '%" + consulta + "%' OR BatchDescription LIKE '%" + consulta + "%') AND Posted = 1 ORDER BY BatchNumber";
                        break;
                    case 44:
                        sqlQuery = "SELECT RTRIM(PONUMBER) Id, RTRIM(PURCHCMPNYNAM) Descripción, '' DataExtended " +
                        "FROM " + Helpers.InterCompanyId + ".dbo.POP10100 " +
                        "WHERE (PONUMBER LIKE '%" + consulta + "%' OR PURCHCMPNYNAM LIKE '%" + consulta + "%') AND Workflow_Status <> 6 ORDER BY PONUMBER";
                        break;
                    case 45:
                        sqlQuery = $"SELECT Id, Descripción, DataExtended FROM (" +
                        $"SELECT RTRIM(A.BACHNUMB) Id, RTRIM(A.BCHCOMNT) Descripción, CONVERT(NVARCHAR(10), CREATDDT, 103) DataExtended, A.CREATDDT DATEBACH " +
                        $"FROM {Helpers.InterCompanyId}.dbo.SY00500 A " +
                        $"WHERE A.BCHSOURC = 'Sales Entry' AND (A.BACHNUMB LIKE '%{consulta}%' OR A.BCHCOMNT LIKE '%{consulta}%') " +
                        $"UNION ALL " +
                        $"SELECT RTRIM(A.BACHNUMB) Id, RTRIM(A.BCHCOMNT) Descripción, CONVERT(NVARCHAR(10), GLPOSTDT, 103) DataExtended, A.GLPOSTDT DATEBACH " +
                        $"FROM {Helpers.InterCompanyId}.dbo.SOP30100 A " +
                        $"WHERE A.BCHSOURC = 'Sales Entry' AND (A.BACHNUMB LIKE '%{consulta}%' OR A.BCHCOMNT LIKE '%{consulta}%')) A " +
                        $"ORDER BY A.DATEBACH DESC";
                        break;
                    case 46:
                        sqlQuery = "SELECT RTRIM(A.DOCNUMBR) Id, A.CURNCYID Descripción, CONVERT(NVARCHAR(10), A.DOCDATE, 103) DataPlus, CONVERT(NVARCHAR(10), A.DUEDATE, 103) DataExtended " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.RM20101 A " +
                            "WHERE A.DOCNUMBR LIKE '%" + consulta + "%' AND A.CUSTNMBR = '" + consultaExtra + "' AND A.RMDTYPAL IN(1, 3) " +
                            "ORDER BY A.DOCDATE DESC";
                        break;
                    case 47:
                        sqlQuery = "SELECT RTRIM(VNDCLSID) Id, RTRIM(VNDCLDSC) Descripción, '' DataExtended " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.PM00100 " +
                            "WHERE VNDCLSID LIKE '%" + consulta + "%' OR VNDCLDSC LIKE '%" + consulta + "%' " +
                            "ORDER BY VNDCLSID DESC";
                        break;
                    case 48:
                        sqlQuery = "SELECT RTRIM(DEPRTMNT) Id, RTRIM(DSCRIPTN) Descripción, '' DataExtended " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.UPR40300 " +
                            "WHERE DEPRTMNT LIKE '%" + consulta + "%' OR DSCRIPTN LIKE '%" + consulta + "%' " +
                            "ORDER BY DSCRIPTN";
                        break;
                    case 49:
                        sqlQuery = "SELECT RTRIM(FieldId) Id, RTRIM(FieldDescription) Descripción, '' DataExtended, '' DataPlus " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.EHUPR50100 " +
                            "WHERE FieldId LIKE '%" + consulta + "%' OR FieldDescription LIKE '%" + consulta + "%' " +
                            "ORDER BY RowId";
                        break;
                    case 50:
                        sqlQuery = "SELECT RTRIM(GroupCode) Id, RTRIM(GroupDescription) Descripción, '' DataExtended, '' DataPlus " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.EHUPR40100 " +
                            "WHERE GroupCode LIKE '%" + consulta + "%' OR GroupDescription LIKE '%" + consulta + "%' " +
                            "ORDER BY GroupCode";
                        break;
                    case 51:
                        sqlQuery = "SELECT RTRIM(JOBTITLE) Id, RTRIM(DSCRIPTN) Descripción, '' DataExtended, '' DataPlus " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.UPR40301 " +
                            "WHERE JOBTITLE LIKE '%" + consulta + "%' OR DSCRIPTN LIKE '%" + consulta + "%' " +
                            "ORDER BY JOBTITLE";
                        break;
                    case 52:
                        sqlQuery = "SELECT RTRIM(A.SUPERVISORCODE_I) Id, RTRIM(A.SUPERVISOR) Descripción, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) DataExtended, '' DataPlus " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.UPR41700 A " +
                            "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR00100 B ON A.EMPLOYID = B.EMPLOYID " +
                            "WHERE A.SUPERVISORCODE_I LIKE '%" + consulta + "%' OR A.SUPERVISOR LIKE '%" + consulta + "%' OR B.FRSTNAME LIKE '%" + consulta + "%' OR B.LASTNAME LIKE '%" + consulta + "%' " +
                            "ORDER BY A.SUPERVISORCODE_I";
                        break;
                    case 53:
                        sqlQuery = "SELECT RTRIM(BANKID) Id, RTRIM(BANKNAME) Descripción, '' DataExtended, '' DataPlus " +
                            "FROM " + Helpers.InterCompanyId + ".dbo.SY04100 " +
                            "WHERE BANKID LIKE '%" + consulta + "%' OR BANKNAME LIKE '%" + consulta + "%' " +
                            "ORDER BY BANKID";
                        break;
                }

                switch (tipo)
                {
                    case 0:
                    case 14:
                        var itemLookup = _repository.ExecuteQuery<Item>(sqlQuery).ToList();
                        return Json(itemLookup, JsonRequestBehavior.AllowGet);
                    case 18:
                        {
                            var userLookup = _repository.ExecuteQuery<UserEmployeeViewModel>(sqlQuery).ToList();
                            return Json(userLookup, JsonRequestBehavior.AllowGet);
                        }
                    case 19:
                    case 20:
                        {
                            var userLookup = _repository.ExecuteQuery<PurchaseRequestViewModel>(sqlQuery).ToList();
                            return Json(userLookup, JsonRequestBehavior.AllowGet);
                        }
                    case 26:
                    case 27:
                    case 28:
                    case 29:
                    case 37:
                        {
                            var userLookup = _repository.ExecuteQuery<ItemLookup>(sqlQuery).ToList();
                            return Json(userLookup, JsonRequestBehavior.AllowGet);
                        }
                    case 31:
                    case 33:
                    case 34:
                        var netTransLookup = _repository.ExecuteQuery<MemNetTrans>(sqlQuery).ToList();
                        return Json(netTransLookup, JsonRequestBehavior.AllowGet);
                    default:
                        var lookup = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();
                        return Json(lookup, JsonRequestBehavior.AllowGet);
                }
            }
            catch
            {
                return Json("", JsonRequestBehavior.AllowGet);
            }
        }

        #region Other Methods

        [HttpPost]
        public ActionResult SendWorkflow(string type, string id)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                //ProcessLogic.SendToSharepoint(id, Convert.ToInt32(type), Account.GetAccount(User.Identity.GetUserName()).Email, ref xStatus);
                ProcessLogic.SendToSharepointAsync(id, Convert.ToInt32(type), Account.GetAccount(User.Identity.GetUserName()).Email);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public ActionResult FoodMenuToday()
        {
            var today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            var foodMenu = _repository.GetAll<FoodMenu>(m => m.FoodMenuDate == today).FirstOrDefault();
            if (foodMenu == null) return PartialView("_FoodMenuPartial");
            var foodMenuLines =
                _repository.GetAll<FoodMenuLine>(i => i.FoodMenuId == foodMenu.FoodMenuId,
                    includeProperties: "Food");
            if (foodMenuLines != null)
            {
                ViewBag.FoodMenuDetails = foodMenuLines;
            }

            return PartialView("_FoodMenuPartial");
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult SaveItemRequest(string itemRequestDescription, string itemRequestComment, string itemRequestRequester)
        {
            string xStatus;

            try
            {
                xStatus = "OK";
                var secuence = HelperLogic.AsignaciónSecuencia("LPIV00101",Account.GetAccount(User.Identity.GetUserName()).UserId);
                _repository.ExecuteCommand(string.Format(
                    "LODYNDEV.dbo.LPIV00101SI '{0}','{1}',[{2}],[{3}],[{4}],'{5}','{6}','{7}','{8}','{9}','{10}',[{11}],'{12}','{13}','{14}','{15}','{16:yyyy-MM-ddThh:mm:ss}','{17:yyyy-MM-ddThh:mm:ss}','{18:yyyy-MM-ddThh:mm:ss}','{19}','{20}','{21}','{22}','{23}'",
                    Helpers.InterCompanyId, secuence, itemRequestDescription, itemRequestDescription,
                    itemRequestDescription, "", 0, 0, "", 0, 0, itemRequestComment.Length == 0 ? "N" : itemRequestComment, itemRequestRequester, "", "", "",
                    DateTime.Now, DateTime.Now, DateTime.Now, 0, 0, 0, 0, Account.GetAccount(User.Identity.GetUserName()).UserId));

                _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPIV00103SI '{0}','{1}','{2:yyyy-MM-ddThh:mm:ss}','{3}','{4}','{5}','{6}'",
                    Helpers.InterCompanyId, secuence, DateTime.Now, Account.GetAccount(User.Identity.GetUserName()).UserId, "", 1, Account.GetAccount(User.Identity.GetUserName()).UserId));

                _repository.ExecuteCommand(String.Format("LODYNDEV.dbo.LPIV00101P1 '{0}','{1}','{2:yyyy-MM-ddThh:mm:ss}','{3}','{4}'",
                    Helpers.InterCompanyId, secuence, DateTime.Now, 1,Account.GetAccount(User.Identity.GetUserName()).UserId));

                ProcessLogic.SendToSharepointAsync(secuence, 3, Account.GetAccount(User.Identity.GetUserName()).Email);
                //ProcessLogic.SendToSharepoint(secuence, 3, Account.GetAccount(User.Identity.GetUserName()).Email, ref xStatus);
                HelperLogic.DesbloqueoSecuencia(secuence, "LPIV00101", Account.GetAccount(User.Identity.GetUserName()).UserId);
                
            }
            catch(Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult SaveFood(Food food)
        {
            bool xStatus;
            try
            {
                _repository.Add(food);
                xStatus = true;
            }
            catch
            {
                xStatus = false;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [Authorize]
        public ActionResult PurchaseOrderStatus()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Home", "PurchaseOrderStatus"))
            {
                return RedirectToAction("NotPermission", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult ApprovePurchaseOrder(string purchaseOrder, string note)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.POP10100 SET Workflow_Status = 6 WHERE PONUMBER = '{purchaseOrder.Trim()}'");
                _repository.ExecuteCommand($"LODYNDEV.dbo.LPWF00201SI '{Helpers.InterCompanyId}','{purchaseOrder}','{Account.GetAccount(User.Identity.GetUserName()).UserId}','{note}','{4}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        public JsonResult ConsultaRNC(string RNC)
        {
            string xStatus = "";
            var xContribuyente = new Contribuyente();
            try
            {
                var service = new ServiceContract();
                xContribuyente = service.GetContribuyente(PatronBusqueda.RNC, RNC, ref xStatus);
                if (xContribuyente == null)
                    xStatus = "El RNC o Cedula que digito no es valida o no se encuentra el contribuyente.";
                else
                    xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return new JsonResult { Data = new { status = xStatus, contribuyente = xContribuyente, rnc = RNC }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public JsonResult SendMailHelp(string from, string subject, string message)
        {
            string xStatus;
            try
            {
                var mail = new MailMessage(from, Helpers.HelpdeskMail, subject, message);
                var client = new SmtpClient
                {
                    Host = Helpers.MailServer,
                    Port = Convert.ToInt32(Helpers.MailPort),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };
                client.Send(mail);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return new JsonResult { Data = new { status = xStatus } };
        }

        public List<Lookup> GetFiles(string targetDirectory)
        {
            var fileEntries = Directory.GetFiles(targetDirectory);

            return fileEntries.Select(item => new Lookup
            {
                Id = item,
                Descripción = item.Split('\\')[item.Split('\\').Count() - 1].Split('.')[0].Length > 100
                ? item.Split('\\')[item.Split('\\').Count() - 1].Split('.')[0].Substring(0, 100)
                : item.Split('\\')[item.Split('\\').Count() - 1].Split('.')[0],
                DataExtended = item.Split('\\')[item.Split('\\').Count() - 1]
            }).ToList();
        }

        public List<Lookup> GetImagesFiles()
        {
            var fileEntries = Directory.GetFiles(Server.MapPath("~/Pictures/"));

            return fileEntries.Select(item => new Lookup
            {
                Id = "/Intranet/Pictures/" + item.Split('\\')[item.Split('\\').Count() - 1].ToString(),
                Descripción = item.Split('\\')[item.Split('\\').Count() - 1].ToString().Split('.')[0],
                DataExtended = ""
            }).ToList();
        }

        #endregion
    }
}