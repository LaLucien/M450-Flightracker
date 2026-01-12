using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FlightTracker.Contracts;

namespace FlightTracker.Web.Services
{
    public class FlightService
    {
        private readonly HttpClient _http;

        public FlightService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<List<FlightResponseDto>> GetLatestAsync(int count = 50)
        {
            var url = $"api/flights?count={count}";
            var flights = await _http.GetFromJsonAsync<List<FlightResponseDto>>(url).ConfigureAwait(false);
            return flights ?? new List<FlightResponseDto>();
        }
    }
}
