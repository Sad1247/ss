using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La silhouette commune au pizzaiolo et aux employes : jambes et pieds
    /// separes, t-shirt a manches courtes, bras nus, casquette bombee a
    /// visiere. Seules les couleurs changent d'un personnage a l'autre.
    /// </summary>
    public static class Personnage
    {
        /// <summary>Construit le corps sous <paramref name="parent"/> et le renvoie.</summary>
        public static Transform Construire(Transform parent, Color tshirt, Color casquette)
        {
            var corps = new GameObject("Corps");
            corps.transform.SetParent(parent, false);
            var t = corps.transform;

            Bloc.Capsule("JambeG", t, new Vector3(-0.15f, 0.42f, 0f),
                         new Vector3(0.26f, 0.30f, 0.26f), Bloc.Pantalon).SansCollision();
            Bloc.Capsule("JambeD", t, new Vector3(0.15f, 0.42f, 0f),
                         new Vector3(0.26f, 0.30f, 0.26f), Bloc.Pantalon).SansCollision();
            Bloc.Galet("PiedG", t, new Vector3(-0.15f, 0.09f, 0.06f),
                       new Vector3(0.28f, 0.18f, 0.40f), Bloc.Pantalon).SansCollision();
            Bloc.Galet("PiedD", t, new Vector3(0.15f, 0.09f, 0.06f),
                       new Vector3(0.28f, 0.18f, 0.40f), Bloc.Pantalon).SansCollision();

            Bloc.Galet("Torse", t, new Vector3(0f, 0.95f, 0f),
                       new Vector3(0.62f, 0.62f, 0.46f), tshirt).SansCollision();
            Bloc.Galet("MancheG", t, new Vector3(-0.28f, 1.02f, 0f),
                       new Vector3(0.26f, 0.30f, 0.28f), tshirt).SansCollision();
            Bloc.Galet("MancheD", t, new Vector3(0.28f, 1.02f, 0f),
                       new Vector3(0.26f, 0.30f, 0.28f), tshirt).SansCollision();

            Bloc.Capsule("BrasG", t, new Vector3(-0.32f, 0.76f, 0f),
                         new Vector3(0.19f, 0.20f, 0.19f), Bloc.Peau).SansCollision();
            Bloc.Capsule("BrasD", t, new Vector3(0.32f, 0.76f, 0f),
                         new Vector3(0.19f, 0.20f, 0.19f), Bloc.Peau).SansCollision();

            Bloc.Bille("Tete", t, new Vector3(0f, 1.42f, 0f), 0.52f, Bloc.Peau).SansCollision();
            Bloc.Galet("Calotte", t, new Vector3(0f, 1.52f, -0.01f),
                       new Vector3(0.60f, 0.44f, 0.60f), casquette).SansCollision();
            Bloc.Galet("Visiere", t, new Vector3(0f, 1.46f, 0.26f),
                       new Vector3(0.46f, 0.10f, 0.34f), casquette, -12f).SansCollision();

            return t;
        }
    }
}
