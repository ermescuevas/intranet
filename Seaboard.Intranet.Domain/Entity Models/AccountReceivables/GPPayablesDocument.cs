using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class GpPayablesDocument
    {
        public string VendorId { get; set; }
        public string DocumentNumber { get; set; }
        public string VoucherNumber { get; set; }
        public string Description { get; set; }
        public decimal PurchaseAmount { get; set; }
        public decimal FreightAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal MiscellaneousAmount { get; set; }
        public decimal TradeDiscountAmount { get; set; }
        public string Currency { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime DueDate { get; set; }
        public string TaxDetail { get; set; }
        public string Note { get; set; }

    }
}
