using FlightTracker.Api.Services;
using FlightTracker.Api.Services.Selenium;
using FlightTracker.Api.Services.Background;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Storage.Repositories;

namespace FlightTracker.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // LiteDB context (singleton)
            builder.Services.AddSingleton(new LiteDbContext("Filename=flights.db;Connection=shared"));

            // Repositories
            builder.Services.AddScoped<FlightRepository>();
            builder.Services.AddScoped<ObservationRepository>();
            builder.Services.AddScoped<QueryRepository>();

            // Services
            builder.Services.AddScoped<FlightStatsService>();
            builder.Services.AddScoped<FlightCollectionService>();

            // Background scheduler dependencies (disabled for now until implementation ready)
            // builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();
            // builder.Services.AddSingleton<IScheduleConfigProvider, DefaultScheduleConfigProvider>();
            // builder.Services.AddSingleton<IFlightScrapingService, DefaultFlightScrapingService>();
            // builder.Services.AddHostedService<ScrapeScheduler>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalFrontend", policy =>
                {
                    policy.WithOrigins("https://localhost:7108")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowLocalFrontend");

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
