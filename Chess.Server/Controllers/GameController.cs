using Microsoft.AspNetCore.Mvc;
using Chess.Server.Services;
using Chess.Shared.Constants;
using Chess.Shared.DTOs;
using Chess.Shared.Models;

namespace Chess.Server.Controllers;

[ApiController]
[Route(ApiRoutes.GamesController)]
public class GameController : ControllerBase
{
    private readonly MatchmakerService _matchmaker;

    public GameController(MatchmakerService matchmaker) => _matchmaker = matchmaker;

    /// <summary>Returns the full current state of a game.</summary>
    [HttpGet("{gameId}")]
    public IActionResult GetGame(string gameId)
    {
        var state = _matchmaker.GetGame(gameId);
        if (state is null) return NotFound();
        return Ok(MapToGameDto(state));
    }

    /// <summary>Returns the move history for a game as a list of SAN strings.</summary>
    [HttpGet("{gameId}/history")]
    public IActionResult GetHistory(string gameId)
    {
        var state = _matchmaker.GetGame(gameId);
        if (state is null) return NotFound();

        var history = state.MoveHistory.Select((m, i) => new
        {
            MoveNumber = m.MoveNumber,
            Notation   = m.Notation ?? m.ToString(),
            From       = m.From.ToAlgebraic(),
            To         = m.To.ToAlgebraic(),
            MoveType   = m.MoveType.ToString(),
            Timestamp  = m.Timestamp
        });

        return Ok(history);
    }

    /// <summary>Returns all active games (for admin / spectator use).</summary>
    [HttpGet]
    public IActionResult GetAllGames()
    {
        var games = _matchmaker.GetAllGames().Select(MapToGameDto);
        return Ok(games);
    }

    // ─── Mapping ──────────────────────────────────────────────────────────

    private static GameDto MapToGameDto(GameState s) => new()
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
        Color       = p.AssignedColor ?? Chess.Shared.Enums.PieceColor.White,
        IsConnected = p.IsConnected
    };
}
