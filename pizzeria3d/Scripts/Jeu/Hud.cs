using UnityEngine;
using UnityEngine.UI;

namespace Pizzeria3D
{
    /// <summary>Le strict necessaire a l'ecran : la caisse, et un indice contextuel.</summary>
    public sealed class Hud : MonoBehaviour
    {
        /// <summary>Texte affiche tant que le joueur est sur une zone d'achat.</summary>
        public static string Indice;

        static Hud _instance;
        Text _argent, _indice, _annonce;
        float _tempsAnnonce;

        public static void Annonce(string texte)
        {
            if (_instance == null) return;
            _instance._annonce.text = texte;
            _instance._tempsAnnonce = 2.5f;
        }

        void Awake()
        {
            _instance = this;

            var canvasGo = new GameObject("HudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var police = Police();

            var carte = new GameObject("Caisse", typeof(RectTransform));
            carte.transform.SetParent(canvasGo.transform, false);
            var rt = carte.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-30f, -30f);
            rt.sizeDelta = new Vector2(340f, 110f);
            var fond = carte.AddComponent<Image>();
            fond.color = Color.white;

            _argent = Texte(carte.transform, police, "0 EUR", 54, TextAnchor.MiddleCenter, Bloc.Couleur(0x2B3A2B));
            Etirer(_argent.GetComponent<RectTransform>());

            _indice = Texte(canvasGo.transform, police, "", 40, TextAnchor.LowerCenter, Color.white);
            var ri = _indice.GetComponent<RectTransform>();
            ri.anchorMin = new Vector2(0f, 0f); ri.anchorMax = new Vector2(1f, 0f);
            ri.pivot = new Vector2(0.5f, 0f);
            ri.anchoredPosition = new Vector2(0f, 240f); ri.sizeDelta = new Vector2(-80f, 90f);

            _annonce = Texte(canvasGo.transform, police, "", 46, TextAnchor.UpperCenter, Bloc.Couleur(0xFFE07A));
            var ra = _annonce.GetComponent<RectTransform>();
            ra.anchorMin = new Vector2(0f, 1f); ra.anchorMax = new Vector2(1f, 1f);
            ra.pivot = new Vector2(0.5f, 1f);
            ra.anchoredPosition = new Vector2(0f, -170f); ra.sizeDelta = new Vector2(-80f, 90f);

            Banque.Change += Rafraichir;
            Rafraichir();
        }

        void OnDestroy() => Banque.Change -= Rafraichir;

        void Rafraichir() => _argent.text = Banque.Solde + " EUR";

        void Update()
        {
            _indice.text = Indice ?? "";
            if (_tempsAnnonce > 0f)
            {
                _tempsAnnonce -= Time.deltaTime;
                if (_tempsAnnonce <= 0f) _annonce.text = "";
            }
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

        static Text Texte(Transform parent, Font police, string contenu, int taille,
                          TextAnchor ancre, Color couleur)
        {
            var go = new GameObject("Texte", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = police; t.text = contenu; t.fontSize = taille;
            t.alignment = ancre; t.color = couleur; t.raycastTarget = false;
            return t;
        }
    }
}
