using Seaboard.Intranet.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class AccountPayablesViewModel
    {
        public string VoucherNumber { get; set; }
        public string DocumentNumber { get; set; }
        public string VendorId { get; set; }
        public string VendName { get; set; }
        public DateTime DocumentDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public AccountPayablesModule Module { get; set; }
        public AccountPayablesType Type { get; set; }
    }
}
