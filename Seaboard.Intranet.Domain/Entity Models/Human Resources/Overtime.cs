using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class Overtime
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Note { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public decimal Hours { get; set; }
        public string OvertimeTypeDesc { get; set; }
        public int OvertimeType { get; set; }
        public string DepartmentId { get; set; }
        public int Status { get; set; }
        public int RowId { get; set; }
    }
}
