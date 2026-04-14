using Account;
using IncantoApiServer.UpdateLogic;
using MatchMaking;
using Microsoft.AspNetCore.Mvc;
using Redis;
using StackExchange.Redis;

public class Program {

	public static void ApiSetUp(WebApplication pApp) {
		pApp.MapGet("/", () => "Hello?")
			.WithName("GetWeatherForecast");
		pApp.MapPost("/2fa", async (Account.Account pAccount, SignUpInfo pInfo) =>
			await pAccount.SendMail(pInfo.Mail));
		pApp.MapPost("/signIn", async (Account.Account pAccount, SignUpInfo pInfo) =>
			await pAccount.SignIn(pInfo.Mail, pInfo.PassWord));
		pApp.MapPost("/signUp", async (Account.Account pAccount, SignUpInfo pInfo ) =>
			await pAccount.SignUp(pInfo.Name, pInfo.Mail, pInfo.PassWord, pInfo.TwoFactorAuth));
		pApp.MapPost("/JoinMatch", async (MatchSystem pMatch, AccountToken pToken) =>
			await pMatch.Enter(pToken));
		pApp.MapPost("/ExitMatch", (MatchSystem pMatch, AccountToken pToken) =>
			pMatch.Exit(pToken));
	}

	public static void SetSingleton(WebApplicationBuilder pBuilder) {
		pBuilder.Services.AddSingleton<UpdateManager>();
		pBuilder.Services.AddSingleton<RateLimitService>(provider => {
			var redis = ConnectionMultiplexer.Connect(
				new ConfigurationOptions {
					EndPoints = {
						{ Setting.Setting.Get("RedisEndPoint"), int.Parse(Setting.Setting.Get("RedisPort")) }
					},
					User = "default",
					Password = Setting.Setting.Get("RedisPassword")
				}
			);
			return new RateLimitService(redis.GetDatabase());
		});	
		pBuilder.Services.AddSingleton<Account.Account>();
		pBuilder.Services.AddSingleton<MatchSystem>();
		pBuilder.Services.AddHostedService<UpdateLoop>();
	}
    
	public static void Main(string[] args) {
		Setting.Setting.Setup();
		var builder = WebApplication.CreateBuilder(args);
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();
		
		SetSingleton(builder);
		var app = builder.Build();

		app.UseSwagger();
		app.UseSwaggerUI();
		
		if (app.Environment.IsDevelopment()) {
		}

		ApiSetUp(app);
        
		app.Run();
	}
}