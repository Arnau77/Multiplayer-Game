using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class NewClient : MonoBehaviour
{

    private bool socketReady;
    private TcpClient socket;
    private NetworkStream stream;
    private StreamWriter writter;
    private StreamReader reader;

    public void ConnectToServer()
    {
        if (socketReady) //if connected, ignore function
            return;

        string host = "127.0.0.1";
        int port = 6162;

        string h;
        int p;

        //let the player decide the server and port to connect, if not we keep the default values
        h = GameObject.Find("HostInput").GetComponent<InputField>().text;
        if (h != "")
            host = h;
        int.TryParse(GameObject.Find("PortInput").GetComponent<InputField>().text, out p);
        if (p != 0)
            port = p;

        try //we create the socket for connection
        {
            socket = new TcpClient(host, port);
            stream = socket.GetStream();
            writter = new StreamWriter(stream);
            reader = new StreamReader(stream);

        }
        catch(Exception e)
        {
            Debug.Log("Socket Error : " + e.Message);
        }
    }

    private void Update()
    {
        if (socketReady)
        {
            if (stream.DataAvailable)
            {
                string data = reader.ReadLine();
                if (data != null)
                    OnIncomingData(data);
            

            }
        }
    }

    private void OnIncomingData(string data)
    {
        Debug.Log("Server : " + data);
    }

}
