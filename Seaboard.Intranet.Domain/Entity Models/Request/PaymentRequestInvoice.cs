using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class PaymentRequestInvoice
    {
        [Key]
        public int PaymentRequestInvoiceId { get; set; }
        public string DocumentNumber { get; set; }
        public decimal DocumentAmount { get; set; }
        public virtual string PaymentRequestId { get; set; }
        public virtual PaymentRequest PaymentRequest { get; set; }
    }
}
