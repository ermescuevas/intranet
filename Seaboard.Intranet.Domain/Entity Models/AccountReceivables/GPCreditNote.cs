using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class GpCreditNote
    {
        public int CompanyId { get; set; }
        public string Codigo { get; set; }
        public string Cliente { get; set; }
        public string Descripción { get; set; }
        public string Lote { get; set; }
        public decimal Descuento { get; set; }
        public decimal Monto { get; set; }
        public string Ncf { get; set; }
        public DateTime Fecha { get; set; }
        public string Notas { get; set; }
        public string Moneda { get; set; }
        public DateTime DueDate { get; set; }
        public List<GpItem> Lines { get; set; }
        public string NcfRef { get; set; }
        public string InvoiceRef { get; set; }

        public static string GetNcfDescription(string id)
        {
            try
            {
                switch (id.Substring(9, 2))
                {
                    case "01":
                        return "Crédito Fiscal";
                    case "02":
                        return "Consumidor Final";
                    case "04":
                        return "Nota de crédito";
                    case "14":
                        return "Regimen Especial";
                    case "15":
                        return "Gubernamental";
                    default:
                        return "Consumidor Final";
                }
            }
            catch { return ""; }
        }
    }
}
