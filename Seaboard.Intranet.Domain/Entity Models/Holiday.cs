using System;

namespace Seaboard.Intranet.Domain
{
    public class Holiday
    {
        public string Description { get; set; }
        public DateTime HolidayDate { get; set; }
        public int HolidayYear { get; set; }
        public int RowId { get; set; }
    }
}
