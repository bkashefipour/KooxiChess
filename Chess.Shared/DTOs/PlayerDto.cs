using Chess.Shared.Enums;

namespace Chess.Shared.DTOs;

public class PlayerDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PieceColor Color { get; set; }
    public bool IsConnected { get; set; }
}
