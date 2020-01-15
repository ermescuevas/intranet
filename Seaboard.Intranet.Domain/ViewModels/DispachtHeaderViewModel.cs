using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class DispachtHeaderViewModel
    {
        public string DispachtId { get; set; }
        public string RequestId { get; set; }
        public string Description { get; set; }
        public string DepartmentId { get; set; }
        public string WarehouseId { get; set; }
        public DateTime DocumentDate { get; set; }
        public string Note { get; set; }
        public string Requester { get; set; }
        public string Status { get; set; }

        public virtual ICollection<DispachtDetailViewModel> DispachtLines { get; set; }
    }
}
