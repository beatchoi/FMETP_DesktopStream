using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public class UnityEventFloat: UnityEvent<float> { }
[System.Serializable]
public class UnityEventInt : UnityEvent<int> { }
[System.Serializable]
public class UnityEventBool : UnityEvent<bool> { }
[System.Serializable]
public class UnityEventString : UnityEvent<string> { }

[System.Serializable]
public class UnityEventByteArray: UnityEvent<byte[]> { }

[System.Serializable]
public class UnityEventFloatArray : UnityEvent<float[]> { }

[System.Serializable]
public class UnityEventTexture : UnityEvent<Texture> { }
[System.Serializable]
public class UnityEventTexture2D : UnityEvent<Texture2D> { }
[System.Serializable]
public class UnityEventRenderTexture : UnityEvent<RenderTexture> { }
[System.Serializable]
public class UnityEventWebcamTexture : UnityEvent<WebCamTexture> { }


[System.Serializable]
public class UnityEventInputTouch : UnityEvent<Touch[]> { }

[System.Serializable]
public class UnityEventRect : UnityEvent<Rect> { }

public class UnityEventClass : MonoBehaviour
{

}
