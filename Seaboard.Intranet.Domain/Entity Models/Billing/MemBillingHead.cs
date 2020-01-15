using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class MemBillingHead
    {
        public DateTime BillingMonth { get; set; }
        public string BatchNumber { get; set; }
        public string BatchDescription { get; set; }
        public string ResolutionNumber { get; set; }
        public string Note { get; set; }
        public string NoteSecondary { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime DueDate { get; set; }
        public string FilePath { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalForeignAmount { get; set; }
        public int NumberOfTransactions { get; set; }
        public int Year { get; set; }
        public string UserId { get; set; }
        public decimal ExchangeRate { get; set; }
        public string RangeCorresponding { get; set; }
        public BillingType BillingType { get; set; }
    }
}
