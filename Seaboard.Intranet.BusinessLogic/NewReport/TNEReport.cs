using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Seaboard.Intranet.BusinessLogic
{
    public abstract class TneReport
    {
        protected Document Doc;
        PdfWriter _writer;
        MemoryStream _output;

        public string PageTitle { get; set; }
        public string PageSubTitle { get; set; }
        public string PageSubLogo { get; set; }
        public string BasePath { get; set; }
        public bool ImprimirEncabezadoEstandar { get; set; }
        public bool ImprimirPatronEstandar { get; set; }
        public bool Pagina { get; set; }

        public TneReport()
        {
            ImprimirEncabezadoEstandar = true;
            ImprimirPatronEstandar = true;
            PageTitle = string.Empty;
            PageSubTitle = string.Empty;
            BasePath = string.Empty;
            Pagina = false;
        }

        public MemoryStream GetOutput()
        {
            MontarCuerpoDatos();

            if (_output == null || _output.Length == 0)
            {
                throw new Exception("No hay datos para mostrar.");
            }

            try
            {
                _writer.Flush();

                if (_writer.PageEmpty)
                {
                    Doc.Add(new Paragraph("Ningún registro para listar."));
                }

                Doc.Close();
            }
            catch { }
            finally
            {
                Doc = null;
                _writer = null;
            }

            return _output;
        }

        public virtual void MontarCuerpoDatos()
        {
            if (!Pagina)
            {
                Doc = new Document(PageSize.A4, 20, 20, 80, 40);
            }
            else
            {
                Doc = new Document(PageSize.A4.Rotate(), 20, 20, 80, 80);
            }
            _output = new MemoryStream();
            _writer = PdfWriter.GetInstance(Doc, _output);

            Doc.AddAuthor("TNE - Treta Never Ends");
            Doc.AddTitle(PageTitle);
            Doc.AddSubject(PageTitle);

            var footer = new MspdfFooter();
            footer.PageTitle = PageTitle;
            footer.PageSubTitle = PageSubTitle;
            footer.BasePath = BasePath;
            footer.ImprimirEncabezadoEstandar = ImprimirEncabezadoEstandar;
            footer.ImprimirPatronEstandar = ImprimirPatronEstandar;

            _writer.PageEvent = footer;

            Doc.Open();

            return;
        }

        protected PdfPCell GetNewCell(string texto, Font fuente, int alineacion, float espacio, int borde, BaseColor corBorde, BaseColor corFondo)
        {
            var cell = new PdfPCell(new Phrase(texto, fuente));
            cell.HorizontalAlignment = alineacion;
            cell.Padding = espacio;
            cell.Border = borde;
            cell.BorderColor = corBorde;
            cell.BackgroundColor = corFondo;

            return cell;
        }

        protected PdfPCell GetNewCell(string texto, Font fuente, int alineacion, float espacio, int borde, BaseColor corBorde)
        {
            return GetNewCell(texto, fuente, alineacion, espacio, borde, corBorde, new BaseColor(255, 255, 255));
        }
        protected PdfPCell GetNewCell(string texto, Font fuente, int alineacion = 0, float espacio = 5, int borde = 0)
        {
            return GetNewCell(texto, fuente, alineacion, espacio, borde, new BaseColor(0, 0, 0), new BaseColor(255, 255, 255));
        }
    }
}