namespace Chess.Shared.Models;

/// <summary>
/// Represents a square on the chess board using file (a-h) and rank (1-8).
/// </summary>
public record Square
{
    /// <summary>File index: 0 = a, 7 = h.</summary>
    public int File { get; init; }

    /// <summary>Rank index: 0 = rank 1 (white home), 7 = rank 8 (black home).</summary>
    public int Rank { get; init; }

    public Square(int file, int rank)
    {
        if (file < 0 || file > 7) throw new ArgumentOutOfRangeException(nameof(file));
        if (rank < 0 || rank > 7) throw new ArgumentOutOfRangeException(nameof(rank));
        File = file;
        Rank = rank;
    }

    /// <summary>Create from algebraic notation, e.g. "e4".</summary>
    public static Square FromAlgebraic(string notation)
    {
        if (notation.Length != 2)
            throw new ArgumentException("Expected two characters, e.g. 'e4'.", nameof(notation));

        int file = notation[0] - 'a';
        int rank = notation[1] - '1';
        return new Square(file, rank);
    }

    /// <summary>Returns algebraic notation, e.g. "e4".</summary>
    public string ToAlgebraic() => $"{(char)('a' + File)}{Rank + 1}";

    public override string ToString() => ToAlgebraic();

    /// <summary>Checks whether this square is a valid board coordinate.</summary>
    public static bool IsValid(int file, int rank) => file >= 0 && file <= 7 && rank >= 0 && rank <= 7;
}
