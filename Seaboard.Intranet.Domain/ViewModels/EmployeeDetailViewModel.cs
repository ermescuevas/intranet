using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class EmployeeDetailViewModel
    {
        public string EmployeeId { get; set; }
        public string PayrollTransaction { get; set; }
        public decimal PayrollAmount { get; set; }
        public int PayrollType { get; set; }
        public string PayrollId { get; set; }
    }
}
