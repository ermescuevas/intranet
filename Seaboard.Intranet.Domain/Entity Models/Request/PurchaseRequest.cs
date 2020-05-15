using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Seaboard.Intranet.Domain.Models
{
    public class PurchaseRequest
    {
        public PurchaseRequest()
        {
            PurchaseRequestLines = new List<PurchaseRequestLine>();
        }

        [Key]
        public string PurchaseRequestId { get; set; }
        public string Description { get; set; }
        public string Note { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime RequiredDate { get; set; }
        public string Priority { get; set; }
        public string DepartmentId { get; set; }
        public string AR { get; set; }
        public string Approver { get; set; }
        public string Requester { get; set; }
        public string Interid { get; set; }

        public virtual ICollection<PurchaseRequestLine> PurchaseRequestLines { get; set; }
    }
}