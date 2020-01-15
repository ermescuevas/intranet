using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class InventoryRequestDetail
    {
        public string ItemId { get; set; }
        public string ItemDescription { get; set; }
        public string ItemClass { get; set; }
        public string UnitId { get; set; }
        public decimal QtyOnHand { get; set; }
        public decimal MinStock { get; set; }
        public decimal MaxStock { get; set; }
        public decimal QtyRequest { get; set; }
        public string Status { get; set; }
        public string DataExtended { get; set; }
        public string DataPlus { get; set; }
    }
}
