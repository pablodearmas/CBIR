using System;
using System.Collections.Generic;
using System.Text;

namespace CBIR.Model
{
    public class Category
    {
        public Guid Id { get; set; }

        public Guid? ParentId { get; set; }
        
        public string Name { get; set; }

        public virtual Category Parent { get; set; }

        public virtual ICollection<Image> Images { get; set; }

    }
}
