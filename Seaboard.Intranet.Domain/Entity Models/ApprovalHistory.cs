using System;

namespace Seaboard.Intranet.Domain
{
    public class ApprovalHistory
    {
        public string Module { get; set; }
        public string Requester { get; set; }
        public string Request { get; set; }
        public string Approver { get; set; }
        public string DateApproved { get; set; }
        public string Status { get; set; }
    }
}
