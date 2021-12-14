using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;

public class Server : MonoBehaviour
{
    private List <ServerClient> guests;
    private List<ServerClient> disconnections;

    public int port = 6162;

    private TcpListener server;
    private bool serverconnected;

    private void Start()
    {
        guests = new List<ServerClient>();
        disconnections = new List<ServerClient>();

        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            ServerListening();
            serverconnected = true;
            Debug.Log("Server has started on port " + port.ToString());
        }
        catch (Exception e)
        {
            Debug.Log("Socket Error: " + e.Message);
        }
    }
    private void Update()
    {
        if (!serverconnected)
        {
            return;
        }
        foreach(ServerClient g in guests)
        {
            if (!IsConnected(g.tcp))
            {
                g.tcp.Close();
                disconnections.Add(g);
                continue;
            }
            else
            {
                NetworkStream s = g.tcp.GetStream();
                if (s.DataAvailable)
                {
                    StreamReader reader = new StreamReader(s, true);
                    string data = reader.ReadLine();
                    if(data != null)
                    {
                        OnIncomingData(g, data);
                    }
                }
            }
        }
    }

    private bool IsConnected(TcpClient g)
    {
        try
        {
            if (g != null && g.Client != null && g.Client.Connected)
            {
                if (g.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(g.Client.Receive(new byte[4], SocketFlags.Peek) == 0);
                }

                return true;
            }
            else
                return false;
        }
        catch
        {
            return false;
        }
    }

    private void ServerListening()
    {
        server.BeginAcceptTcpClient(AcceptTcpClient, server);
    }

    private void AcceptTcpClient(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        guests.Add(new ServerClient(listener.EndAcceptTcpClient(ar)));
        ServerListening();
    }

    private void OnIncomingData(ServerClient g, string data)
    {
        Debug.Log(g.clientName + " has sent the following message: " + data);
    }
}

public class ServerClient
{
    public TcpClient tcp;
    public string clientName;

    public ServerClient(TcpClient clientsocket)
    {
        clientName = "Guest";
        tcp = clientsocket;
    }
}