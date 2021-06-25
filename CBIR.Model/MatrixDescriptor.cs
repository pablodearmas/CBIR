using System;
using System.Collections.Generic;
using System.Text;

namespace CBIR.Model
{
    public class MatrixDescriptor
    {
        public int Rows { get; set; }
        
        public int Cols { get; set; }

        public int Depth { get; set; } //Emgu.CV.CvEnum.DepthType 

        public byte[] Data { get; set; }
    }
}
