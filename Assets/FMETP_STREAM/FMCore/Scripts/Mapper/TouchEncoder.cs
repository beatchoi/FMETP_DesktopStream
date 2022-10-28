using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FMETP
{
    [AddComponentMenu("FMETP/Mapper/TouchEncoder")]
    public class TouchEncoder : MonoBehaviour
    {
        public int TouchCount = 0;
        private List<byte> SendByte;
        public UInt16 label = 9001;
        public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();

        private bool detectedTouch = false;

        void Update()
        {
            TouchCount = Input.touchCount;
            if (TouchCount > 10) TouchCount = 10; //maximum 10 finger touches
            if (TouchCount > 0) detectedTouch = true;
            if (detectedTouch)
            {
                SendByte = new List<byte>();
                SendByte.AddRange(BitConverter.GetBytes(label));
                SendByte.Add((byte)TouchCount);

                for (int i = 0; i < TouchCount; i++)
                {
                    byte phaseByte = (byte)0;
                    switch (Input.touches[i].phase)
                    {
                        case TouchPhase.Began: phaseByte = (byte)0; break;
                        case TouchPhase.Moved: phaseByte = (byte)1; break;
                        case TouchPhase.Stationary: phaseByte = (byte)2; break;
                        case TouchPhase.Ended: phaseByte = (byte)3; break;
                        case TouchPhase.Canceled: phaseByte = (byte)4; break;
                    }
                    SendByte.Add(phaseByte);

                    Vector2 position = Input.touches[i].position;
                    SendByte.AddRange(BitConverter.GetBytes(position.x));
                    SendByte.AddRange(BitConverter.GetBytes(position.y));
                }

                OnDataByteReadyEvent.Invoke(SendByte.ToArray());
                SendByte.Clear();

                if (TouchCount == 0) detectedTouch = false; //end...
            }
        }
    }
}