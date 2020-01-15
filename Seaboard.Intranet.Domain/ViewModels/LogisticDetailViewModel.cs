using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class LogisticDetailViewModel
    {
        public int LineId { get; set; }
        public string ItemId { get; set; }
        public string UnitId { get; set; }
        public decimal Quantity { get; set; }
        public int InventoryIndex { get; set; }
        public int RowId { get; set; }
        public string ItemDescription { get; set; }
        public string AccountIndex { get; set; }
        public string Charge { get; set; }
    }
}
