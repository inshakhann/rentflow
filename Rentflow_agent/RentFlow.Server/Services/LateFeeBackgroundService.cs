using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RentFlow.Server.Data;
using RentFlow.Shared.Models;

namespace RentFlow.Server.Services
{
    public class LateFeeBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<LateFeeBackgroundService> _logger;

        public LateFeeBackgroundService(IServiceProvider services, ILogger<LateFeeBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LateFeeBackgroundService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessLateFees(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred processing late fees.");
                }

                // Check once every 24 hours. For testing, you could lower this.
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task ProcessLateFees(CancellationToken stoppingToken)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await EnsureCurrentMonthInvoices(context, stoppingToken);

            // Find all pending payments past their due date
            var overduePayments = await context.Payments
                .Include(p => p.Lease)
                .Where(p => p.Status == "Pending" && p.DueDate < DateTime.UtcNow.Date)
                .ToListAsync(stoppingToken);

            int count = 0;
            foreach (var payment in overduePayments)
            {
                // In a real app, read config for LateFeePercentage. We'll use 5% default.
                var penalty = payment.Amount * 0.05m; 
                
                payment.Status = "Late";
                payment.LateFee = penalty;
                count++;
            }

            if (count > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Processed {count} late fees.");
            }
        }

        private async Task EnsureCurrentMonthInvoices(AppDbContext context, CancellationToken stoppingToken)
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            var activeLeases = await context.Leases
                .Where(l => l.IsActive &&
                            l.StartDate < monthEnd &&
                            (!l.EndDate.HasValue || l.EndDate.Value >= monthStart))
                .Select(l => new { l.Id, l.TenantId, l.MonthlyRent })
                .ToListAsync(stoppingToken);

            if (!activeLeases.Any()) return;

            var leaseIds = activeLeases.Select(l => l.Id).ToList();

            var existingLeaseIdsForMonth = await context.Payments
                .Where(p => leaseIds.Contains(p.LeaseId)
                            && p.DueDate.Year == monthStart.Year
                            && p.DueDate.Month == monthStart.Month)
                .Select(p => p.LeaseId)
                .Distinct()
                .ToListAsync(stoppingToken);

            var newInvoices = activeLeases
                .Where(l => !existingLeaseIdsForMonth.Contains(l.Id))
                .Select(l => new Payment
                {
                    LeaseId = l.Id,
                    TenantId = l.TenantId,
                    DueDate = monthStart,
                    Amount = l.MonthlyRent,
                    LateFee = 0,
                    Status = "Pending"
                })
                .ToList();

            if (!newInvoices.Any()) return;

            context.Payments.AddRange(newInvoices);
            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Generated {Count} current-month rent invoices.", newInvoices.Count);
        }
    }
}
