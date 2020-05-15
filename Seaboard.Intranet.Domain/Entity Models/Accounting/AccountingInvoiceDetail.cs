using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class AccountingInvoiceDetail
    {
        public string DocumentNumber { get; set; }
        public int LineNumber { get; set; }
        public string ItemNumber { get; set; }
        public string ItemDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }

    }
}
