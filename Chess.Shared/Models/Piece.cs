using Chess.Shared.Enums;

namespace Chess.Shared.Models;

/// <summary>
/// Represents a chess piece on the board.
/// </summary>
public class Piece
{
    public PieceType Type { get; set; }
    public PieceColor Color { get; set; }
    public bool HasMoved { get; set; }

    public Piece(PieceType type, PieceColor color)
    {
        Type = type;
        Color = color;
        HasMoved = false;
    }

    /// <summary>
    /// Returns the FEN character for this piece (uppercase = white, lowercase = black).
    /// </summary>
    public char ToFenChar()
    {
        char c = Type switch
        {
            PieceType.Pawn   => 'P',
            PieceType.Knight => 'N',
            PieceType.Bishop => 'B',
            PieceType.Rook   => 'R',
            PieceType.Queen  => 'Q',
            PieceType.King   => 'K',
            _ => throw new InvalidOperationException()
        };
        return Color == PieceColor.Black ? char.ToLower(c) : c;
    }

    public override string ToString() => $"{Color} {Type}";
}
