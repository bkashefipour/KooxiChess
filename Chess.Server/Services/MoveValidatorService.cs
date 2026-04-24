using Chess.Shared.DTOs;
using Chess.Shared.Enums;
using Chess.Shared.Models;

namespace Chess.Server.Services;

public class MoveValidatorService
{
    private readonly GameEngineService _engine;

    public MoveValidatorService(GameEngineService engine) => _engine = engine;

    /// <summary>
    /// Validates a move DTO against the current game state.
    /// Returns the resolved Move on success, or an error message on failure.
    /// </summary>
    public (Move? Move, string? Error) Validate(GameState state, MoveDto dto)
    {
        Square from, to;
        try
        {
            from = Square.FromAlgebraic(dto.From);
            to   = Square.FromAlgebraic(dto.To);
        }
        catch
        {
            return (null, "Invalid square notation.");
        }

        if (state.Status is not (GameStatus.InProgress or GameStatus.Check))
            return (null, "Game is not in progress.");

        var piece = state.Board.GetPiece(from);
        if (piece is null)                    return (null, "No piece on source square.");
        if (piece.Color != state.ActiveColor) return (null, "It is not your turn.");

        var candidates = _engine.GetLegalMoves(state, from).Where(m => m.To == to).ToList();
        if (candidates.Count == 0) return (null, "Illegal move.");

        // Pawn promotion: all candidates share the same destination but differ by promotion piece
        if (candidates.All(m => m.MoveType == MoveType.PawnPromotion))
        {
            var promoType = dto.PromotionPiece ?? PieceType.Queen;
            var match = candidates.FirstOrDefault(m => m.PromotionPiece == promoType);
            return match is not null ? (match, null) : (null, "Invalid promotion piece.");
        }

        return (candidates[0], null);
    }
}
