using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class AccountingConfiguration
    {
        public string PrefixInvoiceNumber { get; set; }
        public int InvoiceNextNumber { get; set; }
        public int InvoiceNumberLength { get; set; }
        public string SqlEmailProfile { get; set; }
        public string TaxRegistrationNumber { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyPhoneNumber { get; set; }
        public string CompanyFaxNumber { get; set; }
    }
}
