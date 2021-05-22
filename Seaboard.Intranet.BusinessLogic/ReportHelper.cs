using Seaboard.Intranet.Data;
using System.Data;
using System;

namespace Seaboard.Intranet.BusinessLogic
{
    public class ReportHelper
    {
        public static void Export(string rutaReporte, string rutaDestino, string reportDetail, int tipo, ref string xStatus, int printOption = 0, string subQuery = "")
        {
            var crystalReport = new CrystalDecisions.CrystalReports.Engine.ReportDocument();

            try
            {
                var reporte = "";
                switch (tipo)
                {
                    case 0:
                        reporte = "VOUPR30300R1.rpt";
                        break;
                    case 1:
                        reporte = "LPPOP10200R1.rpt";
                        break;
                    case 2:
                        reporte = "LPPOP10300R1.rpt";
                        break;
                    case 3:
                        reporte = "LPPOP30600R1.rpt";
                        break;
                    case 4:
                        reporte = "LPPOP10300R2.rpt";
                        break;
                    case 5:
                        reporte = "LLIF10200R1.rpt";
                        break;
                    case 6:
                        reporte = "LLIF10300R1.rpt";
                        break;
                    case 7:
                        reporte = "InventoryReport.rpt";
                        break;
                    case 8:
                        reporte = "DepartmentMovementReport.rpt";
                        break;
                    case 9:
                        reporte = "StockReport.rpt";
                        break;
                    case 10:
                        reporte = "ReorderReport.rpt";
                        break;
                    case 11:
                        reporte = "TRreport.rpt";
                        break;
                    case 12:
                        reporte = "AR000000Rpt.rpt";
                        break;
                    case 13:
                        reporte = "UPR000000Rpt.rpt";
                        break;
                    case 14:
                        reporte = "AgingAccountReceivablesSummary.rpt";
                        break;
                    case 15:
                        reporte = "AgingAccountReceivablesDetail.rpt";
                        break;
                    case 16:
                        reporte = "AccountReceivablesMemTransReport.rpt";
                        break;
                    case 17:
                        reporte = "AccountReceivablesTransAnalysisReport.rpt";
                        break;
                    case 18:
                        reporte = "AccountReceivablesRelationReport.rpt";
                        break;
                    case 19:
                        reporte = "NetTransReport.rpt";
                        break;
                    case 20:
                        reporte = "InvoiceMovementReport.rpt";
                        break;
                    case 21:
                        reporte = "UserPermissionReport.rpt";
                        break;
                    case 22:
                        reporte = "CashReceiptReport.rpt";
                        break;
                    case 23:
                        reporte = "LPPOP10400R1.rpt";
                        break;
                    case 24:
                        reporte = "SopDocumentReportNormal.rpt";
                        break;
                    case 25:
                        reporte = "SopDocumentReportEnglish.rpt";
                        break;
                    case 26:
                        reporte = "SopDocumentReportDuplicate.rpt";
                        break;
                    case 27:
                        reporte = "SopDocumentReportEnglishDuplicate.rpt";
                        break;
                    case 28:
                        reporte = "SopBatchReportNormal.rpt";
                        break;
                    case 29:
                        reporte = "SopBatchReportEnglish.rpt";
                        break;
                    case 30:
                        reporte = "CustomerDataReport.rpt";
                        break;
                    case 31:
                        reporte = "SalesTransDetailReport.rpt";
                        break;
                    case 32:
                        reporte = "SalesTransGeneralLedgerReport.rpt";
                        break;
                    case 33:
                        reporte = "SopDocumentReportWatermark.rpt";
                        break;
                    case 34:
                        reporte = "SopBatchReportWatermark.rpt";
                        break;
                    case 35:
                        reporte = "AbsenceRequestReport.rpt";
                        break;
                    case 36:
                        reporte = "ApprovalOvertimeReport.rpt";
                        break;
                    case 37:
                        reporte = "OvertimeReportSummary.rpt";
                        break;
                    case 38:
                        reporte = "OvertimeReportDetail.rpt";
                        break;
                    case 39:
                        reporte = "InterestSpotReport.rpt";
                        break;
                    case 40:
                        reporte = "InterestSummaryReport.rpt";
                        break;
                    case 41:
                        reporte = "TrainingRequestReport.rpt";
                        break;
                    case 42:
                        reporte = "EstimatedInterestReport.rpt";
                        break;
                    case 43:
                        reporte = "AccountReceivablesReport.rpt";
                        break;
                    case 44:
                        reporte = "EquipmentRequestReport.rpt";
                        break;
                    case 45:
                        reporte = "EquipmentDeliveryReport.rpt";
                        break;
                    case 46:
                        reporte = "EquipmentRepairReport.rpt";
                        break;
                    case 47:
                        reporte = "EquipmentUnassignReport.rpt";
                        break;
                    case 48:
                        reporte = "InformalSupplierInvoiceReport.rpt";
                        break;
                    case 49:
                        reporte = "MinorExpensesInvoiceReport.rpt";
                        break;
                    case 50:
                        reporte = "InformalSupplierTransDetailReport.rpt";
                        break;
                    case 51:
                        reporte = "MinorExpensesTransDetailReport.rpt";
                        break;
                    case 52:
                        reporte = "InformalSupplierTransSummaryReport.rpt";
                        break;
                    case 53:
                        reporte = "MinorExpensesTransSummaryReport.rpt";
                        break;
                    case 54:
                        reporte = "AgingAccountPayablesSummary.rpt";
                        break;
                    case 55:
                        reporte = "AgingAccountPayablesDetail.rpt";
                        break;
                    case 56:
                        reporte = "DigitalDocumentReport.rpt";
                        break;
                    case 57:
                        reporte = "DigitalDocumentByDepartmentReport.rpt";
                        break;
                    case 58:
                        reporte = "DigitalDocumentByDocumentReport.rpt";
                        break;
                    case 59:
                        reporte = "AccountPayablesReport.rpt";
                        break;
                    case 60:
                        reporte = "EmployeeDataReport.rpt";
                        break;
                    case 61:
                        reporte = "AccountReceivablesNetMulticurrency.rpt";
                        break;
                    case 62:
                        reporte = "AgingAccountReceivablesCustomSummary.rpt";
                        break;
                    case 63:
                        reporte = "CustomerStatementReport.rpt";
                        break;
                    case 64:
                        reporte = "CollectionsProtocolReport.rpt";
                        break;
                    case 65:
                        reporte = "CollectionsRelationSummaryReport.rpt";
                        break;
                }

                const string reportHeader = "SELECT '' CompName, '' Titulo, '' Parametro1, '' Parametro2, '' Parametro3, '' Parametro4, '' Parametro5, '' Parametro6, '' Usuario, 1 Marca";

                var cabecera = ConnectionDb.GetDt(reportHeader);
                var detalle = ConnectionDb.GetDt(reportDetail);

                if (detalle.Rows.Count == 0)
                {
                    xStatus = "No existen registros con los parametros suministrados para generar este reporte";
                    return;
                }

                crystalReport.Load(rutaReporte + @"\" + reporte);

                if (tipo == 0 || tipo == 11 || tipo == 12 || tipo == 13)
                    crystalReport.Database.Tables[0].SetDataSource(detalle);
                else
                {
                    crystalReport.Database.Tables[0].SetDataSource(cabecera);
                    crystalReport.Database.Tables[1].SetDataSource(detalle);
                }

                if (tipo == 43 || tipo == 59 || tipo == 62)
                    crystalReport.Subreports[0].SetDataSource(ConnectionDb.GetDt(subQuery));
                var type = CrystalDecisions.Shared.ExportFormatType.PortableDocFormat;
                if (printOption != 0)
                    type = CrystalDecisions.Shared.ExportFormatType.Excel;
                crystalReport.ExportToDisk(type, rutaDestino);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            finally
            {
                crystalReport.Dispose();
            }
        }

        public static void Export(string rutaReporte, string rutaDestino, DataTable detailReport, int tipo, ref string xStatus)
        {
            var crystalReport = new CrystalDecisions.CrystalReports.Engine.ReportDocument();

            try
            {
                var reporte = "";
                switch (tipo)
                {
                    case 0:
                        reporte = "ApprovalHistoryReport.rpt";
                        break;
                }

                const string reportHeader = "SELECT '' CompName, '' Titulo, '' Parametro1, '' Parametro2, '' Parametro3, '' Parametro4, '' Parametro5, '' Parametro6, '' Usuario, 1 Marca";

                var cabecera = ConnectionDb.GetDt(reportHeader);
                var detalle = detailReport;

                if (detalle.Rows.Count == 0)
                {
                    xStatus = "No existen registros con los parametros suministrados para generar este reporte";
                    return;
                }

                crystalReport.Load(rutaReporte + @"\" + reporte);
                crystalReport.Database.Tables[0].SetDataSource(cabecera);
                crystalReport.Database.Tables[1].SetDataSource(detalle);


                crystalReport.ExportToDisk(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat, rutaDestino);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            finally
            {
                crystalReport.Dispose();
            }
        }
    }
}