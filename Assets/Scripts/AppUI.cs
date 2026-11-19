using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GezegenAtlasi
{
    public sealed class AppUI
    {
        private static readonly Color Plum = Hex("#281E35");
        private static readonly Color Cream = Hex("#F8F0E6");
        private static readonly Color Copper = Hex("#C65136");
        private static readonly Color Muted = Hex("#6E6370");
        private static Sprite _rounded;
        private static Font _regular;
        private static Font _bold;

        private readonly RectTransform _safeRoot;
        private readonly Text _status;
        private readonly Text _occlusion;
        private readonly Text _title;
        private readonly Text _description;
        private readonly Text _selectionLabel;
        private readonly Text _count;
        private readonly Text _arText;
        private readonly Button _centerButton;
        private readonly Button _deleteButton;
        private readonly Button _menuToggle;
        private readonly Button _speedButton;
        private readonly Button _singleButton;
        private readonly Button _systemButton;
        private readonly Button[] _planetButtons;
        private readonly GameObject _menu;
        private bool _systemMode;
        private bool _arActive;
        private int _selected = 2;

        public AppUI(Action onMode, Action onCenter, Action onDelete, Action<int> onPlanet,
            Action onSystem, Action onSpeed)
        {
            _regular = Resources.Load<Font>("Fonts/OpenSans-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _bold = Resources.Load<Font>("Fonts/OpenSans-Bold") ?? _regular;
            EnsureRounded();
            var eventSystem = new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            UnityEngine.Object.DontDestroyOnLoad(eventSystem);
            var canvasObject = new GameObject("KOZMOS arayüzü", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            _safeRoot = Box(canvasObject.transform, "Güvenli alan", Color.clear,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            FitSafeArea();

            var header = Box(_safeRoot, "Başlık", new Color(.10f, .07f, .15f, .85f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -83), new Vector2(0, 165));
            TextAt(header, "Marka", "◉  K O Z M O S", 36, Cream,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(35, -20), new Vector2(-70, 52), true);
            _status = TextAt(header, "Durum", "3B ÖNİZLEME", 19, Hex("#E7AE80"),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(36, 38), new Vector2(-72, 30), true);
            _occlusion = TextAt(header, "Örtülme durumu", "", 18, Hex("#E7AE80"),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(36, 9), new Vector2(-72, 28), false);

            var actions = new GameObject("AR eylemleri", typeof(RectTransform)).GetComponent<RectTransform>();
            actions.SetParent(_safeRoot, false);
            Place(actions, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -198), new Vector2(-34, 76));
            var arButton = MakeButton(actions, "AR / önizleme", "AR'YE GEÇ", Copper, Cream,
                new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(165, 0), new Vector2(285, 68), onMode);
            _arText = arButton.GetComponentInChildren<Text>();
            _centerButton = MakeButton(actions, "Ortala", "◎  ORTALA", Plum, Cream,
                new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-165, 0), new Vector2(285, 68), onCenter);
            _centerButton.gameObject.SetActive(false);

            var card = Box(_safeRoot, "Bilgi ve seçim", Cream,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 249), new Vector2(0, 498));
            _singleButton = MakeButton(card, "Tek gezegen modu", "TEK GEZEGEN", Plum, Cream,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(166, -40), new Vector2(260, 59), () => onPlanet(_selected));
            _systemButton = MakeButton(card, "Yörünge modu", "YÖRÜNGE", Hex("#E7DBD1"), Muted,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(411, -40), new Vector2(203, 59), onSystem);
            TextAt(card, "Etiket", "ŞİMDİ SAHNEDE", 20, Copper,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(37, -81), new Vector2(-74, 29), true);
            _title = TextAt(card, "Gezegen", "Dünya", 56, Plum,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(37, -117), new Vector2(-74, 79), true);
            _description = TextAt(card, "Açıklama", "", 25, Muted,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(37, -198), new Vector2(-74, 76), false);
            _description.resizeTextForBestFit = true;
            _description.resizeTextMinSize = 20;
            _description.resizeTextMaxSize = 25;
            _count = TextAt(card, "Nesne sayısı", "0 NESNE", 19, Muted,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(145, 159), new Vector2(225, 48), true);
            _deleteButton = MakeButton(card, "Seçileni sil", "SEÇİLİ SİL", Hex("#F1D7CD"), Hex("#973C2E"),
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-172, 158), new Vector2(270, 55), onDelete);
            _deleteButton.interactable = false;
            _speedButton = MakeButton(card, "Yörünge hızı", "YÖRÜNGE 1×", Hex("#F1D7CD"), Hex("#973C2E"),
                new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 160), new Vector2(310, 55), onSpeed);
            _speedButton.gameObject.SetActive(false);
            _menuToggle = MakeButton(card, "Gezegen menüsünü aç", "GEZEGEN SEÇ  ·  DÜNYA   ⌃", Hex("#FFF9F3"), Plum,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 68), new Vector2(-72, 87), ToggleMenu);
            _menuToggle.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            _menuToggle.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0);
            _selectionLabel = _menuToggle.GetComponentInChildren<Text>();

            var menuRect = Box(_safeRoot, "Gezegen menüsü", Cream,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 817), new Vector2(-32, 635));
            _menu = menuRect.gameObject;
            TextAt(menuRect, "Menü başlığı", "GEZEGEN SEÇ  /  01—08", 32, Plum,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -25), new Vector2(-150, 57), true);
            MakeButton(menuRect, "Kapat", "×", Hex("#E7DBD1"), Plum,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-67, -51), new Vector2(68, 68), ToggleMenu);
            var gridObject = new GameObject("Sekiz gezegen", typeof(RectTransform), typeof(GridLayoutGroup));
            var grid = gridObject.GetComponent<RectTransform>();
            grid.SetParent(menuRect, false);
            Place(grid, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, -36), new Vector2(-50, -110));
            var layout = gridObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(462, 110);
            layout.spacing = new Vector2(18, 16);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.childAlignment = TextAnchor.UpperCenter;
            _planetButtons = new Button[PlanetCatalog.Planets.Length];
            for (int i = 0; i < _planetButtons.Length; i++)
            {
                int index = i;
                var info = PlanetCatalog.Planets[i];
                var b = MakeButton(grid, "Gezegen " + (i + 1),
                    string.Format("{0:00}   {1}", i + 1, info.Name), Hex("#FFF9F3"), Plum,
                    Vector2.zero, Vector2.zero, Vector2.zero, layout.cellSize,
                    () => { onPlanet(index); _menu.SetActive(false); });
                _planetButtons[i] = b;
            }
            _menu.SetActive(false);
            SelectPlanet(2);
        }

        private void ToggleMenu() => _menu.SetActive(!_menu.activeSelf);
        public void SetStatus(string text) => _status.text = text;
        public void SetOcclusion(string text) => _occlusion.text = text;
        public void SetARMode(bool active, bool anyObjects)
        {
            _arActive = active;
            _arText.text = active ? "3B ÖNİZLEME" : "AR'YE GEÇ";
            _centerButton.gameObject.SetActive(active && anyObjects);
            _deleteButton.gameObject.SetActive(active);
            _count.gameObject.SetActive(active);
        }
        public void SetObjectActions(int count, bool selected)
        {
            _count.text = count + " NESNE";
            _deleteButton.interactable = selected;
            _centerButton.gameObject.SetActive(_arActive && count > 0);
        }
        public void SetMode(bool system)
        {
            _systemMode = system;
            _singleButton.GetComponent<Image>().color = system ? Hex("#E7DBD1") : Plum;
            _singleButton.GetComponentInChildren<Text>().color = system ? Muted : Cream;
            _systemButton.GetComponent<Image>().color = system ? Plum : Hex("#E7DBD1");
            _systemButton.GetComponentInChildren<Text>().color = system ? Cream : Muted;
            _menuToggle.gameObject.SetActive(!system);
            _speedButton.gameObject.SetActive(system);
            _menu.SetActive(false);
            if (system)
            {
                _title.text = "Güneş Sistemi";
                _description.text = "Sekiz gezegen, hareketli yörüngeler, tek sahne.";
            }
            else SelectPlanet(_selected);
        }
        public void SetOrbitSpeed(float speed) =>
            _speedButton.GetComponentInChildren<Text>().text = speed == 0 ? "YÖRÜNGE DURDU" : "YÖRÜNGE " + speed + "×";
        public void SelectPlanet(int index)
        {
            _selected = index;
            if (_systemMode) return;
            var info = PlanetCatalog.Planets[index];
            _title.text = info.Name;
            _description.text = info.Description;
            _selectionLabel.text = "GEZEGEN SEÇ  ·  " + info.Name.ToUpperInvariant() + "   ⌃";
            for (int i = 0; i < _planetButtons.Length; i++)
            {
                var b = _planetButtons[i];
                b.GetComponent<Image>().color = i == index ? Hex("#F1D7CD") : Hex("#FFF9F3");
            }
        }
        public void FitSafeArea()
        {
            Rect safe = Screen.safeArea;
            _safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            _safeRoot.offsetMin = _safeRoot.offsetMax = Vector2.zero;
        }
        public void Dispose()
        {
            if (_safeRoot != null) UnityEngine.Object.Destroy(_safeRoot.parent.gameObject);
        }
        private static RectTransform Box(Transform parent, string name, Color color,
            Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, min, max, pos, size);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = _rounded;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return rect;
        }
        private static Button MakeButton(Transform parent, string name, string label,
            Color background, Color foreground, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Action action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, min, max, pos, size);
            var image = go.GetComponent<Image>();
            image.sprite = _rounded;
            image.type = Image.Type.Sliced;
            image.color = background;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            TextAt(rect, "Yazı", label, 23, foreground, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, true, TextAnchor.MiddleCenter);
            return button;
        }
        private static Text TextAt(Transform parent, string name, string value, int size, Color color,
            Vector2 min, Vector2 max, Vector2 pos, Vector2 dimensions, bool bold,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, min, max, pos, dimensions);
            var text = go.GetComponent<Text>();
            text.text = value;
            text.font = bold ? _bold : _regular;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }
        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }
        private static void EnsureRounded()
        {
            if (_rounded != null) return;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(17 - x, 0, x - 46);
                float dy = Mathf.Max(17 - y, 0, y - 46);
                float alpha = Mathf.Clamp01((18.5f - Mathf.Sqrt(dx * dx + dy * dy)) / 2f);
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            texture.Apply();
            _rounded = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(18, 18, 18, 18));
        }
        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out var result);
            return result;
        }
    }
}
