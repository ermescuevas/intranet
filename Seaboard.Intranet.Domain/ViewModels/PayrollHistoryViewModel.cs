using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class PayrollHistoryViewModel
    {
        public string MonthDescription { get; set; }
        public string PayrollId { get; set; }
        public string PeriodId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
