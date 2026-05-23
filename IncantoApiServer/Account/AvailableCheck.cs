using Setting;

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

	private Task RegisterCheckRemove(string pIp) =>
		_rlService.Remove($"rl:CheckAvailableMail:{pIp}");
	
	public async Task<Result> AlreadyRegistered(string pIp, string pMail) {
		var rl = await GetRLAvailableMail(pIp);
		if (!rl.Allow)
			return new(Status.Fail, "잠시 후에 다시 시도해주세요.");
		await using var connection = new MySqlConnection(Env.Get("DBConnect"));
		await connection.OpenAsync();	
		return await AlreadyRegistered(pMail, connection);
	}

	private async Task<Result> AlreadyRegistered(string pMail, MySqlConnection pConnection) {
		const string commandString = "SELECT COUNT(*) FROM USERS WHERE MAIL = @email";
		await using var command = new MySqlCommand(commandString, pConnection);
		command.Parameters.AddWithValue("@email", pMail);
		var cnt = Convert.ToInt32(await command.ExecuteScalarAsync());
		return cnt == 0 
			? new(Status.Success, "사용할 수 있는 메일 주소입니다.")
			: new(Status.Fail, "이 메일은 사용할 수 없습니다.");
	}
}