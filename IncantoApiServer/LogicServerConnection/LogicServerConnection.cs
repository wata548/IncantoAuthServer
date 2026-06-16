using System.Net;
using System.Net.Sockets;
using Setting;

namespace IncantoApiServer.LogicServerConnection;

public class LogicServerConnection {
    private readonly IPAddress _logicIp = IPAddress.Parse(Env.Get("LogicServerIp"));
    private readonly int _logicPort = int.Parse(Env.Get("LogicServerPort"));
    private readonly int _receivePort = int.Parse(Env.Get("Port"));
    
    public async Task SendMatchData(MatchPlayers pData) {
        using var client = new TcpClient();
        var byteData = pData.GetBytes().ToArray();
        
        await client.ConnectAsync(_logicIp, _logicPort);
        await using var stream = client.GetStream();
        await stream.WriteAsync(byteData);
    }

    public async Task ReceiveMatchData() {
        using var listener = new TcpListener(IPAddress.Any, _receivePort);
        while (true) {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();

            var length = Match.MatchResult.Length;
            var data = new byte[length];
            var idx = 0;
            while (idx < length)
                idx += await stream.ReadAsync(data, 0, length - idx);
            idx = 0;
            var result = new Match.MatchResult(data, ref idx);
            
        }
    }
}