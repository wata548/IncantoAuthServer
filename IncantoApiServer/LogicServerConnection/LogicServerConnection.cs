using System.Net;
using System.Net.Sockets;
using MatchMaking;
using Setting;

namespace IncantoApiServer.LogicServerConnection;

public class LogicServerConnection {
    private IPAddress _ip = IPAddress.Parse(Env.Get("LogicServerIp"));
    private int _port = int.Parse(Env.Get("LogicServerPort"));

    public async Task SendMatchData(MatchPlayers pData) {
        Console.WriteLine("Send Start");
        using var client = new TcpClient();
        var byteData = pData.Serialize();
        
        await client.ConnectAsync(_ip, _port);
        await using var stream = client.GetStream();
        await stream.WriteAsync(byteData);
        Console.WriteLine("Send Data by tcp");
    }
}