using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class AbsenceRequest
    {
        public string RequestId { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Note { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int UnitDays { get; set; }
        public string AbsenceType { get; set; }
        public string DepartmentId { get; set; }
        public int AvailableDays { get; set; }
        public int Seniority { get; set; }
        public int Status { get; set; }
        public int RowId { get; set; }
    }
}
