using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class ItemRequestViewModel
    {
        public string RequestId { get; set; }
        public string ItemDescription { get; set; }
        public string UnitId { get; set; }
        public string ItemType { get; set; }
        public decimal CurrentCost { get; set; }
        public string Comment { get; set; }
        public string ClassId { get; set; }
        public string ItemArea { get; set; }
    }
}
