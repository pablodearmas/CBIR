using System;
using System.Collections.Generic;
using System.Text;

namespace CBIR.Model
{
    public class ImageDescriptor : MatrixDescriptor
    {
        public Guid Id { get; set; }

        public Guid ImageId { get; set; }

        public ImageDescriptorType Type { get; set; }

        public virtual Image Image { get; set; }
    }
}
