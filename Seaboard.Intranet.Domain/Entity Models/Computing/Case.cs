using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain
{
    public class Case
    {
        public string CaseNumber { get; set; }
        public string Description { get; set; }
        public string PhoneNumber { get; set; }
        public string EmployeeId { get; set; }
        public string Diagnostics { get; set; }
        public string Note { get; set; }
        public DateTime DocumentDate { get; set; }
        public int Status { get; set; }
    }
}
