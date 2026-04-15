namespace Account;
using Redis;
using MySql.Data.MySqlClient;

public partial class Account {
    public async Task<Result> SignUp(string pName, string pMail, string pPassword, string p2fa) {

        const string CntCommand = "SELECT COUNT(*) FROM USERS WHERE Mail = @mail";
        const string InsertCommand = "INSERT INTO USERS(NAME, MAIL, PASSWORD) VALUES (@name, @mail, @password)";
        
        await using var connection = new MySqlConnection(Setting.Setting.Get("DBConnect"));
        await connection.OpenAsync();
        
        var command = new MySqlCommand(CntCommand, connection);
        command.Parameters.AddWithValue("@mail", pMail);

        var cnt = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (cnt != 0)
            return new(Status.Fail, "이미 등록된 메일 주소입니다.");
        
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
        
        pPassword = BCrypt.Net.BCrypt.HashPassword(pPassword);
        await using var insertCommand = new MySqlCommand(InsertCommand, connection);
        insertCommand.Parameters.AddWithValue("@name", pName);
        insertCommand.Parameters.AddWithValue("@mail", pMail);
        insertCommand.Parameters.AddWithValue("@password", pPassword);
        if (await insertCommand.ExecuteNonQueryAsync() == 1) {
            await _rlService.Remove($"rl:SignUp:{pMail}");
            return new(Status.Success, "성공적으로 회원가입되었습니다.");
        }
        return new(Status.Fail, "회원가입에 실패했습니다.");
    }
}