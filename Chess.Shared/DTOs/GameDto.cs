using Chess.Shared.Enums;

namespace Chess.Shared.DTOs;

/// <summary>
/// Full game state sent to clients after each move or on join.
/// </summary>
public class GameDto
{
    public string GameId { get; set; } = string.Empty;

    /// <summary>FEN string representing the current position.</summary>
    public string Fen { get; set; } = string.Empty;

    public GameStatus Status { get; set; }
    public PieceColor ActiveColor { get; set; }

    public PlayerDto? WhitePlayer { get; set; }
    public PlayerDto? BlackPlayer { get; set; }

    public double WhiteRemainingSeconds { get; set; }
    public double BlackRemainingSeconds { get; set; }

    /// <summary>The last move made (null at game start).</summary>
    public MoveResultDto? LastMove { get; set; }

    /// <summary>Full move history in algebraic notation.</summary>
    public List<string> MoveHistory { get; set; } = [];
}
