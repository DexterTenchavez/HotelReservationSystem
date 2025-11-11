using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HotelReservationSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotelReservationSystem.Services
{
    public class ReservationBackgroundService : BackgroundService
    {
        private readonly ILogger<ReservationBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ReservationBackgroundService(
            ILogger<ReservationBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reservation Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        await UpdateCompletedReservations(dbContext);
                        await UpdateCheckedInReservations(dbContext);
                    }

                    // Run every 5 minutes
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in reservation background service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task UpdateCompletedReservations(AppDbContext context)
        {
            var now = DateTime.Now;

            // Find reservations that should be marked as completed (check-out date passed)
            var expiredReservations = await context.Reservations
                .Include(r => r.Room)
                .Where(r => (r.Status == "Confirmed" || r.Status == "Checked-In") &&
                           r.CheckOutDate.HasValue &&
                           r.CheckOutDate.Value.Date <= now.Date)
                .ToListAsync();

            foreach (var reservation in expiredReservations)
            {
                reservation.Status = "Completed";
                reservation.ActualCheckOut = reservation.CheckOutDate;

                // Make the room available again
                if (reservation.Room != null)
                {
                    reservation.Room.IsAvailable = true;
                }
            }

            if (expiredReservations.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation($"Updated {expiredReservations.Count} reservations to completed status");
            }
        }

        private async Task UpdateCheckedInReservations(AppDbContext context)
        {
            var now = DateTime.Now;

            // Find reservations where check-in date has arrived
            var checkInReservations = await context.Reservations
                .Include(r => r.Room)
                .Where(r => r.Status == "Confirmed" &&
                           r.CheckInDate.HasValue &&
                           r.CheckInDate.Value.Date <= now.Date)
                .ToListAsync();

            foreach (var reservation in checkInReservations)
            {
                reservation.Status = "Checked-In";
            }

            if (checkInReservations.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation($"Updated {checkInReservations.Count} reservations to checked-in status");
            }
        }
    }
}