using System;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class InterestSummaryViewModel
    {
        public DateTime BillingMonth { get; set; }
        public string CustomerId { get; set; }
        public string DocumentNumber { get; set; }
        public decimal DocumentAmount { get; set; }
        public DateTime DocumentDate { get; set; }
        public string PaymentDocumentNumber { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CutDate { get; set; }
        public int Days { get; set; }
        public decimal InterestRate { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal Charge { get; set; }
        public decimal ChargeAmount { get; set; }
        public decimal TotalInterestAmount { get; set; }
        public string DateDescription
        {
            get
            {
                switch (BillingMonth.Month)
                {
                    case 1:
                        return "Jan - " + BillingMonth.Year;
                    case 2:
                        return "Feb - " + BillingMonth.Year;
                    case 3:
                        return "Mar - " + BillingMonth.Year;
                    case 4:
                        return "Apr - " + BillingMonth.Year;
                    case 5:
                        return "May - " + BillingMonth.Year;
                    case 6:
                        return "Jun - " + BillingMonth.Year;
                    case 7:
                        return "Jul - " + BillingMonth.Year;
                    case 8:
                        return "Aug - " + BillingMonth.Year;
                    case 9:
                        return "Sep - " + BillingMonth.Year;
                    case 10:
                        return "Oct - " + BillingMonth.Year;
                    case 11:
                        return "Nov - " + BillingMonth.Year;
                    case 12:
                        return "Dec - " + BillingMonth.Year;
                    default:
                        return "Jan - " + BillingMonth.Year;
                }
            }
        }
    }
}