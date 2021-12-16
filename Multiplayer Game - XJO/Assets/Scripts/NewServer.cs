using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;
using System.Text;
using System.Threading;

public class NewServer : MonoBehaviour
{
    private List<ServerClient> guests; //keeps track of the connections
    private List<ServerClient> disconnections; //keeps track of disconnections
    private List<Action> actions = new List<Action>();
    private List<string> textsToSend = new List<string>();
    private object actionLock;
    private object guestLock;
    private object textLock;
    private Thread serverListenThread;
    private Thread serverSendThread;
    private Socket server;
    private static int maxID = 0;

    public int port = 6162; //default port

    private bool serverconnected; //server started or not

    //definition of who connects to the server
    public class ServerClient
    {
        public EndPoint remoteEP;
        public int clientID;

        public ServerClient(EndPoint clientsocket)
        {
            clientID = maxID++;
            remoteEP = clientsocket;
        }
    }

    private void Start()
    {
        actionLock = new object();
        guestLock = new object();
        textLock = new object();


        guests = new List<ServerClient>();
        disconnections = new List<ServerClient>();
        server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        serverListenThread = new Thread(ServerListenThread);
        serverSendThread = new Thread(ServerSendThread);

        serverconnected = true;

        serverListenThread.Start();
        serverSendThread.Start();
    }
    private void Update()
    {
        if (!serverconnected)
        {
            return;
        }
        lock (actionLock)
        {
            //we clean the list of ongoing actions 
            while (actions.Count > 0)
            {
                Action action = actions[0];
                actions.RemoveAt(0);
                action();
            }
        }

        //foreach (ServerClient g in guests) //g name of the server client
        //{
        //    if (!IsConnected(g.remoteEP)) //is the guest still connected?
        //    {
        //        g.remoteEP.Close();
        //        disconnections.Add(g); //we add the guest to the disconnected list, maybe useful for a reconnection?
        //        continue;
        //    }
        //    else //checking for messages from the guest
        //    {
        //        NetworkStream s = g.remoteEP.GetStream();
        //        if (s.DataAvailable)
        //        {
        //            StreamReader reader = new StreamReader(s, true);
        //            string data = reader.ReadLine();
        //            if (data != null)
        //            {
        //                OnIncomingData(g, data);
        //            }
        //        }
        //    }
        //}
    }


    void ServerListenThread()
    {
        IPEndPoint ipServer = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
        server.Bind(ipServer);

        byte[] buffer = new byte[100];
        EndPoint clientPoint = new IPEndPoint(IPAddress.Any, 0);


        Debug.Log("Server has started on port " + port.ToString());

        while (true)
        {
            //we make the connection with the client and it sends a message, we decode it and if its "ping" the message we proceed to make an output
            int size = server.ReceiveFrom(buffer, ref clientPoint);
            lock (guestLock)
            {
                int id = guests.FindIndex(client => client.remoteEP == clientPoint);
                if (id == -1)
                {
                    guests.Add(new ServerClient(clientPoint));
                }
            }
            //Debug.Log(Encoding.ASCII.GetString(buffer));


        }
    }

    void ServerSendThread()
    {
        List<string> localTexts;
        List<ServerClient> localClients;
        byte[] buffer;

        while (true)
        {
            lock (textLock)
            {
                localTexts = textsToSend;
                textsToSend.Clear();
            }
            lock (guestLock)
            {
                localClients = guests;
            }

            for (int i = 0; i < localTexts.Count; i++)
            {
                buffer = Encoding.ASCII.GetBytes(localTexts[i]);


                for (int j = 0; j < localClients.Count; j++)
                {
                    server.SendTo(buffer, buffer.Length, SocketFlags.None, localClients[j].remoteEP);
                }
            }
        }

    }
    //private bool IsConnected(EndPoint g) //we call the guest again and we try to reach it
    //{
    //    try
    //    {
    //        if (g != null && g.Client != null && g.Client.Connected)
    //        {
    //            if (g.Client.Poll(0, SelectMode.SelectRead))
    //            {
    //                return !(g.Client.Receive(new byte[4], SocketFlags.Peek) == 0); //don't really understand how these lines work SORRY
    //            }

    //            return true;
    //        }
    //        else
    //            return false;
    //    }
    //    catch
    //    {
    //        return false;
    //    }
    //}

    //private void ServerListening()
    //{
    //    server.BeginAcceptTcpClient(AcceptTcpClient, server);
    //}

    //private void AcceptTcpClient(IAsyncResult ar) //we accept a client to connect to the server
    //{
    //    TcpListener listener = (TcpListener)ar.AsyncState;
    //    guests.Add(new ServerClient(listener.EndAcceptTcpClient(ar))); //adding a client to the list
    //    ServerListening();

    //    //Send a message here to everyone to let know somebody has connected maybe?
    //    Broadcast(guests[guests.Count - 1].clientName + "has connected", guests);
    //}

    //private void OnIncomingData(ServerClient g, string data)
    //{
    //    Debug.Log(g.clientName + " has sent the following data: " + data);
    //}
    //private void Broadcast(string data, List<ServerClient> cl)
    //{
    //    foreach (ServerClient g in cl)
    //    {
    //        try
    //        {
    //            StreamWriter writer = new StreamWriter(g.remoteEP.GetStream());
    //            writer.WriteLine(data);
    //            writer.Flush();
    //        }
    //        catch (Exception e)
    //        {
    //            Debug.Log("Write error : " + e.Message + "to client " + g.clientName);
    //        }
    //    }
    //}

    private void OnDestroy()
    {
        server.Close();
        serverListenThread.Abort();
        serverSendThread.Abort();
    }
}