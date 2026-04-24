using System.Collections.Concurrent;
using Chess.Server.Data;
using Chess.Shared.Enums;
using Chess.Shared.Models;

namespace Chess.Server.Services;

public class MatchmakerService
{
    private readonly ConcurrentDictionary<string, GameState> _cache = new();
    private readonly GameRepository _repository;

    public MatchmakerService(GameRepository repository) => _repository = repository;

    // ─── Create / Join ───────────────────────────────────────────────────

    public async Task<GameState> CreateGameAsync(Player creator, int timeControlSeconds = 600, int incrementSeconds = 0)
    {
        var state = GameState.CreateNew(timeControlSeconds, incrementSeconds);

        if (Random.Shared.Next(2) == 0) { state.WhitePlayer = creator; creator.AssignedColor = PieceColor.White; }
        else                            { state.BlackPlayer = creator; creator.AssignedColor = PieceColor.Black; }

        creator.IsConnected = true;
        _cache[state.GameId] = state;
        await _repository.UpsertAsync(state);
        return state;
    }

    public async Task<(GameState? State, string? Error)> JoinGameAsync(string gameId, Player joiner)
    {
        var state = await GetGameAsync(gameId);
        if (state is null) return (null, "Game not found.");
        if (state.WhitePlayer is not null && state.BlackPlayer is not null) return (null, "Game is already full.");

        if (state.WhitePlayer is null) { state.WhitePlayer = joiner; joiner.AssignedColor = PieceColor.White; }
        else                           { state.BlackPlayer = joiner; joiner.AssignedColor = PieceColor.Black; }

        joiner.IsConnected = true;
        state.Status       = GameStatus.InProgress;
        state.StartedAt    = DateTime.UtcNow;
        state.ClockStartedAt = DateTime.UtcNow;

        await _repository.UpsertAsync(state);
        return (state, null);
    }

    // ─── Lookup ──────────────────────────────────────────────────────────

    /// <summary>Returns the game from the in-memory cache, loading from Cosmos if not present.</summary>
    public async Task<GameState?> GetGameAsync(string gameId)
    {
        if (_cache.TryGetValue(gameId, out var cached)) return cached;

        var loaded = await _repository.GetAsync(gameId);
        if (loaded is not null) _cache[loaded.GameId] = loaded;
        return loaded;
    }

    /// <summary>Fast in-memory lookup only — use for hot paths where the game must already be cached.</summary>
    public GameState? GetGame(string gameId) =>
        _cache.TryGetValue(gameId, out var g) ? g : null;

    // ─── Connection tracking ──────────────────────────────────────────────

    public void UpdateConnectionId(string gameId, string userId, string connectionId)
    {
        if (!_cache.TryGetValue(gameId, out var state)) return;
        var player = state.WhitePlayer?.UserId == userId ? state.WhitePlayer
                   : state.BlackPlayer?.UserId == userId ? state.BlackPlayer : null;
        if (player is not null) { player.ConnectionId = connectionId; player.IsConnected = true; }
    }

    public void MarkDisconnected(string connectionId)
    {
        foreach (var state in _cache.Values)
        {
            if (state.WhitePlayer?.ConnectionId == connectionId) state.WhitePlayer.IsConnected = false;
            if (state.BlackPlayer?.ConnectionId == connectionId) state.BlackPlayer.IsConnected = false;
        }
    }

    // ─── Queries ──────────────────────────────────────────────────────────

    public IEnumerable<GameState> GetOpenGames() =>
        _cache.Values.Where(g => g.Status == GameStatus.WaitingForOpponent);

    public IEnumerable<GameState> GetAllGames() => _cache.Values;
}
