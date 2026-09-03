using System.Collections.Generic;
using BellaNotte.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BellaNotte.Unity
{
    /// <summary>
    /// Construit toute l'interface par code (aucune scene a assembler a la main)
    /// et la tient a jour. Ne contient aucune regle de jeu : elle lit l'etat du
    /// moteur et lui envoie des intentions.
    /// </summary>
    [RequireComponent(typeof(GameRunner))]
    public sealed class GameView : MonoBehaviour, IGameView
    {
        // --- palette, reprise du prototype ---
        static readonly Color Fond      = Couleur(0x140E0C);
        static readonly Color Panneau   = Couleur(0x2A1D18);
        static readonly Color Panneau2  = Couleur(0x3A2A22);
        static readonly Color Ligne     = Couleur(0x523A2E);
        static readonly Color Creme     = Couleur(0xF4E7D3);
        static readonly Color Or        = Couleur(0xE8B44A);
        static readonly Color Vert      = Couleur(0x7BAB4A);
        static readonly Color Rouge     = Couleur(0xC8452F);
        static readonly Color Ok        = Couleur(0x6FBF5F);
        static readonly Color Mauvais   = Couleur(0xE05A45);
        static readonly Color Papier    = Couleur(0xF6F0E2);
        static readonly Color Encre     = Couleur(0x3A2A22);

        static Color Couleur(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

        GameRunner _runner;
        PizzeriaGame Game => _runner.Game;
        Font _font;

        // widgets
        Text _hudRecette, _hudNiveau, _hudServies, _hudPerdus, _message, _finTexte;
        RectTransform _ticketsRoot;
        Image _fourJauge;
        GameObject _panneauFin;
        PizzaRenderer _pizza;

        readonly List<TicketWidget> _tickets = new List<TicketWidget>();
        readonly Dictionary<IngredientId, Button> _boutonsIngredients = new Dictionary<IngredientId, Button>();
        Button _btnEnfourner, _btnSortir, _btnServir, _btnJeter;

        float _derniereCuissonDessinee = -1f;
        int _derniereCompositionDessinee = -1;

        sealed class TicketWidget
        {
            public int OrderId;
            public GameObject Root;
            public Image Fond;
            public Image Patience;
        }

        void Awake()
        {
            _runner = GetComponent<GameRunner>();
            _font = ChargerPolice();
            _pizza = new PizzaRenderer();
            ConstruireUI();
            _runner.Brancher(this);
        }

        static Font ChargerPolice()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            if (f == null) Debug.LogWarning("Police integree introuvable : les textes resteront invisibles.");
            return f;
        }

        // ------------------------------------------------------------------
        // construction
        // ------------------------------------------------------------------

        void ConstruireUI()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var fond = Bloc("Fond", canvasGo.transform, Fond);
            Etirer(fond.GetComponent<RectTransform>());

            var colonne = Vide("Colonne", fond.transform);
            Etirer(colonne.GetComponent<RectTransform>());
            var vl = colonne.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(24, 24, 24, 24);
            vl.spacing = 16;
            vl.childForceExpandHeight = false;
            vl.childControlHeight = true;
            vl.childControlWidth = true;
            vl.childForceExpandWidth = true;

            ConstruireHud(colonne.transform);
            ConstruireTickets(colonne.transform);
            ConstruirePizza(colonne.transform);
            ConstruireFour(colonne.transform);
            _message = Texte(colonne.transform, "Choisis une commande, garnis, enfourne.", 30, TextAnchor.MiddleCenter, Creme);
            Hauteur(_message.gameObject, 46);
            ConstruireIngredients(colonne.transform);
            ConstruireActions(colonne.transform);
            ConstruireFin(canvasGo.transform);
        }

        void ConstruireHud(Transform parent)
        {
            var ligne = Vide("Hud", parent);
            var h = ligne.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12;
            h.childControlWidth = true; h.childForceExpandWidth = true;
            h.childControlHeight = true; h.childForceExpandHeight = true;
            Hauteur(ligne, 110);

            _hudRecette = Stat(ligne.transform, "Recette");
            _hudNiveau  = Stat(ligne.transform, "Service");
            _hudServies = Stat(ligne.transform, "Servies");
            _hudPerdus  = Stat(ligne.transform, "Perdus");
        }

        Text Stat(Transform parent, string titre)
        {
            var carte = Bloc("Stat" + titre, parent, Panneau);
            var v = carte.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(6, 6, 8, 8);
            v.childControlHeight = true; v.childForceExpandHeight = true;
            v.childControlWidth = true; v.childForceExpandWidth = true;

            Texte(carte.transform, titre, 22, TextAnchor.MiddleCenter, Couleur(0xC9A179));
            return Texte(carte.transform, "0", 34, TextAnchor.MiddleCenter, Or);
        }

        void ConstruireTickets(Transform parent)
        {
            var carte = Bloc("Commandes", parent, Panneau);
            Hauteur(carte, 300);
            var h = carte.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(10, 10, 10, 10);
            h.spacing = 10;
            h.childControlWidth = true; h.childForceExpandWidth = true;
            h.childControlHeight = true; h.childForceExpandHeight = true;
            _ticketsRoot = carte.GetComponent<RectTransform>();
        }

        void ConstruirePizza(Transform parent)
        {
            var zone = Vide("Pizza", parent);
            Hauteur(zone, 420);
            var img = zone.AddComponent<Image>();
            img.sprite = _pizza.Sprite;
            img.preserveAspect = true;
            img.color = Color.white;

            // taper la pizza = sortir du four
            var btn = zone.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { if (Game.PizzaEnCours != null && Game.PizzaEnCours.AuFour) Game.SortirDuFour(); });
        }

        void ConstruireFour(Transform parent)
        {
            var barre = Bloc("Four", parent, Couleur(0x241713));
            Hauteur(barre, 42);

            var jauge = Vide("Jauge", barre.transform);
            Etirer(jauge.GetComponent<RectTransform>(), 4);
            _fourJauge = jauge.AddComponent<Image>();
            _fourJauge.color = Vert;
            _fourJauge.type = Image.Type.Filled;
            _fourJauge.fillMethod = Image.FillMethod.Horizontal;
            _fourJauge.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fourJauge.fillAmount = 0f;

            // reperes de la zone de cuisson parfaite
            var zone = Vide("ZoneParfaite", barre.transform);
            var rt = zone.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(GameConfig.ZoneParfaiteMin / GameConfig.CuissonMax, 0f);
            rt.anchorMax = new Vector2(GameConfig.ZoneParfaiteMax / GameConfig.CuissonMax, 1f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = zone.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.28f);
            img.raycastTarget = false;
        }

        void ConstruireIngredients(Transform parent)
        {
            var carte = Bloc("Ingredients", parent, Panneau);
            Hauteur(carte, 340);
            var g = carte.AddComponent<GridLayoutGroup>();
            g.padding = new RectOffset(10, 10, 10, 10);
            g.spacing = new Vector2(10, 10);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 4;
            g.cellSize = new Vector2(238, 96);

            foreach (var ing in Ingredients.Tous)
            {
                var id = ing.Id;
                var b = Bouton(carte.transform, ing.Nom, Panneau2, Creme, 24, () => Game.Basculer(id));
                _boutonsIngredients[id] = b;
            }
        }

        void ConstruireActions(Transform parent)
        {
            var ligne = Vide("Actions", parent);
            Hauteur(ligne, 110);
            var h = ligne.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12;
            h.childControlWidth = true; h.childForceExpandWidth = true;
            h.childControlHeight = true; h.childForceExpandHeight = true;

            _btnEnfourner = Bouton(ligne.transform, "Enfourner", Or, Encre, 30, () => Game.Enfourner());
            _btnSortir    = Bouton(ligne.transform, "Sortir !",  Vert, Encre, 30, () => Game.SortirDuFour());
            _btnServir    = Bouton(ligne.transform, "Servir",    Or,   Encre, 30, () => Game.Servir());
            _btnJeter     = Bouton(ligne.transform, "Jeter",     Rouge, Creme, 30, () => Game.Jeter());
        }

        void ConstruireFin(Transform parent)
        {
            _panneauFin = Bloc("Fin", parent, new Color(0.05f, 0.03f, 0.03f, 0.94f));
            Etirer(_panneauFin.GetComponent<RectTransform>());
            var v = _panneauFin.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(60, 60, 60, 60);
            v.spacing = 30;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlHeight = true; v.childForceExpandHeight = false;
            v.childControlWidth = true; v.childForceExpandWidth = true;

            var titre = Texte(_panneauFin.transform, "Le patron ferme boutique", 52, TextAnchor.MiddleCenter, Or);
            Hauteur(titre.gameObject, 90);
            _finTexte = Texte(_panneauFin.transform, "", 32, TextAnchor.UpperCenter, Creme);
            Hauteur(_finTexte.gameObject, 200);

            var rejouer = Bouton(_panneauFin.transform, "Rouvrir la pizzeria", Or, Encre, 34, () =>
            {
                _panneauFin.SetActive(false);
                _runner.Rejouer();
            });
            Hauteur(rejouer.gameObject, 110);

            _panneauFin.SetActive(false);
        }

        // ------------------------------------------------------------------
        // helpers de construction
        // ------------------------------------------------------------------

        static GameObject Vide(string nom, Transform parent)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject Bloc(string nom, Transform parent, Color couleur)
        {
            var go = Vide(nom, parent);
            var img = go.AddComponent<Image>();
            img.color = couleur;
            return go;
        }

        static void Etirer(RectTransform rt, float marge = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(marge, marge);
            rt.offsetMax = new Vector2(-marge, -marge);
        }

        static void Hauteur(GameObject go, float h)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredHeight = h;
            le.minHeight = h;
            le.flexibleHeight = 0f;
        }

        Text Texte(Transform parent, string contenu, int taille, TextAnchor ancre, Color couleur)
        {
            var go = Vide("Texte", parent);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.text = contenu;
            t.fontSize = taille;
            t.alignment = ancre;
            t.color = couleur;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        Button Bouton(Transform parent, string label, Color fond, Color texte, int taille,
                      UnityEngine.Events.UnityAction action)
        {
            var go = Bloc("Btn" + label, parent, fond);
            var b = go.AddComponent<Button>();
            b.targetGraphic = go.GetComponent<Image>();
            b.onClick.AddListener(action);

            var t = Texte(go.transform, label, taille, TextAnchor.MiddleCenter, texte);
            Etirer(t.GetComponent<RectTransform>(), 4);
            return b;
        }

        // ------------------------------------------------------------------
        // mise a jour continue
        // ------------------------------------------------------------------

        void LateUpdate()
        {
            var p = Game.PizzaEnCours;

            _hudRecette.text = Game.Recette + " EUR";
            _hudNiveau.text  = Game.Niveau.ToString();
            _hudServies.text = Game.PizzasServies.ToString();
            _hudPerdus.text  = Game.ClientsPerdus + " / " + GameConfig.ClientsPerdusMax;

            foreach (var t in _tickets)
            {
                var o = TrouverCommande(t.OrderId);
                if (o == null) continue;
                float r = o.RatioPatience;
                t.Patience.fillAmount = r;
                t.Patience.color = r < 0.25f ? Mauvais : r < 0.55f ? Or : Vert;
                bool active = Game.CommandeActive != null && Game.CommandeActive.Id == o.Id;
                t.Fond.color = active ? Papier : new Color(Papier.r, Papier.g, Papier.b, 0.72f);
            }

            float cuisson = p != null ? p.Cuisson : 0f;
            _fourJauge.fillAmount = Mathf.Clamp01(cuisson / GameConfig.CuissonMax);
            _fourJauge.color = p != null && p.Etat == EtatCuisson.Parfaite ? Vert
                             : cuisson > GameConfig.SeuilCramee ? Mauvais : Or;

            bool modifiable = p != null && p.EstModifiable;
            foreach (var kv in _boutonsIngredients)
            {
                kv.Value.interactable = modifiable;
                bool pose = p != null && (Ingredients.EstBase(kv.Key)
                    ? (p.Base.HasValue && p.Base.Value == kv.Key)
                    : Contient(p.Garnitures, kv.Key));
                var img = kv.Value.targetGraphic as Image;
                if (img != null) img.color = pose ? Ligne : Panneau2;
            }

            _btnEnfourner.interactable = p != null && p.EstEnfournable;
            _btnSortir.interactable    = p != null && p.AuFour;
            _btnServir.interactable    = p != null && p.Sortie;
            _btnJeter.interactable     = p != null;

            if (p != null) RedessinerSiBesoin(p);
        }

        void RedessinerSiBesoin(Pizza p)
        {
            int composition = p.Base.HasValue ? ((int)p.Base.Value + 1) : 0;
            for (int i = 0; i < p.Garnitures.Count; i++) composition = composition * 31 + (int)p.Garnitures[i] + 1;

            // on ne regenere la texture que si l'image change vraiment
            if (composition == _derniereCompositionDessinee &&
                Mathf.Abs(p.Cuisson - _derniereCuissonDessinee) < 0.02f) return;

            _derniereCompositionDessinee = composition;
            _derniereCuissonDessinee = p.Cuisson;
            _pizza.Dessiner(p.Base, p.Garnitures, p.Cuisson);
        }

        static bool Contient(IReadOnlyList<IngredientId> liste, IngredientId id)
        {
            for (int i = 0; i < liste.Count; i++) if (liste[i] == id) return true;
            return false;
        }

        Order TrouverCommande(int id)
        {
            var c = Game.Commandes;
            for (int i = 0; i < c.Count; i++) if (c[i].Id == id) return c[i];
            return null;
        }

        // ------------------------------------------------------------------
        // IGameView
        // ------------------------------------------------------------------

        public void OnCommandesChangees(Order _)
        {
            foreach (var t in _tickets) Destroy(t.Root);
            _tickets.Clear();

            foreach (var o in Game.Commandes)
            {
                var carte = Bloc("Ticket" + o.Id, _ticketsRoot, Papier);
                var v = carte.AddComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(8, 8, 8, 8);
                v.spacing = 4;
                v.childControlHeight = true; v.childForceExpandHeight = false;
                v.childControlWidth = true; v.childForceExpandWidth = true;

                var entete = Texte(carte.transform, o.Client + "   " + o.Recette.Prix + " EUR", 24, TextAnchor.UpperLeft, Encre);
                Hauteur(entete.gameObject, 30);
                var nom = Texte(carte.transform, o.Recette.Nom, 26, TextAnchor.UpperLeft, Rouge);
                Hauteur(nom.gameObject, 34);

                var lignes = Ingredients.Nom(o.Recette.Base);
                foreach (var g in o.Recette.Garnitures) lignes += "\n" + Ingredients.Nom(g);
                var liste = Texte(carte.transform, lignes, 20, TextAnchor.UpperLeft, Encre);
                Hauteur(liste.gameObject, 130);

                var barre = Bloc("Patience", carte.transform, Couleur(0xD8CDB6));
                Hauteur(barre, 12);
                var fill = Vide("Fill", barre.transform);
                Etirer(fill.GetComponent<RectTransform>());
                var img = fill.AddComponent<Image>();
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
                img.color = Vert;
                img.raycastTarget = false;

                int id = o.Id;
                var b = carte.AddComponent<Button>();
                b.targetGraphic = carte.GetComponent<Image>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Game.Selectionner(id));

                _tickets.Add(new TicketWidget
                {
                    OrderId = o.Id,
                    Root = carte,
                    Fond = carte.GetComponent<Image>(),
                    Patience = img,
                });
            }
        }

        public void OnClientParti(Order o)
        {
            Afficher(o.Client + " est parti sans attendre sa " + o.Recette.Nom + ".", Mauvais);
        }

        public void OnPizzaChangee() { /* le rendu suit dans LateUpdate */ }

        public void OnPizzaServie(ServiceResult r)
        {
            Afficher(ScoreRules.Resume(r), r.EstParfaite ? Ok : r.EstRefusee ? Mauvais : Creme);
        }

        public void OnNiveauMonte(int niveau)
        {
            Afficher("Service " + niveau + " : ca se remplit, les clients sont presses.", Or);
        }

        public void OnMessage(string texte) => Afficher(texte, Creme);

        public void OnPartieTerminee()
        {
            _finTexte.text =
                "Trois clients perdus, ca suffit.\n\n" +
                Game.Recette + " EUR de recette\n" +
                Game.PizzasServies + " pizzas servies\n" +
                "service " + Game.Niveau;
            _panneauFin.SetActive(true);
        }

        void Afficher(string texte, Color couleur)
        {
            _message.text = texte;
            _message.color = couleur;
        }
    }
}
