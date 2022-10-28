using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FMETP.FMSocketIO;

namespace FMETP
{
    [System.Serializable] public enum DemoDebugSystem { FMWebSocket, FMSocketIO }
    public class FMWebSocketNetwork_debug : MonoBehaviour
    {
        public DemoDebugSystem debugSystem = DemoDebugSystem.FMWebSocket;

        public Text debugText;

        public void Action_SendStringAll(string _string)
        {
            if (debugSystem == DemoDebugSystem.FMWebSocket)
            {
                if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToAll(_string);
            }
            else
            {
                if (FMSocketIOManager.instance != null) FMSocketIOManager.instance.SendToAll(_string);
            }
        }
        public void Action_SendStringServer(string _string)
        {
            if (debugSystem == DemoDebugSystem.FMWebSocket)
            {
                if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToServer(_string);
            }
            else
            {
                if (FMSocketIOManager.instance != null) FMSocketIOManager.instance.SendToServer(_string);
            }
        }

        public void Action_SendStringOthers(string _string)
        {
            if (debugSystem == DemoDebugSystem.FMWebSocket)
            {
                if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToOthers(_string);
            }
            else
            {
                if (FMSocketIOManager.instance != null) FMSocketIOManager.instance.SendToOthers(_string);
            }
        }

        public void Action_SendByteAll()
        {
            if (debugSystem == DemoDebugSystem.FMWebSocket)
            {
                if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToAll(new byte[3]);
            }
            else
            {
                if (FMSocketIOManager.instance != null) FMSocketIOManager.instance.SendToAll(new byte[3]);
            }
        }
        public void Action_SendByteServer()
        {
            if (debugSystem == DemoDebugSystem.FMWebSocket)
            {
                if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToServer(new byte[4]);
            }
            else
            {
                if (FMSocketIOManager.instance != null) FMSocketIOManager.instance.SendToServer(new byte[4]);
            }
        }
        public void Action_SendByteOthers()
        {
            if (debugSystem == DemoDebugSystem.FMWebSocket)
            {
                if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToOthers(new byte[5]);
            }
            else
            {
                if (FMSocketIOManager.instance != null) FMSocketIOManager.instance.SendToOthers(new byte[5]);
            }
        }
        //public void Action_SendByteTarget(string _wsid)
        //{
        //    if (debugSystem == DemoDebugSystem.FMWebSocket)
        //    {
        //        //if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToTarget(new byte[5], _wsid);
        //        if (FMWebSocketManager.instance != null) FMWebSocketManager.instance.SendToTarget("TargetAAA", _wsid);
        //    }
        //}

        // Update is called once per frame
        void Update()
        {

            debugText.text = "";
            if (debugSystem == DemoDebugSystem.FMWebSocket)
            {
                if (FMWebSocketManager.instance != null) debugText.text += "[connected: " + FMWebSocketManager.instance.Settings.ConnectionStatus + "]";
            }
            else
            {
                if (FMSocketIOManager.instance != null) debugText.text += "[connected: " + FMSocketIOManager.instance.Ready + "]";
            }
            debugText.text += _received;

        }

        private string _received = "";
        public void Action_OnReceivedData(string _string)
        {
            //debugText.text = "received: " + _string;
            _received = "received: " + _string;
        }
        public void Action_OnReceivedData(byte[] _byte)
        {
            //debugText.text = "received(byte): " + _byte.Length;
            _received = "received(byte): " + _byte.Length;
        }
    }
}