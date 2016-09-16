using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

public class UTcpClient : MonoBehaviour {

    [SerializeField]
    public GameObject m_Player;
    [SerializeField]
    public GameObject m_Flag1;
    [SerializeField]
    public GameObject m_Flag2;
    [SerializeField]
    public GameObject m_Flag3;
    [SerializeField]
    private Text m_LoggerText;

    [SerializeField]
    private String m_IpAddress;
    [SerializeField]
    private int m_Port;
    [SerializeField]
    private float m_RefreshTime = 1.0f;
    private float m_LastUpdate = 0.0f;

    private TcpClient m_client = new TcpClient();
    private NetworkStream m_stream;
    private IPEndPoint m_ipEndPoint;

    private Queue<string> commandList = new Queue<string>();
    private System.Object locker = new System.Object();

    private bool m_ConnectionOn = false;

    void Awake() {
        m_Flag1.transform.localPosition = m_Flag2.transform.localPosition = m_Flag3.transform.localPosition = new Vector3(-10, 0, -10);
        PathIntegrationTaskClient.Logger.SetLoggerUIFrame(m_LoggerText);
        PathIntegrationTaskClient.Logger.SetLoggerLogToExternal(false);
    }

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        if (m_ConnectionOn)
        {
            if (Time.time - m_LastUpdate > m_RefreshTime)
            {
                m_LastUpdate = Time.time;
                if (commandList.Count != 0)
                {
                    string nextCommand = commandList.Dequeue();
                    SendCommand(nextCommand);
                }
                else
                {
                    SendCommand("GET_UPDATE:");
                }
            }
        }

	}

    private void SendCommand(string command)
    {
        lock (locker)
        {
            try
            {
                //This is a blocking method
                m_client.Connect(m_ipEndPoint);

                Byte[] data = System.Text.Encoding.ASCII.GetBytes(command);

                m_stream = m_client.GetStream();
                m_stream.Write(data, 0, data.Length);

                data = new Byte[256];

                String response = String.Empty;

                Int32 bytes = m_stream.Read(data, 0, data.Length);
                response = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                ParseMessage(response);

                m_stream.Flush();
            }
            catch (SocketException se)
            {
                PathIntegrationTaskClient.Logger.Log(string.Format("SocketException: {0}", se.Message), PathIntegrationTaskClient.LoggerMessageType.Error);
                m_ConnectionOn = false;
            }
            catch (Exception ex)
            {
                PathIntegrationTaskClient.Logger.Log(string.Format("Generic Exception: {0}", ex.Message), PathIntegrationTaskClient.LoggerMessageType.Error);
                m_ConnectionOn = false;
            }
            finally
            {
                if (m_stream != null)
                    m_stream.Close();
                if (m_client != null)
                    m_client.Close();
            }
        }        
    }

    private void ParseMessage(string message)
    {
        //Getting header:
        int h_end = message.IndexOf(":");
        string header = string.Empty;
        string content = String.Empty;

        if (h_end > 0)
        {
            header = message.Substring(0, h_end);
            content = message.Substring(h_end, message.Length - h_end);
        }

        HandleMessage(header, content);

        PathIntegrationTaskClient.Logger.Log(string.Format("Received: {0}", message), PathIntegrationTaskClient.LoggerMessageType.Info);
    }

    private void HandleMessage(string message, string content)
    {

    }

    #region External Events

    public void StartConnection()
    {

        PathIntegrationTaskClient.Logger.Log("Attempting to start connection...", PathIntegrationTaskClient.LoggerMessageType.Info);
        m_ipEndPoint = new IPEndPoint(IPAddress.Parse(m_IpAddress), m_Port);
        SendCommand("INIT:");
        
    }

    public void StartTrial()
    {

    }

    public void StartPractise()
    {

    }
    #endregion

}
