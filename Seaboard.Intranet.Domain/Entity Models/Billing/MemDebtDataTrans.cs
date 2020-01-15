using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class MemDebtDataTrans
    {
        public string Suplidor { get; set; }
        public double Monto { get; set; }
        public string NumeroProveedor { get; set; }
        public string Ncf { get; set; }
        public int TipoGastos { get; set; }
    }
}
