using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FMETP
{
    [AddComponentMenu("FMETP/Mapper/TouchDecoder")]
    public class TouchDecoder : MonoBehaviour
    {
        public UInt16 label = 9001;

        private int touchCount = 0;
        private Touch[] touches;
        public int TouchCount { get { return touchCount; } }
        public Touch[] Touches { get { return touches; } }

        public UnityEventInputTouch OnTouchUpdatedEvent = new UnityEventInputTouch();

        private void Start()
        {
            touchCount = 0;
            touches = new Touch[10];
        }

        public void Action_ProcessTouchesData(byte[] byteData)
        {
            if (!enabled) return;
            if (byteData.Length <= 2) return;

            UInt16 _label = BitConverter.ToUInt16(byteData, 0);
            if (_label != label) return;

            touchCount = (int)byteData[2];
            if (touchCount > 0)
            {
                for (int i = 0; i < touchCount; i++)
                {
                    int index = 3 + (i * (9)); //2 + 1 + (i * (1+4+4))
                    switch (byteData[index])
                    {
                        case 0: touches[i].phase = TouchPhase.Began; break;
                        case 1: touches[i].phase = TouchPhase.Moved; break;
                        case 2: touches[i].phase = TouchPhase.Stationary; break;
                        case 3: touches[i].phase = TouchPhase.Ended; break;
                        case 4: touches[i].phase = TouchPhase.Canceled; break;
                    }

                    Vector2 position = new Vector2(BitConverter.ToSingle(byteData, index + 1), BitConverter.ToSingle(byteData, index + 5));

                    touches[i].deltaPosition = position - touches[i].position;
                    touches[i].position = position;
                    
                }

                Touch[] updatedTouches = new Touch[touchCount];
                for(int i = 0; i<updatedTouches.Length; i++)
                {
                    updatedTouches[i].phase = touches[i].phase;
                    updatedTouches[i].position = touches[i].position;
                    updatedTouches[i].deltaPosition = touches[i].deltaPosition;
                }
                OnTouchUpdatedEvent.Invoke(updatedTouches);
            }
            else
            {
                OnTouchUpdatedEvent.Invoke(new Touch[0]);
            }
        }
    }
}