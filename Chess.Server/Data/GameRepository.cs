using Microsoft.Azure.Cosmos;
using Chess.Shared.Enums;
using Chess.Shared.Models;

namespace Chess.Server.Data;

public class GameRepository
{
    private readonly CosmosClient _client;
    private readonly string _databaseId;
    private readonly string _containerId;
    private Container? _container;

    public GameRepository(CosmosClient client, IConfiguration config)
    {
        _client      = client;
        _databaseId  = config["Cosmos:DatabaseId"] ?? "KooxiChessDb";
        _containerId = config["Cosmos:ContainerId"] ?? "Games";
    }

    /// <summary>Creates the database and container if they don't exist. Called at startup.</summary>
    public async Task InitializeAsync()
    {
        var dbResponse = await _client.CreateDatabaseIfNotExistsAsync(_databaseId);
        var containerResponse = await dbResponse.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_containerId, "/GameId"));
        _container = containerResponse.Container;
    }

    public async Task UpsertAsync(GameState state)
    {
        var doc = GameDocument.FromGameState(state);
        await _container!.UpsertItemAsync(doc, new PartitionKey(doc.GameId));
    }

    public async Task<GameState?> GetAsync(string gameId)
    {
        try
        {
            var response = await _container!.ReadItemAsync<GameDocument>(gameId, new PartitionKey(gameId));
            return response.Resource.ToGameState();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<GameState>> GetByStatusAsync(GameStatus status)
    {
        var query = _container!.GetItemQueryIterator<GameDocument>(
            new QueryDefinition("SELECT * FROM c WHERE c.Status = @status")
                .WithParameter("@status", (int)status));

        var results = new List<GameState>();
        while (query.HasMoreResults)
        {
            var page = await query.ReadNextAsync();
            results.AddRange(page.Select(d => d.ToGameState()));
        }
        return results;
    }

    public async Task<IEnumerable<GameState>> GetAllAsync()
    {
        var query = _container!.GetItemQueryIterator<GameDocument>(
            new QueryDefinition("SELECT * FROM c ORDER BY c.CreatedAt DESC"));

        var results = new List<GameState>();
        while (query.HasMoreResults)
        {
            var page = await query.ReadNextAsync();
            results.AddRange(page.Select(d => d.ToGameState()));
        }
        return results;
    }
}
