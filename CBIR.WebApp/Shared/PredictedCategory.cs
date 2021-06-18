using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBIR.WebApp.Shared
{
    public class PredictedCategory
    {
        public string Label { get; set; }
        public float Score { get; set; }
    }
}
