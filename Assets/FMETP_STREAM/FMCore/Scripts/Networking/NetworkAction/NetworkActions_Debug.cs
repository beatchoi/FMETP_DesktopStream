using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FMETP
{
    public class NetworkActions_Debug : MonoBehaviour
    {

        public Text text;
        public void Action_TextUpdate(string _value) { text.text = _value; }
    }
}
