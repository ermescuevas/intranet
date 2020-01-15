using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Seaboard.Intranet.Domain.Models
{
    public class PaymentVoucher
    {
        public string VoucherDate { get; set; }
        public string EmployeeId { get; set; }
        public string Name { get; set; }
        public string VoucherId { get; set; }
    }
}