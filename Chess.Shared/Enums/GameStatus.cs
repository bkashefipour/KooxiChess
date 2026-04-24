namespace Chess.Shared.Enums;

public enum GameStatus
{
    WaitingForOpponent,
    InProgress,
    Check,
    Checkmate,
    Stalemate,
    Draw,
    Resigned,
    TimedOut,
    Abandoned
}
