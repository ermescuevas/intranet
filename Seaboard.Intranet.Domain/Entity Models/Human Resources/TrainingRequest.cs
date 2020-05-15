using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class TrainingRequest
    {
        public string RequestId { get; set; }
        public string Description { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public DateTime StartDate { get; set; }
        public string Duration { get; set; }
        public string Department { get; set; }
        public decimal Cost { get; set; }
        public string CurrencyId { get; set; }
        public string Location { get; set; }
        public string Supplier { get; set; }
        public string Objectives { get; set; }
        public string Requirements { get; set; }
        public string Participants { get; set; }
        public bool IsCompleted { get; set; }
        public int Status { get; set; }
        public int RowId { get; set; }
    }
}
