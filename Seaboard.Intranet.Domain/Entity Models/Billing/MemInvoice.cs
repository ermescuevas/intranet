using Seaboard.Intranet.Domain.Models;
using System;
using System.Collections.Generic;

namespace Seaboard.Intranet.Domain
{
    public class MemInvoiceHead
    {
        public bool Exclude { get; set; }
        public string BatchNumber { get; set; }
        public string SopNumber { get; set; }
        public string Ncf { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime DocumentDate { get; set; }
        public string CustomerNumber { get; set; }
        public string CustomerName { get; set; }
        public string ContactPerson { get; set; }
        public string TaxRegistrationNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public string CurrencyId { get; set; }
        public decimal ExchangeRate { get; set; }
        public string Note { get; set; }
        public DateTime NcfDueDate { get; set; }
        public DocumentType DocumentType { get; set; }
        public string ReferenceInvoice { get; set; }
        public string ReferenceNcf { get; set; }
        public string NcfType { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<MemInvoiceDetail> Details { get; set; }
    }

    public class MemInvoiceDetail
    {
        public bool Exclude { get; set; }
        public string SopNumber { get; set; }
        public int LineNumber { get; set; }
        public string ItemNumber { get; set; }
        public string ItemDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
    }
}
