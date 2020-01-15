using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class RequisitionLineViewModel
    {
        public string ItemId { get; set; }
        public string ItemDescription { get; set; }
        public string UnitId { get; set; }
        public decimal Quantity { get; set; }
        public string AccountNum { get; set; }
        public string Charge { get; set; }
        public string Warehouse { get; set; }
    }
}
