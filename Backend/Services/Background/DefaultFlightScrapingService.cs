using AngleSharp.Text;
using FlightTracker.Api.Models;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Storage.Repositories;
using OpenQA.Selenium;
using OpenQA.Selenium.BiDi.Input;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System.Diagnostics.Metrics;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace FlightTracker.Api.Services.Background;

/// <summary>
/// Default implementation that scrapes predefined flight routes
/// </summary>
public class DefaultFlightScrapingService : IFlightScrapingService
{
    private IWebDriver _driver;
    private readonly IQueryRepository _queryRepository;
    private readonly IObservationRepository _observationRepository;
    private readonly IFlightRepository _flightRepository;


    public DefaultFlightScrapingService(IQueryRepository queryRepository, IObservationRepository observationRepository, IFlightRepository flightRepository, IConfiguration configuration)
    {
        _queryRepository = queryRepository;
        _observationRepository = observationRepository;
        _flightRepository = flightRepository;

        new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);

        var options = new ChromeOptions();
        if (configuration["TESTRUNNER"] == "roche-pc")
        {
            options.BinaryLocation = configuration["CHROME_EXE"];
        }

        options.AddArgument("--headless");
        _driver = new ChromeDriver(options);



    }

    ~DefaultFlightScrapingService()
    {
        _driver.Quit();
        _driver.Dispose();
    }

    public async Task ScrapeFlightsAsync(CancellationToken cancellationToken = default)
    {
        EnsureGoogleFlightCookiesAccepted();
        var queries = _queryRepository.GetAll();
        var tasks = new List<Task>();
        foreach (var query in queries)
        {
            await Scrape(query, cancellationToken);
        }



    }

    public void EnsureGoogleFlightCookiesAccepted()
    {
        _driver.Navigate().GoToUrl("https://www.google.com/travel/flights?tfs=CBwQARoOagwIAhIIL20vMDg5NjZAAUgBcAGCAQsI____________AZgBAg&tfu=KgIIAw");
        var acceptCokieBanner = _driver.FindElement(By.XPath("//button[@aria-label=\"Accept all\"]"));
        acceptCokieBanner?.Click();
        Thread.Sleep(200);
        
    }

    public async Task Scrape(QueryEntity query, CancellationToken cancellationToken)
    {
        // Input isnt necessarily trustworthy, clamp flexibility days to reasonable range also ensure that date is in the future
        for (int flexDay = Math.Max(-query.FlexibilityDays, -10); flexDay <= Math.Min(query.FlexibilityDays, 10); ++flexDay)
        {
            
            if(query.AnchorDate < DateTime.UtcNow.Date.AddDays(-10) || query.AnchorDate.AddDays(flexDay) < DateTime.UtcNow.Date)
            {
                continue;
            }
            var departureDate = query.AnchorDate.AddDays(flexDay);

            // One Way Selected URL
            _driver.Navigate().GoToUrl("https://www.google.com/travel/flights?tfs=CBwQARoOagwIAhIIL20vMDg5NjZAAUgBcAGCAQsI____________AZgBAg&tfu=KgIIAw");
            EnterOrigin(query.OriginIata);
            EnterDestination(query.DestinationIata);
            EnterDeparture(departureDate);
            Explore();
            Thread.Sleep(500);

            // Scrape top 10 results
            var flightCards = _driver.FindElements(By.XPath("//div[contains(@aria-label, \"Select flight\")]/parent::*/parent::li"));
            foreach (var card in flightCards.Take(10))
            {
                ObservationEntity observation = new()
                {
                    ObservedAtUtc = DateTime.UtcNow
                };

                FlightEntity flight = new()
                {
                    OriginIata = query.OriginIata,
                    DepartureDate = departureDate,
                    DestinationIata = query.DestinationIata,

                };
                //price extraction
                var priceSpan = card.FindElement(By.XPath(".//span[contains(@aria-label, \"Swiss francs\")]"));
                var priceText = priceSpan.GetAttribute("aria-label");
                var priceAsString = priceText?.Split(" ")[0];
                Decimal.TryParse(priceAsString, out decimal price);
                observation.PriceChf = price;

                //Flightnumer extraction
                var CO2DivLink = card.FindElement(By.XPath(".//div[@data-travelimpactmodelwebsiteurl]")).GetAttribute("data-travelimpactmodelwebsiteurl");
                var itenerary = CO2DivLink?.Split("=")[1];
                var legs = itenerary?.SplitCommas();
                var flightNumbers = legs?.Select(l => ExtractNumber(l)).ToList();
                flight.FlightNumber = string.Join(", ", flightNumbers!);

                // Insert flight and observation
                _flightRepository.Insert(flight);
                observation.FlightId = flight.Id;
                _observationRepository.Insert(observation);











            }


        }



    }

    private string ExtractNumber(string leg)
    {
        var parts = leg.Split("-");
        return $"{parts[2]} {parts[3]}";
    }

    private void Explore()
    {
        var searchButton = _driver.FindElement(By.XPath("//span[text()=\"Search\"]"));
        new Actions(_driver)
            .Click(searchButton)
            .Pause(TimeSpan.FromMilliseconds(200))
            .Perform();
    }

    private void EnterDeparture(DateTime dateTime)
    {
        var dateField = _driver.FindElement(By.XPath("//input[@aria-label=\"Departure\"]"));
        var dateString = dateTime.ToString("MMM d");

        new Actions(_driver)
            .Click(dateField)
            .Pause(TimeSpan.FromMilliseconds(200))
            .SendKeys(dateString)
            .Pause(TimeSpan.FromMilliseconds(200))
            .SendKeys(Keys.Enter)
            .Pause(TimeSpan.FromMilliseconds(200))
            .SendKeys(Keys.Escape)
            .Perform();

        //// Loose focus of date input
        //var flightsText = _driver.FindElement(By.XPath("//div[text()=\"Flights\"]"));
        //new Actions(_driver)
        //    .MoveToElement(flightsText, 0, 0)
        //    .Click();

    }

    private void EnterDestination(string destinationIata)
    {
        var destinationField = _driver.FindElement(By.XPath("//input[@aria-label=\"Where to? \"]"));
        new Actions(_driver)
            .Click(destinationField)
            .Pause(TimeSpan.FromMilliseconds(200))
            .SendKeys(destinationIata)
            .Pause(TimeSpan.FromMilliseconds(200))
            .SendKeys(Keys.Enter)
            .Perform();
    }

    private void EnterOrigin(string originIata)
    {
        var inputField = _driver.FindElement(By.XPath("//input[@aria-label=\"Where from?\"]"));
        new Actions(_driver)
        .Click(inputField)
        .Pause(TimeSpan.FromMilliseconds(200))
        .KeyDown(Keys.Control).SendKeys("a").KeyUp(Keys.Control)
        .SendKeys(Keys.Delete)
        .Pause(TimeSpan.FromMilliseconds(200))
        .SendKeys(originIata)
        .Pause(TimeSpan.FromMilliseconds(500))
        .SendKeys(Keys.Enter)
        .Perform();
    }


}
