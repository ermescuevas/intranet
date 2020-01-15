using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class NcfDetailConfiguration
    {
        public string HeaderCode { get; set; }
        public int DetailCode { get; set; }
        public int FromNumber { get; set; }
        public int ToNumber { get; set; }
        public int NextNumber { get; set; }
        public int LeftNumber { get; set; }
        public int AlertNumber { get; set; }
        public DateTime DueDate { get; set; }
        public int Status { get; set; }
    }
}
