using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace GezegenAtlasi
{
    public sealed class PlanetApp : MonoBehaviour
    {
        [SerializeField] private GameObject arRig;
        [SerializeField] private GameObject arSessionObject;
        [SerializeField] private Camera arCamera;
        [SerializeField] private AROcclusionManager occlusionManager;
        [SerializeField] private Camera previewCamera;

        private sealed class PlacedItem
        {
            public GameObject root;
            public int index;
            public float radius;
        }

        private readonly List<PlacedItem> _objects = new List<PlacedItem>();
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private AppUI _ui;
        private SolarSystemView _preview;
        private GameObject _previewHero;
        private int _previewHeroIndex = -1;
        private PlacedItem _selectedObject;
        private PlacedItem _draggedObject;
        private bool _arMode;
        private bool _switching;
        private bool _systemMode;
        private int _selectedPlanet = 2;
        private float _orbitSpeed = 1f;
        private float _lastPinch;
        private float _dragDistance;
        private float _lastStatusUpdate;
        private Vector2 _lastTouch;
        private Rect _safeArea;

        public void Configure(GameObject rig, GameObject session, Camera ar, AROcclusionManager occlusion, Camera preview)
        {
            arRig = rig;
            arSessionObject = session;
            arCamera = ar;
            occlusionManager = occlusion;
            previewCamera = preview;
        }

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
            EnhancedTouchSupport.Enable();
            arRig.SetActive(false);
            arSessionObject.SetActive(false);
            previewCamera.gameObject.SetActive(true);
            _preview = SolarSystemView.Create(transform);
            _preview.Select(_selectedPlanet);
            ShowPreviewHero(_selectedPlanet);
            _ui = new AppUI(ToggleMode, CenterAll, DeleteSelected, SelectPlanet, UseSystem, CycleOrbitSpeed);
            _ui.SetARMode(false, false);
            _ui.SetStatus("3B ÖNİZLEME");
            _ui.SetMode(false);
            _safeArea = Screen.safeArea;
        }

        private void OnDestroy()
        {
            EnhancedTouchSupport.Disable();
            if (_ui != null) _ui.Dispose();
        }

        private void Update()
        {
            if (Screen.safeArea != _safeArea)
            {
                _safeArea = Screen.safeArea;
                _ui.FitSafeArea();
            }
            UpdateOcclusionStatus();
            HandleTouch();
#if UNITY_EDITOR
            HandleMouseInEditor();
#endif
        }

        private void ToggleMode()
        {
            if (_switching) return;
            if (_arMode) ExitAR();
            else StartCoroutine(EnterAR());
        }

        private IEnumerator EnterAR()
        {
            _switching = true;
            _ui.SetStatus("AR DESTEĞİ KONTROL EDİLİYOR");
            arRig.SetActive(true);
            arSessionObject.SetActive(true);
            yield return ARSession.CheckAvailability();
            if (ARSession.state == ARSessionState.NeedsInstall)
                yield return ARSession.Install();
            if (ARSession.state == ARSessionState.Unsupported ||
                ARSession.state == ARSessionState.None ||
                ARSession.state == ARSessionState.NeedsInstall)
            {
                arSessionObject.SetActive(false);
                arRig.SetActive(false);
                _ui.SetStatus("AR YOK · 3B ÖNİZLEME AÇIK");
                _switching = false;
                yield break;
            }
            _arMode = true;
            previewCamera.gameObject.SetActive(false);
            _preview.gameObject.SetActive(false);
            if (occlusionManager != null)
                occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
            _ui.SetARMode(true, false);
            _ui.SetOcclusion("DERİNLİK KONTROL EDİLİYOR");
            _ui.SetStatus("KAMERADA İSTEDİĞİN NOKTAYA DOKUN");
            _switching = false;
        }

        private void ExitAR()
        {
            _arMode = false;
            foreach (var item in _objects) if (item.root != null) Destroy(item.root);
            _objects.Clear();
            _selectedObject = null;
            _draggedObject = null;
            arSessionObject.SetActive(false);
            arRig.SetActive(false);
            previewCamera.gameObject.SetActive(true);
            _preview.gameObject.SetActive(true);
            if (_previewHero != null) _previewHero.SetActive(!_systemMode);
            _ui.SetARMode(false, false);
            _ui.SetOcclusion("");
            _ui.SetStatus("3B ÖNİZLEME");
        }

        private void UpdateOcclusionStatus()
        {
            if (!_arMode || Time.unscaledTime < _lastStatusUpdate + .5f) return;
            _lastStatusUpdate = Time.unscaledTime;
            bool available = occlusionManager != null && occlusionManager.descriptor != null &&
                occlusionManager.descriptor.environmentDepthImageSupported == Supported.Supported;
            bool active = available && occlusionManager.currentEnvironmentDepthMode != EnvironmentDepthMode.Disabled;
            _ui.SetOcclusion(active ? "GERÇEK NESNE ÖRTMESİ AÇIK" :
                available ? "DERİNLİK BEKLENİYOR" : "EL ÖRTMESİ BU CİHAZDA YOK");
        }

        private void SelectPlanet(int index)
        {
            _selectedPlanet = index;
            _systemMode = false;
            _preview.Select(index);
            ShowPreviewHero(index);
            _ui.SetMode(false);
            _ui.SelectPlanet(index);
        }

        private void ShowPreviewHero(int index)
        {
            if (_previewHeroIndex != index || _previewHero == null)
            {
                if (_previewHero != null) Destroy(_previewHero);
                _previewHero = PlanetVisuals.CreatePlanet(index);
                _previewHero.name = "Yakın gezegen: " + PlanetCatalog.Planets[index].Name;
                _previewHero.transform.SetParent(previewCamera.transform, false);
                _previewHero.transform.localPosition = new Vector3(0f, .07f, 1.15f);
                _previewHero.transform.localRotation = Quaternion.Euler(-8f, 28f, 0f);
                _previewHero.transform.localScale = Vector3.one * (index == 5 ? 1.25f : 2f);
                _previewHeroIndex = index;
            }
            _previewHero.SetActive(!_arMode && !_systemMode);
        }

        private void UseSystem()
        {
            _systemMode = true;
            _selectedObject = null;
            if (_previewHero != null) _previewHero.SetActive(false);
            _ui.SetMode(true);
            _ui.SetObjectActions(_objects.Count, false);
        }

        private void CycleOrbitSpeed()
        {
            _orbitSpeed = _orbitSpeed == 1f ? 2f : _orbitSpeed == 2f ? 0f : 1f;
            _preview.SetOrbitSpeed(_orbitSpeed);
            foreach (var item in _objects)
                if (item.index == -1 && item.root != null)
                    item.root.GetComponent<SolarSystemView>()?.SetOrbitSpeed(_orbitSpeed);
            _ui.SetOrbitSpeed(_orbitSpeed);
        }

        private void Place(Vector2 screenPoint)
        {
            if (!_arMode || _objects.Count >= 16) return;
            Ray ray = arCamera.ScreenPointToRay(screenPoint);
            Vector3 point = ray.GetPoint(_systemMode ? 1.6f : 1.25f);
            float radius = _systemMode ? .56f : .13f;
            foreach (var item in _objects)
                if (Vector3.Distance(item.root.transform.position, point) < item.radius * item.root.transform.localScale.x + radius + .045f)
                {
                    _ui.SetStatus("BURADA BAŞKA GEZEGEN VAR");
                    return;
                }
            GameObject root;
            if (_systemMode)
            {
                var view = SolarSystemView.Create(transform);
                view.SetOrbitSpeed(_orbitSpeed);
                view.Select(_selectedPlanet);
                root = view.gameObject;
            }
            else
            {
                root = PlanetVisuals.CreatePlanet(_selectedPlanet);
                root.transform.SetParent(transform, true);
            }
            root.transform.position = point;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            var placed = new PlacedItem { root = root, index = _systemMode ? -1 : _selectedPlanet, radius = radius };
            _objects.Add(placed);
            _selectedObject = placed;
            _ui.SetARMode(true, true);
            _ui.SetObjectActions(_objects.Count, true);
            _ui.SetStatus(_systemMode ? "YÖRÜNGELİ SİSTEM YERLEŞTİRİLDİ" : "GEZEGEN YERLEŞTİRİLDİ");
        }

        private PlacedItem Pick(Vector2 screenPoint)
        {
            Camera camera = _arMode ? arCamera : previewCamera;
            if (camera == null || !camera.gameObject.activeInHierarchy) return null;
            if (!Physics.Raycast(camera.ScreenPointToRay(screenPoint), out RaycastHit hit, 20f)) return null;
            var body = hit.collider.GetComponent<PlanetBody>();
            if (body == null) return null;
            _selectedPlanet = body.Index;
            _preview.Select(_selectedPlanet);
            _ui.SelectPlanet(_selectedPlanet);
            if (!_arMode)
            {
                ShowPreviewHero(_selectedPlanet);
                return null;
            }
            foreach (var item in _objects)
            {
                if (!hit.transform.IsChildOf(item.root.transform)) continue;
                _selectedObject = item;
                _ui.SetObjectActions(_objects.Count, true);
                return item;
            }
            return null;
        }

        private void CenterAll()
        {
            if (!_arMode || _objects.Count == 0) return;
            Vector3 centroid = Vector3.zero;
            foreach (var item in _objects) centroid += item.root.transform.position;
            centroid /= _objects.Count;
            Vector3 target = arCamera.ViewportPointToRay(new Vector3(.5f, .5f)).GetPoint(1.5f);
            Vector3 offset = target - centroid;
            foreach (var item in _objects) item.root.transform.position += offset;
            _ui.SetStatus("NESNELER KAMERAYA ORTALANDI");
        }

        private void DeleteSelected()
        {
            if (_selectedObject == null) return;
            _objects.Remove(_selectedObject);
            Destroy(_selectedObject.root);
            _selectedObject = null;
            _ui.SetARMode(_arMode, _objects.Count > 0);
            _ui.SetObjectActions(_objects.Count, false);
            _ui.SetStatus("SEÇİLİ NESNE SİLİNDİ");
        }

        private void HandleTouch()
        {
            var touches = Touch.activeTouches;
            int count = 0;
            Touch first = default, second = default;
            foreach (var touch in touches)
            {
                if (touch.ended) continue;
                if (count == 0) first = touch;
                else if (count == 1) second = touch;
                count++;
            }
            if (count >= 2)
            {
                float distance = Vector2.Distance(first.screenPosition, second.screenPosition);
                var objectToScale = _arMode ? _selectedObject?.root :
                    _systemMode ? _preview.gameObject : _previewHero;
                if (_lastPinch > 0 && objectToScale != null &&
                    !IsOverUI(first.screenPosition) && !IsOverUI(second.screenPosition))
                {
                    float next = Mathf.Clamp(objectToScale.transform.localScale.x * distance / _lastPinch,
                        .5f, _arMode ? 2.25f : _systemMode ? 2.25f : 4f);
                    objectToScale.transform.localScale = Vector3.one * next;
                }
                _lastPinch = distance;
                _draggedObject = null;
                return;
            }
            _lastPinch = 0;
            if (count == 0 || _switching) { _draggedObject = null; return; }
            Vector2 point = first.screenPosition;
            if (first.began)
            {
                _lastTouch = point;
                _draggedObject = null;
                if (IsOverUI(point)) return;
                _draggedObject = Pick(point);
                if (_draggedObject != null)
                    _dragDistance = Vector3.Distance(arCamera.transform.position, _draggedObject.root.transform.position);
                else if (_arMode) Place(point);
            }
            else if (first.phase == UnityEngine.InputSystem.TouchPhase.Moved && !IsOverUI(point))
            {
                if (_arMode && _draggedObject != null)
                    _draggedObject.root.transform.position = arCamera.ScreenPointToRay(point).GetPoint(_dragDistance);
                else if (!_arMode)
                    (_systemMode ? _preview.transform : _previewHero.transform)
                        .Rotate(Vector3.up, -(point.x - _lastTouch.x) * .28f, Space.Self);
                _lastTouch = point;
            }
        }

        private bool IsOverUI(Vector2 point)
        {
            if (EventSystem.current == null) return false;
            _uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, _uiHits);
            return _uiHits.Count > 0;
        }

#if UNITY_EDITOR
        private void HandleMouseInEditor()
        {
            if (Touchscreen.current != null || Mouse.current == null) return;
            var mouse = Mouse.current;
            Vector2 point = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame && !IsOverUI(point))
            {
                _lastTouch = point;
                Pick(point);
            }
            if (mouse.leftButton.isPressed && !IsOverUI(point))
            {
                (_systemMode ? _preview.transform : _previewHero.transform)
                    .Rotate(Vector3.up, -(point.x - _lastTouch.x) * .28f, Space.Self);
                _lastTouch = point;
            }
            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > .1f && !IsOverUI(point))
            {
                var target = _systemMode ? _preview.transform : _previewHero.transform;
                target.localScale = Vector3.one * Mathf.Clamp(target.localScale.x * (1f + wheel * .001f),
                    .5f, _systemMode ? 2.25f : 4f);
            }
        }
#endif
    }
}
