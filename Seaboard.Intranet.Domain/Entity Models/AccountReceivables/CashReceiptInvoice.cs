using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class CashReceiptInvoice
    {
        public string DocumentNumber { get; set; }
        public string DocumentAmount { get; set; }
    }
}