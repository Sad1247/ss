using System.Collections.Generic;
using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Une pile de pizzas : le joueur en porte une sur la tete, le four et le
    /// comptoir en ont chacun une. Gere le visuel et le compte, rien d'autre.
    /// </summary>
    public sealed class Pile : MonoBehaviour
    {
        readonly List<GameObject> _pizzas = new List<GameObject>();

        public int Max = 12;
        public int Nombre => _pizzas.Count;
        public bool EstPleine => _pizzas.Count >= Max;
        public bool EstVide => _pizzas.Count == 0;

        public bool Ajouter()
        {
            if (EstPleine) return false;
            var p = Pizza(transform, _pizzas.Count);
            _pizzas.Add(p);
            return true;
        }

        public bool Retirer()
        {
            if (EstVide) return false;
            var haut = _pizzas[_pizzas.Count - 1];
            _pizzas.RemoveAt(_pizzas.Count - 1);
            if (haut != null) Destroy(haut);
            return true;
        }

        public void Vider()
        {
            while (!EstVide) Retirer();
        }

        /// <summary>Une pizza : croute, sauce, et quelques rondelles dessus.</summary>
        public static GameObject Pizza(Transform parent, int index)
        {
            var racine = new GameObject("Pizza" + index);
            racine.transform.SetParent(parent, false);
            racine.transform.localPosition = new Vector3(0f, index * Reglages.EpaisseurPizza, 0f);
            // chaque pizza est posee de travers : une pile parfaitement alignee
            // trahit le decor genere
            racine.transform.localRotation = Quaternion.Euler(0f, (index * 37f) % 360f, 0f);

            Bloc.Disque("Croute", racine.transform, Vector3.zero, 0.92f, Reglages.EpaisseurPizza, Bloc.Croute)
                .SansCollision();
            Bloc.Disque("Sauce", racine.transform, new Vector3(0f, Reglages.EpaisseurPizza * 0.55f, 0f),
                        0.76f, Reglages.EpaisseurPizza * 0.4f, Bloc.Sauce).SansCollision();

            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f + index;
                var pos = new Vector3(Mathf.Cos(a) * 0.22f, Reglages.EpaisseurPizza * 0.8f, Mathf.Sin(a) * 0.22f);
                Bloc.Disque("Garniture" + i, racine.transform, pos, 0.16f,
                            Reglages.EpaisseurPizza * 0.35f, Bloc.Couleur(0xC0281F)).SansCollision();
            }
            return racine;
        }
    }
}
