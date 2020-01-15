using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class PayrollBatchViewModel
    {
        [Key]
        public string BatchId { get; set; }
        public string EmployeeId { get; set; }
        public string TransactionType { get; set; }
        public string PayCode { get; set; }
        public string BeginningDate { get; set; }        
        public string EndingDate { get; set; }
        public string Units { get; set; }
        public string Note { get; set; }
        public string UserName { get; set; }
    }
}
