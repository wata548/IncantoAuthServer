namespace Account;
using Redis;
using MySql.Data.MySqlClient;

public partial class Account {
	public static readonly TimeSpan LoginTokenExpire = TimeSpan.FromMinutes(90);
    
	public async Task<Result> SignIn(string pMail, string pPassword) {
		var rl = await _rlService.CheckAndIncrement($"rl:SignIn:{pMail}",
			5,
			TimeSpan.FromHours(1)
		);
		if (!rl.Allow)
			return new(Status.Fail, "잠시 후에 시도해 주세요");

		const string commandString = "SELECT PASSWORD, NAME FROM USERS WHERE MAIL = @mail";
		await using var connection = new MySqlConnection(Setting.Setting.Get("DBConnect"));
		await connection.OpenAsync();
		await using var command = new MySqlCommand(commandString, connection);
		command.Parameters.AddWithValue("@mail", pMail);
		await using var reader = await command.ExecuteReaderAsync();
		//DummyHash
		var password = "$2a$12$25YNmW5kGcHjN06LSf9n9.FHqu7gHRiPXgaZ1/hlW/5FkIPKDI4.m";
		var name = "PlaceHolder";
		if (await reader.ReadAsync()) {
			password = reader.GetString(0);
			name = reader.GetString(1);
		}
		
		if (!BCrypt.Net.BCrypt.Verify(pPassword, password))
			return new(Status.Fail, "이메일 또는 비밀번호가 잘못되었습니다.");

		await _rlService.Remove($"rl:SignIn:{pMail}");
		await _rlService.Remove($"2fa:MailCheck:{pMail}");
		var guid = Guid.NewGuid().ToString();
		await _rlService.Add($"auth:{pMail}", guid, LoginTokenExpire);

		return new Result<AccountToken>(Status.Success, new() {
			Name = name,
			Mail = pMail,
			Guid = guid
		});
	}
}