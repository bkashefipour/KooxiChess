using System.Text;
using Chess.Shared.Enums;

namespace Chess.Shared.Models;

/// <summary>
/// Represents the chess board state as an 8×8 grid of pieces.
/// Supports FEN serialisation for compact storage and transmission.
/// </summary>
public class Board
{
    /// <summary>
    /// The 8×8 grid. Indexed as Squares[file, rank] where
    /// file 0 = a, rank 0 = 1 (white's home row).
    /// </summary>
    public Piece?[,] Squares { get; private set; } = new Piece?[8, 8];

    // ──────────────────────────────────────────────
    //  Accessors
    // ──────────────────────────────────────────────

    public Piece? GetPiece(Square square) => Squares[square.File, square.Rank];

    public Piece? GetPiece(int file, int rank) => Squares[file, rank];

    public void SetPiece(Square square, Piece? piece) => Squares[square.File, square.Rank] = piece;

    public void SetPiece(int file, int rank, Piece? piece) => Squares[file, rank] = piece;

    public void RemovePiece(Square square) => Squares[square.File, square.Rank] = null;

    public bool IsEmpty(Square square) => Squares[square.File, square.Rank] is null;

    // ──────────────────────────────────────────────
    //  Find pieces
    // ──────────────────────────────────────────────

    /// <summary>Finds the square of the king for the given colour.</summary>
    public Square? FindKing(PieceColor color)
    {
        for (int f = 0; f < 8; f++)
        for (int r = 0; r < 8; r++)
        {
            var piece = Squares[f, r];
            if (piece is { Type: PieceType.King } && piece.Color == color)
                return new Square(f, r);
        }
        return null;
    }

    /// <summary>Returns all squares containing pieces of the given colour.</summary>
    public IEnumerable<(Square Square, Piece Piece)> GetPieces(PieceColor color)
    {
        for (int f = 0; f < 8; f++)
        for (int r = 0; r < 8; r++)
        {
            var piece = Squares[f, r];
            if (piece is not null && piece.Color == color)
                yield return (new Square(f, r), piece);
        }
    }

    // ──────────────────────────────────────────────
    //  Initial position
    // ──────────────────────────────────────────────

    /// <summary>Creates a board set up in the standard starting position.</summary>
    public static Board CreateInitial()
    {
        var board = new Board();

        // Back ranks
        PieceType[] backRank =
        [
            PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
            PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
        ];

        for (int f = 0; f < 8; f++)
        {
            board.Squares[f, 0] = new Piece(backRank[f], PieceColor.White);
            board.Squares[f, 1] = new Piece(PieceType.Pawn, PieceColor.White);
            board.Squares[f, 6] = new Piece(PieceType.Pawn, PieceColor.Black);
            board.Squares[f, 7] = new Piece(backRank[f], PieceColor.Black);
        }

        return board;
    }

    // ──────────────────────────────────────────────
    //  FEN serialisation (board placement only)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Converts the board to the piece-placement portion of a FEN string.
    /// Ranks are serialised from 8 (top) down to 1 (bottom).
    /// </summary>
    public string ToFenPlacement()
    {
        var sb = new StringBuilder();

        for (int rank = 7; rank >= 0; rank--)
        {
            int emptyCount = 0;

            for (int file = 0; file < 8; file++)
            {
                var piece = Squares[file, rank];
                if (piece is null)
                {
                    emptyCount++;
                }
                else
                {
                    if (emptyCount > 0)
                    {
                        sb.Append(emptyCount);
                        emptyCount = 0;
                    }
                    sb.Append(piece.ToFenChar());
                }
            }

            if (emptyCount > 0) sb.Append(emptyCount);
            if (rank > 0) sb.Append('/');
        }

        return sb.ToString();
    }

    /// <summary>Parses the piece-placement portion of a FEN string into a Board.</summary>
    public static Board FromFenPlacement(string fenPlacement)
    {
        var board = new Board();
        var ranks = fenPlacement.Split('/');

        if (ranks.Length != 8)
            throw new ArgumentException("FEN placement must have 8 ranks.", nameof(fenPlacement));

        for (int ri = 0; ri < 8; ri++)
        {
            int rank = 7 - ri; // FEN starts from rank 8
            int file = 0;

            foreach (char c in ranks[ri])
            {
                if (char.IsDigit(c))
                {
                    file += c - '0';
                }
                else
                {
                    var color = char.IsUpper(c) ? PieceColor.White : PieceColor.Black;
                    var type = char.ToUpper(c) switch
                    {
                        'P' => PieceType.Pawn,
                        'N' => PieceType.Knight,
                        'B' => PieceType.Bishop,
                        'R' => PieceType.Rook,
                        'Q' => PieceType.Queen,
                        'K' => PieceType.King,
                        _ => throw new ArgumentException($"Unknown piece char '{c}'.")
                    };
                    board.Squares[file, rank] = new Piece(type, color);
                    file++;
                }
            }
        }

        return board;
    }

    /// <summary>Creates a deep copy of the board for move simulation.</summary>
    public Board Clone()
    {
        var clone = new Board();
        for (int f = 0; f < 8; f++)
        for (int r = 0; r < 8; r++)
        {
            var piece = Squares[f, r];
            if (piece is not null)
                clone.Squares[f, r] = new Piece(piece.Type, piece.Color) { HasMoved = piece.HasMoved };
        }
        return clone;
    }
}
