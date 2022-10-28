using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FMETP
{
    public class OffScreenTrigger : MonoBehaviour
    {
        private WorldToScreenSpace[] WSSs;
        // Start is called before the first frame update
        void Start()
        {
            WSSs = transform.parent.GetComponentsInChildren<WorldToScreenSpace>(true);
            foreach (WorldToScreenSpace WSS in WSSs) WSS.enabled = true;
        }

        public void Action_offscreen()
        {
            foreach (WorldToScreenSpace WSS in WSSs) WSS.InvokeOffScreen();
        }
    }
}