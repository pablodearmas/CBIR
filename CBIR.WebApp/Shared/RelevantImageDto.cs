using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBIR.WebApp.Shared
{
    public class RelevantImageDto : CategorizedImageDto
    {
        public bool HasRelevance { get; set; }
        public double Relevance { get; set; }
        public string RelevanceText { get; set; }
    }
}
