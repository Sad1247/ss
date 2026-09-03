// ---------------------------------------------------------------------------
// Faux runtime Unity : assez fonctionnel pour EXECUTER GameView hors editeur.
// Hierarchie, AddComponent/GetComponent, cycle de vie, evenements de boutons.
// Ne pas copier dans Assets/ : ces types entrent en conflit avec ceux d'Unity.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public class Object
    {
        public string name = "";
        public static readonly List<Object> Tous = new List<Object>();
        protected Object() { Tous.Add(this); }

        public static void Destroy(Object o)
        {
            if (o is GameObject go)
            {
                go.Detruit = true;
                foreach (var enfant in new List<Transform>(go.transform.Enfants))
                    Destroy(enfant.gameObject);
                go.transform.SetParent(null, false);
                foreach (var c in go.Composants) Tous.Remove(c);
            }
            Tous.Remove(o);
        }

        public static T FindObjectOfType<T>() where T : Object
        {
            foreach (var o in Tous) if (o is T t) return t;
            return null;
        }
    }

    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
    }

    public class Behaviour : Component { public bool enabled = true; }

    public class MonoBehaviour : Behaviour
    {
        public void Invoquer(string methode)
        {
            var m = GetType().GetMethod(methode,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            m?.Invoke(this, null);
        }
    }

    public class GameObject : Object
    {
        public readonly List<Component> Composants = new List<Component>();
        public Transform transform;
        public bool Actif = true;
        public bool Detruit;

        public GameObject() : this("GameObject") { }

        public GameObject(string n, params Type[] types)
        {
            name = n;
            transform = new Transform { gameObject = this };
            Composants.Add(transform);
            foreach (var t in types) AjouterType(t);
        }

        public void SetActive(bool b) => Actif = b;

        Component AjouterType(Type t)
        {
            var c = (Component)Activator.CreateInstance(t);
            c.gameObject = this;
            Composants.Add(c);
            if (c is MonoBehaviour mb) { Scene.Enregistrer(mb); mb.Invoquer("Awake"); }
            return c;
        }

        public T AddComponent<T>() where T : Component => (T)AjouterType(typeof(T));

        public T GetComponent<T>() where T : Component
        {
            foreach (var c in Composants) if (c is T t) return t;
            return null;
        }
    }

    public class Transform : Component
    {
        public Transform parent;
        public readonly List<Transform> Enfants = new List<Transform>();

        public void SetParent(Transform p, bool worldPositionStays)
        {
            parent?.Enfants.Remove(this);
            parent = p;
            p?.Enfants.Add(this);
        }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, offsetMin, offsetMax;
    }

    /// <summary>Pilote le cycle de vie a la place du moteur.</summary>
    public static class Scene
    {
        static readonly List<MonoBehaviour> Comportements = new List<MonoBehaviour>();
        static readonly HashSet<MonoBehaviour> Demarres = new HashSet<MonoBehaviour>();

        public static void Enregistrer(MonoBehaviour mb) => Comportements.Add(mb);
        public static void Reinitialiser() { Comportements.Clear(); Demarres.Clear(); Object.Tous.Clear(); }

        public static void Frame(float dt)
        {
            Time.deltaTime = dt;
            var copie = new List<MonoBehaviour>(Comportements);
            foreach (var mb in copie)
                if (Demarres.Add(mb)) mb.Invoquer("Start");
            foreach (var mb in copie) mb.Invoquer("Update");
            foreach (var mb in copie) mb.Invoquer("LateUpdate");
        }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float a, float b) { x = a; y = b; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
    }

    public struct Rect { public Rect(float a, float b, float c, float d) { } }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float x, float y, float z) { r = x; g = y; b = z; a = 1f; }
        public Color(float x, float y, float z, float w) { r = x; g = y; b = z; a = w; }
        public static Color white => new Color(1, 1, 1);
        public static Color black => new Color(0, 0, 0);
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color magenta => new Color(1, 0, 1);
        public static Color Lerp(Color x, Color y, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t,
                             x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t);
        }
        public override string ToString() => $"({r:F2},{g:F2},{b:F2},{a:F2})";
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte x, byte y, byte z, byte w) { r = x; g = y; b = z; a = w; }
        public static implicit operator Color(Color32 c) =>
            new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
        public static implicit operator Color32(Color c) => new Color32(
            (byte)(Clamp(c.r) * 255), (byte)(Clamp(c.g) * 255),
            (byte)(Clamp(c.b) * 255), (byte)(Clamp(c.a) * 255));
        static float Clamp(float f) => f < 0 ? 0 : f > 1 ? 1 : f;
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Floor(float f) => (float)Math.Floor(f);
        public static float Abs(float f) => Math.Abs(f);
        public static float Clamp01(float f) => f < 0f ? 0f : f > 1f ? 1f : f;
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
    }

    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Bilinear }

    public class Texture : Object { }

    public class Texture2D : Texture
    {
        public readonly int width, height;
        public FilterMode filterMode;
        public Color32[] Pixels;
        public int NbApply;
        public Texture2D(int w, int h, TextureFormat f, bool mip) { width = w; height = h; }
        public void SetPixels32(Color32[] p) => Pixels = (Color32[])p.Clone();
        public void Apply(bool b) => NbApply++;
    }

    public class Sprite : Object
    {
        public Texture2D texture;
        public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu) =>
            new Sprite { texture = t };
    }

    public class Font : Object { }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object
        {
            if (typeof(T) == typeof(Font) && path == "LegacyRuntime.ttf")
                return (T)(Object)new Font { name = path };
            return null;
        }
    }

    public static class Debug
    {
        public static readonly List<string> Journal = new List<string>();
        public static void Log(object o) => Journal.Add("LOG " + o);
        public static void LogWarning(object o) => Journal.Add("WARN " + o);
        public static void LogError(object o) => Journal.Add("ERROR " + o);
    }

    public static class Time { public static float deltaTime; }

    public class RectOffset
    {
        public int left, right, top, bottom;
        public RectOffset(int l, int r, int t, int b) { left = l; right = r; top = t; bottom = b; }
    }

    public enum TextAnchor { UpperLeft, UpperCenter, MiddleCenter, MiddleLeft }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public enum RenderMode { ScreenSpaceOverlay }

    public class Canvas : Behaviour { public RenderMode renderMode; }

    [AttributeUsage(AttributeTargets.All)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class TooltipAttribute : Attribute { public TooltipAttribute(string s) { } }
    [AttributeUsage(AttributeTargets.All)] public class RequireComponent : Attribute { public RequireComponent(Type t) { } }

    public enum RuntimeInitializeLoadType { AfterSceneLoad }
    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethod : Attribute
    {
        public RuntimeInitializeOnLoadMethod() { }
        public RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType t) { }
    }

    namespace Events
    {
        public delegate void UnityAction();
        public class UnityEvent
        {
            readonly List<UnityAction> _actions = new List<UnityAction>();
            public void AddListener(UnityAction a) => _actions.Add(a);
            public void Invoke() { foreach (var a in new List<UnityAction>(_actions)) a(); }
            public int Count => _actions.Count;
        }
    }
}

namespace UnityEngine.UI
{
    public class Graphic : Behaviour { public Color color = Color.white; public bool raycastTarget = true; }

    public class Image : Graphic
    {
        public Sprite sprite; public bool preserveAspect;
        public enum Type { Simple, Filled }
        public enum FillMethod { Horizontal }
        public enum OriginHorizontal { Left }
        public Type type; public FillMethod fillMethod; public int fillOrigin; public float fillAmount = 1f;
    }

    public class Text : Graphic
    {
        public Font font; public string text = ""; public int fontSize;
        public TextAnchor alignment; public HorizontalWrapMode horizontalOverflow;
        public VerticalWrapMode verticalOverflow;
    }

    public class Selectable : Behaviour
    {
        public enum Transition { None, ColorTint }
        public Transition transition; public Graphic targetGraphic; public bool interactable = true;
    }

    public class Button : Selectable
    {
        public Events.UnityEvent onClick = new Events.UnityEvent();
        /// <summary>Simule un appui : respecte l'etat interactable, comme Unity.</summary>
        public bool Cliquer()
        {
            if (!interactable) return false;
            onClick.Invoke();
            return true;
        }
    }

    public class LayoutElement : Behaviour { public float preferredHeight, minHeight, flexibleHeight; }

    public class LayoutGroup : Behaviour { public RectOffset padding; public TextAnchor childAlignment; }

    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing;
        public bool childControlHeight, childControlWidth, childForceExpandHeight, childForceExpandWidth;
    }

    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public class GridLayoutGroup : LayoutGroup
    {
        public enum Constraint { FixedColumnCount }
        public Vector2 spacing, cellSize; public Constraint constraint; public int constraintCount;
    }

    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ScaleWithScreenSize }
        public ScaleMode uiScaleMode; public Vector2 referenceResolution; public float matchWidthOrHeight;
    }

    public class GraphicRaycaster : Behaviour { }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : Behaviour { }
    public class StandaloneInputModule : Behaviour { }
}
