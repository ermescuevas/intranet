using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class GpPurchaseReceipt
    {
        public string DocumentNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public string InvoiceId { get; set; }
        public string VendorId { get; set; }
        public string CurrencyId { get; set; }
        public string BatchNumber { get; set; }
        public string VendorDocumentNumber { get; set; }
        public List<GpPurchaseReceiptLine> Lines { get; set; }
    }
}
