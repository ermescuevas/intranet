using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class DispachtDetailViewModel
    {
        public string DispachtId { get; set; }
        public string RequestId { get; set; }
        public int LineId { get; set; }
        public string ItemNumber { get; set; }
        public string ItemDescription { get; set; }
        public string BaseUnitId { get; set; }
        public string UnitId { get; set; }
        public string DepartmentId { get; set; }
        public string WarehouseId { get; set; }
        public decimal QtyDispachted { get; set; }
        public decimal QtyOrder { get; set; }
        public decimal QtyPending { get; set; }
        public decimal QtyOnHand { get; set; }
        public string Status { get; set; }
    }
}
