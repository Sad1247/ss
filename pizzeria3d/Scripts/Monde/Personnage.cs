using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La silhouette commune au pizzaiolo et aux employes. Les jambes et les
    /// bras pendent a des pivots — hanches et epaules — sans quoi rien ne
    /// pourrait tourner : une capsule posee a sa place ne sait pas se balancer.
    /// </summary>
    public static class Personnage
    {
        public sealed class Membres
        {
            public Transform Corps;
            public Transform HancheG, HancheD;
            public Transform EpauleG, EpauleD;
        }

        public static Membres Construire(Transform parent, Color tshirt, Color casquette)
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
                             new Vector3(0.26f, 0.30f, 0.26f), Bloc.Pantalon).SansCollision();
                Bloc.Galet("Pied", hanche, new Vector3(0f, -0.69f, 0.06f),
                           new Vector3(0.28f, 0.18f, 0.40f), Bloc.Pantalon).SansCollision();
            }

            Bloc.Galet("Torse", t, new Vector3(0f, 0.95f, 0f),
                       new Vector3(0.62f, 0.62f, 0.46f), tshirt).SansCollision();

            m.EpauleG = Pivot("EpauleG", t, new Vector3(-0.28f, 1.02f, 0f));
            m.EpauleD = Pivot("EpauleD", t, new Vector3(0.28f, 1.02f, 0f));
            foreach (var epaule in new[] { m.EpauleG, m.EpauleD })
            {
                Bloc.Galet("Manche", epaule, Vector3.zero,
                           new Vector3(0.26f, 0.30f, 0.28f), tshirt).SansCollision();
                Bloc.Capsule("Bras", epaule, new Vector3(-0.04f, -0.26f, 0f),
                             new Vector3(0.19f, 0.20f, 0.19f), Bloc.Peau).SansCollision();
            }

            Bloc.Bille("Tete", t, new Vector3(0f, 1.42f, 0f), 0.52f, Bloc.Peau).SansCollision();
            Bloc.Galet("Calotte", t, new Vector3(0f, 1.52f, -0.01f),
                       new Vector3(0.60f, 0.44f, 0.60f), casquette).SansCollision();
            Bloc.Galet("Visiere", t, new Vector3(0f, 1.46f, 0.26f),
                       new Vector3(0.46f, 0.10f, 0.34f), casquette, -12f).SansCollision();

            return m;
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
