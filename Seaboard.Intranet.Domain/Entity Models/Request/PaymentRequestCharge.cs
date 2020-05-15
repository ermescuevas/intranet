using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class PaymentRequestCharge
    {
        public int PaymentRequestChargeId { get; set; }
        public string Department { get; set; }
        public decimal ChargeAmount { get; set; }
        public virtual string PaymentRequestId { get; set; }
        public virtual PaymentRequest PaymentRequest { get; set; }
    }
}