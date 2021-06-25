using System;
using System.Collections.Generic;
using System.Text;

namespace CBIR.Model
{
    public enum ImageDescriptorType
    {
        Default = -1,
        //Detect Keypoints and Descriptors
        Brisk,
        Orb,
        Sift,
        //Only detect Descriptors
        Brief,
        Latch,
        Lucid,
        Freak,
        //Only detect KeyPoints
        Fast,
        SimpleBlob
    }

    public class ImageDescriptor
    {
        public Guid Id { get; set; }

        public Guid ImageId { get; set; }

        public int Rows { get; set; }
        
        public int Cols { get; set; }

        public int Depth { get; set; } //Emgu.CV.CvEnum.DepthType 

        public ImageDescriptorType Type { get; set; }

        public byte[] Data { get; set; }

        public virtual Image Image { get; set; }
    }
}
