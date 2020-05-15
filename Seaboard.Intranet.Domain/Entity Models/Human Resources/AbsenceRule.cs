namespace Seaboard.Intranet.Domain.Models
{
    public class AbsenceRule
    {
        public int RowId { get; set; }
        public string Description { get; set; }
        public int FromYear { get; set; }
        public int ToYear { get; set; }
        public int UnitDays { get; set; }
        public int MinimunDays { get; set; }
        public AbsenceType AbsenceType { get; set; }
        public int ClassId { get; set; }
        public int Gender { get; set; }
        public int Fractions { get; set; }
        public bool IncludeWeekends { get; set; }
        public bool IncludeHolidays { get; set; }
    }
}
