using CarInsurance.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class PolicyExpiredNotifierService(
    ILogger<PolicyExpiredNotifierService> logger,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Expired Policy Notifier Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForExpiredPoliciesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while checking for expired policies.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        logger.LogInformation("Expired Policy Notifier Service is stopping.");
    }

    private async Task CheckForExpiredPoliciesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Checking for expired policies...");

        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateOnly.FromDateTime(DateTime.Now);

            var policiesToNotify = await dbContext.Policies
                .Where(p => !p.IsExpirationNotified && p.EndDate < now)
                .ToListAsync(cancellationToken);

            if (policiesToNotify.Count == 0)
            {
                logger.LogInformation("No new expired policies found.");
                return;
            }

            foreach (var policy in policiesToNotify)
            {
                logger.LogWarning("Policy {PolicyId} for Car {CarId} expired at {EndDate}",
                    policy.Id, policy.CarId, policy.EndDate);

                policy.IsExpirationNotified = true;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully processed {Count} expired policies.", policiesToNotify.Count);
        }
    }
}