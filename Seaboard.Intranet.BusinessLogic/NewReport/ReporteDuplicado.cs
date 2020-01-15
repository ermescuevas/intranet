using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Seaboard.Intranet.Domain.ViewModels;
using System.Collections.Generic;

namespace Seaboard.Intranet.BusinessLogic
{
    public class ReporteDuplicado : TneReport
    {
        public IEnumerable<ArConsultViewModel> Ar { get; set; }

        public ReporteDuplicado(IEnumerable<ArConsultViewModel> ar)
        {
            Pagina = false;
            this.Ar = ar;
        }

        public override void MontarCuerpoDatos()
        {
            base.MontarCuerpoDatos();

            #region enCabezado de reporte
            PdfPTable table = new PdfPTable(9);
            BaseColor preto = new BaseColor(0, 0, 0);
            BaseColor fundo = new BaseColor(200, 200, 200);
            Font font = FontFactory.GetFont("Verdana", 8, Font.NORMAL, preto);
            Font titulo = FontFactory.GetFont("Verdana", 8, Font.BOLD, preto);
            Font header = FontFactory.GetFont("Verdana", 8, Font.BOLD, BaseColor.BLUE);

            float[] colsW = { 14, 10, 12, 12, 14, 8, 10, 11 ,10};
            table.SetWidths(colsW);
            table.HeaderRows = 1;
            table.WidthPercentage = 100f;

            table.DefaultCell.Border = PdfPCell.BOTTOM_BORDER;
            table.DefaultCell.BorderColor = preto;
            table.DefaultCell.BorderColorBottom = new BaseColor(255, 255, 255);
            table.DefaultCell.Padding = 10;

            table.AddCell(GetNewCell("Requisiciones asociadas", titulo, Element.ALIGN_CENTER, 10, PdfPCell.BOTTOM_BORDER, preto, fundo));
            table.AddCell(GetNewCell("Número de AR", titulo, Element.ALIGN_CENTER, 10, PdfPCell.BOTTOM_BORDER, preto, fundo));
            table.AddCell(GetNewCell("Ordenes de compras asociada", titulo, Element.ALIGN_CENTER, 10, PdfPCell.BOTTOM_BORDER, preto, fundo));
            table.AddCell(GetNewCell("Descripción del AR", titulo, Element.ALIGN_CENTER, 10, PdfPCell.BOTTOM_BORDER, preto, fundo));
            table.AddCell(GetNewCell("Departamento", titulo, Element.ALIGN_CENTER, 10, PdfPCell.BOTTOM_BORDER, preto, fundo));
            table.AddCell(GetNewCell("Pago", titulo, Element.ALIGN_CENTER, 10, PdfPCell.BOTTOM_BORDER, preto, fundo));
            table.AddCell(GetNewCell("Moneda OC", titulo, Element.ALIGN_CENTER, 10, PdfPCell.BOTTOM_BORDER, preto, fundo));
            table.AddCell(GetNewCell("Monto solicitado", titulo, Element.ALIGN_CENTER, 10, PdfPCell.BOTTOM_BORDER, preto, fundo));
            table.AddCell(GetNewCell("Monto utilizado", titulo, Element.ALIGN_CENTER, 10, PdfPCell.BOTTOM_BORDER, preto, fundo));

            #endregion
            string project = "";
            string requisit = "";
            foreach (var item in Ar)
            {
                if (item.ProjectDesc != project)
                {
                    var cell = GetNewCell(item.ProjectDesc, header, Element.ALIGN_LEFT, 10, PdfPCell.BOTTOM_BORDER);
                    cell.Colspan = 9;
                    table.AddCell(cell);
                    project = item.ProjectDesc;
                    requisit = item.Requisition;
                }
                if (item.Requisition == requisit)
                {

                }
                table.AddCell(GetNewCell(item.Requisition.ToString(), font, Element.ALIGN_CENTER, 5, PdfPCell.BOTTOM_BORDER));
                table.AddCell(GetNewCell(item.ArNumber.ToString(), font, Element.ALIGN_CENTER, 5, PdfPCell.BOTTOM_BORDER));
                table.AddCell(GetNewCell(item.PurchaseOrder, font, Element.ALIGN_CENTER, 5, PdfPCell.BOTTOM_BORDER));
                table.AddCell(GetNewCell(item.ArDescription.ToString(), font, Element.ALIGN_CENTER, 5, PdfPCell.BOTTOM_BORDER));
                table.AddCell(GetNewCell(item.Department.ToString(), font, Element.ALIGN_CENTER, 5, PdfPCell.BOTTOM_BORDER));
                table.AddCell(GetNewCell(item.Payment.ToString(), font, Element.ALIGN_CENTER, 5, PdfPCell.BOTTOM_BORDER));
                table.AddCell(GetNewCell(item.Currency.ToString(), font, Element.ALIGN_CENTER, 5, PdfPCell.BOTTOM_BORDER));
                table.AddCell(GetNewCell(item.Amount.ToString(), font, Element.ALIGN_CENTER, 5, PdfPCell.BOTTOM_BORDER));
                table.AddCell(GetNewCell(item.UsedAmount.ToString(), font, Element.ALIGN_CENTER, 5, PdfPCell.BOTTOM_BORDER));
            }
            Doc.Add(table);
        }
    }
}
