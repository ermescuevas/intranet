using System;

namespace Seaboard.Intranet.Domain
{
    public class EquipmentRequest
    {
        public string RequestId { get; set; }
        public string RequestType { get; set; }
        public DateTime DocumentDate { get; set; }
        public bool HasData { get; set; }
        public bool OpenMinutes { get; set; }
        public string Note { get; set; }
        public string DepartmentId { get; set; }
        public string Requester { get; set; }
        public string RequestBy { get; set; }
        public int Status { get; set; }
    }
}
