using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class HumanResourcesOvertime
    {
        public string BatchNumber { get; set; }
        public string Description { get; set; }
        public string Note { get; set; }
        public DateTime PayrollDate { get; set; }
        public int NumberOfTransactions { get; set; }
        public int Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public int RowId { get; set; }
    }
}
