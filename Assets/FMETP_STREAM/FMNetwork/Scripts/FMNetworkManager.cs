using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;


using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace FMETP
{
    public enum FMProtocol { UDP, TCP }
    public enum FMNetworkType { Server, Client, DataStream }
    public enum FMSendType { All, Server, Others, TargetIP }
    public enum FMClientSignal { none, handshake, close }
    public enum FMAckSignal { none, ackRespone, ackReceived }

    public enum FMDataStreamType { Receiver, Sender }
    public enum FMUDPListenerType { Unicast, Multicast, Broadcast }
    public struct FMPacket
    {
        public byte[] SendByte;
        public string SkipIP;
        public FMSendType SendType;
        public string TargetIP;

        public bool Reliable;
        public UInt16 syncID;
    }
    public struct FMNetworkTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }

    [AddComponentMenu("FMETP/Network/FMNetworkManager")]
    public class FMNetworkManager : MonoBehaviour
    {
        #region EditorProps

        public bool EditorShowNetworking = true;
        public bool EditorShowSyncTransformation = true;
        public bool EditorShowEvents = true;
        public bool EditorShowDebug = true;

        public bool EditorShowServerSettings = false;
        public bool EditorShowClientSettings = false;
        public bool EditorShowDataStreamSettings = false;

        public bool EditorShowNetworkObjects = false;

        public bool EditorShowReceiverEvents = false;
        public bool EditorShowConnectionEvents = false;
        #endregion

        public string LocalIPAddress()
        {
            string localIP = "0.0.0.0";
            //ssIPHostEntry host;
            //host = Dns.GetHostEntry(Dns.GetHostName());

            List<string> detectedIPs = new List<string>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                //if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                //commented above condition, as it may not work on Android, found issues on Google Pixel Phones, its type returns "0" for unknown reason.
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (ip.IsDnsEligible)
                            {
                                string detectedIP = ip.Address.ToString();
                                if (detectedIP != "127.0.0.1" && detectedIP != "0.0.0.0")
                                {
                                    try
                                    {
                                        if (ip.AddressValidLifetime / 2 != int.MaxValue)
                                        {
                                            localIP = detectedIP;
                                        }
                                        else
                                        {
                                            //if didn't find any yet, this is the only one
                                            if (localIP == "0.0.0.0") localIP = detectedIP;
                                        }
                                    }
                                    catch
                                    {
                                        localIP = detectedIP;
                                    }

                                    detectedIPs.Add(localIP);
                                }
                            }
                        }
                    }
                }
            }

#if UNITY_EDITOR || UNITY_STANDALONE || WINDOWS_UWP
        if (detectedIPs.Count > 1)
        {
            string endPointIP = "0.0.0.0";
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    endPointIP = endPoint.Address.ToString();
                    if (socket.Connected) socket.Disconnect(true);
                }
            }
            catch { }

            for (int i = 0; i < detectedIPs.Count; i++)
            {
                if (detectedIPs[i] == endPointIP) localIP = detectedIPs[i];
            }
        }
#endif
            return localIP;
        }

        private string _localIP;
        public string ReadLocalIPAddress
        {
            get
            {
                if (_localIP == null) _localIP = LocalIPAddress();
                if (_localIP.Length <= 3) _localIP = LocalIPAddress();
                return _localIP;
            }
        }

        public static FMNetworkManager instance;
        public bool AutoInit = true;

        [HideInInspector]
        public bool Initialised = false;

        [Tooltip("Initialise as Server or Client. Otherwise, using DataStream for general udp or tcp streaming from Gstreamer and StereoPi")]
        public FMNetworkType NetworkType;

        [HideInInspector] public FMServer.FMServerComponent Server;
        [HideInInspector] public FMClient.FMClientComponent Client;
        [HideInInspector] public FMDataStream.FMDataStreamComponent DataStream;

        [Serializable]
        public class FMServerSettings
        {
            public int ServerListenPort = 3333;

            [Tooltip("(( on supported devices only ))")]
            public bool UseAsyncListener = false;

            [Tooltip("(( suggested for low-end mobile, but not recommend for streaming large data ))")]
            public bool UseMainThreadSender = true;

            [Tooltip("(( use Multicast for Server to All Clients, for reducing server's loading))")]
            public bool SupportMulticast = false;

            [Tooltip("(( Multicast Address, cannot change during runtime))")]
            public string MulticastAddress = "239.255.255.255";

            public int ConnectionCount;
        }

        [Serializable]
        public class FMClientSettings
        {
            public int ClientListenPort = 3334;

            [Tooltip("(( suggested for low-end mobile, but not recommend for streaming large data ))")]
            public bool UseMainThreadSender = true;

            [Tooltip("(( Experimental: broadcast data to all devices in local network, and this client will be discovered and registered by multiple servers. However, it's not reliable for important data. ))")]
            public bool ForceBroadcast = false;

            [Tooltip("(( Join Multicast Group))")]
            public bool SupportMulticast = false;
            [Tooltip("(( Multicast Address, cannot change during runtime))")]
            public string MulticastAddress = "239.255.255.255";

            [Tooltip("(( true by default ))")]
            public bool AutoNetworkDiscovery = true;
            [Tooltip("(( only applied when Auto Network Discovery is off ))")]
            public string ServerIP;
            public bool IsConnected;
        }

        [Serializable]
        public class FMDataStreamSettings
        {
            public FMDataStreamType DataStreamType = FMDataStreamType.Receiver;

            public FMProtocol DataStreamProtocol = FMProtocol.UDP;
            public int ClientListenPort = 3001;

            [Tooltip("(( UDP Listener Type))")]
            public FMUDPListenerType UDPListenerType = FMUDPListenerType.Unicast;
            [Tooltip("(( Multicast Address, cannot change during runtime))")]
            public string MulticastAddress = "239.255.255.255";

            public bool IsConnected;

            //sender
            public string ClientIP = "127.0.0.1";
            public bool UseMainThreadSender = true;
        }

        [Tooltip("Network Settings for Server")]
        public FMServerSettings ServerSettings;
        [Tooltip("Network Settings for Client")]
        public FMClientSettings ClientSettings;
        [Tooltip("Network Settings for DataStream")]
        public FMDataStreamSettings DataStreamSettings;

        public bool DebugStatus = true;
        public bool ShowLog = true;
        [TextArea(1, 10)]
        public string Status;
        public Text UIStatus;

        public UnityEventByteArray OnReceivedByteDataEvent = new UnityEventByteArray();
        public UnityEventString OnReceivedStringDataEvent = new UnityEventString();
        public UnityEventByteArray GetRawReceivedData = new UnityEventByteArray();

        //server events
        public UnityEventString OnClientConnectedEvent = new UnityEventString();
        public UnityEventString OnClientDisconnectedEvent = new UnityEventString();
        public void OnClientConnected(string ClientIP)
        {
            OnClientConnectedEvent.Invoke(ClientIP);
            if (ShowLog) Debug.Log("OnClientConnected: " + ClientIP);

            //force reset network sync timestamp if owned network objects
            if (NetworkObjects.Length > 0) Server.Action_AddNetworkObjectPacket(new byte[]{ 0 }, FMSendType.TargetIP);
        }
        public void OnClientDisconnected(string ClientIP)
        {
            OnClientDisconnectedEvent.Invoke(ClientIP);
            if (ShowLog) Debug.Log("OnClientDisonnected: " + ClientIP);
        }

        //client events
        public UnityEventString OnFoundServerEvent = new UnityEventString();
        public UnityEventString OnLostServerEvent = new UnityEventString();
        public void OnFoundServer(string ServerIP)
        {
            OnFoundServerEvent.Invoke(ServerIP);
            if (ShowLog) Debug.Log("OnFoundServer: " + ServerIP);

            ResetNetworkObjectSyncTimestamp();
        }

        private void ResetNetworkObjectSyncTimestamp()
        {
            //reset network sync timestamp
            CurrentTimestamp = 0f;
            LastReceivedTimestamp = 0f;
            TargetTimestamp = 0f;
        }

        public void OnLostServer(string ServerIP)
        {
            OnLostServerEvent.Invoke(ServerIP);
            if (ShowLog) Debug.Log("OnLostServer: " + ServerIP);
        }

        #region Network Objects Setup
        [Header("[ Sync ] Server => Client")]
        [Tooltip("Sync Transformation of Network Objects. # Both Server and Clients should have same number of NetworkObjects")]
        public GameObject[] NetworkObjects;
        FMNetworkTransform[] NetworkTransform;

        //[Tooltip("Frequency for sync (second)")]
        private float SyncFrequency = 0.05f;
        [Range(1f, 60f)]
        public float SyncFPS = 20f;
        private float SyncTimer = 0f;
        private float SyncFPS_old = -1;

        [Tooltip("When enabled, the networked objects in lists will be sync from server to clients")]
        public bool EnableNetworkObjectsSync = true;

        private float LastReceivedTimestamp = 0f;
        private float TargetTimestamp = 0f;
        private float CurrentTimestamp = 0f;

        private void Action_SendNetworkObjectTransform()
        {
            if (NetworkType == FMNetworkType.Server)
            {
                byte[] Timestamp = BitConverter.GetBytes(Time.realtimeSinceStartup);

                byte[] Data = new byte[NetworkObjects.Length * 10 * 4];
                byte[] SendByte = new byte[Timestamp.Length + Data.Length];

                int index = 0;
                Buffer.BlockCopy(Timestamp, 0, SendByte, index, Timestamp.Length);
                index += Timestamp.Length;

                foreach (GameObject obj in NetworkObjects)
                {
                    byte[] TransformByte = EncodeTransformByte(obj);
                    Buffer.BlockCopy(TransformByte, 0, SendByte, index, TransformByte.Length);
                    index += TransformByte.Length;
                }
                Server.Action_AddNetworkObjectPacket(SendByte, FMSendType.Others);
            }
        }

        private byte[] EncodeTransformByte(GameObject obj)
        {
            byte[] _byte = new byte[40];
            Vector3 _pos = obj.transform.position;
            Quaternion _rot = obj.transform.rotation;
            Vector3 _scale = obj.transform.localScale;

            float[] _float = new float[]
            {
            _pos.x,_pos.y,_pos.z,
            _rot.x,_rot.y,_rot.z,_rot.w,
            _scale.x,_scale.y,_scale.z
            };
            Buffer.BlockCopy(_float, 0, _byte, 0, _byte.Length);
            return _byte;
        }

        private float[] DecodeByteToFloatArray(byte[] _data, int _offset)
        {
            float[] _transform = new float[10];
            for (int i = 0; i < _transform.Length; i++)
            {
                _transform[i] = BitConverter.ToSingle(_data, i * 4 + _offset);
            }

            return _transform;
        }

        internal void Action_SyncNetworkObjectTransform(byte[] _data)
        {
            if (_data.Length <= 4)
            {
                ResetNetworkObjectSyncTimestamp();
                return;
            }

            float Timestamp = BitConverter.ToSingle(_data, 0);
            int meta_offset = 4;

            if (Timestamp > LastReceivedTimestamp)
            {
                LastReceivedTimestamp = TargetTimestamp;
                TargetTimestamp = Timestamp;
                CurrentTimestamp = LastReceivedTimestamp;

                for (int i = 0; i < NetworkObjects.Length; i++)
                {
                    float[] _transform = DecodeByteToFloatArray(_data, meta_offset + i * 40);
                    NetworkTransform[i].position = new Vector3(_transform[0], _transform[1], _transform[2]);
                    NetworkTransform[i].rotation = new Quaternion(_transform[3], _transform[4], _transform[5], _transform[6]);
                    NetworkTransform[i].localScale = new Vector3(_transform[7], _transform[8], _transform[9]);
                }
            }
        }
        #endregion

        public void Action_InitAsServer()
        {
            NetworkType = FMNetworkType.Server;
            Init();
        }

        public void Action_InitAsClient()
        {
            NetworkType = FMNetworkType.Client;
            Init();
        }

        public void Action_InitDataStream()
        {
            NetworkType = FMNetworkType.DataStream;
            Init();
        }
        public void Action_InitDataStream(string inputClientIP)
        {
            DataStreamSettings.ClientIP = inputClientIP;
            NetworkType = FMNetworkType.DataStream;
            Init();
        }

        /// <summary>
        /// Close connection locally, for either Server or Client
        /// </summary>
        public void Action_Close()
        {
            Initialised = false;
            if (Server != null) Destroy(Server);
            if (Client != null) Destroy(Client);
            if (DataStream != null) Destroy(DataStream);

            ServerSettings.ConnectionCount = 0;
            ClientSettings.IsConnected = false;
            DataStreamSettings.IsConnected = false;

            UpdateDebugText();

            GC.Collect();
        }

        /// <summary>
        /// Server Commands only, close client's connection remotely
        /// </summary>
        public void Action_CloseClientConnection(string _clientIP)
        {
            if (NetworkType != FMNetworkType.Server) return;
            if (!Server.IsConnected) return;
            Server.Action_CloseClientConnection(_clientIP);
        }

        /// <summary>
        /// Server Commands only, close all clients' connection remotely
        /// </summary>
        public void Action_CloseAllClientsConnection()
        {
            if (NetworkType != FMNetworkType.Server) return;
            if (!Server.IsConnected) return;

            if (ServerSettings.ConnectionCount > 0)
            {
                for (int i = 0; i < Server.ConnectedIPs.Count; i++)
                {
                    Server.Action_CloseClientConnection(Server.ConnectedIPs[i]);
                }
            }
        }

        void Init()
        {
            if (Initialised) Action_Close();

            switch (NetworkType)
            {
                case FMNetworkType.Server:
                    Server = this.gameObject.AddComponent<FMServer.FMServerComponent>();
                    Server.hideFlags = HideFlags.HideInInspector;

                    Server.Manager = this;

                    Server.ServerListenPort = ServerSettings.ServerListenPort;
                    Server.ClientListenPort = ClientSettings.ClientListenPort;

                    Server.UseAsyncListener = ServerSettings.UseAsyncListener;
                    Server.UseMainThreadSender = ServerSettings.UseMainThreadSender;

                    Server.SupportMulticast = ServerSettings.SupportMulticast;
                    Server.MulticastAddress = ServerSettings.MulticastAddress;

                    break;
                case FMNetworkType.Client:
                    Client = this.gameObject.AddComponent<FMClient.FMClientComponent>();
                    Client.hideFlags = HideFlags.HideInInspector;

                    Client.Manager = this;

                    Client.ServerListenPort = ServerSettings.ServerListenPort;
                    Client.ClientListenPort = ClientSettings.ClientListenPort;

                    Client.UseMainThreadSender = ClientSettings.UseMainThreadSender;
                    Client.AutoNetworkDiscovery = ClientSettings.AutoNetworkDiscovery;
                    if (ClientSettings.ServerIP == "") ClientSettings.ServerIP = "127.0.0.1";
                    if (!Client.AutoNetworkDiscovery) Client.ServerIP = ClientSettings.ServerIP;

                    Client.ForceBroadcast = ClientSettings.ForceBroadcast;
                    Client.SupportMulticast = ClientSettings.SupportMulticast;
                    Client.MulticastAddress = ClientSettings.MulticastAddress;

                    NetworkTransform = new FMNetworkTransform[NetworkObjects.Length];
                    for (int i = 0; i < NetworkTransform.Length; i++)
                    {
                        NetworkTransform[i] = new FMNetworkTransform();
                        NetworkTransform[i].position = Vector3.zero;
                        NetworkTransform[i].rotation = Quaternion.identity;
                        NetworkTransform[i].localScale = new Vector3(1f, 1f, 1f);
                    }
                    break;
                case FMNetworkType.DataStream:
                    DataStream = this.gameObject.AddComponent<FMDataStream.FMDataStreamComponent>();
                    DataStream.hideFlags = HideFlags.HideInInspector;

                    DataStream.Manager = this;

                    DataStream.DataStreamType = DataStreamSettings.DataStreamType;

                    DataStream.Protocol = DataStreamSettings.DataStreamProtocol;
                    DataStream.ClientListenPort = DataStreamSettings.ClientListenPort;
                    DataStream.UDPListenerType = DataStreamSettings.UDPListenerType;
                    DataStream.MulticastAddress = DataStreamSettings.MulticastAddress;

                    DataStream.ClientIP = DataStreamSettings.ClientIP;
                    DataStream.UseMainThreadSender = DataStreamSettings.UseMainThreadSender;

                    break;
            }

            Initialised = true;
        }

        void Awake()
        {
            Application.runInBackground = true;
            if (instance == null) instance = this;
        }

        //void Awake()
        //{
        //    if (instance == null)
        //    {
        //        instance = this;
        //        this.gameObject.transform.parent = null;
        //        DontDestroyOnLoad(this.gameObject);
        //    }
        //    else
        //    {
        //        Destroy(this.gameObject);
        //    }
        //}

        private void OnEnable()
        {
            if (!Initialised) return;
            switch (NetworkType)
            {
                case FMNetworkType.Server:
                    if (Server != null) Server.enabled = true;
                    break;
                case FMNetworkType.Client:
                    if (Client != null) Client.enabled = true;
                    break;
                case FMNetworkType.DataStream:
                    if (DataStream != null) DataStream.enabled = true;
                    break;
            }

            UpdateDebugText();
        }
        private void OnDisable()
        {
            if (!Initialised) return;
            switch (NetworkType)
            {
                case FMNetworkType.Server:
                    if (Server != null) Server.enabled = false;
                    break;
                case FMNetworkType.Client:
                    if (Client != null) Client.enabled = false;
                    break;
                case FMNetworkType.DataStream:
                    if (DataStream != null) DataStream.enabled = false;
                    break;
            }

            UpdateDebugText(true);
        }

        // Use this for initialization
        void Start() { if (AutoInit) Init(); }

        // Update is called once per frame
        void Update()
        {
            if (Initialised == false) return;
            switch (NetworkType)
            {
                case FMNetworkType.Server:
                    //====================Sync Network Object============================
                    if (EnableNetworkObjectsSync)
                    {
                        if (Server.ConnectionCount > 0)
                        {
                            if (NetworkObjects.Length > 0)
                            {
                                //on sync fps changes, reset the timer..
                                if (SyncFPS != SyncFPS_old)
                                {
                                    SyncFPS_old = SyncFPS;
                                    SyncTimer = 0f;
                                }

                                SyncFrequency = 1f / SyncFPS;
                                SyncTimer += Time.deltaTime;
                                if (SyncTimer > SyncFrequency)
                                {
                                    Action_SendNetworkObjectTransform();
                                    SyncTimer = SyncTimer % SyncFrequency;
                                }
                            }
                        }
                    }
                    Server.ShowLog = ShowLog;
                    //====================Sync Network Object============================
                    ServerSettings.ConnectionCount = Server.ConnectionCount;
                    break;
                case FMNetworkType.Client:
                    //====================Sync Network Object============================
                    if (EnableNetworkObjectsSync)
                    {
                        if (Client.IsConnected)
                        {
                            if (NetworkObjects.Length > 0)
                            {
                                for (int i = 0; i < NetworkObjects.Length; i++)
                                {
                                    CurrentTimestamp += Time.deltaTime;
                                    float step = (CurrentTimestamp - LastReceivedTimestamp) / (TargetTimestamp - LastReceivedTimestamp);
                                    step = Mathf.Clamp(step, 0f, 1f);
                                    NetworkObjects[i].transform.position = Vector3.Slerp(NetworkObjects[i].transform.position, NetworkTransform[i].position, step);
                                    NetworkObjects[i].transform.rotation = Quaternion.Slerp(NetworkObjects[i].transform.rotation, NetworkTransform[i].rotation, step);
                                    NetworkObjects[i].transform.localScale = Vector3.Slerp(NetworkObjects[i].transform.localScale, NetworkTransform[i].localScale, step);
                                }
                            }
                        }
                    }
                    Client.ShowLog = ShowLog;
                    //====================Sync Network Object============================
                    ClientSettings.IsConnected = Client.IsConnected;
                    break;
                case FMNetworkType.DataStream:
                    DataStream.ShowLog = ShowLog;
                    DataStreamSettings.IsConnected = DataStream.IsConnected;
                    break;
            }

            UpdateDebugText();
        }

        private void UpdateDebugText(bool onNetworkManagerDisabled = false)
        {
            //====================Update Debug Text============================
            #region Debug Status
            if (DebugStatus)
            {
                string _status = "";
                _status += "Thread: " + Loom.numThreads + " / " + Loom.maxThreads + "\n";
                _status += "Network Type: " + NetworkType.ToString() + "\n";
                _status += "Local IP: " + ReadLocalIPAddress + "\n";

                if (!onNetworkManagerDisabled)
                {
                    switch (NetworkType)
                    {
                        case FMNetworkType.Server:
                            _status += "Connection Count: " + ServerSettings.ConnectionCount + "\n";
                            _status += "Async Listener: " + ServerSettings.UseAsyncListener + "\n";
                            _status += "Use Main Thread Sender: " + ServerSettings.UseMainThreadSender + "\n";

                            foreach (FMServer.FMServerComponent.ConnectedClient _cc in Server.ConnectedClients)
                            {
                                if (_cc != null)
                                {
                                    _status += "connected ip: " + _cc.IP + "\n";

                                    _status += "last seen: " + _cc.LastSeenTimeMS + "\n";
                                    _status += "last send: " + _cc.LastSentTimeMS + "\n";
                                }
                                else
                                {
                                    _status += "Connected Client: null/unknown issue" + "\n";
                                }
                            }
                            break;
                        case FMNetworkType.Client:
                            _status += "Is Connected: " + ClientSettings.IsConnected + "\n";
                            _status += "Use Main Thread Sender: " + ClientSettings.UseMainThreadSender + "\n";

                            if (ClientSettings.IsConnected)
                            {
                                _status += "last send: " + Client.LastSentTimeMS + "\n";
                                _status += "last received: " + Client.LastReceivedTimeMS + "\n";
                            }
                            break;
                        case FMNetworkType.DataStream:
                            _status += "Is Connected: " + DataStream.IsConnected + "\n";
                            _status += "last received: " + DataStream.LastReceivedTimeMS + "\n";
                            break;
                    }
                }
                else
                {
                    switch (NetworkType)
                    {
                        case FMNetworkType.Server:
                            _status += "Connection Count: " + "0" + "\n";
                            _status += "Async Listener: " + ServerSettings.UseAsyncListener + "\n";
                            _status += "Use Main Thread Sender: " + ServerSettings.UseMainThreadSender + "\n";
                            break;
                        case FMNetworkType.Client:
                            _status += "Is Connected: " + false + "\n";
                            _status += "Use Main Thread Sender: " + ClientSettings.UseMainThreadSender + "\n";
                            break;
                        case FMNetworkType.DataStream:
                            _status += "Is Connected: " + false + "\n";
                            break;
                    }
                }

                Status = _status;
                if (UIStatus != null) UIStatus.text = Status;
            }
            #endregion
            //====================Update Debug Text============================
        }

        #region SENDER MAPPING
        public void StreamData(byte[] _byteData)
        {
            if (!Initialised) return;
            if (NetworkType != FMNetworkType.DataStream) return;
            DataStream.Action_AddBytes(_byteData);
        }
        public void Send(byte[] _byteData, FMSendType _type, bool _reliable = false) { Send(_byteData, _type, null, _reliable); }
        public void Send(string _stringData, FMSendType _type, bool _reliable = false) { Send(_stringData, _type, null, _reliable); }

        public void SendToAll(byte[] _byteData) { Send(_byteData, FMSendType.All, null, false); }
        public void SendToServer(byte[] _byteData) { Send(_byteData, FMSendType.Server, null, false); }
        public void SendToOthers(byte[] _byteData) { Send(_byteData, FMSendType.Others, null, false); }

        public void SendToAll(string _stringData) { Send(_stringData, FMSendType.All, null, false); }
        public void SendToServer(string _stringData) { Send(_stringData, FMSendType.Server, null, false); }
        public void SendToOthers(string _stringData) { Send(_stringData, FMSendType.Others, null, false); }

        public void SendToAllReliable(byte[] _byteData) { Send(_byteData, FMSendType.All, null, true); }
        public void SendToServerReliable(byte[] _byteData) { Send(_byteData, FMSendType.Server, null, true); }
        public void SendToOthersReliable(byte[] _byteData) { Send(_byteData, FMSendType.Others, null, true); }

        public void SendToAllReliable(string _stringData) { Send(_stringData, FMSendType.All, null, true); }
        public void SendToServerReliable(string _stringData) { Send(_stringData, FMSendType.Server, null, true); }
        public void SendToOthersReliable(string _stringData) { Send(_stringData, FMSendType.Others, null, true); }

        public void SendToTargetReliable(byte[] _byteData, string _targetIP) { SendToTarget(_byteData, _targetIP, true); }
        public void SendToTargetReliable(string _stringData, string _targetIP) { SendToTarget(_stringData, _targetIP, true); }

        public void SendToTarget(byte[] _byteData, string _targetIP, bool _reliable = false)
        {
            if (NetworkType == FMNetworkType.Server)
            {
                if (Server.ConnectedIPs.Contains(_targetIP))
                {
                    Send(_byteData, FMSendType.TargetIP, _targetIP, _reliable);
                }
                else
                {
                    if (_targetIP == ReadLocalIPAddress || _targetIP == "127.0.0.1" || _targetIP == "localhost")
                    {
                        OnReceivedByteDataEvent.Invoke(_byteData);
                    }
                }
            }
            else
            {
                if (_targetIP == ReadLocalIPAddress || _targetIP == "127.0.0.1" || _targetIP == "localhost")
                {
                    OnReceivedByteDataEvent.Invoke(_byteData);
                }
                else
                {
                    Send(_byteData, FMSendType.TargetIP, _targetIP, _reliable);
                }
            }
        }
        public void SendToTarget(string _stringData, string _targetIP, bool _reliable = false)
        {
            if (NetworkType == FMNetworkType.Server)
            {
                if (Server.ConnectedIPs.Contains(_targetIP))
                {
                    Send(_stringData, FMSendType.TargetIP, _targetIP, _reliable);
                }
                else
                {
                    if (_targetIP == ReadLocalIPAddress || _targetIP == "127.0.0.1" || _targetIP == "localhost")
                    {
                        OnReceivedStringDataEvent.Invoke(_stringData);
                    }
                }
            }
            else
            {
                if (_targetIP == ReadLocalIPAddress || _targetIP == "127.0.0.1" || _targetIP == "localhost")
                {
                    OnReceivedStringDataEvent.Invoke(_stringData);
                }
                else
                {
                    Send(_stringData, FMSendType.TargetIP, _targetIP, _reliable);
                }
            }
        }

        private void Send(byte[] _byteData, FMSendType _type, string _targetIP, bool _reliable = false)
        {
            if (!Initialised) return;
            if (NetworkType == FMNetworkType.Client && !Client.IsConnected) return;

            if (NetworkType == FMNetworkType.Client)
            {
                if (Client.ForceBroadcast)
                {
                    if (_type == FMSendType.All) OnReceivedByteDataEvent.Invoke(_byteData);
                    //_type = FMSendType.Server; //when broadcast mode enabled, force the send type to server, then it won't send twice to others
                }
            }

            switch (_type)
            {
                case FMSendType.All:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_byteData, _type, _reliable);
                        OnReceivedByteDataEvent.Invoke(_byteData);
                    }
                    else
                    {
                        Client.Action_AddPacket(_byteData, _type, _reliable);
                    }
                    break;
                case FMSendType.Server:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        OnReceivedByteDataEvent.Invoke(_byteData);
                    }
                    else
                    {
                        Client.Action_AddPacket(_byteData, _type, _reliable);
                    }
                    break;
                case FMSendType.Others:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_byteData, _type, _reliable);
                    }
                    else
                    {
                        Client.Action_AddPacket(_byteData, _type, _reliable);
                    }
                    break;
                case FMSendType.TargetIP:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        if (_targetIP.Length > 4) Server.Action_AddPacket(_byteData, _targetIP, _reliable);
                    }
                    else
                    {
                        if (_targetIP.Length > 4) Client.Action_AddPacket(_byteData, _targetIP, _reliable);
                    }
                    break;
            }
        }

        private void Send(string _stringData, FMSendType _type, string _targetIP, bool _reliable = false)
        {
            if (!Initialised) return;
            if (NetworkType == FMNetworkType.Client && !Client.IsConnected) return;

            if (NetworkType == FMNetworkType.Client)
            {
                if (Client.ForceBroadcast)
                {
                    if (_type == FMSendType.All) OnReceivedStringDataEvent.Invoke(_stringData);
                    //_type = FMSendType.Server; //when broadcast mode enabled, force the send type to server, then it won't send twice to others
                }
            }

            switch (_type)
            {
                case FMSendType.All:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_stringData, _type, _reliable);
                        OnReceivedStringDataEvent.Invoke(_stringData);
                    }
                    else
                    {
                        Client.Action_AddPacket(_stringData, _type, _reliable);
                    }
                    break;
                case FMSendType.Server:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        OnReceivedStringDataEvent.Invoke(_stringData);
                    }
                    else
                    {
                        Client.Action_AddPacket(_stringData, _type, _reliable);
                    }
                    break;
                case FMSendType.Others:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        Server.Action_AddPacket(_stringData, _type, _reliable);
                    }
                    else
                    {
                        Client.Action_AddPacket(_stringData, _type, _reliable);
                    }
                    break;
                case FMSendType.TargetIP:
                    if (NetworkType == FMNetworkType.Server)
                    {
                        if (_targetIP.Length > 6) Server.Action_AddPacket(_stringData, _targetIP, _reliable);
                    }
                    else
                    {
                        if (_targetIP.Length > 6) Client.Action_AddPacket(_stringData, _targetIP, _reliable);
                    }
                    break;
            }
        }

        #endregion

        public void Action_ReloadScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

    }

}