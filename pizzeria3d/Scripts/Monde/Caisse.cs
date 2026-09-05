using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La caisse enregistreuse, batie d'apres le PSD fourni : corps creme
    /// arrondi, pupitre incline garni de deux blocs de touches, fente chrome,
    /// afficheur sur pied et grand tiroir a serrure.
    ///
    /// Son tiroir s'ouvre quand un client passe commande et se referme quand il
    /// repart : c'est le seul repere visuel qui dise au joueur qu'une commande
    /// est en cours. Il coulisse vers +z, cote caissier — jamais vers les
    /// clients, qui font la queue de l'autre cote.
    /// </summary>
    public sealed class Caisse : MonoBehaviour
    {
        const float Ferme = 0f;
        const float Ouvert = 0.5f;
        const float Vitesse = 2.6f;

        // couleurs relevees directement sur le PSD
        static readonly Color Creme    = Bloc.Couleur(0xEFDEB0);
        static readonly Color CremeBis = Bloc.Couleur(0xE5CA9C);
        static readonly Color Chrome   = Bloc.Couleur(0xC0C0C0);
        static readonly Color Acier    = Bloc.Couleur(0x9A9A9A);
        static readonly Color Blanc    = Bloc.Couleur(0xF5F5F5);
        static readonly Color Touche   = Bloc.Couleur(0xF3F3F3);
        static readonly Color Rouge    = Bloc.Couleur(0xE08080);
        static readonly Color Brun     = Bloc.Couleur(0x9A786C);
        static readonly Color Vert     = Bloc.Couleur(0x90C078);
        static readonly Color Billet   = Bloc.Couleur(0x7BC86B);

        Transform _tiroir;
        float _cible;

        public bool EstOuverte => _cible > 0.01f;

        public void Ouvrir() => _cible = Ouvert;
        public void Fermer() => _cible = Ferme;

        void Update()
        {
            if (_tiroir == null) return;
            var p = _tiroir.localPosition;
            p.z = Mathf.Lerp(p.z, _cible, Vitesse * Time.deltaTime);
            _tiroir.localPosition = p;
        }

        // ------------------------------------------------------------------

        public static Caisse Creer(Transform parent, Vector3 pos)
        {
            var go = new GameObject("Caisse");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(0.62f, 0.62f, 0.62f);
            var t = go.transform;

            Corps(t);
            Pupitre(t);
            Afficheur(t);
            var tiroir = Tiroir(t);

            var caisse = go.AddComponent<Caisse>();
            caisse._tiroir = tiroir;
            return caisse;
        }

        static void Corps(Transform t)
        {
            Bloc.Boite("Corps", t, new Vector3(0f, 0.35f, 0f), new Vector3(1.50f, 0.70f, 1.15f), Creme)
                .SansCollision();
            // le dos est plus haut que l'avant : c'est ce qui donne la silhouette
            Bloc.Boite("Dosseret", t, new Vector3(0f, 0.90f, -0.35f), new Vector3(1.50f, 0.44f, 0.45f), Creme)
                .SansCollision();
            Bloc.Boite("Semelle", t, new Vector3(0f, 0.04f, 0f), new Vector3(1.54f, 0.08f, 1.19f), CremeBis)
                .SansCollision();
        }

        /// <summary>Le plan incline qui porte les touches, la fente et le panneau.</summary>
        static void Pupitre(Transform t)
        {
            var pupitre = new GameObject("Pupitre");
            pupitre.transform.SetParent(t, false);
            pupitre.transform.localPosition = new Vector3(0f, 0.74f, 0.06f);
            pupitre.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            var p = pupitre.transform;

            Bloc.Boite("Plateau", p, Vector3.zero, new Vector3(1.42f, 0.10f, 0.92f), Creme).SansCollision();

            // fente chrome a gauche
            Bloc.Boite("Fente", p, new Vector3(-0.36f, 0.06f, -0.24f), new Vector3(0.36f, 0.05f, 0.24f), Chrome)
                .SansCollision();
            Bloc.Boite("Rainure", p, new Vector3(-0.36f, 0.09f, -0.24f), new Vector3(0.26f, 0.02f, 0.06f), Acier)
                .SansCollision();

            // panneau blanc a droite
            Bloc.Boite("Panneau", p, new Vector3(0.24f, 0.06f, -0.24f), new Vector3(0.44f, 0.05f, 0.28f), Blanc)
                .SansCollision();

            Touches(p);
        }

        /// <summary>Deux blocs de neuf touches, blanches sauf quelques accents.</summary>
        static void Touches(Transform p)
        {
            for (int bloc = 0; bloc < 2; bloc++)
            {
                float cx = bloc == 0 ? -0.33f : 0.33f;
                for (int rang = 0; rang < 3; rang++)
                for (int col = 0; col < 3; col++)
                {
                    var couleur = Accent(bloc, rang, col);
                    Bloc.Boite($"Touche{bloc}{rang}{col}", p,
                               new Vector3(cx + (col - 1) * 0.13f, 0.08f, 0.04f + rang * 0.12f),
                               new Vector3(0.105f, 0.07f, 0.095f), couleur).SansCollision();
                }
            }
        }

        static Color Accent(int bloc, int rang, int col)
        {
            if (bloc == 0)
            {
                if (rang == 0 && col == 0) return Rouge;
                if (rang == 2 && col == 1) return Brun;
                if (rang == 2 && col == 2) return Vert;
            }
            else
            {
                if (rang == 0 && col == 0) return Rouge;
                if (rang == 0 && col == 1) return Brun;
                if (rang == 0 && col == 2) return Vert;
                if (rang == 1 && col == 2) return Vert;
            }
            return Touche;
        }

        static void Afficheur(Transform t)
        {
            Bloc.Boite("Pied", t, new Vector3(0f, 1.20f, -0.34f), new Vector3(0.11f, 0.52f, 0.11f), CremeBis)
                .SansCollision();
            Bloc.Boite("Cadre", t, new Vector3(0f, 1.58f, -0.34f), new Vector3(0.68f, 0.46f, 0.12f), Chrome)
                .SansCollision();
            Bloc.Boite("Face", t, new Vector3(0f, 1.58f, -0.28f), new Vector3(0.54f, 0.33f, 0.03f), Blanc)
                .SansCollision();
        }

        static Transform Tiroir(Transform t)
        {
            var tiroir = new GameObject("Tiroir");
            tiroir.transform.SetParent(t, false);
            var d = tiroir.transform;

            Bloc.Boite("Caisson", d, new Vector3(0f, 0.22f, 0.02f), new Vector3(1.34f, 0.34f, 0.96f), CremeBis)
                .SansCollision();
            Bloc.Boite("Facade", d, new Vector3(0f, 0.22f, 0.56f), new Vector3(1.42f, 0.42f, 0.07f), CremeBis)
                .SansCollision();
            Bloc.Boite("Moulure", d, new Vector3(0f, 0.22f, 0.60f), new Vector3(1.14f, 0.28f, 0.02f), Creme)
                .SansCollision();
            Bloc.Rondelle("Serrure", d, new Vector3(0f, 0.22f, 0.62f), 0.15f, 0.06f, Chrome).SansCollision();
            Bloc.Boite("Barillet", d, new Vector3(0f, 0.21f, 0.64f), new Vector3(0.03f, 0.07f, 0.02f), Acier)
                .SansCollision();

            // les liasses n'apparaissent que tiroir ouvert
            for (int i = 0; i < 3; i++)
                Bloc.Boite("Liasse" + i, d, new Vector3(-0.38f + i * 0.38f, 0.40f, 0.06f),
                           new Vector3(0.30f, 0.05f, 0.46f), Billet).SansCollision();

            return d;
        }
    }
}
