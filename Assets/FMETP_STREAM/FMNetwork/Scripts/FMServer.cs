using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FMETP
{
    public class FMServer
    {
        public class FMServerComponent : MonoBehaviour
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            private int udpSendBufferSize = 1024 * 65; //max 65535
            private int udpReceiveBufferSize = 1024 * 1024 * 4; //max 2147483647
#else
        private int udpSendBufferSize = 1024 * 60; //max 65535
        private int udpReceiveBufferSize = 1024 * 512; //max 2147483647
#endif

            [HideInInspector] public FMNetworkManager Manager;

            [HideInInspector] public int ServerListenPort = 3333;
            [HideInInspector] public int ClientListenPort = 3334;

            public bool SupportMulticast = true;
            public string MulticastAddress = "239.255.255.255";
            private UdpClient mcastServer;
            private IPEndPoint mcastEndPoint;

            void SendMulticast(byte[] _byte)
            {
                try
                {
                    if (mcastServer == null)
                    {
                        mcastServer = new UdpClient(ClientListenPort);
                        mcastServer.Client.SendBufferSize = udpSendBufferSize;
                        mcastServer.Client.ReceiveBufferSize = udpReceiveBufferSize;
                        mcastServer.Client.SendTimeout = 500;

                        mcastServer.MulticastLoopback = true;
                        mcastServer.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));

                        mcastEndPoint = new IPEndPoint(IPAddress.Parse(MulticastAddress), ClientListenPort);
                    }
                    mcastServer.Send(_byte, _byte.Length, mcastEndPoint);
                }
                catch
                {
                    if (mcastServer != null)
                    {
                        mcastServer.DropMulticastGroup(IPAddress.Parse(MulticastAddress));
                        mcastServer.Close();
                        mcastServer = null;
                    }
                }
            }

            [Serializable]
            public class ConnectedClient
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                private int udpSendBufferSize = 1024 * 65; //max 65535
                private int udpReceiveBufferSize = 1024 * 1024 * 1024; //max 2147483647
#else
            private int udpSendBufferSize = 1024 * 60; //max 65535
            private int udpReceiveBufferSize = 1024 * 512; //max 2147483647
#endif
                public string IP;
                public int Port;

                public ConcurrentQueue<byte[]> AppendQueueAck = new ConcurrentQueue<byte[]>();
                public ConcurrentQueue<FMPacket> AppendQueueRetryPacket = new ConcurrentQueue<FMPacket>();
                public ConcurrentQueue<FMPacket> AppendQueueMissingPacket = new ConcurrentQueue<FMPacket>();

                private int _getSyncID = 0;
                private uint syncIDMax = UInt16.MaxValue - 1024;
                public UInt16 getSyncID
                {
                    get
                    {
                        _getSyncID = Interlocked.Increment(ref _getSyncID);
                        if (_getSyncID >= syncIDMax) _getSyncID = Interlocked.Exchange(ref _getSyncID, 1);
                        return (UInt16)_getSyncID;
                    }
                }

                private long _lastSeenTimeMS = 0;
                public int LastSeenTimeMS
                {
                    get { return (int)Interlocked.Read(ref _lastSeenTimeMS); }
                    set { Interlocked.Exchange(ref _lastSeenTimeMS, (long)value); }
                }
                private long _lastSentTimeMS = 0;
                public int LastSentTimeMS
                {
                    get { return Convert.ToInt32(Interlocked.Read(ref _lastSentTimeMS)); }
                    set { Interlocked.Exchange(ref _lastSentTimeMS, (long)value); }
                }

                public UdpClient Client;
                //public IPEndPoint ClientEp;

                public void SendHandShaking() { Send(new byte[] { 93 }); }
                public void Send(byte[] _byte)
                {
                    try
                    {
                        if (Client == null)
                        {
                            Client = new UdpClient();
                            Client.Client.SendBufferSize = udpSendBufferSize;
                            Client.Client.ReceiveBufferSize = udpReceiveBufferSize;
                            Client.Client.SendTimeout = 500;
                            Client.EnableBroadcast = true;
                        }
                        Client.Send(_byte, _byte.Length, new IPEndPoint(IPAddress.Parse(IP), Port));
                        LastSentTimeMS = Environment.TickCount;
                    }
                    catch { Close(); }
                }

                public int sendBufferSize = 0;
                public int sendBufferThreshold = 1024 * 128;
                public int sendBufferThresholdMin = 1024 * 8;
                public int sendBufferThresholdMax = 1024 * 128;

                public void Send(FMPacket _packet)
                {
                    sendBufferSize += _packet.SendByte.Length;
                    if (sendBufferSize > sendBufferThreshold)
                    {
                        sendBufferSize = 0;
                        GC.Collect();
                        System.Threading.Thread.Sleep(1);
                    }

                    Send(_packet.SendByte);
                    if (_packet.Reliable) AddRetryPacket(_packet);
                }

                public void AddRetryPacket(FMPacket _packet)
                {
                    _packet.syncID = getSyncID;
                    Buffer.BlockCopy(BitConverter.GetBytes(_packet.syncID), 0, _packet.SendByte, 2, 2);

                    _packet.SendType = FMSendType.TargetIP;
                    _packet.TargetIP = IP;
                    _packet.SendByte[1] = 3; //target ip

                    byte[] _ip = IPAddress.Parse(IP).GetAddressBytes();
                    Buffer.BlockCopy(_ip, 0, _packet.SendByte, 4, _ip.Length);

                    AppendQueueRetryPacket.Enqueue(_packet);
                }

                public void Close()
                {
                    AppendQueueAck = new ConcurrentQueue<byte[]>();
                    AppendQueueMissingPacket = new ConcurrentQueue<FMPacket>();
                    AppendQueueRetryPacket = new ConcurrentQueue<FMPacket>();
                    GC.Collect();
                }
            }

            public bool IsConnected = false;
            public int ConnectionCount = 0;
            public List<ConnectedClient> ConnectedClients = new List<ConnectedClient>();
            public List<string> ConnectedIPs = new List<string>();

            public void Action_CheckClientStatus(string _ip, FMClientSignal _signal = FMClientSignal.none, FMAckSignal _ack = FMAckSignal.none, byte[] _ackResponseByte = null, UInt16 _verifiedAckID = 0)
            {
                bool isExistedClient = false;
                int matchedIndex = 0;
                for (int i = 0; i < ConnectedClients.Count; i++)
                {
                    if (_ip == ConnectedClients[i].IP)
                    {
                        if (_signal == FMClientSignal.close)
                        {
                            //remove client immediately, when received CLOSE SIGNAL
                            ConnectedClients[i].Close();
                            _appendQueueDisconnectedClient.Enqueue(ConnectedClients[i].IP);

                            ConnectedClients.Remove(ConnectedClients[i]);
                            ConnectedIPs.Remove(ConnectedIPs[i]);
                        }
                        else
                        {
                            isExistedClient = true;
                            ConnectedClients[i].IP = _ip;
                            ConnectedClients[i].LastSeenTimeMS = Environment.TickCount;

                            matchedIndex = i;
                        }
                    }
                }

                if (isExistedClient)
                {
                    switch (_ack)
                    {
                        case FMAckSignal.none:
                            break;
                        case FMAckSignal.ackRespone:
                            ConnectedClients[matchedIndex].AppendQueueAck.Enqueue(_ackResponseByte);
                            break;
                        case FMAckSignal.ackReceived:
                            if (ConnectedClients[matchedIndex].AppendQueueRetryPacket.Count > 0)
                            {
                                bool _completed = false;
                                if (_verifiedAckID == 0) _completed = true;
                                while (!_completed)
                                {
                                    if (ConnectedClients[matchedIndex].AppendQueueRetryPacket.Count <= 0)
                                    {
                                        _completed = true;
                                    }
                                    else
                                    {
                                        if (ConnectedClients[matchedIndex].AppendQueueRetryPacket.TryDequeue(out FMPacket retryPacket))
                                        {
                                            UInt16 _syncID = BitConverter.ToUInt16(retryPacket.SendByte, 2);
                                            if (_syncID == _verifiedAckID)
                                            {
                                                _completed = true;
                                            }
                                            else
                                            {
                                                ConnectedClients[matchedIndex].AppendQueueMissingPacket.Enqueue(retryPacket);
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }

                switch (_signal)
                {
                    case FMClientSignal.handshake:
                        if (!isExistedClient)
                        {
                            //register new client
                            ConnectedClient NewClient = new ConnectedClient();
                            NewClient.IP = _ip;
                            NewClient.Port = ClientListenPort;
                            NewClient.LastSeenTimeMS = Environment.TickCount;

                            //for handshaking signal
                            NewClient.SendHandShaking();

                            ConnectedClients.Add(NewClient);
                            ConnectedIPs.Add(NewClient.IP);
                            _appendQueueConnectedClient.Enqueue(NewClient.IP);
                        }
                        break;
                    case FMClientSignal.close:
                        //closed in for-loop already
                        break;
                }
            }

            [HideInInspector]
            public int CurrentSeenTimeMS;
            private ConcurrentQueue<string> _appendQueueConnectedClient = new ConcurrentQueue<string>();
            private ConcurrentQueue<string> _appendQueueDisconnectedClient = new ConcurrentQueue<string>();

            public int CmdLength;

            private ConcurrentQueue<FMPacket> _appendQueueSendPacket = new ConcurrentQueue<FMPacket>();
            private ConcurrentQueue<FMPacket> _appendQueueReceivedPacket = new ConcurrentQueue<FMPacket>();

            public void Action_CloseClientConnection(string _targetIP)
            {
                FMPacket _packet = new FMPacket();
                _packet.Reliable = false;
                _packet.SendByte = new byte[] { 94 };
                _packet.SendType = FMSendType.TargetIP;
                _packet.TargetIP = _targetIP;
                _appendQueueSendPacket.Enqueue(_packet);
            }

            public void Action_AddPacket(byte[] _byteData, FMSendType _type, bool _reliable = false)
            {
                byte[] _meta = new byte[4];
                _meta[0] = 0;//raw byte

                if (_type == FMSendType.All) _meta[1] = 0;//all clients
                if (_type == FMSendType.Server) _meta[1] = 1;//all clients
                if (_type == FMSendType.Others) _meta[1] = 2;//skip sender

                byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);

                //if (_appendQueueSendPacket.Count < 120)
                {
                    FMPacket _packet = new FMPacket();
                    _packet.Reliable = _reliable;
                    _packet.SendByte = _sendByte;
                    _packet.SendType = _type;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }
            public void Action_AddPacket(string _stringData, FMSendType _type, bool _reliable = false)
            {
                byte[] _byteData = Encoding.ASCII.GetBytes(_stringData);

                byte[] _meta = new byte[4];
                _meta[0] = 1;//string data

                if (_type == FMSendType.All) _meta[1] = 0;//all clients
                if (_type == FMSendType.Server) _meta[1] = 1;//all clients
                if (_type == FMSendType.Others) _meta[1] = 2;//skip sender

                byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);

                //if (_appendQueueSendPacket.Count < 120)
                {
                    FMPacket _packet = new FMPacket();
                    _packet.Reliable = _reliable;
                    _packet.SendByte = _sendByte;
                    _packet.SendType = _type;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }

            public void Action_AddPacket(byte[] _byteData, string _targetIP, bool _reliable = false)
            {
                byte[] _meta = new byte[4];
                _meta[0] = 0;//raw byte
                _meta[1] = 3;//target ip

                byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);

                //if (_appendQueueSendPacket.Count < 120)
                {
                    FMPacket _packet = new FMPacket();
                    _packet.Reliable = _reliable;
                    _packet.SendByte = _sendByte;
                    _packet.SendType = FMSendType.TargetIP;
                    _packet.TargetIP = _targetIP;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }
            public void Action_AddPacket(string _stringData, string _targetIP, bool _reliable = false)
            {
                byte[] _byteData = Encoding.ASCII.GetBytes(_stringData);

                byte[] _meta = new byte[4];
                _meta[0] = 1;//string data
                _meta[1] = 3;//target ip

                byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);

                //if (_appendQueueSendPacket.Count < 120)
                {
                    FMPacket _packet = new FMPacket();
                    _packet.Reliable = _reliable;
                    _packet.SendByte = _sendByte;
                    _packet.SendType = FMSendType.TargetIP;
                    _packet.TargetIP = _targetIP;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }

            public void Action_AddPacket(FMPacket _packet)
            {
                //if (_appendQueueSendPacket.Count < 120)
                {
                    if (BitConverter.ToUInt16(_packet.SendByte, 2) != 0) _packet.Reliable = true;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }

            public void Action_AddNetworkObjectPacket(byte[] _byteData, FMSendType _type, bool _reliable = false)
            {
                byte[] _meta = new byte[4];
                _meta[0] = 2;//network object packet

                if (_type == FMSendType.All) _meta[1] = 0;//all clients
                if (_type == FMSendType.Server) _meta[1] = 1;//all clients
                if (_type == FMSendType.Others) _meta[1] = 2;//skip sender

                byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);

                if (_appendQueueSendPacket.Count < 120)
                {
                    FMPacket _packet = new FMPacket();
                    _packet.Reliable = _reliable;
                    _packet.SendByte = _sendByte;
                    _packet.SendType = _type;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }

            private long _stop = 0;
            private bool stop
            {
                get { return Interlocked.Read(ref _stop) == 1; }
                set { Interlocked.Exchange(ref _stop, Convert.ToInt64(value)); }
            }

            void Start() { StartAll(); }

            public void Action_StartServer()
            {
                StartCoroutine(NetworkServerStartCOR());
                StartCoroutine(BroadcastCheckerCOR());
            }

            [Header("[Experimental] for supported devices only")]
            public bool UseAsyncListener = false;
            [Header("[Experimental] suggested for mobile")]
            public bool UseMainThreadSender = false;

            private UdpClient Server;
            private IPEndPoint ClientEp;

            private void InitializeServerListener()
            {
                Server = new UdpClient(ServerListenPort);
                Server.Client.SendBufferSize = udpSendBufferSize;
                Server.Client.ReceiveBufferSize = udpReceiveBufferSize;
                Server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                Server.Client.EnableBroadcast = true;
                //Server.Client.ReceiveTimeout = 2000;

                ClientEp = new IPEndPoint(IPAddress.Any, ServerListenPort);
            }

            private UdpClient BroadcastClient = new UdpClient();
            private void BroadcastChecker()
            {
                if (BroadcastClient == null)
                {
                    BroadcastClient = new UdpClient();
                    BroadcastClient.Client.SendTimeout = 200;
                    BroadcastClient.EnableBroadcast = true;
                }

                try { BroadcastClient.Send(new byte[] { 93 }, 1, new IPEndPoint(IPAddress.Broadcast, ClientListenPort)); }
                catch { if (BroadcastClient != null) BroadcastClient.Close(); BroadcastClient = null; }
            }

            IEnumerator BroadcastCheckerCOR()
            {
                int currentTimeMS = Environment.TickCount;
                int nextCheckTimeMS = currentTimeMS + 5000;
                while (!stop)
                {
                    yield return null;

                    currentTimeMS = Environment.TickCount;
                    if (currentTimeMS > nextCheckTimeMS)
                    {
                        BroadcastChecker();
                        nextCheckTimeMS = currentTimeMS + 5000;
                    }
                }
            }

            IEnumerator NetworkServerStartCOR()
            {
                stop = false;
                yield return new WaitForSeconds(0.5f);

                if (!UseAsyncListener)
                {
                    //vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv Server Receiver vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
                    while (Loom.numThreads >= Loom.maxThreads) yield return null;
                    Loom.RunAsync(() =>
                    {
                        while (!stop)
                        {
                            try
                            {
                                if (Server == null) InitializeServerListener();

                                while (!stop && Server.Client.Poll(100, SelectMode.SelectRead))
                                {
                                    while (!stop && Server.Client.Available > 0)
                                    {
                                    //=======================Queue Received Data=======================
                                    byte[] ReceivedData = Server.Receive(ref ClientEp);
                                        int ReceivedDataLength = ReceivedData.Length;
                                        string ClientIP = ClientEp.Address.ToString();
                                        if (ReceivedData.Length > 4)
                                        {
                                            FMPacket _packet = new FMPacket();
                                            _packet.SendByte = ReceivedData;

                                        //others, skip sender
                                        if (ReceivedData[1] == 2) _packet.SkipIP = ClientIP;
                                            _appendQueueReceivedPacket.Enqueue(_packet);
                                        }
                                    //=======================Queue Received Data=======================

                                    //=======================Check is new client?=======================
                                    FMClientSignal _signal = FMClientSignal.none;
                                        FMAckSignal _ack = FMAckSignal.none;
                                        byte[] ackResponseByte = null;
                                        if (ReceivedDataLength == 1)
                                        {
                                        //Received Auto Network Discovery signal from Server
                                        if (ReceivedData[0] == 93) _signal = FMClientSignal.handshake;
                                            if (ReceivedData[0] == 94) _signal = FMClientSignal.close;
                                        }

                                        UInt16 _verifiedAckID = 0;
                                        if (ReceivedDataLength > 4)
                                        {
                                        //ack send queue
                                        if (ReceivedData[2] != 0 && ReceivedData[3] != 0)
                                            {
                                            //_appendQueueAck.Enqueue(new byte[] { ReceivedData[2], ReceivedData[3] });
                                            _ack = FMAckSignal.ackRespone;
                                                ackResponseByte = new byte[] { ReceivedData[2], ReceivedData[3] };
                                            }
                                        }
                                        else if (ReceivedDataLength <= 2)
                                        {
                                            _ack = FMAckSignal.ackReceived;
                                            if (ReceivedDataLength == 2) _verifiedAckID = BitConverter.ToUInt16(ReceivedData, 0);
                                        }

                                        Action_CheckClientStatus(ClientIP, _signal, _ack, ackResponseByte, _verifiedAckID);
                                    //=======================Check is new client?=======================
                                }
                                }
                            }
                            catch
                            {
                            //DebugLog("Server Socket exception: " + socketException);
                            if (Server != null) Server.Close(); Server = null;
                            }
                        //System.Threading.Thread.Sleep(1);
                    }
                    });
                    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Server Receiver ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                }
                else
                {
                    //Use Async Receiver solution
                    //Experimental feature: try to reduce thread usage
                    //Didn't work well on low-end devices during our testing
                    if (Server == null) InitializeServerListener();

                    if (!stop)
                    {
                        try
                        {
                            Server.BeginReceive(new AsyncCallback(UdpReceiveCallback), null);
                        }
                        catch
                        {
                            //DebugLog("Socket exception: " + socketException);
                            if (Server != null) Server.Close(); Server = null;
                            InitializeServerListener();
                        }
                    }
                }

                if (!UseMainThreadSender)
                {
                    //vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv Server Sender vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
                    while (Loom.numThreads >= Loom.maxThreads) yield return null;
                    Loom.RunAsync(() =>
                    {
                        while (!stop)
                        {
                            Sender();
                            System.Threading.Thread.Sleep(1);
                        }
                        System.Threading.Thread.Sleep(1);
                    });
                    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Server Sender ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                }
                else
                {
                    StartCoroutine(MainThreadSenderCOR());
                }

                //processing
                while (!stop)
                {
                    CurrentSeenTimeMS = Environment.TickCount;

                    #region Check Connection Status
                    IsConnected = (ConnectionCount > 0) ? true : false;
                    CmdLength = _appendQueueSendPacket.Count;

                    while (_appendQueueConnectedClient.Count > 0)
                    {
                        if (_appendQueueConnectedClient.TryDequeue(out string connectedClient)) Manager.OnClientConnected(connectedClient);
                    }
                    while (_appendQueueDisconnectedClient.Count > 0)
                    {
                        if (_appendQueueDisconnectedClient.TryDequeue(out string disconnectedClient)) Manager.OnClientDisconnected(disconnectedClient);
                    }
                    #endregion

                    while (_appendQueueReceivedPacket.Count > 0)
                    {
                        if (_appendQueueReceivedPacket.TryDequeue(out FMPacket _packet))
                        {
                            if (Manager != null)
                            {
                                byte[] ReceivedData = _packet.SendByte;
                                if (ReceivedData.Length > 4)
                                {
                                    byte[] _meta = new byte[] { ReceivedData[0], ReceivedData[1] };
                                    if (_meta[1] == 3)
                                    {
                                        //Send to TargetIP, contains 4 bytes ip after meta data
                                        _packet.TargetIP = new IPAddress(new byte[] { ReceivedData[4], ReceivedData[5], ReceivedData[6], ReceivedData[7] }).ToString();
                                        byte[] _data = new byte[ReceivedData.Length - 6];
                                        Buffer.BlockCopy(ReceivedData, 8, _data, 0, _data.Length);

                                        if (_packet.TargetIP == Manager.ReadLocalIPAddress)
                                        {
                                            //process received data>> byte data: 0, string msg: 1
                                            switch (_meta[0])
                                            {
                                                case 0: Manager.OnReceivedByteDataEvent.Invoke(_data); break;
                                                case 1: Manager.OnReceivedStringDataEvent.Invoke(Encoding.ASCII.GetString(_data)); break;
                                            }
                                        }
                                        else
                                        {
                                            //redirect the data to target IP
                                            if (_packet.TargetIP != Manager.ReadLocalIPAddress)
                                            {
                                                _packet.SendType = FMSendType.TargetIP;
                                                _packet.SendByte = new byte[_meta.Length + _data.Length];
                                                _packet.SendByte[0] = _meta[0]; _packet.SendByte[1] = _meta[1];
                                                Buffer.BlockCopy(_data, 0, _packet.SendByte, 4, _data.Length);
                                                Action_AddPacket(_packet);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        byte[] _data = new byte[ReceivedData.Length - 4];
                                        Buffer.BlockCopy(ReceivedData, 4, _data, 0, _data.Length);

                                        //process received data>> byte data: 0, string msg: 1
                                        switch (_meta[0])
                                        {
                                            case 0: Manager.OnReceivedByteDataEvent.Invoke(_data); break;
                                            case 1: Manager.OnReceivedStringDataEvent.Invoke(Encoding.ASCII.GetString(_data)); break;
                                        }

                                        //check send type
                                        switch (_meta[1])
                                        {
                                            //send to all, redirect msg to all other clients
                                            case 0: Action_AddPacket(_packet); break;
                                            //send to server only, do not need to do anything
                                            case 1: break;
                                            //skip sender
                                            case 2: Action_AddPacket(_packet); break;
                                        }
                                    }
                                }

                                Manager.GetRawReceivedData.Invoke(ReceivedData);
                            }
                        }
                    }
                    yield return null;
                }
                yield break;
            }

            void UdpReceiveCallback(IAsyncResult ar)
            {
                if (ar.IsCompleted)
                {
                    //receive callback completed
                    //=======================Queue Received Data=======================
                    byte[] ReceivedData = Server.EndReceive(ar, ref ClientEp);
                    string ClientIP = ClientEp.Address.ToString();
                    if (ReceivedData.Length > 4)
                    {
                        FMPacket _packet = new FMPacket();
                        _packet.SendByte = ReceivedData;

                        //others, skip sender
                        if (ReceivedData[1] == 2) _packet.SkipIP = ClientIP;
                        _appendQueueReceivedPacket.Enqueue(_packet);
                    }
                    //=======================Queue Received Data=======================

                    //=======================Check is new client?=======================
                    FMClientSignal _signal = FMClientSignal.none;
                    FMAckSignal _ack = FMAckSignal.none;
                    byte[] ackResponseByte = null;
                    if (ReceivedData.Length == 1)
                    {
                        //Received Auto Network Discovery signal from Server
                        if (ReceivedData[0] == 93) _signal = FMClientSignal.handshake;
                        if (ReceivedData[0] == 94) _signal = FMClientSignal.close;
                    }

                    UInt16 _verifiedAckID = 0;
                    if (ReceivedData.Length > 4)
                    {
                        //ack send queue
                        if (ReceivedData[2] != 0 && ReceivedData[3] != 0)
                        {
                            //_appendQueueAck.Enqueue(new byte[] { ReceivedData[2], ReceivedData[3] });
                            _ack = FMAckSignal.ackRespone;
                            ackResponseByte = new byte[] { ReceivedData[2], ReceivedData[3] };
                        }
                    }
                    else if (ReceivedData.Length <= 2)
                    {
                        _ack = FMAckSignal.ackReceived;
                        if (ReceivedData.Length == 2) _verifiedAckID = BitConverter.ToUInt16(ReceivedData, 0);
                    }

                    Action_CheckClientStatus(ClientIP, _signal, _ack, ackResponseByte, _verifiedAckID);
                    //=======================Check is new client?=======================
                }

                if (!stop)
                {
                    try
                    {
                        Server.BeginReceive(new AsyncCallback(UdpReceiveCallback), null);
                    }
                    catch
                    {
                        //DebugLog("sth wrong with server receive async: " + socketException.ToString());
                        if (Server != null) Server.Close(); Server = null;
                        InitializeServerListener();
                    }
                }
            }

            IEnumerator MainThreadSenderCOR()
            {
                while (!stop)
                {
                    yield return null;
                    Sender();
                }
            }

            void Sender()
            {
                ConnectionCount = ConnectedClients.Count;
                for (int i = ConnectionCount - 1; i >= 0; i--)
                {
                    bool _active = false;
                    if (CurrentSeenTimeMS < 0 && ConnectedClients[i].LastSeenTimeMS > 0)
                    {
                        _active = (Mathf.Abs(CurrentSeenTimeMS - int.MinValue) + (int.MaxValue - ConnectedClients[i].LastSeenTimeMS) < 3000) ? true : false;
                    }
                    else
                    {
                        _active = ((CurrentSeenTimeMS - ConnectedClients[i].LastSeenTimeMS) < 3000) ? true : false;
                    }
                    if (_active == false)
                    {
                        //remove it if didn't receive any data from client for 3000 ms
                        ConnectedClients[i].Close();
                        _appendQueueDisconnectedClient.Enqueue(ConnectedClients[i].IP);

                        ConnectedClients.Remove(ConnectedClients[i]);
                        ConnectedIPs.Remove(ConnectedIPs[i]);
                    }
                }
                ConnectionCount = ConnectedClients.Count;

                if (ConnectionCount > 0)
                {
                    for (int i = ConnectionCount - 1; i >= 0; i--)
                    {
                        if (ConnectedClients[i].AppendQueueAck.Count > 0 || ConnectedClients[i].AppendQueueMissingPacket.Count > 0)
                        {
                            //send queuedAck
                            int ackCount = 0;
                            while (ConnectedClients[i].AppendQueueAck.Count > 0 && ackCount < 100)
                            {
                                ackCount++;
                                if (ConnectedClients[i].AppendQueueAck.TryDequeue(out byte[] _ackBytes))
                                {
                                    ConnectedClients[i].Send(_ackBytes);
                                }
                            }

                            //check missing count
                            int missingCount = 0;
                            while (ConnectedClients[i].AppendQueueMissingPacket.Count > 0 && missingCount < 100)
                            {
                                missingCount++;
                                if (ConnectedClients[i].AppendQueueMissingPacket.TryDequeue(out FMPacket _missingPacket))
                                {
                                    _missingPacket.Reliable = true;
                                    SendPacket(_missingPacket);
                                }
                            }
                            ConnectedClients[i].sendBufferThreshold = missingCount > 0 ? ConnectedClients[i].sendBufferThresholdMin : ConnectedClients[i].sendBufferThresholdMax;
                        }
                    }
                }

                if (_appendQueueSendPacket.Count > 0)
                {
                    //limit 30 packet sent in each frame, solved overhead issue on receiver
                    int k = 0;
                    //there are some commands in queue
                    //Debug.Log(_appendQueueSendPacket.Count);
                    while (_appendQueueSendPacket.Count > 0 && k < 100)
                    {
                        k++;
                        if (_appendQueueSendPacket.TryDequeue(out FMPacket _packet))
                        {
                            SendPacket(_packet);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < ConnectedClients.Count; i++)
                    {
                        //send empty byte for checking, after 1000 ms
                        if (CurrentSeenTimeMS - ConnectedClients[i].LastSentTimeMS > 1000) ConnectedClients[i].SendHandShaking();
                    }
                }
            }

            void SendPacket(FMPacket _packet)
            {
                if (_packet.SendType != FMSendType.TargetIP)
                {
                    if (SupportMulticast && !_packet.Reliable && _packet.SkipIP == null)
                    {
                        // if it's non-relaible packet, use multicast when supported
                        SendMulticast(_packet.SendByte);
                    }
                    else
                    {
                        // else, use default unicast per client
                        for (int i = 0; i < ConnectedClients.Count; i++)
                        {
                            if (ConnectedClients[i].IP != _packet.SkipIP) ConnectedClients[i].Send(_packet);
                        }
                    }
                }
                else
                {
                    //sending to target ip only
                    for (int i = 0; i < ConnectedClients.Count; i++)
                    {
                        if (ConnectedClients[i].IP == _packet.TargetIP)
                        {
                            ConnectedClients[i].Send(_packet);
                        }
                        else
                        {
                            if (CurrentSeenTimeMS - ConnectedClients[i].LastSentTimeMS > 1000) ConnectedClients[i].SendHandShaking();
                        }
                    }
                }
            }

            public bool ShowLog = true;
            public void DebugLog(string _value) { if (ShowLog) Debug.Log(_value); }

            private void OnApplicationQuit() { StopAll(); }
            private void OnDisable() { StopAll(); }
            private void OnDestroy() { StopAll(); }
            private void OnEnable()
            {
                if (Time.timeSinceLevelLoad <= 3f) return;
                if (stop) StartAll();
            }

#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID || WINDOWS_UWP)
        private void OnApplicationPause(bool pause) { StopAll(); }
        private void OnApplicationFocus(bool focus)
        {
            if (Time.timeSinceLevelLoad <= 3f) return;
            if (stop) StartAll();
        }
#endif

            void StartAll()
            {
                stop = false;
                Action_StartServer();
            }
            void StopAll()
            {
                //skip, if stopped already
                if (stop)
                {
                    StopAllCoroutines();//stop all coroutines, just in case
                    return;
                }

                if (mcastServer != null)
                {
                    try
                    {
                        mcastServer.DropMulticastGroup(IPAddress.Parse(MulticastAddress));
                        mcastServer.Close();
                    }
                    catch (Exception e) { DebugLog(e.Message); }
                    mcastServer = null;
                }

                if (IsConnected && Server != null)
                {
                    if (ConnectedClients.Count > 0)
                    {
                        foreach (ConnectedClient cc in ConnectedClients)
                        {
                            //send status "closed" in background before end...
                            SendServerClosedAsync(cc.IP, cc.Port);

                            cc.Close();
                            Manager.OnClientDisconnected(cc.IP);
                        }
                    }

                    try { Server.Close(); }
                    catch (Exception e) { DebugLog(e.Message); }
                    Server = null;
                }

                stop = true;
                IsConnected = false;
                StopAllCoroutines();

                if (ConnectedClients != null) ConnectedClients.Clear();
                ConnectedClients = new List<ConnectedClient>();
                _appendQueueSendPacket = new ConcurrentQueue<FMPacket>();
                _appendQueueReceivedPacket = new ConcurrentQueue<FMPacket>();
            }

            private async void SendServerClosedAsync(string IP, int Port)
            {
                await Task.Yield();
                UdpClient Client = new UdpClient();
                try
                {
                    Client.Client.SendBufferSize = udpSendBufferSize;
                    Client.Client.ReceiveBufferSize = udpReceiveBufferSize;
                    Client.Client.SendTimeout = 500;
                    Client.EnableBroadcast = true;

                    byte[] _byte = new byte[] { 95 };
                    Client.Send(_byte, _byte.Length, new IPEndPoint(IPAddress.Parse(IP), Port));
                }
                catch { }
            }
        }
    }
}