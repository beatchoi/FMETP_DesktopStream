using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace FMETP
{
    [Serializable]
    public enum MicDeviceMode { Default, TargetDevice }
    [AddComponentMenu("FMETP/Mapper/MicEncoder")]
    public class MicEncoder : MonoBehaviour
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        #region EditorProps
        public bool EditorShowCapture = true;
        public bool EditorShowAudioInfo = true;
        public bool EditorShowEncoded = true;
        public bool EditorShowPairing = true;
        #endregion

        //----------------------------------------------
        private AudioSource AudioMic;
        private ConcurrentQueue<byte> AudioBytes = new ConcurrentQueue<byte>();

        public MicDeviceMode DeviceMode = MicDeviceMode.Default;
        public string TargetDeviceName = "MacBook Pro Microphone";
        string CurrentDeviceName = null;

        [TextArea]
        public string DetectedDevices;

        //[Header("[Capture In-Game Sound]")]
        public bool StreamGameSound = true;
        public int OutputSampleRate = 11025;
        [Range(1, 8)]
        public int OutputChannels = 1;

        private int CurrentAudioTimeSample = 0;
        private int LastAudioTimeSample = 0;
        //----------------------------------------------

        [Range(1f, 60f)]
        public float StreamFPS = 20f;
        private float interval = 0.05f;

        public bool UseHalf = true;
        public bool GZipMode = false;

        public AudioOutputFormat OutputFormat = AudioOutputFormat.FMPCM16;
        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();
        public UnityEventByteArray OnRawPCM16ReadyEvent = new UnityEventByteArray();

        //[Header("Pair Encoder & Decoder")]
        public UInt16 label = 2001;
        private UInt16 dataID = 0;
        private UInt16 maxID = 1024;
        private int chunkSize = 1400; //32768;
        private float next = 0f;
        private bool stop = false;
        private byte[] dataByte;
        private byte[] dataByteTemp;
        private Queue<byte[]> AppendQueueSendByteFMPCM16 = new Queue<byte[]>();
        private Queue<byte[]> AppendQueueSendBytePCM16 = new Queue<byte[]>();

        public int dataLength;
        // Use this for initialization
        private void Start()
        {
            StartCoroutine(CaptureMic());
            StartCoroutine(SenderCOR());
        }

        IEnumerator CaptureMic()
        {
            if (AudioMic == null) AudioMic = GetComponent<AudioSource>();
            if (AudioMic == null) AudioMic = gameObject.AddComponent<AudioSource>();

            //Check Target Device
            DetectedDevices = "";
            string[] MicNames = Microphone.devices;
            foreach (string _name in MicNames) DetectedDevices += _name + "\n";
            if (DeviceMode == MicDeviceMode.TargetDevice)
            {
                bool IsCorrectName = false;
                for(int i = 0; i<MicNames.Length; i++)
                {
                    if(MicNames[i] == TargetDeviceName)
                    {
                        IsCorrectName = true;
                        break;
                    }
                }
                if (!IsCorrectName) TargetDeviceName = null;
            }
            //Check Target Device

            CurrentDeviceName = DeviceMode == MicDeviceMode.Default ? MicNames[0] : TargetDeviceName;
            AudioMic.clip = Microphone.Start(CurrentDeviceName, true, 1, OutputSampleRate);
            AudioMic.loop = true;
            while (!(Microphone.GetPosition(CurrentDeviceName) > 0)) { }
            Debug.Log(CurrentDeviceName + " Start Mic(pos): " + Microphone.GetPosition(CurrentDeviceName));
            AudioMic.Play();

            AudioMic.volume = 0f;

            OutputChannels = AudioMic.clip.channels;

            while (!stop)
            {
                AddMicData();
                yield return null;
            }
            yield return null;
        }

        private void AddMicData()
        {
            LastAudioTimeSample = CurrentAudioTimeSample;
            //CurrentAudioTimeSample = AudioMic.timeSamples;
            CurrentAudioTimeSample = Microphone.GetPosition(CurrentDeviceName);

            if (CurrentAudioTimeSample != LastAudioTimeSample)
            {
                float[] samples = new float[AudioMic.clip.samples];
                AudioMic.clip.GetData(samples, 0);

                if (CurrentAudioTimeSample > LastAudioTimeSample)
                {
                    for (int i = LastAudioTimeSample; i < CurrentAudioTimeSample; i++)
                    {
                        byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(samples[i]));
                        foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                    }
                }
                else if (CurrentAudioTimeSample < LastAudioTimeSample)
                {
                    for (int i = LastAudioTimeSample; i < samples.Length; i++)
                    {
                        byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(samples[i]));
                        foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                    }
                    for (int i = 0; i < CurrentAudioTimeSample; i++)
                    {
                        byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(samples[i]));
                        foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                    }
                }
            }
        }

        private IEnumerator InvokeEventsCheckerCOR()
        {
            while (!stop)
            {
                yield return null;
                while (AppendQueueSendByteFMPCM16.Count > 0) OnDataByteReadyEvent.Invoke(AppendQueueSendByteFMPCM16.Dequeue());
                while (AppendQueueSendBytePCM16.Count > 0) OnRawPCM16ReadyEvent.Invoke(AppendQueueSendBytePCM16.Dequeue());
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
                    interval = 1f / StreamFPS;
                    next = Time.realtimeSinceStartup + interval;

                    //==================getting byte data==================
                    if (OutputFormat == AudioOutputFormat.FMPCM16)
                    {
                        byte[] _samplerateByte = BitConverter.GetBytes(OutputSampleRate);
                        byte[] _channelsByte = BitConverter.GetBytes(OutputChannels);

                        dataByte = new byte[AudioBytes.Count + _samplerateByte.Length + _channelsByte.Length];

                        Buffer.BlockCopy(_samplerateByte, 0, dataByte, 0, _samplerateByte.Length);
                        Buffer.BlockCopy(_channelsByte, 0, dataByte, 4, _channelsByte.Length);
                        Buffer.BlockCopy(AudioBytes.ToArray(), 0, dataByte, 8, AudioBytes.Count);
                    }
                    else
                    {
                        dataByte = new byte[AudioBytes.Count];
                        Buffer.BlockCopy(AudioBytes.ToArray(), 0, dataByte, 0, AudioBytes.Count);
                        GZipMode = false;
                    }

                    AudioBytes = new ConcurrentQueue<byte>();

                    if (GZipMode) yield return FMCoreTools.RunCOR<byte[]>(FMCoreTools.FMZippedByteCOR(dataByte), (output) => dataByte = output);

                    dataByteTemp = dataByte.ToArray();
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
                        if (OutputFormat == AudioOutputFormat.FMPCM16)
                        {
                            byte[] _meta_offset = BitConverter.GetBytes(_offset);
                            byte[] SendByte = new byte[dataByteLength + metaByteLength];

                            Buffer.BlockCopy(_meta_label, 0, SendByte, 0, 2);
                            Buffer.BlockCopy(_meta_id, 0, SendByte, 2, 2);
                            Buffer.BlockCopy(_meta_length, 0, SendByte, 4, 4);
                            Buffer.BlockCopy(_meta_offset, 0, SendByte, 8, 4);
                            SendByte[12] = (byte)(GZipMode ? 1 : 0);
                            SendByte[13] = (byte)0;//not used, but just keep one empty byte for standard

                            Buffer.BlockCopy(dataByteTemp, _offset, SendByte, 14, dataByteLength);
                            AppendQueueSendByteFMPCM16.Enqueue(SendByte);
                        }
                        else
                        {
                            byte[] SendByte = new byte[dataByteLength];
                            Buffer.BlockCopy(dataByteTemp, _offset, SendByte, 0, dataByteLength);

                            if (!BitConverter.IsLittleEndian) Array.Reverse(SendByte);
                            AppendQueueSendBytePCM16.Enqueue(SendByte);
                        }
                        _offset += chunkSize;
                    }

                    dataID++;
                    if (dataID > maxID) dataID = 0;
                }
                yield return null;
            }
        }

        private void OnEnable()
        {
            if (Time.realtimeSinceStartup <= 3f) return;
            StartAll();
        }
        private void OnDisable() { StopAll(); }
        private void OnApplicationQuit() { StopAll(); }
        private void OnDestroy() { StopAll(); }

        private void StartAll()
        {
            if (stop)
            {
                stop = false;
                StartCoroutine(SenderCOR());
                StartCoroutine(CaptureMic());
            }
        }
        private void StopAll()
        {
            stop = true;
            StopAllCoroutines();

            AudioMic.Stop();
            Microphone.End(CurrentDeviceName);

            AppendQueueSendByteFMPCM16.Clear();
            AppendQueueSendBytePCM16.Clear();
        }
#endif
    }
}