using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBIR.ImportImages
{
    public class RelevantImage
    {
        public string Category { get; set; }
        public string Filename { get; set; }
        public double Relevance { get; set; }
    }
}
