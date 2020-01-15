using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class Item
    {
        public string ItemId { get; set; }
        public string ItemDesc { get; set; }
        public string UnitId { get; set; }
        public string Warehouse { get; set; }
        public decimal Quantity { get; set; }
        public string ItemArea { get; set; }
    }
}
