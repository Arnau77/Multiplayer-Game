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
    private List<EndPoint> guests; //keeps track of the connections
    private List<EndPoint> disconnections; //keeps track of disconnections
    private List<Action> actions = new List<Action>();
    private List<TextWithID> textsToSend = new List<TextWithID>();
    private List<TextWithID> backupTexts = new List<TextWithID>();
    private Dictionary<int, uint> listOfMessagesReceived = new Dictionary<int, uint>();
    private Dictionary<int, List<uint>> listOfMessagesNeeded = new Dictionary<int, List<uint>>();
    private Dictionary<InfoOfBackupMessages, string> backupOfMessagesSent = new Dictionary<InfoOfBackupMessages, string>();
    private object actionLock;
    private object guestLock;
    private object backupLock;
    private object textLock;
    private Thread serverListenThread;
    private Thread serverSendThread;
    private Socket server;
    //private static int maxID = 0;
    public int maxPlayers;
    private bool morePlayersAllowed = true;
    public int port = 6162; //default port
    public bool packetLoss = false;
    public bool jitter = false;
    public int lossThreshold = 90;
    public int minJitt = 0;
    public int maxJitt = 800;

    private bool serverconnected; //server started or not

    public class TextWithID
    {
        public string text;
        public int id;
        public DateTime timeToSendMessage;
        public bool jitterApplied;
        public TextWithID(string t, int i)
        {
            text = t;
            id = i;
            timeToSendMessage = DateTime.Now;
            jitterApplied = false;
        }
    }

    public class InfoOfBackupMessages
    {
        public uint messageID;
        public int senderID;
        public int getterID;

        public InfoOfBackupMessages(uint messageId, int senderId, int getterId)
        {
            messageID = messageId;
            senderID = senderId;
            getterID = getterId;
        }
    }

    //definition of who connects to the server
    //public class ServerClient
    //{
    //    public EndPoint remoteEP;
    //    public int clientID;

    //    public ServerClient(EndPoint clientsocket)
    //    {
    //        clientID = maxID++;
    //        remoteEP = clientsocket;
    //    }
    //}

    private void Start()
    {
        actionLock = new object();
        guestLock = new object();
        textLock = new object();
        backupLock = new object();


        guests = new List<EndPoint>();
        disconnections = new List<EndPoint>();
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
        if (Input.GetKeyDown(KeyCode.N))
        {
            lock (textLock)
            {
                MessageClass message = new MessageClass(0, 0, MessageClass.TYPEOFMESSAGE.Disconnection, DateTime.Now);
                textsToSend.Add(new TextWithID(message.Serialize(),0));
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
            MessageClass messageReceived = new MessageClass(Encoding.ASCII.GetString(buffer));
            int id = guests.FindIndex(client => client.Equals(clientPoint));
            bool checkIfThereAreMessagesLost = true;
            switch (messageReceived.typeOfMessage)
            {
                case MessageClass.TYPEOFMESSAGE.Connection:
                    if (morePlayersAllowed && id==-1)
                    {
                        lock (guestLock)
                        {
                            guests.Add(clientPoint);
                            id = guests.Count;
                        }
                        if (id-- >= maxPlayers)
                        {
                            morePlayersAllowed = false;
                        }
                        Debug.Log("NEwwW CLIENT");
                        lock (textLock)
                        {
                            MessageClass message = new MessageClass(messageReceived.id, id, MessageClass.TYPEOFMESSAGE.Connection, DateTime.Now);
                            textsToSend.Add(new TextWithID(message.Serialize(),id));
                        }
                    }
                    checkIfThereAreMessagesLost = false;
                    break;
                case MessageClass.TYPEOFMESSAGE.Input:
                    List<EndPoint> localClients;
                    lock (guestLock)
                    {
                        localClients = new List<EndPoint>(guests);
                    }
                    for (int i = 0; i < localClients.Count; i++)
                    {
                        if (i == id)
                        {
                            //continue;
                        }
                        lock (textLock)
                        {
                            MessageClass message = new MessageClass(messageReceived.id, id, MessageClass.TYPEOFMESSAGE.Input, DateTime.Now,MessageClass.INPUT.Attack);
                            textsToSend.Add(new TextWithID(message.Serialize(), i));
                        }
                    }
                    localClients.Clear();
                    break;
                case MessageClass.TYPEOFMESSAGE.Acknowledgment:
                    {
                        checkIfThereAreMessagesLost = false;
                        Dictionary<InfoOfBackupMessages, string> backupOfTheBackup;
                        lock (backupLock)
                        {
                            backupOfTheBackup = new Dictionary<InfoOfBackupMessages, string>(backupOfMessagesSent);
                        }
                        List<InfoOfBackupMessages> messagesToDelete = new List<InfoOfBackupMessages>();
                        foreach (var backMessage in backupOfTheBackup)
                        {
                            if (backMessage.Key.getterID != id)
                                continue;
                            if (backMessage.Key.senderID != messageReceived.playerID)
                                continue;
                            if (backMessage.Key.messageID > messageReceived.id)
                                continue;

                            if (messageReceived.messagesLostInBetween == false || messageReceived.id == backMessage.Key.messageID)
                            {
                                messagesToDelete.Add(backMessage.Key);
                            }
                            else
                            {
                                lock (textLock)
                                {
                                    textsToSend.Add(new TextWithID(backMessage.Value, id));
                                }
                            }
                        }
                        foreach (var messageDeleting in messagesToDelete)
                        {
                            lock (backupLock)
                            {
                                backupOfMessagesSent.Remove(messageDeleting);
                            }
                        }
                        break;
                    }
                case MessageClass.TYPEOFMESSAGE.MessagesNeeded:
                    {
                        checkIfThereAreMessagesLost = false;
                        Dictionary<InfoOfBackupMessages, string> backupOfTheBackup;
                        lock (backupLock)
                        {
                            backupOfTheBackup = new Dictionary<InfoOfBackupMessages, string>(backupOfMessagesSent);
                        }
                        foreach (var backMessage in backupOfTheBackup)
                        {
                            if (backMessage.Key.getterID != id)
                                continue;
                            if (!messageReceived.messagesNeeded.ContainsKey(backMessage.Key.senderID))
                                continue;
                            if (messageReceived.messagesNeeded[backMessage.Key.senderID].Contains(backMessage.Key.messageID))
                            {
                                lock (textLock)
                                {
                                    textsToSend.Add(new TextWithID(backMessage.Value,id));
                                }
                            }
                        }
                    break;
                    }
            }
            int index = messageReceived.playerID;
            List<MessageClass> newMessages = MessageClass.CheckIfThereAreMessagesLost(ref listOfMessagesReceived, ref listOfMessagesNeeded, messageReceived, index, checkIfThereAreMessagesLost);
            for (int i = 0; newMessages!=null && i < newMessages.Count; i++)
            {
                lock (textLock)
                {
                    textsToSend.Add(new TextWithID(newMessages[i].Serialize(),id));
                }
            }
            //Debug.Log(Encoding.ASCII.GetString(buffer));


        }
    }

    void ServerSendThread()
    {
        List<TextWithID> localTexts;
        List<EndPoint> localClients;
        System.Random r = new System.Random();
        byte[] buffer;

        while (true)
        {
            lock (textLock)
            {
                localTexts = new List<TextWithID>(textsToSend);
                textsToSend.Clear();
            }
            if (backupTexts.Count > 0)
            {
                localTexts.AddRange(backupTexts);
                backupTexts.Clear();
            }
            lock (guestLock)
            {
                localClients = new List<EndPoint>(guests);                
            }


            for (int i = 0; i < localTexts.Count; i++)
            {
                //HERE WE WILL WORK WITH PACKET LOSS AND JITTER
                if (!localTexts[i].jitterApplied)
                {
                    MessageClass.TYPEOFMESSAGE type = (MessageClass.TYPEOFMESSAGE)int.Parse(localTexts[i].text.Split('#')[2]);
                    if (type != MessageClass.TYPEOFMESSAGE.Acknowledgment || type != MessageClass.TYPEOFMESSAGE.MessagesNeeded)
                    {
                        uint id = uint.Parse(localTexts[i].text.Split('#')[0]);
                        int senderID = int.Parse(localTexts[i].text.Split('#')[1]);
                        InfoOfBackupMessages info= new InfoOfBackupMessages(id,senderID,localTexts[i].id);
                        lock (backupLock)
                        {
                            if (!backupOfMessagesSent.ContainsKey(info))
                            {
                                backupOfMessagesSent.Add(info, localTexts[i].text);
                            }
                        }
                    }

                    //FIRST PACKET LOSS
                    if (packetLoss && r.Next(0, 100) <= lossThreshold)
                    {
                        Debug.LogWarning("Message Lost by server");
                        continue;
                    }
                    //THEN JITTER
                    if (jitter)
                    {
                        localTexts[i].timeToSendMessage = DateTime.Now.AddMilliseconds(r.Next(minJitt, maxJitt));
                    }
                    localTexts[i].jitterApplied = true;
                }
                if (localTexts[i].timeToSendMessage > DateTime.Now)
                {
                    backupTexts.Add(localTexts[i]);
                    continue;
                }
                buffer = Encoding.ASCII.GetBytes(localTexts[i].text);
                server.SendTo(buffer, buffer.Length, SocketFlags.None, localClients[localTexts[i].id]);
            }
            localTexts.Clear();
            localClients.Clear();
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
        serverListenThread.Abort();
        serverSendThread.Abort();
        server.Close();
    }
}