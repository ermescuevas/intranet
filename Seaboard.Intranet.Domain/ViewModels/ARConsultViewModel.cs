using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class ArConsultViewModel
    {
        [Key]
        public string Requisition { get; set; }
        public string ArNumber { get; set; }
        public string ProjectDesc { get; set; }
        public string ArDescription { get; set; }
        public decimal Amount { get; set; }
        public string Payment { get; set; }
        public string Currency { get; set; }
        public decimal UsedAmount { get; set; }
        public string PurchaseOrder { get; set; }
        public string Department { get; set; }
    }
}
