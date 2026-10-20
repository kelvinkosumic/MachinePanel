using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MachinePanel;

public class EventStore
{
    private readonly string _connectionString;

    public EventStore()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "machine.db");
        _connectionString = $"Data Source={dbPath}";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS MachineEvents(
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
        command.CommandText = """
            INSERT INTO MachineEvents(LoggedAt, EventType, Message)
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
        command.CommandText = """
            SELECT Message, COUNT(*) AS Times
            FROM MachineEvents
            WHERE EventType = @eventType
            GROUP BY Message
            ORDER BY Times DESC
            """;
        command.Parameters.AddWithValue("@eventType", EventTypes.Fault);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new FaultSummary(reader.GetString(0), reader.GetInt64(1)));
        }

        return results;
    }
}
