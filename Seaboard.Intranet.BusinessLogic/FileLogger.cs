using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using Seaboard.Intranet.Domain;
using System;
using System.Runtime.InteropServices;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Data.Repository;

namespace Seaboard.Intranet.BusinessLogic
{
    public class FileLogger
    {
        private static string _connectionString = string.Empty;
        private static readonly char _separador = ',';
        private static readonly string TableName = "TEMPUPR10302";

        public FileLogger() { }

        public FileLogger(string db)
        {
            _connectionString = Helpers.ConnectionStrings.Replace("INTRANET", db);
        }

        #region Excel Data

        public static void GetExcelData(string path, string batch)
        {
            string excelString = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + path + ";Extended Properties=\"Excel 12.0 Xml;HDR=YES;IMEX=1;\"";

            DataTable dt = new DataTable();
            dt.Columns.Add("BATCHID", typeof(string));
            dt.Columns.Add("EMPLOYEEID", typeof(string));
            dt.Columns.Add("TRANSACTIONTYPE", typeof(int));
            dt.Columns.Add("PAYCODE", typeof(string));
            dt.Columns.Add("BEGINNINGDATE", typeof(DateTime));
            dt.Columns.Add("ENDINGDATE", typeof(DateTime));
            dt.Columns.Add("UNIST", typeof(decimal));
            dt.Columns.Add("NOTE", typeof(string));

            try
            {
                var xlApp = new Microsoft.Office.Interop.Excel.Application();
                var xlWorkbook = xlApp.Workbooks.Open(path);
                var xlWorksheet = xlWorkbook.Sheets[1] as Microsoft.Office.Interop.Excel._Worksheet;
                var xlRange = xlWorksheet?.UsedRange;
                var rowCount = xlRange?.Rows.Count;
                var colCount = xlRange?.Columns.Count;
                var employeeId = string.Empty;
                var transactionType = 0;
                var payCode = string.Empty;
                var beginningDate = DateTime.Now;
                var endingDate = DateTime.Now;
                decimal units = 0;
                var note = string.Empty;
                for (int i = 2; i <= rowCount; i++)
                {
                    employeeId = (xlRange.Cells[i, "A"].Value2 ?? "").ToString();
                    if (!string.IsNullOrEmpty(employeeId))
                    {
                        transactionType = Convert.ToInt32(xlRange.Cells[i, "B"].Value2 ?? 0);
                        payCode = xlRange.Cells[i, "C"].Value2.ToString();
                        beginningDate = DateTime.ParseExact(xlRange.Cells[i, "D"].Value2.ToString() ?? new DateTime(1900, 1, 1).ToString("yyyyMMdd"), "yyyyMMdd", null);
                        endingDate = DateTime.ParseExact(xlRange.Cells[i, "E"].Value2.ToString() ?? new DateTime(1900, 1, 1).ToString("yyyyMMdd"), "yyyyMMdd", null);
                        units = Convert.ToDecimal(xlRange.Cells[i, "F"].Value2 ?? 0);
                        note = (xlRange.Cells[i, "G"].Value2 ?? "").ToString();

                        dt.Rows.Add(batch, employeeId, transactionType, payCode, beginningDate, endingDate, units, note);
                    }
                }
                LlenarTabla(dt);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (xlRange != null) Marshal.ReleaseComObject(xlRange);
                if (xlWorksheet != null) Marshal.ReleaseComObject(xlWorksheet);
                xlWorkbook.Close(false, System.Reflection.Missing.Value, System.Reflection.Missing.Value);
                Marshal.ReleaseComObject(xlWorkbook);
                xlApp.Quit();
                Marshal.ReleaseComObject(xlApp);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Txt Data

        public static void GetTxtData(string path, string batch)
        {
            DataTable dt = new DataTable();
            InsertarEsquemaEnDataTable(dt);
            string ruta = @"" + path;
            EliminarFilasNulas(dt, ruta, batch);
            LlenarTabla(dt);
        }

        private static void InsertarEsquemaEnDataTable(DataTable dt)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlDataAdapter adapter = new SqlDataAdapter($"SELECT TOP 0 [BATCHID],[EMPLOYEEID] ,[TRANSACTIONTYPE],[PAYCODE],[BEGINNINGDATE] ,[ENDINGDATE],[UNIST],[NOTE] " +
                    $"FROM {TableName}", connection))
                {
                    adapter.Fill(dt);
                };
            }
        }

        private static void EliminarFilasNulas(DataTable dt, string ruta, string batch)
        {
            List<string> lineas = File.ReadLines(ruta).ToList();
            int i = 0;
            dt.Clear();

            while (i < lineas.Count)
            {
                if (lineas[i].Equals("") || (lineas[i]).ToLower().Contains("employeeid"))
                    lineas.RemoveAt(i);
                else
                    i++;
            }
            LlenarDataTable(dt, lineas, batch);
        }

        private static void LlenarDataTable(DataTable dt, List<string> lineas, string batch)
        {
            int index = 0;
            for (int i = 0; i < lineas.Count; i++)
            {
                index = lineas.IndexOf(lineas[i]);
                lineas[i] = batch + "," + lineas[i];

                var arr = lineas[i].Split(_separador).Select(x => x.Trim()).ToArray();

                DataRow dr = dt.NewRow();
                for (int j = 0; j < dt.Columns.Count; j++)
                    dr[j] = arr[j];

                dt.Rows.Add(dr);
            }
        }

        #endregion

        private static void LlenarTabla(DataTable dt)
        {
            var db = new SeaboContext();
            var _repository = new GenericRepository(db);
            foreach (DataRow item in dt.Rows)
            {
                var sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.TEMPUPR10302([BATCHID],[EMPLOYEEID],[TRANSACTIONTYPE],[PAYCODE],[BEGINNINGDATE],[ENDINGDATE],[UNIST],[NOTE])" +
                    $"VALUES('{item["BATCHID"].ToString()}','{item["EMPLOYEEID"].ToString()}','{item["TRANSACTIONTYPE"].ToString()}','{item["PAYCODE"].ToString()}'," +
                    $"'{item["BEGINNINGDATE"].ToString()}','{item["ENDINGDATE"].ToString()}','{item["UNIST"].ToString()}','{item["NOTE"].ToString()}')";

                _repository.ExecuteCommand(sqlQuery);
            }
        }
    }
}