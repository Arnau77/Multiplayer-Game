using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Text;
using System.Threading;
using UnityEngine.UI;

public class NewClient : MonoBehaviour
{
    private IPEndPoint ipDestination;
    private EndPoint serverPoint;
    private Socket socket;
    private List<MessageWithPossibleJitter> textsToSend = new List<MessageWithPossibleJitter>();
    private List<MessageWithPossibleJitter> backupTexts = new List<MessageWithPossibleJitter>();
    private Dictionary<int, List<uint>> listOfMessagesReceived = new Dictionary<int, List<uint>>();
    private List<Action> actions = new List<Action>();
    private object actionLock;
    private object textLock;
    private Thread clientListenThread;
    private Thread clientSendThread;
    private StreamWriter writter;
    private StreamReader reader;
    private bool firstMessageSent = false;
    private bool connected = false;
    private uint messageID = 0;
    private int clientID;

    public bool packetLoss = false;
    public bool jitter = false;
    public int lossThreshold = 90;
    public int minJitt = 0;
    public int maxJitt = 800;

    //TEMPORAL!!!!!!!!!!!!!!!!
    public CharacterScript characterScript;


    public class MessageWithPossibleJitter
    {
        public string text;
        public DateTime timeToSendMessage;
        public bool jitterApplied;
        public MessageWithPossibleJitter(string t)
        {
            text = t;
            timeToSendMessage = DateTime.Now;
            jitterApplied = false;
        }
    }
    void Start()
    {
        actionLock = new object();
        textLock = new object();
        MessageClass message = new MessageClass(messageID++, -1, MessageClass.TYPEOFMESSAGE.Connection, DateTime.Now);
        textsToSend.Add(new MessageWithPossibleJitter(message.Serialize()));
        //TODO: WARNING!!!!!!! THIS LINE WILL HAVE TO PROBABLY BE REMOVED LATER, WHEN HAVING THE LOBBY
        ConnectToServer();
    }
    public void ConnectToServer()
    {
        if (connected)
        {
            return;
        }
        connected = true;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        ipDestination = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6162);
        serverPoint = new IPEndPoint(IPAddress.Any, 0);
        clientListenThread = new Thread(ClientListenThread);
        clientSendThread = new Thread(ClientSendThread);

        clientSendThread.Start();
        clientListenThread.Start();
    }

    private void Update()
    {
        lock (actionLock)
        {
            while (actions.Count > 0)
            {
                Action action = actions[0];
                actions.RemoveAt(0);
                action();
            }
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            MessageClass message = new MessageClass(messageID++, -1, MessageClass.TYPEOFMESSAGE.Connection, DateTime.Now);
            lock (textLock)
            {
                textsToSend.Add(new MessageWithPossibleJitter(message.Serialize()));
            }
        }
        //if (socketReady)
        //{
        //    if (stream.DataAvailable)
        //    {
        //        string data = reader.ReadLine();
        //        if (data != null)
        //            OnIncomingData(data);


        //    }
        //}
    }

    void ClientSendThread()
    {
        byte[] buffer;
        List<MessageWithPossibleJitter> localTexts;
        System.Random r = new System.Random();

        while (true)
        {
            lock (textLock)
            {
                localTexts = new List<MessageWithPossibleJitter>(textsToSend);
                textsToSend.Clear();
            }
            if (backupTexts.Count > 0)
            {
                localTexts.AddRange(backupTexts);
                backupTexts.Clear();
            }
            for (int i = 0; i < localTexts.Count; i++)
            {

                //HERE WE WILL WORK WITH PACKET LOSS AND JITTER
                if (!localTexts[i].jitterApplied)
                {
                    //FIRST PACKET LOSS
                    if (packetLoss && r.Next(0, 100) <= lossThreshold)
                    {
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
                socket.SendTo(buffer, buffer.Length, SocketFlags.None, ipDestination);
                firstMessageSent = true;
            }
            localTexts.Clear();


        }
    }

    void ClientListenThread()
    {
        byte[] buffer = new byte[100];
        Socket socket2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        while (true)
        {
            if (firstMessageSent)
            {
                socket.ReceiveFrom(buffer, ref serverPoint);
                MessageClass messageReceived = new MessageClass(Encoding.ASCII.GetString(buffer));
                switch (messageReceived.typeOfMessage)
                {
                    case MessageClass.TYPEOFMESSAGE.Input:

                        if (messageReceived.input == MessageClass.INPUT.Attack)
                        {
                            characterScript.Attack();
                        }
         
                        if(messageReceived.input == MessageClass.INPUT.A || messageReceived.input == MessageClass.INPUT.D)
                        {
                            characterScript.Walk(messageReceived.input);
                        }
                        break;
                    case MessageClass.TYPEOFMESSAGE.Connection:
                        clientID = messageReceived.playerID;
                        break;
                }
                if (messageReceived.typeOfMessage != MessageClass.TYPEOFMESSAGE.Connection)
                {
                    int index = messageReceived.playerID;
                    if (!listOfMessagesReceived.ContainsKey(index))
                    {
                        listOfMessagesReceived.Add(index, new List<uint>());
                    }

                    CheckIfThereAreMessagesLost(listOfMessagesReceived[index], messageReceived);

                    
                    MessageClass message = new MessageClass(messageReceived.id, messageReceived.playerID, MessageClass.TYPEOFMESSAGE.Acknoledgment, DateTime.Now);
                    for(int i = 0; i < 3; i++)
                    {
                        lock (textLock)
                        {
                            textsToSend.Add(new MessageWithPossibleJitter(message.Serialize()));
                        }
                    }
                }
                Debug.Log(Encoding.ASCII.GetString(buffer));
            }
        }
    }

    //private void OnIncomingData(string data)
    //{
    //    Debug.Log("Server : " + data);
    //}

    public void SendInputMessageToServer(MessageClass.INPUT messageInput)
    {
        MessageClass message = new MessageClass(messageID++, clientID, MessageClass.TYPEOFMESSAGE.Input, DateTime.Now, messageInput);
        lock (textLock)
        {
            textsToSend.Add(new MessageWithPossibleJitter(message.Serialize()));
        }
    }

    public void CheckIfThereAreMessagesLost(List<uint> listOfMessages, MessageClass message)
    {
        uint idMessage = message.id;
        int sizeList = listOfMessages.Count;
        bool thereAreMessagesLost = false;
        Dictionary<uint, int> messagesLost= new Dictionary<uint, int>();

        if (sizeList < 1)
        {
            if (idMessage > 0)
            {
                thereAreMessagesLost = true;
                for(uint i = 0; i < idMessage; i++)
                {
                    messagesLost.Add(i, message.playerID);
                }
            }
        }
        //else
        //{
        //    foreach (var id in messagesLost)
        //    {
        //    }
        //}
        else if (sizeList == 1)
        {
            if (messagesLost.ContainsKey(idMessage - 1))
            {
                messagesLost.Clear();
                messagesLost.Add(idMessage, message.playerID);
            }
            else
            {
                uint idLastMessage = messagesLost.Keys.GetEnumerator().Current;
            }
        }
        else
        {

        }


        if (thereAreMessagesLost)
        {
            MessageClass messageToSend = new MessageClass(idMessage, message.playerID, MessageClass.TYPEOFMESSAGE.MessagesNeeded, DateTime.Now, messagesLost);
            lock (textLock)
            {
                textsToSend.Add(new MessageWithPossibleJitter(messageToSend.Serialize()));
            }
        }
    }

    private void OnDestroy()
    {
        clientSendThread.Abort();
        clientListenThread.Abort();
        socket.Close();
    }

}
