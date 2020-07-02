using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using System.IO;
using Seaboard.Intranet.Data.Repository;
using Microsoft.Office.Interop.Excel;
using System.Linq;

namespace Seaboard.Intranet.BusinessLogic
{
    public class OfficeLogic
    {
        private static SeaboContext _db;
        private static GenericRepository _repository;

        #region Public Methods

        public static List<MemDebtData> MemDebtProcess(DateTime mes, ref string message)
        {
            try
            {
                var lista = new List<MemDebtData>();

                _db = new SeaboContext();
                _repository = new GenericRepository(_db);
                var celdas = _repository.ExecuteScalarQuery<MemConfiguration>(
                    "SELECT RUTA FilePath, CONVERT(INT, HOJA) Sheet, ACREEDOR Créditor, CLIENTEID Debtor, PRODUCTO1 Producto1, PRODUCTO2 Producto2, PRODUCTO3 Producto3, " +
                    "PRODUCTO4 Producto4, PRODUCTO5 Producto5, PRODUCTO6 Producto6, PRODUCTO7 Producto7 FROM " +
                    Helpers.InterCompanyId + ".dbo.SESY01502");

                var ruta = celdas.FilePath + "RTE " + mes.ToString("MMyy") + ".xls";
                var isValid = false;

                if (File.Exists(ruta +".xlsx"))
                {
                    isValid = true;
                    ruta += ".xlsx";
                }

                if (File.Exists(ruta + ".xlsm"))
                {
                    isValid = true;
                    ruta += ".xlsm";
                }

                if (!isValid)
                {
                    message = "No existe el archivo correspondiente a ese mes en la ruta " + celdas.FilePath + "RTE " + mes.ToString("MMyy") + ".xls";
                    return lista;
                }
                var xlApp = new Application();
                var xlWorkbook = xlApp.Workbooks.Open(ruta);
                var xlWorksheet = xlWorkbook.Sheets[celdas.Sheet] as _Worksheet;
                var xlRange = xlWorksheet?.UsedRange;
                var rowCount = xlRange?.Rows.Count;
                var colCount = xlRange?.Columns.Count;
                var tmpVendorId = string.Empty;
                isValid = false;

                var rowInit = 6;

                for (var i = rowInit; i <= rowCount; i++)
                {
                    for (var j = 1; j <= colCount; j++)
                    {
                        if (j != 1) continue;
                        var excComp = celdas.Debtor;
                        var excProd1 = celdas.Producto1;
                        var excProd2 = celdas.Producto2;
                        var excProd3 = celdas.Producto3;
                        var excProd4 = celdas.Producto4;
                        var excProd5 = celdas.Producto5;
                        var excProd6 = celdas.Producto6;
                        var excProd7 = celdas.Producto7;

                        if (xlRange.Cells[i, j] == null || xlRange.Cells[i, j]?.Value2 == null) continue;
                        tmpVendorId = xlRange.Cells[i, j]?.Value2;
                        if (tmpVendorId.ToUpper().Contains("ACREEDOR") || tmpVendorId.ToUpper().Contains("ACREEDOR"))
                        {
                            isValid = true;
                            i++;
                            tmpVendorId = xlRange.Cells[i, j]?.Value2;
                        }

                        if (!isValid)
                            continue;

                        var row = i;
                        var colN = NumberFromExcelColumn(excComp);

                        for (var xx = row; xx <= rowCount; xx++)
                        {
                            var tmpVendor = xlRange.Cells[xx, colN]?.Value2 ?? "";
                            if (tmpVendor == "SEABOARD")
                            {
                                var col1 = NumberFromExcelColumn(excProd1);
                                double valor1 = Convert.ToDouble(xlRange.Cells[xx, col1]?.Value2);
                                var col2 = NumberFromExcelColumn(excProd2);
                                double valor2 = Convert.ToDouble(xlRange.Cells[xx, col2]?.Value2);
                                var col3 = NumberFromExcelColumn(excProd3);
                                double valor3 = Convert.ToDouble(xlRange.Cells[xx, col3]?.Value2);
                                var col4 = NumberFromExcelColumn(excProd4);
                                double valor4 = Convert.ToDouble(xlRange.Cells[xx, col4]?.Value2);
                                var col5 = NumberFromExcelColumn(excProd5);
                                double valor5 = Convert.ToDouble(xlRange.Cells[xx, col5]?.Value2);
                                var col6 = NumberFromExcelColumn(excProd6);
                                double valor6 = Convert.ToDouble(xlRange.Cells[xx, col6]?.Value2);
                                var col7 = NumberFromExcelColumn(excProd7);
                                double valor7 = Convert.ToDouble(xlRange.Cells[xx, col7]?.Value2);

                                if (valor1 > 0 || valor2 > 0 || valor3 > 0 || valor4 > 0 || valor5 > 0 || valor6 > 0 || valor7 > 0)
                                {
                                    lista.Add(new MemDebtData()
                                    {
                                        Acreedor = "",
                                        Suplidor = tmpVendorId,
                                        Producto1 = valor1,
                                        Producto2 = valor2,
                                        Producto3 = valor3,
                                        Producto4 = valor4,
                                        Producto5 = valor5,
                                        Producto6 = valor6,
                                        Producto7 = valor7
                                    });
                                }
                            }
                            if (xlRange.Cells[xx, 1].Text.ToString() == tmpVendorId + " Total")
                                break;
                            i = xx + 1;
                        }
                    }
                    if (xlRange.Cells[i, 1].Text.ToString() == tmpVendorId + "TOTAL")
                        break;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (xlRange != null) Marshal.ReleaseComObject(xlRange);
                if (xlWorksheet != null) Marshal.ReleaseComObject(xlWorksheet);
                xlWorkbook.Close(false, System.Reflection.Missing.Value, System.Reflection.Missing.Value);
                Marshal.ReleaseComObject(xlWorkbook);
                xlApp.Quit();
                Marshal.ReleaseComObject(xlApp);

                return lista;
            }
            catch (Exception e)
            {
                message = e.Message;
                return new List<MemDebtData>();
            }
        }

        public static List<MEMData> MEMBillingProcess(DateTime mes, string filePath, ref string message)
        {
            List<MEMData> memData = new List<MEMData>();
            _db = new SeaboContext();
            _repository = new GenericRepository(_db);
            List<MemDataDetail> details;
            try
            {
                var configuration = _repository.ExecuteScalarQuery<MemConfiguration>(
                     "SELECT FILEPATH FilePath, SHEET Sheet, RWBEGDAT RowBeginningData, RWNAMCOL RowProductName, CREDEXCCOL Créditor, DEBTEXCCOL Debtor FROM " +
                     Helpers.InterCompanyId + ".dbo.EFRM40101");
                var products = _repository.ExecuteQuery<MemConfigurationDetail>($"SELECT ITEMNMBR ItemNumber, ITEMDESC ItemDescription, COLNINDX ColumnIndex, EXTNLABL ExternalLabel " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EFRM40102 ORDER BY COLNINDX ").ToList();
                string ruta;
                if (!string.IsNullOrEmpty(filePath))
                    ruta = filePath;
                else
                    ruta = configuration.FilePath + mes.Year.ToString() + "\\RTE\\RTE " + mes.ToString("MMyy") + ".xls";
                var isValid = false;

                if (products == null || products.Count == 0)
                {
                    message += "\nNo se ha configurado aun los componentes de facturación debe de ir a la configuración general y especificarlos";
                    return null;
                }

                if (File.Exists(ruta))
                    isValid = true;

                if (File.Exists(ruta + ".xlsx"))
                {
                    isValid = true;
                    ruta += ".xlsx";
                }

                if (File.Exists(ruta + ".xlsm"))
                {
                    isValid = true;
                    ruta += ".xlsm";
                }

                if (!isValid)
                {
                    message += "\nNo existe el archivo correspondiente a ese mes en la ruta " + ruta;
                    return null;
                }

                if (isValid)
                {
                    Application app = new Application();
                    Workbook book = null;
                    book = app.Workbooks.Open(ruta, false, true);
                    Worksheet sheet = book.Sheets[configuration.Sheet] as Worksheet;

                    for (int i = 1; i < 1000; i++)
                    {
                        Range Créditor;
                        Range CustomerId;
                        Range NewCréditor;
                        Range ProductName;
                        Range ProductAmount;
                        Créditor = sheet.Cells[i, configuration.Créditor] as Range;
                        if (Créditor?.Value != null)
                        {
                            if (Créditor.Value.ToString() == "SEABOARD")
                            {
                                for (int j = i; j < 1000; j++)
                                {
                                    NewCréditor = sheet.Cells[j, configuration.Créditor] as Range;
                                    if (NewCréditor.Value != null)
                                        if (NewCréditor.Value.ToString() == "SEABOARD" + " Total")
                                            break;

                                    CustomerId = sheet.Cells[j, configuration.Debtor] as Range;
                                    details = new List<MemDataDetail>();
                                    foreach (var item in products)
                                    {
                                        ProductName = sheet.Cells[configuration.RowProductName, item.ColumnIndex] as Range;
                                        ProductAmount = sheet.Cells[j, item.ColumnIndex] as Range;
                                        if (ProductAmount.Value > 0)
                                            details.Add(new MemDataDetail
                                            {
                                                ProductId = item.ItemNumber,
                                                ProductName = string.IsNullOrEmpty(item.ExternalLabel) ? Convert.ToString(ProductName?.Value ?? "") : item.ExternalLabel,
                                                Amount = Convert.ToDecimal(ProductAmount?.Value ?? 0)
                                            });
                                    }

                                    if (details.Count > 0)
                                        memData.Add(new MEMData
                                        {
                                            Créditor = Créditor.Value.ToString(),
                                            Debtor = CustomerId.Value.ToString(),
                                            TotalAmount = details.Sum(x => x.Amount),
                                            CurrencyId = "RDPESO",
                                            BillingType = BillingType.MEM,
                                            Details = details
                                        });
                                }
                                break;
                            }
                        }
                    }

                    if (book != null)
                        book.Close(0);
                    app.Workbooks.Close();
                }

                return memData;
            }
            catch (Exception ex)
            {
                message += "\n" + ex.Message + "\n" + ex.StackTrace;
                return null;
            }
        }

        public static List<MEMData> GetTransactionsFile(string filePath, int tipo, BillingType billingType, ref string message)
        {
            var listTransaction = new List<MEMData>();
            try
            {
                decimal Amount = 0m;
                decimal quantity = 0m;
                List<Transaction> lista = new List<Transaction>();
                if (File.Exists(filePath))
                {
                    var xlApp = new Application();
                    Workbook xlWorkbook = null;
                    xlWorkbook = xlApp.Workbooks.Open(filePath, false, true);
                    Worksheet xlWorksheet;

                    if (tipo == 0)
                        xlWorksheet = xlWorkbook.Sheets["FACT"] as Worksheet;
                    else if (tipo == 1)
                        xlWorksheet = xlWorkbook.Sheets["NC"] as Worksheet;
                    else
                        xlWorksheet = xlWorkbook.Sheets["ND"] as Worksheet;

                    for (int i = 3; i < 50; i++)
                    {
                        Range cliente;
                        Range articulo;
                        Range descripción;
                        Range monto;
                        Range nota;
                        Range marcador = null;
                        Range currency;
                        Range cantidad = null;

                        if (tipo == 0)
                        {
                            cliente = xlWorksheet.Cells[i, "B"] as Range;
                            articulo = xlWorksheet.Cells[i, "C"] as Range;
                            descripción = xlWorksheet.Cells[i, "D"] as Range;
                            cantidad = xlWorksheet.Cells[i, "E"] as Range;
                            monto = xlWorksheet.Cells[i, "F"] as Range;
                            currency = xlWorksheet.Cells[i, "G"] as Range;
                            nota = xlWorksheet.Cells[i, "H"] as Range;
                            marcador = xlWorksheet.Cells[i, "I"] as Range;
                        }
                        else
                        {
                            cliente = xlWorksheet.Cells[i, "A"] as Range;
                            articulo = null;
                            descripción = xlWorksheet.Cells[i, "B"] as Range;
                            monto = xlWorksheet.Cells[i, "C"] as Range;
                            currency = xlWorksheet.Cells[i, "D"] as Range;
                            nota = xlWorksheet.Cells[i, "E"] as Range;
                            marcador = xlWorksheet.Cells[i, "F"] as Range;
                        }

                        if (monto != null && !string.IsNullOrEmpty(cliente.Value))
                        {
                            decimal.TryParse(Convert.ToString(monto.Value), out Amount);
                            decimal.TryParse(Convert.ToString(cantidad.Value), out quantity);
                            lista.Add(new Transaction
                            {
                                Moneda = (currency.Value ?? "").ToString(),
                                CustomerId = (cliente.Value ?? "").ToString(),
                                ItemId = tipo == 0 ? (articulo.Value ?? "").ToString() : "",
                                Quantity = quantity,
                                Description = (descripción.Value ?? "").ToString(),
                                Amount = Amount,
                                Notes = nota.Value == null ? "" : nota.Value.ToString().Trim(),
                                Flag = marcador.Value == null ? "1" : marcador.Value.ToString()
                            });
                        }
                    }
                    lista.Select(p => new { p.CustomerId, p.Flag, p.Notes }).Distinct().ToList().ForEach(item =>
                     {
                         listTransaction.Add(new MEMData
                         {
                             Créditor = "SEABOARD",
                             Debtor = item.CustomerId.ToString(),
                             Marker = item.Flag,
                             BillingType = billingType,
                             Note = item.Notes,
                             Details = new List<MemDataDetail>()
                         });
                     });
                    foreach (var customer in listTransaction)
                        foreach (var item in lista)
                            if (item.CustomerId == customer.Debtor && item.Flag == customer.Marker)
                                customer.Details.Add(new MemDataDetail
                                {
                                    ProductId = item.ItemId,
                                    ProductName = item.Description,
                                    Amount = item.Amount,
                                    Quantity = item.Quantity,
                                    LineTotalAmount = item.Quantity * item.Amount,
                                    CurrencyId = item.Moneda == "" ? "" : item.Moneda == "US$" ? "Z-US$" : "RDPESO",
                                });

                    listTransaction.ForEach(p =>
                    {
                        p.TotalAmount = p.Details.Sum(q => q.LineTotalAmount);
                        p.CurrencyId = p.Details.FirstOrDefault()?.CurrencyId ?? "";
                    });

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    if (xlWorksheet != null) Marshal.ReleaseComObject(xlWorksheet);
                    xlWorkbook.Close(false, System.Reflection.Missing.Value, System.Reflection.Missing.Value);
                    Marshal.ReleaseComObject(xlWorkbook);
                    xlApp.Quit();
                    Marshal.ReleaseComObject(xlApp);
                }
                else
                    message = "No existe el archivo correspondiente a ese mes en la ruta " + filePath;
                return listTransaction;
            }
            catch(Exception ex)
            {
                message += "\n" + ex.Message + "\n" + ex.StackTrace;
                return null;
            }
        }

        public static List<MEMData> GetTransactionsFile(string filePath, BillingType billingType, ref string message)
        {
            var listTransaction = new List<MEMData>();
            try
            {
                decimal Amount = 0m;
                List<Transaction> lista = new List<Transaction>();
                if (File.Exists(filePath))
                {
                    var xlApp = new Application();
                    Workbook xlWorkbook = null;
                    xlWorkbook = xlApp.Workbooks.Open(filePath, false, true);
                    Worksheet xlWorksheet;
                    xlWorksheet = xlWorkbook.Sheets[1] as Worksheet;
                    for (int i = 4; i < 50; i++)
                    {
                        Range cliente;
                        Range articulo;
                        Range tipoNota;
                        Range descripción;
                        Range monto;
                        Range referenceInvoice;
                        Range referenceNcf;
                        Range nota;
                        Range marcador = null;
                        Range currency;

                        cliente = xlWorksheet.Cells[i, "A"] as Range;
                        tipoNota = xlWorksheet.Cells[i, "B"] as Range;
                        articulo = xlWorksheet.Cells[i, "C"] as Range;
                        descripción = xlWorksheet.Cells[i, "D"] as Range;
                        monto = xlWorksheet.Cells[i, "E"] as Range;
                        referenceInvoice = xlWorksheet.Cells[i, "G"] as Range;
                        referenceNcf = xlWorksheet.Cells[i, "H"] as Range;
                        currency = xlWorksheet.Cells[i, "I"] as Range;
                        nota = xlWorksheet.Cells[i, "J"] as Range;
                        marcador = xlWorksheet.Cells[i, "K"] as Range;

                        if (monto != null && !string.IsNullOrEmpty(cliente.Value))
                        {
                            decimal.TryParse(Convert.ToString(monto.Value), out Amount);
                            lista.Add(new Transaction
                            {
                                Moneda = (currency.Value ?? "").ToString(),
                                CustomerId = (cliente.Value ?? "").ToString(),
                                DocumentType = (tipoNota.Value ?? "NC").ToString(),
                                ItemId = (articulo.Value ?? "").ToString(),
                                Description = (descripción.Value ?? "").ToString(),
                                Amount = Amount,
                                ReferenceInvoice = (referenceInvoice.Value ?? "").ToString(),
                                ReferenceNcf = (referenceNcf.Value ?? "").ToString(),
                                Notes = nota.Value == null ? "" : nota.Value.ToString().Trim(),
                                Flag = marcador.Value == null ? "1" : marcador.Value.ToString()
                            });
                        }
                    }
                    lista.Select(p => new { p.CustomerId, p.Flag, p.Notes, p.DocumentType }).Distinct().ToList().ForEach(item =>
                    {
                        listTransaction.Add(new MEMData
                        {
                            Créditor = "SEABOARD",
                            Debtor = item.CustomerId.ToString(),
                            Marker = item.Flag,
                            BillingType = billingType,
                            Note = item.Notes,
                            DocumentType = item.DocumentType,
                            Details = new List<MemDataDetail>()
                        });
                    });
                    foreach (var customer in listTransaction)
                        foreach (var item in lista)
                            if (item.CustomerId == customer.Debtor && item.Flag == customer.Marker)
                                customer.Details.Add(new MemDataDetail
                                {
                                    ProductId = item.ItemId,
                                    ProductName = item.Description,
                                    Amount = item.Amount,
                                    CurrencyId = item.Moneda == "" ? "" : item.Moneda == "US$" ? "Z-US$" : "RDPESO",
                                    ReferenceInvoice = item.ReferenceInvoice,
                                    ReferenceNCF = item.ReferenceNcf
                                });

                    listTransaction.ForEach(p =>
                    {
                        p.TotalAmount = p.Details.Sum(q => q.Amount);
                        p.CurrencyId = p.Details.FirstOrDefault()?.CurrencyId ?? "";
                        p.ReferenceInvoice = p.Details.FirstOrDefault()?.ReferenceInvoice ?? "";
                        p.ReferenceNCF = p.Details.FirstOrDefault()?.ReferenceNCF ?? "";
                    });

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    if (xlWorksheet != null) Marshal.ReleaseComObject(xlWorksheet);
                    xlWorkbook.Close(false, System.Reflection.Missing.Value, System.Reflection.Missing.Value);
                    Marshal.ReleaseComObject(xlWorkbook);
                    xlApp.Quit();
                    Marshal.ReleaseComObject(xlApp);
                }
                else
                    message = "No existe el archivo correspondiente a ese mes en la ruta " + filePath;
                return listTransaction;
            }
            catch (Exception ex)
            {
                message += "\n" + ex.Message + "\n" + ex.StackTrace;
                return null;
            }
        }

        public static void WriteDataFiscalSalesFile(List<FiscalSalesTransaction> transactions, string filePath, string excelFilePath, string period, string taxRegisterNumber)
        {
            try
            {
                var xlApp = new Application
                {
                    DisplayAlerts = false
                };
                Workbook xlWorkbook = null;
                xlWorkbook = xlApp.Workbooks.Open(filePath);
                Worksheet xlWorksheet;
                xlWorksheet = xlWorkbook.Sheets[1] as Worksheet;
                int rowNumber = 12;
                xlWorksheet.Cells[4, "C"] = taxRegisterNumber.Replace("-", "");
                xlWorksheet.Cells[5, "C"] = period;
                xlWorksheet.Cells[6, "C"] = transactions.Count;
                foreach (var item in transactions)
                {
                    xlWorksheet.Cells[rowNumber, "B"] = item.Rnc.Trim();
                    xlWorksheet.Cells[rowNumber, "C"] = item.Rnc.Trim().Replace("-", "").Length == 9 ? "1" : "2";
                    xlWorksheet.Cells[rowNumber, "D"] = item.Ncf.Trim();
                    xlWorksheet.Cells[rowNumber, "E"] = item.ApplyNcf.Trim();
                    xlWorksheet.Cells[rowNumber, "F"] = item.IncomeType.Trim();
                    xlWorksheet.Cells[rowNumber, "G"] = item.DocumentDate.ToString("yyyyMMdd");
                    xlWorksheet.Cells[rowNumber, "I"] = item.DocumentAmount;
                    xlWorksheet.Cells[rowNumber, "J"] = item.TaxAmount;
                    xlWorksheet.Cells[rowNumber, "K"] = item.WithholdTax;
                    xlWorksheet.Cells[rowNumber, "U"] = item.DocumentAmount;
                    rowNumber++;
                }

                if (File.Exists(excelFilePath))
                    File.Delete(excelFilePath);

                xlWorkbook.SaveAs(excelFilePath, System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value,
                                           System.Reflection.Missing.Value, System.Reflection.Missing.Value, XlSaveAsAccessMode.xlNoChange,
                                           System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value,
                                           System.Reflection.Missing.Value, System.Reflection.Missing.Value);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (xlWorksheet != null) Marshal.ReleaseComObject(xlWorksheet);
                xlWorkbook.Close(false, System.Reflection.Missing.Value, System.Reflection.Missing.Value);
                Marshal.ReleaseComObject(xlWorkbook);
                xlApp.Quit();
                Marshal.ReleaseComObject(xlApp);
            }
            catch { }
        }

        public static List<Domain.ViewModels.Lookup> OvertimeHours(string path, ref string message)
        {
            try
            {
                var lista = new List<Domain.ViewModels.Lookup>();

                _db = new SeaboContext();
                _repository = new GenericRepository(_db);

                var ruta = path;
                var isValid = false;

                if (File.Exists(path))
                    isValid = true;

                if (!isValid)
                {
                    message = "No existe el archivo en la ruta " + path;
                    return lista;
                }
                var xlApp = new Application();
                var xlWorkbook = xlApp.Workbooks.Open(ruta);
                var xlWorksheet = xlWorkbook.Sheets[2] as _Worksheet;
                var xlRange = xlWorksheet?.UsedRange;
                var rowCount = xlRange?.Rows.Count;
                var colCount = xlRange?.Columns.Count;
                var tmpVendorId = string.Empty;
                isValid = false;

                for (var i = 3; i <= rowCount; i++)
                {
                    if (xlRange.Cells[i, NumberFromExcelColumn("A")] == null || xlRange.Cells[i, NumberFromExcelColumn("A")]?.Value2 == null) continue;
                    bool isNumeric = int.TryParse(Convert.ToString(xlRange.Cells[i, NumberFromExcelColumn("A")]?.Value2), out int employeeId);
                    if (isNumeric)
                    {
                        double holidayHour = Convert.ToDouble(xlRange.Cells[i, NumberFromExcelColumn("S")]?.Value2);
                        double noonHour = Convert.ToDouble(xlRange.Cells[i, NumberFromExcelColumn("T")]?.Value2);

                        if (holidayHour > 0 || noonHour > 0)
                        {
                            lista.Add(new Domain.ViewModels.Lookup()
                            {
                                Id = employeeId.ToString().PadLeft(6, '0'),
                                Descripción = _repository.ExecuteScalarQuery<string>($"SELECT RTRIM(FRSTNAME) + ' ' + RTRIM(LASTNAME) FROM {Helpers.InterCompanyId}.dbo.UPR00100 WHERE EMPLOYID = '{employeeId.ToString().PadLeft(6, '0')}'") ?? "",
                                DataPlus = holidayHour.ToString(),
                                DataExtended = noonHour.ToString()
                            });
                        }
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (xlRange != null) Marshal.ReleaseComObject(xlRange);
                if (xlWorksheet != null) Marshal.ReleaseComObject(xlWorksheet);
                xlWorkbook.Close(false, System.Reflection.Missing.Value, System.Reflection.Missing.Value);
                Marshal.ReleaseComObject(xlWorkbook);
                xlApp.Quit();
                Marshal.ReleaseComObject(xlApp);

                return lista;
            }
            catch (Exception e)
            {
                message = e.Message;
                return new List<Domain.ViewModels.Lookup>();
            }
        }

        #endregion

        #region Private Methods

        public static string NumberFromExcelColumn(string column)
        {
            return column;
            //var number = 0;
            //var pow = 1;
            //for (var i = column.Length - 1; i >= 0; i--)
            //{
            //    number += (column[i] - 'A' + 1) * pow;
            //    pow *= 26;
            //}
            //number = number - 1;
            //return number;
        }

        #endregion
    }
}