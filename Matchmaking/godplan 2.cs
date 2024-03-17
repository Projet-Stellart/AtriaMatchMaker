using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

class MatchMaker
{
    private Dictionary<string, int> servers = new Dictionary<string, int>(); // Server ID -> Maximum Player Count mapping
    private TcpListener listener;

    public MatchMaker(int port)
    {
        listener = new TcpListener(IPAddress.Any, port);
    }

    public void Start()
    {
        listener.Start();
        Console.WriteLine("Match Maker started. Waiting for connections...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("Client connected.");

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Received: " + dataReceived);

            ProcessMessage(dataReceived, stream);

            stream.Close();
            client.Close();
        }
    }

    private void ProcessMessage(string message, NetworkStream stream)
    {
        string[] parts = message.Split(' ');
        string command = parts[0];

        switch (command)
        {
            case "server":
                HandleServerCommand(parts, stream);
                break;
            case "client":
                HandleClientRequest(stream);
                break;
            default:
                Console.WriteLine("Invalid command.");
                break;
        }
    }

    private void HandleServerCommand(string[] parts, NetworkStream stream)
    {
        string subCommand = parts[1];
        string serverId;

        switch (subCommand)
        {
            case "created":
                int maxPlayers = int.Parse(parts[3].Split('=')[1]);
                serverId = Guid.NewGuid().ToString(); // Generate a unique server ID
                servers.Add(serverId, maxPlayers);
                byte[] response = Encoding.ASCII.GetBytes("Server created. ID: " + serverId);
                stream.Write(response, 0, response.Length);
                Console.WriteLine("Server created. ID: " + serverId);
                break;
            case "update":
                serverId = parts[2];
                int nbPlayers = int.Parse(parts[4].Split('=')[1]);
                // Update server info here (if needed)
                Console.WriteLine($"Server {serverId} updated. Number of players: {nbPlayers}");
                break;
            case "deleted":
                serverId = parts[2];
                servers.Remove(serverId);
                Console.WriteLine("Server deleted. ID: " + serverId);
                break;
            default:
                Console.WriteLine("Invalid server command.");
                break;
        }
    }

    private void HandleClientRequest(NetworkStream stream)
    {
        // Find a server with available slots
        foreach (var server in servers)
        {
            if (server.Value < 10) // Assuming maximum player count is 10
            {
                byte[] response = Encoding.ASCII.GetBytes("Available server ID: " + server.Key);
                stream.Write(response, 0, response.Length);
                Console.WriteLine("Client request handled. Sent server ID: " + server.Key);
                return;
            }
        }

        // No available server found
        byte[] noServerResponse = Encoding.ASCII.GetBytes("No available server.");
        stream.Write(noServerResponse, 0, noServerResponse.Length);
        Console.WriteLine("No available server.");
    }
}

class Program
{
    static void Main(string[] args)
    {
        MatchMaker matchMaker = new MatchMaker(8888); // Port number
        matchMaker.Start();
    }
}
