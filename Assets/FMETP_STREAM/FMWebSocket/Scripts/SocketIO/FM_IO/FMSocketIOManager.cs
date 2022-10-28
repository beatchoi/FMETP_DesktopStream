using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FMETP.FMSocketIO;
using System.Threading;
using System.Collections.Concurrent;

namespace FMETP
{
    public struct FMSocketIOData
    {
        public FMSocketIOEmitType EmitType;
        public string DataString;
        public byte[] DataByte;
    }

    public enum FMSocketIONetworkType { Server, Client }
    public enum FMSocketIOEmitType { All, Server, Others }
    public enum FMSslProtocols
    {
        Default = 0xF0,
        None = 0x0,
        Ssl2 = 0xC,
        Ssl3 = 0x30,
        Tls = 0xC0,
#if UNITY_2019_1_OR_NEWER
        Tls11 = 0x300,
        Tls12 = 0xC00
#endif
    }

    [AddComponentMenu("FMETP/Network/FMSocketIOManager")]
    public class FMSocketIOManager : MonoBehaviour
    {
        #region EditorProps

        public bool EditorShowNetworking = true;
        public bool EditorShowSyncTransformation = true;
        public bool EditorShowEvents = true;
        public bool EditorShowDebug = true;

        public bool EditorShowNetworkSettings = false;

        //public bool EditorShowNetworkObjects = false;

        public bool EditorShowReceiverEvents = false;
        //public bool EditorShowConnectionEvents = false;
        #endregion

        public static FMSocketIOManager instance;
        /// <summary>
        /// Auto Initialise and Connect   	
        /// </summary>
        public bool AutoInit = true;

        /// <summary>
        /// Unity3D Network Type for SocketIO connection
        /// </summary>
        public FMSocketIONetworkType NetworkType = FMSocketIONetworkType.Client;

        [System.Serializable]
        public class SocketIOSettings
        {
            public string IP = "127.0.0.1";
            public int port = 3000;
            public bool sslEnabled = false;
            public FMSslProtocols sslProtocols = FMSslProtocols.Default;

            public int reconnectDelay = 5;
            public int ackExpirationTime = 1800;
            public int pingInterval = 25;
            public int pingTimeout = 60;
            public string socketID;

            public bool portRequired = true;
            public bool socketIORequired = true;

            public bool DefaultQueryString = true;
            public string CustomisedQueryString = "?EIO=3&transport=websocket";
        }

        /// <summary>
        /// General Settings for SocketIO connection
        /// </summary>
        public SocketIOSettings Settings;

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
        /// <summary>
        /// Turn on/off "socketIORequired"
        /// </summary>
        /// <param name="_value">true: require socket.io; false: not require socket.io</param>
        public void Action_SetSocketIORequired(bool _value) { Settings.socketIORequired = _value; }

        [HideInInspector]
        public SocketIOComponent socketIO;
        [HideInInspector]
        public SocketIOComponentWebGL socketIOWebGL;

        bool isInitialised = false;
        float DelayInitTimer = 0f;
        [Range(0, 10)]
        public float DelayInitThreshold = 1f;
        bool HasConnected = false;
        public bool Ready = false;

        //networking: receive data
        /// <summary>
        /// Be invoked when received bytes
        /// </summary>
        public UnityEventByteArray OnReceivedByteDataEvent;
        /// <summary>
        /// Be invoked when received strings
        /// </summary>
        public UnityEventString OnReceivedStringDataEvent;
        /// <summary>
        /// Be invoked when received any message
        /// </summary>
        public UnityEventString OnReceivedRawMessageEvent;

        public bool DebugMode = true;
        private void DebugLog(string _value)
        {
            if (!DebugMode) return;
            Debug.Log("FMLog: " + _value);
        }

        public Queue<String> RawMessageQueue = new Queue<String>();

        IEnumerator WaitForSocketIOConnected()
        {
            while (!Ready) yield return null;
            On("OnReceiveData", OnReceivedData);

            StartCoroutine(SenderCOR());
        }

        void OnReceivedData(SocketIOEvent e) { _appendQueueReceivedSocketIOEvent.Enqueue(e); }
        void OnReceivedData(FMSocketIOData _data) { _appendQueueReceivedFMSocketIOData.Enqueue(_data); }

        IEnumerator MainThreadReceiver()
        {
            //client request
            while (!stop)
            {
                yield return null;
                while (_appendQueueReceivedSocketIOEvent.Count > 0)
                {
                    SocketIOEvent _socketIOEvent;
                    if (_appendQueueReceivedSocketIOEvent.TryDequeue(out _socketIOEvent))
                    {
                        FMSocketIOData fmsocketIOData = JsonUtility.FromJson<FMSocketIOData>(_socketIOEvent.data);
                        _appendQueueReceivedFMSocketIOData.Enqueue(fmsocketIOData);
                    }
                }
            }
        }

        IEnumerator OnReceivedDataCOR()
        {
            stop = false;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                StartCoroutine(MainThreadReceiver());
            }
            else
            {
                //vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv ReceivedEvent vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
                receiverThread = new Thread(delegate ()
                {
                    while (!stop)
                    {
                        while (_appendQueueReceivedSocketIOEvent.Count > 0)
                        {
                            SocketIOEvent socketIOEvent;
                            if (_appendQueueReceivedSocketIOEvent.TryDequeue(out socketIOEvent))
                            {
                                FMSocketIOData fmsocketIOData = JsonUtility.FromJson<FMSocketIOData>(socketIOEvent.data);
                                _appendQueueReceivedFMSocketIOData.Enqueue(fmsocketIOData);
                            }
                        }
                        System.Threading.Thread.Sleep(1);
                    }
                });
                receiverThread.IsBackground = true;
                //receiverThread.Priority = System.Threading.ThreadPriority.Highest;
                receiverThread.Start();
                //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ ReceivedEvent ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            }

            while (!stop)
            {
                yield return null;

                while (_appendQueueReceivedFMSocketIOData.Count > 0)
                {
                    FMSocketIOData _data;
                    if (_appendQueueReceivedFMSocketIOData.TryDequeue(out _data))
                    {
                        if (_data.DataString.Length > 1) OnReceivedStringDataEvent.Invoke(_data.DataString);
                        if (_data.DataByte.Length > 1) OnReceivedByteDataEvent.Invoke(_data.DataByte);
                    }
                }
            }

        }

        public void Action_OnReceivedData(string _string) { Debug.Log(_string); }
        public void Action_OnReceivedData(byte[] _byte) { Debug.Log("byte: " + _byte.Length); }

        //Sender functions
        #region Sender
        /// <summary>
        /// Send string to connected clients and/or server, depending on FMSocketIOEmitType
        /// </summary>
        public void Send(string _stringData, FMSocketIOEmitType _type)
        {
            if (!Ready) return;
            FMSocketIOData _data = new FMSocketIOData();
            _data.DataString = _stringData;
            _data.DataByte = new byte[1];
            _data.EmitType = _type;
            _appendQueueSendFMSocketIOData.Enqueue(_data);
        }

        /// <summary>
        /// Send bytes to connected clients and/or server, depending on FMSocketIOEmitType
        /// </summary>
        public void Send(byte[] _byteData, FMSocketIOEmitType _type)
        {
            if (!Ready) return;
            FMSocketIOData _data = new FMSocketIOData();
            _data.DataString = "";
            _data.DataByte = _byteData;
            //_data.DataByte = new byte[561030*2];
            _data.EmitType = _type;
            _appendQueueSendFMSocketIOData.Enqueue(_data);
        }

        private bool stop = false;
        private Thread senderThread;
        private Thread receiverThread;

        private ConcurrentQueue<FMSocketIOData> _appendQueueSendFMSocketIOData = new ConcurrentQueue<FMSocketIOData>();
        private ConcurrentQueue<FMSocketIOData> _appendQueueReceivedFMSocketIOData = new ConcurrentQueue<FMSocketIOData>();
        private ConcurrentQueue<SocketIOEvent> _appendQueueReceivedSocketIOEvent = new ConcurrentQueue<SocketIOEvent>();

        //string SenderJson(ref bool _result)
        //{
        //    FMSocketIOData _data;
        //    if (_appendQueueSendFMSocketIOData.TryDequeue(out _data))
        //    {
        //        _result = true;
        //        return JsonUtility.ToJson(_data);
        //    }
        //    else
        //    {
        //        _result = false;
        //        return null;
        //    }
        //}
        //private bool SenderJson(out string _result)
        //{
        //    FMSocketIOData _data;
        //    if (_appendQueueSendFMSocketIOData.TryDequeue(out _data))
        //    {
        //        _result = JsonUtility.ToJson(_data);
        //        return true;
        //    }

        //    _result = null;
        //    return false;
        //}

        private void SenderJson(Action<string> callback)
        {
            FMSocketIOData _data;
            if (_appendQueueSendFMSocketIOData.TryDequeue(out _data))
            {
                callback(JsonUtility.ToJson(_data));
            }
            else
            {
                callback(null);
            }
        }

        IEnumerator SenderCOR()
        {
            stop = false;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                while (!stop)
                {
                    yield return null;
                    while (_appendQueueSendFMSocketIOData.Count > 0)
                    {
                        SenderJson((string jsonString) => { if (jsonString != null) Emit("OnReceiveData", jsonString); });
                    }
                }
            }
            else
            {

                //vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv Sender vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
                senderThread = new Thread(delegate ()
                {
                    while (!stop)
                    {
                        while (_appendQueueSendFMSocketIOData.Count > 0)
                        {
                            SenderJson((string jsonString) => { if (jsonString != null) Emit("OnReceiveData", jsonString); });
                        }
                        System.Threading.Thread.Sleep(1);
                    }
                });
                senderThread.IsBackground = true;
                //senderThread.Priority = System.Threading.ThreadPriority.Highest;
                senderThread.Start();
                //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Sender ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            }
        }

        /// <summary>
        /// Send bytes to connected clients and server
        /// </summary>
        public void SendToAll(byte[] _byteData)
        {
            Send(_byteData, FMSocketIOEmitType.All);
        }
        /// <summary>
        /// Send bytes to server
        /// </summary>
        public void SendToServer(byte[] _byteData)
        {
            Send(_byteData, FMSocketIOEmitType.Server);
        }
        /// <summary>
        /// Send bytes to connected clients and server, except sender
        /// </summary>
        public void SendToOthers(byte[] _byteData)
        {
            Send(_byteData, FMSocketIOEmitType.Others);
        }
        /// <summary>
        /// Send string to connected clients and server
        /// </summary>
        public void SendToAll(string _stringData)
        {
            Send(_stringData, FMSocketIOEmitType.All);
        }
        /// <summary>
        /// Send string to server
        /// </summary>
        public void SendToServer(string _stringData)
        {
            Send(_stringData, FMSocketIOEmitType.Server);
        }
        /// <summary>
        /// Send string to connected clients and server, except sender
        /// </summary>
        public void SendToOthers(string _stringData)
        {
            Send(_stringData, FMSocketIOEmitType.Others);
        }
        #endregion

        void Awake()
        {
            Application.runInBackground = true;
            if (instance == null) instance = this;

            isInitialised = false;
            HasConnected = false;
            Ready = false;
        }

        private void Start()
        {
            //auto init?
            if (AutoInit) Init();
        }

        void Update()
        {
            if (isInitialised) DelayInitTimer += Time.deltaTime;

            if (DelayInitTimer > DelayInitThreshold)
            {
                if (!HasConnected)
                {
                    HasConnected = true;
                    Connect();

                    if (Settings.socketIORequired)
                    {
                        On("connect", (SocketIOEvent e) =>
                        {
                            DebugLog("SocketIO connected");
                        //Ready = true;
                        if (NetworkType == FMSocketIONetworkType.Server) Emit("RegServerId");
                        });
                        //On("open", (SocketIOEvent e) =>
                        //{
                        //    Debug.Log("SocketIO opened");
                        //    Ready = true;
                        //});
                    }
                }
                Ready = IsWebSocketConnected();
            }

            //show recorded messages
            //RawMessageRecord = _RawMessageRecord;
        }

        /// <summary>
        /// Initialise SocketIO server, will connect as Unity3D(Server), Require socket.io
        /// </summary>
        public void InitAsServer()
        {
            NetworkType = FMSocketIONetworkType.Server;
            Init();
        }
        /// <summary>
        /// Initialise SocketIO server, will connect as Unity3D(Client), Require socket.io
        /// </summary>
        public void InitAsClient()
        {
            NetworkType = FMSocketIONetworkType.Client;
            Init();
        }
        /// <summary>
        /// Initialise SocketIO server
        /// </summary>
        public void Init()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                if (socketIOWebGL == null) socketIOWebGL = gameObject.AddComponent<SocketIOComponentWebGL>();
                socketIOWebGL.hideFlags = HideFlags.HideInInspector;

                socketIOWebGL.IP = Settings.IP;
                socketIOWebGL.port = Settings.port;

                socketIOWebGL.sslEnabled = Settings.sslEnabled;

                socketIOWebGL.portRequired = Settings.portRequired;
                socketIOWebGL.socketIORequired = Settings.socketIORequired;

                socketIOWebGL.DefaultQueryString = Settings.DefaultQueryString;
                socketIOWebGL.CustomisedQueryString = Settings.CustomisedQueryString;

                socketIOWebGL.Init();
            }
            else
            {
                if (socketIO == null) socketIO = gameObject.AddComponent<SocketIOComponent>();
                socketIO.hideFlags = HideFlags.HideInInspector;

                socketIO.IP = Settings.IP;
                socketIO.port = Settings.port;

                socketIO.sslEnabled = Settings.sslEnabled;
                socketIO.sslProtocols = Settings.sslProtocols;

                socketIO.reconnectDelay = Settings.reconnectDelay;
                socketIO.ackExpirationTime = Settings.ackExpirationTime;
                socketIO.pingInterval = Settings.pingInterval;
                socketIO.pingTimeout = Settings.pingTimeout;

                socketIO.portRequired = Settings.portRequired;
                socketIO.socketIORequired = Settings.socketIORequired;

                socketIO.DefaultQueryString = Settings.DefaultQueryString;
                socketIO.CustomisedQueryString = Settings.CustomisedQueryString;

                socketIO.Init();
            }
            isInitialised = true;
            DelayInitTimer = 0f;

            StartCoroutine(WaitForSocketIOConnected());
            StartCoroutine(OnReceivedDataCOR());
        }

        /// <summary>
        /// return WebSocket connection status
        /// </summary>
        public bool IsWebSocketConnected()
        {
            if (!isInitialised) return false;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                return socketIOWebGL.IsWebSocketConnected();
            }
            else
            {
                return socketIO.IsWebSocketConnected();
            }
        }

        /// <summary>
        /// Connect to WebSocket server, initialisation is required
        /// </summary>
        public void Connect()
        {
            if (!isInitialised)
            {
                Init();
                return;
            }

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                socketIOWebGL.Connect();
            }
            else
            {
                socketIO.Connect();
            }
        }

        /// <summary>
        /// Close WebSocket connection
        /// </summary>
        public void Close()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                socketIOWebGL.Close();
            }
            else
            {
                socketIO.Close();
            }

            isInitialised = false;
            HasConnected = false;
            Ready = false;
        }

        public void Emit(string e)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                socketIOWebGL.Emit(e);
            }
            else
            {
                socketIO.Emit(e);
            }
        }
        public void Emit(string e, Action<string> action)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                socketIOWebGL.Emit(e, action);
            }
            else
            {
                socketIO.Emit(e, action);
            }
        }
        public void Emit(string e, string data)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                socketIOWebGL.Emit(e, data);
            }
            else
            {
                socketIO.Emit(e, data);
            }
        }
        public void Emit(string e, string data, Action<string> action)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                socketIOWebGL.Emit(e, data, action);
            }
            else
            {
                socketIO.Emit(e, data, action);
            }
        }

        public void On(string e, Action<SocketIOEvent> callback)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                socketIOWebGL.On(e, callback);
            }
            else
            {
                socketIO.On(e, callback);
            }
        }
        public void Off(string e, Action<SocketIOEvent> callback)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                socketIOWebGL.Off(e, callback);
            }
            else
            {
                socketIO.Off(e, callback);
            }
        }

        void StopAll()
        {
            if (HasConnected) Close();

            stop = true;
            StopAllCoroutines();

            if (senderThread != null) senderThread.Abort();
            if (receiverThread != null) receiverThread.Abort();
        }

        private void OnEnable()
        {
            if (Time.realtimeSinceStartup < 3f) return;
            if (AutoInit) Init();

            StartCoroutine(SenderCOR());
            StartCoroutine(OnReceivedDataCOR());
        }

        private void OnDisable() { StopAll(); }
        private void OnApplicationQuit() { StopAll(); }
        private void OnDestroy() { StopAll(); }
    }
}