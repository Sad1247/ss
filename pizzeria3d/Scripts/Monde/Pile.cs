using System.Collections.Generic;
using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Une pile de pizzas : le joueur en porte une dans les mains, le four et
    /// le comptoir en ont chacun une. Chaque element est nu ou emballe dans une
    /// boite — c'est ce que le caissier change en passant par la table.
    /// </summary>
    public sealed class Pile : MonoBehaviour
    {
        struct Element
        {
            public GameObject Objet;
            public bool Emballe;
        }

        readonly List<Element> _elements = new List<Element>();

        public int Max = 12;
        public int Nombre => _elements.Count;
        public bool EstPleine => _elements.Count >= Max;
        public bool EstVide => _elements.Count == 0;

        /// <summary>Vrai si tout ce que contient la pile est en boite.</summary>
        public bool ToutEmballe
        {
            get
            {
                foreach (var e in _elements) if (!e.Emballe) return false;
                return true;
            }
        }

        /// <summary>Le prochain a partir, celui du dessus : en boite ou nu.</summary>
        public bool SommetEmballe => _elements.Count > 0 && _elements[_elements.Count - 1].Emballe;

        public bool Ajouter() => Ajouter(false);

        public bool Ajouter(bool emballe)
        {
            if (EstPleine) return false;
            _elements.Add(new Element { Objet = Creer(_elements.Count, emballe), Emballe = emballe });
            Reposer();
            return true;
        }

        public bool Retirer()
        {
            if (EstVide) return false;
            var haut = _elements[_elements.Count - 1];
            _elements.RemoveAt(_elements.Count - 1);
            if (haut.Objet != null) Destroy(haut.Objet);
            return true;
        }

        public void Vider()
        {
            while (!EstVide) Retirer();
        }

        /// <summary>
        /// Met une pizza en boite, la plus basse d'abord, et renvoie faux quand
        /// il n'y a plus rien a emballer. Une par appel : le caissier emballe au
        /// rythme du geste, pas d'un coup.
        /// </summary>
        public bool EmballerUne()
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                if (_elements[i].Emballe) continue;
                if (_elements[i].Objet != null) Destroy(_elements[i].Objet);
                _elements[i] = new Element { Objet = Creer(i, true), Emballe = true };
                Reposer();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Rempile tout apres chaque changement : une boite est plus haute
        /// qu'une pizza nue, donc chaque mise en boite decale ce qui est
        /// au-dessus.
        /// </summary>
        void Reposer()
        {
            float hauteur = 0f;
            foreach (var e in _elements)
            {
                if (e.Objet != null)
                {
                    var t = e.Objet.transform;
                    t.localPosition = new Vector3(0f, hauteur, t.localPosition.z);
                }
                hauteur += e.Emballe ? Reglages.EpaisseurBoite : Reglages.EpaisseurPizza;
            }
        }

        /// <summary>Une pizza nue ou une boite fermee, selon l'element.</summary>
        GameObject Creer(int index, bool emballe)
            => emballe ? Boite3D.Creer(transform, index) : Pizza3D.Creer(transform, index);

        /// <summary>Une pizza, batie par Pizza3D.</summary>
        public static GameObject Pizza(Transform parent, int index) => Pizza3D.Creer(parent, index);
    }
}
