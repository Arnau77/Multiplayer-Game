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
    private List<string> textsToSend = new List<string>();
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

    //TEMPORAL!!!!!!!!!!!!!!!!
    public CharacterScript characterScript;

    void Start()
    {
        actionLock = new object();
        textLock = new object();
        MessageClass message = new MessageClass(messageID++, -1, MessageClass.TYPEOFMESSAGE.Connection, System.DateTime.Now);
        textsToSend.Add(message.Serialize());
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
        List<string> localTexts =new List<string>();
       
        while (true)
        {
            lock (textLock)
            {
                for(int i = 0; i < textsToSend.Count; i++)
                {
                    localTexts.Add(textsToSend[i]);
                }
                textsToSend.Clear();
            }
            for (int i = 0; i < localTexts.Count; i++)
            {
                buffer = Encoding.ASCII.GetBytes(localTexts[i]);
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
                        break;
                }
                Debug.Log(Encoding.ASCII.GetString(buffer));
            }
        }
    }

    private void OnIncomingData(string data)
    {
        Debug.Log("Server : " + data);
    }

    public void SendInputMessageToServer(MessageClass.INPUT messageInput)
    {
        lock (textLock)
        {
            MessageClass message = new MessageClass(messageID++, clientID, MessageClass.TYPEOFMESSAGE.Input, System.DateTime.Now, messageInput);
            textsToSend.Add(message.Serialize());
        }
    }

    private void OnDestroy()
    {
        clientSendThread.Abort();
        clientListenThread.Abort();
        socket.Close();
    }

}
