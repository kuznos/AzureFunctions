using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traveler.Models
{
    public class Result
    {
        public int Code { get; set; }
        public string Description { get; set; } = String.Empty;
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public string RequestedBy { get; set; } = String.Empty;
    }
}
