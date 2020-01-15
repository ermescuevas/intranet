using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class Ar
    {
        public string ArNumber { get; set; }
        public string ArDescription { get; set; }
        public string AccountNum { get; set; }
        public string DepartmentId { get; set; }
        public decimal Amount { get; set; }
        public Project Project { get; set; }
    }
}
