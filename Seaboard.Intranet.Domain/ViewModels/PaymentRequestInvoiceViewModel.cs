using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class PaymentRequestInvoiceViewModel
    {
        public string DocumentNumber { get; set; }
        public decimal DocumentAmount { get; set; }
        public string DocumentDate { get; set; }
    }
}
