using Account;
using MatchMaking;
using Redis;
using StackExchange.Redis;

public class Program {

	public static void ApiSetUp(WebApplication pApp) {
		Setting.Setting.Setup();
		var redis = ConnectionMultiplexer.Connect(
			new ConfigurationOptions{
				EndPoints= { {Setting.Setting.Get("RedisEndPoint"), int.Parse(Setting.Setting.Get("RedisPort"))} },
				User="default",
				Password=Setting.Setting.Get("RedisPassword")
			}
		);
		var rlService = new RateLimitService(redis.GetDatabase());

		var account = new Account.Account(rlService);
		var match = new MatchSystem(rlService);
        
		pApp.MapGet("/", () => "Hello?")
			.WithName("GetWeatherForecast");
		pApp.MapPost("/2fa", async (SignUpInfo pInfo) =>
			await account.SendMail(pInfo.Mail));
		pApp.MapPost("/signIn", async (SignUpInfo pInfo) =>
			await account.SignIn(pInfo.Mail, pInfo.PassWord));
		pApp.MapPost("/signUp", async (SignUpInfo pInfo) =>
			await account.SignUp(pInfo.Name, pInfo.Mail, pInfo.PassWord, pInfo.TwoFactorAuth));
		pApp.MapPost("/test", async (string pMail, string pGuid) =>
			await match.Enter(pMail, pGuid));
	}
    
	public static void Main(string[] args) {
		var builder = WebApplication.CreateBuilder(args);
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();
		
		//builder.Services.AddHostedService<>()
		var app = builder.Build();

		app.UseSwagger();
		app.UseSwaggerUI();
        
		if (app.Environment.IsDevelopment()) {
		}

		app.UseHttpsRedirection();
		ApiSetUp(app);
        
		app.Run();
	}
}