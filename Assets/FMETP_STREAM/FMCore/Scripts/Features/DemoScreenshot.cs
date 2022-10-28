using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace FMETP
{
    public class DemoScreenshot : MonoBehaviour
    {
        enum screenshotSaveFormat { png, jpg }
        [SerializeField] private screenshotSaveFormat saveFormat = screenshotSaveFormat.png;

#if UNITY_EDITOR
        [SerializeField] private bool panorama = false;
#endif
        [SerializeField] private Vector2 panoResolution = new Vector2(4096, 2048);
        [SerializeField] private int quality = 80;

        [HideInInspector] public Material MatPano;
        void Reset() { MatPano = new Material(Shader.Find("Hidden/FMCubemapToEquirect")); }

        // Update is called once per frame
        void Update()
        {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (panorama)
            {
                StartCoroutine(SaveScreenshotPano());
            }
            else
            {
                SaveScreenshot();
            }
        }
#endif
        }

        int order = 0;
        void SaveScreenshot()
        {
            string path = Directory.GetParent(Application.dataPath).ToString() + "/Screenshots/";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            string SavePath = path + SceneManager.GetActiveScene().name + order;

            if (saveFormat == screenshotSaveFormat.png) ScreenCapture.CaptureScreenshot(SavePath + ".png");

            if (saveFormat == screenshotSaveFormat.jpg)
            {
                StartCoroutine(SaveJPG(SavePath));
            }

            order++;
            print(SavePath + (saveFormat == screenshotSaveFormat.png ? ".png" : ".jpg"));
        }

        IEnumerator SaveJPG(string rootPath)
        {
            yield return new WaitForEndOfFrame();
            byte[] saveBytes = ScreenCapture.CaptureScreenshotAsTexture().FMEncodeToJPG(quality, FMChromaSubsamplingOption.Subsampling420);
            File.WriteAllBytes(rootPath + ".jpg", saveBytes);
        }

        IEnumerator SaveScreenshotPano()
        {
            yield return new WaitForEndOfFrame();

            RenderTexture rt_cube = new RenderTexture(2048, 2048, 16, RenderTextureFormat.ARGB32);
            rt_cube.antiAliasing = 1;
            rt_cube.filterMode = FilterMode.Bilinear;
            rt_cube.anisoLevel = 0;
            rt_cube.dimension = TextureDimension.Cube;
            rt_cube.autoGenerateMips = false;

            RenderTexture rt_equirect = new RenderTexture((int)panoResolution.x, (int)panoResolution.y, 16, RenderTextureFormat.ARGB32);
            Camera.main.targetTexture = rt_cube;
            Camera.main.RenderToCubemap(rt_cube, 63, Camera.MonoOrStereoscopicEye.Mono);
            Camera.main.targetTexture = null;
            yield return null;

            Shader.SetGlobalFloat("FORWARD", Camera.main.transform.eulerAngles.y * 0.01745f);
            Graphics.Blit(rt_cube, rt_equirect, MatPano);

            RenderTexture.active = rt_equirect;
            Texture2D pano = new Texture2D(rt_equirect.width, rt_equirect.height, TextureFormat.ARGB32, false);
            pano.ReadPixels(new Rect(0, 0, pano.width, pano.height), 0, 0);
            pano.Apply();
            RenderTexture.active = null;

            byte[] bytes = pano.FMEncodeToJPG(quality);

            string path = Directory.GetParent(Application.dataPath).ToString() + "/Screenshots/";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            string SavePath = path + SceneManager.GetActiveScene().name + order + ".jpg";

            File.WriteAllBytes(SavePath, bytes);

            order++;
            print(SavePath);
        }
    }
}