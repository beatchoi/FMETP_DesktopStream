using System;
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System.Linq;

namespace FMETP
{
    [CustomEditor(typeof(GameViewEncoder))]
    [CanEditMultipleObjects]
    public class GameViewEncoder_Editor : UnityEditor.Editor
    {
        private GameViewEncoder GVEncoder;

        SerializedProperty CaptureModeProp;

        SerializedProperty ResolutionScalingProp;

        SerializedProperty MainCamProp;
        SerializedProperty RenderCamProp;
        SerializedProperty ResolutionProp;
        SerializedProperty MatchScreenAspectProp;

        SerializedProperty FMDesktopShowAdvancedOptionsProp;
        SerializedProperty FMDesktopFlipXProp;
        SerializedProperty FMDesktopFlipYProp;
        SerializedProperty FMDesktopTargetDisplayProp;

        SerializedProperty FMDesktopRangeXProp;
        SerializedProperty FMDesktopRangeYProp;
        SerializedProperty FMDesktopOffsetXProp;
        SerializedProperty FMDesktopOffsetYProp;
        SerializedProperty FMDesktopRotationAngleProp;

        SerializedProperty FastModeProp;
        SerializedProperty AsyncModeProp;
        SerializedProperty GZipModeProp;
        SerializedProperty EnableAsyncGPUReadbackProp;

        SerializedProperty ColorReductionLevelProp;

        SerializedProperty PanoramaModeProp;
        SerializedProperty CubemapResolutionProp;

        SerializedProperty QualityProp;
        SerializedProperty ChromaSubsamplingProp;

        SerializedProperty StreamFPSProp;

        SerializedProperty ignoreSimilarTextureProp;
        SerializedProperty similarByteSizeThresholdProp;

        SerializedProperty EnableMixedRealityProp;
        SerializedProperty MixedRealityTargetCamIDProp;
        SerializedProperty MixedRealityUseFrontCamProp;
        SerializedProperty MixedRealityRequestResolutionProp;

        SerializedProperty MixedRealityFlipXProp;
        SerializedProperty MixedRealityFlipYProp;
        SerializedProperty MixedRealityScaleXProp;
        SerializedProperty MixedRealityScaleYProp;
        SerializedProperty MixedRealityOffsetXProp;
        SerializedProperty MixedRealityOffsetYProp;

        SerializedProperty PreviewTypeProp;
        SerializedProperty PreviewRawImageProp;
        SerializedProperty PreviewMeshRendererProp;

        SerializedProperty OutputFormatProp;
        SerializedProperty OnDataByteReadyEventProp;
        SerializedProperty OnRawMJPEGReadyEventProp;

        SerializedProperty labelProp;
        SerializedProperty dataLengthProp;

        void OnEnable()
        {
            CaptureModeProp = serializedObject.FindProperty("CaptureMode");

            ResolutionScalingProp = serializedObject.FindProperty("ResolutionScaling");
            MainCamProp = serializedObject.FindProperty("MainCam");
            RenderCamProp = serializedObject.FindProperty("RenderCam");
            ResolutionProp = serializedObject.FindProperty("Resolution");
            MatchScreenAspectProp = serializedObject.FindProperty("MatchScreenAspect");

            FMDesktopShowAdvancedOptionsProp = serializedObject.FindProperty("FMDesktopShowAdvancedOptions");
            FMDesktopFlipXProp = serializedObject.FindProperty("FMDesktopFlipX");
            FMDesktopFlipYProp = serializedObject.FindProperty("FMDesktopFlipY");

            FMDesktopTargetDisplayProp = serializedObject.FindProperty("FMDesktopTargetDisplay");
            FMDesktopRangeXProp = serializedObject.FindProperty("FMDesktopRangeX");
            FMDesktopRangeYProp = serializedObject.FindProperty("FMDesktopRangeY");
            FMDesktopOffsetXProp = serializedObject.FindProperty("FMDesktopOffsetX");
            FMDesktopOffsetYProp = serializedObject.FindProperty("FMDesktopOffsetY");
            FMDesktopRotationAngleProp = serializedObject.FindProperty("FMDesktopRotationAngle");

            FastModeProp = serializedObject.FindProperty("FastMode");
            AsyncModeProp = serializedObject.FindProperty("AsyncMode");
            GZipModeProp = serializedObject.FindProperty("GZipMode");
            EnableAsyncGPUReadbackProp = serializedObject.FindProperty("EnableAsyncGPUReadback");

            ColorReductionLevelProp = serializedObject.FindProperty("ColorReductionLevel");

            PanoramaModeProp = serializedObject.FindProperty("PanoramaMode");
            CubemapResolutionProp = serializedObject.FindProperty("CubemapResolution");

            QualityProp = serializedObject.FindProperty("Quality");
            ChromaSubsamplingProp = serializedObject.FindProperty("ChromaSubsampling");

            StreamFPSProp = serializedObject.FindProperty("StreamFPS");

            ignoreSimilarTextureProp = serializedObject.FindProperty("ignoreSimilarTexture");
            similarByteSizeThresholdProp = serializedObject.FindProperty("similarByteSizeThreshold");

            EnableMixedRealityProp = serializedObject.FindProperty("EnableMixedReality");
            MixedRealityTargetCamIDProp = serializedObject.FindProperty("MixedRealityTargetCamID");
            MixedRealityUseFrontCamProp = serializedObject.FindProperty("MixedRealityUseFrontCam");
            MixedRealityRequestResolutionProp = serializedObject.FindProperty("MixedRealityRequestResolution");

            MixedRealityFlipXProp = serializedObject.FindProperty("MixedRealityFlipX");
            MixedRealityFlipYProp = serializedObject.FindProperty("MixedRealityFlipY");
            MixedRealityScaleXProp = serializedObject.FindProperty("MixedRealityScaleX");
            MixedRealityScaleYProp = serializedObject.FindProperty("MixedRealityScaleY");
            MixedRealityOffsetXProp = serializedObject.FindProperty("MixedRealityOffsetX");
            MixedRealityOffsetYProp = serializedObject.FindProperty("MixedRealityOffsetY");

            PreviewTypeProp = serializedObject.FindProperty("PreviewType");
            PreviewRawImageProp = serializedObject.FindProperty("PreviewRawImage");
            PreviewMeshRendererProp = serializedObject.FindProperty("PreviewMeshRenderer");

            OutputFormatProp = serializedObject.FindProperty("OutputFormat");
            OnDataByteReadyEventProp = serializedObject.FindProperty("OnDataByteReadyEvent");
            OnRawMJPEGReadyEventProp = serializedObject.FindProperty("OnRawMJPEGReadyEvent");

            labelProp = serializedObject.FindProperty("label");
            dataLengthProp = serializedObject.FindProperty("dataLength");
        }

        private void GUILayoutLabel(string inputString, Color inputColor)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = inputColor;
            GUILayout.Label(inputString, style);
        }

        private void Action_SetSymbol()
        {
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();

            //remove FMCOLOR symbol
            for (int i = 0; i < allDefines.Count; i++)
            {
                if (allDefines[i].Contains("FMETP_URP")) allDefines.RemoveAt(i);
            }

            List<string> newDefines = new List<string>();
            for (int i = 0; i < allDefines.Count; i++)
            {
                for (int j = 0; j < newDefines.Count; j++)
                {
                    if (allDefines[i] == newDefines[j]) break;
                }
                newDefines.Add(allDefines[i]);
            }

            if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null)
            {
                //found render pipeline
                newDefines.Add("FMETP_URP");
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", newDefines.ToArray()));
        }

        // Update is called once per frame
        public override void OnInspectorGUI()
        {
            if (GVEncoder == null) GVEncoder = (GameViewEncoder)target;

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

                if (!GVEncoder.EditorShowMode)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Mode")) GVEncoder.EditorShowMode = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Mode")) GVEncoder.EditorShowMode = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(CaptureModeProp, new GUIContent("Capture Mode"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        switch (GVEncoder.CaptureMode)
                        {
                            case GameViewCaptureMode.MainCam: GUILayoutLabel("* Capture camera with screen aspect", Color.yellow); break;
                            case GameViewCaptureMode.RenderCam: GUILayoutLabel("* Render texture with free aspect", Color.yellow); break;
                            case GameViewCaptureMode.FullScreen: GUILayoutLabel("* Capture full screen with UI Canvas", Color.yellow); break;
                            case GameViewCaptureMode.Desktop:
                                if (Application.platform == RuntimePlatform.WindowsEditor
                                    || Application.platform == RuntimePlatform.OSXEditor
                                    || Application.platform == RuntimePlatform.LinuxEditor
                                    || Application.platform == RuntimePlatform.WindowsPlayer
                                    || Application.platform == RuntimePlatform.OSXPlayer
                                    || Application.platform == RuntimePlatform.LinuxPlayer)
                                {
                                    GUILayoutLabel("* Capture System OS Desktop(Windows, Mac OS)", Color.yellow);
                                }
                                else
                                {
                                    GUILayoutLabel("* Capture System OS Desktop(Windows, Mac OS only)", Color.red);
                                }
                                break;
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    if (GVEncoder.CaptureMode == GameViewCaptureMode.Desktop)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(FMDesktopShowAdvancedOptionsProp, new GUIContent("Advanced Options"));
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();

                        if (GVEncoder.FMDesktopShowAdvancedOptions)
                        {
                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(FMDesktopTargetDisplayProp, new GUIContent("Target Display" + "(Count: " + GVEncoder.FMDesktopMonitorCount + ")"));
                                GUILayout.EndHorizontal();

                                if (GVEncoder.FMDesktopFrameWidth > 0 && GVEncoder.FMDesktopFrameHeight > 0)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label("= System Offset (" + GVEncoder.FMDesktopMonitorOffsetX + ", " + GVEncoder.FMDesktopMonitorOffsetY + ") Resolution (" + GVEncoder.FMDesktopFrameWidth + ", " + GVEncoder.FMDesktopFrameHeight + ")");
                                    GUILayout.EndHorizontal();
                                }

                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(FMDesktopFlipXProp, new GUIContent("Flip X"));
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(FMDesktopFlipYProp, new GUIContent("Flip Y"));
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(FMDesktopRangeXProp, new GUIContent("Range X"));
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(FMDesktopRangeYProp, new GUIContent("Range Y"));
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(FMDesktopOffsetXProp, new GUIContent("Offset X"));
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(FMDesktopOffsetYProp, new GUIContent("Offset Y"));
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(FMDesktopRotationAngleProp, new GUIContent("Rotation Angle"));
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();
                        }
                    }

                    if (GVEncoder.CaptureMode == GameViewCaptureMode.MainCam)
                    {
                        //Add symbol for render pipeline
                        Action_SetSymbol();

                        if (GVEncoder.MainCam == null)
                        {
                            if (GVEncoder.MainCam == null) GVEncoder.MainCam = GVEncoder.gameObject.GetComponent<Camera>();
                            if (GVEncoder.MainCam == null) GVEncoder.MainCam = GVEncoder.gameObject.AddComponent<Camera>();
                        }
                        else
                        {
                            if (GVEncoder.MainCam != GVEncoder.gameObject.GetComponent<Camera>()) GVEncoder.MainCam = null;

                            if (GVEncoder.MainCam == null) GVEncoder.MainCam = GVEncoder.gameObject.GetComponent<Camera>();
                            if (GVEncoder.MainCam == null) GVEncoder.MainCam = GVEncoder.gameObject.AddComponent<Camera>();
                        }

                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(EnableMixedRealityProp, new GUIContent(" Enable Webcam for Mixed Reality"));
                            GUILayout.EndHorizontal();

                            if (GVEncoder.EnableMixedReality)
                            {
                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(MixedRealityTargetCamIDProp, new GUIContent(" Target Cam ID"));
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(MixedRealityUseFrontCamProp, new GUIContent(" Use Front Cam"));
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(MixedRealityRequestResolutionProp, new GUIContent(" Request Resolution"));
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.EndVertical();

                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(MixedRealityScaleXProp, new GUIContent(" Scale X"));
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(MixedRealityScaleYProp, new GUIContent(" Scale Y"));
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(MixedRealityOffsetXProp, new GUIContent(" Offset X"));
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(MixedRealityOffsetYProp, new GUIContent(" Offset Y"));
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(MixedRealityFlipXProp, new GUIContent(" Flip X"));
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(MixedRealityFlipYProp, new GUIContent(" Flip Y"));
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.EndVertical();

                                if (GVEncoder.WebcamTexture != null)
                                {
                                    GUILayout.Label("Preview " + GVEncoder.WebcamTexture.GetType().ToString() + " ( " + GVEncoder.WebcamTexture.width + " x " + GVEncoder.WebcamTexture.height + " ) ");
                                }
                                else
                                {
                                    GUILayout.Label("Preview (Empty)");
                                }

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
                                    if (GVEncoder.WebcamTexture != null)
                                    {
                                        GUI.DrawTexture(r, GVEncoder.WebcamTexture, ScaleMode.ScaleToFit);
                                    }
                                }
                                GUILayout.EndVertical();
                            }
                        }
                        GUILayout.EndVertical();
                    }
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!GVEncoder.EditorShowSettings)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Settings")) GVEncoder.EditorShowSettings = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Settings")) GVEncoder.EditorShowSettings = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    if (GVEncoder.CaptureMode == GameViewCaptureMode.MainCam)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(MainCamProp, new GUIContent("Main Cam"));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(ResolutionScalingProp, new GUIContent("ResolutionScaling"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                    else if (GVEncoder.CaptureMode == GameViewCaptureMode.RenderCam)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(RenderCamProp, new GUIContent("Render Cam"));
                            GUILayout.EndHorizontal();

                            if (GVEncoder.RenderCam == null)
                            {
                                //GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayoutLabel("* Render Camera cannot be null", Color.red);
                                    GUILayout.EndHorizontal();

                                }
                                //GUILayout.EndVertical();
                            }

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(ResolutionProp, new GUIContent("Resolution"));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(MatchScreenAspectProp, new GUIContent("MatchScreenAspect"));
                            GUILayout.EndHorizontal();

                            GUILayout.Space(2);
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(PanoramaModeProp, new GUIContent("Panorama Mode", "Render 360 view as Panorama"));
                            GUILayout.EndHorizontal();

                            if (GVEncoder.PanoramaMode)
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(CubemapResolutionProp, new GUIContent("Cubemap Sampling"));
                                GUILayout.EndHorizontal();
                            }
                        }
                        GUILayout.EndVertical();
                    }
                    else if (GVEncoder.CaptureMode == GameViewCaptureMode.FullScreen)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(ResolutionScalingProp, new GUIContent("Resolution ResolutionScaling"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                    else if (GVEncoder.CaptureMode == GameViewCaptureMode.Desktop)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(ResolutionProp, new GUIContent("Resolution"));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(MatchScreenAspectProp, new GUIContent("MatchScreenAspect"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }

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

                        {
                            GUILayout.BeginHorizontal();
                            GUIStyle style = new GUIStyle();
                            style.normal.textColor = Color.yellow;
                            GUILayout.Label("* Experiment for Mac, Windows, Android (Forced Enabled on iOS)", style);
                            GUILayout.EndHorizontal();
                        }

                        if (GVEncoder.FastMode)
                        {
                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(AsyncModeProp, new GUIContent("Async Encode (multi-threading)"));
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                GUILayoutLabel(" Async GPU Readback (" + (GVEncoder.SupportsAsyncGPUReadback ? "Supported" : "Unknown or Not Supported") + ")", GVEncoder.SupportsAsyncGPUReadback ? Color.green : Color.gray);
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(EnableAsyncGPUReadbackProp, new GUIContent("Enabled When Supported"));
                                GUILayout.EndHorizontal();

                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(ChromaSubsamplingProp, new GUIContent("Chroma Subsampling"));
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();
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

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(ColorReductionLevelProp, new GUIContent("ColorReductionLevel"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayoutLabel("* Experiment feature: Reduce network traffic", Color.yellow);
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!GVEncoder.EditorShowNetworking)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Networking")) GVEncoder.EditorShowNetworking = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Networking")) GVEncoder.EditorShowNetworking = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(ignoreSimilarTextureProp, new GUIContent("Ignore Similar Texture"));
                        GUILayout.EndHorizontal();

                        if (GVEncoder.ignoreSimilarTexture)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(similarByteSizeThresholdProp, new GUIContent("similar Byte Size Threshold"));
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
                if (!GVEncoder.EditorShowEncoded)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Encoded")) GVEncoder.EditorShowEncoded = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Encoded")) GVEncoder.EditorShowEncoded = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    if (GVEncoder.GetStreamTexture != null)
                    {
                        GUILayout.Label("Preview " + GVEncoder.GetStreamTexture.GetType().ToString() + " ( " + GVEncoder.GetStreamTexture.width + " x " + GVEncoder.GetStreamTexture.height + " ) ");
                    }
                    else
                    {
                        GUILayout.Label("Preview (Empty)");
                    }
                   
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
                        if (GVEncoder.GetStreamTexture != null)
                        {
                            GUI.DrawTexture(r, GVEncoder.GetStreamTexture, ScaleMode.ScaleToFit);
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(PreviewTypeProp, new GUIContent("Preview Type"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        switch (GVEncoder.PreviewType)
                        {
                            case GameViewPreviewType.None: break;
                            case GameViewPreviewType.RawImage: EditorGUILayout.PropertyField(PreviewRawImageProp, new GUIContent("- RawImage")); break;
                            case GameViewPreviewType.MeshRenderer: EditorGUILayout.PropertyField(PreviewMeshRendererProp, new GUIContent("- MeshRenderer")); break;
                        }
                        GUILayout.EndHorizontal();
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
                        if (GVEncoder.OutputFormat == GameViewOutputFormat.FMMJPEG)
                        {
                            EditorGUILayout.PropertyField(OnDataByteReadyEventProp, new GUIContent("OnDataByteReadyEvent"));
                        }
                        else if (GVEncoder.OutputFormat == GameViewOutputFormat.MJPEG)
                        {
                            EditorGUILayout.PropertyField(OnRawMJPEGReadyEventProp, new GUIContent("OnRawMJPEGReadyEvent"));
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();


            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!GVEncoder.EditorShowPairing)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Pair Encoder & Decoder ")) GVEncoder.EditorShowPairing = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Pair Encoder & Decoder ")) GVEncoder.EditorShowPairing = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        if (GVEncoder.OutputFormat == GameViewOutputFormat.FMMJPEG)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(labelProp, new GUIContent("label"));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(dataLengthProp, new GUIContent("Encoded Size(byte)"));
                            GUILayout.EndHorizontal();
                        }
                        else if (GVEncoder.OutputFormat == GameViewOutputFormat.MJPEG)
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