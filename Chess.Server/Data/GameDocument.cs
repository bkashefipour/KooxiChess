using Newtonsoft.Json;
using Chess.Shared.Enums;
using Chess.Shared.Models;

namespace Chess.Server.Data;

/// <summary>
/// Cosmos DB document representation of a GameState.
/// Flattens the Board's 2D array into a serializable piece list.
/// </summary>
public class GameDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty; // must match GameId for Cosmos

    public string GameId { get; set; } = string.Empty;
    public List<PieceRecord> Pieces { get; set; } = [];

    public PieceColor ActiveColor { get; set; }
    public GameStatus Status { get; set; }

    public bool WhiteCanCastleKingSide { get; set; }
    public bool WhiteCanCastleQueenSide { get; set; }
    public bool BlackCanCastleKingSide { get; set; }
    public bool BlackCanCastleQueenSide { get; set; }

    public int? EnPassantFile { get; set; }
    public int? EnPassantRank { get; set; }

    public int HalfMoveClock { get; set; }
    public int FullMoveNumber { get; set; }

    public PlayerRecord? WhitePlayer { get; set; }
    public PlayerRecord? BlackPlayer { get; set; }

    public int TimeControlSeconds { get; set; }
    public int IncrementSeconds { get; set; }
    public double WhiteRemainingSeconds { get; set; }
    public double BlackRemainingSeconds { get; set; }
    public DateTime? ClockStartedAt { get; set; }

    public List<MoveRecord> MoveHistory { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // ─── Mapping ─────────────────────────────────────────────────────────

    public static GameDocument FromGameState(GameState s)
    {
        var pieces = new List<PieceRecord>();
        for (int f = 0; f < 8; f++)
        for (int r = 0; r < 8; r++)
        {
            var p = s.Board.GetPiece(f, r);
            if (p is not null)
                pieces.Add(new PieceRecord { File = f, Rank = r, Type = p.Type, Color = p.Color, HasMoved = p.HasMoved });
        }

        return new GameDocument
        {
            Id                    = s.GameId,
            GameId                = s.GameId,
            Pieces                = pieces,
            ActiveColor           = s.ActiveColor,
            Status                = s.Status,
            WhiteCanCastleKingSide  = s.WhiteCanCastleKingSide,
            WhiteCanCastleQueenSide = s.WhiteCanCastleQueenSide,
            BlackCanCastleKingSide  = s.BlackCanCastleKingSide,
            BlackCanCastleQueenSide = s.BlackCanCastleQueenSide,
            EnPassantFile         = s.EnPassantTarget?.File,
            EnPassantRank         = s.EnPassantTarget?.Rank,
            HalfMoveClock         = s.HalfMoveClock,
            FullMoveNumber        = s.FullMoveNumber,
            WhitePlayer           = s.WhitePlayer is null ? null : ToPlayerRecord(s.WhitePlayer),
            BlackPlayer           = s.BlackPlayer is null ? null : ToPlayerRecord(s.BlackPlayer),
            TimeControlSeconds    = s.TimeControlSeconds,
            IncrementSeconds      = s.IncrementSeconds,
            WhiteRemainingSeconds = s.WhiteRemainingSeconds,
            BlackRemainingSeconds = s.BlackRemainingSeconds,
            ClockStartedAt        = s.ClockStartedAt,
            MoveHistory           = s.MoveHistory.Select(m => new MoveRecord
            {
                FromFile       = m.From.File,
                FromRank       = m.From.Rank,
                ToFile         = m.To.File,
                ToRank         = m.To.Rank,
                MoveType       = m.MoveType,
                PromotionPiece = m.PromotionPiece,
                Notation       = m.Notation,
                MoveNumber     = m.MoveNumber,
                Timestamp      = m.Timestamp
            }).ToList(),
            CreatedAt   = s.CreatedAt,
            StartedAt   = s.StartedAt,
            CompletedAt = s.CompletedAt
        };
    }

    public GameState ToGameState()
    {
        var board = new Board();
        foreach (var p in Pieces)
            board.SetPiece(p.File, p.Rank, new Piece(p.Type, p.Color) { HasMoved = p.HasMoved });

        return new GameState
        {
            GameId                = GameId,
            Board                 = board,
            ActiveColor           = ActiveColor,
            Status                = Status,
            WhiteCanCastleKingSide  = WhiteCanCastleKingSide,
            WhiteCanCastleQueenSide = WhiteCanCastleQueenSide,
            BlackCanCastleKingSide  = BlackCanCastleKingSide,
            BlackCanCastleQueenSide = BlackCanCastleQueenSide,
            EnPassantTarget       = EnPassantFile.HasValue && EnPassantRank.HasValue
                                    ? new Square(EnPassantFile.Value, EnPassantRank.Value) : null,
            HalfMoveClock         = HalfMoveClock,
            FullMoveNumber        = FullMoveNumber,
            WhitePlayer           = WhitePlayer is null ? null : ToPlayer(WhitePlayer),
            BlackPlayer           = BlackPlayer is null ? null : ToPlayer(BlackPlayer),
            TimeControlSeconds    = TimeControlSeconds,
            IncrementSeconds      = IncrementSeconds,
            WhiteRemainingSeconds = WhiteRemainingSeconds,
            BlackRemainingSeconds = BlackRemainingSeconds,
            ClockStartedAt        = ClockStartedAt,
            MoveHistory           = MoveHistory.Select(m => new Move
            {
                From           = new Square(m.FromFile, m.FromRank),
                To             = new Square(m.ToFile, m.ToRank),
                MoveType       = m.MoveType,
                PromotionPiece = m.PromotionPiece,
                Notation       = m.Notation,
                MoveNumber     = m.MoveNumber,
                Timestamp      = m.Timestamp
            }).ToList(),
            CreatedAt   = CreatedAt,
            StartedAt   = StartedAt,
            CompletedAt = CompletedAt
        };
    }

    private static PlayerRecord ToPlayerRecord(Player p) => new()
    {
        UserId        = p.UserId,
        DisplayName   = p.DisplayName,
        Email         = p.Email,
        ConnectionId  = p.ConnectionId,
        AssignedColor = p.AssignedColor,
        IsConnected   = false // always start disconnected on reload
    };

    private static Player ToPlayer(PlayerRecord r) => new()
    {
        UserId        = r.UserId,
        DisplayName   = r.DisplayName,
        Email         = r.Email,
        ConnectionId  = r.ConnectionId,
        AssignedColor = r.AssignedColor,
        IsConnected   = false
    };
}

public class PieceRecord
{
    public int File { get; set; }
    public int Rank { get; set; }
    public PieceType Type { get; set; }
    public PieceColor Color { get; set; }
    public bool HasMoved { get; set; }
}

public class PlayerRecord
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ConnectionId { get; set; }
    public PieceColor? AssignedColor { get; set; }
    public bool IsConnected { get; set; }
}

public class MoveRecord
{
    public int FromFile { get; set; }
    public int FromRank { get; set; }
    public int ToFile { get; set; }
    public int ToRank { get; set; }
    public MoveType MoveType { get; set; }
    public PieceType? PromotionPiece { get; set; }
    public string? Notation { get; set; }
    public int MoveNumber { get; set; }
    public DateTime Timestamp { get; set; }
}
