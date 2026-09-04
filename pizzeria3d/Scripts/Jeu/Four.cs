using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Produit une pizza toutes les N secondes dans sa pile de sortie, et la
    /// cede au joueur qui vient se coller a elle.
    /// </summary>
    public sealed class Four : MonoBehaviour
    {
        public Pile Sortie;
        public Joueur Joueur;

        float _compteur;

        void Awake()
        {
            if (Sortie != null) Sortie.Max = Reglages.StockFourMax;
        }

        void Update()
        {
            Produire();
            Servir();
        }

        void Produire()
        {
            if (Sortie == null || Sortie.EstPleine) return;
            _compteur += Time.deltaTime;
            if (_compteur < Reglages.DureeCuisson) return;
            _compteur = 0f;
            Sortie.Ajouter();
        }

        void Servir()
        {
            if (Joueur == null || Sortie == null || Sortie.EstVide) return;
            if (Joueur.Portee.EstPleine || !Joueur.PretPourTransfert) return;
            if (!Joueur.EstPres(Sortie.transform.position, Reglages.RayonRamassage)) return;

            Sortie.Retirer();
            Joueur.Portee.Ajouter();
            Joueur.ArmerTransfert();
        }
    }
}
