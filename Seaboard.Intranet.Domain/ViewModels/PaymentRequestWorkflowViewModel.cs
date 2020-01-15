using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class PaymentRequestWorkflowViewModel
    {
        public string PaymentNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public string Department { get; set; }
        public string Requester { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }
        public string Currency { get; set; }
        public string VendName { get; set; }
        public string PurchaseOrder { get; set; }

    }
}
