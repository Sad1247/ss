using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La boite a pizza : un carton carre, son couvercle et la pastille rouge
    /// imprimee dessus. Meme sobriete que la pizza — quelques primitives, des
    /// materiaux partages — parce qu'il peut y en avoir trente a l'ecran.
    /// </summary>
    public static class Boite3D
    {
        public static GameObject Creer(Transform parent, int index)
        {
            var racine = new GameObject("Boite" + index);
            racine.transform.SetParent(parent, false);
            racine.transform.localPosition = new Vector3(0f, index * Reglages.EpaisseurBoite, 0f);
            // legerement de travers, comme les pizzas : une pile au cordeau
            // trahit le decor genere
            racine.transform.localRotation = Quaternion.Euler(0f, (index * 23f) % 360f, 0f);

            float c = Reglages.LargeurBoite;
            float h = Reglages.EpaisseurBoite;

            Bloc.Boite("Carton", racine.transform, new Vector3(0f, h * 0.42f, 0f),
                       new Vector3(c, h * 0.84f, c), Bloc.Carton).SansCollision();
            // le couvercle deborde a peine : c'est ce qui la distingue d'un cube
            Bloc.Boite("Couvercle", racine.transform, new Vector3(0f, h * 0.90f, 0f),
                       new Vector3(c * 1.04f, h * 0.20f, c * 1.04f), Bloc.CartonClair)
                .SansCollision();
            // la rainure de fermeture, sur le devant
            Bloc.Boite("Rainure", racine.transform, new Vector3(0f, h * 0.80f, c * 0.51f),
                       new Vector3(c * 0.9f, h * 0.10f, 0.02f), Bloc.CartonOmbre).SansCollision();
            // l'etiquette : une pastille de sauce, pour reconnaitre une boite
            // a pizza d'un carton de demenagement
            Bloc.Disque("Etiquette", racine.transform, new Vector3(0f, h * 1.00f, 0f),
                        c * 0.42f, h * 0.06f, Bloc.Sauce).SansCollision();

            return racine;
        }
    }
}
