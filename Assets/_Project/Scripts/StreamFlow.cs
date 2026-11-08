using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liuruan
{
    /// R3：溪水原本是單色平面，看起來像藍色地板。
    /// 執行期自動找到 Stream_NorthToSouth，換上程序產生的水紋貼圖（流向條紋＋白色浪花＋岸邊泡沫），
    /// 提高光滑度反光，並沿北→南捲動貼圖做出流動感。不必改場景。
    public class StreamFlow : MonoBehaviour
    {
        const string StreamName = "Stream_NorthToSouth";
        const float Speed = 0.35f; // 每秒捲動的 UV（貼圖 tiling 後約 1.2 m/s）
        Material mat;
        float dir = 1f, offset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Attach(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            SceneManager.sceneLoaded += Attach;
        }

        static void Attach(Scene scene, LoadSceneMode mode)
        {
            var go = GameObject.Find(StreamName);
            if (go && !go.GetComponent<StreamFlow>()) go.AddComponent<StreamFlow>();
        }

        void Start()
        {
            var r = GetComponent<Renderer>();
            if (!r) { enabled = false; return; }
            mat = r.material;
            var tex = MakeTexture(256);
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.88f);
            mat.SetFloat("_Metallic", 0.05f);
            float length = transform.lossyScale.z * 10f, width = transform.lossyScale.x * 10f;
            mat.SetTextureScale("_BaseMap", new Vector2(1f, Mathf.Max(1f, length / width * 0.6f)));
            dir = DownstreamSign();
        }

        // 流向北→南（世界 -Z）：從網格頂點算出 v 增加的方向，讓捲動方向不靠猜。
        float DownstreamSign()
        {
            var mf = GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) return 1f;
            var v = mf.sharedMesh.vertices; var uv = mf.sharedMesh.uv;
            if (uv.Length != v.Length || v.Length < 2) return 1f;
            int a = 0, b = 0;
            for (int i = 1; i < v.Length; i++)
            {
                float za = transform.TransformPoint(v[i]).z;
                if (za > transform.TransformPoint(v[a]).z) a = i;
                if (za < transform.TransformPoint(v[b]).z) b = i;
            }
            // 從北(a)到南(b) v 變大 → 貼圖 offset 要減少，花紋才會往南移
            return uv[b].y > uv[a].y ? -1f : 1f;
        }

        void Update()
        {
            if (!mat) return;
            offset = Mathf.Repeat(offset + dir * Speed * Time.deltaTime, 1f);
            mat.SetTextureOffset("_BaseMap", new Vector2(0f, offset));
        }

        static Texture2D MakeTexture(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "StreamRipples" };
            var deep = new Color(0.07f, 0.33f, 0.45f);
            var shallow = new Color(0.30f, 0.62f, 0.66f);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    // 中央深、兩岸淺
                    float edge = Mathf.Abs(u - 0.5f) * 2f;
                    var c = Color.Lerp(deep, shallow, Mathf.SmoothStep(0.2f, 1f, edge));
                    // 沿流向拉長的波紋（v 方向週期性，確保無縫）
                    float w = Tile(u * 6f, v * 2f, 6f, 2f) * 0.6f + Tile(u * 14f, v * 5f, 14f, 5f) * 0.4f;
                    c = Color.Lerp(c, c * 1.35f, Mathf.SmoothStep(0.55f, 0.8f, w));
                    c = Color.Lerp(c, c * 0.8f, Mathf.SmoothStep(0.45f, 0.2f, w));
                    // 細碎白浪花
                    float f = Tile(u * 24f, v * 10f, 24f, 10f);
                    if (f > 0.78f) c = Color.Lerp(c, new Color(0.9f, 0.96f, 0.97f), (f - 0.78f) * 3.5f);
                    // 岸邊泡沫
                    if (edge > 0.88f) c = Color.Lerp(c, new Color(0.85f, 0.93f, 0.92f), (edge - 0.88f) * 6f * (0.5f + w));
                    c.a = 1f;
                    px[y * n + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        // 可無縫重複的 Perlin：四角混合
        static float Tile(float x, float y, float w, float h)
        {
            float fx = x / w, fy = y / h;
            fx -= Mathf.Floor(fx); fy -= Mathf.Floor(fy);
            float px = fx * w, py = fy * h;
            float a = Mathf.PerlinNoise(px + 17.3f, py + 5.1f);
            float b = Mathf.PerlinNoise(px - w + 17.3f, py + 5.1f);
            float c = Mathf.PerlinNoise(px + 17.3f, py - h + 5.1f);
            float d = Mathf.PerlinNoise(px - w + 17.3f, py - h + 5.1f);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
    }
}
