using Microsoft.Data.Sqlite;
namespace CaseApi;

public class CaseRepository(string connectionString)
{
    private readonly string _connectionString = connectionString;

    public long Count()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM cases;";

        return (long)command.ExecuteScalar()!;
    }

    public List<CaseResponse> List()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, title, status FROM cases ORDER BY id;";

        List<CaseResponse> cases = [];
        using var reader = command.ExecuteReader();

        while(reader.Read())
        {
            string id = reader.GetString(0);
            string title = reader.GetString(1);
            string status = reader.GetString(2);

            CaseResponse response = new(id, title, status);
            cases.Add(response);
        }

        return cases;
    }

    public CaseResponse? Get(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, title, status FROM cases WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        string newId = reader.GetString(0);
        string title = reader.GetString(1);
        string status = reader.GetString(2);

        CaseResponse response = new(newId, title, status);
        return response;
    }

    public CaseResponse Create(CreateCaseRequest request)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        string id = Guid.NewGuid().ToString();
        string title = request.Title!.Trim();
        string status = "New";

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO cases (id, title, status) VALUES ($id, $title, $status);";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$status", status);
        int rowsAffected = command.ExecuteNonQuery();

        CaseResponse caseResponse = new(id, title, status);
        return caseResponse;
    }
}