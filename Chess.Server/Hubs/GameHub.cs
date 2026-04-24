using Microsoft.AspNetCore.SignalR;
using Chess.Shared.Constants;
using Chess.Shared.DTOs;

namespace Chess.Server.Hubs;

public class GameHub : Hub
{
    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    }

    public async Task MakeMove(MoveDto move)
    {
        // TODO: validate move via MoveValidatorService, then broadcast result
        await Clients.Group(move.GameId).SendAsync(HubMethods.MoveMade, move);
    }

    public async Task Resign(string gameId)
    {
        await Clients.Group(gameId).SendAsync(HubMethods.GameOver, new { Reason = "Resignation" });
    }

    public async Task OfferDraw(string gameId)
    {
        await Clients.OthersInGroup(gameId).SendAsync(HubMethods.DrawOffered);
    }

    public async Task AcceptDraw(string gameId)
    {
        await Clients.Group(gameId).SendAsync(HubMethods.GameOver, new { Reason = "Draw" });
    }

    public async Task DeclineDraw(string gameId)
    {
        await Clients.OthersInGroup(gameId).SendAsync(HubMethods.DrawDeclined);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
