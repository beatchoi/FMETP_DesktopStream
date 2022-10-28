using System.Collections;
using UnityEngine;
using System;

using UnityEngine.Rendering;
using System.Linq;
using System.Collections.Generic;

using UnityEngine.UI;

namespace FMETP
{
    public enum GameViewCaptureMode { RenderCam, MainCam, FullScreen, Desktop }
    public enum GameViewCubemapSample
    {
        High = 2048,
        Medium = 1024,
        Low = 512,
        Minimum = 256
    }
    public enum FMDesktopDisplayID
    {
        Display1 = 0,
        Display2 = 1,
        Display3 = 2,
        Display4 = 3,
        Display5 = 4,
        Display6 = 5,
        Display7 = 6,
        Display8 = 7
    }
    public enum FMDesktopRotationAngle
    {
        Degree0 = 0,
        Degree90 = 90,
        Degree180 = 180,
        Degree270 = 270
    }
    public enum GameViewOutputFormat { FMMJPEG, MJPEG }
    public enum GameViewPreviewType { None, RawImage, MeshRenderer }

    [AddComponentMenu("FMETP/Mapper/GameViewEncoder")]
    public class GameViewEncoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowMode = true;
        public bool EditorShowSettings = true;
        public bool EditorShowNetworking = true;
        public bool EditorShowEncoded = true;
        public bool EditorShowPairing = true;
        #endregion

        public GameViewCaptureMode CaptureMode = GameViewCaptureMode.RenderCam;
        private GameViewCaptureMode _CaptureMode = GameViewCaptureMode.RenderCam;
        [Range(0.05f, 1f)] public float ResolutionScaling = 0.25f;

        public Camera MainCam;
        public Camera RenderCam;

        public Vector2 Resolution = new Vector2(512, 512);
        private Vector2 renderResolution = new Vector2(512, 512);
        public bool MatchScreenAspect = true;

        public bool FastMode = false;
        public bool AsyncMode = false;

        public bool GZipMode = false;
        public bool PanoramaMode = false;

        [Range(10, 100)]
        public int Quality = 40;
        public FMChromaSubsamplingOption ChromaSubsampling = FMChromaSubsamplingOption.Subsampling420;

        [Range(0f, 60f)]
        public float StreamFPS = 20f;
        private float interval = 0.05f;

        public bool ignoreSimilarTexture = true;
        private int lastRawDataByte = 0;
        [Tooltip("Compare previous image data size(byte)")]
        public int similarByteSizeThreshold = 8;

        private bool NeedUpdateTexture = false;
        private bool EncodingTexture = false;

        //experimental feature: check if your GPU supports AsyncReadback
        private bool supportsAsyncGPUReadback = false;
        public bool EnableAsyncGPUReadback = true;
        public bool SupportsAsyncGPUReadback { get { return supportsAsyncGPUReadback; } }

        private int streamWidth;
        private int streamHeight;

        public GameViewPreviewType PreviewType = GameViewPreviewType.None;
        public RawImage PreviewRawImage;
        public MeshRenderer PreviewMeshRenderer;
        public Texture2D CapturedTexture;
        public Texture GetStreamTexture
        {
            get
            {
                if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) return rt;
                return CapturedTexture;
            }
        }
        private RenderTextureDescriptor sourceDescriptor;
        private RenderTexture rt_reserved;//try to reserved existing render texture in RenderCamera Mode
        private bool reservedExistingRenderTexture = false;

        private RenderTexture rt;
        private RenderTexture rt_cube;
        private WebCamTexture webcamTexture;
        public WebCamTexture WebcamTexture { get { return webcamTexture; } set { webcamTexture = value; } }
        public void Action_SetWebcamTexture(WebCamTexture inputWebcamTexture) { webcamTexture = inputWebcamTexture; }

        [HideInInspector] public Material MatPano; //has to be public, otherwise the shader will be  missing
        [HideInInspector] public Material MatFMDesktop;
        [HideInInspector] public Material MatColorAdjustment;
        [HideInInspector] public Material MatMixedReality;

        public bool EnableMixedReality = false;
        private WebcamManager webcamManager;

        public int MixedRealityTargetCamID = 0;
        public bool MixedRealityUseFrontCam = false;
        public Vector2 MixedRealityRequestResolution = new Vector2(1280, 720);

        public bool MixedRealityFlipX = false;
        public bool MixedRealityFlipY = false;
        [Range(0.01f, 2f)] public float MixedRealityScaleX = 1f;
        [Range(0.01f, 2f)] public float MixedRealityScaleY = 1f;
        [Range(-0.5f, 0.5f)] public float MixedRealityOffsetX = 0f;
        [Range(-0.5f, 0.5f)] public float MixedRealityOffsetY = 0f;

        [Range(0, 2)]
        public int ColorReductionLevel = 0;
        private float brightness { get { return 1f / Mathf.Pow(2, ColorReductionLevel); } }

        //for URP only
        [HideInInspector] public Material mat_source;

        public GameViewCubemapSample CubemapResolution = GameViewCubemapSample.Medium;

        private Texture2D Screenshot;
        private ColorSpace ColorSpace;

        public bool FMDesktopShowAdvancedOptions = false;
        private FMDesktopManager fmDesktopManager;

        private Int16 fmDesktopMonitorOffsetX = 0;
        private Int16 fmDesktopMonitorOffsetY = 0;
        private Int16 fmDesktopFrameWidth = 0;
        private Int16 fmDesktopFrameHeight = 0;
        public Int16 FMDesktopMonitorOffsetX { get { return fmDesktopMonitorOffsetX; } }
        public Int16 FMDesktopMonitorOffsetY { get { return fmDesktopMonitorOffsetY; } }
        public Int16 FMDesktopFrameWidth { get { return fmDesktopFrameWidth; } }
        public Int16 FMDesktopFrameHeight { get { return fmDesktopFrameHeight; } }

        public float FMDesktopRotation = 0f;

        public Vector2 FMDesktopResolution = Vector2.zero;
        public bool FMDesktopFlipX = true;
        public bool FMDesktopFlipY = false;

        [Range(0.00001f, 2f)] public float FMDesktopRangeX = 1f;
        [Range(0.00001f, 2f)] public float FMDesktopRangeY = 1f;
        [Range(-0.5f, 0.5f)] public float FMDesktopOffsetX = 0f;
        [Range(-0.5f, 0.5f)] public float FMDesktopOffsetY = 0f;
        public FMDesktopRotationAngle FMDesktopRotationAngle = FMDesktopRotationAngle.Degree0;

        public FMDesktopDisplayID FMDesktopTargetDisplay = FMDesktopDisplayID.Display1;
        public int FMDesktopMonitorID { get { return (int)FMDesktopTargetDisplay; } }
        [Range(0, 8)] public int FMDesktopMonitorCount = 0;

        public GameViewOutputFormat OutputFormat = GameViewOutputFormat.FMMJPEG;
        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();
        public UnityEventByteArray OnRawMJPEGReadyEvent = new UnityEventByteArray();

        //[Header("Pair Encoder & Decoder")]
        public UInt16 label = 1001;
        private UInt16 dataID = 0;
        private UInt16 maxID = 1024;
        private int chunkSize = 1400;//8096; //32768
        private float next = 0f;
        private bool stop = false;
        private byte[] dataByte;
        private byte[] dataByteTemp;
        private Queue<byte[]> AppendQueueSendByteFMMJPEG = new Queue<byte[]>();
        private Queue<byte[]> AppendQueueSendByteMJPEG = new Queue<byte[]>();

        public int dataLength;

        private void CaptureModeUpdate()
        {
#if !UNITY_EDITOR && !UNITY_STANDALONE
            if (CaptureMode == GameViewCaptureMode.Desktop) CaptureMode = GameViewCaptureMode.FullScreen;
#endif
            if (_CaptureMode != CaptureMode)
            {
                _CaptureMode = CaptureMode;
                if (rt != null) Destroy(rt);
                if (CapturedTexture != null) Destroy(CapturedTexture);

                if (fmDesktopManager != null) Destroy(fmDesktopManager);

                AssignMaterials();
            }
        }

        private void AssignMaterials(bool _override = false)
        {
            if (_override)
            {
                MatPano = new Material(Shader.Find("Hidden/FMCubemapToEquirect"));
                MatFMDesktop = new Material(Shader.Find("Hidden/FMDesktopMask"));
                MatColorAdjustment = new Material(Shader.Find("Hidden/FMETPColorAdjustment"));
                MatMixedReality = new Material(Shader.Find("Hidden/FMETPMixedReality"));

#if FMETP_URP
            //for URP only
            mat_source = new Material(Shader.Find("Hidden/FMETPMainCamURP"));
#endif
            }
            else
            {
                if (MatPano == null) MatPano = new Material(Shader.Find("Hidden/FMCubemapToEquirect"));
                if (MatFMDesktop == null) MatFMDesktop = new Material(Shader.Find("Hidden/FMDesktopMask"));
                if (MatColorAdjustment == null) MatColorAdjustment = new Material(Shader.Find("Hidden/FMETPColorAdjustment"));
                if (MatMixedReality == null) MatMixedReality = new Material(Shader.Find("Hidden/FMETPMixedReality"));

#if FMETP_URP
            //for URP only
            if(mat_source == null) mat_source = new Material(Shader.Find("Hidden/FMETPMainCamURP"));
#endif
            }
        }

        //init when added component, or reset component
        private void Reset()
        {
            AssignMaterials(true);
        }

        private void Start()
        {
            Application.runInBackground = true;
            ColorSpace = QualitySettings.activeColorSpace;

#if UNITY_2018_2_OR_NEWER
            try { supportsAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback; }
            catch { supportsAsyncGPUReadback = false; }
#else
        supportsAsyncGPUReadback = false;
#endif

            sourceDescriptor = (UnityEngine.XR.XRSettings.enabled) ? UnityEngine.XR.XRSettings.eyeTextureDesc : new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32);
            sourceDescriptor.depthBufferBits = 24;

#if WINDOWS_UWP
        if (supportsAsyncGPUReadback && EnableAsyncGPUReadback && FastMode) sourceDescriptor.colorFormat = RenderTextureFormat.ARGB32;
#endif

            if (RenderCam != null)
            {
                if (RenderCam.targetTexture != null)
                {
                    rt_reserved = RenderCam.targetTexture;
                    reservedExistingRenderTexture = true;
                }
                else
                {
                    reservedExistingRenderTexture = false;
                }
            }

            CaptureModeUpdate();
            StartCoroutine(SenderCOR());
        }

        private void Update()
        {
            Resolution.x = Mathf.RoundToInt(Resolution.x);
            Resolution.y = Mathf.RoundToInt(Resolution.y);
            if (Resolution.x <= 1) Resolution.x = 1;
            if (Resolution.y <= 1) Resolution.y = 1;
            renderResolution = Resolution;

            CaptureModeUpdate();

            switch (_CaptureMode)
            {
                case GameViewCaptureMode.MainCam:
                    if (MainCam == null) MainCam = this.GetComponent<Camera>();
                    if (!EncodingTexture) renderResolution = new Vector2(Screen.width, Screen.height) * ResolutionScaling;
                    if (sourceDescriptor.vrUsage == VRTextureUsage.TwoEyes) renderResolution.x /= 2f;

                    if (EnableMixedReality)
                    {
                        if (webcamManager == null)
                        {
                            webcamManager = this.gameObject.AddComponent<WebcamManager>();
                            webcamManager.hideFlags = HideFlags.HideInInspector;

                            webcamManager.TargetCamID = MixedRealityTargetCamID;
                            webcamManager.useFrontCam = MixedRealityUseFrontCam;
                            webcamManager.requestResolution = MixedRealityRequestResolution;

                            webcamManager.useFrontCam = false;
                            webcamManager.OnWebcamTextureReady.AddListener(Action_SetWebcamTexture);

                            MainCam.clearFlags = CameraClearFlags.SolidColor;
                            MainCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                        }
                    }
                    else
                    {
                        if (webcamManager != null)
                        {
                            Destroy(webcamManager);
                            webcamManager = null;
                        }
                    }
                    break;
                case GameViewCaptureMode.RenderCam:
                    if (MatchScreenAspect)
                    {
                        if (Screen.width > Screen.height) renderResolution.y = renderResolution.x / (float)(Screen.width) * (float)(Screen.height);
                        if (Screen.width < Screen.height) renderResolution.x = renderResolution.y / (float)(Screen.height) * (float)(Screen.width);
                    }
                    break;
                case GameViewCaptureMode.FullScreen:
                    if (!EncodingTexture) renderResolution = new Vector2(Screen.width, Screen.height) * ResolutionScaling;
                    break;
                case GameViewCaptureMode.Desktop:
                    if (MatchScreenAspect)
                    {
                        if (Screen.width > Screen.height) renderResolution.y = renderResolution.x / (float)(Screen.width) * (float)(Screen.height);
                        if (Screen.width < Screen.height) renderResolution.x = renderResolution.y / (float)(Screen.height) * (float)(Screen.width);
                    }
                    break;
            }

            if (_CaptureMode != GameViewCaptureMode.RenderCam)
            {
                if (RenderCam != null)
                {
                    if (RenderCam.targetTexture != null) RenderCam.targetTexture = null;
                }
            }

            switch (PreviewType)
            {
                case GameViewPreviewType.None: break;
                case GameViewPreviewType.RawImage: PreviewRawImage.texture = GetStreamTexture; break;
                case GameViewPreviewType.MeshRenderer: PreviewMeshRenderer.material.mainTexture = GetStreamTexture; break;
            }
        }

        private void CheckResolution()
        {
            if (renderResolution.x <= 1) renderResolution.x = 1;
            if (renderResolution.y <= 1) renderResolution.y = 1;

            bool IsLinear = (ColorSpace == ColorSpace.Linear) && (CaptureMode == GameViewCaptureMode.FullScreen);

            sourceDescriptor.width = Mathf.RoundToInt(renderResolution.x);
            sourceDescriptor.height = Mathf.RoundToInt(renderResolution.y);
            sourceDescriptor.sRGB = !IsLinear;

            if (PanoramaMode && CaptureMode == GameViewCaptureMode.RenderCam)
            {
                if (rt_cube == null)
                {
                    rt_cube = new RenderTexture((int)CubemapResolution, (int)CubemapResolution, 0, RenderTextureFormat.ARGB32, IsLinear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
                    //rt_cube.Create();
                }
                else
                {
                    if (rt_cube.width != (int)CubemapResolution || rt_cube.height != (int)CubemapResolution || rt_cube.sRGB != IsLinear)
                    {
                        if (MainCam != null) { if (MainCam.targetTexture == rt_cube) MainCam.targetTexture = null; }
                        if (RenderCam != null) { if (RenderCam.targetTexture == rt_cube) RenderCam.targetTexture = null; }
                        Destroy(rt_cube);
                        rt_cube = new RenderTexture((int)CubemapResolution, (int)CubemapResolution, 0, RenderTextureFormat.ARGB32, IsLinear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
                        //rt_cube.Create();
                    }
                }

                rt_cube.antiAliasing = 1;
                rt_cube.filterMode = FilterMode.Bilinear;
                rt_cube.anisoLevel = 0;
                rt_cube.dimension = TextureDimension.Cube;
                rt_cube.autoGenerateMips = false;
            }


            if (rt == null)
            {
                //may have unsupport graphic format bug on Unity2019/2018, fallback to not using descriptor
                try { rt = new RenderTexture(sourceDescriptor); }
                catch
                {
                    DestroyImmediate(rt);
                    rt = new RenderTexture(sourceDescriptor.width, sourceDescriptor.height, sourceDescriptor.depthBufferBits, RenderTextureFormat.ARGB32);
                }
                rt.Create();
            }
            else
            {
                if (rt.width != sourceDescriptor.width || rt.height != sourceDescriptor.height || rt.sRGB != IsLinear)
                {
                    if (MainCam != null) { if (MainCam.targetTexture == rt) MainCam.targetTexture = null; }
                    if (RenderCam != null) { if (RenderCam.targetTexture == rt) RenderCam.targetTexture = null; }
                    DestroyImmediate(rt);
                    //may have unsupport graphic format bug on Unity2019/2018, fallback to not using descriptor
                    try { rt = new RenderTexture(sourceDescriptor); }
                    catch
                    {
                        DestroyImmediate(rt);
                        rt = new RenderTexture(sourceDescriptor.width, sourceDescriptor.height, sourceDescriptor.depthBufferBits, RenderTextureFormat.ARGB32);
                    }
                    rt.Create();
                }
            }

            if (CapturedTexture == null) { CapturedTexture = new Texture2D(sourceDescriptor.width, sourceDescriptor.height, TextureFormat.RGB24, false, IsLinear); }
            else
            {
                if (CapturedTexture.width != sourceDescriptor.width || CapturedTexture.height != sourceDescriptor.height)
                {
                    DestroyImmediate(CapturedTexture);
                    CapturedTexture = new Texture2D(sourceDescriptor.width, sourceDescriptor.height, TextureFormat.RGB24, false, IsLinear);
                }
            }
        }

        void ProcessCapturedTexture()
        {
            streamWidth = rt.width;
            streamHeight = rt.height;

            if (!FastMode) EnableAsyncGPUReadback = false;
            if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) { StartCoroutine(ProcessCapturedTextureGPUReadbackCOR()); }
            else { StartCoroutine(ProcessCapturedTextureCOR()); }
        }

        IEnumerator ProcessCapturedTextureCOR()
        {
            //render texture to texture2d
            RenderTexture.active = rt;
            CapturedTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            CapturedTexture.Apply();
            RenderTexture.active = null;

            //encode to byte for streaming
            StartCoroutine(EncodeBytes());
            yield break;
        }


        IEnumerator ProcessCapturedTextureGPUReadbackCOR()
        {
#if UNITY_2018_2_OR_NEWER
            if (rt != null)
            {
                AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(rt, 0, TextureFormat.RGB24);
                while (!request.done) yield return null;
                if (!request.hasError) { StartCoroutine(EncodeBytes(request.GetData<byte>().ToArray())); }
                else { EncodingTexture = false; }
            }
            else { EncodingTexture = false; }
#endif
        }

#if FMETP_URP
    private RenderTexture rt_source;

    IEnumerator DelayAddRenderPipelineListenersCOR(float delaySeconds = 0f)
    {
        yield return new WaitForSeconds(delaySeconds);
        AddRenderPipelineListeners();
    }

    private void AddRenderPipelineListeners()
    {
        RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
        RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
    }

    private void RemoveRenderPipelineListeners()
    {
        RenderPipelineManager.endCameraRendering -= RenderPipelineManager_endCameraRendering;
        RenderPipelineManager.beginCameraRendering -= RenderPipelineManager_beginCameraRendering;
    }

    private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        //OnPreRender();
        if (_CaptureMode != GameViewCaptureMode.MainCam) return;
        if (rt_source == null)
        {
            try { rt_source = new RenderTexture(sourceDescriptor); }
            catch
            {
                DestroyImmediate(rt_source);
                rt_source = new RenderTexture(sourceDescriptor.width, sourceDescriptor.height, sourceDescriptor.depthBufferBits, RenderTextureFormat.ARGB32);
            }
            rt_source.Create();
        }
        else
        {
            if (rt_source.width != Screen.width || rt_source.height != Screen.height)
            {
                if (MainCam != null) { if (MainCam.targetTexture == rt_source) MainCam.targetTexture = null; }
                DestroyImmediate(rt_source);
                sourceDescriptor.width = Screen.width;
                sourceDescriptor.height = Screen.height;
                renderResolution = new Vector2(Screen.width, Screen.height) * ResolutionScaling;

                try { rt_source = new RenderTexture(sourceDescriptor); }
                catch
                {
                    DestroyImmediate(rt_source);
                    rt_source = new RenderTexture(sourceDescriptor.width, sourceDescriptor.height, sourceDescriptor.depthBufferBits, RenderTextureFormat.ARGB32);
                }
                rt_source.Create();
            }
        }

        MainCam.targetTexture = rt_source;
    }

    private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        //OnPostRender();
        if (_CaptureMode != GameViewCaptureMode.MainCam) return;
        MainCam.targetTexture = null;
        OnRenderImageURP();
    }

    private void OnRenderImageURP()
    {
        //Graphics.Blit(rt_source, null as RenderTexture);
        if(NeedUpdateTexture && !EncodingTexture)
        {
            NeedUpdateTexture = false;
            CheckResolution();

            if (EnableMixedReality) SetMixedRealityMaterial();
            if (ColorReductionLevel > 0)
            {
                MatColorAdjustment.SetFloat("_Brightness", brightness);
                Graphics.Blit(rt_source, rt, MatColorAdjustment);
            }
            else
            {
                if (EnableMixedReality)
                {
                    Graphics.Blit(rt_source, rt, MatMixedReality);
                }
                else
                {
                    Graphics.Blit(rt_source, rt);
                }
            }

            //RenderTexture to Texture2D
            ProcessCapturedTexture();
        }

        Graphics.Blit(rt_source, null as RenderTexture, mat_source);
    }
#else
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_CaptureMode == GameViewCaptureMode.MainCam)
            {
                if (NeedUpdateTexture && !EncodingTexture)
                {
                    NeedUpdateTexture = false;
                    CheckResolution();

                    if (EnableMixedReality) SetMixedRealityMaterial();
                    if (ColorReductionLevel > 0)
                    {
                        MatColorAdjustment.SetFloat("_Brightness", brightness);
                        Graphics.Blit(source, rt, MatColorAdjustment);
                    }
                    else
                    {
                        if (EnableMixedReality)
                        {
                            Graphics.Blit(source, rt, MatMixedReality);
                        }
                        else
                        {
                            Graphics.Blit(source, rt);
                        }
                    }

                    //RenderTexture to Texture2D
                    ProcessCapturedTexture();
                }
            }

            Graphics.Blit(source, destination);
        }
#endif

        private void SetMixedRealityMaterial()
        {
            if (ColorReductionLevel > 0)
            {
                MatColorAdjustment.SetFloat("_FlipX", MixedRealityFlipX ? 1f : 0f);
                MatColorAdjustment.SetFloat("_FlipY", MixedRealityFlipY ? 1f : 0f);
                MatColorAdjustment.SetFloat("_ScaleX", MixedRealityScaleX);
                MatColorAdjustment.SetFloat("_ScaleY", MixedRealityScaleY);
                MatColorAdjustment.SetFloat("_OffsetX", MixedRealityOffsetX);
                MatColorAdjustment.SetFloat("_OffsetY", MixedRealityOffsetY);
                if (webcamTexture != null) MatColorAdjustment.SetTexture("_WebcamTex", (Texture)webcamTexture);
            }
            else
            {
                MatMixedReality.SetFloat("_FlipX", MixedRealityFlipX ? 1f : 0f);
                MatMixedReality.SetFloat("_FlipY", MixedRealityFlipY ? 1f : 0f);
                MatMixedReality.SetFloat("_ScaleX", MixedRealityScaleX);
                MatMixedReality.SetFloat("_ScaleY", MixedRealityScaleY);
                MatMixedReality.SetFloat("_OffsetX", MixedRealityOffsetX);
                MatMixedReality.SetFloat("_OffsetY", MixedRealityOffsetY);
                if (webcamTexture != null) MatMixedReality.SetTexture("_WebcamTex", (Texture)webcamTexture);
            }
        }

        IEnumerator RenderTextureRefresh()
        {
            if (NeedUpdateTexture && !EncodingTexture)
            {
                NeedUpdateTexture = false;
                EncodingTexture = true;

                yield return new WaitForEndOfFrame();
                CheckResolution();

                if (_CaptureMode == GameViewCaptureMode.RenderCam)
                {
                    if (RenderCam != null)
                    {
                        if (RenderCam.enabled == false) RenderCam.enabled = true;
                        if (PanoramaMode)
                        {
                            RenderCam.targetTexture = rt_cube;
                            RenderCam.RenderToCubemap(rt_cube);

                            Shader.SetGlobalFloat("FORWARD", RenderCam.transform.eulerAngles.y * 0.01745f);
                            MatPano.SetFloat("_Brightness", brightness);
                            Graphics.Blit(rt_cube, rt, MatPano);
                        }
                        else
                        {
                            if (reservedExistingRenderTexture)
                            {
                                RenderCam.targetTexture = rt_reserved;

                                //apply color adjustment for bandwidth
                                if (ColorReductionLevel > 0)
                                {
                                    MatColorAdjustment.SetFloat("_Brightness", brightness);
                                    Graphics.Blit(rt_reserved, rt, MatColorAdjustment);
                                }
                                else { Graphics.Blit(rt_reserved, rt); }
                            }
                            else
                            {
                                RenderCam.targetTexture = rt;
                                RenderCam.Render();
                                RenderCam.targetTexture = null;

                                //apply color adjustment for bandwidth
                                if (ColorReductionLevel > 0)
                                {
                                    MatColorAdjustment.SetFloat("_Brightness", brightness);
                                    Graphics.Blit(rt, rt, MatColorAdjustment);
                                }
                            }

                        }

                        // RenderTexture to Texture2D
                        ProcessCapturedTexture();
                    }
                    else { EncodingTexture = false; }
                }

                if (_CaptureMode == GameViewCaptureMode.FullScreen)
                {
                    if (ResolutionScaling == 1f)
                    {
                        // cleanup
                        if (CapturedTexture != null) Destroy(CapturedTexture);
                        CapturedTexture = ScreenCapture.CaptureScreenshotAsTexture();
                        if (ColorReductionLevel > 0)
                        {
                            MatColorAdjustment.SetFloat("_Brightness", brightness);
                            Graphics.Blit(CapturedTexture, rt, MatColorAdjustment);

                            // RenderTexture to Texture2D
                            ProcessCapturedTexture();
                        }
                        else { StartCoroutine(EncodeBytes()); }
                    }
                    else
                    {
                        // cleanup
                        if (Screenshot != null) Destroy(Screenshot);
                        Screenshot = ScreenCapture.CaptureScreenshotAsTexture();

                        if (ColorReductionLevel > 0)
                        {
                            MatColorAdjustment.SetFloat("_Brightness", brightness);
                            Graphics.Blit(Screenshot, rt, MatColorAdjustment);
                        }
                        else { Graphics.Blit(Screenshot, rt); }

                        // RenderTexture to Texture2D
                        ProcessCapturedTexture();
                    }
                }

                if (_CaptureMode == GameViewCaptureMode.Desktop)
                {
#if UNITY_EDITOR || UNITY_STANDALONE
                    if (fmDesktopManager == null)
                    {
                        if (FMDesktopManager.instance == null)
                        {
                            fmDesktopManager = this.gameObject.AddComponent<FMDesktopManager>();
                            fmDesktopManager.hideFlags = HideFlags.HideInInspector;
                        }
                        else
                        {
                            fmDesktopManager = FMDesktopManager.instance;
                        }
                        while (!fmDesktopManager.IsCapturing()) yield return null;
                    }

                    FMDesktopMonitorCount = fmDesktopManager.MonitorCount;
                    if (FMDesktopMonitorID >= (FMDesktopMonitorCount - 1)) FMDesktopTargetDisplay = (FMDesktopDisplayID)(FMDesktopMonitorCount - 1);

                    int _monitorID = FMDesktopMonitorID;
                    if (fmDesktopManager.AvailableMonitor(_monitorID))
                    {
                        FMMonitor _fmmonitor = fmDesktopManager.FMMonitors[_monitorID];
                        if (!_fmmonitor.FrameTextureReady)
                        {
                            _fmmonitor.RequestUpdate(TextureFormat.RGBA32);
                            fmDesktopManager.TargetTextureFormat = TextureFormat.RGBA32;
                            yield return null;
                        }

                        bool _textureReady = false;
                        while (!_textureReady)
                        {
                            if (_monitorID != FMDesktopMonitorID) yield break;
                            if (fmDesktopManager == null) yield break;
                            if (!fmDesktopManager.IsCapturing()) yield break;

                            _textureReady = fmDesktopManager.FMMonitors[_monitorID].FrameTextureReady && fmDesktopManager.CursorTextureReady;

                            yield return null;
                        }

                        if (_textureReady)
                        {
                            //FMMonitor _fmmonitor = fmDesktopManager.FMMonitors[_monitorID];
                            MatFMDesktop.SetFloat("_FrameWidth", _fmmonitor.FrameWidth);
                            MatFMDesktop.SetFloat("_FrameHeight", _fmmonitor.FrameHeight);

                            float cursor_ratio = (float)fmDesktopManager.CursorWidth / (float)fmDesktopManager.CursorHeight;
                            float screen_cursor_scaling = 80 / _fmmonitor.Scaling;
                            bool landscapeMode = _fmmonitor.FrameWidth > _fmmonitor.FrameHeight;
                            MatFMDesktop.SetFloat("_CursorWidth", (float)(landscapeMode ? _fmmonitor.FrameWidth : _fmmonitor.FrameHeight) / screen_cursor_scaling);
                            MatFMDesktop.SetFloat("_CursorHeight", ((float)(landscapeMode ? _fmmonitor.FrameWidth : _fmmonitor.FrameHeight) / screen_cursor_scaling) / cursor_ratio);

                            MatFMDesktop.SetFloat("_MonitorScaling", _fmmonitor.Scaling);

                            float cursorPointX = (fmDesktopManager.CursorPoint.Position.x - _fmmonitor.MonitorOffsetX) * _fmmonitor.Scaling;
                            float cursorPointY = (fmDesktopManager.CursorPoint.Position.y - _fmmonitor.MonitorOffsetY) * _fmmonitor.Scaling;
                            cursorPointX -= fmDesktopManager.CursorPoint.HotSpot.x * _fmmonitor.Scaling;
                            cursorPointY -= fmDesktopManager.CursorPoint.HotSpot.y * _fmmonitor.Scaling;
                            MatFMDesktop.SetFloat("_CursorPointX", cursorPointX);
                            MatFMDesktop.SetFloat("_CursorPointY", cursorPointY);

                            MatFMDesktop.SetFloat("_FlipX", FMDesktopFlipX ? 0f : 1f);
                            MatFMDesktop.SetFloat("_FlipY", FMDesktopFlipY ? 0f : 1f);
                            MatFMDesktop.SetFloat("_RangeX", FMDesktopRangeX);
                            MatFMDesktop.SetFloat("_RangeY", FMDesktopRangeY);
                            MatFMDesktop.SetFloat("_OffsetX", FMDesktopOffsetX);
                            MatFMDesktop.SetFloat("_OffsetY", FMDesktopOffsetY);
                            MatFMDesktop.SetFloat("_RotationAngle", (float)FMDesktopRotationAngle);


                            MatFMDesktop.SetTexture("_MainTex", _fmmonitor.TextureFrame);
                            MatFMDesktop.SetTexture("_CursorTex", fmDesktopManager.TextureCursor);

                            MatFMDesktop.SetFloat("_Brightness", brightness);
                            Graphics.Blit(_fmmonitor.TextureFrame, rt, MatFMDesktop);

                            fmDesktopFrameWidth = (Int16)(_fmmonitor.FrameWidth/_fmmonitor.Scaling);
                            fmDesktopFrameHeight = (Int16)(_fmmonitor.FrameHeight/_fmmonitor.Scaling);
                            fmDesktopMonitorOffsetX = (Int16)_fmmonitor.MonitorOffsetX;
                            fmDesktopMonitorOffsetY = (Int16)_fmmonitor.MonitorOffsetY;

                            //request for next frame
                            _fmmonitor.RequestUpdate(TextureFormat.RGBA32);
                            fmDesktopManager.TargetTextureFormat = TextureFormat.RGBA32;

                            //RenderTexture to Texture2D
                            ProcessCapturedTexture();
                        }
                        else { EncodingTexture = false; }
                    }
                    else { EncodingTexture = false; }
#else
                    EncodingTexture = false;
#endif
                }
            }
        }

        public void Action_UpdateTexture() { RequestTextureUpdate(); }

        private void RequestTextureUpdate()
        {
            if (EncodingTexture) return;
            NeedUpdateTexture = true;
            if (_CaptureMode != GameViewCaptureMode.MainCam) StartCoroutine(RenderTextureRefresh());
        }

        private IEnumerator InvokeEventsCheckerCOR()
        {
            while (!stop)
            {
                yield return null;
                while (AppendQueueSendByteFMMJPEG.Count > 0) OnDataByteReadyEvent.Invoke(AppendQueueSendByteFMMJPEG.Dequeue());
                while (AppendQueueSendByteMJPEG.Count > 0) OnRawMJPEGReadyEvent.Invoke(AppendQueueSendByteMJPEG.Dequeue());
            }
            yield break;
        }

        private IEnumerator SenderCOR()
        {
            StartCoroutine(InvokeEventsCheckerCOR());
            while (!stop)
            {
                if (Time.realtimeSinceStartup > next)
                {
                    if (StreamFPS > 0)
                    {
                        interval = 1f / StreamFPS;
                        next = Time.realtimeSinceStartup + interval;

                        RequestTextureUpdate();
                    }
                }
                yield return null;
            }
        }

        private IEnumerator EncodeBytes(byte[] RawTextureData = null)
        {
            if (CapturedTexture != null || RawTextureData != null)
            {
                //==================getting byte data==================
#if UNITY_IOS && !UNITY_EDITOR
            FastMode = true;
#endif


#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_WIN || UNITY_IOS || UNITY_ANDROID || WINDOWS_UWP || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                //supported fast mode
#else
                //not supported fast mode
                FastMode = false;
#endif
                if (OutputFormat != GameViewOutputFormat.FMMJPEG)
                {
                    GZipMode = false;
                }

                bool detectedSimilarTexture = false;
                if (FastMode)
                {
                    //try AsyncMode, on supported platform
                    if (AsyncMode && Loom.numThreads < Loom.maxThreads)
                    {
                        //has spare thread
                        if (RawTextureData == null)
                        {
                            RawTextureData = CapturedTexture.GetRawTextureData();
                            streamWidth = CapturedTexture.width;
                            streamHeight = CapturedTexture.height;
                        }
                        bool AsyncEncoding = true;
                        Loom.RunAsync(() =>
                        {
                            try { dataByte = RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling); } catch { }

                            if (ignoreSimilarTexture) detectedSimilarTexture = FMCoreTools.CheckSimilarSize(dataByte.Length, lastRawDataByte, similarByteSizeThreshold);
                            lastRawDataByte = dataByte.Length;

                            if (GZipMode && !detectedSimilarTexture) try { dataByte = dataByte.FMZipBytes(); } catch { }

                            AsyncEncoding = false;
                        });
                        while (AsyncEncoding) yield return null;
                    }
                    else
                    {
                        //need yield return, in order to fix random error "coroutine->IsInList()"
                        yield return dataByte = RawTextureData == null ? CapturedTexture.FMEncodeToJPG(Quality, ChromaSubsampling) : RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling);

                        if (ignoreSimilarTexture) detectedSimilarTexture = FMCoreTools.CheckSimilarSize(dataByte.Length, lastRawDataByte, similarByteSizeThreshold);
                        lastRawDataByte = dataByte.Length;

                        if (GZipMode && !detectedSimilarTexture) yield return FMCoreTools.RunCOR<byte[]>(FMCoreTools.FMZippedByteCOR(dataByte), (output) => dataByte = output);
                    }
                }
                else
                {
                    dataByte = RawTextureData == null ? CapturedTexture.EncodeToJPG(Quality) : RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling);

                    if (ignoreSimilarTexture) detectedSimilarTexture = FMCoreTools.CheckSimilarSize(dataByte.Length, lastRawDataByte, similarByteSizeThreshold);
                    lastRawDataByte = dataByte.Length;

                    if (GZipMode && !detectedSimilarTexture) yield return FMCoreTools.RunCOR<byte[]>(FMCoreTools.FMZippedByteCOR(dataByte), (output) => dataByte = output);
                }

                if (!detectedSimilarTexture)
                {
                    dataByteTemp = dataByte.ToArray();
                    EncodingTexture = false;
                    //==================getting byte data==================
                    int _length = dataByteTemp.Length;
                    dataLength = _length;

                    int _offset = 0;
                    byte[] _meta_label = BitConverter.GetBytes(label);
                    byte[] _meta_id = BitConverter.GetBytes(dataID);
                    byte[] _meta_length = BitConverter.GetBytes(_length);

                    byte[] _meta_monitorOffsetX = BitConverter.GetBytes(FMDesktopMonitorOffsetX);
                    byte[] _meta_monitorOffsetY = BitConverter.GetBytes(FMDesktopMonitorOffsetY);
                    byte[] _meta_frameWidth = BitConverter.GetBytes(FMDesktopFrameWidth);
                    byte[] _meta_frameHeight = BitConverter.GetBytes(FMDesktopFrameHeight);

                    int chunks = Mathf.CeilToInt((float)_length / (float)chunkSize);
                    int metaByteLength = CaptureMode == GameViewCaptureMode.Desktop ? 23 : 15;
                    for (int i = 1; i <= chunks; i++)
                    {
                        int dataByteLength = (i == chunks) ? (_length % chunkSize) : (chunkSize);
                        if (OutputFormat == GameViewOutputFormat.FMMJPEG)
                        {
                            byte[] _meta_offset = BitConverter.GetBytes(_offset);
                            byte[] SendByte = new byte[dataByteLength + metaByteLength];

                            Buffer.BlockCopy(_meta_label, 0, SendByte, 0, 2);
                            Buffer.BlockCopy(_meta_id, 0, SendByte, 2, 2);
                            Buffer.BlockCopy(_meta_length, 0, SendByte, 4, 4);

                            Buffer.BlockCopy(_meta_offset, 0, SendByte, 8, 4);
                            SendByte[12] = (byte)(GZipMode ? 1 : 0);
                            SendByte[13] = (byte)ColorReductionLevel;

                            //for desktop
                            SendByte[14] = (byte)(CaptureMode == GameViewCaptureMode.Desktop ? 1 : 0);
                            if (CaptureMode == GameViewCaptureMode.Desktop)
                            {
                                Buffer.BlockCopy(_meta_monitorOffsetX, 0, SendByte, 15, 2);
                                Buffer.BlockCopy(_meta_monitorOffsetY, 0, SendByte, 17, 2);
                                Buffer.BlockCopy(_meta_frameWidth, 0, SendByte, 19, 2);
                                Buffer.BlockCopy(_meta_frameHeight, 0, SendByte, 21, 2);
                            }

                            Buffer.BlockCopy(dataByteTemp, _offset, SendByte, metaByteLength, dataByteLength);
                            AppendQueueSendByteFMMJPEG.Enqueue(SendByte);
                        }
                        else if (OutputFormat == GameViewOutputFormat.MJPEG)
                        {
                            //==================output raw mjpeg data==================
                            byte[] SendByte = new byte[dataByteLength];
                            Buffer.BlockCopy(dataByteTemp, _offset, SendByte, 0, dataByteLength);
                            AppendQueueSendByteMJPEG.Enqueue(SendByte);
                        }

                        _offset += chunkSize;
                    }

                    dataID++;
                    if (dataID > maxID) dataID = 0;
                }
            }

            EncodingTexture = false;
            yield break;
        }

        private void OnEnable() { StartAll(); }
        private void OnDisable() { StopAll(); }
        private void OnApplicationQuit() { StopAll(); }
        private void OnDestroy() { StopAll(); }

        private void StopAll()
        {
            stop = true;
            StopAllCoroutines();

            lastRawDataByte = 0;
            AppendQueueSendByteFMMJPEG.Clear();
            AppendQueueSendByteMJPEG.Clear();

#if FMETP_URP
        RemoveRenderPipelineListeners();
#endif
        }

        private void StartAll()
        {
#if FMETP_URP
        StartCoroutine(DelayAddRenderPipelineListenersCOR(2f));
#endif
            if (Time.realtimeSinceStartup < 3f) return;
            stop = false;
            StartCoroutine(SenderCOR());

            NeedUpdateTexture = false;
            EncodingTexture = false;
        }
    }
}