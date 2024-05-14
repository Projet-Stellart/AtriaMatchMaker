using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class MatchMaker
{
    private Dictionary<int, ServerInfo> _servers = new Dictionary<int, ServerInfo>();
    private HashSet<TcpClient> _clients = new HashSet<TcpClient>();

    private int _nextServerId = 1;

    private void CreateServer(int maxPlayers)
    {
        int serverId = _nextServerId++;
        _servers[serverId] = new ServerInfo { MaxPlayers = maxPlayers, CurrentPlayers = 0 };
        SendMessageToClient(serverId, $"server created {serverId}");
    }

    private void UpdateServer(int serverId, int currentPlayers)
    {
        if (_servers.TryGetValue(serverId, out ServerInfo server))
        {
            server.CurrentPlayers = currentPlayers;
        }
    }

    private void DeleteServer(int serverId)
    {
        _servers.Remove(serverId);
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

    private void SendMessageToClient(int serverId, string message)
    {
        foreach (var client in _clients)
        {
            SendMessage(client, message);
        }
    }

    private void HandleClient(TcpClient client)
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
                case "server created":
                    {
                        int maxPlayers = int.Parse(parts[2]);
                        CreateServer(maxPlayers);
                        break;
                    }
                case "server update":
                    {
                        int serverId = int.Parse(parts[1]);
                        int nbPlayers = int.Parse(parts[3]);
                        UpdateServer(serverId, nbPlayers);
                        break;
                    }
                case "server deleted":
                    {
                        int serverId = int.Parse(parts[1]);
                        DeleteServer(serverId);
                        break;
                    }
                case "client request":
                    {
                        int availableServerId = GetAvailableServer();
                        SendMessageToClient(availableServerId, $"client request {availableServerId}");
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

    static void Main(string[] args)
    {
        MatchMaker matchMaker = new MatchMaker();

        int port = 12345;
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"MatchMaker listening on port {port}");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            matchMaker._clients.Add(client);
            ThreadPool.QueueUserWorkItem(matchMaker.HandleClient, client);
        }
    }
}