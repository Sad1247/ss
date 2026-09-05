using UnityEngine;
using UnityEngine.UI;

namespace Pizzeria3D
{
    /// <summary>
    /// La bulle qui flotte au-dessus d'un client et annonce combien de pizzas
    /// il attend. Sans elle, le joueur ne sait pas quand la file est servie.
    ///
    /// Elle vit dans un canvas en espace monde, tourne face a la camera, et
    /// son fond est dessine dans une texture generee : aucun sprite a importer.
    /// </summary>
    public sealed class Bulle : MonoBehaviour
    {
        static Sprite _fond;

        Text _texte;
        Transform _camera;

        public static Bulle Creer(Transform porteur, float hauteur)
        {
            var go = new GameObject("Bulle", typeof(RectTransform));
            go.transform.SetParent(porteur, false);
            go.transform.localPosition = new Vector3(0f, hauteur, 0f);
            go.transform.localScale = new Vector3(0.0075f, 0.0075f, 0.0075f);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220f, 150f);

            if (_fond == null) _fond = Fond();

            var fond = new GameObject("Fond", typeof(RectTransform));
            fond.transform.SetParent(go.transform, false);
            var fimg = fond.AddComponent<Image>();
            fimg.sprite = _fond;
            fimg.raycastTarget = false;
            Etirer(fond.GetComponent<RectTransform>());

            var icone = new GameObject("Icone", typeof(RectTransform));
            icone.transform.SetParent(go.transform, false);
            var iimg = icone.AddComponent<Image>();
            iimg.sprite = Pizza3D.Icone;
            iimg.raycastTarget = false;
            var irt = icone.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 1f);
            irt.pivot = new Vector2(1f, 1f);
            irt.anchoredPosition = new Vector2(6f, -18f);
            irt.sizeDelta = new Vector2(70f, 70f);

            var b = go.AddComponent<Bulle>();
            b._texte = Texte(go.transform);
            return b;
        }

        void Awake()
        {
            var cam = Object.FindObjectOfType<Camera>();
            if (cam != null) _camera = cam.transform;
        }

        void LateUpdate()
        {
            // la camera ne tourne jamais, mais le client, lui, pivote en marchant
            if (_camera != null) transform.rotation = _camera.rotation;
        }

        public void Afficher(int nombre)
        {
            gameObject.SetActive(nombre > 0);
            if (nombre > 0) _texte.text = "x" + nombre;
        }

        // ------------------------------------------------------------------

        static Text Texte(Transform parent)
        {
            var go = new GameObject("Nombre", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Police();
            t.fontSize = 58;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Bloc.Couleur(0x2B2118);
            t.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(14f, -22f);
            rt.sizeDelta = new Vector2(110f, 62f);
            return t;
        }

        static Font Police()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            return f;
        }

        static void Etirer(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        /// <summary>Rectangle arrondi blanc, avec la pointe qui designe le client.</summary>
        static Sprite Fond()
        {
            const int L = 220, H = 150, Coin = 34, Corps = 108;   // hauteur du rectangle
            var tex = new Texture2D(L, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[L * H];

            var blanc = new Color32(255, 252, 244, 255);
            var vide = new Color32(0, 0, 0, 0);
            var bord = new Color32(206, 190, 165, 255);

            for (int y = 0; y < H; y++)
            for (int x = 0; x < L; x++)
            {
                bool dedans;
                int yh = H - 1 - y;                       // depuis le haut

                if (yh < Corps)
                {
                    // rectangle a coins arrondis
                    float cx = Mathf.Clamp(x, Coin, L - 1 - Coin);
                    float cy = Mathf.Clamp(yh, Coin, Corps - 1 - Coin);
                    float dx = x - cx, dy = yh - cy;
                    dedans = dx * dx + dy * dy <= Coin * Coin;
                }
                else
                {
                    // la pointe, un triangle sous le rectangle
                    float t = (yh - Corps) / (float)(H - Corps);
                    float demi = (1f - t) * 22f;
                    dedans = Mathf.Abs(x - L * 0.42f) <= demi;
                }

                int i = y * L + x;
                px[i] = dedans ? blanc : vide;
                if (dedans && yh > 2 && yh < Corps && (x < 3 || x > L - 4)) px[i] = bord;
            }

            tex.SetPixels32(px);
            tex.Apply(false);
            return Sprite.Create(tex, new Rect(0, 0, L, H), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
