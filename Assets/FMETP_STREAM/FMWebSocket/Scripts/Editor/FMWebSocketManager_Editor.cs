using System;
using UnityEngine;
using UnityEditor;

namespace FMETP
{
    [CustomEditor(typeof(FMWebSocketManager))]
    [CanEditMultipleObjects]
    public class FMWebSocketManager_Editor : UnityEditor.Editor
    {
        private FMWebSocketManager FMWebSocket;
        SerializedProperty AutoInitProp;
        SerializedProperty NetworkTypeProp;

        SerializedProperty Settings_IPProp;
        SerializedProperty Settings_portProp;
        SerializedProperty Settings_sslEnabledProp;
        SerializedProperty Settings_sslProtocolsProp;
        SerializedProperty Settings_portRequiredProp;
        SerializedProperty Settings_UseMainThreadSenderProp;
        SerializedProperty Settings_ConnectionStatusProp;
        SerializedProperty Settings_wsidProp;
        SerializedProperty Settings_autoReconnectProp;

        SerializedProperty OnReceivedByteDataEventProp;
        SerializedProperty OnReceivedStringDataEventProp;
        SerializedProperty GetRawReceivedByteDataEventProp;
        SerializedProperty GetRawReceivedStringDataEventProp;

        SerializedProperty OnClientConnectedEventProp;
        SerializedProperty OnClientDisconnectedEventProp;
        SerializedProperty OnFoundServerEventProp;
        SerializedProperty OnLostServerEventProp;

        SerializedProperty ShowLogProp;

        void OnEnable()
        {
            AutoInitProp = serializedObject.FindProperty("AutoInit");
            NetworkTypeProp = serializedObject.FindProperty("NetworkType");

            Settings_IPProp = serializedObject.FindProperty("Settings.IP");
            Settings_portProp = serializedObject.FindProperty("Settings.port");
            Settings_sslEnabledProp = serializedObject.FindProperty("Settings.sslEnabled");
            Settings_sslProtocolsProp = serializedObject.FindProperty("Settings.sslProtocols");
            Settings_portRequiredProp = serializedObject.FindProperty("Settings.portRequired");
            Settings_UseMainThreadSenderProp = serializedObject.FindProperty("Settings.UseMainThreadSender");
            Settings_ConnectionStatusProp = serializedObject.FindProperty("Settings.ConnectionStatus");
            Settings_wsidProp = serializedObject.FindProperty("Settings.wsid");
            Settings_autoReconnectProp = serializedObject.FindProperty("Settings.autoReconnect");

            OnReceivedByteDataEventProp = serializedObject.FindProperty("OnReceivedByteDataEvent");
            OnReceivedStringDataEventProp = serializedObject.FindProperty("OnReceivedStringDataEvent");
            GetRawReceivedByteDataEventProp = serializedObject.FindProperty("GetRawReceivedByteDataEvent");
            GetRawReceivedStringDataEventProp = serializedObject.FindProperty("GetRawReceivedStringDataEvent");

            OnClientConnectedEventProp = serializedObject.FindProperty("OnClientConnectedEvent");
            OnClientDisconnectedEventProp = serializedObject.FindProperty("OnClientDisconnectedEvent");
            OnFoundServerEventProp = serializedObject.FindProperty("OnFoundServerEvent");
            OnLostServerEventProp = serializedObject.FindProperty("OnLostServerEvent");

            ShowLogProp = serializedObject.FindProperty("ShowLog");
        }

        // Update is called once per frame
        public override void OnInspectorGUI()
        {
            if (FMWebSocket == null) FMWebSocket = (FMWebSocketManager)target;

            serializedObject.Update();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                {
                    //Header
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.white;
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontSize = 15;

                    Texture2D backgroundTexture = new Texture2D(1, 1);
                    backgroundTexture.SetPixel(0, 0, new Color(0.02745098f, 0.1176471f, 0.254902f));
                    backgroundTexture.Apply();
                    style.normal.background = backgroundTexture;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("(( FM WebSocket 2.0 ))", style);
                    GUILayout.EndHorizontal();
                }

                if (!FMWebSocket.EditorShowNetworking)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Networking")) FMWebSocket.EditorShowNetworking = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Networking")) FMWebSocket.EditorShowNetworking = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginVertical();
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(AutoInitProp, new GUIContent("Auto Init"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(NetworkTypeProp, new GUIContent("NetworkType"));
                        GUILayout.EndHorizontal();

                        if (FMWebSocket.NetworkType == FMWebSocketNetworkType.Server
                            || FMWebSocket.NetworkType == FMWebSocketNetworkType.Client
                            || FMWebSocket.NetworkType == FMWebSocketNetworkType.WebSocket)
                        {
                            GUILayout.BeginVertical();
                            {
                                if (!FMWebSocket.EditorShowWebSocketSettings)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("= WebSocket Settings")) FMWebSocket.EditorShowWebSocketSettings = true;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();
                                }
                                else
                                {
                                    GUILayout.BeginHorizontal();
                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("- WebSocket Settings")) FMWebSocket.EditorShowWebSocketSettings = false;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        {
                                            string _url = "ws" + (FMWebSocket.Settings.sslEnabled ? "s" : "") + "://" + FMWebSocket.Settings.IP;
                                            if (FMWebSocket.Settings.portRequired) _url += ":" + FMWebSocket.Settings.port;
                                            GUILayout.Label("URL: " + _url);
                                        }
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        {
                                            string _url = "ws" + (FMWebSocket.Settings.sslEnabled ? "s" : "") + "://" + FMWebSocket.Settings.IP;
                                            if (FMWebSocket.Settings.portRequired) _url += ":" + FMWebSocket.Settings.port;
                                            GUILayout.Label("URL(WebGL): " + _url);
                                        }
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        {
                                            string _wsid = FMWebSocket.Settings.wsid.Length == 0 ? "null" : FMWebSocket.Settings.wsid;
                                            GUILayout.Label("WebSocket ID(wsid): " + _wsid);
                                        }
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_IPProp, new GUIContent("IP"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_portProp, new GUIContent("Port"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_sslEnabledProp, new GUIContent("ssl Enabled"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_sslProtocolsProp, new GUIContent("ssl Protocols"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_portRequiredProp, new GUIContent("port Required"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_ConnectionStatusProp, new GUIContent("Connection Status"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_autoReconnectProp, new GUIContent("auto Reconnect"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.BeginVertical("box");
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_UseMainThreadSenderProp, new GUIContent("UseMainThreadSender"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();
                                }
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.Space(2);
                    GUILayout.BeginVertical("box");
                    {
                        if (!FMWebSocket.EditorShowEvents)
                        {
                            GUILayout.BeginHorizontal();
                            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                            if (GUILayout.Button("+ Events")) FMWebSocket.EditorShowEvents = true;
                            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.BeginHorizontal();
                            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                            if (GUILayout.Button("- Events")) FMWebSocket.EditorShowEvents = false;
                            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            GUILayout.EndHorizontal();

                            if (FMWebSocket.NetworkType == FMWebSocketNetworkType.Server || FMWebSocket.NetworkType == FMWebSocketNetworkType.Client)
                            {
                                GUILayout.BeginVertical("box");
                                {
                                    if (!FMWebSocket.EditorShowConnectionEvents)
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                        if (GUILayout.Button("= Connection Events")) FMWebSocket.EditorShowConnectionEvents = true;
                                        GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                        GUILayout.EndHorizontal();
                                    }
                                    else
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                        if (GUILayout.Button("- Connection Events")) FMWebSocket.EditorShowConnectionEvents = false;
                                        GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginVertical("box");
                                        {
                                            if (FMWebSocket.NetworkType == FMWebSocketNetworkType.Server)
                                            {
                                                GUILayout.BeginHorizontal();
                                                EditorGUILayout.PropertyField(OnClientConnectedEventProp, new GUIContent("OnClientConnectedEvent"));
                                                GUILayout.EndHorizontal();

                                                GUILayout.BeginHorizontal();
                                                EditorGUILayout.PropertyField(OnClientDisconnectedEventProp, new GUIContent("OnClientDisconnectedEvent"));
                                                GUILayout.EndHorizontal();
                                            }
                                            else if (FMWebSocket.NetworkType == FMWebSocketNetworkType.Client)
                                            {
                                                GUILayout.BeginHorizontal();
                                                EditorGUILayout.PropertyField(OnFoundServerEventProp, new GUIContent("OnFoundServerEvent"));
                                                GUILayout.EndHorizontal();

                                                GUILayout.BeginHorizontal();
                                                EditorGUILayout.PropertyField(OnLostServerEventProp, new GUIContent("OnLostServerEvent"));
                                                GUILayout.EndHorizontal();
                                            }
                                        }
                                        GUILayout.EndVertical();
                                    }
                                }
                                GUILayout.EndVertical();
                            }

                            GUILayout.BeginVertical("box");
                            {
                                if (!FMWebSocket.EditorShowReceiverEvents)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("= Receiver Events")) FMWebSocket.EditorShowReceiverEvents = true;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();
                                }
                                else
                                {
                                    GUILayout.BeginHorizontal();
                                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                    if (GUILayout.Button("- Receiver Events")) FMWebSocket.EditorShowReceiverEvents = false;
                                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                    GUILayout.EndHorizontal();

                                    //GUILayout.Label("- Receiver");
                                    GUILayout.BeginVertical("box");
                                    {
                                        if (FMWebSocket.NetworkType == FMWebSocketNetworkType.Server || FMWebSocket.NetworkType == FMWebSocketNetworkType.Client)
                                        {
                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(OnReceivedByteDataEventProp, new GUIContent("OnReceivedByteDataEvent"));
                                            GUILayout.EndHorizontal();

                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.PropertyField(OnReceivedStringDataEventProp, new GUIContent("OnReceivedStringDataEvent"));
                                            GUILayout.EndHorizontal();
                                        }

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(GetRawReceivedByteDataEventProp, new GUIContent("GetRawReceivedByteDataEvent"));
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(GetRawReceivedStringDataEventProp, new GUIContent("GetRawReceivedStringDataEvent"));
                                        GUILayout.EndHorizontal();
                                    }
                                    GUILayout.EndVertical();
                                }
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.Space(2);
                    GUILayout.BeginVertical("box");
                    {
                        if (!FMWebSocket.EditorShowDebug)
                        {
                            GUILayout.BeginHorizontal();
                            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                            if (GUILayout.Button("+ Debug")) FMWebSocket.EditorShowDebug = true;
                            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.BeginHorizontal();
                            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                            if (GUILayout.Button("- Debug")) FMWebSocket.EditorShowDebug = false;
                            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            GUILayout.EndHorizontal();

                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(ShowLogProp, new GUIContent("ShowLog"));
                                GUILayout.EndHorizontal();

                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}