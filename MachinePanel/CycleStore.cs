using Microsoft.Data.Sqlite;

namespace MachinePanel;

public class CycleStore
{
    private readonly string _connectionString;
    private const string TABLE_NAME = "ProductionCycles";
    public CycleStore(string connectionString)
    {
        _connectionString = connectionString;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
                CREATE TABLE IF NOT EXISTS {TABLE_NAME}(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CompletedAt TEXT NOT NULL,
                PartNumber INTEGER NOT NULL,
                CycleTimeMs INTEGER NOT NULL
                )
            """;

        command.ExecuteNonQuery();
    }

    public void Add(DateTime completedAt, int partNumber, int cycleTime)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
                INSERT INTO {TABLE_NAME}(CompletedAt, PartNumber, CycleTimeMs)
                VALUES ($completedAt, $partNumber, $cycleTimeMs)
            """;

        command.Parameters.AddWithValue("$completedAt", completedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$partNumber", partNumber);
        command.Parameters.AddWithValue("$cycleTimeMs", cycleTime);
        command.ExecuteNonQuery();
    }

    public CycleSummary GetCycleSummary()
    {
        CycleSummary summary = new CycleSummary(0, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT COUNT(*) AS TotalCycles, AVG(CycleTimeMs) AS AverageCycleTime, MIN(CycleTimeMs) AS MinCycleTime, MAX(CycleTimeMs) AS MaxCycleTime
            FROM {TABLE_NAME}
            """;

        using var reader = command.ExecuteReader();


        if(reader.Read())
        {
            if (reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3)) return summary;

            summary = new CycleSummary(
                reader.GetInt64(0),
                TimeSpan.FromMilliseconds(reader.GetDouble(1)),
                TimeSpan.FromMilliseconds(reader.GetDouble(2)),
                TimeSpan.FromMilliseconds(reader.GetDouble(3))
            );
        }

        return summary;
    }
}
