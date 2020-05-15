using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class AccountingConfiguration
    {
        public string VendorClassId { get; set; }
        public string PrefixInvoiceNumber { get; set; }
        public int InvoiceNextNumber { get; set; }
        public int InvoiceNumberLength { get; set; }
        public string TaxRegistrationNumber { get; set; }
    }
}
