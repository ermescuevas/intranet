using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class ArViewModel
    {
        public string ArNumber { get; set; }
        public string ArDescription { get; set; }
        public string AccountNum { get; set; }
        public string DepartmentId { get; set; }
        public decimal Amount { get; set; }
        public string ProjectId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public ICollection<ArPurchaseRequest> PurchaseList { get; set; }
    }
}
