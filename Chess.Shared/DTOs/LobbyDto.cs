using Chess.Shared.Enums;

namespace Chess.Shared.DTOs;

/// <summary>
/// Represents a game listing in the lobby.
/// </summary>
public class LobbyGameDto
{
    public string GameId { get; set; } = string.Empty;
    public string CreatorDisplayName { get; set; } = string.Empty;
    public int TimeControlSeconds { get; set; }
    public int IncrementSeconds { get; set; }
    public GameStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Friendly label like "10+5" or "3+0".</summary>
    public string TimeControlLabel =>
        $"{TimeControlSeconds / 60}+{IncrementSeconds}";
}

/// <summary>
/// Request to create a new game in the lobby.
/// </summary>
public class CreateGameDto
{
    public int TimeControlSeconds { get; set; } = 600;
    public int IncrementSeconds { get; set; } = 0;
}
