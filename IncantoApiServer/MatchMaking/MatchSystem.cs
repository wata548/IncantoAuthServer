using System.Collections.Concurrent;
using System.Text;
using Account;
using IncantoApiServer.LogicServerConnection;
using IncantoApiServer.UpdateLogic;
using Match;
using Newtonsoft.Json;
using Redis;

namespace MatchMaking;

using System.Text;
using System.Text.Json;

public class MatchSystem: UpdateModule {
	
	private readonly RateLimitService _rlService;
	private readonly LogicServerConnection _logicServer;
	private readonly ConcurrentQueue<int> _waitMatch = new();
	private readonly ConcurrentDictionary<int, bool> _waitStatus = new();
	private readonly ConcurrentDictionary<int, bool> _playingGame = new();

	public MatchSystem(RateLimitService pRl, UpdateManager pManager, LogicServerConnection pLogicServer): base(pManager) {
		_rlService = pRl;
		_logicServer = pLogicServer;
		_logicServer.Init();
		_logicServer.OnMatchEnd += End;
	}

	private void End(IEnumerable<MatchPlayerResult> pPlayers) {
		foreach (var player in pPlayers) {
			_playingGame[player.Idx] = false;
		}
	} 

	public async Task<Result> Enter(AccountToken pToken) {
		var loginToken = await _rlService.Get($"auth:{pToken.Mail}");
		
		if (loginToken.TTL == -2)
			return new(Status.Fail, "로그인 상태가 아닙니다. 로그인 후 다시 시도해 주세요.");
		if (_waitStatus.TryGetValue(pToken.Id, out var isWait) && isWait)
			return new(Status.Fail, "이미 대기 중입니다.");

		if (_playingGame.TryGetValue(pToken.Id, out var isPlaying) && isPlaying)
			return new(Status.Success, "재접속 시도합니다.");
        
		if((string)loginToken.Value != pToken.Guid)
			return new(Status.Fail, "올바르지 않은 토큰입니다.");
		
		_waitMatch.Enqueue(pToken.Id);
		_waitStatus[pToken.Id] = true;
		await _rlService.ChangeTTL($"auth:{pToken.Mail}", Account.Account.LoginTokenExpire);
		return new(Status.Success, "정상적으로 매치에 참여했습니다.");
	}

	public Result Exit(AccountToken pToken) =>
		_waitStatus.TryRemove(pToken.Id, out _)
			? new(Status.Success, "정상적으로 매치에서 나왔습니다.")
			: new(Status.Fail, "매치에서 나오는 데 실패했습니다. 다시 시도해주세요.");

	public async Task<MatchPlayers[]> Tick() {

		if (_waitMatch.Count < MatchPlayers.MatchPerPlayer)
			return [];
		
		var playablePlayer = new List<int>();
		while (_waitMatch.TryDequeue(out var player)) {
			if(!_waitStatus.TryGetValue(player, out var v) || !v)
				continue;
			playablePlayer.Add(player);
		}

		var idx = 0;
		var generatedGroup = playablePlayer.Count / MatchPlayers.MatchPerPlayer;
		var result = new MatchPlayers[generatedGroup];
		for (int i = 0; i < generatedGroup; i++, idx += MatchPlayers.MatchPerPlayer) {
			Console.WriteLine("Make");
			result[i] = new MatchPlayers(playablePlayer, idx);
			for (int j = 0; j < MatchPlayers.MatchPerPlayer; j++) {
				_playingGame.TryAdd(playablePlayer[idx + j], true);
				_waitStatus.Remove(playablePlayer[idx + j], out _);
			}
		}

		for (; idx < playablePlayer.Count; idx++) {
			_waitMatch.Enqueue(playablePlayer[idx]);
		}

		await Task.WhenAll(
			result.Select(match => _logicServer.SendMatchData(match))
		);
		SaveMatchInfo.SaveMatches(result);
		Console.WriteLine("Generation End");

		return result;
	}

	public async override Task Update() {
		var match = Tick();
	}
}