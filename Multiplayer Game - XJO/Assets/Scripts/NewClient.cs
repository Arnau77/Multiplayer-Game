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
    private Dictionary<int, uint> listOfMessagesReceived = new Dictionary<int, uint>();
    private Dictionary<int, List<uint>> listOfMessagesNeeded = new Dictionary<int, List<uint>>();
    private Dictionary<uint, string> backupOfMessagesSent = new Dictionary<uint, string>();
    private List<Action> actions = new List<Action>();
    private object actionLock;
    private object backupLock;
    private object textLock;
    private Thread clientListenThread;
    private Thread clientSendThread;
    private StreamWriter writter;
    private StreamReader reader;
    private bool firstMessageSent = false;
    private bool connected = false;
    private uint messageID = 0;
    public int clientID = -1;

    public bool packetLoss = false;
    public bool jitter = false;
    public int lossThreshold = 90;
    public int minJitt = 0;
    public int maxJitt = 800;
    public Dictionary<int, Vector3> positionsDic = new Dictionary<int, Vector3>();

    public static Action<int> onConnectionReceived;

    //TEMPORAL!!!!!!!!!!!!!!!!
    public List<CharacterScript> characterScripts;


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

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        actionLock = new object();
        textLock = new object();
        backupLock = new object();
        MessageClass message = new MessageClass(messageID++, clientID, MessageClass.TYPEOFMESSAGE.Connection, DateTime.Now, new Vector3(0,0,0));
        textsToSend.Add(new MessageWithPossibleJitter(message.Serialize()));
        //TODO: WARNING!!!!!!! THIS LINE WILL HAVE TO PROBABLY BE REMOVED LATER, WHEN HAVING THE LOBBY
     
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
            MessageClass message = new MessageClass(messageID++, clientID, MessageClass.TYPEOFMESSAGE.Disconnection, DateTime.Now);
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
                    MessageClass.TYPEOFMESSAGE type = (MessageClass.TYPEOFMESSAGE)int.Parse(localTexts[i].text.Split('#')[2]);
                    if (type != MessageClass.TYPEOFMESSAGE.Acknowledgment && type != MessageClass.TYPEOFMESSAGE.MessagesNeeded)
                    {
                        uint id = uint.Parse(localTexts[i].text.Split('#')[0]);
                        lock (backupLock)
                        {
                            if (!backupOfMessagesSent.ContainsKey(id))
                            {
                                backupOfMessagesSent.Add(id, localTexts[i].text);
                            }
                        }
                    }
                    //FIRST PACKET LOSS
                    if (packetLoss && r.Next(0, 100) <= lossThreshold)
                    {
                        Debug.LogWarning("Message Lost by client: " + clientID);
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
        while (true)
        {
            if (firstMessageSent)
            {
                socket.ReceiveFrom(buffer, ref serverPoint);
                MessageClass messageReceived = new MessageClass(Encoding.ASCII.GetString(buffer));
                bool checkIfThereAreMessagesLost = true;
                switch (messageReceived.typeOfMessage)
                {
                    case MessageClass.TYPEOFMESSAGE.Input:
                        foreach (var character in characterScripts)
                        {
                            if (character.ID != messageReceived.playerID)
                                continue;

                            if (messageReceived.input == MessageClass.INPUT.Attack)
                            {
                                character.Attack();
                            }

                            if (messageReceived.input == MessageClass.INPUT.Move)
                            {
                                character.Walk(messageReceived.position);
                            }
                        }
                        break;
                    case MessageClass.TYPEOFMESSAGE.Connection:
                        if (clientID == -1)
                        {
                            lock (backupLock)
                            {
                                backupOfMessagesSent.Remove(0);
                            }
                            clientID = messageReceived.playerID;
                        }
                        actions.Add(() => onConnectionReceived?.Invoke(messageReceived.playerID));
                        positionsDic.Add(messageReceived.playerID, messageReceived.position);
                        break;
                    case MessageClass.TYPEOFMESSAGE.Acknowledgment:
                        checkIfThereAreMessagesLost = false;
                        Dictionary<uint, string> backupOfTheBackup;
                        lock (backupLock)
                        {
                            backupOfTheBackup = new Dictionary<uint, string>(backupOfMessagesSent);
                        }
                        List<uint> messagesToDelete = new List<uint>();
                        foreach(var backMessage in backupOfTheBackup)
                        {
                            if (backMessage.Key > messageReceived.id)
                                break;
                            if (messageReceived.messagesLostInBetween == false || messageReceived.id==backMessage.Key)
                            {
                                messagesToDelete.Add(backMessage.Key);
                            }
                            else
                            {
                                lock (textLock)
                                {
                                    textsToSend.Add(new MessageWithPossibleJitter(backMessage.Value));
                                }
                            }
                        }
                        foreach(var messageDeleting in messagesToDelete)
                        {
                            lock (backupLock)
                            {
                                backupOfMessagesSent.Remove(messageDeleting);
                            }
                        }
                        break;
                    case MessageClass.TYPEOFMESSAGE.MessagesNeeded:
                        checkIfThereAreMessagesLost = false;
                        lock (backupLock)
                        {
                            foreach (var backMessage in backupOfMessagesSent)
                            {
                                if (messageReceived.messagesNeeded[clientID].Contains(backMessage.Key))
                                {
                                    lock (textLock)
                                    {
                                        textsToSend.Add(new MessageWithPossibleJitter(backMessage.Value));
                                    }
                                }
                            }
                        }
                        break;
                }

                int index = messageReceived.playerID;
                List<MessageClass> newMessages= MessageClass.CheckIfThereAreMessagesLost(ref listOfMessagesReceived, ref listOfMessagesNeeded, messageReceived, index,checkIfThereAreMessagesLost, clientID);
                for(int i = 0; newMessages != null && i < newMessages.Count; i++)
                {
                    lock (textLock)
                    {
                        textsToSend.Add(new MessageWithPossibleJitter(newMessages[i].Serialize()));
                    }
                }
                if (messageReceived.typeOfMessage != MessageClass.TYPEOFMESSAGE.Acknowledgment)

                    Debug.Log("Player " + clientID + ": " + Encoding.ASCII.GetString(buffer));
            }
        }
    }

    //private void OnIncomingData(string data)
    //{
    //    Debug.Log("Server : " + data);
    //}

    public void SendInputMessageToServer(MessageClass.INPUT messageInput, bool sendPos = false, float x = 0, float y = 0, float z = 0)
    {
        MessageClass message;
        if (sendPos)
        {
            message = new MessageClass(messageID++, clientID, MessageClass.TYPEOFMESSAGE.Input, DateTime.Now, messageInput,new Vector3(x,y,z));
        }
        else
        {

            message = new MessageClass(messageID++, clientID, MessageClass.TYPEOFMESSAGE.Input, DateTime.Now, messageInput);
        }
        lock (textLock)
        {
            textsToSend.Add(new MessageWithPossibleJitter(message.Serialize()));
        }
    }

   

    private void OnDestroy()
    {
        clientSendThread.Abort();
        clientListenThread.Abort();
        socket.Close();
    }

}
