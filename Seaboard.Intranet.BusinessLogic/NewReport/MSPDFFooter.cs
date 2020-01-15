using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Seaboard.Intranet.BusinessLogic
{
    public class MspdfFooter : PdfPageEventHelper
    {
        public string PageTitle { get; set; }
        public string PageSubTitle { get; set; }
        public string PageSubLogo { get; set; }
        public string BasePath { get; set; }
        public bool ImprimirEncabezadoEstandar { get; set; }
        public bool ImprimirPatronEstandar { get; set; }

        public override void OnOpenDocument(PdfWriter writer, Document doc)
        {
            base.OnOpenDocument(writer, doc);
        }

        public override void OnStartPage(PdfWriter writer, Document doc)
        {
            base.OnStartPage(writer, doc);

            ImprimeCabezado(writer, doc);
        }

        public override void OnEndPage(PdfWriter writer, Document doc)
        {
            base.OnEndPage(writer, doc);

            //ImprimePieDepagina(writer, doc);
        }

        #region Pie de pagina
        /// <summary>
        /// Este metodo crea el pie de pagina
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="doc"></param>
        /*
        private void ImprimePieDepagina(PdfWriter writer, Document doc)
        {
            #region Datos del pie de página
            if (ImprimirPatronEstandar)
            {
                BaseColor preto = new BaseColor(0, 0, 0);
                Font font = FontFactory.GetFont("Verdana", 8, Font.NORMAL, preto);
                Font negrito = FontFactory.GetFont("Verdana", 8, Font.BOLD, preto);
                float[] sizes = new float[] { 0f, 1.5f, 1f };

                PdfPTable table = new PdfPTable(3);
                table.TotalWidth = doc.PageSize.Width - (doc.LeftMargin + doc.RightMargin);
                table.SetWidths(sizes);

                #region Columna TNE

                PdfPCell cell = new PdfPCell();
                cell.HorizontalAlignment = Element.ALIGN_LEFT;
                cell.Border = 0;
                cell.BorderWidthTop = 0f;
                cell.PaddingLeft = 0f;
                cell.PaddingTop = 0f;
                table.AddCell(cell);

                PdfPTable micros = new PdfPTable(1);
                cell = new PdfPCell(new Phrase("SEABOARD", negrito));
                cell.Border = 0;
                micros.AddCell(cell);
                cell = new PdfPCell(new Phrase("Transcontinental Capital Corporation", font));
                cell.Border = 0;
                micros.AddCell(cell);
                cell = new PdfPCell(new Phrase("http://seaboardpower.com/", font));
                cell.Border = 0;
                micros.AddCell(cell);

                cell = new PdfPCell(micros);
                cell.HorizontalAlignment = Element.ALIGN_LEFT;
                cell.Border = 0;
                cell.BorderWidthTop = 1.5f;
                cell.PaddingTop = 10f;
                table.AddCell(cell);
                #endregion

                #region Página
                micros = new PdfPTable(1);
                cell = new PdfPCell(new Phrase(DateTime.Today.ToString("dd/MM/yyyy"), font));
                cell.Border = 0;
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                micros.AddCell(cell);
                cell = new PdfPCell(new Phrase(DateTime.Now.ToString("HH:mm:ss"), font));
                cell.Border = 0;
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                micros.AddCell(cell);

                cell = new PdfPCell(micros);
                cell.HorizontalAlignment = Element.ALIGN_LEFT;
                cell.Border = 0;
                cell.BorderWidthTop = 1.5f;
                cell.PaddingTop = 10f;
                table.AddCell(cell);
                #endregion

                table.WriteSelectedRows(0, -1, doc.LeftMargin, 70, writer.DirectContent);
            }
            #endregion 
        }
        */
        #endregion

        private void ImprimeCabezado(PdfWriter writer, Document doc)
        {
            #region Datos del encabezado
            if (ImprimirEncabezadoEstandar)
            {
                BaseColor preto = new BaseColor(0, 0, 0);
                Font font = FontFactory.GetFont("Verdana", 8, Font.NORMAL, preto);
                Font titulo = FontFactory.GetFont("Verdana", 12, Font.BOLD, preto);
                float[] sizes = new float[] { 1f, 3f, 1f };

                PdfPTable table = new PdfPTable(3);
                table.TotalWidth = doc.PageSize.Width - (doc.LeftMargin + doc.RightMargin);
                table.SetWidths(sizes);

                #region Logo Empresa
                Image foot;
                if (File.Exists(BasePath + @"\PublicResources\" + PageSubLogo))
                {
                    foot = Image.GetInstance(BasePath + @"\PublicResources\" + PageSubLogo);
                }
                else
                {
                    foot = Image.GetInstance(BasePath + @"\Images\Seaboard.png");
                }
                foot.ScalePercent(60);

                PdfPCell cell = new PdfPCell(foot);
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.Border = 0;
                cell.BorderWidthTop = 1.5f;
                cell.BorderWidthBottom = 1.5f;
                cell.PaddingTop = 10f;
                cell.PaddingBottom = 10f;
                table.AddCell(cell);

                PdfPTable micros = new PdfPTable(1);
                cell = new PdfPCell(new Phrase(PageSubTitle, font));
                cell.Border = 0;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                micros.AddCell(cell);
                cell = new PdfPCell(new Phrase(PageTitle, titulo));
                cell.Border = 0;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                micros.AddCell(cell);

                cell = new PdfPCell(micros);
                cell.HorizontalAlignment = Element.ALIGN_LEFT;
                cell.Border = 0;
                cell.BorderWidthTop = 1.5f;
                cell.BorderWidthBottom = 1.5f;
                cell.PaddingTop = 10f;
                table.AddCell(cell);
                #endregion

                #region Página
                micros = new PdfPTable(1);
                cell = new PdfPCell(new Phrase("Página: " + (doc.PageNumber).ToString(), font));
                cell.Border = 0;
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                micros.AddCell(cell);

                cell = new PdfPCell(micros);
                cell.HorizontalAlignment = Element.ALIGN_LEFT;
                cell.Border = 0;
                cell.BorderWidthTop = 1.5f;
                cell.BorderWidthBottom = 1.5f;
                cell.PaddingTop = 10f;
                table.AddCell(cell);
                #endregion

                table.WriteSelectedRows(0, -1, doc.LeftMargin, (doc.PageSize.Height - 10), writer.DirectContent);
            }
            #endregion
        }

        public override void OnCloseDocument(PdfWriter writer, Document document)
        {
            base.OnCloseDocument(writer, document);
        }
    }
}