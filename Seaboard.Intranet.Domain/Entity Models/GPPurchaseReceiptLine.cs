using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class GpPurchaseReceiptLine
    {
        public string ItemId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public string ItemDescription { get; set; }
        public string Warehouse { get; set; }
        public string UnitId { get; set; }
    }
}
