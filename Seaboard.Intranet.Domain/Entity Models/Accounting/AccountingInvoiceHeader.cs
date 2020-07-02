using System;
using System.Collections.Generic;

namespace Seaboard.Intranet.Domain.Models
{
    public class AccountingInvoiceHeader
    {
        public string DocumentNumber { get; set; }
        public string SupplierNumber { get; set; }
        public string SupplierName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PhoneNumber { get; set; }
        public string Ncf { get; set; }
        public DateTime NcfDueDate { get; set; }
        public string NcfType { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime DueDate { get; set; }
        public string CurrencyId { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Note { get; set; }
        public int DocumentType { get; set; }
        public int ExpenseType { get; set; }
        public int Status { get; set; }

        public List<AccountingInvoiceDetail> Details { get; set; }
    }
}
