using Chess.Shared.Enums;

namespace Chess.Shared.Models;

/// <summary>
/// Represents a chess move from one square to another.
/// </summary>
public class Move
{
    public Square From { get; set; } = null!;
    public Square To { get; set; } = null!;
    public MoveType MoveType { get; set; } = MoveType.Normal;

    /// <summary>Piece type the pawn promotes to (only relevant when MoveType is PawnPromotion).</summary>
    public PieceType? PromotionPiece { get; set; }

    /// <summary>The piece that was captured, if any (set after move validation).</summary>
    public Piece? CapturedPiece { get; set; }

    /// <summary>The piece that moved.</summary>
    public Piece? MovedPiece { get; set; }

    /// <summary>Timestamp when the move was made (UTC).</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Move number in the game (1-based, increments after black moves).</summary>
    public int MoveNumber { get; set; }

    /// <summary>Standard algebraic notation for display, e.g. "Nf3", "O-O", "exd5".</summary>
    public string? Notation { get; set; }

    public override string ToString() => Notation ?? $"{From}-{To}";
}
