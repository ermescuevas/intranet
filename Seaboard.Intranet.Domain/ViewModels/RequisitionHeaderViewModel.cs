using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class RequisitionHeaderViewModel
    {
        public string PopRequisitionNumber { get; set; }
        public string RequisitionDescription { get; set; }
        public string FechaDocumento { get; set; }
        public string FechaRequerida { get; set; }
        public string Reqstdby { get; set; }
        public string Prioridad { get; set; }
        public string Aprobador { get; set; }
        public string Nota { get; set; }
        public string Aprobador2 { get; set; }
        public string Ar { get; set; }
        public string Departamento { get; set; }
    }
}
