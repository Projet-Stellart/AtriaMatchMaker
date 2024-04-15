using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Matchmaking;

public class MatchMaker
{
    private static int _nextServerId = 1;
    private static Dictionary<int, ServerInfo> _servers = new Dictionary<int, ServerInfo>();

    static void Main(string[] args)
    {
        int port = 12345;
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"MatchMaker listening on port {port}");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            ThreadPool.QueueUserWorkItem(ProcessClient, client);
        }
    }

    private static void ProcessClient(object state)
    {
        TcpClient client = (TcpClient)state;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
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
                        int serverId = RegisterServer(maxPlayers);
                        SendMessage(stream, $"server update {serverId} nbPlayers=0");
                        break;
                    }
                case "server update":
                    {
                        int serverId = int.Parse(parts[1]);
                        int nbPlayers = int.Parse(parts[3]);
                        UpdateServer(serverId, nbPlayers);
                        break;