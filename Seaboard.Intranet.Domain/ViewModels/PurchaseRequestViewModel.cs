using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class PurchaseRequestViewModel
    {
        public string RequestId { get; set; }
        public string Priority { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime RequiredDate { get; set; }
        public string Description { get; set; }
        public string QuoteId { get; set; }
        public string Status { get; set; }
        public string WorkNumber { get; set; }
        public string Ar { get; set; }
        public string Requester { get; set; }
        public string OriginalRequest { get; set; }
    }
}
