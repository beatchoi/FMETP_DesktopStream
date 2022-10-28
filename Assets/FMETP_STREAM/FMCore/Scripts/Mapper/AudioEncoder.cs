using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace FMETP
{
    public enum AudioOutputFormat { FMPCM16, PCM16 }
    public enum AudioReadMethod { OnAudioFilterRead, AudioListenerGetOutputData }

    [AddComponentMenu("FMETP/Mapper/AudioEncoder")]
    public class AudioEncoder : MonoBehaviour
    {
        #region EditorProps
        public bool EditorShowCapture = true;
        public bool EditorShowAudioInfo = true;
        public bool EditorShowEncoded = true;
        public bool EditorShowPairing = true;
        #endregion

        //----------------------------------------------
        private AudioListener[] AudioListenerObject;
        private bool isReadingOrEncodingAudioBuffer = false;

        private ConcurrentQueue<byte> AudioBytes = new ConcurrentQueue<byte>();
        private ConcurrentQueue<float> AudioBuffer = new ConcurrentQueue<float>();

        //[Header("[Capture In-Game Sound]")]
        public bool StreamGameSound = true;
        public int SystemSampleRate = 48000;
        [Range(1, 8)]
        public int SystemChannels = 2;
        public bool ForceMono = true;

        [Tooltip("Mute the local audio playback when the In-Game audio is streaming")]
        public bool MuteLocalAudioPlayback = false;
        public AudioReadMethod AudioReadMode = AudioReadMethod.OnAudioFilterRead;
        //----------------------------------------------

        [Range(1f, 60f)]
        public float StreamFPS = 20f;
        private float interval = 0.05f;

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
            Application.runInBackground = true;

            SystemSampleRate = AudioSettings.GetConfiguration().sampleRate;

            if (GetComponent<AudioListener>() == null) this.gameObject.AddComponent<AudioListener>();
            AudioListenerObject = FindObjectsOfType<AudioListener>();
            for (int i = 0; i < AudioListenerObject.Length; i++)
            {
                if (AudioListenerObject[i] != null) AudioListenerObject[i].enabled = (AudioListenerObject[i].gameObject == this.gameObject);
            }
            StartCoroutine(SenderCOR());
        }

        public int OutputSampleRate = 24000;
        public int OutputChannels = 2;
        public bool MatchSystemSampleRate = true;
        public int TargetSampleRate = 24000;
        private float sampleRateScalar = 0f;
        private float scaledStep = 0f;
        private float lastAudioBuffer = 0f;

        private long _readCount = 0;
        public int ReadCount
        {
            get { return Convert.ToInt32(Interlocked.Read(ref _readCount)); }
            set { Interlocked.Exchange(ref _readCount, (long)value); }
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

        private void OnAudioFilterRead(float[] data, int channels)
        {
            OutputSampleRate = MatchSystemSampleRate ? SystemSampleRate : TargetSampleRate;
            OutputChannels = ForceMono ? 1 : SystemChannels;
            SystemChannels = channels;

            if (StreamGameSound)
            {
                if (AudioReadMode == AudioReadMethod.OnAudioFilterRead)
                {
                    ReadCount = 0;

                    int step = ForceMono ? channels : 1;
                    int dataLength = data.Length;
                    if (MatchSystemSampleRate)
                    {
                        for (int i = 0; i < dataLength; i += step) AudioBuffer.Enqueue(data[i]);
                    }
                    else
                    {
                        sampleRateScalar = (float)SystemSampleRate / (float)TargetSampleRate;
                        scaledStep %= 1f;
                        do
                        {
                            float currentAudioBuffer = data[(int)scaledStep];
                            AudioBuffer.Enqueue(Mathf.Lerp(lastAudioBuffer, currentAudioBuffer, scaledStep % 1f));
                            lastAudioBuffer = currentAudioBuffer;
                            scaledStep += step * sampleRateScalar;
                        } while (scaledStep < dataLength);
                    }

                    if (MuteLocalAudioPlayback) Array.Copy(new float[dataLength], data, dataLength);
                }
                else if (AudioReadMode == AudioReadMethod.AudioListenerGetOutputData)
                {
                    ReadCount += data.Length / channels;
                }
            }
        }

        private IEnumerator EncodeAudioBufferCOR()
        {
            while (!stop)
            {
                if (AudioReadMode == AudioReadMethod.OnAudioFilterRead)
                {
                    //Dequeue buffered audio data from OnAudioFilterRead(), and Encode Bytes in main thread
                    int audioBufferCount = AudioBuffer.Count;
                    if (audioBufferCount > 0)
                    {
                        //skip extra queued data, make sure there is no accumulated audio buffer
                        while (audioBufferCount > OutputSampleRate)
                        {
                            if (AudioBuffer.TryDequeue(out float removedAudioBuffer)) audioBufferCount--;
                        }

                        isReadingOrEncodingAudioBuffer = true;
                        do
                        {
                            if (AudioBuffer.TryDequeue(out float _data))
                            {
                                if (OutputFormat == AudioOutputFormat.FMPCM16)
                                {
                                    byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(_data));
                                    foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                }
                                else
                                {
                                    //byte[] byteData = BitConverter.GetBytes(_data);
                                    byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(_data));
                                    foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                }

                                audioBufferCount--;
                            }
                        } while (audioBufferCount > 0);
                        isReadingOrEncodingAudioBuffer = false;
                    }
                }
                else if (AudioReadMode == AudioReadMethod.AudioListenerGetOutputData)
                {
                    //Read audio buffer from AudioListener.GetOutputData() directly and Encode Bytes in the main thread
                    if (ReadCount > 0)
                    {
                        /*
                        //for unknown reason, this may create some lag in playback unexpected
                        int _readCount = 1;
                        while (ReadCount >= _readCount) _readCount *= 2;
                        _readCount /= 2;
                        ReadCount -= _readCount;
                        */

                        int _readCount = ReadCount;
                        ReadCount = 0;

                        if (!StreamGameSound) _readCount = 0; //skip data if not streaming
                        if (_readCount > OutputSampleRate) _readCount = OutputSampleRate; //skip overloaded buffer...

                        if (_readCount > 0)
                        {
                            isReadingOrEncodingAudioBuffer = true;
                            List<float[]> data = new List<float[]>();
                            for (int i = 0; i < OutputChannels; i++)
                            {
                                data.Add(new float[_readCount]);
                                AudioListener.GetOutputData(data[i], i);
                            }

                            if (MatchSystemSampleRate)
                            {
                                for (int i = 0; i < _readCount; i++)
                                {
                                    for (int targetChannel = 0; targetChannel < OutputChannels; targetChannel++)
                                    {
                                        if (OutputFormat == AudioOutputFormat.FMPCM16)
                                        {
                                            byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(data[targetChannel][i]));
                                            foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                        }
                                        else
                                        {
                                            byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(data[targetChannel][i]));
                                            foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                sampleRateScalar = (float)SystemSampleRate / (float)TargetSampleRate;
                                scaledStep %= 1f;

                                do
                                {
                                    for (int targetChannel = 0; targetChannel < OutputChannels; targetChannel++)
                                    {
                                        float currentAudioBuffer = data[targetChannel][(int)scaledStep];

                                        lastAudioBuffer = currentAudioBuffer;
                                        scaledStep += sampleRateScalar;

                                        if (OutputFormat == AudioOutputFormat.FMPCM16)
                                        {
                                            byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(Mathf.Lerp(lastAudioBuffer, currentAudioBuffer, scaledStep % 1f)));
                                            foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                        }
                                        else
                                        {
                                            byte[] byteData = BitConverter.GetBytes(FMCoreTools.FloatToInt16(Mathf.Lerp(lastAudioBuffer, currentAudioBuffer, scaledStep % 1f)));
                                            foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                                        }
                                    }
                                } while (scaledStep < _readCount);
                            }
                            isReadingOrEncodingAudioBuffer = false;
                        }
                    }
                }
                yield return null;
            }
        }

        private IEnumerator SenderCOR()
        {
            StartCoroutine(InvokeEventsCheckerCOR());

            yield return null; //just skip one frame, in case that you want to change audio read mode

            StartCoroutine(EncodeAudioBufferCOR());
            while (!stop)
            {
                if (Time.realtimeSinceStartup > next)
                {
                    interval = 1f / StreamFPS;
                    next = Time.realtimeSinceStartup + interval;

                    //check if reading buffer? and check if there is any audio bytes?
                    while (isReadingOrEncodingAudioBuffer || AudioBytes.Count <= 0) yield return null;

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
            }

            if (AudioListenerObject != null)
            {
                for (int i = 0; i < AudioListenerObject.Length; i++)
                {
                    if (AudioListenerObject[i] != null) AudioListenerObject[i].enabled = (AudioListenerObject[i].gameObject == this.gameObject);
                }
            }
        }
        private void StopAll()
        {
            stop = true;
            StopAllCoroutines();

            AppendQueueSendByteFMPCM16.Clear();
            AppendQueueSendBytePCM16.Clear();

            //reset listener
            if (AudioListenerObject != null)
            {
                for (int i = 0; i < AudioListenerObject.Length; i++)
                {
                    if (AudioListenerObject[i] != null) AudioListenerObject[i].enabled = (AudioListenerObject[i].gameObject != this.gameObject);
                }
            }
        }
    }
}