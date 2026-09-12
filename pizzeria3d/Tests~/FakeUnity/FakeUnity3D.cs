// ---------------------------------------------------------------------------
// Faux runtime Unity : assez fonctionnel pour EXECUTER le jeu hors editeur.
// Hierarchie, AddComponent/GetComponent, cycle de vie, transforms 3D, entrees.
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
            if (o is Component comp && !(o is Transform))
            {
                comp.gameObject.Composants.Remove(comp);
                Tous.Remove(comp);
                return;
            }
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

        public void SetActive(bool b)
        {
            bool avant = Actif;
            Actif = b;
            // Unity appelle OnEnable au moment ou l'objet devient actif :
            // sans cela, un objet revele en cours de partie ne s'annonce jamais.
            if (b && !avant) Reveiller(this);
        }

        static void Reveiller(GameObject go)
        {
            foreach (var c in new List<Component>(go.Composants))
                if (c is MonoBehaviour mb) Scene.Reveiller(mb);
            foreach (var e in new List<Transform>(go.transform.Enfants))
                if (e.gameObject.Actif) Reveiller(e.gameObject);
        }

        public bool activeSelf => Actif;

        /// <summary>Actif seulement si tous ses parents le sont aussi.</summary>
        public bool ActifDansHierarchie
        {
            get
            {
                if (!Actif) return false;
                for (var p = transform.parent; p != null; p = p.parent)
                    if (!p.gameObject.Actif) return false;
                return true;
            }
        }

        /// <summary>Forme d'origine, que le vrai Unity garde dans son maillage.</summary>
        public PrimitiveType Primitive = PrimitiveType.Cube;

        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            var go = new GameObject(type.ToString()) { Primitive = type };
            go.AddComponent<MeshRenderer>();
            go.AddComponent<BoxCollider>();
            return go;
        }

        Component AjouterType(Type t)
        {
            // Unity remplace le Transform d'un objet par un RectTransform des
            // qu'on en demande un : sans cela, l'objet en aurait deux, et
            // transform ne designerait pas celui que GetComponent renvoie.
            if (t == typeof(RectTransform) && !(transform is RectTransform))
            {
                var rt = new RectTransform { gameObject = this };
                rt.SetParent(transform.parent, false);
                foreach (var e in new List<Transform>(transform.Enfants)) e.SetParent(rt, false);
                Composants.Remove(transform);
                Tous.Remove(transform);
                transform = rt;
                Composants.Insert(0, rt);
                return rt;
            }

            var c = (Component)Activator.CreateInstance(t);
            c.gameObject = this;
            Composants.Add(c);
            if (c is MonoBehaviour mb)
            {
                Scene.Enregistrer(mb);
                // Unity n'eveille un composant que si son objet est actif — et
                // il enchaine alors Awake ET OnEnable. Ajoute a un objet
                // eteint, il attend l'allumage. Sans cette regle, un employe
                // bati puis eteint s'annoncait quand meme au comptoir.
                if (ActifDansHierarchie) Scene.Reveiller(mb);
            }
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

        public Vector3 localPosition;
        public Vector3 localScale = Vector3.one;
        public Quaternion localRotation = Quaternion.identity;

        /// <summary>Composition simplifiee : translation seule, suffisante pour les tests.</summary>
        public Vector3 position
        {
            get => parent == null ? localPosition : parent.position + localPosition;
            set => localPosition = parent == null ? value : value - parent.position;
        }

        public Quaternion rotation
        {
            get => localRotation;
            set => localRotation = value;
        }

        public Vector3 forward => rotation * Vector3.forward;
        public Vector3 right => rotation * new Vector3(1f, 0f, 0f);
        public Vector3 up => rotation * Vector3.up;

        public void SetParent(Transform p, bool worldPositionStays)
        {
            parent?.Enfants.Remove(this);
            parent = p;
            p?.Enfants.Add(this);
        }

        public int childCount => Enfants.Count;
        public Transform GetChild(int i) => Enfants[i];

        /// <summary>Comme Unity : accepte un chemin "Corps/Mains", pas seulement un nom.</summary>
        public Transform Find(string chemin)
        {
            var courant = this;
            foreach (var nom in chemin.Split('/'))
            {
                if (nom.Length == 0) continue;
                Transform trouve = null;
                foreach (var e in courant.Enfants) if (e.gameObject.name == nom) { trouve = e; break; }
                if (trouve == null) return null;
                courant = trouve;
            }
            return courant == this ? null : courant;
        }

        public void Rotate(float x, float y, float z) { }
    }

    public class RectTransform : Transform
    {
        // Les valeurs par defaut d'Unity pour un objet d'interface neuf :
        // ancre et pivot au centre. A zero, tout ce qui n'y touche pas se
        // retrouvait cale par un coin.
        public Vector2 anchorMin = new Vector2(0.5f, 0.5f);
        public Vector2 anchorMax = new Vector2(0.5f, 0.5f);
        public Vector2 offsetMin, offsetMax;
        public Vector2 pivot = new Vector2(0.5f, 0.5f);
        public Vector2 anchoredPosition, sizeDelta;
    }

    /// <summary>Pilote le cycle de vie a la place du moteur.</summary>
    public static class Scene
    {
        static readonly List<MonoBehaviour> Comportements = new List<MonoBehaviour>();
        static readonly HashSet<MonoBehaviour> Demarres = new HashSet<MonoBehaviour>();

        public static void Enregistrer(MonoBehaviour mb) => Comportements.Add(mb);

        static readonly HashSet<MonoBehaviour> Eveilles = new HashSet<MonoBehaviour>();

        /// <summary>
        /// Le reveil d'un composant : Awake une seule fois dans sa vie, puis
        /// OnEnable a chaque allumage — l'ordre et la regle d'Unity.
        /// </summary>
        public static void Reveiller(MonoBehaviour mb)
        {
            if (Eveilles.Add(mb)) mb.Invoquer("Awake");
            mb.Invoquer("OnEnable");
        }
        public static void Reinitialiser()
        { Comportements.Clear(); Demarres.Clear(); Eveilles.Clear(); Object.Tous.Clear(); }

        public static void Frame(float dt)
        {
            // Unity livre un deltaTime deja mis a l'echelle : sans cela,
            // accelerer le jeu ne changerait rien ici.
            Time.deltaTime = dt * Time.timeScale;
            Time.time += Time.deltaTime;
            // Unity cesse d'appeler Update sur un objet detruit : sans cette
            // regle, un objet supprime continuerait de jouer sa logique ici.
            Comportements.RemoveAll(mb => mb.gameObject == null || mb.gameObject.Detruit);

            var copie = new List<MonoBehaviour>(Comportements);
            foreach (var mb in copie)
            {
                if (mb.gameObject.Detruit) continue;
                if (Demarres.Add(mb)) mb.Invoquer("Start");
            }
            // Unity ne met a jour que les objets actifs : sans ce filtre, le
            // second four produirait des pizzas avant meme d'etre achete.
            foreach (var mb in copie)
                if (!mb.gameObject.Detruit && mb.gameObject.ActifDansHierarchie) mb.Invoquer("Update");
            foreach (var mb in copie)
                if (!mb.gameObject.Detruit && mb.gameObject.ActifDansHierarchie) mb.Invoquer("LateUpdate");
            Input.FinDeFrame();
        }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float a, float b) { x = a; y = b; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public Vector2 normalized { get { float m = magnitude; return m <= 0f ? zero : new Vector2(x / m, y / m); } }
        public static Vector2 operator *(Vector2 v, float f) => new Vector2(v.x * f, v.y * f);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float a, float b, float c) { x = a; y = b; z = c; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);
        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized { get { float m = magnitude; return m <= 0f ? zero : new Vector3(x / m, y / m, z / m); } }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 v, float f) => new Vector3(v.x * f, v.y * f, v.z * f);
        public static Vector3 operator *(float f, Vector3 v) => v * f;
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float pas)
        {
            var d = b - a; float m = d.magnitude;
            return m <= pas || m == 0f ? b : a + d * (pas / m);
        }
        public override string ToString() => $"({x:F2},{y:F2},{z:F2})";
    }

    /// <summary>
    /// Un vrai quaternion, et non un bouchon : sans lui, impossible de verifier
    /// qu'un deplacement calcule depuis l'orientation de la camera tombe juste.
    /// </summary>
    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float px, float py, float pz, float pw) { x = px; y = py; z = pz; w = pw; }

        public static Quaternion identity => new Quaternion(0, 0, 0, 1);

        /// <summary>Ordre d'Unity : Z, puis X, puis Y.</summary>
        public static Quaternion Euler(float ex, float ey, float ez)
        {
            float rx = ex * (float)Math.PI / 180f * 0.5f;
            float ry = ey * (float)Math.PI / 180f * 0.5f;
            float rz = ez * (float)Math.PI / 180f * 0.5f;

            var qx = new Quaternion((float)Math.Sin(rx), 0, 0, (float)Math.Cos(rx));
            var qy = new Quaternion(0, (float)Math.Sin(ry), 0, (float)Math.Cos(ry));
            var qz = new Quaternion(0, 0, (float)Math.Sin(rz), (float)Math.Cos(rz));
            return qy * qx * qz;
        }

        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);

        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            float ix = q.w * v.x + q.y * v.z - q.z * v.y;
            float iy = q.w * v.y + q.z * v.x - q.x * v.z;
            float iz = q.w * v.z + q.x * v.y - q.y * v.x;
            float iw = -q.x * v.x - q.y * v.y - q.z * v.z;
            return new Vector3(
                ix * q.w + iw * -q.x + iy * -q.z - iz * -q.y,
                iy * q.w + iw * -q.y + iz * -q.x - ix * -q.z,
                iz * q.w + iw * -q.z + ix * -q.y - iy * -q.x);
        }

        public static Quaternion LookRotation(Vector3 avant, Vector3 haut)
        {
            var f = avant.normalized;
            if (f.magnitude <= 0f) return identity;
            var r = Croix(haut, f).normalized;
            var u = Croix(f, r);

            float trace = r.x + u.y + f.z;
            if (trace > 0f)
            {
                float s = (float)Math.Sqrt(trace + 1f) * 2f;
                return new Quaternion((u.z - f.y) / s, (f.x - r.z) / s, (r.y - u.x) / s, 0.25f * s);
            }
            if (r.x > u.y && r.x > f.z)
            {
                float s = (float)Math.Sqrt(1f + r.x - u.y - f.z) * 2f;
                return new Quaternion(0.25f * s, (u.x + r.y) / s, (f.x + r.z) / s, (u.z - f.y) / s);
            }
            if (u.y > f.z)
            {
                float s = (float)Math.Sqrt(1f + u.y - r.x - f.z) * 2f;
                return new Quaternion((u.x + r.y) / s, 0.25f * s, (f.y + u.z) / s, (f.x - r.z) / s);
            }
            else
            {
                float s = (float)Math.Sqrt(1f + f.z - r.x - u.y) * 2f;
                return new Quaternion((f.x + r.z) / s, (f.y + u.z) / s, 0.25f * s, (r.y - u.x) / s);
            }
        }

        static Vector3 Croix(Vector3 a, Vector3 b) => new Vector3(
            a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);

        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            t = Mathf.Clamp01(t);
            var q = new Quaternion(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t,
                                   a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t);
            float n = (float)Math.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return n <= 0f ? identity : new Quaternion(q.x / n, q.y / n, q.z / n, q.w / n);
        }
    }

    public enum PrimitiveType { Cube, Sphere, Capsule, Cylinder, Plane, Quad }

    public class Shader : Object
    {
        public static Shader Find(string nom) => nom == "Standard" ? new Shader { name = nom } : null;
    }

    public class Material : Object
    {
        public Color color;
        public Texture mainTexture;
        public int renderQueue;

        readonly Dictionary<string, float> _reglages = new Dictionary<string, float>();
        readonly List<string> _motsCles = new List<string>();

        public Material(Shader s) { }
        public bool HasProperty(string nom) => true;
        public void SetFloat(string nom, float v) => _reglages[nom] = v;
        public float GetFloat(string nom) => _reglages.TryGetValue(nom, out var v) ? v : 0f;
        public void SetInt(string nom, int v) => _reglages[nom] = v;
        public void EnableKeyword(string nom) { if (!_motsCles.Contains(nom)) _motsCles.Add(nom); }
        public void DisableKeyword(string nom) => _motsCles.Remove(nom);
        public bool IsKeywordEnabled(string nom) => _motsCles.Contains(nom);
        public void SetShaderPassEnabled(string passe, bool actif) { }
    }

    public class Renderer : Behaviour { public Material sharedMaterial, material; }
    public class MeshRenderer : Renderer { }

    public class Mesh : Object
    {
        public Vector3[] vertices = new Vector3[0];
        public Vector3[] normals = new Vector3[0];
        public Vector2[] uv = new Vector2[0];
        public int[] triangles = new int[0];
    }

    public class MeshFilter : Component { public Mesh mesh, sharedMesh; }
    public class Collider : Behaviour { }
    public class BoxCollider : Collider { }

    public enum LightType { Directional, Point, Spot }
    public enum LightShadows { None, Hard, Soft }
    public class Light : Behaviour
    {
        public LightType type; public Color color; public float intensity;
        public LightShadows shadows; public float shadowStrength;
        public float shadowBias = 0.05f, shadowNormalBias = 0.4f;
        public LightRenderMode renderMode;
    }

    public enum LightRenderMode { Auto, ForcePixel, ForceVertex }
    public enum AnisotropicFiltering { Disable, Enable, ForceEnable }

    public enum ShadowQuality { Disable, HardOnly, All }
    public enum ShadowResolution { Low, Medium, High, VeryHigh }

    public static class QualitySettings
    {
        public static int antiAliasing;
        public static ShadowQuality shadows;
        public static ShadowResolution shadowResolution;
        public static float shadowDistance;
        public static int shadowCascades;
        public static int pixelLightCount;
        public static int vSyncCount;
        public static int globalTextureMipmapLimit;
        public static AnisotropicFiltering anisotropicFiltering;
    }

    public static class Application
    {
        public static int targetFrameRate;
    }

    public static class RenderSettings
    {
        public static Rendering.AmbientMode ambientMode;
        public static Color ambientLight;
        public static Color ambientSkyColor, ambientEquatorColor, ambientGroundColor;
        public static float ambientIntensity = 1f;
        public static bool fog;
    }

    namespace Rendering
    {
        public enum AmbientMode { Skybox, Trilight, Flat, Custom }

        public enum BlendMode { Zero, One, DstColor, SrcColor, OneMinusDstColor, SrcAlpha,
                                OneMinusSrcColor, DstAlpha, OneMinusDstAlpha, SrcAlphaSaturate,
                                OneMinusSrcAlpha }

        public enum RenderQueue { Background = 1000, Geometry = 2000, AlphaTest = 2450,
                                  GeometryLast = 2500, Transparent = 3000, Overlay = 4000 }
    }

    public class Camera : Behaviour
    {
        public bool orthographic; public float orthographicSize; public Color backgroundColor;
        public static Camera main;
    }

    public static class Screen { public static int width = 1080, height = 1920; }

    namespace EventSystems
    {
        /// <summary>
        /// Sans lui, aucun bouton d'interface ne recoit de clic dans Unity :
        /// c'est lui qui distribue les evenements aux elements survolés.
        /// </summary>
        public class EventSystem : Behaviour
        {
            public static EventSystem current;
            public EventSystem() { current = this; }

            /// <summary>
            /// Le banc d'essai ne simule pas de position d'ecran reelle pour
            /// la souris : ce commutateur, pilote par le test, en tient lieu
            /// pour verifier que le code de jeu reagit bien a la reponse.
            /// </summary>
            public static bool SimulerSurUI;
            public bool IsPointerOverGameObject() => SimulerSurUI;
        }

        /// <summary>Le module qui lit la souris et le doigt pour l'EventSystem.</summary>
        public class StandaloneInputModule : Behaviour { }
    }

    /// <summary>Entrees simulees : le harnais pilote le doigt.</summary>
    public enum KeyCode { None = 0, Space = 32, Tab = 9, T = 116, Alpha1 = 49, Alpha4 = 52 }

    public static class Input
    {
        public static Vector3 mousePosition;
        public static bool[] Boutons = new bool[3];
        public static bool[] BoutonsPrecedents = new bool[3];
        public static bool GetMouseButton(int b) => Boutons[b];
        public static bool GetMouseButtonDown(int b) => Boutons[b] && !BoutonsPrecedents[b];
        public static bool GetMouseButtonUp(int b) => !Boutons[b] && BoutonsPrecedents[b];

        static readonly System.Collections.Generic.HashSet<KeyCode> Touches =
            new System.Collections.Generic.HashSet<KeyCode>();
        static readonly System.Collections.Generic.HashSet<KeyCode> TouchesPrecedentes =
            new System.Collections.Generic.HashSet<KeyCode>();

        public static bool GetKey(KeyCode k) => Touches.Contains(k);
        public static bool GetKeyDown(KeyCode k) => Touches.Contains(k) && !TouchesPrecedentes.Contains(k);
        public static bool GetKeyUp(KeyCode k) => !Touches.Contains(k) && TouchesPrecedentes.Contains(k);

        /// <summary>Le banc d'essai appuie et relache les touches lui-meme.</summary>
        public static void Enfoncer(KeyCode k) => Touches.Add(k);
        public static void Relacher(KeyCode k) => Touches.Remove(k);

        public static void FinDeFrame()
        {
            for (int i = 0; i < Boutons.Length; i++) BoutonsPrecedents[i] = Boutons[i];
            TouchesPrecedentes.Clear();
            foreach (var k in Touches) TouchesPrecedentes.Add(k);
        }
    }

    public static class Random
    {
        static readonly System.Random R = new System.Random(12345);
        public static float value => (float)R.NextDouble();
        public static int Range(int min, int max) => R.Next(min, max);
        public static float Range(float min, float max) => min + (float)R.NextDouble() * (max - min);
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
        public const float Deg2Rad = (float)(Math.PI / 180.0);
        public const float Rad2Deg = (float)(180.0 / Math.PI);
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Floor(float f) => (float)Math.Floor(f);
        public static float Abs(float f) => Math.Abs(f);
        public static float Clamp01(float f) => f < 0f ? 0f : f > 1f ? 1f : f;
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static float MoveTowards(float de, float vers, float pas)
            => Abs(vers - de) <= pas ? vers : de + Sign(vers - de) * pas;
        public static float Sign(float f) => f < 0f ? -1f : 1f;
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static float Log(float f, float p) => (float)(Math.Log(f) / Math.Log(p));
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

    public static class Time
    {
        public static float deltaTime;
        public static float time;   // avance avec les images simulees
        /// <summary>
        /// Comme dans Unity : deltaTime est deja multiplie par cette echelle.
        /// C'est par elle que passe l'acceleration du jeu.
        /// </summary>
        public static float timeScale = 1f;
    }

    public class RectOffset
    {
        public int left, right, top, bottom;
        public RectOffset(int l, int r, int t, int b) { left = l; right = r; top = t; bottom = b; }
    }

    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public class Canvas : Behaviour { public RenderMode renderMode; public int sortingOrder; public bool overrideSorting; }

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

    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }

    public class Text : Graphic
    {
        public Font font; public string text = ""; public int fontSize;
        public FontStyle fontStyle;
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

