using System;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class OpenOrdersView
    {
        public string OC_Numero { get; set; }
        public DateTime FECHA { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Itbis { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public string Moneda { get; set; }
        public string Id_Vendedor { get; set; }
        public string Vendedor { get; set; }
        public string Requisición { get; set; }
        public string Análisis { get; set; }
        public string Comentario { get; set; }
        public string DPTO { get; set; }
        public string CONCEPTO { get; set; }
        public string TIPO { get; set; }
    }
}
