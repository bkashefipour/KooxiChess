using Microsoft.AspNetCore.Mvc;
using Chess.Server.Services;
using Chess.Shared.Constants;
using Chess.Shared.DTOs;
using Chess.Shared.Models;

namespace Chess.Server.Controllers;

[ApiController]
[Route(ApiRoutes.LobbyController)]
public class LobbyController : ControllerBase
{
    private readonly MatchmakerService _matchmaker;

    public LobbyController(MatchmakerService matchmaker) => _matchmaker = matchmaker;

    /// <summary>Returns all games waiting for a second player.</summary>
    [HttpGet]
    public IActionResult GetOpenGames()
    {
        var games = _matchmaker.GetOpenGames().Select(MapToLobbyDto);
        return Ok(games);
    }

    /// <summary>Creates a new game and returns its ID.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameDto dto)
    {
        var creator = new Player
        {
            UserId      = GetUserId(),
            DisplayName = GetDisplayName()
        };

        var state = await _matchmaker.CreateGameAsync(creator, dto.TimeControlSeconds, dto.IncrementSeconds);
        return CreatedAtAction(nameof(GetGame), new { gameId = state.GameId }, new { state.GameId });
    }

    /// <summary>Joins an existing open game.</summary>
    [HttpPost("{gameId}/join")]
    public async Task<IActionResult> JoinGame(string gameId)
    {
        var joiner = new Player
        {
            UserId      = GetUserId(),
            DisplayName = GetDisplayName()
        };

        var (state, error) = await _matchmaker.JoinGameAsync(gameId, joiner);
        if (error is not null) return BadRequest(new { error });

        return Ok(MapToGameDto(state!));
    }

    /// <summary>Returns the full state of a single game.</summary>
    [HttpGet("{gameId}")]
    public async Task<IActionResult> GetGame(string gameId)
    {
        var state = await _matchmaker.GetGameAsync(gameId);
        if (state is null) return NotFound();
        return Ok(MapToGameDto(state));
    }

    // ─── Mapping ──────────────────────────────────────────────────────────

    private static LobbyGameDto MapToLobbyDto(GameState s) => new()
    {
        GameId             = s.GameId,
        CreatorDisplayName = s.WhitePlayer?.DisplayName ?? s.BlackPlayer?.DisplayName ?? "Unknown",
        TimeControlSeconds = s.TimeControlSeconds,
        IncrementSeconds   = s.IncrementSeconds,
        Status             = s.Status,
        CreatedAt          = s.CreatedAt
    };

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

    private string GetUserId() =>
        User.FindFirst("oid")?.Value
        ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? HttpContext.Connection.Id;

    private string GetDisplayName() =>
        User.FindFirst("name")?.Value
        ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        ?? "Anonymous";
}
