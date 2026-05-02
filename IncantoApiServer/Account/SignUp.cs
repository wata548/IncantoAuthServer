namespace Account;
using Redis;
using MySql.Data.MySqlClient;

public partial class Account {
    public async Task<Result> SignUp(string pIp, string pName, string pMail, string pPassword, string p2fa) {

        const string InsertCommand = "INSERT INTO USERS(NAME, MAIL, PASSWORD) VALUES (@name, @mail, @password)";

        if (pName.Length < 3)
            return new(Status.Fail, "이름이 너무 짧습니다.(최소 3)");
        if (pPassword.Length < 8)
            return new(Status.Fail, "비밀번호가 너무 짧습니다.(최소 8)");
        
        var rl = await _rlService.CheckAndIncrement($"rl:SignUp:{pMail}",
            10,
            TimeSpan.FromHours(1)
        );
        if (!rl.Allow)
            return new(Status.Fail, "잠시 후 다시 시도해 주세요.");
        
        var token = await _rlService.Get($"2fa:MailCheck:{pMail}");
        if (token.TTL == -2)
            return new(Status.Fail, "2차 인증을 완료해 주세요.");
        if ((string)token.Value != p2fa)
            return new(Status.Fail, "2차 인증 번호가 틀렸습니다.");
        
        var available = await AlreadyRegistered(pIp, pMail);
        if (available.Status != Status.Success)
            return available;
        
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(pPassword);
        
        await using var connection = new MySqlConnection(Setting.Setting.Get("DBConnect"));
        await connection.OpenAsync();
        await using var insertCommand = new MySqlCommand(InsertCommand, connection);
        insertCommand.Parameters.AddWithValue("@name", pName);
        insertCommand.Parameters.AddWithValue("@mail", pMail);
        insertCommand.Parameters.AddWithValue("@password", hashedPassword);
        if (await insertCommand.ExecuteNonQueryAsync() == 1) {
            await _rlService.Remove($"rl:SignUp:{pMail}");
            await _rlService.Remove($"2fa:MailCheck:{pMail}");
            await RegisterCheckRemove(pIp);
            
            return await SignIn(pMail, pPassword);
        }
        return new(Status.Fail, "회원가입에 실패했습니다.");
    }
}