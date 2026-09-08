using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// L'avance rapide : une touche, ou le bouton du Hud, et toute la
    /// pizzeria tourne quatre fois plus vite — l'horloge comme le four, les
    /// clients comme le caissier. Accelerer la seule horloge ferait defiler
    /// la journee sans que rien ne se passe dedans.
    /// </summary>
    public sealed class Acceleration : MonoBehaviour
    {
        public static Acceleration Active { get; private set; }

        /// <summary>Le facteur applique quand l'avance rapide est enclenchee.</summary>
        public float Facteur = 4f;
        /// <summary>La touche qui bascule, au clavier.</summary>
        public KeyCode Touche = KeyCode.Space;

        public bool Rapide { get; private set; }

        /// <summary>Le facteur en cours, tel qu'il s'affiche : 1 ou 4.</summary>
        public float FacteurCourant => Rapide ? Facteur : 1f;

        void Awake()
        {
            Active = this;
            Appliquer();
        }

        void Update()
        {
            if (Input.GetKeyDown(Touche)) Basculer();
        }

        /// <summary>Passe de la vitesse normale a l'avance rapide, et retour.</summary>
        public void Basculer()
        {
            Rapide = !Rapide;
            Appliquer();
        }

        void Appliquer() => Time.timeScale = FacteurCourant;

        // Le temps reprend son cours normal si l'objet disparait : une echelle
        // laissee a quatre survivrait au chargement d'une autre scene.
        void OnDisable() => Time.timeScale = 1f;
    }
}
