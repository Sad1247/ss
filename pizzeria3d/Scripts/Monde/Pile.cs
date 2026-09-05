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

        /// <summary>Une pizza, batie par Pizza3D.</summary>
        public static GameObject Pizza(Transform parent, int index) => Pizza3D.Creer(parent, index);
    }
}
