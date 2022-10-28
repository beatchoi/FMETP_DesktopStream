using System;
using UnityEngine;
using UnityEditor;

namespace FMETP
{
    [CustomEditor(typeof(GameViewDecoder))]
    [CanEditMultipleObjects]
    public class GameViewDecoder_Editor : UnityEditor.Editor
    {
        private GameViewDecoder GVDecoder;


        SerializedProperty FastModeProp;
        SerializedProperty AsyncModeProp;
        SerializedProperty DecoderDelayProp;

        SerializedProperty MonoProp;
        SerializedProperty SharpenProp;
        SerializedProperty DeNoiseProp;

        SerializedProperty DecodedFilterModeProp;
        SerializedProperty DecodedWrapModeProp;

        SerializedProperty OnReceivedTextureEventProp;
        SerializedProperty OnReceivedDesktopFrameRectEventProp;

        SerializedProperty PreviewTypeProp;
        SerializedProperty PreviewRawImageProp;
        SerializedProperty PreviewMeshRendererProp;

        SerializedProperty labelProp;

        void OnEnable()
        {

            FastModeProp = serializedObject.FindProperty("FastMode");
            AsyncModeProp = serializedObject.FindProperty("AsyncMode");
            DecoderDelayProp = serializedObject.FindProperty("DecoderDelay");

            MonoProp = serializedObject.FindProperty("Mono");
            SharpenProp = serializedObject.FindProperty("Sharpen");
            DeNoiseProp = serializedObject.FindProperty("DeNoise");

            DecodedFilterModeProp = serializedObject.FindProperty("DecodedFilterMode");
            DecodedWrapModeProp = serializedObject.FindProperty("DecodedWrapMode");

            OnReceivedTextureEventProp = serializedObject.FindProperty("OnReceivedTextureEvent");
            OnReceivedDesktopFrameRectEventProp = serializedObject.FindProperty("OnReceivedDesktopFrameRectEvent");

            PreviewTypeProp = serializedObject.FindProperty("PreviewType");
            PreviewRawImageProp = serializedObject.FindProperty("PreviewRawImage");
            PreviewMeshRendererProp = serializedObject.FindProperty("PreviewMeshRenderer");

            labelProp = serializedObject.FindProperty("label");
        }

        // Update is called once per frame
        public override void OnInspectorGUI()
        {
            if (GVDecoder == null) GVDecoder = (GameViewDecoder)target;

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

                if (!GVDecoder.EditorShowSettings)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Settings")) GVDecoder.EditorShowSettings = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Settings")) GVDecoder.EditorShowSettings = false;
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

                        if (GVDecoder.FastMode)
                        {
                            //GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(AsyncModeProp, new GUIContent("Async Decode (multi-threading)"));
                                GUILayout.EndHorizontal();
                            }
                            //GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(DecoderDelayProp, new GUIContent("Delay Decode (sec)"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                if (!GVDecoder.EditorShowDecoded)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Decoded")) GVDecoder.EditorShowDecoded = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Decoded")) GVDecoder.EditorShowDecoded = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(MonoProp, new GUIContent("Mono"));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(DecodedFilterModeProp, new GUIContent("Filter Mode"));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(DecodedWrapModeProp, new GUIContent("Wrap Mode"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(SharpenProp, new GUIContent("Sharpen"));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(DeNoiseProp, new GUIContent("DeNoise"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.BeginVertical("box");
                {
                    if (GVDecoder.ReceivedTexture != null)
                    {
                        GUILayout.Label("Preview " + " ( " + GVDecoder.ReceivedTexture.width + " x " + GVDecoder.ReceivedTexture.height + " ) ");
                    }
                    else
                    {
                        GUILayout.Label("Preview (Empty)");
                    }

                    const float maxLogoWidth = 430.0f;
                    EditorGUILayout.Separator();
                    float w = EditorGUIUtility.currentViewWidth;
                    Rect r = new Rect();
                    r.width = Math.Min(w - 40.0f, maxLogoWidth);
                    r.height = r.width / 4.886f;
                    Rect r2 = GUILayoutUtility.GetRect(r.width, r.height);
                    r.x = r2.x;
                    r.y = r2.y;
                    if (GVDecoder.ReceivedTexture != null)
                    {
                        GUI.DrawTexture(r, GVDecoder.ReceivedTexture, ScaleMode.ScaleToFit);
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
                    EditorGUILayout.PropertyField(OnReceivedTextureEventProp, new GUIContent("OnReceivedTextureEvent"));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(PreviewTypeProp, new GUIContent("Preview Type"));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    switch (GVDecoder.PreviewType)
                    {
                        case GameViewPreviewType.None: break;
                        case GameViewPreviewType.RawImage: EditorGUILayout.PropertyField(PreviewRawImageProp, new GUIContent("- RawImage")); break;
                        case GameViewPreviewType.MeshRenderer: EditorGUILayout.PropertyField(PreviewMeshRendererProp, new GUIContent("- MeshRenderer")); break;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                if (!GVDecoder.EditorShowDesktopFrameInfo)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Remote Desktop (info)")) GVDecoder.EditorShowDesktopFrameInfo = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Remote Desktop (info)")) GVDecoder.EditorShowDesktopFrameInfo = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        if (GVDecoder.IsDesktopFrame)
                        {
                            {
                                GUILayout.BeginHorizontal();
                                GUIStyle style = new GUIStyle();
                                style.normal.textColor = Color.green;
                                GUILayout.Label("= Remote Desktop Frame: Yes", style);
                                GUILayout.EndHorizontal();
                            }

                            {
                                GUILayout.BeginHorizontal();
                                GUIStyle style = new GUIStyle();
                                style.normal.textColor = Color.white;
                                GUILayout.Label("= Offset (" + GVDecoder.GetFMDesktopFrameRect.x + ", " + GVDecoder.GetFMDesktopFrameRect.y + ") Resolution (" + GVDecoder.GetFMDesktopFrameRect.width + ", " + GVDecoder.GetFMDesktopFrameRect.height + ")", style);
                                GUILayout.EndHorizontal();
                            }
                        }
                        else
                        {
                            {
                                GUILayout.BeginHorizontal();
                                GUIStyle style = new GUIStyle();
                                style.normal.textColor = Color.red;
                                GUILayout.Label("= Remote Desktop Frame: Unknown / Not Detected", style);
                                GUILayout.EndHorizontal();
                            }

                            {
                                GUILayout.BeginHorizontal();
                                GUIStyle style = new GUIStyle();
                                style.normal.textColor = Color.grey;
                                GUILayout.Label("= Offset (" + GVDecoder.GetFMDesktopFrameRect.x + ", " + GVDecoder.GetFMDesktopFrameRect.y + ") Resolution (" + GVDecoder.GetFMDesktopFrameRect.width + ", " + GVDecoder.GetFMDesktopFrameRect.height + ")", style);
                                GUILayout.EndHorizontal();
                            }
                        }

                        {
                            GUILayout.BeginHorizontal();
                            GUIStyle style = new GUIStyle();
                            style.normal.textColor = Color.yellow;
                            GUILayout.Label("* Will be invoked when the Remote Desktop Frame is detected", style);
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(OnReceivedDesktopFrameRectEventProp, new GUIContent("OnReceivedDesktopFrameRectEvent"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!GVDecoder.EditorShowPairing)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Pair Encoder & Decoder")) GVDecoder.EditorShowPairing = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Pair Encoder & Decoder")) GVDecoder.EditorShowPairing = false;
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