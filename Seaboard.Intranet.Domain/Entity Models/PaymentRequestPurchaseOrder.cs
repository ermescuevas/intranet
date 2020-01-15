using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class PaymentRequestPurchaseOrder
    {
        public int PaymentRequestPurchaseOrderId { get; set; }
        public string PaymentPurchaseOrder { get; set; }
        public virtual string PaymentRequestId { get; set; }
        public virtual PaymentRequest PaymentRequest { get; set; }
    }
}
