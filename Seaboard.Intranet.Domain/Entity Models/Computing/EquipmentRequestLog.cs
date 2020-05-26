using System;

namespace Seaboard.Intranet.Domain
{
    public class EquipmentRequestLog
    {
        public string RequestId { get; set; }
        public string UserId { get; set; }
        public string Note { get; set; }
        public DateTime LogDate { get; set; }
    }
}
