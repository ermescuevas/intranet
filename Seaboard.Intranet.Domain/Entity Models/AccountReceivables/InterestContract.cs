namespace Seaboard.Intranet.Domain
{
    public class InterestContract
    {
        public string CustomerNumber { get; set; }
        public string CustomerName { get; set; }
        public decimal ChargePercent { get; set; }
        public int DaysDelay { get; set; }
        public bool IncludeWeekends { get; set; }
        public bool IncludeHolidays { get; set; }
        public int RowId { get; set; }
    }
}
