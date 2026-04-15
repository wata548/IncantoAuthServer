using System.Net;
using System.Security.Cryptography.X509Certificates;
using Account;
using IncantoApiServer.UpdateLogic;
using MatchMaking;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Pkcs;
using Redis;
using StackExchange.Redis;

public class Program {

	private static void ApiSetUp(WebApplication pApp) {
		pApp.MapGet("/", () => "Hello?")
			.WithName("GetWeatherForecast");
		pApp.MapPost("/2fa", async (Account.Account pAccount, SignUpInfo pInfo) =>
			await pAccount.SendMail(pInfo.Mail));
		pApp.MapPost("/SignIn", async (Account.Account pAccount, SignUpInfo pInfo) =>
			await pAccount.SignIn(pInfo.Mail, pInfo.PassWord));
		pApp.MapPost("/SignUp", async (Account.Account pAccount, SignUpInfo pInfo ) =>
			await pAccount.SignUp(pInfo.Name, pInfo.Mail, pInfo.PassWord, pInfo.TwoFactorAuth));
		pApp.MapPost("/JoinMatch", async (MatchSystem pMatch, AccountToken pToken) =>
			await pMatch.Enter(pToken));
		pApp.MapPost("/ExitMatch", (MatchSystem pMatch, AccountToken pToken) =>
			pMatch.Exit(pToken));
	}

	private static void SetSingleton(WebApplicationBuilder pBuilder) {
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

	private static void Certification(WebApplicationBuilder pBuilder) {

		pBuilder.WebHost.ConfigureKestrel(options => {
			options.Listen(IPAddress.Any, 7272, listenOptions =>
				listenOptions.UseHttps("Certification/certificate.pfx", Setting.Setting.Get("CertPassword"))
			);
		});
	}
    
	public static void Main(string[] args) {
		Setting.Setting.Setup();
		var builder = WebApplication.CreateBuilder(args);
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();
		
		Certification(builder);
		SetSingleton(builder);

		builder.Services.ConfigureHttpJsonOptions(options => 
			options.SerializerOptions.PropertyNamingPolicy = null
		);
		
		var app = builder.Build();

		app.UseSwagger();
		app.UseSwaggerUI();
		
		if (app.Environment.IsDevelopment()) {
		}

		ApiSetUp(app);
        
		app.Run();
	}
}