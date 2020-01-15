using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class ItemRequest
    {
        public string Description { get; set; }
        public string Comment { get; set; }
        public string Requester { get; set; }
    }
}
