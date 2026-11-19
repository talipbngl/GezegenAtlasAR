using UnityEngine;
using System.Collections.Generic;

namespace GezegenAtlasi
{
    public static class PlanetVisuals
    {
        private static readonly string[] Names =
        {
            "mercury", "venus_atmosphere", "earth_daymap", "mars",
            "jupiter", "saturn", "uranus", "neptune"
        };

        public static Texture2D LoadTexture(int index) =>
            Resources.Load<Texture2D>("PlanetTextures/" + Names[index]);

        public static Texture2D RingTexture =>
            Resources.Load<Texture2D>("PlanetTextures/saturn-ring");

        public static Texture2D SunTexture =>
            Resources.Load<Texture2D>("PlanetTextures/sun");

        private static Mesh _sphere;
        public static Mesh SmoothSphere
        {
            get
            {
                if (_sphere != null) return _sphere;
                const int longitude = 96;
                const int latitude = 48;
                var vertices = new Vector3[(longitude + 1) * (latitude + 1)];
                var normals = new Vector3[vertices.Length];
                var uv = new Vector2[vertices.Length];
                var triangles = new int[longitude * latitude * 6];
                for (int y = 0; y <= latitude; y++)
                {
                    float theta = y * Mathf.PI / latitude;
                    for (int x = 0; x <= longitude; x++)
                    {
                        float phi = x * Mathf.PI * 2f / longitude;
                        int i = y * (longitude + 1) + x;
                        normals[i] = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi),
                            Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                        vertices[i] = normals[i] * .5f;
                        uv[i] = new Vector2(1f - (float)x / longitude, 1f - (float)y / latitude);
                    }
                }
                int t = 0;
                for (int y = 0; y < latitude; y++)
                for (int x = 0; x < longitude; x++)
                {
                    int a = y * (longitude + 1) + x;
                    int b = a + longitude + 1;
                    triangles[t++] = a; triangles[t++] = a + 1; triangles[t++] = b;
                    triangles[t++] = a + 1; triangles[t++] = b + 1; triangles[t++] = b;
                }
                _sphere = new Mesh { name = "Gezegen küresi", vertices = vertices, normals = normals,
                    uv = uv, triangles = triangles };
                _sphere.RecalculateBounds();
                return _sphere;
            }
        }

        private static Mesh _ring;
        public static Mesh RingMesh
        {
            get
            {
                if (_ring != null) return _ring;
                const int segments = 128;
                var vertices = new Vector3[(segments + 1) * 4];
                var uv = new Vector2[vertices.Length];
                var triangles = new int[segments * 12];
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    int p = i * 4;
                    vertices[p] = direction * .57f;
                    vertices[p + 1] = direction * 1.22f;
                    vertices[p + 2] = vertices[p];
                    vertices[p + 3] = vertices[p + 1];
                    uv[p] = uv[p + 2] = new Vector2(0, .5f);
                    uv[p + 1] = uv[p + 3] = new Vector2(1, .5f);
                }
                int t = 0;
                for (int i = 0; i < segments; i++)
                {
                    int a = i * 4, b = a + 4;
                    triangles[t++] = a; triangles[t++] = b; triangles[t++] = a + 1;
                    triangles[t++] = a + 1; triangles[t++] = b; triangles[t++] = b + 1;
                    triangles[t++] = a + 2; triangles[t++] = a + 3; triangles[t++] = b + 2;
                    triangles[t++] = a + 3; triangles[t++] = b + 3; triangles[t++] = b + 2;
                }
                _ring = new Mesh { name = "Satürn halkası", vertices = vertices, uv = uv,
                    triangles = triangles };
                _ring.RecalculateNormals();
                _ring.RecalculateBounds();
                return _ring;
            }
        }

        public static GameObject CreatePlanet(int index)
        {
            var item = PlanetCatalog.Planets[index];
            var root = new GameObject(item.Name);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = item.Name + " yüzey";
            sphere.transform.SetParent(root.transform, false);
            sphere.transform.localScale = Vector3.one * .23f;
            sphere.GetComponent<MeshFilter>().sharedMesh = SmoothSphere;
            sphere.transform.localRotation = Quaternion.Euler(0f, 0f, index == 6 ? 98f : index == 5 ? 27f : 0f);
            var texture = LoadTexture(index);
            var material = new Material(Shader.Find("Standard"));
            var owned = root.AddComponent<PlanetOwnedMaterials>();
            owned.Track(material);
            material.mainTexture = texture;
            material.color = texture == null ? item.Color : Color.white;
            material.SetFloat("_Glossiness", index == 2 ? .42f : index < 4 ? .16f : .08f);
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            sphere.AddComponent<PlanetBody>().Initialize(index, .23f, 6f);
            if (index == 5)
            {
                var ring = new GameObject("Satürn halkaları", typeof(MeshFilter), typeof(MeshRenderer));
                ring.transform.SetParent(sphere.transform, false);
                ring.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
                ring.GetComponent<MeshFilter>().sharedMesh = RingMesh;
                var ringMaterial = new Material(Shader.Find("Sprites/Default"));
                owned.Track(ringMaterial);
                ringMaterial.mainTexture = RingTexture;
                ring.GetComponent<MeshRenderer>().sharedMaterial = ringMaterial;
            }
            return root;
        }
    }

    public sealed class PlanetOwnedMaterials : MonoBehaviour
    {
        private readonly List<Material> _materials = new List<Material>();
        public void Track(Material material) => _materials.Add(material);
        private void OnDestroy()
        {
            foreach (var material in _materials)
                if (material != null) Destroy(material);
        }
    }
}
