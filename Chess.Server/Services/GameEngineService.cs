using System.Text;
using Chess.Shared.Enums;
using Chess.Shared.Models;

namespace Chess.Server.Services;

public class GameEngineService
{
    // ─── Check Detection ─────────────────────────────────────────────────

    public bool IsInCheck(Board board, PieceColor color)
    {
        var king = board.FindKing(color);
        return king is not null && IsSquareAttacked(board, king, Opponent(color));
    }

    public bool IsSquareAttacked(Board board, Square square, PieceColor byColor)
    {
        // Knight
        int[] knightDf = [-2, -1, 1, 2, 2, 1, -1, -2];
        int[] knightDr = [1, 2, 2, 1, -1, -2, -2, -1];
        for (int i = 0; i < 8; i++)
        {
            int f = square.File + knightDf[i], r = square.Rank + knightDr[i];
            if (Square.IsValid(f, r))
            {
                var p = board.GetPiece(f, r);
                if (p?.Color == byColor && p.Type == PieceType.Knight) return true;
            }
        }

        // Diagonals (bishop / queen)
        int[][] diags = [[-1, -1], [-1, 1], [1, -1], [1, 1]];
        foreach (var d in diags)
        {
            for (int n = 1; n < 8; n++)
            {
                int f = square.File + d[0] * n, r = square.Rank + d[1] * n;
                if (!Square.IsValid(f, r)) break;
                var p = board.GetPiece(f, r);
                if (p is null) continue;
                if (p.Color == byColor && p.Type is PieceType.Bishop or PieceType.Queen) return true;
                break;
            }
        }

        // Orthogonals (rook / queen)
        int[][] orthos = [[-1, 0], [1, 0], [0, -1], [0, 1]];
        foreach (var d in orthos)
        {
            for (int n = 1; n < 8; n++)
            {
                int f = square.File + d[0] * n, r = square.Rank + d[1] * n;
                if (!Square.IsValid(f, r)) break;
                var p = board.GetPiece(f, r);
                if (p is null) continue;
                if (p.Color == byColor && p.Type is PieceType.Rook or PieceType.Queen) return true;
                break;
            }
        }

        // Pawns — white pawns attack upward, black pawns attack downward
        int pawnRank = square.Rank - (byColor == PieceColor.White ? 1 : -1);
        foreach (int df in new[] { -1, 1 })
        {
            int f = square.File + df;
            if (Square.IsValid(f, pawnRank))
            {
                var p = board.GetPiece(f, pawnRank);
                if (p?.Color == byColor && p.Type == PieceType.Pawn) return true;
            }
        }

        // King
        for (int df = -1; df <= 1; df++)
        for (int dr = -1; dr <= 1; dr++)
        {
            if (df == 0 && dr == 0) continue;
            int f = square.File + df, r = square.Rank + dr;
            if (Square.IsValid(f, r))
            {
                var p = board.GetPiece(f, r);
                if (p?.Color == byColor && p.Type == PieceType.King) return true;
            }
        }

        return false;
    }

    // ─── Move Generation ─────────────────────────────────────────────────

    /// <summary>Returns all legal moves from the given square for the active player.</summary>
    public IEnumerable<Move> GetLegalMoves(GameState state, Square from)
    {
        var piece = state.Board.GetPiece(from);
        if (piece is null || piece.Color != state.ActiveColor) yield break;

        foreach (var move in GeneratePseudoLegal(state, from, piece))
        {
            if (!LeavesKingInCheck(state, move))
                yield return move;
        }
    }

    public IEnumerable<Move> GetAllLegalMoves(GameState state)
    {
        foreach (var (sq, _) in state.Board.GetPieces(state.ActiveColor))
            foreach (var move in GetLegalMoves(state, sq))
                yield return move;
    }

    private bool LeavesKingInCheck(GameState state, Move move)
    {
        var clone = state.Board.Clone();
        ApplyMoveToBoard(clone, move, state.EnPassantTarget);
        return IsInCheck(clone, state.ActiveColor);
    }

    private IEnumerable<Move> GeneratePseudoLegal(GameState state, Square from, Piece piece)
        => piece.Type switch
        {
            PieceType.Pawn   => GeneratePawnMoves(state, from, piece),
            PieceType.Knight => GenerateKnightMoves(state.Board, from, piece),
            PieceType.Bishop => GenerateSliding(state.Board, from, piece, [[-1, -1], [-1, 1], [1, -1], [1, 1]]),
            PieceType.Rook   => GenerateSliding(state.Board, from, piece, [[-1, 0], [1, 0], [0, -1], [0, 1]]),
            PieceType.Queen  => GenerateSliding(state.Board, from, piece, [[-1,-1],[-1,1],[1,-1],[1,1],[-1,0],[1,0],[0,-1],[0,1]]),
            PieceType.King   => GenerateKingMoves(state, from, piece),
            _ => []
        };

    private IEnumerable<Move> GeneratePawnMoves(GameState state, Square from, Piece piece)
    {
        var board = state.Board;
        int dir = piece.Color == PieceColor.White ? 1 : -1;
        int startRank = piece.Color == PieceColor.White ? 1 : 6;
        int promoRank = piece.Color == PieceColor.White ? 7 : 0;

        // One square forward
        int toRank = from.Rank + dir;
        if (Square.IsValid(from.File, toRank) && board.IsEmpty(new Square(from.File, toRank)))
        {
            if (toRank == promoRank)
            {
                foreach (var pp in PromotionPieces())
                    yield return Promo(from, new Square(from.File, toRank), piece, null, pp);
            }
            else
            {
                yield return Make(from, new Square(from.File, toRank), MoveType.Normal, piece, null);

                // Two squares from start
                if (from.Rank == startRank)
                {
                    var sq2 = new Square(from.File, from.Rank + 2 * dir);
                    if (board.IsEmpty(sq2))
                        yield return Make(from, sq2, MoveType.Normal, piece, null);
                }
            }
        }

        // Diagonal captures and en passant
        foreach (int df in new[] { -1, 1 })
        {
            int tf = from.File + df, tr = from.Rank + dir;
            if (!Square.IsValid(tf, tr)) continue;
            var toSq = new Square(tf, tr);
            var target = board.GetPiece(tf, tr);

            if (target is not null && target.Color != piece.Color)
            {
                if (tr == promoRank)
                    foreach (var pp in PromotionPieces())
                        yield return Promo(from, toSq, piece, target, pp);
                else
                    yield return Make(from, toSq, MoveType.Capture, piece, target);
            }

            if (state.EnPassantTarget == toSq)
            {
                var epPawn = board.GetPiece(tf, from.Rank);
                yield return Make(from, toSq, MoveType.EnPassant, piece, epPawn);
            }
        }
    }

    private IEnumerable<Move> GenerateKnightMoves(Board board, Square from, Piece piece)
    {
        int[] df = [-2, -1, 1, 2, 2, 1, -1, -2];
        int[] dr = [1, 2, 2, 1, -1, -2, -2, -1];
        for (int i = 0; i < 8; i++)
        {
            int tf = from.File + df[i], tr = from.Rank + dr[i];
            if (!Square.IsValid(tf, tr)) continue;
            var target = board.GetPiece(tf, tr);
            if (target?.Color == piece.Color) continue;
            yield return Make(from, new Square(tf, tr), target is null ? MoveType.Normal : MoveType.Capture, piece, target);
        }
    }

    private IEnumerable<Move> GenerateSliding(Board board, Square from, Piece piece, int[][] dirs)
    {
        foreach (var d in dirs)
        {
            for (int n = 1; n < 8; n++)
            {
                int tf = from.File + d[0] * n, tr = from.Rank + d[1] * n;
                if (!Square.IsValid(tf, tr)) break;
                var target = board.GetPiece(tf, tr);
                if (target?.Color == piece.Color) break;
                yield return Make(from, new Square(tf, tr), target is null ? MoveType.Normal : MoveType.Capture, piece, target);
                if (target is not null) break;
            }
        }
    }

    private IEnumerable<Move> GenerateKingMoves(GameState state, Square from, Piece piece)
    {
        var board = state.Board;
        var opp = Opponent(piece.Color);

        // Normal moves
        for (int df = -1; df <= 1; df++)
        for (int dr = -1; dr <= 1; dr++)
        {
            if (df == 0 && dr == 0) continue;
            int tf = from.File + df, tr = from.Rank + dr;
            if (!Square.IsValid(tf, tr)) continue;
            var target = board.GetPiece(tf, tr);
            if (target?.Color == piece.Color) continue;
            yield return Make(from, new Square(tf, tr), target is null ? MoveType.Normal : MoveType.Capture, piece, target);
        }

        // Castling — king must not be in check and must not have moved
        if (piece.HasMoved || IsInCheck(board, piece.Color)) yield break;
        int rank = piece.Color == PieceColor.White ? 0 : 7;

        // King-side
        bool ksRight = piece.Color == PieceColor.White ? state.WhiteCanCastleKingSide : state.BlackCanCastleKingSide;
        if (ksRight)
        {
            var rook = board.GetPiece(7, rank);
            if (rook is { Type: PieceType.Rook, HasMoved: false } &&
                board.IsEmpty(new Square(5, rank)) && board.IsEmpty(new Square(6, rank)) &&
                !IsSquareAttacked(board, new Square(5, rank), opp) &&
                !IsSquareAttacked(board, new Square(6, rank), opp))
            {
                yield return Make(from, new Square(6, rank), MoveType.CastleKingSide, piece, null);
            }
        }

        // Queen-side
        bool qsRight = piece.Color == PieceColor.White ? state.WhiteCanCastleQueenSide : state.BlackCanCastleQueenSide;
        if (qsRight)
        {
            var rook = board.GetPiece(0, rank);
            if (rook is { Type: PieceType.Rook, HasMoved: false } &&
                board.IsEmpty(new Square(1, rank)) && board.IsEmpty(new Square(2, rank)) && board.IsEmpty(new Square(3, rank)) &&
                !IsSquareAttacked(board, new Square(3, rank), opp) &&
                !IsSquareAttacked(board, new Square(2, rank), opp))
            {
                yield return Make(from, new Square(2, rank), MoveType.CastleQueenSide, piece, null);
            }
        }
    }

    // ─── Move Application ─────────────────────────────────────────────────

    /// <summary>Applies a move to a board clone (for check-detection simulation).</summary>
    public void ApplyMoveToBoard(Board board, Move move, Square? enPassantTarget)
    {
        var piece = board.GetPiece(move.From)!;

        switch (move.MoveType)
        {
            case MoveType.EnPassant:
                board.RemovePiece(move.From);
                board.SetPiece(move.To, piece);
                board.SetPiece(move.To.File, move.From.Rank, null); // remove captured pawn
                break;

            case MoveType.CastleKingSide:
                board.RemovePiece(move.From);
                board.SetPiece(move.To, piece);
                var rookKS = board.GetPiece(7, move.From.Rank)!;
                board.SetPiece(7, move.From.Rank, null);
                board.SetPiece(5, move.From.Rank, rookKS);
                rookKS.HasMoved = true;
                break;

            case MoveType.CastleQueenSide:
                board.RemovePiece(move.From);
                board.SetPiece(move.To, piece);
                var rookQS = board.GetPiece(0, move.From.Rank)!;
                board.SetPiece(0, move.From.Rank, null);
                board.SetPiece(3, move.From.Rank, rookQS);
                rookQS.HasMoved = true;
                break;

            case MoveType.PawnPromotion:
                board.RemovePiece(move.From);
                board.SetPiece(move.To, new Piece(move.PromotionPiece!.Value, piece.Color) { HasMoved = true });
                break;

            default:
                board.RemovePiece(move.From);
                board.SetPiece(move.To, piece);
                break;
        }

        piece.HasMoved = true;
    }

    /// <summary>Applies a validated move to the game state and advances turn.</summary>
    public void ApplyMove(GameState state, Move move)
    {
        var piece = state.Board.GetPiece(move.From)!;
        bool isPawn = piece.Type == PieceType.Pawn;
        bool isCapture = move.MoveType is MoveType.Capture or MoveType.EnPassant;

        ApplyMoveToBoard(state.Board, move, state.EnPassantTarget);

        // En passant target: set when a pawn advances two squares
        state.EnPassantTarget = isPawn && Math.Abs(move.To.Rank - move.From.Rank) == 2
            ? new Square(move.From.File, (move.From.Rank + move.To.Rank) / 2)
            : null;

        // Castling rights — forfeit when king or rook moves, or rook square is captured
        UpdateCastlingRights(state, piece, move);

        // Half-move clock
        state.HalfMoveClock = isPawn || isCapture ? 0 : state.HalfMoveClock + 1;

        // Toggle turn
        state.ActiveColor = Opponent(state.ActiveColor);
        if (state.ActiveColor == PieceColor.White) state.FullMoveNumber++;

        // Record move
        move.MoveNumber = state.FullMoveNumber;
        state.MoveHistory.Add(move);

        // Status
        state.Status = DetermineStatus(state);
    }

    private void UpdateCastlingRights(GameState state, Piece piece, Move move)
    {
        if (piece.Type == PieceType.King)
        {
            if (piece.Color == PieceColor.White) { state.WhiteCanCastleKingSide = false; state.WhiteCanCastleQueenSide = false; }
            else                                  { state.BlackCanCastleKingSide = false; state.BlackCanCastleQueenSide = false; }
        }
        if (piece.Type == PieceType.Rook)
        {
            if (piece.Color == PieceColor.White)
            {
                if (move.From.File == 7 && move.From.Rank == 0) state.WhiteCanCastleKingSide = false;
                if (move.From.File == 0 && move.From.Rank == 0) state.WhiteCanCastleQueenSide = false;
            }
            else
            {
                if (move.From.File == 7 && move.From.Rank == 7) state.BlackCanCastleKingSide = false;
                if (move.From.File == 0 && move.From.Rank == 7) state.BlackCanCastleQueenSide = false;
            }
        }
        // Rook captured on its starting square
        if (move.To.Rank == 0) { if (move.To.File == 7) state.WhiteCanCastleKingSide = false; if (move.To.File == 0) state.WhiteCanCastleQueenSide = false; }
        if (move.To.Rank == 7) { if (move.To.File == 7) state.BlackCanCastleKingSide = false;  if (move.To.File == 0) state.BlackCanCastleQueenSide = false; }
    }

    // ─── Game Status ─────────────────────────────────────────────────────

    public GameStatus DetermineStatus(GameState state)
    {
        bool inCheck = IsInCheck(state.Board, state.ActiveColor);
        bool hasLegal = state.Board.GetPieces(state.ActiveColor).Any(e => GetLegalMoves(state, e.Square).Any());

        if (!hasLegal) return inCheck ? GameStatus.Checkmate : GameStatus.Stalemate;
        if (inCheck)   return GameStatus.Check;
        if (state.HalfMoveClock >= 100) return GameStatus.Draw; // fifty-move rule
        if (IsInsufficientMaterial(state.Board)) return GameStatus.Draw;

        return GameStatus.InProgress;
    }

    private static bool IsInsufficientMaterial(Board board)
    {
        var pieces = new List<Piece>();
        for (int f = 0; f < 8; f++)
        for (int r = 0; r < 8; r++)
        {
            var p = board.GetPiece(f, r);
            if (p is not null) pieces.Add(p);
        }
        if (pieces.Count == 2) return true; // K vs K
        if (pieces.Count == 3) return pieces.Any(p => p.Type is PieceType.Bishop or PieceType.Knight); // K+B/N vs K
        return false;
    }

    // ─── Notation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Generates standard algebraic notation for a move.
    /// <paramref name="boardBefore"/> must be the board position BEFORE the move was applied.
    /// </summary>
    public string GenerateNotation(Board boardBefore, Move move, bool isCheck, bool isMate)
    {
        string suffix = isMate ? "#" : isCheck ? "+" : "";

        if (move.MoveType == MoveType.CastleKingSide)  return $"O-O{suffix}";
        if (move.MoveType == MoveType.CastleQueenSide) return $"O-O-O{suffix}";

        var sb = new StringBuilder();
        var piece = move.MovedPiece!;

        if (piece.Type != PieceType.Pawn)
        {
            sb.Append(PieceLetter(piece.Type));
            AppendDisambiguation(sb, boardBefore, move, piece);
        }
        else if (move.MoveType is MoveType.Capture or MoveType.EnPassant)
        {
            sb.Append((char)('a' + move.From.File));
        }

        if (move.MoveType is MoveType.Capture or MoveType.EnPassant) sb.Append('x');
        sb.Append(move.To.ToAlgebraic());

        if (move.MoveType == MoveType.PawnPromotion)
            sb.Append($"={PieceLetter(move.PromotionPiece!.Value)}");

        sb.Append(suffix);
        return sb.ToString();
    }

    private void AppendDisambiguation(StringBuilder sb, Board board, Move move, Piece piece)
    {
        var ambiguous = board.GetPieces(piece.Color)
            .Where(x => x.Piece.Type == piece.Type && x.Square != move.From && CanAttackSquare(x.Square, move.To, piece.Type))
            .ToList();

        if (!ambiguous.Any()) return;

        bool sameFile = ambiguous.Any(x => x.Square.File == move.From.File);
        bool sameRank = ambiguous.Any(x => x.Square.Rank == move.From.Rank);

        if (!sameFile)        sb.Append((char)('a' + move.From.File));
        else if (!sameRank)   sb.Append(move.From.Rank + 1);
        else                  { sb.Append((char)('a' + move.From.File)); sb.Append(move.From.Rank + 1); }
    }

    private static bool CanAttackSquare(Square from, Square to, PieceType type)
    {
        int df = Math.Abs(to.File - from.File), dr = Math.Abs(to.Rank - from.Rank);
        return type switch
        {
            PieceType.Knight => (df == 1 && dr == 2) || (df == 2 && dr == 1),
            PieceType.Bishop => df == dr,
            PieceType.Rook   => df == 0 || dr == 0,
            PieceType.Queen  => df == 0 || dr == 0 || df == dr,
            _ => false
        };
    }

    private static char PieceLetter(PieceType type) => type switch
    {
        PieceType.Knight => 'N', PieceType.Bishop => 'B',
        PieceType.Rook   => 'R', PieceType.Queen  => 'Q',
        PieceType.King   => 'K', _ => '?'
    };

    // ─── Helpers ──────────────────────────────────────────────────────────

    public static PieceColor Opponent(PieceColor c) =>
        c == PieceColor.White ? PieceColor.Black : PieceColor.White;

    private static Move Make(Square from, Square to, MoveType mt, Piece moved, Piece? captured) =>
        new() { From = from, To = to, MoveType = mt, MovedPiece = moved, CapturedPiece = captured };

    private static Move Promo(Square from, Square to, Piece moved, Piece? captured, PieceType promo) =>
        new() { From = from, To = to, MoveType = MoveType.PawnPromotion, MovedPiece = moved, CapturedPiece = captured, PromotionPiece = promo };

    private static PieceType[] PromotionPieces() =>
        [PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight];
}
