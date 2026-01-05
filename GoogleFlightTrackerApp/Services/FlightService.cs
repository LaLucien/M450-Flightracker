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

        public async Task<List<FlightDto>> CollectAsync(FlightQueryDto query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var response = await _http.PostAsJsonAsync("api/flights/collect", query).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var flights = await response.Content.ReadFromJsonAsync<List<FlightDto>>().ConfigureAwait(false);
            return flights ?? new List<FlightDto>();
        }

        public async Task<List<FlightDto>> GetLatestAsync(int count = 50)
        {
            var url = $"api/flights/latest?count={count}";
            var flights = await _http.GetFromJsonAsync<List<FlightDto>>(url).ConfigureAwait(false);
            return flights ?? new List<FlightDto>();
        }
    }
}
