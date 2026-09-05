using UnityEngine;
using UnityEngine.UI;

namespace Pizzeria3D
{
    /// <summary>
    /// Le joystick a l'ecran. Il est flottant : il apparait la ou le doigt se
    /// pose, plutot qu'a une place fixe — c'est ce qui evite au joueur de
    /// chercher le pouce sur un telephone.
    ///
    /// Les cercles sont dessines dans des textures generees : aucun sprite a
    /// importer, comme le reste du jeu.
    /// </summary>
    public sealed class Manette : MonoBehaviour
    {
        public static Manette Active { get; private set; }

        /// <summary>Direction voulue, en repere ecran, longueur 0 a 1.</summary>
        public Vector2 Direction { get; private set; }
        public bool EstVisible { get; private set; }

        RectTransform _socle, _bouton;
        Vector3 _origine;
        bool _tenu;

        static float Rayon => Mathf.Min(Screen.height * 0.12f, 150f);

        void Awake()
        {
            Active = this;
            Construire();
            Montrer(false);
        }

        void Update()
        {
            Doigt.Lire();

            if (Doigt.Presse)
            {
                _origine = Doigt.Position;
                _tenu = true;
                Montrer(true);
                _socle.position = _origine;
                _bouton.position = _origine;
            }
            if (Doigt.Relache)
            {
                _tenu = false;
                Direction = Vector2.zero;
                Montrer(false);
            }
            if (!_tenu) { Direction = Vector2.zero; return; }

            var delta = Doigt.Position - _origine;
            var plan = new Vector2(delta.x, delta.y);
            float force = Mathf.Clamp01(plan.magnitude / Rayon);
            Direction = force < 0.12f ? Vector2.zero : plan.normalized * force;

            _bouton.position = _origine + new Vector3(Direction.x, Direction.y, 0f) * Rayon;
        }

        void Montrer(bool visible)
        {
            EstVisible = visible;
            if (_socle != null) _socle.gameObject.SetActive(visible);
            if (_bouton != null) _bouton.gameObject.SetActive(visible);
        }

        // ------------------------------------------------------------------

        void Construire()
        {
            var canvasGo = new GameObject("ManetteCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            _socle = Cercle(canvasGo.transform, "Socle", Rayon * 2.1f,
                            Rond(160, 0.30f), new Color(1f, 1f, 1f, 0.30f));
            _bouton = Cercle(canvasGo.transform, "Bouton", Rayon * 1.05f,
                             Rond(160, 0f), new Color(1f, 1f, 1f, 0.75f));
        }

        static RectTransform Cercle(Transform parent, string nom, float diametre, Sprite sprite, Color couleur)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = couleur;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(diametre, diametre);
            return rt;
        }

        /// <summary>
        /// Disque ou anneau, dessine dans une texture. L'epaisseur est donnee
        /// en fraction du rayon ; zero donne un disque plein.
        /// </summary>
        static Sprite Rond(int taille, float epaisseur)
        {
            var tex = new Texture2D(taille, taille, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[taille * taille];
            float c = taille / 2f, R = c - 1f;
            float interieur = epaisseur <= 0f ? 0f : R * (1f - epaisseur);

            for (int y = 0; y < taille; y++)
            for (int x = 0; x < taille; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(R - d);                   // bord adouci
                if (interieur > 0f) a = Mathf.Min(a, Mathf.Clamp01(d - interieur));
                px[y * taille + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false);
            return Sprite.Create(tex, new Rect(0, 0, taille, taille), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
