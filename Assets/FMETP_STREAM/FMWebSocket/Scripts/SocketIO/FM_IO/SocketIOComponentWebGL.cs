using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using FMETP.FMSocketIO;
using WebSocketSharp;
using WebSocketSharp.Net;

using System.Runtime.InteropServices;

namespace FMETP
{
    [System.Serializable]
    public class EventJson
    {
        public string socketEvent;
        public string eventData;
    }

    public class SocketIOComponentWebGL : MonoBehaviour
    {
        public static SocketIOComponentWebGL instance;
        public string sid;

        // Use this for initialization
        void Awake() { if (instance == null) instance = this; }
        public string IP = "127.0.0.1";
        public int port = 3000;
        public bool sslEnabled = false;

        public bool portRequired = true;
        public bool socketIORequired = true;

        public bool DefaultQueryString = true;
        public string CustomisedQueryString = "?EIO=3&transport=websocket";

        public bool DebugMode = true;
        private void DebugLog(string _value)
        {
            if (!DebugMode) return;
            Debug.Log("FMLog: " + _value);
        }

        int packetId;
        Dictionary<string, List<Action<SocketIOEvent>>> eventHandlers;
        List<Ack> ackList;

        private bool _WebSocketConnected = false;
        public bool IsWebSocketConnected() { return _WebSocketConnected; }

        bool Ready = false;
        public bool IsReady() { return Ready; }

        private void Update()
        {

            if (FMSocketIOManager.instance != null) DebugMode = FMSocketIOManager.instance.DebugMode;

#if UNITY_WEBGL
            if (Ready)
            {
                if (!socketIORequired)
                {
                    REG_IsWebSocketConnected(gameObject.name);
                }
                else
                {
                    IsSocketIOConnected(gameObject.name);
                }
            }
#endif
        }

#if UNITY_WEBGL
        //>>> SocketIO >>>
        private void WebSocketAddSocketIO(string _src)
        {
#if UNITY_2021_2_OR_NEWER
            WebSocketAddSocketIO_2021_2(_src);
#else
            WebSocketAddSocketIO_2021_2_before(_src);
#endif
        }
        private void WebSocketAddGZip(string _src)
        {
#if UNITY_2021_2_OR_NEWER
            WebSocketAddGZip_2021_2(_src);
#else
            WebSocketAddGZip_2021_2_before(_src);
#endif
        }
        private void WebSocketAddEventListeners(string _gameobject)
        {
#if UNITY_2021_2_OR_NEWER
            WebSocketAddEventListeners_2021_2(_gameobject);
#else
            WebSocketAddEventListeners_2021_2_before(_gameobject);
#endif
        }
        private void WebSocketConnect(string _src, string _gameobject)
        {
#if UNITY_2021_2_OR_NEWER
            WebSocketConnect_2021_2(_src, _gameobject);
#else
            WebSocketConnect_2021_2_before(_src, _gameobject);
#endif
        }
        private void WebSocketClose()
        {
#if UNITY_2021_2_OR_NEWER
            WebSocketClose_2021_2();
#else
            WebSocketClose_2021_2_before();
#endif
        }
        private void WebSocketEmitEvent(string _e)
        {
#if UNITY_2021_2_OR_NEWER
            WebSocketEmitEvent_2021_2(_e);
#else
            WebSocketEmitEvent_2021_2_before(_e);
#endif
        }
        private void WebSocketEmitData(string _e, string _data)
        {
#if UNITY_2021_2_OR_NEWER
            WebSocketEmitData_2021_2(_e, _data);
#else
            WebSocketEmitData_2021_2_before(_e, _data);
#endif
        }
        private void WebSocketEmitEventAction(string _e, string _packetId, string _gameobject)
        {
#if UNITY_2021_2_OR_NEWER
            WebSocketEmitEventAction_2021_2(_e, _packetId, _gameobject);
#else
            WebSocketEmitEventAction_2021_2_before(_e, _packetId, _gameobject);
#endif
        }
        private void WebSocketEmitDataAction(string _e, string _data, string _packetId, string _gameobject)
        {
#if UNITY_2021_2_OR_NEWER
            WebSocketEmitDataAction_2021_2(_e, _data, _packetId, _gameobject);
#else
            WebSocketEmitDataAction_2021_2_before(_e, _data, _packetId, _gameobject);
#endif
        }
        private void WebSocketOn(string _e)
        {
#if UNITY_2021_2_OR_NEWER
            WebSocketOn_2021_2(_e);
#else
            WebSocketOn_2021_2_before(_e);
#endif
        }
        //>>> SocketIO >>>

        //>>> Check connection >>>
        private void IsSocketIOConnected(string _gameobject)
        {
#if UNITY_2021_2_OR_NEWER
            IsSocketIOConnected_2021_2(_gameobject);
#else
            IsSocketIOConnected_2021_2_before(_gameobject);
#endif
        }
        private void REG_IsWebSocketConnected(string _gameobject)
        {
#if UNITY_2021_2_OR_NEWER
            REG_IsWebSocketConnected_2021_2(_gameobject);
#else
            REG_IsWebSocketConnected_2021_2_before(_gameobject);
#endif
        }
        //>>> Check connection >>>

        //>>> Echo Server Test >>>
        private void REG_WebSocketAddEventListeners(string _src, string _gameobject)
        {
#if UNITY_2021_2_OR_NEWER
            REG_WebSocketAddEventListeners_2021_2(_src, _gameobject);
#else
            REG_WebSocketAddEventListeners_2021_2_before(_src, _gameobject);
#endif
        }
        private void REG_Send(string _src)
        {
#if UNITY_2021_2_OR_NEWER
            REG_Send_2021_2(_src);
#else
            REG_Send_2021_2_before(_src);
#endif
        }
        private void REG_Close()
        {
#if UNITY_2021_2_OR_NEWER
            REG_Close_2021_2();
#else
            REG_Close_2021_2_before();
#endif
        }
        //>>> Echo Server Test >>>

#if UNITY_2021_2_OR_NEWER
        //>>> SocketIO >>>
        [DllImport("__Internal")] private static extern void WebSocketAddSocketIO_2021_2(string _src);
        [DllImport("__Internal")] private static extern void WebSocketAddGZip_2021_2(string _src);
        [DllImport("__Internal")] private static extern void WebSocketAddEventListeners_2021_2(string _gameobject);
        [DllImport("__Internal")] private static extern void WebSocketConnect_2021_2(string _src, string _gameobject);
        [DllImport("__Internal")] private static extern void WebSocketClose_2021_2();
        [DllImport("__Internal")] private static extern void WebSocketEmitEvent_2021_2(string _e);
        [DllImport("__Internal")] private static extern void WebSocketEmitData_2021_2(string _e, string _data);
        [DllImport("__Internal")] private static extern void WebSocketEmitEventAction_2021_2(string _e, string _packetId, string _gameobject);
        [DllImport("__Internal")] private static extern void WebSocketEmitDataAction_2021_2(string _e, string _data, string _packetId, string _gameobject);
        [DllImport("__Internal")] private static extern void WebSocketOn_2021_2(string _e);
        //>>> SocketIO >>>

        //>>> Check connection >>>
        [DllImport("__Internal")] private static extern void IsSocketIOConnected_2021_2(string _gameobject);
        [DllImport("__Internal")] private static extern void REG_IsWebSocketConnected_2021_2(string _gameobject);
        //>>> Check connection >>>

        //>>> Echo Server Test >>>
        [DllImport("__Internal")] private static extern void REG_WebSocketAddEventListeners_2021_2(string _src, string _gameobject);
        [DllImport("__Internal")] private static extern void REG_Send_2021_2(string _src);
        [DllImport("__Internal")] private static extern void REG_Close_2021_2();
        //>>> Echo Server Test >>>
#else
        //>>> SocketIO >>>
        [DllImport("__Internal")] private static extern void WebSocketAddSocketIO_2021_2_before(string _src);
        [DllImport("__Internal")] private static extern void WebSocketAddGZip_2021_2_before(string _src);
        [DllImport("__Internal")] private static extern void WebSocketAddEventListeners_2021_2_before(string _gameobject);
        [DllImport("__Internal")] private static extern void WebSocketConnect_2021_2_before(string _src, string _gameobject);
        [DllImport("__Internal")] private static extern void WebSocketClose_2021_2_before();
        [DllImport("__Internal")] private static extern void WebSocketEmitEvent_2021_2_before(string _e);
        [DllImport("__Internal")] private static extern void WebSocketEmitData_2021_2_before(string _e, string _data);
        [DllImport("__Internal")] private static extern void WebSocketEmitEventAction_2021_2_before(string _e, string _packetId, string _gameobject);
        [DllImport("__Internal")] private static extern void WebSocketEmitDataAction_2021_2_before(string _e, string _data, string _packetId, string _gameobject);
        [DllImport("__Internal")] private static extern void WebSocketOn_2021_2_before(string _e);
        //>>> SocketIO >>>

        //>>> Check connection >>>
        [DllImport("__Internal")] private static extern void IsSocketIOConnected_2021_2_before(string _gameobject);
        [DllImport("__Internal")] private static extern void REG_IsWebSocketConnected_2021_2_before(string _gameobject);
        //>>> Check connection >>>

        //>>> Echo Server Test >>>
        [DllImport("__Internal")] private static extern void REG_WebSocketAddEventListeners_2021_2_before(string _src, string _gameobject);
        [DllImport("__Internal")] private static extern void REG_Send_2021_2_before(string _src);
        [DllImport("__Internal")] private static extern void REG_Close_2021_2_before();
        //>>> Echo Server Test >>>
#endif
#endif

        public void Init()
        {
            if (!socketIORequired) return;

            eventHandlers = new Dictionary<string, List<Action<SocketIOEvent>>>();
            ackList = new List<Ack>();
            AddSocketIO();
            AddEventListeners();
        }

        private void OnConnected(SocketIOEvent e) { DebugLog("[Event] SocketIO connected"); }

        void AddSocketIO()
        {
#if UNITY_WEBGL
            string src = "http" + (sslEnabled ? "s" : "") + "://" + IP;
            //if (portRequired) src += (!sslEnabled && port != 0 ? ":" + port.ToString() : "");
            if (portRequired) src += (port != 0 ? ":" + port.ToString() : "");

            string srcSocketIO = src + "/socket.io/socket.io.js";
            WebSocketAddSocketIO(srcSocketIO);

            string srcGZip = src + "/lib/gunzip.min.js";
            WebSocketAddGZip(srcGZip);
#endif
        }
        void AddEventListeners()
        {
#if UNITY_WEBGL
            WebSocketAddEventListeners(gameObject.name);
#endif
        }

        public void Connect()
        {
            DebugLog(">>> start connecting");
#if UNITY_WEBGL
            if (!socketIORequired)
            {
                string src = "ws" + (sslEnabled ? "s" : "") + "://" + IP;
                if (portRequired) src += (port != 0 ? ":" + port.ToString() : "");
                REG_WebSocketAddEventListeners(src, gameObject.name);
            }
            else
            {
                string src = "http" + (sslEnabled ? "s" : "") + "://" + IP;
                if (portRequired) src += (port != 0 ? ":" + port.ToString() : "");
                if (!DefaultQueryString) src += "/" + CustomisedQueryString;
                WebSocketConnect(src, gameObject.name);
            }
#endif
        }
        public void Close()
        {
#if UNITY_WEBGL
            if (!socketIORequired)
            {
                REG_Close();
            }
            else
            {
                WebSocketClose();
            }
#endif
            Ready = false;
        }


        public void Emit(string e)
        {
#if UNITY_WEBGL
            if (!socketIORequired)
            {
                REG_Send(e);
            }
            else
            {
                WebSocketEmitEvent(e);
            }
#endif
        }

        public void Emit(string e, string data)
        {
#if UNITY_WEBGL
            if (!socketIORequired)
            {
                REG_Send(e + ":" + data);
            }
            else
            {
                WebSocketEmitData(e, string.Format("{0}", data));
            }
#endif
        }

        public void Emit(string e, Action<string> action)
        {
            if (!socketIORequired) return;
#if UNITY_WEBGL
            packetId++;
            WebSocketEmitEventAction(e, packetId.ToString(), gameObject.name);
            ackList.Add(new Ack(packetId, action));
#endif
        }

        public void Emit(string e, string data, Action<string> action)
        {
            if (!socketIORequired) return;
#if UNITY_WEBGL
            packetId++;
            WebSocketEmitDataAction(e, data, packetId.ToString(), gameObject.name);
            ackList.Add(new Ack(packetId, action));
#endif
        }

        public void On(string e, Action<SocketIOEvent> callback)
        {
            if (!socketIORequired) return;
#if UNITY_WEBGL
            if (!eventHandlers.ContainsKey(e)) eventHandlers[e] = new List<Action<SocketIOEvent>>();
            eventHandlers[e].Add(callback);
            WebSocketOn(e);
#endif
        }

        public void Off(string e, Action<SocketIOEvent> callback)
        {
            if (!eventHandlers.ContainsKey(e)) return;
            List<Action<SocketIOEvent>> _eventHandlers = eventHandlers[e];
            if (!_eventHandlers.Contains(callback)) return;
            _eventHandlers.Remove(callback);
            if (_eventHandlers.Count == 0) eventHandlers.Remove(e);
        }

        public void InvokeAck(string ackJson)
        {
            Ack ack;
            Ack ackData = JsonUtility.FromJson<Ack>(ackJson);
            for (int i = 0; i < ackList.Count; i++)
            {
                if (ackList[i].packetId == ackData.packetId)
                {
                    ack = ackList[i];
                    ackList.RemoveAt(i);
                    ack.Invoke(ackJson);
                    return;
                }
            }
        }

        public void OnOpen()
        {
            Ready = true;
            DebugLog(">>> UNITY: ON OPEN");
        }

        public void SetSocketID(string socketID)
        {
            sid = socketID;
            DebugLog("socket id !: " + socketID);
            FMSocketIOManager.instance.Settings.socketID = sid;
        }

        public void InvokeEventCallback(string eventJson)
        {
            //DebugLog("getting event!");
            EventJson eventData = JsonUtility.FromJson<EventJson>(eventJson);
            if (!eventHandlers.ContainsKey(eventData.socketEvent)) return;
            for (int i = 0; i < eventHandlers[eventData.socketEvent].Count; i++)
            {
                SocketIOEvent socketEvent = new SocketIOEvent(eventData.socketEvent, eventData.eventData);
                eventHandlers[eventData.socketEvent][i](socketEvent);
            }
        }


        public void RegOnOpen()
        {
            Ready = true;
            //DebugLog(">>> UNITY: ON OPEN");
        }
        public void RegOnClose()
        {
            Ready = false;
            _WebSocketConnected = false;
            //DebugLog(">>> UNITY: ON Close");
        }

        public void RegOnMessage(string _msg)
        {
            if (FMSocketIOManager.instance != null) FMSocketIOManager.instance.OnReceivedRawMessageEvent.Invoke(_msg);
            //DebugLog(">>> UNITY: (MESSAGE) " + _msg);
        }
        public void RegOnError(string _msg)
        {
            //DebugLog(">>> UNITY: (Error) " + _msg);
        }

        public void RegWebSocketConnected()
        {
            _WebSocketConnected = true;
            //DebugLog(">>>>>>>>>>>>>>>> UNITY: (ReadyState) Connected! ");
        }
        public void RegWebSocketDisconnected()
        {
            _WebSocketConnected = false;
            //DebugLog(">>>>>>>>>>>>>>>> UNITY: (ReadyState) Not Connected! ");
        }
    }
}