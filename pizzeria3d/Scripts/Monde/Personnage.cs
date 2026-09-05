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
        }

        public sealed class Membres
        {
            public Transform Corps;
            public Transform HancheG, HancheD;
            public Transform EpauleG, EpauleD;
        }

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

            m.HancheG = Pivot("HancheG", t, new Vector3(-0.15f, 0.78f, 0f));
            m.HancheD = Pivot("HancheD", t, new Vector3(0.15f, 0.78f, 0f));
            foreach (var hanche in new[] { m.HancheG, m.HancheD })
            {
                Bloc.Capsule("Jambe", hanche, new Vector3(0f, -0.36f, 0f),
                             new Vector3(0.26f, 0.30f, 0.26f), a.Pantalon).SansCollision();
                Bloc.Galet("Chaussure", hanche, new Vector3(0f, -0.69f, 0.06f),
                           new Vector3(0.28f, 0.18f, 0.40f), a.Chaussures).SansCollision();
            }

            Bloc.Galet("Torse", t, new Vector3(0f, 0.95f, 0.01f),
                       new Vector3(0.62f, 0.62f, 0.46f), a.Tshirt).SansCollision();

            // Le bassin est accroche au buste, pas aux hanches : suspendu aux
            // pivots, il se balancerait avec les jambes.
            Bloc.Galet("FesseG", t, new Vector3(-0.13f, 0.74f, -0.13f),
                       new Vector3(0.30f, 0.28f, 0.26f), a.Pantalon).SansCollision();
            Bloc.Galet("FesseD", t, new Vector3(0.13f, 0.74f, -0.13f),
                       new Vector3(0.30f, 0.28f, 0.26f), a.Pantalon).SansCollision();

            m.EpauleG = Pivot("EpauleG", t, new Vector3(-0.28f, 1.02f, 0f));
            m.EpauleD = Pivot("EpauleD", t, new Vector3(0.28f, 1.02f, 0f));
            foreach (var epaule in new[] { m.EpauleG, m.EpauleD })
            {
                Bloc.Galet("Manche", epaule, Vector3.zero,
                           new Vector3(0.26f, 0.30f, 0.28f), a.Tshirt).SansCollision();
                Bloc.Capsule("Bras", epaule, new Vector3(-0.04f, -0.26f, 0f),
                             new Vector3(0.19f, 0.20f, 0.19f), a.Peau).SansCollision();
            }

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

        static Transform Pivot(string nom, Transform parent, Vector3 position)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }
    }
}
