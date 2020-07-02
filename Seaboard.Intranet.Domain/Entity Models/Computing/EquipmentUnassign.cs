using System;

namespace Seaboard.Intranet.Domain
{
    public class EquipmentUnassign
    {
        public string RequestId { get; set; }
        public string EmployeeId { get; set; }
        public DateTime DocumentDate  { get; set; }
        public string DeviceSimCard { get; set; }
        public string DeviceType { get; set; }
        public string Reason { get; set; }
        public string Note { get; set; }
    }
}
