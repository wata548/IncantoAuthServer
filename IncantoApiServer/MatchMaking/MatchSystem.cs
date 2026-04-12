using System.Collections.Concurrent;
using Redis;

namespace MatchMaking;

public class MatchSystem(RateLimitService pRl) {
    private readonly RateLimitService _rlService = pRl;
    private readonly ConcurrentQueue<string> _waitMatch = new();

    public async Task<Result> EnterMatch(string pAuthGuid) {
        var loginToken = await _rlService.Get($"auth:{pAuthGuid}");
        if (loginToken.TTL == -2)
            return new(Status.Fail, "로그인 상태가 아닙니다. 로그인 후 다시 시도해 주세요.");
        
        _waitMatch.Enqueue(pAuthGuid);
        await _rlService.Add($"match:wait:{pAuthGuid}", "", TimeSpan.FromMinutes(10));
        await _rlService.ChangeTTL($"auth:{pAuthGuid}", Account.Account.LoginTokenExpire);
        return new(Status.Success, "정상적으로 매치에 참여했습니다.");
    }

    public async Task Tick() {
        while (_waitMatch.Count > 0) {
            if(!_waitMatch.TryDequeue(out var value))
                continue;
            var playerInfo = await _rlService.Get($"match:wait:{value}");
            if(playerInfo.TTL == -2)
                continue;
        }
    }
}