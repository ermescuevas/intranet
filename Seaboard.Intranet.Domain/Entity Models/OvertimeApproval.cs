using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class OvertimeApproval
    {
        public string BatchNumber { get; set; }
        public string Description { get; set; }
        public string Note { get; set; }
        public int NumberOfTransactions { get; set; }
        public string Approver { get; set; }
        public string User { get; set; }
        public int Status { get; set; }
        public int RowId { get; set; }
    }
}
