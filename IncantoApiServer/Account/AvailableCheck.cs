namespace Account;
using Redis;
using MySql.Data.MySqlClient;

public partial class Account (RateLimitService pRl) {
    
    private RateLimitService _rlService = pRl;
    
    private Task<RateLimit> GetRLAvailableMail(string pIp) =>
        _rlService.CheckAndIncrement(
            $"rl:CheckAvailableMail:{pIp}",
            20,
            TimeSpan.FromMinutes(3)
        );
    
    public async Task<Result> AlreadyRegistered(string pIp, string pMail) {

        var rl = await GetRLAvailableMail(pIp);
        if (!rl.Allow)
            return new(Status.OutLimit, "잠시 후에 다시 시도해주세요.");
        return await AlreadyRegistered(pMail);
    }

    private async Task<Result> AlreadyRegistered(string pMail) {
        const string commandString = "SELECT * FROM USERS WHERE MAIL = @email LIMIT 1";
        await using var connection = new MySqlConnection(Setting.Setting.Get("DBConnect"));
        await connection.OpenAsync();
        await using var command = new MySqlCommand(commandString, connection);
        command.Parameters.AddWithValue("@email", pMail);
        var value = await command.ExecuteScalarAsync();
        return value == null
            ? new(Status.Success, "사용할 수 있는 메일 주소입니다.")
            : new(Status.Fail, "이 메일은 사용할 수 없습니다.");
    }
}