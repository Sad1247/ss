using UnityEngine;
using UnityEngine.UI;

namespace Pizzeria3D
{
    /// <summary>Le strict necessaire a l'ecran : la caisse, et un indice contextuel.</summary>
    public sealed class Hud : MonoBehaviour
    {
        /// <summary>
        /// Texte affiche tant que le joueur est sur une zone d'achat. Chaque
        /// zone ne peut effacer que son propre message : sans cette regle, les
        /// autres dalles remettent l'indice a zero des qu'on quitte la leur.
        /// </summary>
        public static string Indice { get; private set; }
        static object _source;

        public static void MontrerIndice(object source, string texte)
        {
            _source = source;
            Indice = texte;
        }

        public static void EffacerIndice(object source)
        {
            if (_source != source) return;
            _source = null;
            Indice = null;
        }

        static Hud _instance;
        Text _argent, _indice, _annonce, _portees, _heure, _service;
        Joueur _joueur;
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
            rt.sizeDelta = new Vector2(300f, 100f);
            var fond = carte.AddComponent<Image>();
            fond.color = Bloc.Couleur(0x5CD65C);        // la pastille verte du genre

            _argent = Texte(carte.transform, police, "0", 56, TextAnchor.MiddleCenter, Bloc.Couleur(0x18400F));
            Etirer(_argent.GetComponent<RectTransform>());

            // L'heure, en haut au centre : la caisse tient le coin droit et la
            // pile portee le coin gauche, c'est la seule place libre.
            var pendule = new GameObject("Pendule", typeof(RectTransform));
            pendule.transform.SetParent(canvasGo.transform, false);
            var rh = pendule.GetComponent<RectTransform>();
            rh.anchorMin = new Vector2(0.5f, 1f); rh.anchorMax = new Vector2(0.5f, 1f);
            rh.pivot = new Vector2(0.5f, 1f);
            rh.anchoredPosition = new Vector2(0f, -30f);
            rh.sizeDelta = new Vector2(360f, 100f);
            pendule.AddComponent<Image>().color = Bloc.Couleur(0x1E2430);

            _heure = Texte(pendule.transform, police, "", 46, TextAnchor.UpperCenter,
                           Bloc.Couleur(0xFFE7A8));
            var rt2 = _heure.GetComponent<RectTransform>();
            rt2.anchorMin = new Vector2(0f, 1f); rt2.anchorMax = new Vector2(1f, 1f);
            rt2.pivot = new Vector2(0.5f, 1f);
            rt2.anchoredPosition = new Vector2(0f, -8f);
            rt2.sizeDelta = new Vector2(0f, 56f);

            // OUVERT / FERME sous l'heure : c'est la reponse a « pourquoi
            // plus personne n'entre ? ».
            _service = Texte(pendule.transform, police, "", 30, TextAnchor.UpperCenter,
                             Bloc.Couleur(0x8CE99A));
            var rs = _service.GetComponent<RectTransform>();
            rs.anchorMin = new Vector2(0f, 1f); rs.anchorMax = new Vector2(1f, 1f);
            rs.pivot = new Vector2(0.5f, 1f);
            rs.anchoredPosition = new Vector2(0f, -62f);
            rs.sizeDelta = new Vector2(0f, 34f);

            // pile portee : sans ce compteur, rien ne dit au joueur qu'il a
            // bien charge des pizzas au four
            _portees = Texte(canvasGo.transform, police, "", 44, TextAnchor.UpperLeft, Color.white);
            var rp = _portees.GetComponent<RectTransform>();
            rp.anchorMin = new Vector2(0f, 1f); rp.anchorMax = new Vector2(0f, 1f);
            rp.pivot = new Vector2(0f, 1f);
            rp.anchoredPosition = new Vector2(34f, -34f); rp.sizeDelta = new Vector2(420f, 70f);

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

        void Rafraichir() => _argent.text = Banque.Solde.ToString();

        void Update()
        {
            if (_joueur == null) _joueur = Object.FindObjectOfType<Joueur>();
            if (_joueur != null && _joueur.Portee != null)
                _portees.text = _joueur.Portee.Nombre > 0
                    ? $"Pizzas portees : {_joueur.Portee.Nombre} / {Reglages.CapacitePortee}"
                    : "";

            if (Horloge.Active != null)
            {
                _heure.text = Horloge.Active.Affichage;
                bool ouverte = Horloge.Active.Ouverte;
                _service.text = ouverte ? "OUVERT" : "FERME";
                _service.color = ouverte ? Bloc.Couleur(0x8CE99A) : Bloc.Couleur(0xFF8A7A);
            }

            _indice.text = Indice ?? "";
            if (_tempsAnnonce > 0f)
            {
                _tempsAnnonce -= Time.deltaTime;
                if (_tempsAnnonce <= 0f) _annonce.text = "";
            }
        }

        public static Font Police()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            return f;
        }

        public static void Etirer(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        public static Text Texte(Transform parent, Font police, string contenu, int taille,
                          TextAnchor ancre, Color couleur)
        {
            var go = new GameObject("Texte", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = police; t.text = contenu; t.fontSize = taille;
            t.alignment = ancre; t.color = couleur; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
