using Chess.Shared.Enums;

namespace Chess.Shared.Models;

/// <summary>
/// Represents a player in the chess game.
/// Maps to an Azure AD identity.
/// </summary>
public class Player
{
    /// <summary>Azure AD Object ID (from the JWT sub/oid claim).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Display name shown in the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional email (from Azure AD claims).</summary>
    public string? Email { get; set; }

    /// <summary>SignalR connection ID for the current session.</summary>
    public string? ConnectionId { get; set; }

    /// <summary>The colour assigned to this player in the game.</summary>
    public PieceColor? AssignedColor { get; set; }

    /// <summary>Whether the player is currently connected.</summary>
    public bool IsConnected { get; set; }

    public override string ToString() => DisplayName;
}
