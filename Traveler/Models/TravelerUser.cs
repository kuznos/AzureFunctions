using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traveler.Models
{
	public class TravelerUnit
	{
		public string Name { get; set; }
		public string Country { get; set; }
		public bool IsNewClient { get; set; } = false;
		public decimal TotalTicketsGrossPrice { get; set; }
	}
	public class TravelerUser
   
	{
		public string id { get; } = Guid.NewGuid().ToString();
		public string Name { get; set; }
		public string Country { get; set; }
		public decimal TotalTicketsGrossPrice { get; set; }
		public bool IsNewClient { get; set; } = false;
		public Result result { get; set; }
	}
}
