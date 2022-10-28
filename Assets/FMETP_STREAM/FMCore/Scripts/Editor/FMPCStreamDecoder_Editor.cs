using System;
using UnityEngine;
using UnityEditor;

namespace FMETP
{
    [CustomEditor(typeof(FMPCStreamDecoder))]
    [CanEditMultipleObjects]
    public class FMPCStreamDecoder_Editor : UnityEditor.Editor
    {
        private FMPCStreamDecoder FMPCDecoder;

        SerializedProperty FastModeProp;
        SerializedProperty AsyncModeProp;

        SerializedProperty MainColorProp;
        SerializedProperty PointSizeProp;
        SerializedProperty ApplyDistanceProp;

        SerializedProperty labelProp;

        void OnEnable()
        {
            FastModeProp = serializedObject.FindProperty("FastMode");
            AsyncModeProp = serializedObject.FindProperty("AsyncMode");

            MainColorProp = serializedObject.FindProperty("MainColor");
            PointSizeProp = serializedObject.FindProperty("PointSize");
            ApplyDistanceProp = serializedObject.FindProperty("ApplyDistance");

            labelProp = serializedObject.FindProperty("label");
        }

        // Update is called once per frame
        public override void OnInspectorGUI()
        {
            if (FMPCDecoder == null) FMPCDecoder = (FMPCStreamDecoder)target;

            serializedObject.Update();
            GUILayout.Space(10);
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
                    GUILayout.Label("(( FMETP STREAM CORE V3 ))", style);
                    GUILayout.EndHorizontal();
                }

                if (!FMPCDecoder.EditorShowSettings)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Settings")) FMPCDecoder.EditorShowSettings = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Settings")) FMPCDecoder.EditorShowSettings = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(FastModeProp, new GUIContent("Fast Decode Mode"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUIStyle style = new GUIStyle();
                        style.normal.textColor = Color.yellow;
                        GUILayout.Label("* Experiment for Mac, Windows, Android (Forced Enabled on iOS)", style);
                        GUILayout.EndHorizontal();

                        if (FMPCDecoder.FastMode)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(AsyncModeProp, new GUIContent("Async Decode (multi-threading)"));
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                if (!FMPCDecoder.EditorShowDecoded)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Decoded")) FMPCDecoder.EditorShowDecoded = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Decoded")) FMPCDecoder.EditorShowDecoded = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Total Point Count: " + (Application.isPlaying ? FMPCDecoder.PCCount.ToString() : "null"));

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(MainColorProp, new GUIContent("Main Color"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(PointSizeProp, new GUIContent("Point Size"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(ApplyDistanceProp, new GUIContent("Apply Distance"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            {
                if (!FMPCDecoder.EditorShowPairing)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Pair Encoder & Decoder ")) FMPCDecoder.EditorShowPairing = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Pair Encoder & Decoder ")) FMPCDecoder.EditorShowPairing = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(labelProp, new GUIContent("label"));
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