using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;

namespace App.WindowsService;

public sealed class WindowsBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WindowsBackgroundService> _logger;
    private Timer? _timer;
    EventLog eventLog = new EventLog();

    public WindowsBackgroundService(IConfiguration configuration, ILogger<WindowsBackgroundService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        eventLog.Source = "WorkServiceKillOnTime";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            TimeSpan timeUntilThreeAM = GetLaunchTime();
            _timer = new Timer(state => DoWork(), null, timeUntilThreeAM, TimeSpan.FromDays(1)); // Run the task every day

            // This construct is essentially a placeholder to make the method asynchronous without adding any actual asynchronous behavior
            await Task.CompletedTask;

            stoppingToken.Register(() =>
            {
                _timer?.Change(Timeout.Infinite, 0);
                _timer?.Dispose();
            });
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }

    private void DoWork()
    {
        string[]? killTargets = _configuration.GetSection("KillTargets").Get<string[]>();
        if (killTargets == null || !killTargets.Any())
        {
            _logger.LogCritical("No targets specified in appsettings.json");
            return;
        }

        foreach (string target in killTargets)
        {
            Process[] processes = Process.GetProcessesByName(target);
            foreach (Process process in processes)
            {
                process.Kill();
                _logger.LogInformation($"Killed process: {target}");
                eventLog.WriteEntry($"Killed process: {target} - ID {process.Id}");
            }
        }
    }

    private TimeSpan GetLaunchTime()
    {
        DateTime now = DateTime.Now;
        // Get launch time from configuration
        string? launchTimeStr = _configuration["LaunchTime"];

        if (string.IsNullOrEmpty(launchTimeStr))
        {
            throw new InvalidOperationException("Launch time not found in appsettings.json.");
        }

        // Parse launch time string to TimeSpan
        if (!TimeSpan.TryParse(launchTimeStr, out TimeSpan launchTime))
        {
            throw new InvalidOperationException($"Invalid launch time format in appsettings.json. Launch time: '{launchTimeStr}'. Please use the HH:mm format.");
        }
        DateTime nextLaunchDateTime = now.Date.Add(launchTime);

        _logger.LogInformation($"LauchTime set to: {launchTime}");
        eventLog.WriteEntry($"LauchTime set to: {launchTime}");

        if (now > nextLaunchDateTime)
        {
            nextLaunchDateTime = nextLaunchDateTime.AddDays(1);
        }
        return nextLaunchDateTime - now;
    }
}