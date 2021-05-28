using System;

namespace Seaboard.Intranet.Domain
{
    public class NetProtocolMem
    {
        public string CustomerNumber { get; set; }
        public string CustomerExternalNumber { get; set; }
        public DateTime BillingMonth { get; set; }
        public DateTime DueDate { get; set; }
        public string ResolutionNumber { get; set; }
        public decimal InvoiceAmount { get; set; }
        public decimal DebtAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string InvoiceNumber { get; set; }
        public bool IsPreliminar { get; set; }
        public NetProtocolMemStatus Status { get; set; }
    }

    public enum NetProtocolMemStatus
    {
        CXC = 0,
        PAYMENT=1,
        NETEO=2,
        NETEO_CXC = 3,
        CXC_PAYMENT = 4,
        NETEO_PAYMENT = 5
    }
}