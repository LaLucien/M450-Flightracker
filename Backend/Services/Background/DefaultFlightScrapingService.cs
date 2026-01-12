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
/// TODO: Implement
/// </summary>
public class DefaultFlightScrapingService : IFlightScrapingService
{
    private IWebDriver _driver;
    private readonly QueryRepository _queryRepository;
    private readonly ObservationRepository _observationRepository;


    public DefaultFlightScrapingService(QueryRepository queryRepository, ObservationRepository observationRepository, IConfiguration configuration)
    {
        _queryRepository = queryRepository;
        _observationRepository = observationRepository;

        new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);

        var options = new ChromeOptions();
        if (configuration["TESTRUNNER"] == "roche-pc")
        {
            options.BinaryLocation = configuration["CHROME_EXE"];
        }



        _driver = new ChromeDriver(options);


    }

    public void Dispose()
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
            tasks.Add(Scrape(query, cancellationToken));
        }

        await Task.WhenAll(tasks);



    }

    public void EnsureGoogleFlightCookiesAccepted()
    {
        _driver.Navigate().GoToUrl("https://www.google.com/travel/flights?tfs=CBwQARoOagwIAhIIL20vMDg5NjZAAUgBcAGCAQsI____________AZgBAg&tfu=KgIIAw");
        var acceptCokieBanner = _driver.FindElement(By.XPath("//button[@aria-label=\"Accept all\"]"));
        acceptCokieBanner?.Click();
    }

    public async Task Scrape(QueryEntity query, CancellationToken cancellationToken)
    {
        for (int flexDay = -query.FlexibilityDays; flexDay <= query.FlexibilityDays; ++flexDay)
        {

            // One Way Selected URL
            _driver.Navigate().GoToUrl("https://www.google.com/travel/flights?tfs=CBwQARoOagwIAhIIL20vMDg5NjZAAUgBcAGCAQsI____________AZgBAg&tfu=KgIIAw");
            
            EnterOrigin(query.OriginIata);
            EnterDestination(query.DestinationIata);
            EnterDeparture(query.AnchorDate.AddDays(flexDay));
            Explore();
            Thread.Sleep(1000);


        }



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
