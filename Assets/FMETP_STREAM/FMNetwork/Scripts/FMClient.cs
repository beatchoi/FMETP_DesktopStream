using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FMETP
{
    public class FMClient
    {
        public class FMClientComponent : MonoBehaviour
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

            public bool SupportMulticast = false;
            public string MulticastAddress = "239.255.255.255";

            //[HideInInspector]
            public string ServerIP = "0,0,0,0";
            [HideInInspector]
            public string ClientIP = "0,0,0,0";

            public bool IsConnected = false;
            //public bool FoundServer = false;
            private long _foundServer = 0;
            private bool FoundServer
            {
                get { return Interlocked.Read(ref _foundServer) == 1; }
                set { Interlocked.Exchange(ref _foundServer, Convert.ToInt64(value)); }
            }

            public bool AutoNetworkDiscovery = true;
            public bool ForceBroadcast = false;

            [HideInInspector]
            public int CurrentSeenTimeMS;
            private long _lastReceivedTimeMS = 0;
            public int LastReceivedTimeMS
            {
                get { return Convert.ToInt32(Interlocked.Read(ref _lastReceivedTimeMS)); }
                set { Interlocked.Exchange(ref _lastReceivedTimeMS, (long)value); }
            }
            private long _lastSentTimeMS = 0;
            public int LastSentTimeMS
            {
                get { return (int)Interlocked.Read(ref _lastSentTimeMS); }
                set { Interlocked.Exchange(ref _lastSentTimeMS, (long)value); }
            }


            [Header("[Experimental] suggested for mobile")]
            public bool UseMainThreadSender = false;
            private ConcurrentQueue<FMPacket> _appendQueueSendPacket = new ConcurrentQueue<FMPacket>();
            private ConcurrentQueue<FMPacket> _appendQueueReceivedPacket = new ConcurrentQueue<FMPacket>();
            private ConcurrentQueue<byte[]> _appendQueueAck = new ConcurrentQueue<byte[]>();
            private ConcurrentQueue<FMPacket> _appendQueueRetryPacket = new ConcurrentQueue<FMPacket>();
            private ConcurrentQueue<FMPacket> _appendQueueMissingPacket = new ConcurrentQueue<FMPacket>();

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

            private void EnqueueReceivedPacket(byte[] _receivedData)
            {
                FMPacket _packet = new FMPacket();
                _packet.SendByte = _receivedData;
                _appendQueueReceivedPacket.Enqueue(_packet);
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

                //if (_appendQueueSendPacket.Count < 60)
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
                _meta[0] = 1;//raw byte

                if (_type == FMSendType.All) _meta[1] = 0;//all clients
                if (_type == FMSendType.Server) _meta[1] = 1;//all clients
                if (_type == FMSendType.Others) _meta[1] = 2;//skip sender

                byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);

                //if (_appendQueueSendPacket.Count < 60)
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
                //if (ServerIP == _targetIP)
                //{
                //    Action_AddPacket(_byteData, FMSendType.Server);
                //    return;
                //}

                //Send To Target IP
                byte[] _meta = new byte[4];
                _meta[0] = 0;//raw byte
                _meta[1] = 3;//target ip

                byte[] _ip = IPAddress.Parse(_targetIP).GetAddressBytes();
                byte[] _sendByte = new byte[_byteData.Length + _meta.Length + _ip.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_ip, 0, _sendByte, 4, _ip.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 8, _byteData.Length);

                //if (_appendQueueSendPacket.Count < 60)
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
                if (ServerIP == _targetIP)
                {
                    Action_AddPacket(_stringData, FMSendType.Server);
                    return;
                }
                byte[] _byteData = Encoding.ASCII.GetBytes(_stringData);

                byte[] _meta = new byte[4];
                _meta[0] = 1;//raw byte
                _meta[1] = 3;//target ip

                byte[] _ip = IPAddress.Parse(_targetIP).GetAddressBytes();
                byte[] _sendByte = new byte[_byteData.Length + _meta.Length + _ip.Length];
                Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                Buffer.BlockCopy(_ip, 0, _sendByte, 4, _ip.Length);
                Buffer.BlockCopy(_byteData, 0, _sendByte, 6, _byteData.Length);

                //if (_appendQueueSendPacket.Count < 60)
                {
                    FMPacket _packet = new FMPacket();
                    _packet.Reliable = _reliable;
                    _packet.SendByte = _sendByte;
                    _packet.SendType = FMSendType.TargetIP;
                    _packet.TargetIP = _targetIP;
                    _appendQueueSendPacket.Enqueue(_packet);
                }
            }

            private long _stop = 0;
            private bool stop
            {
                get { return Interlocked.Read(ref _stop) == 1; }
                set { Interlocked.Exchange(ref _stop, Convert.ToInt64(value)); }
            }
            private long _destroy = 0;
            private bool destroy
            {
                get { return Interlocked.Read(ref _destroy) == 1; }
                set { Interlocked.Exchange(ref _destroy, Convert.ToInt64(value)); }
            }

            void Start() { StartAll(); }
            public void Action_StartClient() { StartCoroutine(NetworkClientStartCOR()); }

            private UdpClient Client;
            private UdpClient ClientListener;
            private IPEndPoint ServerEp;
            IEnumerator NetworkClientStartCOR()
            {
                LastSentTimeMS = Environment.TickCount;
                LastReceivedTimeMS = Environment.TickCount;

                stop = false;
                yield return new WaitForSeconds(0.5f);

                if (UseMainThreadSender)
                {
                    StartCoroutine(MainThreadSenderCOR());
                }
                else
                {
                    //vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv Client Sender vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
                    while (Loom.numThreads >= Loom.maxThreads) yield return null;
                    Loom.RunAsync(() =>
                    {
                    //client request
                    while (!stop)
                        {
                            Sender();
                            System.Threading.Thread.Sleep(FoundServer ? 1 : 200);
                        }
                        System.Threading.Thread.Sleep(1);
                    });
                    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Client Sender ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                }

                //vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv Client Receiver vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
                while (Loom.numThreads >= Loom.maxThreads) yield return null;
                Loom.RunAsync(() =>
                {
                    while (!stop)
                    {
                        try
                        {
                            if (ClientListener == null)
                            {
                                ClientListener = new UdpClient(ClientListenPort);
                                ClientListener.Client.SendBufferSize = udpSendBufferSize;
                                ClientListener.Client.ReceiveBufferSize = udpReceiveBufferSize;
                                ClientListener.Client.ReceiveTimeout = 2000;
                            //ClientListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                            if (SupportMulticast)
                                {
                                //enable multicast option
                                ClientListener.MulticastLoopback = true;
                                    ClientListener.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));
                                }

                                ServerEp = new IPEndPoint(IPAddress.Any, ClientListenPort);
                            }

                            while (!stop && ClientListener.Client.Poll(100, SelectMode.SelectRead))
                            {
                                while (!stop && ClientListener.Client.Available > 0)
                                {
                                    byte[] ReceivedData = ClientListener.Receive(ref ServerEp);
                                    int ReceivedDataLength = ReceivedData.Length;
                                    LastReceivedTimeMS = Environment.TickCount;

                                //=======================Decode Data=======================
                                if (!FoundServer)
                                    {
                                    //looking for server and handshake
                                    if (AutoNetworkDiscovery)
                                        {
                                            if (ReceivedDataLength == 1)
                                            {
                                            //Received Auto Network Discovery signal from Server
                                            if (ReceivedData[0] == 93)
                                                {
                                                    ServerIP = ServerEp.Address.ToString();
                                                    FoundServer = true;

                                                //handshaking signal
                                                SendHandShaking(new IPEndPoint(IPAddress.Parse(ServerIP), ServerListenPort));
                                                    EnqueueReceivedPacket(ReceivedData);
                                                }
                                            }
                                        }
                                        else
                                        {
                                        //Any response from server will be consider as handshake
                                        if (ServerIP == ServerEp.Address.ToString())
                                            {
                                                FoundServer = true;

                                            //handshaking signal
                                            SendHandShaking(new IPEndPoint(IPAddress.Parse(ServerIP), ServerListenPort));
                                                EnqueueReceivedPacket(ReceivedData);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (ReceivedDataLength == 1)
                                        {
                                        //Received Close() command from Server
                                        if (ReceivedData[0] == 94)
                                            {
                                                destroy = true;
                                                stop = true;
                                            }
                                            else if (ReceivedData[0] == 95)
                                            {
                                            //Down server...
                                            FoundServer = false;
                                            }
                                        }
                                    }

                                    UInt16 _verifiedAckID = 0;
                                    if (ReceivedDataLength > 4)
                                    {
                                        EnqueueReceivedPacket(ReceivedData);

                                    //ack send queue
                                    if (ReceivedData[2] != 0 && ReceivedData[3] != 0)
                                        {
                                            _appendQueueAck.Enqueue(new byte[] { ReceivedData[2], ReceivedData[3] });
                                        }
                                    }
                                    else if (ReceivedDataLength <= 2)
                                    {
                                    //ack received
                                    if (_appendQueueRetryPacket.Count > 0)
                                        {
                                            if (ReceivedData.Length == 2) _verifiedAckID = BitConverter.ToUInt16(ReceivedData, 0);

                                            bool _completed = false;
                                            if (_verifiedAckID == 0)
                                            {
                                            //Debug.LogError("confirmed AckID");
                                            _completed = true;
                                            }
                                            while (!_completed)
                                            {
                                                if (_appendQueueRetryPacket.Count <= 0)
                                                {
                                                //complete when there is no retry packet to check
                                                _completed = true;
                                                }
                                                else
                                                {
                                                    if (_appendQueueRetryPacket.TryDequeue(out FMPacket retryPacket))
                                                    {
                                                        if (retryPacket.syncID == _verifiedAckID)
                                                        {
                                                        //found matching packet, confirmed
                                                        _completed = true;
                                                        }
                                                        else
                                                        {
                                                            _appendQueueMissingPacket.Enqueue(retryPacket);
                                                        }
                                                    }
                                                }

                                            }
                                        }
                                    }
                                //=======================Decode Data=======================
                            }
                            }
                        }
                        catch
                        {
                        //DebugLog("Client Receiver Timeout: " + socketException.ToString());
                        if (ClientListener != null) ClientListener.Close(); ClientListener = null;
                        }
                    //System.Threading.Thread.Sleep(1);
                }
                    System.Threading.Thread.Sleep(1);
                });
                //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Client Receiver ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

                //processing
                while (!stop)
                {
                    CurrentSeenTimeMS = Environment.TickCount;

                    #region Check Connection Status
                    bool connected = false;
                    if (FoundServer)
                    {
                        int connectionThreshold = 10000;
                        if (CurrentSeenTimeMS < 0 && LastReceivedTimeMS > 0)
                        {
                            connected = (Mathf.Abs(CurrentSeenTimeMS - int.MinValue) + (int.MaxValue - LastReceivedTimeMS) < connectionThreshold) ? true : false;
                        }
                        else
                        {
                            connected = ((CurrentSeenTimeMS - LastReceivedTimeMS) < connectionThreshold) ? true : false;
                        }
                    }

                    if (IsConnected != connected)
                    {
                        if (connected)
                        {
                            Manager.OnFoundServer(ServerIP);
                        }
                        else
                        {
                            Manager.OnLostServer(ServerIP);

                            _appendQueueSendPacket = new ConcurrentQueue<FMPacket>();
                            _appendQueueReceivedPacket = new ConcurrentQueue<FMPacket>();

                            _appendQueueAck = new ConcurrentQueue<byte[]>();
                            _appendQueueRetryPacket = new ConcurrentQueue<FMPacket>();
                            _appendQueueMissingPacket = new ConcurrentQueue<FMPacket>();
                        }
                        IsConnected = connected;
                    }
                    #endregion

                    while (_appendQueueReceivedPacket.Count > 0)
                    {
                        ReceivedCount = _appendQueueReceivedPacket.Count;
                        if (_appendQueueReceivedPacket.TryDequeue(out FMPacket _packet))
                        {
                            if (Manager != null)
                            {
                                byte[] ReceivedData = _packet.SendByte;
                                if (ReceivedData.Length > 4)
                                {
                                    byte[] _meta = new byte[] { ReceivedData[0], ReceivedData[1] };
                                    byte[] _data = new byte[ReceivedData.Length - 4];
                                    Buffer.BlockCopy(ReceivedData, 4, _data, 0, _data.Length);

                                    //process received data>> byte data: 0, string msg: 1, network object data: 2
                                    switch (_meta[0])
                                    {
                                        case 0: Manager.OnReceivedByteDataEvent.Invoke(_data); break;
                                        case 1: Manager.OnReceivedStringDataEvent.Invoke(Encoding.ASCII.GetString(_data)); break;
                                        case 2: Manager.Action_SyncNetworkObjectTransform(_data); break;
                                    }
                                }
                                Manager.GetRawReceivedData.Invoke(ReceivedData);
                            }
                        }
                    }
                    yield return null;
                }

                while (!destroy) yield return null;
                if (destroy)
                {
                    StopAll();
                    yield return null;
                    Manager.Action_Close();
                }

                yield break;
            }
            public int ReceivedCount = 0;

            IEnumerator MainThreadSenderCOR()
            {
                //client request
                while (!stop)
                {
                    yield return null;
                    Sender();
                }
            }

            void SendHandShaking(IPEndPoint ipEndPoint)
            {
                if (Client == null) return;
                try { Client.Send(new byte[] { 93 }, 1, ipEndPoint); }
                catch { if (Client != null) Client.Close(); Client = null; }
            }
            void SendClosed()
            {
                if (Client == null) return;
                try { Client.Send(new byte[] { 94 }, 1, new IPEndPoint(IPAddress.Parse(ServerIP), ServerListenPort)); }
                catch { if (Client != null) Client.Close(); Client = null; }
            }

            void Sender()
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

                    byte[] RequestData = new byte[1];
                    if (FoundServer == false && AutoNetworkDiscovery)
                    {
                        if (CurrentSeenTimeMS - LastSentTimeMS > 2000)
                        {
                            //broadcast
                            SendHandShaking(new IPEndPoint(IPAddress.Broadcast, ServerListenPort));
                            LastSentTimeMS = Environment.TickCount;
                        }
                    }
                    else
                    {
                        //send to server ip only
                        if (_appendQueueSendPacket.Count > 0 || _appendQueueAck.Count > 0 || _appendQueueMissingPacket.Count > 0)
                        {
                            bool sent = false;

                            //send queuedAck
                            int ackCount = 0;
                            while (_appendQueueAck.Count > 0 && ackCount < 100)
                            {
                                ackCount++;
                                if (_appendQueueAck.TryDequeue(out byte[] _ackBytes))
                                {
                                    if (SendPacket(_ackBytes)) sent = true;
                                }
                            }

                            //limit 30 packet sent in each frame, solved overhead issue on receiver
                            int sendCount = 0;
                            while (_appendQueueSendPacket.Count > 0 && sendCount < 100)
                            {
                                sendCount++;
                                if (_appendQueueSendPacket.TryDequeue(out FMPacket _packet))
                                {
                                    if (SendPacket(_packet)) sent = true;
                                }
                            }

                            int missingCount = 0;
                            while (_appendQueueMissingPacket.Count > 0 && missingCount < 100)
                            {
                                missingCount++;
                                if (_appendQueueMissingPacket.TryDequeue(out FMPacket _missingPacket))
                                {
                                    _missingPacket.Reliable = true;
                                    SendPacket(_missingPacket);
                                }
                            }
                            sendBufferThreshold = missingCount > 0 ? sendBufferThresholdMin : sendBufferThresholdMax;

                            if (sent) LastSentTimeMS = Environment.TickCount;
                        }
                        else
                        {
                            if (CurrentSeenTimeMS - LastSentTimeMS > 2000)
                            {
                                //check connection: minimum 2000ms
                                SendHandShaking(new IPEndPoint(ForceBroadcast ? IPAddress.Broadcast : IPAddress.Parse(ServerIP), ServerListenPort));
                                LastSentTimeMS = Environment.TickCount;
                            }
                        }
                    }
                }
                catch
                {
                    //DebugLog("client sender timeout: " + socketException.ToString());
                    if (Client != null) Client.Close(); Client = null;
                }
            }

            int sendBufferSize = 0;
            int sendBufferThreshold = 1024 * 128;
            int sendBufferThresholdMin = 1024 * 8;
            int sendBufferThresholdMax = 1024 * 128;

            bool SendPacket(byte[] _bytes)
            {
                bool sent = false;
                try
                {
                    Client.Send(_bytes, _bytes.Length, new IPEndPoint(ForceBroadcast ? IPAddress.Broadcast : IPAddress.Parse(ServerIP), ServerListenPort));
                    sent = true;
                }
                catch
                {
                    if (Client != null) Client.Close(); Client = null;
                }
                return sent;
            }
            bool SendPacket(FMPacket _packet)
            {
                bool sent = false;
                sendBufferSize += _packet.SendByte.Length;
                if (sendBufferSize > sendBufferThreshold)
                {
                    sendBufferSize = 0;
                    GC.Collect();
                    System.Threading.Thread.Sleep(1);
                }

                if (_packet.Reliable)
                {
                    _packet.syncID = getSyncID;
                    Buffer.BlockCopy(BitConverter.GetBytes(_packet.syncID), 0, _packet.SendByte, 2, 2);
                }

                if (!ForceBroadcast)
                {
                    //default mode, non-broadcasting
                    Client.Send(_packet.SendByte, _packet.SendByte.Length, new IPEndPoint(IPAddress.Parse(ServerIP), ServerListenPort));
                    sent = true;
                }
                else
                {
                    //broadcasting mode for multiple servers..etc
                    if (_packet.SendType == FMSendType.TargetIP)
                    {
                        //ignore broadcast, if you have a target IP
                        Client.Send(_packet.SendByte, _packet.SendByte.Length, new IPEndPoint(IPAddress.Parse(_packet.TargetIP), ServerListenPort));
                        if (_packet.TargetIP == ServerIP) sent = true;
                    }
                    else
                    {
                        _packet.SendType = FMSendType.Server;
                        _packet.SendByte[1] = 1;//when broadcast mode enabled, force the send type to server(SendByte[1] = 1), then it won't send twice to others

                        Client.Send(_packet.SendByte, _packet.SendByte.Length, new IPEndPoint(IPAddress.Broadcast, ServerListenPort));
                        sent = true;
                    }

                    if (CurrentSeenTimeMS - LastSentTimeMS > 2000)
                    {
                        //check connection: minimum 2000ms
                        SendHandShaking(new IPEndPoint(IPAddress.Broadcast, ServerListenPort));
                        sent = true;
                    }
                }

                //buffer retry... check ack later
                if (_packet.Reliable) _appendQueueRetryPacket.Enqueue(_packet);

                return sent;
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
                destroy = false;
                Action_StartClient();
            }

            void StopAll()
            {
                //skip, if stopped already
                if (stop)
                {
                    StopAllCoroutines();//stop all coroutines, just in case
                    return;
                }

                //try sending disconnect signal 94 as possible, before destroy
                if (IsConnected && FoundServer)
                {
                    //SendClosed();
                    //send status "closed" in background before end...
                    SendClientClosedAsync(ServerIP, ServerListenPort);

                    if (Client != null)
                    {
                        try { Client.Close(); }
                        catch (Exception e) { DebugLog(e.Message); }
                        Client = null;
                    }

                    Manager.OnLostServer(ServerIP);
                }

                if (ClientListener != null)
                {
                    try
                    {
                        ClientListener.DropMulticastGroup(IPAddress.Parse(MulticastAddress));
                        ClientListener.Close();
                    }
                    catch (Exception e) { DebugLog(e.Message); }
                    ClientListener = null;
                }

                stop = true;
                IsConnected = false;
                FoundServer = false;
                StopAllCoroutines();

                _appendQueueSendPacket = new ConcurrentQueue<FMPacket>();
                _appendQueueReceivedPacket = new ConcurrentQueue<FMPacket>();
            }

            private async void SendClientClosedAsync(string IP, int Port)
            {
                await Task.Yield();
                UdpClient Client = new UdpClient();
                try
                {
                    Client.Client.SendBufferSize = udpSendBufferSize;
                    Client.Client.ReceiveBufferSize = udpReceiveBufferSize;
                    Client.Client.SendTimeout = 500;
                    Client.EnableBroadcast = true;

                    byte[] _byte = new byte[] { 94 };
                    Client.Send(_byte, _byte.Length, new IPEndPoint(IPAddress.Parse(IP), Port));
                }
                catch { }
            }
        }
    }
}