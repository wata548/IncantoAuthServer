using System.Net;
using System.Net.Sockets;
using Match;
using MySql.Data.MySqlClient;
using Setting;

namespace IncantoApiServer.LogicServerConnection;

public class LogicServerConnection {
    public event Action<IEnumerable<MatchPlayerResult>> OnMatchEnd;
    
    private readonly IPAddress _logicIp = IPAddress.Parse(Env.Get("LogicServerIp"));
    private readonly int _logicPort = int.Parse(Env.Get("LogicServerPort"));
    private readonly int _receivePort = int.Parse(Env.Get("Port"));
    private bool _inited = false;

    public void Init() {
        if (_inited)
            return;
        _inited = true;
        Task.Run(ReceiveMatchData);
    }
    
    public async Task SendMatchData(MatchPlayers pData) {
        using var client = new TcpClient();
        var byteData = pData.GetBytes().ToArray();
        
        await client.ConnectAsync(_logicIp, _logicPort);
        await using var stream = client.GetStream();
        await stream.WriteAsync(byteData);
    }

    public async Task ReceiveMatchData() {
        using var listener = new TcpListener(IPAddress.Any, _receivePort);
        listener.Start();
        while (true) {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();

            var length = MatchResult.Length;
            var data = new byte[length];
            var idx = 0;
            while (idx < length)
                idx += await stream.ReadAsync(data, 0, length - idx);
            idx = 0;
            var result = new MatchResult(data, ref idx);
            await SaveMatchInfo.SaveMatchResult(result);
            OnMatchEnd?.Invoke(result.Rank);
        }
    }
}