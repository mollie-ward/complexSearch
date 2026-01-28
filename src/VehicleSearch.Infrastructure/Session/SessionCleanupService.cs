using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VehicleSearch.Infrastructure.Session;

/// <summary>
/// Background service that periodically cleans up expired sessions.
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly InMemoryConversationSessionService _sessionService;
    private readonly TimeSpan _cleanupInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionCleanupService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="sessionService">The session service.</param>
    /// <param name="configuration">The configuration.</param>
    public SessionCleanupService(
        ILogger<SessionCleanupService> logger,
        InMemoryConversationSessionService sessionService,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        
        var cleanupHours = configuration.GetValue<int>("ConversationSession:CleanupIntervalHours", 1);
        _cleanupInterval = TimeSpan.FromHours(cleanupHours);

        _logger.LogInformation("SessionCleanupService initialized with interval: {IntervalHours}h", cleanupHours);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionCleanupService starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                _logger.LogDebug("Running session cleanup");
                var removedCount = _sessionService.CleanupExpiredSessions();
                
                if (removedCount > 0)
                {
                    _logger.LogInformation("Session cleanup completed: removed {Count} expired sessions", removedCount);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                _logger.LogInformation("SessionCleanupService stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("SessionCleanupService stopped");
    }
}
