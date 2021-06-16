using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBIR.WebApp.Shared
{
    public enum ImageKeypointsDetector
    {
        Default = -1,
        //Detect Keypoints and Descriptors
        Brisk,
        Orb,
        Sift,
        //Only detect KeyPoints
        Fast,
        SimpleBlob
    }
}
