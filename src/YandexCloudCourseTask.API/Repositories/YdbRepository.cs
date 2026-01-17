using YandexCloudCourseTask.API.Models;
using Ydb.Sdk;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Value;
using Ydb.Sdk.Yc;

namespace YandexCloudCourseTask.API.Repositories;

public class YdbRepository : IAsyncDisposable
{
    private readonly Driver _driver;
    private readonly TableClient _tableClient;

    public YdbRepository(IConfiguration configuration)
    {
        var metadataProvider = new MetadataProvider();

        var driverConfig = new DriverConfig(
            endpoint: configuration["DB_ENDPOINT"] ?? string.Empty,
            database: configuration["DB_NAME"] ?? string.Empty,
            credentials: metadataProvider
        );

        _driver = new Driver(driverConfig);
        _tableClient = new TableClient(_driver);
    }

    public async Task Initialize()
        => await _driver.Initialize();

    public async Task CreateSchema()
    {
        const string query =
            """
            CREATE TABLE messages (
                id Utf8,
                username Utf8,
                content Utf8,
                created_at Timestamp,
                PRIMARY KEY (id)
            );
            """;

        try
        {
            await _tableClient.SessionExec(async session =>
                await session.ExecuteSchemeQuery(query));
            Console.WriteLine("Schema created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Schema creation failed. {ex.Message}");
        }
    }

    public async Task<List<Message>> GetMessages(ulong limit = 20)
    {
        const string query =
            """
            DECLARE $limit AS Uint64;

            SELECT id, username, content, created_at 
            FROM messages 
            ORDER BY created_at DESC 
            LIMIT $limit;
            """;

        var parameters = new Dictionary<string, YdbValue>
        {
            { "$limit", YdbValue.MakeUint64(limit) }
        };

        var response = await _tableClient.SessionExec(async session =>
            await session.ExecuteDataQuery(
                query: query,
                txControl: TxControl.BeginSerializableRW().Commit(),
                parameters: parameters
            ));

        response.Status.EnsureSuccess();

        var resultSet = ((ExecuteDataQueryResponse)response).Result.ResultSets[0];

        return (from row in resultSet.Rows
            let id = (string?)row["id"] ?? Guid.Empty.ToString()
            let username = (string?)row["username"] ?? "Anonymous"
            let content = (string?)row["content"] ?? ""
            let createdAt = (DateTime?)row["created_at"] ?? DateTime.UtcNow
            select new Message(id, username, content, createdAt)).ToList();
    }

    public async Task AddMessage(string username, string content)
    {
        const string query =
            """
            DECLARE $id AS Utf8;
            DECLARE $username AS Utf8;
            DECLARE $content AS Utf8;
            DECLARE $created AS Timestamp;

            UPSERT INTO messages (id, username, content, created_at)
            VALUES ($id, $username, $content, $created);
            """;

        var parameters = new Dictionary<string, YdbValue>
        {
            { "$id", YdbValue.MakeUtf8(Guid.NewGuid().ToString()) },
            { "$username", YdbValue.MakeUtf8(username) },
            { "$content", YdbValue.MakeUtf8(content) },
            { "$created", YdbValue.MakeTimestamp(DateTime.UtcNow) }
        };

        var response = await _tableClient.SessionExec(async session =>
            await session.ExecuteDataQuery(
                query: query,
                txControl: TxControl.BeginSerializableRW().Commit(),
                parameters: parameters
            ));

        response.Status.EnsureSuccess();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        _tableClient.Dispose();
        await _driver.DisposeAsync();
    }
}