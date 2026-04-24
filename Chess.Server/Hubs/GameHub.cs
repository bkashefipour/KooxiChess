using Microsoft.AspNetCore.SignalR;
using Chess.Server.Data;
using Chess.Server.Services;
using Chess.Shared.Constants;
using Chess.Shared.DTOs;
using Chess.Shared.Enums;
using Chess.Shared.Models;

namespace Chess.Server.Hubs;

public class GameHub : Hub
{
    private readonly MatchmakerService _matchmaker;
    private readonly MoveValidatorService _validator;
    private readonly GameEngineService _engine;
    private readonly GameRepository _repository;
    private readonly ILogger<GameHub> _logger;

    public GameHub(MatchmakerService matchmaker, MoveValidatorService validator,
                   GameEngineService engine, GameRepository repository, ILogger<GameHub> logger)
    {
        _matchmaker = matchmaker;
        _validator  = validator;
        _engine     = engine;
        _repository = repository;
        _logger     = logger;
    }

    // ─── Client → Server ─────────────────────────────────────────────────

    public async Task JoinGame(string gameId, string userId, string displayName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

        var existing = await _matchmaker.GetGameAsync(gameId);
        if (existing is null)
        {
            var creator = new Player { UserId = userId, DisplayName = displayName, ConnectionId = Context.ConnectionId };
            var state   = await _matchmaker.CreateGameAsync(creator);
            await Groups.AddToGroupAsync(Context.ConnectionId, state.GameId);
            await Clients.Caller.SendAsync(HubMethods.GameStarted, MapToDto(state));
            return;
        }

        // Reconnecting player
        var isWhite = existing.WhitePlayer?.UserId == userId;
        var isBlack = existing.BlackPlayer?.UserId == userId;
        if (isWhite || isBlack)
        {
            _matchmaker.UpdateConnectionId(gameId, userId, Context.ConnectionId);
            await Clients.Caller.SendAsync(HubMethods.GameStarted, MapToDto(existing));
            await Clients.OthersInGroup(gameId).SendAsync(HubMethods.OpponentConnected);
            return;
        }

        // Second player joining for the first time
        var joiner = new Player { UserId = userId, DisplayName = displayName, ConnectionId = Context.ConnectionId };
        var (updated, error) = await _matchmaker.JoinGameAsync(gameId, joiner);
        if (error is not null) { await Clients.Caller.SendAsync("Error", error); return; }

        await Clients.Group(gameId).SendAsync(HubMethods.GameStarted, MapToDto(updated!));
    }

    public async Task MakeMove(MoveDto dto)
    {
        var state = _matchmaker.GetGame(dto.GameId); // hot path — must already be cached
        if (state is null) return;

        var (move, error) = _validator.Validate(state, dto);
        if (error is not null)
        {
            await Clients.Caller.SendAsync(HubMethods.MoveRejected, new MoveResultDto
            {
                IsValid = false, ErrorMessage = error,
                From = dto.From, To = dto.To, Fen = state.ToFen()
            });
            return;
        }

        // Tick clock for the player who just moved, then add increment
        if (state.ClockStartedAt.HasValue)
        {
            var elapsed = (DateTime.UtcNow - state.ClockStartedAt.Value).TotalSeconds;
            if (state.ActiveColor == PieceColor.White)
                state.WhiteRemainingSeconds = Math.Max(0, state.WhiteRemainingSeconds - elapsed + state.IncrementSeconds);
            else
                state.BlackRemainingSeconds = Math.Max(0, state.BlackRemainingSeconds - elapsed + state.IncrementSeconds);
        }

        var boardBefore = state.Board.Clone();
        _engine.ApplyMove(state, move!);
        state.ClockStartedAt = DateTime.UtcNow;

        bool isCheck = state.Status == GameStatus.Check;
        bool isMate  = state.Status == GameStatus.Checkmate;
        move!.Notation = _engine.GenerateNotation(boardBefore, move, isCheck, isMate);

        await _repository.UpsertAsync(state);

        await Clients.Group(dto.GameId).SendAsync(HubMethods.MoveMade, new MoveResultDto
        {
            IsValid        = true,
            From           = dto.From,
            To             = dto.To,
            MoveType       = move.MoveType,
            PromotionPiece = move.PromotionPiece,
            Notation       = move.Notation,
            Fen            = state.ToFen(),
            GameStatus     = state.Status,
            IsCheck        = isCheck,
            IsCheckmate    = isMate
        });
    }

    public async Task Resign(string gameId, string userId)
    {
        var state = _matchmaker.GetGame(gameId);
        if (state is null) return;

        state.Status      = GameStatus.Resigned;
        state.CompletedAt = DateTime.UtcNow;

        await _repository.UpsertAsync(state);
        await Clients.Group(gameId).SendAsync(HubMethods.GameOver,
            new { Reason = "Resignation", ResignedBy = userId, Fen = state.ToFen() });
    }

    public async Task OfferDraw(string gameId) =>
        await Clients.OthersInGroup(gameId).SendAsync(HubMethods.DrawOffered);

    public async Task AcceptDraw(string gameId)
    {
        var state = _matchmaker.GetGame(gameId);
        if (state is null) return;

        state.Status      = GameStatus.Draw;
        state.CompletedAt = DateTime.UtcNow;

        await _repository.UpsertAsync(state);
        await Clients.Group(gameId).SendAsync(HubMethods.GameOver,
            new { Reason = "Draw", Fen = state.ToFen() });
    }

    public async Task DeclineDraw(string gameId) =>
        await Clients.OthersInGroup(gameId).SendAsync(HubMethods.DrawDeclined);

    // ─── Connection lifecycle ─────────────────────────────────────────────

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _matchmaker.MarkDisconnected(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private static GameDto MapToDto(GameState s) => new()
    {
        GameId                = s.GameId,
        Fen                   = s.ToFen(),
        Status                = s.Status,
        ActiveColor           = s.ActiveColor,
        WhitePlayer           = s.WhitePlayer is null ? null : MapPlayer(s.WhitePlayer),
        BlackPlayer           = s.BlackPlayer is null ? null : MapPlayer(s.BlackPlayer),
        WhiteRemainingSeconds = s.WhiteRemainingSeconds,
        BlackRemainingSeconds = s.BlackRemainingSeconds,
        MoveHistory           = s.MoveHistory.Select(m => m.Notation ?? m.ToString()).ToList()
    };

    private static PlayerDto MapPlayer(Player p) => new()
    {
        UserId      = p.UserId,
        DisplayName = p.DisplayName,
        Color       = p.AssignedColor ?? PieceColor.White,
        IsConnected = p.IsConnected
    };
}
