namespace FlightTracker.Api.Models
{
    public class FlightPriceSnapshot
    {
        public int Id { get; set; }
        public string Origin { get; set; } = "";
        public string Destination { get; set; } = "";
        public decimal Price { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime CheckedAt { get; set; }
    }

}
