using System;
using System.Collections.Generic;

namespace Seaboard.Intranet.Domain.Models
{
    public class GPInvoice
    {
        public string BatchNumber { get; set; }
        public string SopNumber { get; set; }
        public string DocumentNumber { get; set; }
        public string CustomerId { get; set; }
        public string CurrencyId { get; set; }
        public decimal ExchangeRate { get; set; }
        public string ContactPerson { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Ncf { get; set; }
        public DateTime NcfDueDate { get; set; }
        public string NcfType { get; set; }
        public string Note { get; set; }
        public string ReferenceInvoice { get; set; }
        public string ReferenceNcf { get; set; }
        public List<GPInvoiceLine> Lines { get; set; }
    }

    public class GPInvoiceLine
    {
        public string SopNumber { get; set; }
        public int LineNumber { get; set; }
        public string ItemNumber { get; set; }
        public string ItemDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}
