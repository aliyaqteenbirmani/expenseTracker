using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.Entities
{
    public class Business
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; } = null;
        public string Description { get; set; } = null;
        public string FileName { get; set; } = null;
        public Guid UserId { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; } = null;
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
    }
}
