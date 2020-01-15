using System.Collections.Generic;

namespace Seaboard.Intranet.Domain.Models
{
    public class Project
    {
        public Project()
        {
            ProjectLines = new List<Ar>();
        }
        public string ProjectId { get; set; }
        public string ProjectDesc { get; set; }
        public string AccountNum { get; set; }
        public virtual ICollection<Ar> ProjectLines { get; set; }
    }
}
