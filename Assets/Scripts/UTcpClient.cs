using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

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

    private IPAddress m_RegisteredIpAddress = null;
    private int m_RegisteredPort;

    [SerializeField]
    private Text m_InputIPText;
    [SerializeField]
    private Text m_InputPortText;

    //private TcpClient m_client = new TcpClient();
    //private NetworkStream m_stream;
    //private IPEndPoint m_ipEndPoint;

    private Queue<string> commandList = new Queue<string>();
    private System.Object locker = new System.Object();

    private bool m_ConnectionOn = false;
    private bool m_ReleaseCommand = true;

    public SteamVR_PlayArea m_TrackedArea;
    public SteamVR_PlayArea m_OffsetedArea;

    void Awake() {
        m_Flag1.transform.localPosition = m_Flag2.transform.localPosition = m_Flag3.transform.localPosition = new Vector3(-10, 0, -10);
        PathIntegrationTaskClient.Logger.SetLoggerUIFrame(m_LoggerText);
        PathIntegrationTaskClient.Logger.SetLoggerLogToExternal(false);
        m_TrackedArea = GameObject.FindGameObjectWithTag("TrackedArea").GetComponent<SteamVR_PlayArea>();
        m_OffsetedArea = GameObject.FindGameObjectWithTag("OffsetArea").GetComponent<SteamVR_PlayArea>();
    }

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        if (m_ConnectionOn)
        {
            if (Time.time - m_LastUpdate > m_RefreshTime && m_ReleaseCommand)
            {
                m_LastUpdate = Time.time;
                if (commandList.Count != 0)
                {
                    string nextCommand = String.Empty;
                    //locking queue for concurrence
                    lock (locker)
                    {
                        nextCommand = commandList.Dequeue();
                    }
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
        NetworkStream _stream = null;
        TcpClient client = new TcpClient();
        String response = String.Empty;

        try
        {
            m_ReleaseCommand = false;
            //This is a blocking method
            IPEndPoint ipEndPoint = new IPEndPoint(m_RegisteredIpAddress, m_RegisteredPort);
            
            client.Connect(ipEndPoint);

            Byte[] data = System.Text.Encoding.ASCII.GetBytes(command);

            _stream = client.GetStream();
            _stream.Write(data, 0, data.Length);

            data = new Byte[256];            

            Int32 bytes = _stream.Read(data, 0, data.Length);
            response = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            
            ParseMessage(response);

            _stream.Flush();

            
        }
        catch (SocketException se)
        {
            response = se.Message; //PathIntegrationTaskClient.Logger.Log(string.Format("SocketException: {0}", se.Message), PathIntegrationTaskClient.LoggerMessageType.Error);
            m_ConnectionOn = false;
            if (client != null)
                client.Close();
        }
        catch (Exception ex)
        {
            response = ex.Message; //PathIntegrationTaskClient.Logger.Log(string.Format("Generic Exception: {0}", ex.Message), PathIntegrationTaskClient.LoggerMessageType.Error);
            m_ConnectionOn = false;
            if (client != null)
                client.Close();
        }
        finally
        {
            if (_stream != null)
                _stream.Close();
            if (client != null)
                client.Close();

            m_ReleaseCommand = true;
        }

        PathIntegrationTaskClient.Logger.Log(string.Format("Received: {0}", response), PathIntegrationTaskClient.LoggerMessageType.Info);    

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
            content = message.Substring(h_end + 1, message.Length - h_end - 1);
        }

        HandleMessage(header, content);
                
    }

    private void HandleMessage(string message, string content)
    {
        switch (message)
        {
            case "INIT":
                if (content == "ACK")
                {
                    m_LastUpdate = Time.time;
                    //Adding get room command to the chain
                    AddCommandQueue("GET_ROOM:");
                    m_ConnectionOn = true;
                }
                break;
            case "START_PRACT":
                if (content == "ACK")
                {
                    m_LastUpdate = Time.time;
                    m_ConnectionOn = true;
                }
                break;
            case "START_TRIAL":
                if (content == "ACK")
                {
                    m_LastUpdate = Time.time;
                    m_ConnectionOn = true;
                }
                break;
            case "GET_ROOM":
                //getting coordinates of the room
                GetRoomCoordinates(content);
                break;
            case "UPD_PLAYER":
                SetPlayerCoordinates(content);
                break;
            case "UPD_FLAGS":
                SetFlagCoordinates(content);
                break;
            default:
                //error
                break;
        }
    }

    public void AddCommandQueue(string command)
    {
        lock (locker)
        {
            commandList.Enqueue(command);
        }
    }

    private void GetRoomCoordinates(string content)
    {
        string[] coordinates = content.Split(';');

        float x = float.Parse(coordinates[0]);
        float y = float.Parse(coordinates[1]);
        float z = float.Parse(coordinates[2]);

        //Updating bounds
        m_TrackedArea.SetRect(x, y);
        StartCoroutine(m_TrackedArea.UpdateBounds());
        m_OffsetedArea.SetRect(x + 2 * z, y + 2 * z);
        StartCoroutine(m_OffsetedArea.UpdateBounds());

    }

    private void SetPlayerCoordinates(string content)
    {
        string[] coordinates = content.Split(';');

        float x = float.Parse(coordinates[0]);
        float y = float.Parse(coordinates[1]);
        float z = float.Parse(coordinates[2]);
        float or_x = float.Parse(coordinates[3]);
        float or_y = float.Parse(coordinates[4]);
        float or_z = float.Parse(coordinates[5]);

        m_Player.transform.localPosition = new Vector3(x, y, z);
        m_Player.transform.rotation = Quaternion.Euler(or_x, or_y, or_z);

    }

    private void SetFlagCoordinates(string content)
    {
        string[] coordinates = content.Split(';');

        float x1 = float.Parse(coordinates[0]);
        float z1 = float.Parse(coordinates[1]);
        float x2 = float.Parse(coordinates[2]);
        float z2 = float.Parse(coordinates[3]);
        float x3 = float.Parse(coordinates[4]);
        float z3 = float.Parse(coordinates[5]);

        m_Flag1.transform.localPosition = new Vector3(x1, -1.0f, z1);
        m_Flag2.transform.localPosition = new Vector3(x2, -1.0f, z2);
        m_Flag3.transform.localPosition = new Vector3(x3, -1.0f, z3);

    }

    #region External Events

    public void StartConnection()
    {


        //m_ipEndPoint = new IPEndPoint(IPAddress.Parse(m_IpAddress), m_Port);
        try
        {
            if (!IPAddress.TryParse(m_InputIPText.text, out m_RegisteredIpAddress))
            {
                PathIntegrationTaskClient.Logger.Log("Ip address is not valid...", PathIntegrationTaskClient.LoggerMessageType.Warning);
                return;
            }

            if (!Int32.TryParse(m_InputPortText.text, out m_RegisteredPort))
            {

            }
            else if ((1024 > m_RegisteredPort || m_RegisteredPort > 65536))
            {
                PathIntegrationTaskClient.Logger.Log("Valid port range: 1024 to 65536...", PathIntegrationTaskClient.LoggerMessageType.Warning);
                return;
            }

            PathIntegrationTaskClient.Logger.Log("Attempting to start connection...", PathIntegrationTaskClient.LoggerMessageType.Info);
            SendCommand("INIT:");
        }
        catch (Exception ex)
        {
            PathIntegrationTaskClient.Logger.Log(ex.Message, PathIntegrationTaskClient.LoggerMessageType.Error);
        }
        
        
    }

    public void StartTrial()
    {
        AddCommandQueue("START_TRIAL:");
    }

    public void StartPractise()
    {
        AddCommandQueue("START_PRACT:");
    }

    #endregion

}
