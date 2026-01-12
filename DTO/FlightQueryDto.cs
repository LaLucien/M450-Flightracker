using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightTracker.Contracts;


public class FlightQueryDto
{
    [Required]
    public string OriginIata { get; set; } = string.Empty;
    [Required]
    public string DestinationIata { get; set; } = string.Empty;
    [Range(0, int.MaxValue)]
    public int FlexibilityDays { get; set; } = 0;
    [Required]
    public DateTime DepartureDate { get; set; }
}
