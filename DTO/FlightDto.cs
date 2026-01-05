namespace FlightTracker.Contracts
{
    public class FlightDto
    {
        public int Id { get; set; }
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public decimal Price { get; set; }
        public string Airline { get; set; } = string.Empty;
    }
}
