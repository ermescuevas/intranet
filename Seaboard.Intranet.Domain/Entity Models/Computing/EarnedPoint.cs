using System;

namespace Seaboard.Intranet.Domain
{
    public class EarnedPoint
    {
        public string Description { get; set; }
        public int Points { get; set; }
        public DateTime DocumentDate { get; set; }
        public int SummaryType { get; set; }
    }
}
