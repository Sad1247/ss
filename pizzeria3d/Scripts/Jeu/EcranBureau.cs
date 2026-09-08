using UnityEngine;
using UnityEngine.UI;

namespace Pizzeria3D
{
    /// <summary>
    /// L'ecran de l'ordinateur du bureau, affiche par-dessus le jeu des que le
    /// patron s'assoit : un bureau facon Ubuntu — barre du haut, dock
    /// aubergine, une fenetre — ou se lit la liste du personnel et ce qu'il
    /// coute. Il se referme des qu'il se leve : la manette sert deja a cela.
    /// </summary>
    public sealed class EcranBureau : MonoBehaviour
    {
        // les teintes de la distribution : aubergine, orange, gris Yaru
        static readonly Color Aubergine = Bloc.Couleur(0x2C001E);
        static readonly Color DockSombre = Bloc.Couleur(0x1D0413);
        static readonly Color Orange = Bloc.Couleur(0xE95420);
        static readonly Color Entete = Bloc.Couleur(0x303030);
        static readonly Color Papier = Bloc.Couleur(0xFAFAFA);
        static readonly Color Encre = Bloc.Couleur(0x3D3D3D);
        static readonly Color Filet = Bloc.Couleur(0xDCDCDC);
        static readonly Color Vert = Bloc.Couleur(0x0E8420);
        static readonly Color Gris = Bloc.Couleur(0x8A8A8A);

        public Joueur Joueur;
        /// <summary>Le portable du bureau : l'ecran suit son capot.</summary>
        public OrdinateurPortable Portable;

        GameObject _ecran;
        Text[] _statuts;
        Text _masse, _pendule;

        /// <summary>Vrai quand l'ecran est affiche par-dessus le jeu.</summary>
        public bool Ouvert => _ecran != null && _ecran.activeSelf;

        /// <summary>Combien de lignes de personnel sont affichees.</summary>
        public int Lignes => _statuts != null ? _statuts.Length : 0;

        /// <summary>Le statut affiche sur la ligne demandee.</summary>
        public string Statut(int ligne)
            => _statuts != null && ligne >= 0 && ligne < _statuts.Length
               ? _statuts[ligne].text : null;

        void Awake()
        {
            Construire();
            _ecran.SetActive(false);
        }

        void Update()
        {
            if (Joueur == null) Joueur = Object.FindObjectOfType<Joueur>();
            bool voulu = Portable != null
                ? Portable.Ouvert
                : Joueur != null && Joueur.Assis;
            if (voulu != Ouvert) _ecran.SetActive(voulu);
            if (voulu) Rafraichir();
        }

        /// <summary>
        /// Les seules colonnes qui changent en cours de partie : le poste est
        /// pourvu ou non, et la paie qui en decoule.
        /// </summary>
        void Rafraichir()
        {
            var fiches = Personnel.Fiches;
            for (int i = 0; i < _statuts.Length && i < fiches.Count; i++)
            {
                bool en = fiches[i].EstEmbauche;
                _statuts[i].text = en ? "En poste" : "Poste vacant";
                _statuts[i].color = en ? Vert : Gris;
            }
            _masse.text = "Masse salariale : " + Personnel.MasseSalariale + " / jour";

            // La barre du haut porte l'heure, comme sur un vrai bureau.
            if (Horloge.Active != null) _pendule.text = Horloge.Active.Affichage;
        }

        void Construire()
        {
            var police = Hud.Police();

            var canvasGo = new GameObject("EcranBureauCanvas",
                                          typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            // Au-dessus du HUD : l'ecran de l'ordinateur cache le jeu, il ne
            // se glisse pas dessous.
            canvasGo.GetComponent<Canvas>().overrideSorting = true;
            canvasGo.GetComponent<Canvas>().sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            _ecran = Cadre("EcranOrdi", canvasGo.transform, Aubergine,
                           new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(980f, 760f));

            // la barre du haut, puis le dock a gauche : c'est a ces deux
            // bandes qu'on reconnait le bureau au premier coup d'oeil
            var barre = Cadre("BarreHaut", _ecran.transform, DockSombre,
                              new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(980f, 44f));
            _pendule = Hud.Texte(barre.transform, police, "Pizzeria OS", 26,
                                 TextAnchor.MiddleCenter, Color.white);
            Hud.Etirer(_pendule.GetComponent<RectTransform>());

            var dock = Cadre("Dock", _ecran.transform, DockSombre,
                             new Vector2(0f, 0.5f), new Vector2(44f, -22f), new Vector2(88f, 716f));
            for (int i = 0; i < 4; i++)
                Cadre("Lanceur", dock.transform, i == 0 ? Orange : Bloc.Couleur(0x4A2338),
                      new Vector2(0.5f, 1f), new Vector2(0f, -50f - i * 96f), new Vector2(64f, 64f));

            // la fenetre elle-meme
            var fenetre = Cadre("Fenetre", _ecran.transform, Papier,
                                new Vector2(0.5f, 0.5f), new Vector2(44f, -30f), new Vector2(844f, 640f));

            var titre = Cadre("EnteteFenetre", fenetre.transform, Entete,
                              new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(844f, 68f));
            var nom = Hud.Texte(titre.transform, police, "  Personnel — Pizzeria", 34,
                                TextAnchor.MiddleLeft, Color.white);
            Hud.Etirer(nom.GetComponent<RectTransform>());
            Cadre("Fermer", titre.transform, Orange,
                  new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(34f, 34f));

            // l'en-tete du tableau, puis une ligne par fiche
            float y = -100f;
            Colonnes(fenetre.transform, police, y, "NOM", "POSTE", "STATUT", "SALAIRE", Orange, 26);
            y -= 26f;
            Cadre("Filet", fenetre.transform, Orange,
                  new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(780f, 4f));

            var fiches = Personnel.Fiches;
            _statuts = new Text[fiches.Count];
            for (int i = 0; i < fiches.Count; i++)
            {
                y -= 74f;
                var f = fiches[i];
                _statuts[i] = Colonnes(fenetre.transform, police, y, f.Nom, f.Poste, "",
                                       f.Salaire + " / jour", Encre, 30);
                y -= 40f;
                Cadre("Filet", fenetre.transform, Filet,
                      new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(780f, 2f));
            }

            _masse = Hud.Texte(fenetre.transform, police, "", 32, TextAnchor.MiddleLeft, Encre);
            var rm = _masse.GetComponent<RectTransform>();
            rm.anchorMin = new Vector2(0.5f, 1f); rm.anchorMax = new Vector2(0.5f, 1f);
            rm.pivot = new Vector2(0.5f, 0.5f);
            rm.anchoredPosition = new Vector2(-390f + 390f, y - 70f);
            rm.sizeDelta = new Vector2(780f, 44f);

            var pied = Hud.Texte(fenetre.transform, police,
                                 "Bouge pour te lever et refermer l'ecran", 26,
                                 TextAnchor.MiddleCenter, Gris);
            var rp = pied.GetComponent<RectTransform>();
            rp.anchorMin = new Vector2(0.5f, 0f); rp.anchorMax = new Vector2(0.5f, 0f);
            rp.pivot = new Vector2(0.5f, 0f);
            rp.anchoredPosition = new Vector2(0f, 26f);
            rp.sizeDelta = new Vector2(780f, 40f);
        }

        /// <summary>Une ligne du tableau. Rend la colonne « statut », la seule qui bouge.</summary>
        static Text Colonnes(Transform parent, Font police, float y, string nom, string poste,
                             string statut, string salaire, Color couleur, int taille)
        {
            Colonne(parent, police, -390f, y, 280f, nom, TextAnchor.MiddleLeft, couleur, taille);
            Colonne(parent, police, -100f, y, 190f, poste, TextAnchor.MiddleLeft, couleur, taille);
            // Les colonnes ne se recouvrent pas, meme quand leur texte est
            // court : sinon un salaire a quatre chiffres passerait sous le
            // statut.
            var t = Colonne(parent, police, 95f, y, 150f, statut, TextAnchor.MiddleLeft, couleur, taille);
            Colonne(parent, police, 250f, y, 140f, salaire, TextAnchor.MiddleRight, couleur, taille);
            return t;
        }

        static Text Colonne(Transform parent, Font police, float x, float y, float largeur,
                            string contenu, TextAnchor ancre, Color couleur, int taille)
        {
            var t = Hud.Texte(parent, police, contenu, taille, ancre, couleur);
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(largeur, 44f);
            return t;
        }

        static GameObject Cadre(string nom, Transform parent, Color couleur, Vector2 ancre,
                                Vector2 position, Vector2 taille)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancre; rt.anchorMax = ancre; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = taille;
            go.AddComponent<Image>().color = couleur;
            return go;
        }
    }
}
