using MySql.Data.MySqlClient;
using Setting;

namespace Match;

public static class SaveMatchInfo {
    static readonly int[] SocreByRank = [-30, -20, 30, 50]; 
    public static async Task SaveMatches(MatchPlayers[] pMatches) {
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

    public static async Task SaveMatchResult(MatchResult pResult) {

        const string ResultUpdateCommand = "INSERT INTO result_match (PLAYER_ID, ALIVE_TIME, RANKING, MATCH_ID) VALUES ";
        const string RatingUpdateCommand = "UPDATE users SET RATING = GREATEST(0, RATING + @Delta) WHERE ID = @Idx";
        const string Args = "(@Player{0}, @Alive{0}, @Rank{0}, @Match{0})";
        var args = String.Join(',', 
            Enumerable.Range(0, MatchPlayers.MatchPerPlayer)
                .Select(idx => string.Format(Args, idx))
        );
        var addResultCommand = ResultUpdateCommand + args;
        await using var connection = new MySqlConnection(Env.Get("DBConnect"));
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await using var setResultCommand = new MySqlCommand(addResultCommand, connection, transaction);
        await using var updateRatingCommand = new MySqlCommand(RatingUpdateCommand, connection, transaction);
        updateRatingCommand.Parameters.Add("@Delta", MySqlDbType.Int32);
        updateRatingCommand.Parameters.Add("@Idx", MySqlDbType.Int32);
        
        var idx = -1;
        foreach (var player in pResult.Rank) {
            idx++;
            updateRatingCommand.Parameters["@Delta"].Value = SocreByRank[idx];
            updateRatingCommand.Parameters["@Idx"].Value = player.Idx;
            await updateRatingCommand.ExecuteNonQueryAsync();
            
            setResultCommand.Parameters.AddWithValue($"@Player{idx}", player.Idx);
            setResultCommand.Parameters.AddWithValue($"@Alive{idx}", player.AliveTime);
            setResultCommand.Parameters.AddWithValue($"@Rank{idx}", idx + 1);
            setResultCommand.Parameters.AddWithValue($"@Match{idx}", pResult.MatchUUID.ToByteArray());
        }
        await setResultCommand.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }
    
}