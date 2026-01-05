using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightTracker.Contracts
{
    public class FlightQueryDto
    {
        public string Origin { get; set; } = "";
        public string Destination { get; set; } = "";
        public DateTime DepartureDate { get; set; }
        public int FlexibilityDays { get; set; } = 0;
    }

}
