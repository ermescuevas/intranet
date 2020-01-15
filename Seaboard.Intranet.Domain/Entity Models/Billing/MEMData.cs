using System.Collections.Generic;

namespace Seaboard.Intranet.Domain.Models
{
    public class MEMData
    {
        public string Créditor { get; set; }
        public string Debtor { get; set; }
        public string CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public bool Exclude { get; set; }
        public string Marker { get; set; }
        public string CurrencyId { get; set; }
        public string Note { get; set; }
        public string ReferenceInvoice { get; set; }
        public string ReferenceNCF { get; set; }
        public string DocumentType { get; set; }
        public decimal TotalApplyAmount { get; set; }
        public BillingType BillingType { get; set; }
        public List<MemDataDetail> Details { get; set; }
    }

    public class MemDataDetail
    {
        public string ProductName { get; set; }
        public string ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public decimal LineTotalAmount { get; set; }
        public bool Exclude { get; set; }
        public string CurrencyId { get; set; }
        public string ReferenceInvoice { get; set; }
        public string ReferenceNCF { get; set; }
    }

    public enum BillingType
    {
        MEM = 10,
        Miscellaneous = 20,
        Reliquidation = 30
    }

    public enum DocumentType
    {
        Invoice = 1,
        DebitNote = 3,
        CreditNote = 4
    }
}
