using System.Collections;
using UnityEngine;
using System;

using UnityEngine.Rendering;
using System.Linq;
using System.Collections.Generic;

namespace FMETP
{
    [RequireComponent(typeof(Camera)), AddComponentMenu("FMETP/Mapper/FMPCStreamEncoder")]
    public class FMPCStreamEncoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowSettings = true;
        public bool EditorShowNetworking = true;
        public bool EditorShowEncoded = true;
        public bool EditorShowPairing = true;
        #endregion

        public Camera TargetCamera;

        private RenderTextureDescriptor renderTextureDescriptor;
        private RenderTexture rt;
        private RenderTexture rt_fx;

        [Range(1, 8192)] public int TargetWidth = 256;
        [Range(1, 8192)] public int TargetHeight = 256;

        private int streamWidth = 256;
        private int streamHeight = 256;

        [Range(10, 100)]
        public int Quality = 80;
        public FMChromaSubsamplingOption ChromaSubsampling = FMChromaSubsamplingOption.Subsampling420;

        [Range(0f, 60f)]
        public float StreamFPS = 20f;
        private float interval = 0.05f;

        [HideInInspector] public Material MatFMPCStream;

        private void Reset()
        {
            MatFMPCStream = new Material(Shader.Find("Hidden/FMPCStreamEncoder"));
            TargetCamera = GetComponent<Camera>();
            TargetCamera.depthTextureMode = DepthTextureMode.DepthNormals;
        }

        private void CheckResolution()
        {
            streamWidth = TargetWidth;
            streamHeight = TargetHeight;
            streamWidth = Mathf.Clamp(streamWidth, 1, 8192);
            streamHeight = Mathf.Clamp(streamHeight, 1, 8192);

            if (rt == null)
            {
                rt = new RenderTexture(streamWidth, streamHeight, 32, RenderTextureFormat.ARGB32);
                rt.Create();
                rt.filterMode = FilterMode.Point;
                TargetCamera.targetTexture = rt;
            }
            else
            {
                if (rt.width != streamWidth || rt.height != streamHeight)
                {
                    TargetCamera.targetTexture = null;
                    DestroyImmediate(rt);
                    rt = new RenderTexture(streamWidth, streamHeight, 32, RenderTextureFormat.ARGB32);
                    rt.Create();
                    rt.filterMode = FilterMode.Point;
                    TargetCamera.targetTexture = rt;
                }
            }

            if (CapturedTexture == null) { CapturedTexture = new Texture2D(streamWidth, streamHeight, TextureFormat.RGB24, false, false); }
            else
            {
                if (CapturedTexture.width != streamWidth || CapturedTexture.height != streamHeight)
                {
                    DestroyImmediate(CapturedTexture);
                    CapturedTexture = new Texture2D(streamWidth, streamHeight, TextureFormat.RGB24, false, false);
                    CapturedTexture.filterMode = FilterMode.Point;
                }
            }

            if (TargetCamera.depthTextureMode != DepthTextureMode.DepthNormals)
            {
                TargetCamera.depthTextureMode = DepthTextureMode.DepthNormals;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (NeedUpdateTexture && !EncodingTexture)
            {
                NeedUpdateTexture = false;

                if (rt_fx == null)
                {
                    renderTextureDescriptor = source.descriptor;
                    renderTextureDescriptor.sRGB = false;
                    //renderTextureDescriptor.width *= 2;
                    rt_fx = RenderTexture.GetTemporary(renderTextureDescriptor);
                }
                else if (rt_fx.width != source.descriptor.width * 2)
                {
                    rt_fx.Release();
                    renderTextureDescriptor = source.descriptor;
                    //renderTextureDescriptor.width *= 2;
                    rt_fx = RenderTexture.GetTemporary(renderTextureDescriptor);
                }
                Graphics.Blit(source, rt_fx, MatFMPCStream);

                //RenderTexture to Texture2D
                ProcessCapturedTexture();
            }
            Graphics.Blit(source, destination);
        }

        private void RequestTextureUpdate()
        {
            if (EncodingTexture) return;
            NeedUpdateTexture = true;

            CheckResolution();
            TargetCamera.Render();
        }

        public bool FastMode = false;
        public bool AsyncMode = false;

        public bool GZipMode = false;
        private bool NeedUpdateTexture = false;
        private bool EncodingTexture = false;

        public bool ignoreSimilarTexture = true;
        private int lastRawDataByte = 0;
        [Tooltip("Compare previous image data size(byte)")]
        public int similarByteSizeThreshold = 8;

        //experimental feature: check if your GPU supports AsyncReadback
        private bool supportsAsyncGPUReadback = false;
        public bool EnableAsyncGPUReadback = true;
        public bool SupportsAsyncGPUReadback { get { return supportsAsyncGPUReadback; } }

        public Texture2D CapturedTexture;
        public Texture GetStreamTexture
        {
            get
            {
                if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) return rt;
                return CapturedTexture;
            }
        }

        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();

        //[Header("Pair Encoder & Decoder")]
        public UInt16 label = 4001;
        private UInt16 dataID = 0;
        private UInt16 maxID = 1024;
        private int chunkSize = 1400; //32768
        private float next = 0f;
        private bool stop = false;
        private byte[] dataByte;
        private byte[] dataByteTemp;
        private Queue<byte[]> AppendQueueSendByte = new Queue<byte[]>();

        public int dataLength;

        private void ProcessCapturedTexture()
        {
            streamWidth = rt_fx.width;
            streamHeight = rt_fx.height;

            if (!FastMode) EnableAsyncGPUReadback = false;
            if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) { StartCoroutine(ProcessCapturedTextureGPUReadbackCOR()); }
            else { StartCoroutine(ProcessCapturedTextureCOR()); }
        }

        IEnumerator ProcessCapturedTextureCOR()
        {
            //render texture to texture2d
            //Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt_fx;
            CapturedTexture.ReadPixels(new Rect(0, 0, rt_fx.width, rt_fx.height), 0, 0);
            CapturedTexture.Apply();
            RenderTexture.active = previous;

            //encode to byte for streaming
            StartCoroutine(EncodeBytes());
            yield break;
        }

        IEnumerator ProcessCapturedTextureGPUReadbackCOR()
        {
            yield return new WaitForEndOfFrame();
#if UNITY_2018_2_OR_NEWER
            if (rt_fx != null)
            {
                AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(rt_fx, 0, TextureFormat.RGB24);
                while (!request.done) yield return null;
                if (!request.hasError) { StartCoroutine(EncodeBytes(request.GetData<byte>().ToArray())); }
                else { EncodingTexture = false; }
            }
            else { EncodingTexture = false; }
#endif
        }

        private IEnumerator InvokeEventsCheckerCOR()
        {
            while (!stop)
            {
                yield return null;
                while (AppendQueueSendByte.Count > 0) OnDataByteReadyEvent.Invoke(AppendQueueSendByte.Dequeue());
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
                bool detectedSimilarTexture = false;
                if (FastMode)
                {
                    //try AsyncMode, on supported platform
                    if (AsyncMode && Loom.numThreads < Loom.maxThreads)
                    {
                        //has spare thread
                        if (RawTextureData == null) RawTextureData = CapturedTexture.GetRawTextureData();
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
                    //add camera meta data
                    byte[] _camNearClipPlane = BitConverter.GetBytes(TargetCamera.nearClipPlane);
                    byte[] _camFarClipPlane = BitConverter.GetBytes(TargetCamera.farClipPlane);

                    byte[] _camFOV = BitConverter.GetBytes(TargetCamera.fieldOfView);
                    byte[] _camAspect = BitConverter.GetBytes(TargetCamera.aspect);

                    byte[] _camOrthographicProjection = BitConverter.GetBytes((TargetCamera.orthographic ? 1f : 0f));
                    byte[] _camOrthographicSize = BitConverter.GetBytes(TargetCamera.orthographicSize);

                    byte[] _dataByteTmp = new byte[dataByte.Length + 24];

                    Buffer.BlockCopy(_camNearClipPlane, 0, _dataByteTmp, 0, 4);
                    Buffer.BlockCopy(_camFarClipPlane, 0, _dataByteTmp, 4, 4);
                    Buffer.BlockCopy(_camFOV, 0, _dataByteTmp, 8, 4);
                    Buffer.BlockCopy(_camAspect, 0, _dataByteTmp, 12, 4);
                    Buffer.BlockCopy(_camOrthographicProjection, 0, _dataByteTmp, 16, 4);
                    Buffer.BlockCopy(_camOrthographicSize, 0, _dataByteTmp, 20, 4);
                    Buffer.BlockCopy(dataByte, 0, _dataByteTmp, 24, dataByte.Length);
                    dataByte = _dataByteTmp;

                    dataByteTemp = dataByte.ToArray();
                    EncodingTexture = false;
                    //==================getting byte data==================
                    int _length = dataByteTemp.Length;
                    dataLength = _length;

                    int _offset = 0;
                    byte[] _meta_label = BitConverter.GetBytes(label);
                    byte[] _meta_id = BitConverter.GetBytes(dataID);
                    byte[] _meta_length = BitConverter.GetBytes(_length);

                    int chunks = Mathf.CeilToInt((float)_length / (float)chunkSize);
                    int metaByteLength = 14;
                    for (int i = 1; i <= chunks; i++)
                    {
                        int dataByteLength = (i == chunks) ? (_length % chunkSize) : (chunkSize);
                        byte[] _meta_offset = BitConverter.GetBytes(_offset);
                        byte[] SendByte = new byte[dataByteLength + metaByteLength];

                        Buffer.BlockCopy(_meta_label, 0, SendByte, 0, 2);
                        Buffer.BlockCopy(_meta_id, 0, SendByte, 2, 2);
                        Buffer.BlockCopy(_meta_length, 0, SendByte, 4, 4);

                        Buffer.BlockCopy(_meta_offset, 0, SendByte, 8, 4);
                        SendByte[12] = (byte)(GZipMode ? 1 : 0);
                        SendByte[13] = (byte)0;//not used, but just keep one empty byte for standard

                        Buffer.BlockCopy(dataByteTemp, _offset, SendByte, 14, dataByteLength);
                        AppendQueueSendByte.Enqueue(SendByte);

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
            AppendQueueSendByte.Clear();
        }

        private void StartAll()
        {
            if (Time.realtimeSinceStartup < 3f) return;
            stop = false;
            StartCoroutine(SenderCOR());

            NeedUpdateTexture = false;
            EncodingTexture = false;
        }
    }
}