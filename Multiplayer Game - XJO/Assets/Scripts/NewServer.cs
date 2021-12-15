using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;

public class NewServer : MonoBehaviour
{
    private List<ServerClient> guests; //keeps track of the connections
    private List<ServerClient> disconnections; //keeps track of disconnections

    public int port = 6162; //default port

    private TcpListener server;
    private bool serverconnected; //server started or not

    private void Start()
    {
        guests = new List<ServerClient>();
        disconnections = new List<ServerClient>();

        try
        {
            server = new TcpListener(IPAddress.Any, port); //listen to anyone who connects to the default port
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
        foreach (ServerClient g in guests) //g name of the server client
        {
            if (!IsConnected(g.tcp)) //is the guest still connected?
            {
                g.tcp.Close();
                disconnections.Add(g); //we add the guest to the disconnected list, maybe useful for a reconnection?
                continue;
            }
            else //checking for messages from the guest
            {
                NetworkStream s = g.tcp.GetStream();
                if (s.DataAvailable)
                {
                    StreamReader reader = new StreamReader(s, true);
                    string data = reader.ReadLine();
                    if (data != null)
                    {
                        OnIncomingData(g, data);
                    }
                }
            }
        }
    }

    private bool IsConnected(TcpClient g) //we call the guest again and we try to reach it
    {
        try
        {
            if (g != null && g.Client != null && g.Client.Connected)
            {
                if (g.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(g.Client.Receive(new byte[4], SocketFlags.Peek) == 0); //don't really understand how these lines work SORRY
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

    private void AcceptTcpClient(IAsyncResult ar) //we accept a client to connect to the server
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        guests.Add(new ServerClient(listener.EndAcceptTcpClient(ar))); //adding a client to the list
        ServerListening();

        //Send a message here to everyone to let know somebody has connected maybe?
        Broadcast(guests[guests.Count-1].clientName + "has connected", guests);
    }

    private void OnIncomingData(ServerClient g, string data)
    {
        Debug.Log(g.clientName + " has sent the following data: " + data);
    }
    private void Broadcast(string data, List<ServerClient> cl)
    {
        foreach (ServerClient g in cl)
        {
            try
            {
                StreamWriter writer = new StreamWriter(g.tcp.GetStream());
                writer.WriteLine(data);
                writer.Flush();
            }
            catch(Exception e)
            {
                Debug.Log("Write error : " + e.Message + "to client " + g.clientName);
            }
        }
    }
}

//definition of who connects to the server
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