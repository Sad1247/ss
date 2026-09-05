using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La dalle verte : le joueur s'y arrete, l'argent s'ecoule et l'objet
    /// achete apparait. Rien ne se debloque en appuyant sur un bouton — c'est
    /// le fait de rester dessus qui paie.
    /// </summary>
    public sealed class ZoneAchat : MonoBehaviour
    {
        public Joueur Joueur;
        public GameObject Achat;          // objet a reveler une fois paye
        public int Prix = Reglages.PrixCaissier;
        public string Libelle = "Nouveau four";

        float _verse;
        float _reste;

        public int Restant => Mathf.Max(0, Prix - Mathf.FloorToInt(_verse));

        void Update()
        {
            if (Joueur == null) return;
            bool dessus = Joueur.EstPres(transform.position, 1.2f);
            if (dessus) Hud.MontrerIndice(this, $"{Libelle} — reste {Restant} EUR");
            else { Hud.EffacerIndice(this); return; }

            // on retire par euros entiers, en gardant la fraction pour la suite
            _reste += Reglages.DebitAchat * Time.deltaTime;
            int voulu = Mathf.FloorToInt(_reste);
            if (voulu <= 0) return;
            _reste -= voulu;

            int pris = Banque.Retirer(Mathf.Min(voulu, Restant));
            if (pris <= 0) return;
            _verse += pris;

            if (Restant <= 0) Livrer();
        }

        void Livrer()
        {
            if (Achat != null) Achat.SetActive(true);
            Hud.EffacerIndice(this);
            Hud.Annonce(Libelle + " installe !");
            Destroy(gameObject);
        }
    }
}
