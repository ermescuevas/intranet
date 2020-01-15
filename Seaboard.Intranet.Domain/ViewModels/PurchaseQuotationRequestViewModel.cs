using System;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class PurchaseQuotationRequestViewModel
    {
        public string RequestId { get; set; }
        public string Description { get; set; }
        public DateTime DocumentDate { get; set; }
        public string Note { get; set; }
        public string Requester { get; set; }
        public string DepartmentId { get; set; }
        public string Status { get; set; }
        public string UserId { get; set; }
        public string ApproveUser { get; set; }
        public string PurchaseNote { get; set; }
        public int StatusInteger { get; set; }
        public DateTime ApproveDate { get; set; }

    }
}
