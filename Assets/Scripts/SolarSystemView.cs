using System;
using System.Collections.Generic;
using UnityEngine;

namespace GezegenAtlasi
{
    public sealed class PlanetBody : MonoBehaviour
    {
        public int Index { get; private set; }
        private float _baseDiameter;
        private float _spin;

        public void Initialize(int index, float diameter, float spin)
        {
            Index = index;
            _baseDiameter = diameter;
            _spin = spin;
            transform.localScale = Vector3.one * diameter;
        }

        public void SetSelected(bool selected)
        {
            transform.localScale = Vector3.one * _baseDiameter * (selected ? 1.22f : 1f);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, _spin * Time.deltaTime, Space.Self);
        }
    }

    public sealed class SolarSystemView : MonoBehaviour
    {
        private readonly List<PlanetBody> _bodies = new List<PlanetBody>();
        private readonly List<Material> _materials = new List<Material>();
        private readonly List<float> _phases = new List<float>();
        private int _selected = 2;
        private float _orbitSpeed = 1f;

        public static SolarSystemView Create(Transform parent)
        {
            var root = new GameObject("Güneş Sistemi • ölçekli temsil");
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<SolarSystemView>();
            view.Build();
            return view;
        }

        public void Select(int index)
        {
            _selected = Mathf.Clamp(index, 0, _bodies.Count - 1);
            for (int i = 0; i < _bodies.Count; i++)
                _bodies[i].SetSelected(i == _selected);
        }

        public void SetOrbitSpeed(float speed) => _orbitSpeed = speed;

        private void Update()
        {
            if (_orbitSpeed <= 0f) return;
            for (int i = 0; i < _bodies.Count; i++)
            {
                _phases[i] += Time.deltaTime * _orbitSpeed * .28f / (1f + i * .36f);
                float radius = PlanetCatalog.Planets[i].Orbit;
                var p = _bodies[i].transform.localPosition;
                _bodies[i].transform.localPosition = new Vector3(Mathf.Cos(_phases[i]) * radius,
                    p.y, Mathf.Sin(_phases[i]) * radius);
            }
        }

        private void Build()
        {
            MakeSun();
            for (int i = 0; i < PlanetCatalog.Planets.Length; i++)
            {
                PlanetInfo item = PlanetCatalog.Planets[i];
                MakeOrbit(item.Orbit, i);

                float theta = item.Angle * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(theta) * item.Orbit,
                    item.Radius + .012f, Mathf.Sin(theta) * item.Orbit);
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = item.Name;
                sphere.transform.SetParent(transform, false);
                sphere.transform.localPosition = position;
                sphere.transform.localRotation = Quaternion.Euler(0f, 0f, i == 6 ? 98f : i == 5 ? 27f : 0f);
                sphere.GetComponent<MeshFilter>().sharedMesh = PlanetVisuals.SmoothSphere;

                var body = sphere.AddComponent<PlanetBody>();
                body.Initialize(i, item.Radius * 2f, 7f + i * 1.7f);
                sphere.GetComponent<Renderer>().sharedMaterial = MakePlanetMaterial(item, i);
                _bodies.Add(body);
                _phases.Add(theta);

                if (i == 5) MakeSaturnRings(sphere.transform, item.Color);
            }
            Select(_selected);
        }

        private void MakeSun()
        {
            var sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sun.name = "Güneş";
            sun.transform.SetParent(transform, false);
            sun.transform.localPosition = new Vector3(0f, .045f, 0f);
            sun.transform.localScale = Vector3.one * .09f;
            sun.GetComponent<MeshFilter>().sharedMesh = PlanetVisuals.SmoothSphere;
            Destroy(sun.GetComponent<Collider>());
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.white;
            material.mainTexture = PlanetVisuals.SunTexture;
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", PlanetVisuals.SunTexture);
            material.SetColor("_EmissionColor", new Color(1f, .65f, .32f) * .55f);
            sun.GetComponent<Renderer>().sharedMaterial = material;
            _materials.Add(material);

            var glow = new GameObject("Güneş ışığı");
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = new Vector3(0f, .10f, 0f);
            var light = glow.AddComponent<PointLightPlaceholder>();
            light.Setup();
        }

        private void MakeOrbit(float radius, int index)
        {
            var go = new GameObject("Yörünge " + (index + 1));
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            _materials.Add(mat);
            line.sharedMaterial = mat;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 120;
            line.widthMultiplier = index == 2 ? .0024f : .0016f;
            line.numCornerVertices = 2;
            line.startColor = index == 2 ? new Color(.37f, .8f, 1f, .62f) : new Color(.62f, .75f, .9f, .35f);
            line.endColor = line.startColor;
            for (int j = 0; j < 120; j++)
            {
                float angle = j * Mathf.PI * 2f / 120f;
                line.SetPosition(j, new Vector3(Mathf.Cos(angle) * radius, .005f,
                    Mathf.Sin(angle) * radius));
            }
        }

        private Material MakePlanetMaterial(PlanetInfo item, int index)
        {
            var texture = PlanetVisuals.LoadTexture(index);
            var material = new Material(Shader.Find("Standard"));
            material.mainTexture = texture;
            material.color = texture == null ? item.Color : Color.white;
            material.SetFloat("_Glossiness", index == 2 ? .42f : index < 4 ? .16f : .08f);
            _materials.Add(material);
            return material;
        }

        private void MakeSaturnRings(Transform planet, Color color)
        {
            var ring = new GameObject("Satürn halkaları");
            ring.transform.SetParent(planet, false);
            ring.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            var filter = ring.AddComponent<MeshFilter>();
            var renderer = ring.AddComponent<MeshRenderer>();
            filter.sharedMesh = PlanetVisuals.RingMesh;
            var material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = PlanetVisuals.RingTexture;
            material.color = Color.white;
            renderer.sharedMaterial = material;
            _materials.Add(material);
        }

        private void OnDestroy()
        {
            foreach (var material in _materials) if (material) Destroy(material);
        }
    }

    // Sunlight is local to the display so the tabletop scene remains readable indoors.
    public sealed class PointLightPlaceholder : MonoBehaviour
    {
        public void Setup()
        {
            Light light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, .72f, .46f);
            light.range = 1.4f;
            light.intensity = 1.7f;
        }
    }
}
