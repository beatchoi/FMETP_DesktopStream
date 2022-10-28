using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;


namespace FMETP
{
    public class FMWebSocketWebGL
    {
        public class FMWebSocketComponentWebGL : MonoBehaviour
        {
#if UNITY_WEBGL
#if UNITY_2021_2_OR_NEWER
        [DllImport("__Internal")] private static extern void FMWebSocket_AddGZip_2021_2(string _src);
        [DllImport("__Internal")] private static extern string FMWebSocket_IsWebSocketConnected_2021_2();
        [DllImport("__Internal")] private static extern void FMWebSocket_AddEventListeners_2021_2(string _src, string _gameobject);
        [DllImport("__Internal")] private static extern void FMWebSocket_SendByte_2021_2(byte[] array, int size);
        [DllImport("__Internal")] private static extern void FMWebSocket_SendString_2021_2(string _src);
        [DllImport("__Internal")] private static extern void FMWebSocket_Close_2021_2();
#else
        [DllImport("__Internal")] private static extern void FMWebSocket_AddGZip_2021_2_before(string _src);
        [DllImport("__Internal")] private static extern string FMWebSocket_IsWebSocketConnected_2021_2_before();
        [DllImport("__Internal")] private static extern void FMWebSocket_AddEventListeners_2021_2_before(string _src, string _gameobject);
        [DllImport("__Internal")] private static extern void FMWebSocket_SendByte_2021_2_before(byte[] array, int size);
        [DllImport("__Internal")] private static extern void FMWebSocket_SendString_2021_2_before(string _src);
        [DllImport("__Internal")] private static extern void FMWebSocket_Close_2021_2_before();
#endif
#endif
            [HideInInspector] public FMWebSocketManager Manager;
            public string IP = "127.0.0.1";
            public int port = 3000;
            public bool sslEnabled = false;
            public FMSslProtocols sslProtocols = FMSslProtocols.Default;
            public bool portRequired = true;
            public string url = "ws://127.0.0.1:3000";

            public bool autoReconnect = true;
            public bool ShowLog = true;
            private void DebugLog(string _value) { if (ShowLog) Debug.Log("FMLog: " + _value); }
            private ConcurrentQueue<byte[]> _appendQueueSendFMWebSocketData = new ConcurrentQueue<byte[]>();
            private ConcurrentQueue<byte[]> _appendQueueSendWebSocketByteData = new ConcurrentQueue<byte[]>();
            private ConcurrentQueue<string> _appendQueueSendWebSocketStringData = new ConcurrentQueue<string>();

            private ConcurrentQueue<byte[]> _appendQueueReceivedData = new ConcurrentQueue<byte[]>();
            private ConcurrentQueue<string> _appendQueueReceivedStringData = new ConcurrentQueue<string>();

            public FMWebSocketNetworkType NetworkType = FMWebSocketNetworkType.Client;
            public int CurrentSeenTimeMS = 0;
            public int LastSeenTimeMS = 0;

            private bool stop = false;

            private bool _fmconnected = false;
            private bool fmconnected
            {
                get { return _fmconnected; }
                set
                {
                    _fmconnected = value;
                    UpdateManagerStatus();
                }
            }
            private bool wsConnected;

            private FMWebSocketConnectionStatus connectionStatus = FMWebSocketConnectionStatus.Disconnected;
            private void UpdateManagerStatus()
            {
                if (wsConnected)
                {
                    if (fmconnected)
                    {
                        connectionStatus = FMWebSocketConnectionStatus.FMWebSocketConnected;
                    }
                    else
                    {
                        connectionStatus = FMWebSocketConnectionStatus.WebSocketReady;
                    }
                }
                else
                {
                    connectionStatus = FMWebSocketConnectionStatus.Disconnected;
                }
                Manager.Settings.ConnectionStatus = connectionStatus;
            }

            public string wsid = "";
            public string serverWSID = "";
            public List<ConnectedFMWebSocketClient> ConnectedClients = new List<ConnectedFMWebSocketClient>();

            private void FMWebSocket_AddGZip(string _src)
            {
#if UNITY_WEBGL
#if UNITY_2021_2_OR_NEWER
            FMWebSocket_AddGZip_2021_2(_src);
#else
            FMWebSocket_AddGZip_2021_2_before(_src);
#endif
#endif
            }
            private void Start()
            {
                //add once only
                #region Add GZip
                string src = "http" + (sslEnabled ? "s" : "") + "://" + IP;
                if (portRequired) src += (port != 0 ? ":" + port.ToString() : "");
                string srcGZip = src + "/lib/gunzip.min.js";
                FMWebSocket_AddGZip(srcGZip);
                #endregion

                StartAll();
            }
            private void RegisterNetworkType()
            {
                if (NetworkType == FMWebSocketNetworkType.Server) { FMWebSocket_Send(new byte[] { 0, 0, 9, 3 }); }
                else { FMWebSocket_Send(new byte[] { 0, 0, 9, 4 }); }

                GetWSConnectionStatus();
                StartCoroutine(WebSocketStartCOR());
            }
            public void RegOnOpen() { RegisterNetworkType(); DebugLog(">>> UNITY: ON OPEN"); }
            public void RegOnClose() { DebugLog(">>> UNITY: ON Close"); wsConnected = false; fmconnected = false; }
            public void RegOnError(string _msg) { DebugLog(">>> UNITY: (Error) " + _msg); wsConnected = false; fmconnected = false; }
            public void RegOnMessageString(string _msg)
            {
                _appendQueueReceivedStringData.Enqueue(_msg);
            }
            public void RegOnMessageRawData(string _stringData)
            {
                //Binary
                _appendQueueReceivedData.Enqueue(System.Convert.FromBase64String(_stringData));
            }
            private void OnMessageCheck(string _msg)
            {
                DebugLog(">>> UNITY: (MESSAGE) " + _msg);
                if (_msg.Contains("heartbeat"))
                {
                    LastSeenTimeMS = Environment.TickCount;
                    if (LastSeenTimeMS == 0) LastSeenTimeMS++;
                }
                if (_msg.Contains("OnReceivedWSIDEvent("))
                {
                    string[] tempStrings = _msg.Split('(');
                    string result = tempStrings[1].Remove(tempStrings[1].Length - 1);
                    wsid = result;
                    Manager.Settings.wsid = result;
                }
                else if (_msg.Contains("OnFoundServerEvent("))
                {
                    string[] tempStrings = _msg.Split('(');
                    string result = tempStrings[1].Remove(tempStrings[1].Length - 1);
                    Manager.OnFoundServerEvent.Invoke(result);

                    serverWSID = result;
                }
                else if (_msg.Contains("OnLostServerEvent("))
                {
                    string[] tempStrings = _msg.Split('(');
                    string result = tempStrings[1].Remove(tempStrings[1].Length - 1);
                    Manager.OnLostServerEvent.Invoke(result);

                    serverWSID = "";
                }
                else if (_msg.Contains("OnClientConnectedEvent("))
                {
                    string[] tempStrings = _msg.Split('(');
                    string result = tempStrings[1].Remove(tempStrings[1].Length - 1);
                    bool existed = false;
                    for (int i = 0; i < ConnectedClients.Count; i++)
                    {
                        if (result == ConnectedClients[i].wsid) existed = true;
                    }

                    if (!existed)
                    {
                        //register new client
                        ConnectedFMWebSocketClient NewClient = new ConnectedFMWebSocketClient();
                        NewClient.wsid = result;
                        ConnectedClients.Add(NewClient);
                        Manager.OnClientConnectedEvent.Invoke(result);
                    }
                }
                else if (_msg.Contains("OnClientDisconnectedEvent("))
                {
                    string[] tempStrings = _msg.Split('(');
                    string result = tempStrings[1].Remove(tempStrings[1].Length - 1);
                    Manager.OnClientDisconnectedEvent.Invoke(result);

                    for (int i = 0; i < ConnectedClients.Count; i++)
                    {
                        if (result == ConnectedClients[i].wsid)
                        {
                            //remove disconnected client
                            ConnectedClients.Remove(ConnectedClients[i]);
                        }
                    }
                }
            }

            private bool FMWebSocket_IsWebSocketConnected()
            {
                string returnValue = "false";
#if UNITY_WEBGL
#if UNITY_2021_2_OR_NEWER
            returnValue = FMWebSocket_IsWebSocketConnected_2021_2();
#else
            returnValue = FMWebSocket_IsWebSocketConnected_2021_2_before();
#endif
#endif
                //DebugLog("return value: -> > > " + returnValue);
                return returnValue == "true" ? true : false;
            }

            public void RegWebSocketConnected() { wsConnected = true; fmconnected = false; }
            public void RegWebSocketDisconnected() { wsConnected = false; fmconnected = false; }

            public void Connect() { if (connectionStatus == FMWebSocketConnectionStatus.Disconnected) StartAll(); }
            public void Close() { StopAll(); }
            private void OnApplicationQuit() { StopAll(); }
            private void OnDisable() { StopAll(); }
            private void OnDestroy() { StopAll(); }
            private void OnEnable()
            {
                if (Time.timeSinceLevelLoad <= 3f) return;
                if (stop) StartAll();
            }

            private void StartAll()
            {
                stop = false;
                ConnectAndAddEventListeners();
            }

            public void ConnectAndAddEventListeners()
            {
                DebugLog(">>> ConnectAndAddEventListeners");
                string src = "ws" + (sslEnabled ? "s" : "") + "://" + IP;
                if (portRequired) src += (port != 0 ? ":" + port.ToString() : "");
                FMWebSocket_AddEventListeners(src, gameObject.name);

                StartCoroutine(ConnectionCheckerCOR());
            }

            private void FMWebSocket_AddEventListeners(string _src, string _gameobject)
            {
#if UNITY_WEBGL
#if UNITY_2021_2_OR_NEWER
            FMWebSocket_AddEventListeners_2021_2(_src, _gameobject);
#else
            FMWebSocket_AddEventListeners_2021_2_before(_src, _gameobject);
#endif
#endif
            }

            private bool GetWSConnectionStatus()
            {
                wsConnected = FMWebSocket_IsWebSocketConnected();
                //check if fm heartbeat received?
                CurrentSeenTimeMS = Environment.TickCount;
                if (LastSeenTimeMS != 0)
                {
                    if (CurrentSeenTimeMS < 0 && LastSeenTimeMS > 0)
                    {
                        fmconnected = (Mathf.Abs(CurrentSeenTimeMS - int.MinValue) + (int.MaxValue - LastSeenTimeMS) < 3000) ? true : false;
                    }
                    else
                    {
                        fmconnected = ((CurrentSeenTimeMS - LastSeenTimeMS) < 3000) ? true : false;
                    }
                }
                else { fmconnected = false; }

                return wsConnected;
            }
            IEnumerator ConnectionCheckerCOR()
            {
                yield return new WaitForSecondsRealtime(5f);
                while (!stop)
                {
                    if (!GetWSConnectionStatus())
                    {
                        if (autoReconnect)
                        {
                            try
                            {
                                FMWebSocket_Close();
                                ConnectAndAddEventListeners();
                                DebugLog("reconnecting");
                            }
                            catch (Exception e)
                            {
                                DebugLog("Connection Execption: " + e.Message);
                            }
                        }
                    }
                    yield return new WaitForSecondsRealtime(5f);
                }
                FMWebSocket_Close();
            }

            private void FMWebSocket_Close()
            {
#if UNITY_WEBGL
#if UNITY_2021_2_OR_NEWER
            FMWebSocket_Close_2021_2();
#else
            FMWebSocket_Close_2021_2_before();
#endif
#endif
                wsConnected = false; fmconnected = false;
            }
            private void StopAll()
            {
                FMWebSocket_Close();

                //skip, if stopped already
                if (stop)
                {
                    StopAllCoroutines();//stop all coroutines, just in case
                    return;
                }

                stop = true;
                wsConnected = false;
                fmconnected = false;

                StopAllCoroutines();
                _appendQueueSendFMWebSocketData = new ConcurrentQueue<byte[]>();
                _appendQueueSendWebSocketByteData = new ConcurrentQueue<byte[]>();
                _appendQueueSendWebSocketStringData = new ConcurrentQueue<string>();

                _appendQueueReceivedData = new ConcurrentQueue<byte[]>();
                _appendQueueReceivedStringData = new ConcurrentQueue<string>();
            }

            private void FMWebSocket_Send(byte[] _byteData)
            {
#if UNITY_WEBGL
#if UNITY_2021_2_OR_NEWER
            FMWebSocket_SendByte_2021_2(_byteData, _byteData.Length);
#else
            FMWebSocket_SendByte_2021_2_before(_byteData, _byteData.Length);
#endif
#endif
            }
            private void WebSocket_Send(byte[] _byteData)
            {
#if UNITY_WEBGL
#if UNITY_2021_2_OR_NEWER
            FMWebSocket_SendByte_2021_2(_byteData, _byteData.Length);
#else
            FMWebSocket_SendByte_2021_2_before(_byteData, _byteData.Length);
#endif
#endif
            }
            private void WebSocket_Send(string _stringData)
            {
#if UNITY_WEBGL
#if UNITY_2021_2_OR_NEWER
            FMWebSocket_SendString_2021_2(_stringData);
#else
            FMWebSocket_SendString_2021_2_before(_stringData);
#endif
#endif
            }
            private void Sender()
            {
                if (NetworkType != FMWebSocketNetworkType.WebSocket)
                {
                    //FM WebSocket Mode
                    if (connectionStatus != FMWebSocketConnectionStatus.FMWebSocketConnected) return;
                    if (_appendQueueSendFMWebSocketData.Count > 0)
                    {
                        //limit 100 packet sent in each frame, solved overhead issue on receiver
                        int k = 0;
                        while (_appendQueueSendFMWebSocketData.Count > 0 && k < 100)
                        {
                            k++;
                            try
                            {
                                if (_appendQueueSendFMWebSocketData.TryDequeue(out byte[] _bytes)) FMWebSocket_Send(_bytes);
                            }
                            catch (Exception e) { DebugLog(e.ToString()); }
                        }
                    }
                }
                else
                {
                    //Raw WebSocket
                    if (connectionStatus == FMWebSocketConnectionStatus.Disconnected) return;
                    if (_appendQueueSendWebSocketByteData.Count > 0)
                    {
                        //limit 100 packet sent in each frame, solved overhead issue on receiver
                        int k = 0;
                        while (_appendQueueSendWebSocketByteData.Count > 0 && k < 100)
                        {
                            k++;
                            try
                            {
                                if (_appendQueueSendWebSocketByteData.TryDequeue(out byte[] _bytes)) WebSocket_Send(_bytes);
                            }
                            catch (Exception e) { DebugLog(e.ToString()); }
                        }
                    }
                    if (_appendQueueSendWebSocketStringData.Count > 0)
                    {
                        //limit 100 packet sent in each frame, solved overhead issue on receiver
                        int k = 0;
                        while (_appendQueueSendWebSocketStringData.Count > 0 && k < 100)
                        {
                            k++;
                            try
                            {
                                if (_appendQueueSendWebSocketStringData.TryDequeue(out string _string)) WebSocket_Send(_string);
                            }
                            catch (Exception e) { DebugLog(e.ToString()); }
                        }
                    }
                }
            }

            private IEnumerator MainThreadSenderCOR()
            {
                while (!stop)
                {
                    yield return null;
                    Sender();
                }
            }

            private IEnumerator WebSocketStartCOR()
            {
                stop = false;
                yield return new WaitForSeconds(0.5f);

                //WebGL is using main thread only
                StartCoroutine(MainThreadSenderCOR());

                while (!stop)
                {
                    while (_appendQueueReceivedData.Count > 0)
                    {
                        if (_appendQueueReceivedData.TryDequeue(out byte[] ReceivedData))
                        {
                            if (ReceivedData.Length > 4)
                            {
                                byte[] _meta = new byte[] { ReceivedData[0], ReceivedData[1], ReceivedData[2], ReceivedData[3] };
                                byte[] _data = new byte[ReceivedData.Length - 4];

                                if (_meta[1] == 3)
                                {
                                    //remove target wsid meta
                                    int _wsidByteLength = (int)BitConverter.ToUInt16(ReceivedData, 4);
                                    _data = new byte[ReceivedData.Length - 6 - _wsidByteLength];
                                    Buffer.BlockCopy(ReceivedData, 6 + _wsidByteLength, _data, 0, _data.Length);
                                }
                                else
                                {
                                    Buffer.BlockCopy(ReceivedData, 4, _data, 0, _data.Length);
                                }

                                switch (_meta[0])
                                {
                                    case 0: Manager.OnReceivedByteDataEvent.Invoke(_data); break;
                                    case 1: Manager.OnReceivedStringDataEvent.Invoke(Encoding.ASCII.GetString(_data)); break;
                                }
                            }
                            Manager.GetRawReceivedByteDataEvent.Invoke(ReceivedData);
                        }
                    }
                    while (_appendQueueReceivedStringData.Count > 0)
                    {
                        if (_appendQueueReceivedStringData.TryDequeue(out string ReceivedData))
                        {
                            OnMessageCheck(ReceivedData);
                            Manager.GetRawReceivedStringDataEvent.Invoke(ReceivedData);
                        }
                    }
                    yield return null;
                }
                yield break;
            }

            public void Send(byte[] _byteData, FMWebSocketSendType _type, string _targetID)
            {
                if (NetworkType == FMWebSocketNetworkType.WebSocket || connectionStatus != FMWebSocketConnectionStatus.FMWebSocketConnected) return;

                byte[] _meta = new byte[4]; _meta[0] = 0;//raw byte
                switch (_type)
                {
                    case FMWebSocketSendType.All: _meta[1] = 0; break;
                    case FMWebSocketSendType.Server: _meta[1] = 1; break;
                    case FMWebSocketSendType.Others: _meta[1] = 2; break;
                    case FMWebSocketSendType.Target: _meta[1] = 3; break;
                }

                if (_type != FMWebSocketSendType.Target)
                {
                    byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                    Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                    Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);
                    _appendQueueSendFMWebSocketData.Enqueue(_sendByte);
                }
                else
                {
                    byte[] _wsid = Encoding.ASCII.GetBytes(_targetID);
                    byte[] _wsidByteLength = BitConverter.GetBytes((UInt16)_wsid.Length);
                    byte[] _meta_wsid = new byte[_wsid.Length + _wsidByteLength.Length];
                    Buffer.BlockCopy(_wsidByteLength, 0, _meta_wsid, 0, _wsidByteLength.Length);
                    Buffer.BlockCopy(_wsid, 0, _meta_wsid, 2, _wsid.Length);

                    byte[] _sendByte = new byte[_byteData.Length + _meta.Length + _meta_wsid.Length];
                    Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                    Buffer.BlockCopy(_meta_wsid, 0, _sendByte, 4, _meta_wsid.Length);
                    Buffer.BlockCopy(_byteData, 0, _sendByte, _meta.Length + _meta_wsid.Length, _byteData.Length);
                    _appendQueueSendFMWebSocketData.Enqueue(_sendByte);
                }
            }
            public void Send(string _stringData, FMWebSocketSendType _type, string _targetID)
            {
                if (NetworkType == FMWebSocketNetworkType.WebSocket || connectionStatus != FMWebSocketConnectionStatus.FMWebSocketConnected) return;

                byte[] _byteData = Encoding.ASCII.GetBytes(_stringData);
                byte[] _meta = new byte[4]; _meta[0] = 1;//string data
                switch (_type)
                {
                    case FMWebSocketSendType.All: _meta[1] = 0; break;
                    case FMWebSocketSendType.Server: _meta[1] = 1; break;
                    case FMWebSocketSendType.Others: _meta[1] = 2; break;
                    case FMWebSocketSendType.Target: _meta[1] = 3; break;
                }

                if (_type != FMWebSocketSendType.Target)
                {
                    byte[] _sendByte = new byte[_byteData.Length + _meta.Length];
                    Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                    Buffer.BlockCopy(_byteData, 0, _sendByte, 4, _byteData.Length);
                    _appendQueueSendFMWebSocketData.Enqueue(_sendByte);
                }
                else
                {
                    byte[] _wsid = Encoding.ASCII.GetBytes(_targetID);
                    byte[] _wsidByteLength = BitConverter.GetBytes((UInt16)_wsid.Length);
                    byte[] _meta_wsid = new byte[_wsid.Length + _wsidByteLength.Length];
                    Buffer.BlockCopy(_wsidByteLength, 0, _meta_wsid, 0, _wsidByteLength.Length);
                    Buffer.BlockCopy(_wsid, 0, _meta_wsid, 2, _wsid.Length);

                    byte[] _sendByte = new byte[_byteData.Length + _meta.Length + _meta_wsid.Length];
                    Buffer.BlockCopy(_meta, 0, _sendByte, 0, _meta.Length);
                    Buffer.BlockCopy(_meta_wsid, 0, _sendByte, 4, _meta_wsid.Length);
                    Buffer.BlockCopy(_byteData, 0, _sendByte, _meta.Length + _meta_wsid.Length, _byteData.Length);
                    _appendQueueSendFMWebSocketData.Enqueue(_sendByte);
                }
            }

            public void Send(byte[] _byteData)
            {
                if (NetworkType != FMWebSocketNetworkType.WebSocket || connectionStatus == FMWebSocketConnectionStatus.Disconnected) return;
                _appendQueueSendWebSocketByteData.Enqueue(_byteData);
            }
            public void Send(string _stringData)
            {
                if (NetworkType != FMWebSocketNetworkType.WebSocket || connectionStatus == FMWebSocketConnectionStatus.Disconnected) return;
                _appendQueueSendWebSocketStringData.Enqueue(_stringData);
            }
        }
    }
}