using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FMETP
{
    public class TouchVisualization : MonoBehaviour
    {
        public GameObject fingerPrefab;
        public List<GameObject> fingers = new List<GameObject>();

        void Start() { fingerPrefab.SetActive(false); }
        void Update()
        {
            if (Input.touchCount > 0)
            {
                Touch[] touches = Input.touches;
                while (fingers.Count <= touches.Length) fingers.Add(Instantiate(fingerPrefab, Vector3.zero, Quaternion.identity, this.transform));
                for (int i = 0; i < fingers.Count; i++)
                {
                    if (i < Input.touchCount)
                    {
                        fingers[i].SetActive(true);
                        fingers[i].GetComponent<RectTransform>().position = new Vector3(touches[i].position.x, touches[i].position.y, 0f);
                    }
                    else
                    {
                        fingers[i].SetActive(false);
                    }
                }
            }
            else
            {
                for (int i = 0; i < fingers.Count; i++) fingers[i].SetActive(false);
            }
        }
    }
}
