namespace Account;
using Redis;
using MySql.Data.MySqlClient;

public partial class Account {
    public async Task<Result> SignUp(string pName, string pMail, string pPassword, string p2fa) {
        
        var rl = _rlService.CheckAndIncrement($"rl:SignUp:{pMail}",
            10,
            TimeSpan.FromHours(1)
        );    
        
        var token = await _rlService.Get($"2fa:MailCheck:{pMail}");
        if (token.TTL == -2)
            return new(Status.Fail, "2차 인증을 완료해 주세요.");
        if ((string)token.Value != p2fa)
            return new(Status.Fail, "2차 인증 번호가 틀렸습니다.");
        
        var available = await AlreadyRegistered(pMail);
        if (available.Status != Status.Success)
            return available;
        
        await using var connection = new MySqlConnection(Setting.Setting.Get("DBConnect"));
        connection.Open();
        pPassword = BCrypt.Net.BCrypt.HashPassword(pPassword);
        var commandString = $"INSERT INTO USERS(NAME, EMAIL, PASSWORD) VALUES ('{pName}', '{pMail}', '{pPassword}')";
        var command = new MySqlCommand(commandString, connection);
        if (await command.ExecuteNonQueryAsync() == 1) {
            await _rlService.Remove($"rl:SignUp:{pMail}");
            return new(Status.Success, "성공적으로 회원가입되었습니다.");
        }

        return new(Status.Fail, "회원가입에 실패했습니다.");
    }
}