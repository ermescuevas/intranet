using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class PettyCashRequest
    {
        public string RequestId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }
        public string Requester { get; set; }
        public string DepartmentId { get; set; }
        public string Currency { get; set; }
        public short Status { get; set; }
    }
}
