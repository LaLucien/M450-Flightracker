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

        public async Task<List<FlightResponseDto>> GetFlightsAsync(string? origin = null, string? destination = null, string? departureDate = null, string? flightNumber = null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(origin)) parts.Add($"origin={Uri.EscapeDataString(origin)}");
            if (!string.IsNullOrWhiteSpace(destination)) parts.Add($"destination={Uri.EscapeDataString(destination)}");
            if (!string.IsNullOrWhiteSpace(departureDate)) parts.Add($"departure_date={Uri.EscapeDataString(departureDate)}");
            if (!string.IsNullOrWhiteSpace(flightNumber)) parts.Add($"flight_number={Uri.EscapeDataString(flightNumber)}");

            var url = "api/flights" + (parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty);
            var flights = await _http.GetFromJsonAsync<List<FlightResponseDto>>(url).ConfigureAwait(false);
            return flights ?? new List<FlightResponseDto>();
        }

        public async Task<FlightResponseDto?> GetFlightByIdAsync(string id)
        {
            try
            {
                return await _http.GetFromJsonAsync<FlightResponseDto>($"api/flights/{Uri.EscapeDataString(id)}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<ObservationResponseDto>> GetObservationsAsync(string flightId, string? from = null, string? to = null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(from)) parts.Add($"from={Uri.EscapeDataString(from)}");
            if (!string.IsNullOrWhiteSpace(to)) parts.Add($"to={Uri.EscapeDataString(to)}");
            var url = $"api/flights/{Uri.EscapeDataString(flightId)}/observations" + (parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty);
            var obs = await _http.GetFromJsonAsync<List<ObservationResponseDto>>(url).ConfigureAwait(false);
            return obs ?? new List<ObservationResponseDto>();
        }

        public async Task<WeekdayStatsResponseDto?> GetWeekdayStatsAsync(string flightId)
        {
            try
            {
                return await _http.GetFromJsonAsync<WeekdayStatsResponseDto>($"api/flights/{Uri.EscapeDataString(flightId)}/stats/weekday");
            }
            catch
            {
                return null;
            }
        }

        public async Task<BookingDateStatsResponseDto?> GetBookingDateStatsAsync(string flightId, string? from = null, string? to = null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(from)) parts.Add($"from={Uri.EscapeDataString(from)}");
            if (!string.IsNullOrWhiteSpace(to)) parts.Add($"to={Uri.EscapeDataString(to)}");
            var url = $"api/flights/{Uri.EscapeDataString(flightId)}/stats/booking-date" + (parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty);
            try
            {
                return await _http.GetFromJsonAsync<BookingDateStatsResponseDto>(url);
            }
            catch
            {
                return null;
            }
        }

        public async Task<DaysToDepartureStatsResponseDto?> GetDaysToDepartureStatsAsync(string flightId, int bucket = 1)
        {
            try
            {
                return await _http.GetFromJsonAsync<DaysToDepartureStatsResponseDto>($"api/flights/{Uri.EscapeDataString(flightId)}/stats/days-to-departure?bucket={bucket}");
            }
            catch
            {
                return null;
            }
        }

        public async Task AddFlightToWatchlistAsync(FlightQueryDto queryDto)
        {
            await _http.PostAsJsonAsync("api/flights/query", queryDto).ConfigureAwait(false);
        }

        public async Task<List<QueryResponseDto>> GetQueriesAsync()
        {
            try
            {
                var list = await _http.GetFromJsonAsync<List<QueryResponseDto>>("api/flights/queries");
                return list ?? new List<QueryResponseDto>();
            }
            catch
            {
                return new List<QueryResponseDto>();
            }
        }

        public async Task<bool> DeleteQueryAsync(string id)
        {
            try
            {
                var resp = await _http.DeleteAsync($"api/flights/queries/{Uri.EscapeDataString(id)}");
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
