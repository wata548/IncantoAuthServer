using Redis;

namespace Account;

public partial class Account {
    private Random _random = new();
    private Task<RateLimit> GetRLMailCheck(string pAddress) =>
        _rlService.CheckAndIncrement(
            $"rl:MailCheck:{pAddress}", 
            5, 
            TimeSpan.FromMinutes(10)
        );
    
    public async Task<Result> SendMail(string pAddress) {
        var rateLimit = await GetRLMailCheck(pAddress);
        if (!rateLimit.Allow) {
            Console.WriteLine("to much try email");
            return new(Status.Fail, "잠시 후에 시도해주세요.");
        }
        
        var code = _random.Next(0, 1000000);
        var codeString = code.ToString().PadLeft(6, '0');
        await _rlService.Add(
            $"2fa:MailCheck:{pAddress}",
            codeString,
            TimeSpan.FromMinutes(10)
        );
        
        await Extension.Mail.Send(pAddress, "인증 정보", $"\"{codeString}\"을 입력창 작성해주세요.");
        return new(Status.Success, "성공적으로 전송되었습니다.");
    }
}