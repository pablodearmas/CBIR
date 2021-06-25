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

}
