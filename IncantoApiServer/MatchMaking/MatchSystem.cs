using System.Collections.Concurrent;
using Redis;

namespace MatchMaking;

public class MatchPlayers(List<string> pPlayers, int pStartIdx) {
	public const int MatchPerPlayer = 4;
	public readonly IReadOnlyCollection<string> Players = 
		pPlayers.Count - pStartIdx >= MatchPerPlayer 
			? pPlayers.Slice(pStartIdx, MatchPerPlayer)
			: throw new IndexOutOfRangeException(
				$"try to access {pStartIdx} ~ {pStartIdx + MatchPerPlayer} (size: {pPlayers.Count})"
			);
}

public class MatchSystem(RateLimitService pRl) {
	private readonly RateLimitService _rlService = pRl;
	private readonly ConcurrentQueue<string> _waitMatch = new();
	private readonly ConcurrentDictionary<string, bool> _waitStatus = new();

	public async Task<Result> Enter(string pMail, string pAuthGuid) {
		var loginToken = await _rlService.Get($"auth:{pMail}");
		if (loginToken.TTL == -2)
			return new(Status.Fail, "로그인 상태가 아닙니다. 로그인 후 다시 시도해 주세요.");
        
		if((string)loginToken.Value != pAuthGuid)
			return new(Status.Fail, "올바르지 않은 토큰입니다.");
		
		_waitMatch.Enqueue(pAuthGuid);
		_waitStatus[pAuthGuid] = true;
		await _rlService.ChangeTTL($"auth:{pMail}", Account.Account.LoginTokenExpire);
		return new(Status.Success, "정상적으로 매치에 참여했습니다.");
	}

	public void Exit(string pAuthGuid) {
		_waitStatus.TryRemove(pAuthGuid, out _);
	}

	public MatchPlayers[] Tick() {

		var playablePlayer = new List<string>();
		while (_waitMatch.TryDequeue(out var player)) {
			if(!_waitStatus.TryGetValue(player, out var v) || !v)
				continue;
			playablePlayer.Add(player);
			_waitStatus[player] = false;
		}

		var idx = 0;
		var generatedGroup = playablePlayer.Count / MatchPlayers.MatchPerPlayer;
		var result = new MatchPlayers[generatedGroup];
		for (int i = 0; i < generatedGroup; i++, idx += MatchPlayers.MatchPerPlayer) {
			result[i] = new MatchPlayers(playablePlayer, idx);
			for (int j = 0; j < MatchPlayers.MatchPerPlayer; j++)
				_waitStatus.Remove(playablePlayer[idx + j], out _);
		}

		for (; idx < playablePlayer.Count; idx++) {
			_waitMatch.Enqueue(playablePlayer[idx]);
			_waitStatus[playablePlayer[idx]] = true;
		}

		return result;
	}
}