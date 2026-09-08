using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le panneau OUVERT / FERME pose sur le comptoir. Vert pendant le
    /// service, rouge des la fermeture : sans lui, rien dans le decor ne dit
    /// pourquoi les clients cessent d'arriver.
    /// </summary>
    public sealed class Enseigne : MonoBehaviour
    {
        public Renderer Panneau;
        public Color Ouvert = Bloc.Couleur(0x36B04A);
        public Color Ferme = Bloc.Couleur(0xC0392B);

        /// <summary>Vrai quand le panneau affiche l'ouverture.</summary>
        public bool MontreOuvert { get; private set; } = true;

        void Awake() => Peindre(true);

        void Update()
        {
            bool ouverte = Horloge.Active == null || Horloge.Active.Ouverte;
            if (ouverte != MontreOuvert) Peindre(ouverte);
        }

        void Peindre(bool ouverte)
        {
            MontreOuvert = ouverte;
            if (Panneau == null) return;
            // Deux peintures partagees, une par etat : reteindre le materiau
            // en place aurait repeint tout ce qui est de la meme couleur.
            Panneau.sharedMaterial = Bloc.Peinture(ouverte ? Ouvert : Ferme);
        }
    }
}
