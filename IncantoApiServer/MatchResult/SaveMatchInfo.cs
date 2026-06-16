using MySql.Data.MySqlClient;
using Setting;

namespace Match;

public static class SaveMatchInfo {
    public static async void SaveMatches(MatchPlayers[] pMatches) {
        const string Command = "INSERT INTO MATCH_INFO (UUID) VALUES (@UUID)";
        await using var connection = new MySqlConnection(Env.Get("DBConnect"));
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        
        await using var command = new MySqlCommand(Command, connection, transaction);
        command.Parameters.Add("@UUID", MySqlDbType.Binary, 16);
        foreach (var match in pMatches) {
            command.Parameters["@UUID"].Value = match.UUID.ToByteArray();
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }
    
}