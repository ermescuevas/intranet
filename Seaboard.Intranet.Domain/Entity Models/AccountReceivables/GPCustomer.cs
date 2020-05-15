using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class GPCustomer
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string ShortName { get; set; }
        public int MyProperty { get; set; }
        public decimal Amount { get; set; }
        public DateTime DocumentDate { get; set; }
        public string DocumentNumber { get; set; }
        public string BatchNumber { get; set; }
        public CashReceiptType Type { get; set; }
        public string Description { get; set; }
    }
}
