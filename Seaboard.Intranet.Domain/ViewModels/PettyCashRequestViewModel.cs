using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class PettyCashRequestViewModel
    {
        public string RequestId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }
        public string Requester { get; set; }
        public string Department { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string UserId { get; set; }
        public DateTime DocumentDate { get; set; }
    }
}
