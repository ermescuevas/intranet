using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class GpItem
    {
        public string Codigo { get; set; }
        public int Cantidad { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Descuento { get; set; }
        public decimal Precio { get; set; }
        public string UofM { get; set; }
        public string ListaPrecio { get; set; }
        public string Descripción { get; set; }

        public static string GetDescription(string id, DateTime fecha)
        {
            try
            {
                switch (id)
                {
                    case "ENERGIA":
                        return " Energía suministrada durante el mes de " + Helpers.DateParseDescription(fecha) + " " + fecha.Year;
                    case "POTENCIA":
                        return " Potencia suministrada durante el mes de " + Helpers.DateParseDescription(fecha) + " " + fecha.Year;
                    case "DC":
                        return " Transf. por derecho de conexión durante el mes de " + Helpers.DateParseDescription(fecha) + " " + fecha.Year;
                    case "SERVICIO":
                        return "Servicios S/E correspondientes al mes de " + Helpers.DateParseDescription(fecha) + " " + fecha.Year;
                    default:
                        return " Energía suministrada durante el mes de " + Helpers.DateParseDescription(fecha) + " " + fecha.Year;
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
