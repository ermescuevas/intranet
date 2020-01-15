using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Seaboard.Intranet.Data.Repository;

namespace Seaboard.Intranet.BusinessLogic
{
    public class HelperLogic
    {
        private static SeaboContext _db;
        private static GenericRepository _repository;

        public static bool InsertSignature(string userId)
        {
            try
            {
                _db = new SeaboContext();
                _repository = new GenericRepository(_db);
                var sqlQuery = "SELECT TOP 1 SING FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40101 WHERE USERID = '" + userId + "' AND TYPE = 1 AND SING IS NOT NULL";
                var sign = _repository.ExecuteScalarQuery<byte[]>(sqlQuery);

                if (sign != null)
                {
                    var myData = sign;
                    var stream = new MemoryStream(myData);
                    Image.FromStream(stream).Save(Helpers.ReportPath + @"\Images\Image.jpeg");
                    return true;
                }

                Properties.Resources.blank.Save(Helpers.ReportPath + @"\Images\Image.jpeg");
                return false;
            }
            catch { return false; }
        }

        public static string GetAproverByDepartmentDescription(string departmentId)
        {
            try
            {
                _db = new SeaboContext();
                _repository = new GenericRepository(_db);
                var sqlQuery = "SELECT TOP 1 A.USERID FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40101 A "
                    + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40100 B "
                    + "ON A.DEPRTMID = B.DEPRTMID "
                    + "WHERE B.DEPRTMDS = '" + departmentId + "' AND A.TYPE = 1 AND A.ISPRINC = 1";

                var approver = _repository.ExecuteScalarQuery<string>(sqlQuery);

                if (approver != null)
                    return approver.Trim();
                else
                    return "";
            }
            catch { return ""; }
        }

        public static string AsignaciónSecuencia(string formulario, string userName)
        {
            _db = new SeaboContext();
            _repository = new GenericRepository(_db);
            var secuencia = _repository.ExecuteScalarQuery<string>(String.Format("LODYNDEV.dbo.LPSY40000P1 '{0}','{1}','{2}'", Helpers.InterCompanyId, formulario, userName));
            return secuencia?.Trim() ?? "";
        }

        public static void DesbloqueoSecuencia(string secuencia, string formulario, string usuario)
        {
            _db = new SeaboContext();
            _repository = new GenericRepository(_db);
            _repository.ExecuteCommand($"LODYNDEV.dbo.LPSY40001P1 '{Helpers.InterCompanyId}','{secuencia}','{formulario}','{usuario}'");
        }

        public static string GetAproverPayment()
        {
            try
            {
                _db = new SeaboContext();
                _repository = new GenericRepository(_db);
                var aprover = _repository.ExecuteScalarQuery<string>("SELECT PAYAPROV FROM " + Helpers.InterCompanyId + ".dbo.LPSY00101 ");
                return aprover ?? "";
            }
            catch { return ""; }
        }

        public static string GetSecondAproverPayment()
        {
            try
            {
                _db = new SeaboContext();
                _repository = new GenericRepository(_db);
                var aprover = _repository.ExecuteScalarQuery<string>("SELECT PAYSCAPR FROM " + Helpers.InterCompanyId + ".dbo.LPSY00101 ");
                return aprover ?? "";
            }
            catch { return ""; }
        }

        public static bool InsertSignaturePayment(string userId)
        {
            try
            {
                _db = new SeaboContext();
                _repository = new GenericRepository(_db);
                var sqlQuery = "SELECT TOP 1 SING FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40101 WHERE USERID = '" + userId + "' AND TYPE = 1 AND SING IS NOT NULL";
                var sign = _repository.ExecuteScalarQuery<byte[]>(sqlQuery);

                if (sign != null)
                {
                    var myData = sign;
                    var stream = new MemoryStream(myData);
                    Image.FromStream(stream).Save(Helpers.ReportPath + @"\Images\Aprobador2.jpeg");

                    return true;
                }

                Properties.Resources.blank.Save(Helpers.ReportPath + @"\Images\Aprobador2.jpeg");
                return false;
            }
            catch { return false; }
        }

        public static bool GetPermission(string userId, string controller, string action)
        {
            _db = new SeaboContext();
            _repository = new GenericRepository(_db);
            var permission = _repository.GetAll<Permission>(m => m.Action == action && m.Controller == controller).FirstOrDefault();
            var isPermit = _repository.GetAll<UserPermission>(m => m.UserId == userId && m.PermissionId == permission.PermissionId).Any();

            return isPermit;
        }

        public static DateTime GetLastDate()
        {
            try
            {
                _db = new SeaboContext();
                _repository = new GenericRepository(_db);
                var sqlQuery = $"SELECT TOP 1 FECHA FROM {Helpers.InterCompanyId}.dbo.LO_VENTAS_GENERAL_NCF ORDER BY FECHA DESC";
                var fecha = _repository.ExecuteScalarQuery<DateTime?>(sqlQuery) ?? new DateTime(1900, 1, 1);

                return fecha;
            }
            catch { return new DateTime(1900, 1, 1); }
        }
    }
}