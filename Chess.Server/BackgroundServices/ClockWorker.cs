using Microsoft.AspNetCore.SignalR;
using Chess.Server.Hubs;
using Chess.Server.Services;
using Chess.Shared.Constants;
using Chess.Shared.Enums;

namespace Chess.Server.BackgroundServices;

public class ClockWorker : BackgroundService
{
    private readonly MatchmakerService _matchmaker;
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<ClockWorker> _logger;

    public ClockWorker(MatchmakerService matchmaker, IHubContext<GameHub> hub, ILogger<ClockWorker> logger)
    {
        _matchmaker = matchmaker;
        _hub        = hub;
        _logger     = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
            await TickAsync();
        }
    }

    private async Task TickAsync()
    {
        var active = _matchmaker.GetAllGames()
            .Where(g => g.Status is GameStatus.InProgress or GameStatus.Check
                     && g.ClockStartedAt.HasValue);

        foreach (var game in active)
        {
            var elapsed = (DateTime.UtcNow - game.ClockStartedAt!.Value).TotalSeconds;

            // Compute display values without mutating stored remaining (hub does that on each move)
            double whiteRemaining = game.ActiveColor == PieceColor.White
                ? game.WhiteRemainingSeconds - elapsed
                : game.WhiteRemainingSeconds;

            double blackRemaining = game.ActiveColor == PieceColor.Black
                ? game.BlackRemainingSeconds - elapsed
                : game.BlackRemainingSeconds;

            if (whiteRemaining <= 0 || blackRemaining <= 0)
            {
                game.Status       = GameStatus.TimedOut;
                game.CompletedAt  = DateTime.UtcNow;

                _logger.LogInformation("Game {GameId} timed out.", game.GameId);

                await _hub.Clients.Group(game.GameId).SendAsync(HubMethods.GameOver, new
                {
                    Reason  = "Timeout",
                    Fen     = game.ToFen(),
                    Winner  = whiteRemaining <= 0 ? "Black" : "White"
                });
            }
            else
            {
                await _hub.Clients.Group(game.GameId).SendAsync(HubMethods.ClockUpdate, new
                {
                    WhiteRemainingSeconds = Math.Round(whiteRemaining, 1),
                    BlackRemainingSeconds = Math.Round(blackRemaining, 1)
                });
            }
        }
    }
}
