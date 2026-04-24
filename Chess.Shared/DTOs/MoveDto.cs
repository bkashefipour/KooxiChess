using Chess.Shared.Enums;

namespace Chess.Shared.DTOs;

/// <summary>
/// Sent by the client to the server when a player makes a move.
/// Kept intentionally slim — the server validates everything.
/// </summary>
public class MoveDto
{
    /// <summary>The game this move belongs to.</summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>Origin square in algebraic notation, e.g. "e2".</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Destination square in algebraic notation, e.g. "e4".</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Piece type to promote to (only required for pawn promotion).</summary>
    public PieceType? PromotionPiece { get; set; }
}
