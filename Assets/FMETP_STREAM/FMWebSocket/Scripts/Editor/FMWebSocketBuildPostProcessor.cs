using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace FMETP
{
    public class FMWebSocketBuildPostProcessor
    {
#if UNITY_WEBGL
        [PostProcessBuildAttribute(1)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            //fix missing game instance variable, which found in Unity2020
            bool needUpdateIndex = false;
            string indexPath = Path.Combine(path, "index.html");
            string[] lines = File.ReadAllLines(indexPath);
            List<string> writeLines = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                writeLines.Add(lines[i]);
                if (lines[i].Contains(".then((unityInstance) => {"))
                {
                    writeLines.Add("window.gameInstance = unityInstance;");
                    needUpdateIndex = true;
                }
            }

            if (needUpdateIndex)
            {
                File.WriteAllLines(indexPath, writeLines.ToArray());
                Debug.Log("[FMETP STREAM WebGL Post Process Build] modified: " + indexPath);
            }

            //fix Mac OS SUR bug
            string[] filePathsJS = Directory.GetFiles(path, "*.js", SearchOption.AllDirectories);
            foreach (string file in filePathsJS)
            {
                if (file.ToLower().Contains("loader.js"))
                {
                    string text = File.ReadAllText(file);
                    if (text.Contains(@"Mac OS X (10[\.\_\d]+)"))
                    {
                        text = text.Replace(@"Mac OS X (10[\.\_\d]+)", @"Mac OS X (1[\.\_\d][\.\_\d]+)");
                        File.WriteAllText(file, text);
                        Debug.Log("[FMETP STREAM WebGL Post Process Build] modified: " + file);
                    }
                }
            }

        }
#endif
    }
}
