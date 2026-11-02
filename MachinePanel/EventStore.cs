using Microsoft.Data.Sqlite;

namespace MachinePanel;

public class EventStore
{
    private readonly string _connectionString;
    private const string TABLE_NAME = "MachineEvents";

    public EventStore(string connectionString)
    {
        _connectionString = connectionString;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {TABLE_NAME}(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LoggedAt TEXT NOT NULL,
                EventType TEXT NOT NULL,
                Message TEXT NOT NULL
                )
            """;

        command.ExecuteNonQuery();
    }

    public void Add(DateTime loggedAt, string eventType, string message)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO {TABLE_NAME}(LoggedAt, EventType, Message)
            VALUES ($loggedAt, $eventType, $message)
            """;
        command.Parameters.AddWithValue("$loggedAt", loggedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$eventType", eventType);
        command.Parameters.AddWithValue("$message", message);
        command.ExecuteNonQuery();
    }

    public List<FaultSummary> GetFaultSummary()
    {
        var results = new List<FaultSummary>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT Message, COUNT(*) AS Times
            FROM {TABLE_NAME}
            WHERE EventType = $eventType
            GROUP BY Message
            ORDER BY Times DESC
            """;
        command.Parameters.AddWithValue("$eventType", EventTypes.Fault);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new FaultSummary(reader.GetString(0), reader.GetInt64(1)));
        }

        return results;
    }
}
