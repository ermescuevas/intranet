using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Data.Repository;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class HumanResourcesController : Controller
    {
        private readonly GenericRepository _repository;

        public HumanResourcesController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        #region Configuracion de Ausencias

        public ActionResult AbsenceConfiguration()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "HumanResources", "AbsenceConfiguration"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = "SELECT RowId, [Description], FromYear, ToYear, UnitDays, AbsenceType, Gender, ClassId, Fractions, MinumumDays MinimunDays, IncludeWeekends, IncludeHolidays " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFUPR40100 ";
            var list = _repository.ExecuteQuery<AbsenceRule>(sqlQuery).ToList();
            return View(list);
        }

        public JsonResult GetAbsenceRule(int rowId)
        {
            var status = "";
            AbsenceRule absenceRule = null;
            try
            {
                var sqlQuery = "SELECT RowId, [Description], FromYear, ToYear, UnitDays, AbsenceType, Gender, ClassId, Fractions, MinumumDays MinimunDays, IncludeWeekends, IncludeHolidays " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFUPR40100 WHERE RowId = {rowId}";
                absenceRule = _repository.ExecuteScalarQuery<AbsenceRule>(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, model = absenceRule } };
        }

        [HttpPost]
        public JsonResult SaveAbsenceRule(AbsenceRule rule)
        {
            var status = "";
            string sqlQuery;
            try
            {
                if (rule != null)
                {
                    if (rule.RowId == 0)
                        sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR40100 (Description, FromYear, ToYear, UnitDays, AbsenceType, Gender, ClassId, " +
                            $"Fractions, MinumumDays, IncludeWeekends, IncludeHolidays, LastUserId) " +
                            $"VALUES ('{rule.Description}','{rule.FromYear}','{rule.ToYear}','{rule.UnitDays}','{(int)rule.AbsenceType}','{rule.Gender}','{rule.ClassId}','{rule.Fractions}', " +
                            $"'{rule.MinimunDays}','{rule.IncludeWeekends}', '{rule.IncludeHolidays}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}')";
                    else
                        sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFUPR40100 SET Description = '{rule.Description}', FromYear = '{rule.FromYear}', ToYear = '{rule.ToYear}', UnitDays = '{rule.UnitDays}', " +
                            $"AbsenceType = '{(int)rule.AbsenceType}', Gender = '{rule.Gender}', ClassId = '{rule.ClassId}', Fractions = '{rule.Fractions}', MinumumDays = '{rule.MinimunDays}', " +
                            $"IncludeWeekends = '{rule.IncludeWeekends}', IncludeHolidays = '{rule.IncludeHolidays}', LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                            $"WHERE RowId = {rule.RowId}";
                    _repository.ExecuteCommand(sqlQuery);
                }
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult DeleteAbsenceRule(int rowId)
        {
            var status = "";

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFUPR40100 WHERE RowId = {rowId}";
                _repository.ExecuteCommand(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        #endregion

        #region Dias feriados

        public ActionResult HolidayConfiguration()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "HumanResources", "HolidayConfiguration"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = $"SELECT RowId, [Description], HolidayDate, YEAR(HolidayDate) HolidayYear FROM {Helpers.InterCompanyId}.dbo.EFUPR40110 " +
                $"WHERE YEAR(HolidayDate) BETWEEN YEAR(GETDATE()) AND YEAR(DATEADD(YEAR, 1, GETDATE())) " +
                $"ORDER BY YEAR(HolidayDate) ";
            var list = _repository.ExecuteQuery<Holiday>(sqlQuery).ToList();
            return View(list);
        }

        public JsonResult GetHoliday(int rowId)
        {
            var status = "";
            Holiday holiday = null;
            try
            {
                var sqlQuery = $"SELECT RowId, [Description], HolidayDate FROM {Helpers.InterCompanyId}.dbo.EFUPR40110 WHERE RowId = {rowId}";
                holiday = _repository.ExecuteScalarQuery<Holiday>(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, model = holiday } };
        }

        [HttpPost]
        public JsonResult SaveHoliday(Holiday holiday)
        {
            var status = "";
            string sqlQuery;
            try
            {
                if (holiday != null)
                {
                    if (holiday.RowId == 0)
                        sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR40110 (Description, HolidayDate, LastUserId) " +
                            $"VALUES ('{holiday.Description}','{holiday.HolidayDate.ToString("yyyyMMdd")}','{Account.GetAccount(User.Identity.GetUserName()).UserId}')";
                    else
                        sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFUPR40110 SET Description = '{holiday.Description}', HolidayDate = '{holiday.HolidayDate.ToString("yyyyMMdd")}', " +
                            $"ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                            $"WHERE RowId = {holiday.RowId}";
                    _repository.ExecuteCommand(sqlQuery);
                }
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult DeleteHoliday(int rowId)
        {
            var status = "";

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFUPR40110 WHERE RowId = {rowId}";
                _repository.ExecuteCommand(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        #endregion

        #region Solicitud de Ausencias

        public ActionResult AbsenceRequest()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "HumanResources", "AbsenceRequest"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = "SELECT A.EMPLOYID Id, RTRIM(A.FRSTNAME) + ' ' + RTRIM(A.LASTNAME) Descripción "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.UPR00100 A "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40300 B "
                            + "ON A.DEPRTMNT = B.DEPRTMNT "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40301 C "
                            + "ON A.JOBTITLE = C.JOBTITLE "
                            + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY01200 D "
                            + "ON A.EMPLOYID = D.Master_ID AND D.Master_Type = 'EMP' "
                            + "WHERE (INACTIVE = 0 OR UPPER(USERDEF2) = 'SI') "
                            + "ORDER BY A.EMPLOYID ";
            ViewBag.Employees = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();

            sqlQuery = "SELECT A.RequestId, B.EMPLOYID EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartDate, A.EndDate, A.UnitDays, A.Note, " +
                "CASE A.AbsenceType WHEN 1 THEN 'Vacaciones' WHEN 2 THEN 'Permiso' WHEN 3 THEN 'Duelo' WHEN 4 THEN 'Paternidad' " +
                "WHEN 5 THEN 'Maternidad' WHEN 6 THEN 'Matrimonio' WHEN 7 THEN 'Cumpleaños' WHEN 8 THEN 'Licencia Medica' " +
                "WHEN 9 THEN 'Cita Medica' ELSE 'Vacaciones' END AbsenceType, B.DEPRTMNT DepartmentId, A.Status, A.RowId " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30100 A INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                $"WHERE A.LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                $"ORDER BY A.StartDate DESC";
            var list = _repository.ExecuteQuery<AbsenceRequest>(sqlQuery).ToList();

            return View(list);
        }

        public JsonResult AbsenceRule(AbsenceType absenceType, string employeeId)
        {
            int aYears = 0, aDays = 0, aFractions = 0, aDaysTaken = 0, aFractionsTaken = 0, aMinimunDays = 0;
            string aEmployeeName = "";
            string xStatus;
            try
            {
                var gender = _repository.ExecuteScalarQuery<int>($"SELECT CONVERT(INT, GENDER) FROM {Helpers.InterCompanyId}.dbo.UPR00100 WHERE EMPLOYID = '{employeeId}'");
                string aGender = "3," + gender.ToString();
                var classId = _repository.ExecuteScalarQuery<string>($"SELECT RTRIM(EMPLCLAS) FROM {Helpers.InterCompanyId}.dbo.UPR00100 WHERE EMPLOYID = '{employeeId}'");
                var foreign = _repository.ExecuteScalarQuery<string>($"SELECT RTRIM(NICKNAME) FROM {Helpers.InterCompanyId}.dbo.UPR00100 WHERE EMPLOYID = '{employeeId}'");
                int aClassId;
                if (classId == "TCC")
                    aClassId = 1;
                else
                {
                    if (!foreign.ToLower().StartsWith("domin"))
                        aClassId = 3;
                    else
                        aClassId = 2;
                }

                aYears = _repository.ExecuteScalarQuery<int>($"SELECT CASE WHEN DATEADD(YEAR,DATEDIFF(YEAR, STRTDATE, GETDATE()),STRTDATE) > GETDATE() " +
                    $"THEN DATEDIFF(YEAR, STRTDATE, GETDATE()) - 1 ELSE DATEDIFF(YEAR, STRTDATE, GETDATE()) END  FROM {Helpers.InterCompanyId}.dbo.UPR00100 WHERE EMPLOYID = '{employeeId}'");
                if (absenceType == AbsenceType.Cita_Medica)
                    absenceType = AbsenceType.Permiso;
                aDays = _repository.ExecuteScalarQuery<int>($"SELECT UnitDays FROM {Helpers.InterCompanyId}.dbo.EFUPR40100 WHERE ClassId = {aClassId} AND Gender IN ({aGender}) AND {aYears} BETWEEN FromYear AND ToYear AND AbsenceType = {(int)absenceType}");
                aFractions = _repository.ExecuteScalarQuery<int>($"SELECT Fractions FROM {Helpers.InterCompanyId}.dbo.EFUPR40100 WHERE ClassId = {aClassId} AND Gender IN ({aGender}) AND {aYears} BETWEEN FromYear AND ToYear AND AbsenceType = {(int)absenceType}");
                aMinimunDays = _repository.ExecuteScalarQuery<int>($"SELECT MinumumDays FROM {Helpers.InterCompanyId}.dbo.EFUPR40100 WHERE ClassId = {aClassId} AND Gender IN ({aGender}) AND {aYears} BETWEEN FromYear AND ToYear AND AbsenceType = {(int)absenceType}");
                if (absenceType == AbsenceType.Vacaciones)
                {
                    var fromDate = _repository.ExecuteScalarQuery<DateTime>("SELECT CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()), STRTDATE) > GETDATE() " +
                        "THEN DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()) - 1, STRTDATE) ELSE DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()), STRTDATE) END " +
                        $"FROM {Helpers.InterCompanyId}.dbo.UPR00100 WHERE EMPLOYID = '{employeeId}'");
                    var toDate = _repository.ExecuteScalarQuery<DateTime>("SELECT CASE WHEN DATEADD(YEAR,DATEDIFF(YEAR, STRTDATE, GETDATE()),STRTDATE) > GETDATE() " +
                        "THEN DATEADD(YEAR,DATEDIFF(YEAR, STRTDATE, GETDATE()),STRTDATE) ELSE DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()) + 1, STRTDATE) END " +
                        $"FROM {Helpers.InterCompanyId}.dbo.UPR00100 WHERE EMPLOYID = '{employeeId}'");
                    aDaysTaken = _repository.ExecuteScalarQuery<int>($"SELECT ISNULL(SUM(UnitDays), 0) FROM {Helpers.InterCompanyId}.dbo.EFUPR30100 " +
                        $"WHERE EmployeeId = '{employeeId}' AND AbsenceType = {(int)absenceType} AND StartDate BETWEEN '{fromDate.ToString("yyyyMMdd")}' AND '{toDate.ToString("yyyyMMdd")}' AND Status <> 2");
                }
                else if (absenceType == AbsenceType.Permiso || absenceType == AbsenceType.Cita_Medica)
                    aDaysTaken = _repository.ExecuteScalarQuery<int>($"SELECT ISNULL(SUM(UnitDays), 0) FROM {Helpers.InterCompanyId}.dbo.EFUPR30100 WHERE EmployeeId = '{employeeId}' AND AbsenceType IN ({(int)AbsenceType.Cita_Medica}, {(int)AbsenceType.Permiso}) AND [Year] = YEAR(GETDATE()) AND [Month] = MONTH(GETDATE()) AND Status <> 2");
                else
                    aDaysTaken = _repository.ExecuteScalarQuery<int>($"SELECT ISNULL(SUM(UnitDays), 0) FROM {Helpers.InterCompanyId}.dbo.EFUPR30100 WHERE EmployeeId = '{employeeId}' AND AbsenceType = {(int)absenceType} AND [Year] = YEAR(GETDATE())  AND Status <> 2");
                aFractionsTaken = _repository.ExecuteScalarQuery<int>($"SELECT ISNULL(COUNT(UnitDays), 0) FROM {Helpers.InterCompanyId}.dbo.EFUPR30100 WHERE EmployeeId = '{employeeId}' AND AbsenceType = {(int)absenceType} AND [Year] = YEAR(GETDATE())  AND Status <> 2");
                aEmployeeName = _repository.ExecuteScalarQuery<string>($"SELECT RTRIM(FRSTNAME) + ' ' + RTRIM(LASTNAME) FROM {Helpers.InterCompanyId}.dbo.UPR00100 WHERE EMPLOYID = '{employeeId}'");

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new
            {
                status = xStatus,
                years = aYears,
                days = aDays,
                fractions = aFractions,
                daysTaken = aDaysTaken,
                fractionsTaken = aFractionsTaken,
                employeeName = aEmployeeName,
                minimunDays = aMinimunDays
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult AbsenceInfo(string fromDate, string employeeId, AbsenceType absenceType, int days)
        {
            DateTime aEndDate = DateTime.Now;
            int aDays = days;
            string aFromDate = DateTime.ParseExact(fromDate, "MM/dd/yyyy", null).ToString("dd/MM/yyyy");
            string xStatus;
            try
            {
                var gender = _repository.ExecuteScalarQuery<int>($"SELECT CONVERT(INT, GENDER) FROM {Helpers.InterCompanyId}.dbo.UPR00100 WHERE EMPLOYID = '{employeeId}'");
                string aGender = "3," + gender.ToString();
                var classId = _repository.ExecuteScalarQuery<string>($"SELECT RTRIM(EMPLCLAS) FROM {Helpers.InterCompanyId}.dbo.UPR00100 WHERE EMPLOYID = '{employeeId}'");
                var foreign = _repository.ExecuteScalarQuery<string>($"SELECT RTRIM(NICKNAME) FROM {Helpers.InterCompanyId}.dbo.UPR00100 WHERE EMPLOYID = '{employeeId}'");
                int aClassId;
                if (!foreign.ToLower().StartsWith("domin"))
                    aClassId = 3;
                else if (classId == "TCC")
                    aClassId = 1;
                else
                    aClassId = 2;
                var sqlQuery = $"INTRANET.dbo.CalculateAbsenceDays '{Helpers.InterCompanyId}', '{employeeId}', '{aGender}', '{aClassId}', '{DateTime.ParseExact(fromDate, "MM/dd/yyyy", null).ToString("yyyyMMdd")}', '{days}', '{(int)absenceType}'";
                aEndDate = _repository.ExecuteScalarQuery<DateTime?>(sqlQuery) ?? DateTime.MinValue;
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return Json(new
            {
                status = xStatus,
                endDate = aEndDate.ToString("dd/MM/yyyy"),
                days = aDays,
                fromDate = aFromDate
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveAbsenceRequest(string employeeId, string startDate, string endDate, AbsenceType absenceType, int unitDays, string note, int availableDays, int rowId)
        {
            var newRowId = 0;
            var secuence = "";
            string status;
            try
            {
                status = "OK";
                var aStartDate = DateTime.ParseExact(startDate, "MM/dd/yyyy", null);
                var aEndDate = DateTime.ParseExact(endDate, "dd/MM/yyyy", null);
                secuence = HelperLogic.AsignaciónSecuencia("EFUPR30100", Account.GetAccount(User.Identity.GetUserName()).UserId);
                var sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR30100 ([RequestId],[EmployeeId],[Year],[Month],[StartDate],[EndDate],[AbsenceType],[UnitDays],[AvailableDays],[Note],[Status],[LastUserId]) " +
                    $"VALUES ('{secuence}','{employeeId}','{aStartDate.Year}','{aStartDate.Month}','{aStartDate.ToString("yyyyMMdd")}','{aEndDate.ToString("yyyyMMdd")}','{(int)absenceType}', " +
                    $"'{unitDays}','{availableDays}','{note}', 0, '{Account.GetAccount(User.Identity.GetUserName()).UserId}') ";
                _repository.ExecuteCommand(sqlQuery);
                newRowId = _repository.ExecuteScalarQuery<int>($"SELECT RowId FROM {Helpers.InterCompanyId}.dbo.EFUPR30100 ORDER BY RowId DESC");
                ProcessLogic.SendToSharepoint(newRowId.ToString(), 5, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
                
            }
            catch (Exception ex)
            {
                status = ex.Message;
                if (newRowId != 0)
                {
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFUPR30100 WHERE RowId = '{newRowId}'");
                    HelperLogic.DesbloqueoSecuencia(secuence, "EFUPR30100", Account.GetAccount(User.Identity.GetUserName()).UserId);
                }
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult DeleteAbsenceRequest(int rowId)
        {
            string status;
            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFUPR30100 WHERE RowId = {rowId}";
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
        public JsonResult SendAbsenceRequest(int rowId)
        {
            string status;
            try
            {
                status = "OK";
                ProcessLogic.SendToSharepoint(rowId.ToString(), 5, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult AbsenceReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "HumanResources", "AbsenceReport"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }
        
        [HttpPost]
        public ActionResult AbsenceReport(int year = 0)
        {
            string status;
            List<AbsenceInquiry> list = null;
            try
            {
                string sqlQuery = "SELECT A.RowId, A.EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, StartDate, EndDate, UnitDays DaysTaken, Note Comment, " +
                    $"RTRIM(C.FirstName) + ' ' + RTRIM(C.LastName) Requester, " +
                    "CASE A.AbsenceType WHEN 1 THEN 'Vacaciones' WHEN 2 THEN 'Permiso' WHEN 3 THEN 'Duelo' WHEN 4 THEN 'Paternidad' " +
                    "WHEN 5 THEN 'Maternidad' WHEN 6 THEN 'Matrimonio' WHEN 7 THEN 'Cumpleaños' WHEN 8 THEN 'Licencia Medica' " +
                    "WHEN 9 THEN 'Cita Medica' ELSE 'Vacaciones' END AbsenceType " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30100 A " +
                    $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                    $"INNER JOIN INTRANET..USERS C ON A.LastUserId = C.UserId " +
                    $"WHERE Year = {year}";
                list = _repository.ExecuteQuery<AbsenceInquiry>(sqlQuery).ToList();
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, registros = list } };
        }

        public ActionResult VacationPendingReport()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "HumanResources", "VacationPendingReport"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [HttpPost]
        public ActionResult VacationPendingReport(int year = 0)
        {
            string status;
            List<AbsenceInquiry> list = null;
            try
            {
                string sqlQuery = "SELECT DISTINCT A.EMPLOYID EmployeeId, RTRIM(A.FRSTNAME) + ' ' + RTRIM(A.LASTNAME) EmployeeName, A.DEPRTMNT Department, A.STRTDATE StartDate, " +
                $"YEAR(CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, A.STRTDATE, GETDATE()), A.STRTDATE) > GETDATE() " +
                $"THEN DATEADD(YEAR, DATEDIFF(YEAR, A.STRTDATE, GETDATE()) - 1, STRTDATE) ELSE DATEADD(YEAR, DATEDIFF(YEAR, A.STRTDATE, GETDATE()), STRTDATE) END) WorkYear, " +
                $"(SELECT TOP 1 ISNULL(SUM(UnitDays), 0) FROM {Helpers.InterCompanyId}.dbo.EFUPR30100 AA WHERE AA.EmployeeId = A.EMPLOYID AND AA.StartDate BETWEEN " +
                $"(SELECT TOP 1 CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()), STRTDATE) > GETDATE() " +
                $"THEN DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()) -1, STRTDATE) ELSE DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()), STRTDATE) END " +
                $"FROM {Helpers.InterCompanyId}.dbo.UPR00100 BB WHERE BB.EMPLOYID = AA.EmployeeId) " +
                $"AND(SELECT TOP 1 CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()), STRTDATE) > GETDATE() " +
                $"THEN DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()), STRTDATE) ELSE DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()) + 1, STRTDATE) END " +
                $"FROM {Helpers.InterCompanyId}.dbo.UPR00100 BB WHERE BB.EMPLOYID = AA.EmployeeId) " +
                $"AND AA.AbsenceType = 1 AND AA.[Year] = YEAR(GETDATE()) AND AA.[Status] <> 2) DaysTaken, " +
                $"ISNULL(CASE (SELECT TOP 1 RTRIM(EMPLCLAS) FROM {Helpers.InterCompanyId}.dbo.UPR00100 BB WHERE BB.EMPLOYID = A.EMPLOYID) " +
                $"WHEN 'TCC' THEN (SELECT TOP 1 UnitDays FROM  {Helpers.InterCompanyId}.dbo.EFUPR40100 BB WHERE BB.ClassId = 1 AND Gender IN (1, 2, 3) AND " +
                $"(SELECT TOP 1 CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()),STRTDATE) > GETDATE() " +
                $"THEN DATEDIFF(YEAR, STRTDATE, GETDATE()) - 1 ELSE DATEDIFF(YEAR, STRTDATE, GETDATE()) END FROM {Helpers.InterCompanyId}.dbo.UPR00100 " +
                $"WHERE EMPLOYID = A.EMPLOYID) BETWEEN BB.FromYear AND BB.ToYear AND BB.AbsenceType = 1) ELSE " +
                $"CASE(SELECT TOP 1 LOWER(LEFT(RTRIM(NICKNAME), 5)) FROM {Helpers.InterCompanyId}.dbo.UPR00100 BB WHERE BB.EMPLOYID = A.EMPLOYID) " +
                $"WHEN 'domin' THEN (SELECT TOP 1 UnitDays FROM  {Helpers.InterCompanyId}.dbo.EFUPR40100 BB WHERE BB.ClassId = 2 AND Gender IN (1, 2, 3) AND " +
                $"(SELECT TOP 1 CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()),STRTDATE) > GETDATE() " +
                $"THEN DATEDIFF(YEAR, STRTDATE, GETDATE()) - 1 ELSE DATEDIFF(YEAR, STRTDATE, GETDATE()) END FROM {Helpers.InterCompanyId}.dbo.UPR00100 " +
                $"WHERE EMPLOYID = A.EMPLOYID) BETWEEN BB.FromYear AND BB.ToYear AND BB.AbsenceType = 1) " +
                $"ELSE (SELECT TOP 1 UnitDays FROM  {Helpers.InterCompanyId}.dbo.EFUPR40100 BB WHERE BB.ClassId = 3 AND Gender IN (1, 2, 3) " +
                $"AND(SELECT TOP 1 CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, STRTDATE, GETDATE()),STRTDATE) > GETDATE() " +
                $"THEN DATEDIFF(YEAR, STRTDATE, GETDATE()) - 1 ELSE DATEDIFF(YEAR, STRTDATE, GETDATE()) END FROM {Helpers.InterCompanyId}.dbo.UPR00100 " +
                $"WHERE EMPLOYID = A.EMPLOYID) BETWEEN BB.FromYear AND BB.ToYear AND BB.AbsenceType = 1) END END, 0) AssignedDays " +
                $"FROM {Helpers.InterCompanyId}.dbo.UPR00100 A WHERE(INACTIVE = 0 OR UPPER(USERDEF2) = 'SI') " +
                $"ORDER BY A.EMPLOYID";
                list = _repository.ExecuteQuery<AbsenceInquiry>(sqlQuery).ToList();
                list.ForEach(p =>
                {
                    p.LeftDays = p.AssignedDays - p.DaysTaken;
                });
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, registros = list } };
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult PrintRequest(int id)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "AbsenceRequestReport.pdf",
                    string.Format("INTRANET.dbo.AbsenceRequestReport '{0}','{1}'", Helpers.InterCompanyId, id), 35, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        #endregion

        #region Solicitud de Capacitacion

        public ActionResult TrainingRequest()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "HumanResources", "TrainingRequest"))
                return RedirectToAction("NotPermission", "Home");

            var sqlQuery = "SELECT A.EMPLOYID Id, RTRIM(A.FRSTNAME) + ' ' + RTRIM(A.LASTNAME) Descripción "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.UPR00100 A "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40300 B "
                            + "ON A.DEPRTMNT = B.DEPRTMNT "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40301 C "
                            + "ON A.JOBTITLE = C.JOBTITLE "
                            + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY01200 D "
                            + "ON A.EMPLOYID = D.Master_ID AND D.Master_Type = 'EMP' "
                            + "WHERE (INACTIVE = 0 OR UPPER(USERDEF2) = 'SI') "
                            + "ORDER BY A.EMPLOYID ";
            ViewBag.Employees = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();

            sqlQuery = "SELECT RTRIM(SGMNTID) [Id], RTRIM(DSCRIPTN) [Descripción] FROM " +
                Helpers.InterCompanyId + ".dbo.GL40200 WHERE SGMTNUMB = 2 AND LEN(RTRIM(DSCRIPTN)) > 0 ";
            ViewBag.Departments = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();

            sqlQuery = "SELECT A.RequestId, A.EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.Description, A.StartDate, A.Duration, C.DEPRTMDS Department, A.Cost, " +
                "A.CurrencyId, A.Location, A.Supplier, A.Objectives, A.Requirements, A.Participants, A.Department DepartmentId, A.Status, A.RowId " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30400 A INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                $"LEFT JOIN {Helpers.InterCompanyId}.dbo.LPPOP40100 C ON A.Department = C.DEPRTMID ";
            if (Account.GetAccount(User.Identity.GetUserName()).Department != "RECURSOS HUMANOS")
                sqlQuery += $"WHERE A.LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' ";
            var list = _repository.ExecuteQuery<TrainingRequest>(sqlQuery).ToList();

            return View(list);
        }

        public JsonResult GetTrainingRequest(int rowId)
        {
            TrainingRequest trainingRequest = null;
            string status;
            try
            {
                var sqlQuery = "SELECT A.Description, A.EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartDate, A.Duration, A.Department, A.Cost, " +
                "A.CurrencyId, A.Location, A.Supplier, A.Objectives, A.Requirements, A.Participants, B.DEPRTMNT DepartmentId, A.IsCompleted, A.Status, A.RowId " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30400 A INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                $"WHERE A.RowId = '{rowId}' ";
                trainingRequest = _repository.ExecuteScalarQuery<TrainingRequest>(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, model = trainingRequest } };
        }

        [HttpPost]
        public JsonResult SaveTrainingRequest(TrainingRequest request, bool send = true)
        {
            var newRowId = 0;
            var secuence = "";
            string status;
            try
            {
                DateTime startDate;
                if (request.StartDate == DateTime.MinValue)
                    startDate = new DateTime(1900, 1, 1);
                else
                    startDate = request.StartDate;
                string sqlQuery;

                if (request.RowId == 0)
                {
                    secuence = HelperLogic.AsignaciónSecuencia("EFUPR30400", Account.GetAccount(User.Identity.GetUserName()).UserId);
                    sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR30400 ([RequestId],[Description],[EmployeeId],[StartDate],[Duration],[Department],[Cost]," +
                        $"[CurrencyId],[Location],[Supplier],[Objectives],[Requirements],[Participants],[IsCompleted],[Status],[LastUserId]) " +
                        $"VALUES ('{secuence}','{request.Description}','{request.EmployeeId}','{startDate.ToString("yyyyMMdd")}','{request.Duration}','{request.Department}','{request.Cost}', " +
                        $"'{request.CurrencyId}','{request.Location}','{request.Supplier}', '{request.Objectives}', '{request.Requirements}', " +
                        $"'{request.Participants}', '{request.IsCompleted}', 1, '{Account.GetAccount(User.Identity.GetUserName()).UserId}') ";
                }
                else
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFUPR30400 SET [Description] = '{request.Description}', [EmployeeId] = '{request.EmployeeId}', [StartDate] = '{startDate.ToString("yyyyMMdd")}', " +
                        $"[Duration] = '{request.Duration}', [Department] = '{request.Department}', [Cost] = '{request.Cost}', [CurrencyId] = '{request.CurrencyId}', [Location] = '{request.Location}', " +
                        $"[Supplier] = '{request.Supplier}', [Objectives] = '{request.Objectives}', [Requirements] = '{request.Requirements}', [Participants] = '{request.Participants}', " +
                        $"[IsCompleted] = '{request.IsCompleted}', [Status] = 1, [LastUserId] = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                        $"WHERE RowId = {request.RowId}";
                _repository.ExecuteCommand(sqlQuery);
                
                newRowId = request.RowId == 0 ? _repository.ExecuteScalarQuery<int>($"SELECT RowId FROM {Helpers.InterCompanyId}.dbo.EFUPR30400 ORDER BY RowId DESC") : request.RowId;
                status = "OK";
                if (send)
                    ProcessLogic.SendToSharepoint(newRowId.ToString(), 6, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
            }
            catch (Exception ex)
            {
                status = ex.Message;
                if (newRowId != 0 && request.RowId == 0)
                {
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFUPR30400 WHERE RowId = '{newRowId}'");
                    HelperLogic.DesbloqueoSecuencia(secuence, "EFUPR30400", Account.GetAccount(User.Identity.GetUserName()).UserId);
                }
            }

            return new JsonResult { Data = new { status, rowId = newRowId } };
        }

        [HttpPost]
        public JsonResult DeleteTrainingRequest(int rowId)
        {
            string status;
            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFUPR30400 WHERE RowId = {rowId}";
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
        public ActionResult PrintTrainingRequest(string requestId)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "TrainingRequestReport.pdf",
                    string.Format("INTRANET.dbo.TrainingRequestReport '{0}','{1}'", Helpers.InterCompanyId, requestId), 41, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        #endregion

        #region Solicitud de Creacion de Usuario

        public ActionResult UserRequest()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "HumanResources", "UserRequest"))
                return RedirectToAction("NotPermission", "Home");

            var sqlQuery = "SELECT A.EMPLOYID Id, RTRIM(A.FRSTNAME) + ' ' + RTRIM(A.LASTNAME) Descripción "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.UPR00100 A "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40300 B "
                            + "ON A.DEPRTMNT = B.DEPRTMNT "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40301 C "
                            + "ON A.JOBTITLE = C.JOBTITLE "
                            + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY01200 D "
                            + "ON A.EMPLOYID = D.Master_ID AND D.Master_Type = 'EMP' "
                            + "WHERE (INACTIVE = 0 OR UPPER(USERDEF2) = 'SI') "
                            + "ORDER BY A.EMPLOYID ";
            ViewBag.Employees = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();

            sqlQuery = "SELECT RTRIM(SGMNTID) [Id], RTRIM(DSCRIPTN) [Descripción] FROM " +
                Helpers.InterCompanyId + ".dbo.GL40200 WHERE SGMTNUMB = 2 AND LEN(RTRIM(DSCRIPTN)) > 0 ";
            ViewBag.Departments = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();

            sqlQuery = "SELECT A.RequestId, A.EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartTime, A.EndTime, A.DaysWork, A.Resources, " +
                "C.DEPRTMDS Department, CONVERT(NVARCHAR(20), A.EmailAccount) EmailAccount, CONVERT(NVARCHAR(20), A.InternetAccess) InternetAccess, A.Department, A.Comments, A.IsPolicy, A.Status, A.RowId " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30500 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                $"LEFT JOIN {Helpers.InterCompanyId}.dbo.LPPOP40100 C ON A.Department = C.DEPRTMID ";

            if (Account.GetAccount(User.Identity.GetUserName()).Department != "INFORMATICA")
                sqlQuery += $"WHERE A.LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' ";

            var list = _repository.ExecuteQuery<UserRequest>(sqlQuery).ToList();
            list.ForEach(p =>
            {
                var days = p.DaysWork.Split(',');
                var dayDescription = "";
                p.DaysWork = "";
                foreach (var item in days)
                {
                    switch (item)
                    {
                        case "1":
                            dayDescription = "Domingo";
                            break;
                        case "2":
                            dayDescription = "Lunes";
                            break;
                        case "3":
                            dayDescription = "Martes";
                            break;
                        case "4":
                            dayDescription = "Miercoles";
                            break;
                        case "5":
                            dayDescription = "Jueves";
                            break;
                        case "6":
                            dayDescription = "Viernes";
                            break;
                        case "7":
                            dayDescription = "Sabado";
                            break;
                        default:
                            dayDescription = "Domingo";
                            break;
                    }
                    p.DaysWork += dayDescription + ",";
                }
            });

            return View(list);
        }

        public JsonResult GetUserRequest(int rowId)
        {
            var status = "";
            UserRequest userRequest = null;
            try
            {
                var sqlQuery = "SELECT A.EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartTime, A.EndTime, A.DaysWork, A.Resources, " +
                "A.Department, CONVERT(NVARCHAR(20), A.EmailAccount) EmailAccount, CONVERT(NVARCHAR(20), A.InternetAccess) InternetAccess, A.Comments, A.IsPolicy, A.Status, A.RowId " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30500 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                $"LEFT JOIN {Helpers.InterCompanyId}.dbo.LPPOP40100 C ON A.Department = C.DEPRTMID " +
                $"WHERE A.RowId = '{rowId}' ";
                userRequest = _repository.ExecuteScalarQuery<UserRequest>(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, model = userRequest } };
        }

        [HttpPost]
        public JsonResult SaveUserRequest(UserRequest request)
        {
            var status = "";
            var newRowId = 0;
            var secuence = "";
            try
            {
                string sqlQuery;

                if (request.RowId == 0)
                {
                    secuence = HelperLogic.AsignaciónSecuencia("EFUPR30500", Account.GetAccount(User.Identity.GetUserName()).UserId);
                    sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR30500 ([RequestId],[EmployeeId],[StartTime],[EndTime],[DaysWork],[Resources]," +
                        $"[EmailAccount],[InternetAccess],[Department],[Comments],[IsPolicy],[Status],[LastUserId]) " +
                        $"VALUES ('{secuence}','{request.EmployeeId}','{request.StartTime}','{request.EndTime}','{request.DaysWork}','{request.Resources}','{request.EmailAccount}', " +
                        $"'{request.InternetAccess}','{request.Department}','{request.Comments}', '{request.IsPolicy}', 1, '{Account.GetAccount(User.Identity.GetUserName()).UserId}') ";
                }
                else
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFUPR30500 SET [EmployeeId] = '{request.EmployeeId}', [StartTime] = '{request.StartTime}', " +
                        $"[EndTime] = '{request.EndTime}', [DaysWork] = '{request.DaysWork}', [Resources] = '{request.Resources}', [EmailAccount] = '{request.EmailAccount}', " +
                        $"[InternetAccess] = '{request.InternetAccess}', [Department] = '{request.Department}', [Comments] = '{request.Comments}', " +
                        $"[IsPolicy] = '{request.IsPolicy}', [Status] = 1, [LastUserId] = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                        $"WHERE RowId = {request.RowId}";
                _repository.ExecuteCommand(sqlQuery);
                newRowId = request.RowId == 0 ? _repository.ExecuteScalarQuery<int>($"SELECT RowId FROM {Helpers.InterCompanyId}.dbo.EFUPR30500 ORDER BY RowId DESC") : request.RowId;
                status = "OK";
                ProcessLogic.SendToSharepoint(newRowId.ToString(), 7, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
                
            }
            catch (Exception ex)
            {
                status = ex.Message;
                if (newRowId != 0 && request.RowId == 0)
                {
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFUPR30500 WHERE RowId = '{newRowId}'");
                    HelperLogic.DesbloqueoSecuencia(secuence, "EFUPR30500", Account.GetAccount(User.Identity.GetUserName()).UserId);
                }
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult DeleteUserRequest(int rowId)
        {
            var status = "";

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFUPR30500 WHERE RowId = {rowId}";
                _repository.ExecuteCommand(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        #endregion

        #region Horas Extras

        public ActionResult OvertimeRequest()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "HumanResources", "OvertimeRequest"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = "SELECT A.EMPLOYID Id, RTRIM(A.FRSTNAME) + ' ' + RTRIM(A.LASTNAME) Descripción "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.UPR00100 A "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40300 B "
                            + "ON A.DEPRTMNT = B.DEPRTMNT "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40301 C "
                            + "ON A.JOBTITLE = C.JOBTITLE "
                            + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY01200 D "
                            + "ON A.EMPLOYID = D.Master_ID AND D.Master_Type = 'EMP' "
                            + "WHERE (INACTIVE = 0 OR UPPER(USERDEF2) = 'SI') "
                            + "ORDER BY A.EMPLOYID ";
            ViewBag.Employees = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();

            sqlQuery = $"SELECT BatchNumber, [Description], Note, RowId, NumberOfTransactions, A.Approver, A.Status FROM {Helpers.InterCompanyId}.dbo.EFUPR30300 A " +
                $"WHERE A.LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' AND A.Module = 1";
            ViewBag.BatchApprovals = _repository.ExecuteQuery<OvertimeApproval>(sqlQuery).ToList();

            sqlQuery = "SELECT RTRIM(SGMNTID) [Id], RTRIM(DSCRIPTN) [Descripción] FROM " +
               Helpers.InterCompanyId + ".dbo.GL40200 WHERE SGMTNUMB = 2 AND LEN(RTRIM(DSCRIPTN)) > 0 ";
            ViewBag.Departments = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();

            sqlQuery = "SELECT B.EMPLOYID EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartDate, A.EndDate, A.Hours, A.Note, " +
                "CASE A.OvertimeType WHEN 1 THEN '100%' WHEN 2 THEN '35%' WHEN 3 THEN '15%' WHEN 4 THEN 'Feriado' ELSE '100%' END OvertimeTypeDesc, A.OvertimeType, A.Status, A.RowId " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30200 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                $"WHERE A.LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' AND A.Module = 1 AND A.Status <> 4";
            var list = _repository.ExecuteQuery<Overtime>(sqlQuery).ToList();

            return View(list);
        }

        public ActionResult ExtraOvertimeRequest()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "HumanResources", "ExtraOvertimeRequest"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = "SELECT A.EMPLOYID Id, RTRIM(A.FRSTNAME) + ' ' + RTRIM(A.LASTNAME) Descripción "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.UPR00100 A "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40300 B "
                            + "ON A.DEPRTMNT = B.DEPRTMNT "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.UPR40301 C "
                            + "ON A.JOBTITLE = C.JOBTITLE "
                            + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.SY01200 D "
                            + "ON A.EMPLOYID = D.Master_ID AND D.Master_Type = 'EMP' "
                            + "WHERE (INACTIVE = 0 OR UPPER(USERDEF2) = 'SI') "
                            + "ORDER BY A.EMPLOYID ";
            ViewBag.Employees = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();

            sqlQuery = $"SELECT BatchNumber, [Description], Note, RowId, NumberOfTransactions, A.Approver, A.Status FROM {Helpers.InterCompanyId}.dbo.EFUPR30300 A " +
                $"WHERE A.LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' AND A.Module = 2";
            ViewBag.BatchApprovals = _repository.ExecuteQuery<OvertimeApproval>(sqlQuery).ToList();

            sqlQuery = "SELECT RTRIM(SGMNTID) [Id], RTRIM(DSCRIPTN) [Descripción] FROM " +
               Helpers.InterCompanyId + ".dbo.GL40200 WHERE SGMTNUMB = 2 AND LEN(RTRIM(DSCRIPTN)) > 0 ";
            ViewBag.Departments = _repository.ExecuteQuery<Lookup>(sqlQuery).ToList();

            sqlQuery = "SELECT B.EMPLOYID EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartDate, A.EndDate, A.Hours, A.Note, " +
                "CASE A.OvertimeType WHEN 1 THEN '100%' WHEN 2 THEN '35%' WHEN 3 THEN '15%' WHEN 4 THEN 'Feriado' ELSE '100%' END OvertimeTypeDesc, A.OvertimeType, A.Status, A.RowId " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30200 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                $"WHERE A.LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' AND Module = 2 AND A.Status <> 4";
            var list = _repository.ExecuteQuery<Overtime>(sqlQuery).ToList();

            return View(list);
        }

        public ActionResult HumanResourcesOvertime()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "HumanResources", "HumanResourcesOvertime"))
                return RedirectToAction("NotPermission", "Home");

            var sqlQuery = "SELECT BatchNumber, [Description], PayrollDate, Note, NumberOfTransactions, Status, CreatedDate, RowId " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30600 ";
            var list = _repository.ExecuteQuery<HumanResourcesOvertime>(sqlQuery).ToList();
            return View(list);
        }

        public JsonResult GetOvertime(int rowId)
        {
            Overtime overtime = null;
            string status;
            try
            {
                var sqlQuery = "SELECT B.EMPLOYID EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartDate, A.EndDate, A.Hours, A.Note, " +
                "CASE A.OvertimeType WHEN 1 THEN '100%' WHEN 2 THEN '30%' WHEN 3 THEN '15%' WHEN 4 THEN 'Feriado' ELSE '100%' END OvertimeTypeDesc, A.OvertimeType, A.Status, A.RowId " +
                $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30200 A INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                $"WHERE A.RowId = '{rowId}' ";
                overtime = _repository.ExecuteScalarQuery<Overtime>(sqlQuery);
                overtime.StartTime = overtime.StartDate.ToString("HH:mm");
                overtime.EndTime = overtime.EndDate.ToString("HH:mm");
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, model = overtime } };
        }

        [HttpPost]
        public JsonResult SaveOvertime(string employeeId, string startDate, string endDate, int overtimeType, string note, string startTime, string endTime, int rowId, int module, decimal hours = 0)
        {
            string status;
            try
            {
                var aStartDate = DateTime.ParseExact(startDate + " " + startTime, "MM/dd/yyyy HH:mm", null);
                var aEndDate = DateTime.ParseExact(endDate + " " + endTime, "MM/dd/yyyy HH:mm", null);
                if (hours == 0)
                    hours = aEndDate.Subtract(aStartDate).Hours;
                string sqlQuery;
                if (rowId == 0)
                    sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR30200 ([EmployeeId],[StartDate],[EndDate],[OvertimeType],[Hours],[Note],[Status],[Module],[LastUserId]) " +
                   $"VALUES ('{employeeId}','{aStartDate.ToString("yyyyMMdd HH:mm:ss")}','{aEndDate.ToString("yyyyMMdd HH:mm:ss")}','{overtimeType}','{hours}','{note}',0,{module},'{Account.GetAccount(User.Identity.GetUserName()).UserId}') ";
                
                else
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFUPR30200 SET [EmployeeId] = '{employeeId}', [StartDate] = '{aStartDate.ToString("yyyyMMdd HH:mm:ss")}', [EndDate] = '{aEndDate.ToString("yyyyMMdd HH:mm:ss")}', " +
                       $"[OvertimeType] = '{overtimeType}', [Hours] = '{hours}', [Note] = '{note}', [LastUserId] = '{Account.GetAccount(User.Identity.GetUserName()).UserId}', ModifiedDate = GETDATE() " +
                       $"WHERE RowId = '{rowId}'";
                _repository.ExecuteCommand(sqlQuery);

                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status} };
        }

        [HttpPost]
        public JsonResult DeleteOvertime(int rowId)
        {
            var status = "";

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFUPR30200 WHERE RowId = {rowId}";
                _repository.ExecuteCommand(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public JsonResult GetApprovalOvertime(string batchNumber)
        {
            OvertimeApproval overtime = null;
            string status;
            try
            {
                var sqlQuery = $"SELECT BatchNumber, [Description], Note, NumberOfTransactions, Approver, RowId FROM {Helpers.InterCompanyId}.dbo.EFUPR30300 WHERE BatchNumber = '{batchNumber}'";
                overtime = _repository.ExecuteScalarQuery<OvertimeApproval>(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, model = overtime } };
        }

        public JsonResult GetOvertimeNotApproved(int module, string batchNumber = "")
        {
            var status = "";
            var xRegistros = new List<Overtime>();
            try
            {
                string sqlQuery;
                if (string.IsNullOrEmpty(batchNumber))
                    sqlQuery = "SELECT B.EMPLOYID EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartDate, A.EndDate, A.Hours, A.Note, " +
                    "CASE A.OvertimeType WHEN 1 THEN '100%' WHEN 2 THEN '30%' WHEN 3 THEN '15%' WHEN 4 THEN 'Feriado' ELSE '100%' END OvertimeTypeDesc, A.OvertimeType, A.Status, A.RowId " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30200 A INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                    $"WHERE A.Status = 0 AND LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' AND Module = {module}";
                else
                    sqlQuery = "SELECT B.EMPLOYID EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartDate, A.EndDate, A.Hours, A.Note, " +
                        "CASE A.OvertimeType WHEN 1 THEN '100%' WHEN 2 THEN '30%' WHEN 3 THEN '15%' WHEN 4 THEN 'Feriado' ELSE '100%' END OvertimeTypeDesc, A.OvertimeType, A.Status, A.RowId " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30200 A " +
                        $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                        $"INNER JOIN {Helpers.InterCompanyId}.dbo.EFUPR30310 C ON A.RowId = C.OvertimeRowId " +
                        $"WHERE C.BatchNumber = '{batchNumber}' ";
                xRegistros = _repository.ExecuteQuery<Overtime>(sqlQuery).ToList();
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, registros = xRegistros } };
        }

        public JsonResult SaveApprovalOvertime(string description, string approver, int rowId, string note, int module, List<int> detailRowIds, HttpPostedFileBase fileData = null)
        {
            var status = "";
            var newRowId = 0;
            try
            {
                var count = detailRowIds.Count;
                string sqlQuery;
                if (rowId == 0)
                {
                    var batchNumber = "OT" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR30300 ([BatchNumber],[Description],[NumberOfTransactions],[Approver],[Note],[Module],[Status],[LastUserId]) " +
                   $"VALUES ('{batchNumber}','{description}','{count}','{approver}','{note}',{module},0,'{Account.GetAccount(User.Identity.GetUserName()).UserId}') ";
                    _repository.ExecuteCommand(sqlQuery);
                    foreach (var item in detailRowIds)
                    {
                        sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR30310 (BatchNumber,OvertimeRowId,LastUserId) " +
                            $"VALUES ('{batchNumber}','{item}','{Account.GetAccount(User.Identity.GetUserName()).UserId}')";
                        _repository.ExecuteCommand(sqlQuery);
                        _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFUPR30200 SET Status = 1 WHERE RowId = '{item}'");
                    }
                }
                else
                {
                    sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EFUPR30300 SET [Description] = '{description}', [NumberOfTransactions] = '{count}', [Note] = '{note}', " +
                        $"[LastUserId] = '{Account.GetAccount(User.Identity.GetUserName()).UserId}', [Approver] = '{approver}', ModifiedDate = GETDATE() " +
                        $"WHERE RowId = '{rowId}'";
                    _repository.ExecuteCommand(sqlQuery);
                    var batchNumber = _repository.ExecuteScalarQuery<string>($"SELECT BatchNumber FROM {Helpers.InterCompanyId}.dbo.EFUPR30300 WHERE RowId = '{rowId}'");

                    _repository.ExecuteQuery<int>($"SELECT OvertimeRowId FROM {Helpers.InterCompanyId}.dbo.EFUPR30310 WHERE BatchNumber = '{batchNumber}'").ToList().ForEach(p =>
                    {
                        _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFUPR30200 SET Status = 0 WHERE RowId = '{p}'");
                    });
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFUPR30310 WHERE BatchNumber = '{batchNumber}'");

                    foreach (var item in detailRowIds)
                    {
                        sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR30310 (BatchNumber,OvertimeRowId,LastUserId) " +
                            $"VALUES ('{batchNumber}','{item}','{Account.GetAccount(User.Identity.GetUserName()).UserId}')";
                        _repository.ExecuteCommand(sqlQuery);
                        _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFUPR30200 SET Status = 1 WHERE RowId = '{item}'");
                    }
                }

                newRowId = rowId == 0 ? _repository.ExecuteScalarQuery<int>($"SELECT RowId FROM {Helpers.InterCompanyId}.dbo.EFUPR30300 ORDER BY RowId DESC") : rowId;
                status = "OK";
                if (fileData != null)
                {
                    byte[] fileStream = null;
                    using (var binaryReader = new BinaryReader(fileData.InputStream))
                        fileStream = binaryReader.ReadBytes(fileData.ContentLength);

                    var fileName = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1];
                    var fileType = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].Split('.')[1];

                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachmentInsert '{0}','{1}','{2}',{3},'{4}','{5}','{6}'",
                        Helpers.InterCompanyId, "Overtime" + newRowId, fileName, "0x" + BitConverter.ToString(fileStream).Replace("-", String.Empty),
                        fileType, Account.GetAccount(User.Identity.GetUserName()).UserId, "REQ"));
                }
                ProcessLogic.SendToSharepoint(newRowId.ToString(), 8, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
            }
            catch (Exception ex)
            {
                status = ex.Message;
                if (newRowId != 0 && rowId == 0)
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EFUPR30500 WHERE RowId = '{newRowId}'");
            }

            return new JsonResult { Data = new { status , RowId = newRowId } };
        }

        public JsonResult SendApprovalOvertime(int rowId)
        {
            string status;
            try
            {
                status = "OK";
                ProcessLogic.SendToSharepoint(rowId.ToString(), 8, Account.GetAccount(User.Identity.GetUserName()).Email, ref status);
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public JsonResult DeleteApprovalOvertime(string batchNumber)
        {
            var status = "";

            try
            {
                var sqlQuery = $"SELECT OvertimeRowId FROM {Helpers.InterCompanyId}.dbo.EFUPR30310 WHERE BatchNumber = '{batchNumber}'";
                _repository.ExecuteQuery<int>(sqlQuery).ToList().ForEach(p =>
                {
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFUPR30200 SET Status = 0 WHERE RowId = {p}");
                });
                sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFUPR30300 WHERE BatchNumber = '{batchNumber}'";
                _repository.ExecuteCommand(sqlQuery);
                sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EFUPR30310 WHERE BatchNumber = '{batchNumber}'";
                _repository.ExecuteCommand(sqlQuery);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public JsonResult GetOvertimeApproved(string batchNumber = "")
        {
            var status = "";
            var xRegistros = new List<OvertimeApproval>();
            try
            {
                string sqlQuery;
                if (string.IsNullOrEmpty(batchNumber))
                    sqlQuery = "SELECT BatchNumber, [Description], NumberOfTransactions, Approver, A.Note, B.FirstName + ' ' + B.LastName [User], A.RowId " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30300 A INNER JOIN INTRANET.dbo.USERS B ON A.LastUserId = B.UserId " +
                    $"WHERE A.Status = 2";
                else
                    sqlQuery = "SELECT B.EMPLOYID EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartDate, A.EndDate, A.Hours, A.Note, " +
                        "CASE A.OvertimeType WHEN 1 THEN '100%' WHEN 2 THEN '30%' WHEN 3 THEN '15%' WHEN 4 THEN 'Feriado' ELSE '100%' END OvertimeTypeDesc, A.OvertimeType, A.Status, A.RowId " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30200 A " +
                        $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                        $"INNER JOIN {Helpers.InterCompanyId}.dbo.EFUPR30610 C ON A.RowId = C.OvertimeRowId " +
                        $"WHERE C.BatchNumber = '{batchNumber}' ";
                xRegistros = _repository.ExecuteQuery<OvertimeApproval>(sqlQuery).ToList();
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status, registros = xRegistros } };
        }

        public JsonResult ProcessOvertime(string batchNumber, string description, string payrollDate, string note, List<string> detailBatches)
        {
            var status = "";
            try
            {
                var count = detailBatches.Count;
                var aPayrollDate = DateTime.ParseExact(payrollDate, "MM/dd/yyyy", null);
                string sqlQuery;

                sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR30600 ([BatchNumber],[Description],[PayrollDate],[Note],[NumberOfTransactions],[Status],[LastUserId]) " +
               $"VALUES ('{batchNumber}','{description}','{aPayrollDate.ToString("yyyyMMdd")}','{note}','{count}',4,'{Account.GetAccount(User.Identity.GetUserName()).UserId}') ";
                _repository.ExecuteCommand(sqlQuery);
                foreach (var item in detailBatches)
                {
                    sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR30610 (BatchNumber, OvertimeBatchNumber, LastUserId) " +
                        $"VALUES ('{batchNumber}','{item}','{Account.GetAccount(User.Identity.GetUserName()).UserId}')";
                    _repository.ExecuteCommand(sqlQuery);
                }

                _repository.ExecuteCommand($"INTRANET.dbo.PostOvertime '{Helpers.InterCompanyId}','{batchNumber}','{Account.GetAccount(User.Identity.GetUserName()).UserId}'");

                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        private Overtime GetOvertimeEntity(int rowId)
        {
            var sqlQuery = "SELECT B.EMPLOYID EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartDate, A.EndDate, A.Hours, A.Note, " +
            "CASE A.OvertimeType WHEN 1 THEN '100%' WHEN 2 THEN '30%' WHEN 3 THEN '15%' WHEN 4 THEN 'Feriado' ELSE '100%' END OvertimeTypeDesc, A.OvertimeType, A.Status, A.RowId " +
            $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30200 A INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
            $"WHERE A.RowId = '{rowId}' ";
            var overtime = _repository.ExecuteScalarQuery<Overtime>(sqlQuery);
            overtime.StartTime = overtime.StartDate.ToString("HH:mm");
            overtime.EndTime = overtime.EndDate.ToString("HH:mm");

            return overtime;
        }

        [HttpPost]
        public ActionResult PrintOvertimeReport(string batchNumber, string printOption = "10")
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                if (printOption == "10")
                    ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "OvertimeReportSummary.pdf",
                        string.Format("INTRANET.dbo.OvertimeReportSummary '{0}','{1}'", Helpers.InterCompanyId, batchNumber), 37, ref xStatus);
                else
                    ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "OvertimeReportDetail.pdf",
                    string.Format("INTRANET.dbo.OvertimeReportDetail '{0}','{1}'", Helpers.InterCompanyId, batchNumber), 38, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public ActionResult ApprovalOvertimeReport(int rowId)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "ApprovalOvertimeReport.pdf",
                    string.Format("INTRANET.dbo.ApprovalOvertimeReport '{0}','{1}'", Helpers.InterCompanyId, rowId), 36, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult GetImportHours(HttpPostedFileBase fileData)
        {
            var xStatus = "";
            var xRegistros = new List<Lookup>();
            var totalNocturnas = 0;
            var totalFeriados = 0;
            try
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
                    xRegistros = OfficeLogic.OvertimeHours(filePath, ref xStatus);
                }
                else
                    xStatus = "No se encontro el archivo especificado por favor verificar si existe o si lo ha especificado";

                if (xRegistros != null && xRegistros.Count > 0)
                    xStatus = "OK";
                else
                    if (string.IsNullOrEmpty(xStatus))
                    xStatus += "No se encontraron transacciones para este periodo";

                xRegistros.ForEach(p =>
                {
                    totalNocturnas += Convert.ToInt32(p.DataExtended);
                    totalFeriados += Convert.ToInt32(p.DataPlus);
                });
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = xRegistros, totalNocturnas, totalFeriados } };
        }

        public JsonResult SaveImportOvertime(string overtimeDate, string note, List<Lookup> items)
        {
            var status = "";
            try
            {
                string sqlQuery;
                var aOvertimeDate = DateTime.ParseExact(overtimeDate, "MM/dd/yyyy", null);
                foreach (var item in items)
                {
                    sqlQuery = "";
                    if (Convert.ToDecimal(item.DataExtended) > 0)
                    {
                        sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR30200 ([EmployeeId],[StartDate],[EndDate],[OvertimeType],[Hours],[Note],[Status],[Module],[LastUserId]) " +
                        $"VALUES ('{item.Id}','{aOvertimeDate.ToString("yyyyMMdd")}','{aOvertimeDate.ToString("yyyyMMdd")}','{3}','{item.DataExtended}','{note}',0,{2},'{Account.GetAccount(User.Identity.GetUserName()).UserId}') ";
                        _repository.ExecuteCommand(sqlQuery);
                    }
                    if (Convert.ToDecimal(item.DataPlus) > 0)
                    {
                        sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFUPR30200 ([EmployeeId],[StartDate],[EndDate],[OvertimeType],[Hours],[Note],[Status],[Module],[LastUserId]) " +
                        $"VALUES ('{item.Id}','{aOvertimeDate.ToString("yyyyMMdd")}','{aOvertimeDate.ToString("yyyyMMdd")}','{4}','{item.DataPlus}','{note}',0,{2},'{Account.GetAccount(User.Identity.GetUserName()).UserId}') ";
                        _repository.ExecuteCommand(sqlQuery);
                    }
                }

                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        #endregion

        #region Adjuntos

        [HttpPost]
        public JsonResult AttachFile(HttpPostedFileBase fileData, string id)
        {
            var status = false;

            try
            {
                if (fileData != null)
                {
                    byte[] fileStream = null;
                    using (var binaryReader = new BinaryReader(fileData.InputStream))
                        fileStream = binaryReader.ReadBytes(fileData.ContentLength);

                    var fileName = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1];
                    var fileType = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].Split('.')[1];

                    _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachmentInsert '{0}','{1}','{2}',{3},'{4}','{5}','{6}'",
                        Helpers.InterCompanyId, id, fileName, "0x" + BitConverter.ToString(fileStream).Replace("-", String.Empty),
                        fileType, Account.GetAccount(User.Identity.GetUserName()).UserId, "REQ"));
                }
                status = true;
            }
            catch
            {
                status = false;
            }

            return new JsonResult { Data = new { status } };
        }

        [HttpPost]
        public ActionResult LoadAttachmentFiles(string id)
        {
            try
            {
                var files = new List<string>();
                var sqlQuery = $"SELECT RTRIM(fileName) FileName FROM {Helpers.InterCompanyId}.dbo.CO00105 WHERE DOCNUMBR = '{id}' AND DELETE1 = 0";
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
                var sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.CO00105 SET DELETE1 = 1 WHERE DOCNUMBR = '{id}' AND RTRIM(fileName) = '{fileName}'";
                _repository.ExecuteCommand(sqlQuery);

                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult Download(string id, string FileName)
        {
            var sqlQuery = "SELECT BinaryBlob, FileType, A.fileName "
                              + $"FROM {Helpers.InterCompanyId}.dbo.CO00105 A "
                              + $"INNER JOIN {Helpers.InterCompanyId}.dbo.coAttachmentItems B ON A.Attachment_ID = B.Attachment_ID " +
                              $"WHERE A.DOCNUMBR = '{id}' AND A.DELETE1 = 0 AND RTRIM(A.fileName) = '{FileName}'";

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

        public class AttachmentViewModel
        {
            public HttpPostedFileBase FileData { get; set; }
        }

        #endregion
    }
}