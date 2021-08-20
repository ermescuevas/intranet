using System;

namespace Seaboard.Intranet.Domain
{
    public class InterestDetail
    {
        public string BatchNumber { get; set; }
        public string CustomerId { get; set; }
        public string DocumentNumber { get; set; }
        public int DocumentType { get; set; }
        public decimal PreviousAmount { get; set; }
        public decimal AppliedAmount { get; set; }
        public decimal DocumentAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime CutDate { get; set; }
        public DateTime DueDate { get; set; }
        public int Days { get; set; }
        public decimal InterestRate { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal Charge { get; set; }
        public decimal ChargeAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool Exclude { get; set; }
    }
}
