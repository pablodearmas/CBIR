using System;
using System.Collections.Generic;

namespace CBIR.Model
{
    public class Image
    {
        public Guid Id { get; set; }

        public string Hash1 { get; set; } //Perceptual Hash

        public string Hash2 { get; set; } //Color Moment Hash

        public string ExternalFile { get; set; }

        public byte[] Data { get; set; }

        public virtual ICollection<Category> Categories { get; set; }

        public virtual ICollection<ImageDescriptor> Descriptors { get; set; }
    }
}
