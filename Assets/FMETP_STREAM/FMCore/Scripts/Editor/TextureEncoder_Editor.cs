using System;
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System.Linq;

namespace FMETP
{
    [CustomEditor(typeof(TextureEncoder))]
    [CanEditMultipleObjects]
    public class TextureEncoder_Editor : UnityEditor.Editor
    {
        private TextureEncoder TEncoder;

        SerializedProperty TextureTypeProp;

        SerializedProperty StreamTextureProp;
        SerializedProperty StreamRenderTextureProp;
        SerializedProperty ResolutionScalingProp;

        SerializedProperty FastModeProp;
        SerializedProperty AsyncModeProp;
        SerializedProperty GZipModeProp;
        SerializedProperty EnableAsyncGPUReadbackProp;

        SerializedProperty QualityProp;
        SerializedProperty ChromaSubsamplingProp;

        SerializedProperty StreamFPSProp;

        SerializedProperty ignoreSimilarTextureProp;
        SerializedProperty similarByteSizeThresholdProp;

        SerializedProperty OutputFormatProp;
        SerializedProperty OnDataByteReadyEventProp;
        SerializedProperty OnRawMJPEGReadyEventProp;

        SerializedProperty labelProp;
        SerializedProperty dataLengthProp;

        void OnEnable()
        {
            TextureTypeProp = serializedObject.FindProperty("TextureType");

            StreamTextureProp = serializedObject.FindProperty("StreamTexture");
            StreamRenderTextureProp = serializedObject.FindProperty("StreamRenderTexture");

            ResolutionScalingProp = serializedObject.FindProperty("ResolutionScaling");

            FastModeProp = serializedObject.FindProperty("FastMode");
            AsyncModeProp = serializedObject.FindProperty("AsyncMode");
            GZipModeProp = serializedObject.FindProperty("GZipMode");
            EnableAsyncGPUReadbackProp = serializedObject.FindProperty("EnableAsyncGPUReadback");

            QualityProp = serializedObject.FindProperty("Quality");
            ChromaSubsamplingProp = serializedObject.FindProperty("ChromaSubsampling");

            StreamFPSProp = serializedObject.FindProperty("StreamFPS");

            ignoreSimilarTextureProp = serializedObject.FindProperty("ignoreSimilarTexture");
            similarByteSizeThresholdProp = serializedObject.FindProperty("similarByteSizeThreshold");

            OutputFormatProp = serializedObject.FindProperty("OutputFormat");
            OnDataByteReadyEventProp = serializedObject.FindProperty("OnDataByteReadyEvent");
            OnRawMJPEGReadyEventProp = serializedObject.FindProperty("OnRawMJPEGReadyEvent");

            labelProp = serializedObject.FindProperty("label");
            dataLengthProp = serializedObject.FindProperty("dataLength");
        }

        // Update is called once per frame
        public override void OnInspectorGUI()
        {
            if (TEncoder == null) TEncoder = (TextureEncoder)target;

            serializedObject.Update();

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

                if (!TEncoder.EditorShowMode)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Mode")) TEncoder.EditorShowMode = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Mode")) TEncoder.EditorShowMode = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(TextureTypeProp, new GUIContent("Texture Type"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    if (TEncoder.TextureType == FMTextureType.Texture2D)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(StreamTextureProp, new GUIContent("Stream Texture"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                    else if (TEncoder.TextureType == FMTextureType.RenderTexture)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(StreamRenderTextureProp, new GUIContent("Stream Render Texture"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                    else if (TEncoder.TextureType == FMTextureType.WebcamTexture)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(ResolutionScalingProp, new GUIContent("Resolution Scaling"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            {
                if (!TEncoder.EditorShowSettings)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Settings")) TEncoder.EditorShowSettings = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Settings")) TEncoder.EditorShowSettings = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("(supported format: RGB24, RGBA32)");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(QualityProp, new GUIContent("Quality"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(StreamFPSProp, new GUIContent("StreamFPS"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(FastModeProp, new GUIContent("Fast Encode Mode"));
                        GUILayout.EndHorizontal();

                        if (TEncoder.FastMode)
                        {
                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(AsyncModeProp, new GUIContent("Async (multi-threading)"));
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                GUIStyle style = new GUIStyle();
                                style.normal.textColor = TEncoder.SupportsAsyncGPUReadback ? Color.green : Color.gray;
                                GUILayout.Label(" Async GPU Readback (" + (TEncoder.SupportsAsyncGPUReadback ? "Supported" : "Unknown or Not Supported") + ")", style);
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(EnableAsyncGPUReadbackProp, new GUIContent("Enabled When Supported"));
                                GUILayout.EndHorizontal();

                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(ChromaSubsamplingProp, new GUIContent("Chroma Subsampling"));
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.EndVertical();
                            }
                            GUILayout.EndVertical();
                        }

                        {
                            GUILayout.BeginHorizontal();
                            GUIStyle style = new GUIStyle();
                            style.normal.textColor = Color.yellow;
                            GUILayout.Label(" Experiment for Mac, Windows, Android (Forced Enabled on iOS)", style);
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(GZipModeProp, new GUIContent("GZip Mode", "Reduce network traffic"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            {
                if (!TEncoder.EditorShowNetworking)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Networking")) TEncoder.EditorShowNetworking = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Networking")) TEncoder.EditorShowNetworking = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(ignoreSimilarTextureProp, new GUIContent("Ignore Similar Texture"));
                        GUILayout.EndHorizontal();

                        if (TEncoder.ignoreSimilarTexture)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(similarByteSizeThresholdProp, new GUIContent("Similar Byte Size Threshold"));
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            {
                if (!TEncoder.EditorShowEncoded)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Encoded")) TEncoder.EditorShowEncoded = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Encoded")) TEncoder.EditorShowEncoded = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    if (TEncoder.GetPreviewTexture != null) { GUILayout.Label("Preview " + " ( " + TEncoder.StreamWidth + " x " + TEncoder.StreamHeight + " ) "); }
                    else { GUILayout.Label("Preview (Empty)"); }
                    GUILayout.BeginVertical("box");
                    {
                        const float maxLogoWidth = 430.0f;
                        EditorGUILayout.Separator();
                        float w = EditorGUIUtility.currentViewWidth;
                        Rect r = new Rect();
                        r.width = Math.Min(w - 40.0f, maxLogoWidth);
                        r.height = r.width / 4.886f;
                        Rect r2 = GUILayoutUtility.GetRect(r.width, r.height);
                        r.x = r2.x;
                        r.y = r2.y;

                        if (TEncoder.GetPreviewTexture != null)
                        {
                            GUI.DrawTexture(r, TEncoder.GetPreviewTexture, ScaleMode.ScaleToFit);
                        }
                        else
                        {
                            //GUI.DrawTexture(r, new Texture2D((int)r.width, (int)r.height, TextureFormat.RGB24, false), ScaleMode.ScaleToFit);
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(OutputFormatProp, new GUIContent("Output Format"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        if (TEncoder.OutputFormat == GameViewOutputFormat.FMMJPEG)
                        {
                            EditorGUILayout.PropertyField(OnDataByteReadyEventProp, new GUIContent("OnDataByteReadyEvent"));
                        }
                        else if (TEncoder.OutputFormat == GameViewOutputFormat.MJPEG)
                        {
                            EditorGUILayout.PropertyField(OnRawMJPEGReadyEventProp, new GUIContent("OnRawMJPEGReadyEvent"));
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            {
                if (!TEncoder.EditorShowPairing)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Pair Encoder & Decoder ")) TEncoder.EditorShowPairing = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Pair Encoder & Decoder ")) TEncoder.EditorShowPairing = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        if (TEncoder.OutputFormat == GameViewOutputFormat.FMMJPEG)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(labelProp, new GUIContent("label"));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(dataLengthProp, new GUIContent("Encoded Size(byte)"));
                            GUILayout.EndHorizontal();
                        }
                        else if (TEncoder.OutputFormat == GameViewOutputFormat.MJPEG)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(labelProp, new GUIContent("label"));
                            GUILayout.EndHorizontal();

                            {
                                GUIStyle style = new GUIStyle();
                                style.normal.textColor = Color.red;

                                GUILayout.BeginHorizontal();
                                GUILayout.Label("(Requires FMMJPEG Output Format) ", style);
                                GUILayout.EndHorizontal();
                            }

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(dataLengthProp, new GUIContent("Encoded Size(byte)"));
                            GUILayout.EndHorizontal();
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