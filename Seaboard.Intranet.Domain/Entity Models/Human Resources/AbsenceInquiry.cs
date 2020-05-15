using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class AbsenceInquiry
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public DateTime StartDate { get; set; }
        public int AssignedDays { get; set; }
        public int DaysTaken { get; set; }
        public int LeftDays { get; set; }
        public string Department { get; set; }
        public int WorkYear { get; set; }
        public DateTime EndDate { get; set; }
        public string AbsenceType { get; set; }
        public string Comment { get; set; }
        public string Requester { get; set; }
        public int RowId { get; set; }
    }
}
