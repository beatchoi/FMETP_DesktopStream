using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FMETP
{
    public enum FMWebSocketNetworkType { Server, Client, WebSocket }
    public enum FMWebSocketSendType { All, Server, Others, Target }
    public enum FMWebSocketConnectionStatus { Disconnected, WebSocketReady, FMWebSocketConnected }

    [System.Serializable]
    public class ConnectedFMWebSocketClient
    {
        public string wsid = "";
    }

    [AddComponentMenu("FMETP/Network/FMWebSocketManager")]
    public class FMWebSocketManager : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowNetworking = true;
        public bool EditorShowSyncTransformation = true;
        public bool EditorShowEvents = true;
        public bool EditorShowDebug = true;

        public bool EditorShowWebSocketSettings = false;

        public bool EditorShowNetworkObjects = false;

        public bool EditorShowReceiverEvents = false;
        public bool EditorShowConnectionEvents = false;
        #endregion

        /// <summary name="Action_SetIP()">
        /// Assign new IP address for connection
        /// </summary>
        /// <param name="_ip">ip address of server, "127.0.0.1" by default</param>
        public void Action_SetIP(string _ip) { Settings.IP = _ip; }
        /// <summary>
        /// Assign new port number for connection
        /// </summary>
        /// <param name="_port">port of server, (string)3000 -> (int)3000 by default</param>
        public void Action_SetPort(string _port) { Settings.port = int.Parse(_port); }
        /// <summary>
        /// Turn on/off Ssl support
        /// </summary>
        /// <param name="_value">true: enable Ssl; false: disable Ssl</param>
        public void Action_SetSslEnabled(bool _value) { Settings.sslEnabled = _value; }
        /// <summary>
        /// Turn on/off "portRequired"
        /// </summary>
        /// <param name="_value">true: require port; false: not require port</param>
        public void Action_SetPortRequired(bool _value) { Settings.portRequired = _value; }

        [System.Serializable]
        public class FMWebSocketSettings
        {
            public string IP = "127.0.0.1";
            public int port = 3000;
            public bool sslEnabled = false;
            public FMSslProtocols sslProtocols = FMSslProtocols.Default;

            public bool portRequired = true;

            [Tooltip("(( suggested for low-end mobile, but not recommend for streaming large data ))")]
            public bool UseMainThreadSender = true;

            public FMWebSocketConnectionStatus ConnectionStatus = FMWebSocketConnectionStatus.Disconnected;
            public string wsid = "";
            public bool autoReconnect = true;
        }

        public List<ConnectedFMWebSocketClient> ConnectedClients
        {
            get
            {
                if (!Initialised) return null;
                if (NetworkType != FMWebSocketNetworkType.Server) return null;
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    return fmwebsocket_webgl.ConnectedClients;
                }
                else
                {
                    return fmwebsocket.ConnectedClients;
                }
            }
        }

        public static FMWebSocketManager instance;
        private void Awake()
        {
            Application.runInBackground = true;
            if (instance == null) instance = this;

            Initialised = false;
        }

        private void Start() { if (AutoInit) Init(); }
        public bool AutoInit = true;
        public bool Initialised = false;
        public FMWebSocketNetworkType NetworkType = FMWebSocketNetworkType.Client;
        public FMWebSocketSettings Settings;

        [HideInInspector] public FMWebSocket.FMWebSocketComponent fmwebsocket;
        [HideInInspector] public FMWebSocketWebGL.FMWebSocketComponentWebGL fmwebsocket_webgl;
        public UnityEventByteArray OnReceivedByteDataEvent = new UnityEventByteArray();
        public UnityEventString OnReceivedStringDataEvent = new UnityEventString();
        public UnityEventByteArray GetRawReceivedByteDataEvent = new UnityEventByteArray();
        public UnityEventString GetRawReceivedStringDataEvent = new UnityEventString();

        public UnityEventString OnFoundServerEvent = new UnityEventString();
        public UnityEventString OnLostServerEvent = new UnityEventString();
        public UnityEventString OnClientConnectedEvent = new UnityEventString();
        public UnityEventString OnClientDisconnectedEvent = new UnityEventString();

        public bool ShowLog = true;
        public void Connect()
        {
            if (!Initialised)
            {
                Init();
            }
            else
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    fmwebsocket_webgl.Connect();
                }
                else
                {
                    fmwebsocket.Connect();
                }
            }
        }
        public void Close()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                fmwebsocket_webgl.Close();
            }
            else
            {
                fmwebsocket.Close();
            }
            //Initialised = false;
            Settings.ConnectionStatus = FMWebSocketConnectionStatus.Disconnected;
        }

        /// <summary>
        /// Initialise FMWebSocket server
        /// </summary>
        public void Init()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                if (fmwebsocket_webgl == null) fmwebsocket_webgl = gameObject.AddComponent<FMWebSocketWebGL.FMWebSocketComponentWebGL>();
                fmwebsocket_webgl.hideFlags = HideFlags.HideInInspector;

                fmwebsocket_webgl.Manager = this;
                fmwebsocket_webgl.NetworkType = NetworkType;

                fmwebsocket_webgl.IP = Settings.IP;
                fmwebsocket_webgl.port = Settings.port;

                fmwebsocket_webgl.sslEnabled = Settings.sslEnabled;
                fmwebsocket_webgl.sslProtocols = Settings.sslProtocols;
                fmwebsocket_webgl.portRequired = Settings.portRequired;

                fmwebsocket_webgl.autoReconnect = Settings.autoReconnect;

                fmwebsocket_webgl.ShowLog = ShowLog;
            }
            else
            {
                if (fmwebsocket == null) fmwebsocket = gameObject.AddComponent<FMWebSocket.FMWebSocketComponent>();
                fmwebsocket.hideFlags = HideFlags.HideInInspector;

                fmwebsocket.Manager = this;
                fmwebsocket.NetworkType = NetworkType;

                fmwebsocket.IP = Settings.IP;
                fmwebsocket.port = Settings.port;

                fmwebsocket.sslEnabled = Settings.sslEnabled;
                fmwebsocket.sslProtocols = Settings.sslProtocols;
                fmwebsocket.portRequired = Settings.portRequired;

                fmwebsocket.autoReconnect = Settings.autoReconnect;

                fmwebsocket.UseMainThreadSender = Settings.UseMainThreadSender;

                fmwebsocket.ShowLog = ShowLog;
            }
            Initialised = true;
        }

        public void Action_InitAsServer()
        {
            NetworkType = FMWebSocketNetworkType.Server;
            Init();
        }
        public void Action_InitAsClient()
        {
            NetworkType = FMWebSocketNetworkType.Client;
            Init();
        }
        public void Action_InitAsWebSocket()
        {
            NetworkType = FMWebSocketNetworkType.WebSocket;
            Init();
        }

        public void Send(byte[] _byteData, FMWebSocketSendType _type) { Send(_byteData, _type, null); }
        public void Send(string _stringData, FMWebSocketSendType _type) { Send(_stringData, _type, null); }

        public void SendToAll(byte[] _byteData) { Send(_byteData, FMWebSocketSendType.All, null); }
        public void SendToServer(byte[] _byteData) { Send(_byteData, FMWebSocketSendType.Server, null); }
        public void SendToOthers(byte[] _byteData) { Send(_byteData, FMWebSocketSendType.Others, null); }
        public void SendToTarget(byte[] _byteData, string _wsid) { Send(_byteData, FMWebSocketSendType.Target, _wsid); }

        public void SendToAll(string _stringData) { Send(_stringData, FMWebSocketSendType.All, null); }
        public void SendToServer(string _stringData) { Send(_stringData, FMWebSocketSendType.Server, null); }
        public void SendToOthers(string _stringData) { Send(_stringData, FMWebSocketSendType.Others, null); }
        public void SendToTarget(string _stringData, string _wsid) { Send(_stringData, FMWebSocketSendType.Target, _wsid); }

        /// <summary name="Send()">
        /// Send FMWebSocket data as byte[]
        /// </summary>
        /// <param name="_ip">It requires FMWebSocket NetworkType: Server or Client</param>
        public void Send(byte[] _byteData, FMWebSocketSendType _type, string _targetID)
        {
            if (!Initialised) return;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                fmwebsocket_webgl.Send(_byteData, _type, _targetID);
            }
            else
            {
                fmwebsocket.Send(_byteData, _type, _targetID);
            }
        }

        /// <summary name="Send()">
        /// Send FMWebSocket message as string
        /// </summary>
        /// <param name="_ip">It requires FMWebSocket NetworkType: Server or Client</param>
        public void Send(string _stringData, FMWebSocketSendType _type, string _targetID)
        {
            if (!Initialised) return;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                fmwebsocket_webgl.Send(_stringData, _type, _targetID);
            }
            else
            {
                fmwebsocket.Send(_stringData, _type, _targetID);
            }
        }

        /// <summary name="Send()">
        /// Send WebSocket message as string
        /// </summary>
        /// <param name="_ip">It requires FMWebSocket NetworkType: WebSocket</param>
        public void WebSocketSend(string _stringData)
        {
            if (!Initialised) return;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                fmwebsocket_webgl.Send(_stringData);
            }
            else
            {
                fmwebsocket.Send(_stringData);
            }
        }

        /// <summary name="Send()">
        /// Send WebSocket data as byte[]
        /// </summary>
        /// <param name="_ip">It requires FMWebSocket NetworkType: WebSocket</param>
        public void WebSocketSend(byte[] _byteData)
        {
            if (!Initialised) return;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                fmwebsocket_webgl.Send(_byteData);
            }
            else
            {
                fmwebsocket.Send(_byteData);
            }
        }
    }
}