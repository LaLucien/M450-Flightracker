using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightTracker.Contracts;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace FlightTracker.Api.Services.Selenium
{
    public class GoogleFlightsSeleniumService : IFlightScraper
    {
        
        public GoogleFlightsSeleniumService()
        {
        }



        private IWebDriver CreateDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--window-size=1920,1080");

            return new ChromeDriver(options);
        }

        public Task ScrapeFlights()
        {
            throw new NotImplementedException();
        }
    }
}
