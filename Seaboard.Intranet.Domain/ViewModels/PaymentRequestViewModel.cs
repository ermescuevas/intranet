using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class PaymentRequestViewModel
    {
        public string PaymentRequestId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Priority { get; set; }
        public string VendorId { get; set; }
        public string VendName { get; set; }
        public string CurrencyId { get; set; }
        public DateTime DocumentDate { get; set; }
        public string Status { get; set; }
        public bool Voided { get; set; }
    }
}
