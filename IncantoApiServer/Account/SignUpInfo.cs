namespace Account;

public class SignUpInfo {
	public string Name { get; set; }
	public string Mail { get; set; }
	public string PassWord { get; set; }
	//2fa
	public string TwoFactorAuth { get; set; }
}

public class AccountToken {
	public int Id { get; set; }
	public string Name { get; set; }
	public string Mail { get; set; }
	public string Guid { get; set; }
}