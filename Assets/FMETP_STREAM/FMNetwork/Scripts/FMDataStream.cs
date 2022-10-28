using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;

/*
 * + StereoPi Commands(Example):
 * Connect via ssh:
 * ssh root@192.168.xx.xx
 * Pwd: root
 * 
 * Stop Default Stream:
 * /opt/StereoPi/stop.sh
 * 
 * Sending stream from Raspberry:
 * 
 * For UDP:
 * raspivid -t 0 -w 1280 -h 720 -fps 30 -3d sbs -cd MJPEG -o - | nc 192.168.1.10 3001 -u
 *
 * For TCP:
 * raspivid -t 0 -w 1280 -h 720 -fps 30 -3d sbs -cd MJPEG -o - | nc 192.168.1.10 3001
 * 
 * where 192.168.1.10 3001 - IP and port
*/

/*
 * + GStreamer Commands(Example):
 * + Desktop Capture to Unity
 * gst-launch-1.0 gdiscreencapsrc ! queue ! video/x-raw,framerate=60/1,width=1920, height=1080 ! jpegenc ! rndbuffersize max=65000 ! udpsink host=192.168.1.10 port=3001
 * 
 * + Video Stream to Unity
 * gst-launch-1.0 filesrc location="videopath.mp4" ! queue ! decodebin ! videoconvert ! jpegenc ! rndbuffersize max=65000 ! udpsink host=192.168.1.10 port=3001
 */

namespace FMETP
{
    public class FMDataStream
    {
        public class FMDataStreamComponent : MonoBehaviour
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            private int udpSendBufferSize = 1024 * 65; //max 65535
            private int udpReceiveBufferSize = 1024 * 1024 * 4; //max 2147483647
#else
        private int udpSendBufferSize = 1024 * 60; //max 65535
        private int udpReceiveBufferSize = 1024 * 512; //max 2147483647
#endif

            [HideInInspector] public FMNetworkManager Manager;

            public FMDataStreamType DataStreamType = FMDataStreamType.Receiver;
            public FMProtocol Protocol = FMProtocol.UDP;

            public void BroadcastChecker()
            {
                UdpClient BroadcastClient = new UdpClient();
                try
                {
                    BroadcastClient.Client.SendTimeout = 200;
                    BroadcastClient.EnableBroadcast = true;

                    byte[] _byte = new byte[1];
                    BroadcastClient.Send(_byte, _byte.Length, new IPEndPoint(IPAddress.Broadcast, ClientListenPort));

                    if (BroadcastClient != null) BroadcastClient.Close();
                }
                catch
                {
                    if (BroadcastClient != null) BroadcastClient.Close();
                }
            }

            //Sender props..
            public string ClientIP = "127.0.0.1";
            public UdpClient ClientSender;
            private ConcurrentQueue<byte[]> _appendSendBytes = new ConcurrentQueue<byte[]>();
            public bool UseMainThreadSender = false;

            public void Action_AddBytes(byte[] inputBytes)
            {
                _appendSendBytes.Enqueue(inputBytes);
            }
            IEnumerator NetworkClientStartUDPSenderCOR()
            {
                stop = false;

                BroadcastChecker();
                yield return null;

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
                            System.Threading.Thread.Sleep(1);
                        }
                        System.Threading.Thread.Sleep(1);
                    });
                    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Client Sender ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                }
            }

            IEnumerator MainThreadSenderCOR()
            {
                //client request
                while (!stop)
                {
                    yield return null;
                    Sender();
                }
            }

            private void Sender()
            {
                try
                {
                    if (ClientSender == null)
                    {
                        ClientSender = new UdpClient();
                        ClientSender.Client.SendBufferSize = udpSendBufferSize;
                        ClientSender.Client.ReceiveBufferSize = udpReceiveBufferSize;
                        ClientSender.Client.SendTimeout = 500;
                        ClientSender.EnableBroadcast = true;
                    }

                    //send to server ip only
                    if (_appendSendBytes.Count > 0)
                    {
                        //limit 30 packet sent in each frame, solved overhead issue on receiver
                        int sendCount = 0;
                        while (_appendSendBytes.Count > 0 && sendCount < 100)
                        {
                            sendCount++;
                            if (_appendSendBytes.TryDequeue(out byte[] _bytes))
                            {
                                if (UDPListenerType == FMUDPListenerType.Broadcast)
                                {
                                    ClientSender.Send(_bytes, _bytes.Length, new IPEndPoint(IPAddress.Broadcast, ClientListenPort));
                                }
                                else
                                {
                                    ClientSender.Send(_bytes, _bytes.Length, new IPEndPoint(IPAddress.Parse(ClientIP), ClientListenPort));
                                }
                            }
                        }
                    }
                }
                catch
                {
                    //DebugLog("client sender timeout: " + socketException.ToString());
                    if (ClientSender != null) ClientSender.Close(); ClientSender = null;
                }
            }

            public int ClientListenPort = 3001;

            public FMUDPListenerType UDPListenerType = FMUDPListenerType.Unicast;
            public string MulticastAddress = "239.255.255.255";

            private UdpClient ClientListener;
            private IPEndPoint ServerEp;

            private TcpListener listener;
            private List<TcpClient> clients = new List<TcpClient>();
            private List<NetworkStream> streams = new List<NetworkStream>();
            private bool CreatedServer = false;
            public bool IsConnected = false;


            [HideInInspector] public int CurrentSeenTimeMS;
            [HideInInspector] public int LastReceivedTimeMS;

            private bool stop = false;
            private ConcurrentQueue<byte[]> _appendQueueReceivedBytes = new ConcurrentQueue<byte[]>();

            private int ReceivedCount = 0;

            #region TCP
            IEnumerator NetworkServerStartTCPListenerCOR()
            {
                if (!CreatedServer)
                {
                    CreatedServer = true;

                    // create listener
                    listener = new TcpListener(IPAddress.Any, ClientListenPort);
                    listener.Start();
                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                    // create LOOM thread, only create on first time, otherwise we will crash max thread limit
                    // Wait for client to connect in another Thread 
                    Loom.RunAsync(() =>
                    {
                        while (!stop)
                        {
                        // Wait for client connection
                        clients.Add(listener.AcceptTcpClient());
                            clients[clients.Count - 1].NoDelay = true;
                        //IsConnected = true;

                        streams.Add(clients[clients.Count - 1].GetStream());
                            streams[streams.Count - 1].WriteTimeout = 500;

                            Loom.QueueOnMainThread(() =>
                            {
                            //IsConnected = true;
                            if (clients != null)
                                {
                                    if (clients.Count > 0) StartCoroutine(TCPReceiverCOR(clients[clients.Count - 1], streams[streams.Count - 1]));
                                }

                            });
                            System.Threading.Thread.Sleep(1);
                        }
                    });

                    while (!stop)
                    {
                        ReceivedCount = _appendQueueReceivedBytes.Count;
                        while (_appendQueueReceivedBytes.Count > 0)
                        {
                            if (_appendQueueReceivedBytes.TryDequeue(out byte[] receivedBytes))
                            {
                                Manager.OnReceivedByteDataEvent.Invoke(receivedBytes);
                            }
                        }
                        yield return null;
                    }
                }
                yield break;
            }

            IEnumerator TCPReceiverCOR(TcpClient _client, NetworkStream _stream)
            {
                bool _break = false;
                _stream.ReadTimeout = 1000;

                Loom.RunAsync(() =>
                {
                    while (!_client.Connected) System.Threading.Thread.Sleep(1);
                    while (!stop && !_break)
                    {
                        _stream.Flush();
                        byte[] bytes = new byte[300000];

                    // Loop to receive all the data sent by the client.
                    int _length;
                        while ((_length = _stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            if (_length > 0)
                            {
                                byte[] _s = new byte[_length];
                                Buffer.BlockCopy(bytes, 0, _s, 0, _length);
                                _appendQueueReceivedBytes.Enqueue(_s);
                                LastReceivedTimeMS = Environment.TickCount;
                            }
                            System.Threading.Thread.Sleep(1);
                        }

                        if (_length == 0)
                        {
                            if (_stream != null)
                            {
                                try { _stream.Close(); }
                                catch (Exception e) { DebugLog(e.Message); }
                            }

                            if (_client != null)
                            {
                                try { _client.Close(); }
                                catch (Exception e) { DebugLog(e.Message); }
                            }

                            for (int i = 0; i < clients.Count; i++)
                            {
                                if (_client == clients[i])
                                {
                                    streams.Remove(streams[i]);
                                    clients.Remove(clients[i]);
                                }
                            }
                            _break = true;
                        }
                    }
                    System.Threading.Thread.Sleep(1);
                });

                while (!stop && !_break) yield return null;
                yield break;
            }
            #endregion

            #region UDP
            IEnumerator NetworkClientStartUDPListenerCOR()
            {
                LastReceivedTimeMS = Environment.TickCount;

                stop = false;
                yield return new WaitForSeconds(0.5f);

                BroadcastChecker();
                yield return new WaitForSeconds(0.5f);

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
                                ClientListener.Client.ReceiveTimeout = 2000;

                                switch (UDPListenerType)
                                {
                                    case FMUDPListenerType.Unicast: break;
                                    case FMUDPListenerType.Multicast:
                                        ClientListener.MulticastLoopback = true;
                                        ClientListener.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));
                                        break;
                                    case FMUDPListenerType.Broadcast:
                                        ClientListener.EnableBroadcast = true;
                                        break;
                                }

                                ServerEp = new IPEndPoint(IPAddress.Any, ClientListenPort);
                            }

                            byte[] ReceivedData = ClientListener.Receive(ref ServerEp);
                            LastReceivedTimeMS = Environment.TickCount;

                        //=======================Decode Data=======================
                        _appendQueueReceivedBytes.Enqueue(ReceivedData);
                        }
                        catch
                        {
                            if (ClientListener != null) ClientListener.Close(); ClientListener = null;
                        }
                    //System.Threading.Thread.Sleep(1);
                }
                    System.Threading.Thread.Sleep(1);
                });
                //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Client Receiver ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

                while (!stop)
                {
                    ReceivedCount = _appendQueueReceivedBytes.Count;
                    while (_appendQueueReceivedBytes.Count > 0)
                    {
                        if (_appendQueueReceivedBytes.TryDequeue(out byte[] receivedBytes))
                        {
                            Manager.OnReceivedByteDataEvent.Invoke(receivedBytes);
                        }
                    }
                    yield return null;
                }
                yield break;
            }
            #endregion

            public void Action_StartClient() { StartAll(); }
            public void Action_StopClient() { StopAll(); }

            void StartAll()
            {
                stop = false;

                if (DataStreamType == FMDataStreamType.Receiver)
                {
                    switch (Protocol)
                    {
                        case FMProtocol.UDP: StartCoroutine(NetworkClientStartUDPListenerCOR()); break;
                        case FMProtocol.TCP: StartCoroutine(NetworkServerStartTCPListenerCOR()); break;
                    }
                }
                else
                {
                    StartCoroutine(NetworkClientStartUDPSenderCOR());
                }
            }

            void StopAll()
            {
                stop = true;

                if (DataStreamType == FMDataStreamType.Receiver)
                {
                    switch (Protocol)
                    {
                        case FMProtocol.UDP:
                            StopAllCoroutines();
                            if (ClientListener != null)
                            {
                                try { ClientListener.Close(); }
                                catch (Exception e) { DebugLog(e.Message); }
                                ClientListener = null;
                            }
                            break;
                        case FMProtocol.TCP:
                            foreach (TcpClient client in clients)
                            {
                                if (client != null)
                                {
                                    try { client.Close(); }
                                    catch (Exception e) { DebugLog(e.Message); }
                                }
                                IsConnected = false;
                            }
                            break;
                    }
                }
                else
                {
                    StopAllCoroutines();
                    _appendSendBytes = new ConcurrentQueue<byte[]>();
                }
            }

            // Start is called before the first frame update
            void Start()
            {
                Application.runInBackground = true;
                StartAll();
            }

            private void Update()
            {
                CurrentSeenTimeMS = Environment.TickCount;
                if (CurrentSeenTimeMS < 0 && LastReceivedTimeMS > 0)
                {
                    IsConnected = (Mathf.Abs(CurrentSeenTimeMS - int.MinValue) + (int.MaxValue - LastReceivedTimeMS) < 3000) ? true : false;
                }
                else
                {
                    IsConnected = ((CurrentSeenTimeMS - LastReceivedTimeMS) < 3000) ? true : false;
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
        }
    }
}