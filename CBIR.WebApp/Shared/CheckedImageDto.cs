using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBIR.WebApp.Shared
{
    public class CheckedImageDto : ImageDto
    {
        public bool Checked { get; set; }
    }
}
