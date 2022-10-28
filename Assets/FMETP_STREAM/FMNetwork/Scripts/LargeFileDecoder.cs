using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FMETP
{
    [AddComponentMenu("FMETP/Mapper/LargeFileDecoder")]
    public class LargeFileDecoder : MonoBehaviour
    {
        public UnityEventByteArray OnReceivedByteArray;

        // Use this for initialization
        void Start()
        {
            Application.runInBackground = true;
        }

        [Header("Pair Encoder & Decoder")]
        public UInt16 label = 8001;
        public class LargeDataBuffer
        {
            public Dictionary<int, int> loadedchunks = new Dictionary<int, int>();
            public byte[] data;

            public int totalLength = 0;
            public int loadedLength = 0;
        }

        private Dictionary<int, LargeDataBuffer> dataBuffers = new Dictionary<int, LargeDataBuffer>();

        public void Action_ProcessData(byte[] _byteData)
        {
            if (_byteData.Length <= 12) return;

            UInt16 _label = BitConverter.ToUInt16(_byteData, 0);
            if (_label != label) return;
            UInt16 _dataID = BitConverter.ToUInt16(_byteData, 2);
            int _length = BitConverter.ToInt32(_byteData, 4);
            int _offset = BitConverter.ToInt32(_byteData, 8);

            LargeDataBuffer _dataBuffer;
            bool needCreateBuffer = false;
            if (dataBuffers.TryGetValue(_dataID, out _dataBuffer))
            {
                //existing buffer
                if (dataBuffers[_dataID].data.Length != _length)
                {
                    //remove wrong data
                    dataBuffers.Remove(_dataID);
                    Debug.LogError(_dataID);

                    needCreateBuffer = true;
                }
            }
            else
            {
                needCreateBuffer = true;
            }

            if (needCreateBuffer)
            {
                //no existing buffer
                //create new buffer
                _dataBuffer = new LargeDataBuffer();
                _dataBuffer.totalLength = _length;
                _dataBuffer.data = new byte[_length];

                //register to buffer dictionary
                dataBuffers.Add(_dataID, _dataBuffer);
            }

            if (!dataBuffers[_dataID].loadedchunks.TryGetValue(_offset, out int value))
            {
                //not existing chunk, new data
                //load new data
                int _chunkSize = _byteData.Length - 12;
                Buffer.BlockCopy(_byteData, 12, dataBuffers[_dataID].data, _offset, _chunkSize);

                dataBuffers[_dataID].loadedchunks.Add(_offset, _chunkSize);
                dataBuffers[_dataID].loadedLength += _chunkSize;
                if (dataBuffers[_dataID].loadedLength == dataBuffers[_dataID].totalLength) StartCoroutine(ProcessDataCOR((int)_dataID));
            }
        }

        IEnumerator ProcessDataCOR(int _dataID)
        {
            byte[] _data = new byte[dataBuffers[_dataID].data.Length];
            Buffer.BlockCopy(dataBuffers[_dataID].data, 0, _data, 0, dataBuffers[_dataID].data.Length);
            dataBuffers.Remove(_dataID);

            yield return null;
            OnReceivedByteArray.Invoke(_data);
        }

        private void OnDisable() { StopAllCoroutines(); }
    }
}