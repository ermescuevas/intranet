using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Seaboard.Intranet.Domain.Models
{
    public class PurchaseRequestLine
    {
        public int PurchaseRequestLineId { get; set; }
        public string ItemId { get; set; }
        public string ItemDescription { get; set; }
        public string UnitId { get; set; }
        public decimal Quantity { get; set; }
        public string Warehouse { get; set; }
        public string AccountNum { get; set; }
        public string Charge { get; set; }
        public string PurchaseRequestId { get; set; }
        public virtual PurchaseRequest PurchaseRequest { get; set; }
    }
}