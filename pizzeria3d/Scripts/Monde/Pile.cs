using System.Collections.Generic;
using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Une pile de pizzas : le joueur en porte une dans les mains, le four et
    /// le comptoir en ont chacun une. Chaque element y est nu, en boite pour
    /// l'emporter, ou servi sur un plateau pour manger sur place.
    /// </summary>
    public sealed class Pile : MonoBehaviour
    {
        /// <summary>Comment se presente une pizza : nue, en boite, sur plateau.</summary>
        public enum Forme { Nue, Boite, Plateau, PlateauVide }

        struct Element
        {
            public GameObject Objet;
            public Forme Forme;
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
                foreach (var e in _elements) if (e.Forme != Forme.Boite) return false;
                return true;
            }
        }

        /// <summary>Le prochain a partir, celui du dessus : en boite ou nu.</summary>
        public bool SommetEmballe =>
            _elements.Count > 0 && _elements[_elements.Count - 1].Forme == Forme.Boite;

        /// <summary>Sous quelle forme se presente le prochain a partir.</summary>
        public Forme SommetForme =>
            _elements.Count > 0 ? _elements[_elements.Count - 1].Forme : Forme.Nue;

        public bool Ajouter() => Ajouter(Forme.Nue);
        public bool Ajouter(bool emballe) => Ajouter(emballe ? Forme.Boite : Forme.Nue);

        public bool Ajouter(Forme forme)
        {
            if (EstPleine) return false;
            _elements.Add(new Element { Objet = Creer(_elements.Count, forme), Forme = forme });
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
        /// Remplace le sommet par la meme chose sous une autre forme : un
        /// plateau vide qui recoit sa pizza, par exemple.
        /// </summary>
        public bool ChangerSommet(Forme forme)
        {
            if (EstVide) return false;
            int i = _elements.Count - 1;
            if (_elements[i].Objet != null) Destroy(_elements[i].Objet);
            _elements[i] = new Element { Objet = Creer(i, forme), Forme = forme };
            Reposer();
            return true;
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
                if (_elements[i].Forme == Forme.Boite) continue;
                if (_elements[i].Objet != null) Destroy(_elements[i].Objet);
                _elements[i] = new Element { Objet = Creer(i, Forme.Boite), Forme = Forme.Boite };
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
                hauteur += e.Forme == Forme.Boite ? Reglages.EpaisseurBoite
                         : e.Forme == Forme.Nue ? Reglages.EpaisseurPizza
                         : Reglages.EpaisseurPlateau;
            }
        }

        /// <summary>Une pizza nue, une boite fermee ou un plateau servi.</summary>
        GameObject Creer(int index, Forme forme)
        {
            if (forme == Forme.Boite) return Boite3D.Creer(transform, index);
            // porte : dans la longueur, face a qui le recoit
            if (forme == Forme.Plateau) return Plateau3D.Creer(transform, index, true, true);
            // propre et vide : la pizza n'apparait qu'une fois posee dessus
            if (forme == Forme.PlateauVide) return Plateau3D.Creer(transform, index, false, true);
            return Pizza3D.Creer(transform, index);
        }

        /// <summary>Une pizza, batie par Pizza3D.</summary>
        public static GameObject Pizza(Transform parent, int index) => Pizza3D.Creer(parent, index);
    }
}
