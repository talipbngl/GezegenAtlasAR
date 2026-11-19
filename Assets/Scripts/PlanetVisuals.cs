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

        private static Mesh _quad;
        public static Mesh RingQuad
        {
            get
            {
                if (_quad != null) return _quad;
                _quad = new Mesh
                {
                    vertices = new[]
                    {
                        new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0),
                        new Vector3(-.5f,.5f,0), new Vector3(.5f,.5f,0)
                    },
                    uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one },
                    triangles = new[] { 0, 2, 1, 1, 2, 3 }
                };
                _quad.RecalculateNormals();
                return _quad;
            }
        }

        public static GameObject CreatePlanet(int index)
        {
            var item = PlanetCatalog.Planets[index];
            var root = new GameObject(item.Name);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = item.Name + " yüzey";
            sphere.transform.SetParent(root.transform, false);
            sphere.transform.localScale = Vector3.one * .16f;
            var texture = LoadTexture(index);
            var material = new Material(Shader.Find("Standard"));
            var owned = root.AddComponent<PlanetOwnedMaterials>();
            owned.Track(material);
            material.mainTexture = texture;
            material.color = texture == null ? item.Color : Color.white;
            material.SetFloat("_Glossiness", .22f);
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            sphere.AddComponent<PlanetBody>().Initialize(index, .16f, 6f);
            if (index == 5)
            {
                var ring = new GameObject("Satürn halkaları", typeof(MeshFilter), typeof(MeshRenderer));
                ring.transform.SetParent(sphere.transform, false);
                ring.transform.localRotation = Quaternion.Euler(68f, 0, 14f);
                ring.transform.localScale = Vector3.one * 1.9f;
                ring.GetComponent<MeshFilter>().sharedMesh = RingQuad;
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
