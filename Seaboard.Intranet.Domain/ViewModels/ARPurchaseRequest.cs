using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class ArPurchaseRequest
    {
        public string RequestId { get; set; }
        public string PurchaseOrder { get; set; }
        public decimal Amount { get; set; }
        public DateTime PurchaseDocumentDate { get; set; }
        public DateTime RequestDocumentDate { get; set; }
    }
}
