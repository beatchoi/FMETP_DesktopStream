using System.Collections;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace FMETP
{
    [Serializable]
    public enum FMTextureType { Texture2D, RenderTexture, WebcamTexture }
    [AddComponentMenu("FMETP/Mapper/TextureEncoder")]
    public class TextureEncoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowMode = true;
        public bool EditorShowSettings = true;
        public bool EditorShowNetworking = true;
        public bool EditorShowEncoded = true;
        public bool EditorShowPairing = true;
        #endregion

        public FMTextureType TextureType = FMTextureType.Texture2D;
        public Texture2D StreamTexture;
        public RenderTexture StreamRenderTexture;

        public WebCamTexture StreamWebCamTexture;
        public RenderTexture StreamWebCamRenderTexture;
        public WebCamTexture SetStreamWebCamTexture { set { StreamWebCamTexture = value; } }

        [Range(0.05f, 1f)]
        public float ResolutionScaling = 0.5f;

        public Texture GetStreamTexture
        {
            get
            {
                if (TextureType == FMTextureType.Texture2D) return StreamTexture;
                if (TextureType == FMTextureType.WebcamTexture) return StreamWebCamTexture;
                return StreamRenderTexture;
            }
        }
        public Texture GetPreviewTexture
        {
            get
            {
                if (TextureType == FMTextureType.Texture2D) return StreamTexture;
                if (TextureType == FMTextureType.WebcamTexture) return StreamWebCamRenderTexture;
                return StreamRenderTexture;
            }
        }

        public bool FastMode = false;
        public bool AsyncMode = false;
        public bool GZipMode = false;

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

        public GameViewOutputFormat OutputFormat = GameViewOutputFormat.FMMJPEG;
        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();
        public UnityEventByteArray OnRawMJPEGReadyEvent = new UnityEventByteArray();

        //[Header("Pair Encoder & Decoder")]
        public UInt16 label = 1001;
        private UInt16 dataID = 0;
        private UInt16 maxID = 1024;
        private int chunkSize = 1400; //8096 //32768 //1400
        private float next = 0f;
        private bool stop = false;
        private byte[] dataByte;
        private byte[] dataByteTemp;
        private Queue<byte[]> AppendQueueSendByteFMMJPEG = new Queue<byte[]>();
        private Queue<byte[]> AppendQueueSendByteMJPEG = new Queue<byte[]>();

        public int dataLength;

        //texture settings
        private byte[] RawTextureData;
        private int streamWidth = 8;
        private int streamHeight = 8;

        public int StreamWidth { get { return streamWidth; } }
        public int StreamHeight { get { return streamHeight; } }

        private void Start()
        {
            Application.runInBackground = true;

#if UNITY_2018_2_OR_NEWER
            supportsAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback;
#else
        supportsAsyncGPUReadback = false;
#endif

            StartCoroutine(SenderCOR());
        }

        public void Action_StreamTexture(byte[] inputRawTextureData, int inputWidth, int inputHeight)
        {
            int stride = inputRawTextureData.Length / (inputWidth * inputHeight);
            if (stride != 3 && stride != 4)
            {
                Debug.LogError("unsupported stride count: " + stride + ", only RGB24(stride: 3) and RGBA32(stride: 4) are supported");
                return;
            }

            RawTextureData = new byte[inputRawTextureData.Length];
            Buffer.BlockCopy(inputRawTextureData, 0, RawTextureData, 0, inputRawTextureData.Length);

            streamWidth = inputWidth;
            streamHeight = inputHeight;
            NeedUpdateTexture = true;
        }

        public void Action_StreamTexture(Texture2D inputTexture2D)
        {
            if (inputTexture2D == null) return;
            if (!inputTexture2D.isReadable) return;
            if (inputTexture2D.format != TextureFormat.RGB24 && inputTexture2D.format != TextureFormat.RGBA32)
            {
                Debug.LogError("unsupported formmat: " + inputTexture2D.format + ", only RGB24 and RGBA32 are supported");
                return;
            }

            Action_StreamTexture(inputTexture2D.GetRawTextureData(), inputTexture2D.width, inputTexture2D.height);
        }

        public void Action_StreamWebcamTexture(WebCamTexture inputWebcamTexture)
        {
            if (inputWebcamTexture == null) return;

            streamWidth = Mathf.RoundToInt(inputWebcamTexture.width * ResolutionScaling);
            streamHeight = Mathf.RoundToInt(inputWebcamTexture.height * ResolutionScaling);

            if (streamWidth < 1) streamWidth = 1;
            if (streamHeight < 1) streamHeight = 1;

            if (StreamWebCamRenderTexture == null)
            {
                StreamWebCamRenderTexture = new RenderTexture(streamWidth, streamHeight, 0, RenderTextureFormat.ARGB32);
                StreamWebCamRenderTexture.Create();
            }
            else
            {
                if (StreamWebCamRenderTexture.width != streamWidth || StreamWebCamRenderTexture.height != streamHeight)
                {
                    Destroy(StreamWebCamRenderTexture);
                    StreamWebCamRenderTexture = new RenderTexture(streamWidth, streamHeight, 0, RenderTextureFormat.ARGB32);
                    StreamWebCamRenderTexture.Create();
                }
            }

            Graphics.Blit(inputWebcamTexture, StreamWebCamRenderTexture);
            Action_StreamRenderTexture(StreamWebCamRenderTexture);
        }

        public void Action_StreamRenderTexture(RenderTexture inputRenderTexture)
        {
            if (inputRenderTexture == null) return;

            streamWidth = inputRenderTexture.width;
            streamHeight = inputRenderTexture.height;

            //RenderTexture to Texture2D
            if (!FastMode) EnableAsyncGPUReadback = false;
            if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) { StartCoroutine(ProcessCapturedTextureGPUReadbackCOR(inputRenderTexture)); }
            else
            {
                if (StreamTexture == null) { StreamTexture = new Texture2D(inputRenderTexture.width, inputRenderTexture.height, TextureFormat.RGB24, false); }
                else
                {
                    if (StreamTexture.width != inputRenderTexture.width || StreamTexture.height != inputRenderTexture.height)
                    {
                        DestroyImmediate(StreamTexture);
                        StreamTexture = new Texture2D(inputRenderTexture.width, inputRenderTexture.height, TextureFormat.RGB24, false);
                    }
                }

                RenderTexture.active = inputRenderTexture;
                StreamTexture.ReadPixels(new Rect(0, 0, inputRenderTexture.width, inputRenderTexture.height), 0, 0);
                StreamTexture.Apply();
                RenderTexture.active = null;

                Action_StreamTexture(StreamTexture);
            }
        }

        IEnumerator ProcessCapturedTextureGPUReadbackCOR(RenderTexture inputRenderTexture)
        {
            yield return new WaitForEndOfFrame();
#if UNITY_2018_2_OR_NEWER
            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(inputRenderTexture, 0, TextureFormat.RGB24);
            while (!request.done) yield return null;
            if (!request.hasError) RawTextureData = request.GetData<byte>().ToArray();
            NeedUpdateTexture = true;
#endif
        }

        private void RequestTextureUpdate()
        {
            switch (TextureType)
            {
                case FMTextureType.Texture2D:
                    if (StreamTexture != null) Action_StreamTexture(StreamTexture);
                    break;
                case FMTextureType.RenderTexture:
                    if (StreamRenderTexture != null) Action_StreamRenderTexture(StreamRenderTexture);
                    break;
                case FMTextureType.WebcamTexture:
                    if (StreamWebCamTexture != null) Action_StreamWebcamTexture(StreamWebCamTexture);
                    break;
            }


            if (!NeedUpdateTexture) return;
            if (!EncodingTexture)
            {
                //update it now
                EncodingTexture = true;
                StartCoroutine(EncodeBytes());

                NeedUpdateTexture = false;
            }
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

        private IEnumerator EncodeBytes()
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
                    yield return dataByte = RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling);

                    if (ignoreSimilarTexture) detectedSimilarTexture = FMCoreTools.CheckSimilarSize(dataByte.Length, lastRawDataByte, similarByteSizeThreshold);
                    lastRawDataByte = dataByte.Length;

                    if (GZipMode && !detectedSimilarTexture) yield return FMCoreTools.RunCOR<byte[]>(FMCoreTools.FMZippedByteCOR(dataByte), (output) => dataByte = output);
                }
            }
            else
            {
                dataByte = RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling);

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

                int chunks = Mathf.CeilToInt((float)_length / (float)chunkSize);
                int metaByteLength = 14;
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
                        SendByte[13] = (byte)0;

                        Buffer.BlockCopy(dataByteTemp, _offset, SendByte, 14, dataByteLength);
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