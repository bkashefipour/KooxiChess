namespace Chess.Shared.Constants;

/// <summary>
/// SignalR hub method names. Shared between client and server to prevent
/// magic-string mismatches at runtime.
/// </summary>
public static class HubMethods
{
    // ── Client → Server (invocations) ──────────────
    public const string JoinGame = "JoinGame";
    public const string MakeMove = "MakeMove";
    public const string Resign = "Resign";
    public const string OfferDraw = "OfferDraw";
    public const string AcceptDraw = "AcceptDraw";
    public const string DeclineDraw = "DeclineDraw";

    // ── Server → Client (broadcasts) ──────────────
    public const string GameStarted = "GameStarted";
    public const string MoveMade = "MoveMade";
    public const string MoveRejected = "MoveRejected";
    public const string GameOver = "GameOver";
    public const string ClockUpdate = "ClockUpdate";
    public const string OpponentConnected = "OpponentConnected";
    public const string OpponentDisconnected = "OpponentDisconnected";
    public const string DrawOffered = "DrawOffered";
    public const string DrawDeclined = "DrawDeclined";
}

/// <summary>
/// API route constants.
/// </summary>
public static class ApiRoutes
{
    public const string HubPath = "/hubs/game";
    public const string GamesController = "api/games";
    public const string LobbyController = "api/lobby";
}
