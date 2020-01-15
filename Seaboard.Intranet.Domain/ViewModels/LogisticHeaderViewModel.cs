using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class LogisticHeaderViewModel
    {
        public string RequestId { get; set; }
        public string DocumentId { get; set; }
        public string DocumentDate { get; set; }
        public string RequiredDate { get; set; }
        public string Ar { get; set; }
        public string Description { get; set; }
        public string Note { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentDesc { get; set; }
        public string Priority { get; set; }
        public string WareHouse { get; set; }
        public string WorkNumber { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int RowId { get; set; }
        public string Aprover1 { get; set; }
        public string Aprover2 { get; set; }
    }
}
