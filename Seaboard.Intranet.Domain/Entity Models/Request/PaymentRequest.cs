using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class PaymentRequest
    {
        [Key]
        public string PaymentRequestId { get; set; }
        public string Description { get; set; }
        public string Note { get; set; }
        public decimal Amount { get; set; }
        public string DepartmentId { get; set; }
        public string Priority { get; set; }
        public string PaymentCondition { get; set; }
        public DateTime DocumentDate { get; set; }
        public string Currency { get; set; }
        public string VendorId { get; set; }
        public string VendName { get; set; }
        public string Requester { get; set; }
        public decimal ApplyAmount { get; set; }
        public string BatchNumber { get; set; }
        public virtual ICollection<PaymentRequestCharge> PaymentRequestChargeLines { get; set; }
        public virtual ICollection<PaymentRequestInvoice> PaymentRequestInvoiceLines { get; set; }
        public virtual ICollection<PaymentRequestPurchaseOrder> PaymentRequestPurchaseOrders { get; set; }
    }
}
