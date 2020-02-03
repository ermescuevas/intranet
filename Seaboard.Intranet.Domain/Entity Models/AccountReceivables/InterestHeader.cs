using System;

namespace Seaboard.Intranet.Domain
{
    public class InterestHeader
    {
        public string BatchNumber { get; set; }
        public string Description { get; set; }
        public DateTime CutDate { get; set; }
        public DateTime SearchDate { get; set; }
        public DateTime BillingMonth { get; set; }
        public DateTime DocumentDate { get; set; }
        public decimal InterestRate { get; set; }
        public decimal Charge { get; set; }
        public bool Preliminar { get; set; }
        public MarketType MarketType { get; set; }
        public bool Posted { get; set; }
        public int RowId { get; set; }
    }

    public enum MarketType
    {
        SPOT = 0,
        CONTRATO = 1
    }
}
