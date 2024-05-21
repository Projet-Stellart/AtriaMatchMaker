using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Matchmaking;

public class MatchMaker
{
    private Dictionary<int, ServerInfo> _servers = new Dictionary<int, ServerInfo>();
    private HashSet<TcpClient> _clients = new HashSet<TcpClient>();

    private int _nextServerId = 1;

    private void CreateServer(int maxPlayers, TcpClient client)
    {
        int serverId = _nextServerId++;
        _servers[serverId] = new ServerInfo { MaxPlayers = maxPlayers, CurrentPlayers = 0 };
        SendMessageToClient(client, $"server created {serverId}");
        Console.WriteLine("[ServerHandler] server created: " + serverId);
    }

    private void UpdateServer(int serverId, int currentPlayers)
    {
        if (_servers.TryGetValue(serverId, out ServerInfo server))
        {
            server.CurrentPlayers = currentPlayers;
        }
        Console.WriteLine("[ServerHandler] server updated: " + serverId + " nbPlayer: " + currentPlayers);
    }

    private void DeleteServer(int serverId)
    {
        _servers.Remove(serverId);
        Console.WriteLine("[ServerHandler] server removed: " + serverId);
    }

    private int GetAvailableServer()
    {
        foreach (var server in _servers)
        {
            if (server.Value.CurrentPlayers < server.Value.MaxPlayers)
            {
                return server.Key;
            }
        }
        return -1;
    }

    private void SendMessage(TcpClient client, string message)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(message);
        client.GetStream().Write(buffer, 0, buffer.Length);
    }

    private void SendMessageToClient(TcpClient client, string message)
    {
            SendMessage(client, message);
    }

    private void SendMessageToClients(string message)
    {
        foreach (var client in _clients)
        {
            SendMessage(client, message);
        }
    }

    private void HandleServer(TcpClient client, string[] parts)
    {
        switch (parts[1])
        {
            case "create":
                {
                    if (!int.TryParse(parts[2], out int maxPlayers))
                    {
                        SendMessageToClient(client, "Unvalid syntaxe");
                        return;
                    }
                    CreateServer(maxPlayers, client);
                    break;
                }
            case "update":
                {
                    if (!int.TryParse(parts[2], out int serverId))
                    {
                        SendMessageToClient(client, "Unvalid syntaxe");
                        return;
                    }
                    if (!int.TryParse(parts[3], out int nbPlayers))
                    {
                        SendMessageToClient(client, "Unvalid syntaxe");
                        return;
                    }
                    UpdateServer(serverId, nbPlayers);
                    break;
                }
            case "delete":
                {
                    if (!int.TryParse(parts[2], out int serverId))
                    {
                        SendMessageToClient(client, "Unvalid syntaxe");
                        return;
                    }
                    DeleteServer(serverId);
                    break;
                }
        }
    }

    private void HandleClient(TcpClient client, string[] parts)
    {
        switch (parts[1])
        {
            case "request":
                {
                    int availableServerId = GetAvailableServer();
                    SendMessageToClient(client, $"client request {availableServerId}");
                    Console.WriteLine("[ClientHandler] client to server: " + availableServerId);
                    break;
                }
        }
    }

    private void HandleMessage(TcpClient client)
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = client.GetStream().Read(buffer, 0, buffer.Length)) != 0)
        {
            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Received: {message}");

            string[] parts = message.Split(' ');
            string command = parts[0];

            switch (command)
            {
                case "server":
                {
                    HandleServer(client, parts);
                    break;
                }
                case "client":
                {
                    HandleClient(client, parts);
                    break;
                }
            }
        }

        _clients.Remove(client);
        client.Close();
    }

    private class ServerInfo
    {
        public int MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; }
    }

    public void MainHandle()
    {

    }

    public void Start()
    {
        MatchMaker matchMaker = new MatchMaker();

        int port = 12345;
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"MatchMaker listening on port {port}");

        Task.Run(() =>
        {
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                matchMaker._clients.Add(client);
                try
                {
                    matchMaker.HandleMessage(client);
                }catch (IOException e) {}
            }
        });

        while (true)
        {
            MainHandle();
        }
    }
}
