using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightTracker.Contracts;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace FlightTracker.Api.Services.Selenium
{
    public class GoogleFlightsSeleniumService : IFlightProvider
    {
        // Single public method that implements the interface
        public async Task<List<FlightDto>> FetchFlightsAsync(FlightQueryDto query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            // Keep method asynchronous even though scraping isn't implemented yet.
            await Task.Yield();

            // Return a safe stub list (one item) based on the provided query.
            var departure = query.DepartureDate != default ? query.DepartureDate : DateTime.Today.AddDays(7);

            var stub = new FlightDto
            {
                Origin = string.IsNullOrWhiteSpace(query.Origin) ? "ZRH" : query.Origin,
                Destination = string.IsNullOrWhiteSpace(query.Destination) ? "LHR" : query.Destination,
                DepartureDate = departure,
                Airline = "Stub Airline",
                Price = 199.99m
            };

            return new List<FlightDto> { stub };
        }

        // Create and configure the Chrome driver. Returned as IWebDriver.
        private IWebDriver CreateDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--window-size=1920,1080");

            return new ChromeDriver(options);
        }
    }
}
