namespace Account;

public class SignUpInfo {
	public string Name { get; set; }
	public string Mail { get; set; }
	public string PassWord { get; set; }
	//2fa
	public string TwoFactorAuth { get; set; }
}

public class AccountToken(string pName, string pMail, string pGuid) {
	public string Name { get; set; } = pName;
	public string Mail { get; set; } = pMail;
	public string Guid { get; set; }= pGuid;
}