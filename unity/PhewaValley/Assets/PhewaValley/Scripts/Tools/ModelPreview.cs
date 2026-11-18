// Renders a thumbnail of every .glb in a folder (used to identify Tripo downloads):
//   PhewaValley.exe -phewa-preview <input folder> <output folder>
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace PhewaValley
{
    public class ModelPreview : MonoBehaviour
    {
        public string input, output;

        IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            var cam = new GameObject("Preview Camera").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.82f, .84f, .86f); cam.fieldOfView = 30;
            var sun = new GameObject("Light").AddComponent<Light>(); sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(40, -35, 0); sun.intensity = 1.2f;
            RenderSettings.ambientLight = new Color(.55f, .55f, .58f);
            var rt = new RenderTexture(640, 640, 24); cam.targetTexture = rt; var tex = new Texture2D(640, 640, TextureFormat.RGB24, false);
            foreach (var path in Directory.GetFiles(input, "*.glb"))
            {
                var holder = new GameObject("preview"); var task = Load(path, holder.transform);
                while (!task.IsCompleted) yield return null;
                if (!task.Result) { Debug.Log($"[preview] FAILED {Path.GetFileName(path)}"); Destroy(holder); continue; }
                yield return null;
                var rs = holder.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float radius = b.extents.magnitude; var dir = Quaternion.Euler(18, 35, 0) * Vector3.back;
                cam.transform.position = b.center - dir * (radius / Mathf.Sin(cam.fieldOfView * .5f * Mathf.Deg2Rad)); cam.transform.LookAt(b.center);
                cam.nearClipPlane = radius * .05f; cam.farClipPlane = radius * 10;
                cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); tex.Apply(); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(output, Path.GetFileNameWithoutExtension(path) + ".png"), tex.EncodeToPNG());
                Debug.Log($"[preview] {Path.GetFileName(path)} size={b.size}");
                Destroy(holder); yield return null;
            }
            Debug.Log("[preview] done"); Application.Quit();
        }

        static async Task<bool> Load(string path, Transform parent)
        {
            var import = new GltfImport();
            return await import.Load(new Uri(path).AbsoluteUri) && await import.InstantiateMainSceneAsync(parent);
        }
    }
}
