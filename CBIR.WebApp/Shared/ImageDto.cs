using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBIR.WebApp.Shared
{
    public class ImageDto
    {
        public bool HasRelevance { get; set; }
        public string Category { get; set; }
        public string ExternalFile { get; set; }
        public double Relevance { get; set; }
        public string RelevanceText { get; set; }
    }
}
