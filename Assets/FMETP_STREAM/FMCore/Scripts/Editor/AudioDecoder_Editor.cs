using System;
using UnityEngine;
using UnityEditor;

namespace FMETP
{
    [CustomEditor(typeof(AudioDecoder))]
    [CanEditMultipleObjects]
    public class AudioDecoder_Editor : UnityEditor.Editor
    {
        private AudioDecoder ADecoder;
        SerializedProperty labelProp;
        SerializedProperty volumeProp;

        SerializedProperty SourceFormatDectionProp;
        SerializedProperty SourceSampleRateProp;
        SerializedProperty SourceChannelsProp;

        SerializedProperty OnPCMFloatReadyEventProp;

        void OnEnable()
        {
            labelProp = serializedObject.FindProperty("label");
            volumeProp = serializedObject.FindProperty("volume");

            SourceFormatDectionProp = serializedObject.FindProperty("SourceFormatDection");
            SourceSampleRateProp = serializedObject.FindProperty("SourceSampleRate");
            SourceChannelsProp = serializedObject.FindProperty("SourceChannels");

            OnPCMFloatReadyEventProp = serializedObject.FindProperty("OnPCMFloatReadyEvent");
        }

        // Update is called once per frame
        public override void OnInspectorGUI()
        {
            if (ADecoder == null) ADecoder = (AudioDecoder)target;

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
                    GUILayout.Label("(( FMETP STREAM CORE V3 ))", style);
                    GUILayout.EndHorizontal();
                }

                if (!ADecoder.EditorShowPlayback)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Playback")) ADecoder.EditorShowPlayback = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Playback")) ADecoder.EditorShowPlayback = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(volumeProp, new GUIContent("Volume"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }

                if (!ADecoder.EditorShowAudioInfo)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Audio Info")) ADecoder.EditorShowAudioInfo = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Audio Info")) ADecoder.EditorShowAudioInfo = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Device Sample Rate: " + ADecoder.DeviceSampleRate);
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(SourceFormatDectionProp, new GUIContent(" Source Format Dection"));
                        GUILayout.EndHorizontal();

                        if (ADecoder.SourceFormatDection == AudioDecoderSourceFormat.Auto)
                        {
                            {
                                GUIStyle style = new GUIStyle();
                                style.normal.textColor = Color.yellow;

                                GUILayout.BeginHorizontal();
                                GUILayout.Label(" * Requires FMPCM16 Output Format from Audio/Mic Encoder", style);
                                GUILayout.EndHorizontal();
                            }

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Source Sample Rate: " + ADecoder.SourceSampleRate);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Source Channels: " + ADecoder.SourceChannels);
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(SourceSampleRateProp, new GUIContent(" Source Sample Rate"));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(SourceChannelsProp, new GUIContent(" Source Channels"));
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!ADecoder.EditorShowDecoded)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Decoded")) ADecoder.EditorShowDecoded = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Decoded")) ADecoder.EditorShowDecoded = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(OnPCMFloatReadyEventProp, new GUIContent("OnPCMFloatReadyEvent"));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!ADecoder.EditorShowPairing)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Pair Encoder & Decoder")) ADecoder.EditorShowPairing = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Pair Encoder & Decoder ")) ADecoder.EditorShowPairing = false;
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