using Microsoft.EntityFrameworkCore;
using TravelAgencyProject.Data;
using TravelAgencyProject.Models;

namespace TravelAgencyProject.Services
{
    public class NotificationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly EmailService _emailService;

        public NotificationWorker(IServiceScopeFactory scopeFactory, EmailService emailService)
        {
            _scopeFactory = scopeFactory;
            _emailService = emailService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // This loop runs as long as the website is alive
            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWork();
                // Wait for 1 hour before checking again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); //
            }
        }

        private async Task DoWork()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

              

                // Keep only the 5-day reminder:

                var targetDate = DateTime.Today.AddDays(5);
                var reminders = await context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Trip)
                    .Where(b => b.Trip.StartDate.Date == targetDate.Date)
                    .ToListAsync();

                foreach (var booking in reminders)
                {
                    await _emailService.SendEmailAsync(booking.User.Email, "Upcoming Trip!",
                        $"Get ready! Your trip to {booking.Trip.Destination} starts in 5 days.");
                }

                await context.SaveChangesAsync();
            }
        }
    }
    
}