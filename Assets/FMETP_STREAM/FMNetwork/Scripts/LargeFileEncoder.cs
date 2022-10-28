using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FMETP
{
    [AddComponentMenu("FMETP/Mapper/LargeFileEncoder")]
    public class LargeFileEncoder : MonoBehaviour
    {
        [Tooltip("target streaming byte per second")]
        [Range(1, 200000)]
        public int TargetBytePerMS = 10000;
        private float targetWaitSecond
        {
            get
            {
                return (((float)streamedLength) / ((float)TargetBytePerMS)) / 1000f;
            }
        }

        private bool stop = false;
        private int streamedLength = 0;
        private Queue<byte[]> _appendQueueSendData = new Queue<byte[]>();
        private Queue<byte[]> _appendQueueChunksBytes = new Queue<byte[]>();

        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();

        [Header("Pair Encoder & Decoder")]
        public UInt16 label = 8001;

        private UInt16 offsetSequence = 0;
        private UInt16 localSequence = 0;
        private UInt16 dataID
        {
            get
            {
                localSequence++;
                if (localSequence > maxSequence) localSequence = 0;
                return (UInt16)(offsetSequence + localSequence);
            }
        }

        private UInt16 maxSequence = 1024;
        private int chunkSize = 1400;//8096; //32768;

        void Start()
        {
            Application.runInBackground = true;
        }

        public void Action_SendLargeByte(byte[] _data)
        {
            _appendQueueSendData.Enqueue(_data);
        }


        IEnumerator CheckQueuedDataCOR()
        {
            while (!stop)
            {
                yield return null;
                while (_appendQueueSendData.Count > 0)
                {
                    yield return null;
                    byte[] _dataByte = _appendQueueSendData.Dequeue();
                    int _length = _dataByte.Length;

                    int _offset = 0;
                    byte[] _meta_label = BitConverter.GetBytes(label);
                    byte[] _meta_id = BitConverter.GetBytes(dataID);
                    byte[] _meta_length = BitConverter.GetBytes(_length);

                    int chunks = Mathf.CeilToInt((float)_length / (float)chunkSize);
                    int metaByteLength = 12;
                    for (int i = 1; i <= chunks; i++)
                    {
                        int dataByteLength = (i == chunks) ? (_length % chunkSize) : (chunkSize);
                        byte[] _meta_offset = BitConverter.GetBytes(_offset);
                        byte[] SendByte = new byte[dataByteLength + metaByteLength];

                        Buffer.BlockCopy(_meta_label, 0, SendByte, 0, 2);
                        Buffer.BlockCopy(_meta_id, 0, SendByte, 2, 2);
                        Buffer.BlockCopy(_meta_length, 0, SendByte, 4, 4);

                        Buffer.BlockCopy(_meta_offset, 0, SendByte, 8, 4);

                        Buffer.BlockCopy(_dataByte, _offset, SendByte, 12, dataByteLength);
                        
                        _appendQueueChunksBytes.Enqueue(SendByte);
                        _offset += chunkSize;
                    }
                }

                while (_appendQueueChunksBytes.Count > 0)
                {
                    byte[] _streamByte = _appendQueueChunksBytes.Dequeue();
                    OnDataByteReadyEvent.Invoke(_streamByte);

                    streamedLength += _streamByte.Length;

                    if (streamedLength > TargetBytePerMS)
                    {
                        yield return new WaitForSecondsRealtime(targetWaitSecond);
                        streamedLength %= TargetBytePerMS;
                    }
                }
            }
        }

        void OnDisable() { StopAll(); }
        void OnApplicationQuit() { StopAll(); }
        void OnDestroy() { StopAll(); }

        private void OnEnable()
        {
            StartAll();
        }

        void StartAll()
        {
            stop = false;
            offsetSequence = (UInt16)(2048 + Mathf.RoundToInt(UnityEngine.Random.Range(0f, 1024f)) * 2048);
            StartCoroutine(CheckQueuedDataCOR());
        }

        void StopAll()
        {
            stop = true;
            StopAllCoroutines();

            _appendQueueSendData.Clear();
            _appendQueueChunksBytes.Clear();
            _appendQueueSendData = new Queue<byte[]>();
            _appendQueueChunksBytes = new Queue<byte[]>();
        }
    }
}