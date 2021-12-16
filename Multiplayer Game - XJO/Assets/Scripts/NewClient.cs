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

    void Start()
    {
        actionLock = new object();
        textLock = new object();
    }
    public void ConnectToServer()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        ipDestination = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6162);
        serverPoint = new IPEndPoint(IPAddress.Any, 0);
        clientListenThread = new Thread(ClientListenThread);
        clientSendThread = new Thread(ClientSendThread);

        clientListenThread.Start();
        clientSendThread.Start();
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
        List<string> localTexts;
       
        while (true)
        {
            lock (textLock)
            {
                localTexts = textsToSend;
                textsToSend.Clear();
            }
            for (int i = 0; i < localTexts.Count; i++)
            {
                buffer = Encoding.ASCII.GetBytes(localTexts[i]);
                socket.SendTo(buffer, buffer.Length, SocketFlags.None, ipDestination);

            }


        }
    }

    void ClientListenThread()
    {
        byte[] buffer = new byte[100];

        socket.ReceiveFrom(buffer, ref serverPoint);

        Debug.Log(Encoding.ASCII.GetString(buffer));
    }

    private void OnIncomingData(string data)
    {
        Debug.Log("Server : " + data);
    }

    private void OnDestroy()
    {
        socket.Close();
        clientSendThread.Abort();
        clientListenThread.Abort();
    }

}
