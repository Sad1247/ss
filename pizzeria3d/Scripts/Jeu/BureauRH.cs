using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le bureau des ressources humaines : le joueur s'en approche et
    /// l'ordinateur s'ouvre directement sur l'onglet Personnel, sans passer
    /// par le fauteuil ni le portable. Reutilise l'ecran existant plutot que
    /// d'en batir un second.
    /// </summary>
    public sealed class BureauRH : MonoBehaviour
    {
        public Joueur Joueur;
        public EcranBureau Ecran;
        public float Rayon = 1.5f;

        void Update()
        {
            if (Joueur == null || Ecran == null) return;
            bool pres = Joueur.EstPres(transform.position, Rayon);
            if (pres) Ecran.OuvrirDepuisRH();
            else Ecran.FermerDepuisRH();
        }
    }
}
