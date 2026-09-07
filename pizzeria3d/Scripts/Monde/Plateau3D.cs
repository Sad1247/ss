using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le plateau de la salle : on y sert la pizza de celui qui mange sur
    /// place. Un fond et quatre rebords, rien de plus — mais c'est ce qui
    /// distingue le repas servi de la boite a emporter.
    /// </summary>
    public static class Plateau3D
    {
        public const float Cote = 1.02f, Profondeur = 0.80f, Hauteur = 0.09f;

        /// <summary>
        /// Un plateau, avec sa pizza entiere dessus si on le demande. Porte a
        /// bout de bras, il se presente dans la longueur — de face pour celui
        /// qu'on sert, et non en travers.
        /// </summary>
        public static GameObject Creer(Transform parent, int index, bool avecPizza,
                                       bool dansLaLongueur = false)
        {
            var racine = new GameObject("Plateau" + index);
            racine.transform.SetParent(parent, false);
            racine.transform.localPosition = new Vector3(0f, index * Reglages.EpaisseurPlateau, 0f);
            if (dansLaLongueur) racine.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var corps = Bloc.Couleur(0xB4462F);
            var rebord = Bloc.Couleur(0x8E3220);

            Bloc.Boite("Fond", racine.transform, new Vector3(0f, 0.03f, 0f),
                       new Vector3(Cote, 0.06f, Profondeur), corps).SansCollision();
            foreach (float x in new[] { -1f, 1f })
                Bloc.Boite("Rebord", racine.transform, new Vector3(x * Cote * 0.5f, 0.055f, 0f),
                           new Vector3(0.05f, 0.09f, Profondeur), rebord).SansCollision();
            foreach (float z in new[] { -1f, 1f })
                Bloc.Boite("Rebord", racine.transform, new Vector3(0f, 0.055f, z * Profondeur * 0.5f),
                           new Vector3(Cote, 0.09f, 0.05f), rebord).SansCollision();

            if (avecPizza) PoserDessus(Pizza3D.Creer(racine.transform, 0));
            return racine;
        }

        /// <summary>Cale un objet sur le plateau, juste au-dessus du fond.</summary>
        public static GameObject PoserDessus(GameObject objet)
        {
            var p = objet.transform.localPosition;
            objet.transform.localPosition = new Vector3(p.x, Hauteur * 0.75f, p.z);
            return objet;
        }
    }
}
