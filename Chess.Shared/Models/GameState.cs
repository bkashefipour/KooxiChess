using Chess.Shared.Enums;

namespace Chess.Shared.Models;

/// <summary>
/// Complete state of a chess game: board position, turn, castling rights,
/// en passant target, move history, clocks, and game status.
/// This is the single source of truth for one game.
/// </summary>
public class GameState
{
    /// <summary>Unique game identifier.</summary>
    public string GameId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Current board position.</summary>
    public Board Board { get; set; } = Board.CreateInitial();

    /// <summary>Whose turn it is.</summary>
    public PieceColor ActiveColor { get; set; } = PieceColor.White;

    /// <summary>Current game status.</summary>
    public GameStatus Status { get; set; } = GameStatus.WaitingForOpponent;

    // ──────────────────────────────────────────────
    //  Castling rights
    // ──────────────────────────────────────────────

    public bool WhiteCanCastleKingSide { get; set; } = true;
    public bool WhiteCanCastleQueenSide { get; set; } = true;
    public bool BlackCanCastleKingSide { get; set; } = true;
    public bool BlackCanCastleQueenSide { get; set; } = true;

    // ──────────────────────────────────────────────
    //  En passant
    // ──────────────────────────────────────────────

    /// <summary>
    /// The square behind a pawn that just advanced two squares,
    /// making it eligible for en passant capture. Null if not applicable.
    /// </summary>
    public Square? EnPassantTarget { get; set; }

    // ──────────────────────────────────────────────
    //  Move counters (for fifty-move rule and move numbering)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Half-move clock: number of half-moves since the last pawn advance
    /// or capture. Used for the fifty-move draw rule.
    /// </summary>
    public int HalfMoveClock { get; set; } = 0;

    /// <summary>
    /// Full-move number: starts at 1, incremented after black's move.
    /// </summary>
    public int FullMoveNumber { get; set; } = 1;

    // ──────────────────────────────────────────────
    //  Players
    // ──────────────────────────────────────────────

    public Player? WhitePlayer { get; set; }
    public Player? BlackPlayer { get; set; }

    // ──────────────────────────────────────────────
    //  Clocks (remaining time in seconds)
    // ──────────────────────────────────────────────

    /// <summary>Time control: total seconds per player (e.g. 600 for 10 min).</summary>
    public int TimeControlSeconds { get; set; } = 600;

    /// <summary>Increment per move in seconds (e.g. 5 for 10+5).</summary>
    public int IncrementSeconds { get; set; } = 0;

    public double WhiteRemainingSeconds { get; set; } = 600;
    public double BlackRemainingSeconds { get; set; } = 600;

    /// <summary>UTC timestamp of when the current player's clock started ticking.</summary>
    public DateTime? ClockStartedAt { get; set; }

    // ──────────────────────────────────────────────
    //  Move history
    // ──────────────────────────────────────────────

    public List<Move> MoveHistory { get; set; } = [];

    // ──────────────────────────────────────────────
    //  Timestamps
    // ──────────────────────────────────────────────

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // ──────────────────────────────────────────────
    //  FEN
    // ──────────────────────────────────────────────

    /// <summary>
    /// Exports the full FEN string for this game state.
    /// Example: "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
    /// </summary>
    public string ToFen()
    {
        string placement = Board.ToFenPlacement();
        string active = ActiveColor == PieceColor.White ? "w" : "b";

        string castling = "";
        if (WhiteCanCastleKingSide) castling += "K";
        if (WhiteCanCastleQueenSide) castling += "Q";
        if (BlackCanCastleKingSide) castling += "k";
        if (BlackCanCastleQueenSide) castling += "q";
        if (castling == "") castling = "-";

        string enPassant = EnPassantTarget?.ToAlgebraic() ?? "-";

        return $"{placement} {active} {castling} {enPassant} {HalfMoveClock} {FullMoveNumber}";
    }

    /// <summary>Creates a new game in starting position with the given time control.</summary>
    public static GameState CreateNew(int timeControlSeconds = 600, int incrementSeconds = 0)
    {
        return new GameState
        {
            TimeControlSeconds = timeControlSeconds,
            IncrementSeconds = incrementSeconds,
            WhiteRemainingSeconds = timeControlSeconds,
            BlackRemainingSeconds = timeControlSeconds
        };
    }
}
