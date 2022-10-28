using System;
using UnityEngine;
using UnityEditor;

namespace FMETP
{
    [CustomEditor(typeof(FMSocketIOManager))]
    [CanEditMultipleObjects]
    public class FMSocketIOManager_Editor : UnityEditor.Editor
    {
        private FMSocketIOManager FMSocketIO;

        SerializedProperty AutoInitProp;
        SerializedProperty DelayInitThresholdProp;
        SerializedProperty NetworkTypeProp;

        SerializedProperty Settings_IPProp;
        SerializedProperty Settings_PortProp;

        SerializedProperty Settings_SslEnabledProp;
        SerializedProperty Settings_SslProtocolsProp;

        SerializedProperty Settings_ReconnectDelayProp;

        SerializedProperty Settings_portRequiredProp;
        SerializedProperty Settings_socketIORequiredProp;

        SerializedProperty Settings_AckExpirationTimeProp;
        SerializedProperty Settings_PingIntervalProp;
        SerializedProperty Settings_PingTimeoutProp;
        SerializedProperty Settings_SocketIDProp;

        SerializedProperty Settings_DefaultQueryStringProp;
        SerializedProperty Settings_CustomisedQueryStringProp;


        SerializedProperty ReadyProp;

        SerializedProperty OnReceivedByteDataEventProp;
        SerializedProperty OnReceivedStringDataEventProp;
        SerializedProperty OnReceivedRawMessageEventProp;

        SerializedProperty DebugModeProp;

        void OnEnable()
        {

            AutoInitProp = serializedObject.FindProperty("AutoInit");
            DelayInitThresholdProp = serializedObject.FindProperty("DelayInitThreshold");
            NetworkTypeProp = serializedObject.FindProperty("NetworkType");

            Settings_IPProp = serializedObject.FindProperty("Settings.IP");
            Settings_PortProp = serializedObject.FindProperty("Settings.port");

            Settings_SslEnabledProp = serializedObject.FindProperty("Settings.sslEnabled");
            Settings_SslProtocolsProp = serializedObject.FindProperty("Settings.sslProtocols");

            Settings_ReconnectDelayProp = serializedObject.FindProperty("Settings.reconnectDelay");

            Settings_portRequiredProp = serializedObject.FindProperty("Settings.portRequired");
            Settings_socketIORequiredProp = serializedObject.FindProperty("Settings.socketIORequired");

            Settings_AckExpirationTimeProp = serializedObject.FindProperty("Settings.ackExpirationTime");
            Settings_PingIntervalProp = serializedObject.FindProperty("Settings.pingInterval");
            Settings_PingTimeoutProp = serializedObject.FindProperty("Settings.pingTimeout");
            Settings_SocketIDProp = serializedObject.FindProperty("Settings.socketID");

            Settings_DefaultQueryStringProp = serializedObject.FindProperty("Settings.DefaultQueryString");
            Settings_CustomisedQueryStringProp = serializedObject.FindProperty("Settings.CustomisedQueryString");

            ReadyProp = serializedObject.FindProperty("Ready");

            OnReceivedByteDataEventProp = serializedObject.FindProperty("OnReceivedByteDataEvent");
            OnReceivedStringDataEventProp = serializedObject.FindProperty("OnReceivedStringDataEvent");
            OnReceivedRawMessageEventProp = serializedObject.FindProperty("OnReceivedRawMessageEvent");

            DebugModeProp = serializedObject.FindProperty("DebugMode");

        }

        // Update is called once per frame
        public override void OnInspectorGUI()
        {
            if (FMSocketIO == null) FMSocketIO = (FMSocketIOManager)target;

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
                    GUILayout.Label("(( FMWebSocket 2.0))", style);
                    GUILayout.EndHorizontal();
                }

                if (!FMSocketIO.EditorShowNetworking)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Networking")) FMSocketIO.EditorShowNetworking = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Networking")) FMSocketIO.EditorShowNetworking = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(AutoInitProp, new GUIContent("Auto Init"));
                        GUILayout.EndHorizontal();

                        if (FMSocketIO.AutoInit)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(DelayInitThresholdProp, new GUIContent("Delay Threshold"));
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(Settings_socketIORequiredProp, new GUIContent("Using SocketIO"));
                        GUILayout.EndHorizontal();

                        if (FMSocketIO.Settings.socketIORequired)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(NetworkTypeProp, new GUIContent("NetworkType"));
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginVertical();
                        {
                            if (!FMSocketIO.EditorShowNetworkSettings)
                            {
                                GUILayout.BeginHorizontal();
                                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                if (GUILayout.Button("= Settings")) FMSocketIO.EditorShowNetworkSettings = true;
                                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                GUILayout.EndHorizontal();
                            }
                            else
                            {
                                GUILayout.BeginHorizontal();
                                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                                if (GUILayout.Button("- Settings")) FMSocketIO.EditorShowNetworkSettings = false;
                                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                                GUILayout.EndHorizontal();

                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    {
                                        string _url = "ws" + (FMSocketIO.Settings.sslEnabled ? "s" : "") + "://" + FMSocketIO.Settings.IP;
                                        if (FMSocketIO.Settings.portRequired) _url += ":" + FMSocketIO.Settings.port;
                                        if (FMSocketIO.Settings.socketIORequired)
                                        {
                                            _url += "/socket.io/";
                                            _url += FMSocketIO.Settings.DefaultQueryString ? "?EIO=3&transport=websocket" : FMSocketIO.Settings.CustomisedQueryString;
                                        }
                                        GUILayout.Label("URL: " + _url);
                                        GUILayout.EndHorizontal();
                                    }

                                    GUILayout.BeginHorizontal();
                                    {
                                        string _url = "ws" + (FMSocketIO.Settings.sslEnabled ? "s" : "") + "://" + FMSocketIO.Settings.IP;
                                        if (FMSocketIO.Settings.portRequired) _url += ":" + FMSocketIO.Settings.port;
                                        if (FMSocketIO.Settings.socketIORequired)
                                        {
                                            _url += "/";
                                            if (!FMSocketIO.Settings.DefaultQueryString) _url += FMSocketIO.Settings.CustomisedQueryString;
                                        }
                                        GUILayout.Label("URL(WebGL): " + _url);
                                        GUILayout.EndHorizontal();
                                    }

                                }
                                GUILayout.EndVertical();

                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(Settings_IPProp, new GUIContent("IP"));
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(Settings_SslEnabledProp, new GUIContent("Ssl Enabled"));
                                    GUILayout.EndHorizontal();

                                    if (FMSocketIO.Settings.sslEnabled)
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_SslProtocolsProp, new GUIContent("Ssl Protocols"));
                                        GUILayout.EndHorizontal();
                                    }
                                }
                                GUILayout.EndVertical();

                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(Settings_PortProp, new GUIContent("Port"));
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(Settings_portRequiredProp, new GUIContent("Port Required"));
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.EndVertical();

                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(Settings_DefaultQueryStringProp, new GUIContent("Default QueryString"));
                                    GUILayout.EndHorizontal();

                                    if (!FMSocketIO.Settings.DefaultQueryString)
                                    {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.PropertyField(Settings_CustomisedQueryStringProp, new GUIContent("Customised QueryString"));
                                        GUILayout.EndHorizontal();
                                    }
                                }
                                GUILayout.EndVertical();


                                GUILayout.BeginVertical("box");
                                {

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(Settings_ReconnectDelayProp, new GUIContent("Reconnect Delay"));
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(Settings_AckExpirationTimeProp, new GUIContent("Ack Expiration Time"));
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(Settings_PingIntervalProp, new GUIContent("Ping Interval"));
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(Settings_PingTimeoutProp, new GUIContent("Ping Timeout"));
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(Settings_SocketIDProp, new GUIContent("Socket ID"));
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.EndVertical();
                            }
                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(ReadyProp, new GUIContent("Ready"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!FMSocketIO.EditorShowEvents)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Events")) FMSocketIO.EditorShowEvents = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Events")) FMSocketIO.EditorShowEvents = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        if (!FMSocketIO.EditorShowReceiverEvents)
                        {
                            GUILayout.BeginHorizontal();
                            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                            if (GUILayout.Button("= Receiver Events")) FMSocketIO.EditorShowReceiverEvents = true;
                            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.BeginHorizontal();
                            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                            if (GUILayout.Button("- Receiver Events")) FMSocketIO.EditorShowReceiverEvents = false;
                            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                            GUILayout.EndHorizontal();

                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(OnReceivedByteDataEventProp, new GUIContent("OnReceivedByteDataEvent"));
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(OnReceivedStringDataEventProp, new GUIContent("OnReceivedStringDataEvent"));
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(OnReceivedRawMessageEventProp, new GUIContent("OnReceivedRawMessageEvent"));
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
                if (!FMSocketIO.EditorShowDebug)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Debug")) FMSocketIO.EditorShowDebug = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Debug")) FMSocketIO.EditorShowDebug = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(DebugModeProp, new GUIContent("Enable"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}