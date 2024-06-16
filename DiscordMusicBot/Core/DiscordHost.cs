using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Core.DiscordHost
{
  public sealed class DiscordHost : IHostedService
  {
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscordHost> _logger;
    private readonly IConfiguration _configuration;
    private readonly TaskCompletionSource<bool> _clientReadyCompletionSource = new();

    public DiscordHost(
        DiscordSocketClient discordSocketClient,
        InteractionService interactionService,
        IServiceProvider serviceProvider,
        ILogger<DiscordHost> logger,
        IConfiguration configuration)
    {
      _discordSocketClient = discordSocketClient ?? throw new ArgumentNullException(nameof(discordSocketClient));
      _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
      _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      _discordSocketClient.Log += LogAsync;
      _discordSocketClient.InteractionCreated += InteractionCreated;
      _discordSocketClient.Ready += ClientReady;

      _logger.LogInformation("Logging in the bot.");
      try
      {
        var token = _configuration["Discord:Token"];
        if (string.IsNullOrEmpty(token))
        {
          throw new InvalidOperationException("Bot token is not configured.");
        }

        await _discordSocketClient
            .LoginAsync(TokenType.Bot, token)
            .ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error logging in the bot.");
        throw;
      }

      _logger.LogInformation("Starting the bot.");
      try
      {
        await _discordSocketClient
            .StartAsync()
            .ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error starting the bot.");
        throw;
      }

      _logger.LogInformation("Waiting for the Discord client to be ready.");
      var clientReadyTask = _clientReadyCompletionSource.Task;

      if (await Task.WhenAny(clientReadyTask, Task.Delay(TimeSpan.FromMinutes(2))) != clientReadyTask)
      {
        _logger.LogError("Timed out while waiting for Discord client to be ready.");
        throw new TimeoutException("Timed out while waiting for Discord client to be ready.");
      }

      _logger.LogInformation("Discord client is ready.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
      _discordSocketClient.Log -= LogAsync;
      _discordSocketClient.InteractionCreated -= InteractionCreated;
      _discordSocketClient.Ready -= ClientReady;

      _logger.LogInformation("Stopping the bot.");
      await _discordSocketClient
          .StopAsync()
          .ConfigureAwait(false);
    }

    private Task InteractionCreated(SocketInteraction interaction)
    {
      var interactionContext = new SocketInteractionContext(_discordSocketClient, interaction);
      return _interactionService.ExecuteCommandAsync(interactionContext, _serviceProvider);
    }

    private async Task ClientReady()
    {
      _logger.LogInformation("Client is ready, adding modules.");
      ulong guildID = ulong.Parse(_configuration["Discord:GuildID"]!);
      try
      {
        await _interactionService
            .AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider)
            .ConfigureAwait(false);

        await _interactionService
            .RegisterCommandsToGuildAsync(guildID) 
            .ConfigureAwait(false);

        _logger.LogInformation("Modules added and commands registered.");
        _clientReadyCompletionSource.SetResult(true);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during ClientReady.");
        throw;
      }
    }

    private Task LogAsync(LogMessage logMessage)
    {
      _logger.Log(logMessage.Severity.ToLogLevel(), logMessage.Exception, logMessage.Message);
      return Task.CompletedTask;
    }
  }

  public static class LogSeverityExtensions
  {
    public static LogLevel ToLogLevel(this LogSeverity severity)
    {
      return severity switch
      {
        LogSeverity.Critical => LogLevel.Critical,
        LogSeverity.Error => LogLevel.Error,
        LogSeverity.Warning => LogLevel.Warning,
        LogSeverity.Info => LogLevel.Information,
        LogSeverity.Verbose => LogLevel.Debug,
        LogSeverity.Debug => LogLevel.Trace,
        _ => LogLevel.None,
      };
    }
  }
}