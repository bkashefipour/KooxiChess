using Chess.Shared.Enums;

namespace Chess.Shared.DTOs;

/// <summary>
/// Broadcast to both players after a move is validated and applied.
/// </summary>
public class MoveResultDto
{
    public bool IsValid { get; set; }

    /// <summary>Error message when the move is rejected.</summary>
    public string? ErrorMessage { get; set; }

    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public MoveType MoveType { get; set; }
    public PieceType? PromotionPiece { get; set; }

    /// <summary>Standard algebraic notation, e.g. "Nf3+", "O-O".</summary>
    public string? Notation { get; set; }

    /// <summary>Updated FEN after the move.</summary>
    public string Fen { get; set; } = string.Empty;

    /// <summary>New game status after the move.</summary>
    public GameStatus GameStatus { get; set; }

    /// <summary>Is the opposing king now in check?</summary>
    public bool IsCheck { get; set; }

    /// <summary>Is this checkmate?</summary>
    public bool IsCheckmate { get; set; }
}
