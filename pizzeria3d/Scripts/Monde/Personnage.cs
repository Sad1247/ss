using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La silhouette commune a tout le monde : joueur, employes et clients.
    /// Les jambes et les bras pendent a des pivots — hanches et epaules — sans
    /// quoi rien ne pourrait tourner : une capsule posee a sa place ne sait pas
    /// se balancer.
    /// </summary>
    public static class Personnage
    {
        public enum Coiffure { Rien, Casquette, CheveuxCourts, CheveuxLongs }

        /// <summary>De quoi habiller quelqu'un sans toucher a sa construction.</summary>
        public sealed class Apparence
        {
            public Color Tshirt = Bloc.Tablier;
            public Color Pantalon = Bloc.Pantalon;
            public Color Chaussures = Bloc.Pantalon;
            public Color Peau = Bloc.Peau;
            public Color Cheveux = Bloc.Couleur(0x2E2018);
            public Coiffure Tete = Coiffure.Rien;

            /// <summary>Carrure : 0,80 pour un maigre, 1,35 pour un corpulent.</summary>
            public float Largeur = 1f;
            /// <summary>Taille generale, autour de 1.</summary>
            public float Hauteur = 1f;
        }

        public sealed class Membres
        {
            public Transform Corps;
            public Transform HancheG, HancheD;
            public Transform GenouG, GenouD;
            public Transform EpauleG, EpauleD;
            /// <summary>Point de portage, a hauteur des paumes bras tendus devant.</summary>
            public Transform Mains;
        }

        /// <summary>Inclinaison des epaules quand les bras tiennent une pile.</summary>
        public const float AngleBrasPortant = -72f;

        // Position d'une paume dans le repere de son epaule, bras pendant.
        const float HauteurEpaule = 1.02f;
        static readonly Vector3 PaumeAuRepos = new Vector3(0f, -0.47f, 0.01f);

        /// <summary>Raccourci pour le joueur et les employes, coiffes d'une casquette.</summary>
        public static Membres Construire(Transform parent, Color tshirt, Color casquette)
            => Construire(parent, new Apparence
            {
                Tshirt = tshirt,
                Cheveux = casquette,
                Tete = Coiffure.Casquette,
            });

        public static Membres Construire(Transform parent, Apparence a)
        {
            var corps = new GameObject("Corps");
            corps.transform.SetParent(parent, false);
            var t = corps.transform;

            var m = new Membres { Corps = t };
            t.localScale = new Vector3(a.Hauteur, a.Hauteur, a.Hauteur);

            float ecartJambes = 0.15f * Mathf.Lerp(1f, 1.25f, Mathf.Clamp01(a.Largeur - 1f));
            float grosseurJambe = 0.26f * Mathf.Lerp(1f, 1.30f, Mathf.Clamp01((a.Largeur - 0.8f) / 0.55f));

            m.HancheG = Pivot("HancheG", t, new Vector3(-ecartJambes, 0.78f, 0f));
            m.HancheD = Pivot("HancheD", t, new Vector3(ecartJambes, 0.78f, 0f));

            // La jambe est coupee en deux au genou. D'un seul tenant, elle
            // balayait le sol comme un baton : c'est le genou qui fait
            // reconnaitre une marche.
            for (int i = 0; i < 2; i++)
            {
                var hanche = i == 0 ? m.HancheG : m.HancheD;

                Bloc.Capsule("Cuisse", hanche, new Vector3(0f, -0.20f, 0f),
                             new Vector3(grosseurJambe, 0.17f, grosseurJambe), a.Pantalon)
                    .SansCollision();

                var genou = Pivot("Genou", hanche, new Vector3(0f, -0.40f, 0f));
                if (i == 0) m.GenouG = genou; else m.GenouD = genou;

                Bloc.Capsule("Mollet", genou, new Vector3(0f, -0.15f, 0f),
                             new Vector3(grosseurJambe * 0.92f, 0.16f, grosseurJambe * 0.92f),
                             a.Pantalon).SansCollision();
                Bloc.Galet("Chaussure", genou, new Vector3(0f, -0.29f, 0.06f),
                           new Vector3(0.28f, 0.18f, 0.40f), a.Chaussures).SansCollision();
            }

            Bloc.Galet("Torse", t, new Vector3(0f, 0.95f, 0.01f),
                       new Vector3(0.62f * a.Largeur, 0.62f, 0.46f * a.Largeur), a.Tshirt).SansCollision();

            // le ventre ne sort que sur les corpulents
            if (a.Largeur > 1.12f)
                Bloc.Galet("Ventre", t, new Vector3(0f, 0.86f, 0.10f),
                           new Vector3(0.58f * a.Largeur, 0.46f, 0.40f * a.Largeur), a.Tshirt)
                    .SansCollision();

            // Le bassin est accroche au buste, pas aux hanches : suspendu aux
            // pivots, il se balancerait avec les jambes.
            float ecartFesses = 0.115f * a.Largeur;
            foreach (float cote in new[] { -1f, 1f })
                Bloc.Galet("Fesse", t, new Vector3(cote * ecartFesses, 0.74f, -0.11f),
                           new Vector3(0.24f * a.Largeur, 0.22f, 0.20f), a.Pantalon).SansCollision();

            float ecartEpaules = 0.28f * a.Largeur;
            m.EpauleG = Pivot("EpauleG", t, new Vector3(-ecartEpaules, HauteurEpaule, 0f));
            m.EpauleD = Pivot("EpauleD", t, new Vector3(ecartEpaules, HauteurEpaule, 0f));
            for (int i = 0; i < 2; i++)
            {
                var epaule = i == 0 ? m.EpauleG : m.EpauleD;
                float cote = i == 0 ? -1f : 1f;   // le decalage doit etre en miroir

                Bloc.Galet("Manche", epaule, Vector3.zero,
                           new Vector3(0.26f, 0.30f, 0.28f), a.Tshirt).SansCollision();
                Bloc.Capsule("Bras", epaule, new Vector3(cote * 0.02f, -0.26f, 0f),
                             new Vector3(0.18f, 0.19f, 0.18f), a.Peau).SansCollision();
                Bloc.Bille("Main", epaule,
                           new Vector3(cote * 0.035f, PaumeAuRepos.y, PaumeAuRepos.z),
                           0.17f, a.Peau).SansCollision();
            }

            // Le point ou se rejoignent les deux paumes une fois les bras
            // tendus devant : c'est la que se pose la pile de pizzas. Il tient
            // au buste et non aux epaules, sinon la pile se balancerait avec
            // les bras au moindre pas.
            m.Mains = Pivot("Mains", t, PositionDesPaumes());

            Bloc.Bille("Tete", t, new Vector3(0f, 1.42f, 0f), 0.52f, a.Peau).SansCollision();
            Visage(t, a);
            Coiffer(t, a);
            return m;
        }

        /// <summary>
        /// Yeux et nez. Sans eux, une silhouette de dos et de face se
        /// ressemblent : rien ne disait dans quel sens marchait un personnage.
        /// </summary>
        static void Visage(Transform t, Apparence a)
        {
            var blanc = Bloc.Couleur(0xF7F3EA);
            var pupille = Bloc.Couleur(0x22201E);

            foreach (float cote in new[] { -1f, 1f })
            {
                Bloc.Galet("Oeil", t, new Vector3(cote * 0.115f, 1.455f, 0.205f),
                           new Vector3(0.135f, 0.155f, 0.07f), blanc).SansCollision();
                Bloc.Galet("Pupille", t, new Vector3(cote * 0.115f, 1.45f, 0.245f),
                           new Vector3(0.075f, 0.09f, 0.04f), pupille).SansCollision();
            }

            Bloc.Galet("Nez", t, new Vector3(0f, 1.375f, 0.235f),
                       new Vector3(0.10f, 0.09f, 0.12f),
                       Color.Lerp(a.Peau, Color.black, 0.12f)).SansCollision();
        }

        static void Coiffer(Transform t, Apparence a)
        {
            switch (a.Tete)
            {
                case Coiffure.Casquette:
                    Bloc.Galet("Calotte", t, new Vector3(0f, 1.52f, -0.01f),
                               new Vector3(0.60f, 0.44f, 0.60f), a.Cheveux).SansCollision();
                    Bloc.Galet("Visiere", t, new Vector3(0f, 1.46f, 0.26f),
                               new Vector3(0.46f, 0.10f, 0.34f), a.Cheveux, -12f).SansCollision();
                    break;

                case Coiffure.CheveuxCourts:
                    Bloc.Galet("Cheveux", t, new Vector3(0f, 1.50f, -0.02f),
                               new Vector3(0.56f, 0.42f, 0.56f), a.Cheveux).SansCollision();
                    break;

                case Coiffure.CheveuxLongs:
                    Bloc.Galet("Cheveux", t, new Vector3(0f, 1.50f, -0.02f),
                               new Vector3(0.58f, 0.46f, 0.58f), a.Cheveux).SansCollision();
                    // la masse qui retombe jusqu'aux epaules
                    Bloc.Galet("Natte", t, new Vector3(0f, 1.18f, -0.20f),
                               new Vector3(0.44f, 0.60f, 0.34f), a.Cheveux).SansCollision();
                    break;
            }
        }

        /// <summary>
        /// Ou tombent les paumes quand l'epaule pivote de AngleBrasPortant :
        /// la meme rotation autour de X que celle appliquee par la demarche.
        /// </summary>
        static Vector3 PositionDesPaumes()
        {
            float r = AngleBrasPortant * Mathf.Deg2Rad;
            float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
            return new Vector3(
                0f,
                HauteurEpaule + PaumeAuRepos.y * cos - PaumeAuRepos.z * sin,
                PaumeAuRepos.y * sin + PaumeAuRepos.z * cos);
        }

        static Transform Pivot(string nom, Transform parent, Vector3 position)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }
    }
}
