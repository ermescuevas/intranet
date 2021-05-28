using Seaboard.Intranet.Domain.Enums;
using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class AccountPayables
    {
        public AccountPayablesModule Module { get; set; }
        public AccountPayablesType Type { get; set; }
        public string VoucherNumber { get; set; }
        public string Description { get; set; }
        public string VendorId { get; set; }
        public string VendName { get; set; }
        public string DocumentNumber { get; set; }
        public string TaxDetailId { get; set; }
        public decimal PurchaseAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FreightAmount { get; set; }
        public decimal MiscellaneousAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string Currency { get; set; }
        public string Note { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime DueDate { get; set; }
        public int PostType { get; set; }
    }
}
