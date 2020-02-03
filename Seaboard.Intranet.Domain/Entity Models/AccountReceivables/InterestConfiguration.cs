using System;

namespace Seaboard.Intranet.Domain
{
    public class InterestConfiguration
    {
        public decimal ChargeRate { get; set; }
        public DateTime StartDateInvoice { get; set; }
        public string InterestItemCode { get; set; }
        public string RechargeItemCode { get; set; }
    }
}
