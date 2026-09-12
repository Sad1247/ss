using System.Collections.Generic;
using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La comptabilite du restaurant : ce que rapportent les ventes, ce que
    /// coute le personnel et les ameliorations, et l'historique jour par
    /// jour. La Banque ne tient que le solde ; c'est ici qu'on garde la trace
    /// de ce qui l'a fait bouger, pour que l'ecran du bureau affiche des
    /// chiffres reels et non decoratifs.
    /// </summary>
    public static class Comptabilite
    {
        /// <summary>Un jour clos, tel qu'il reste dans l'historique.</summary>
        public sealed class JourClos
        {
            public int Numero, Revenu, Depense;
            public int Benefice => Revenu - Depense;
        }

        /// <summary>Le prix demande pour une pizza, modifiable depuis le menu.</summary>
        public static int PrixPizza { get; private set; }
        /// <summary>La cuisson courante, raccourcie par les ameliorations du four.</summary>
        public static float DureeCuissonActuelle { get; private set; }
        public static int NiveauFour { get; private set; }
        public const int NiveauFourMax = 3;

        public static int RevenuJour { get; private set; }
        public static int DepenseJour { get; private set; }
        public static int RevenuTotal { get; private set; }
        public static int DepenseTotal { get; private set; }
        public static int PizzasVendues { get; private set; }
        public static int ClientsSatisfaits { get; private set; }
        public static int ClientsInsatisfaits { get; private set; }
        static float _attenteTotale;

        /// <summary>Le benefice du jour en cours, avant sa cloture.</summary>
        public static int BeneficeJour => RevenuJour - DepenseJour;
        public static int BeneficeTotal => RevenuTotal - DepenseTotal;

        /// <summary>Combien de temps un client sert, en moyenne, avant d'etre servi.</summary>
        public static float TempsAttenteMoyen =>
            ClientsSatisfaits > 0 ? _attenteTotale / ClientsSatisfaits : 0f;

        /// <summary>Part des clients repartis satisfaits, sur cent.</summary>
        public static int SatisfactionPourcent
        {
            get
            {
                int total = ClientsSatisfaits + ClientsInsatisfaits;
                return total > 0 ? (ClientsSatisfaits * 100 + total / 2) / total : 100;
            }
        }

        static readonly List<JourClos> _historique = new List<JourClos>();
        /// <summary>Les jours clos, le plus recent en tete.</summary>
        public static IReadOnlyList<JourClos> Historique => _historique;

        public static void Reinitialiser()
        {
            PrixPizza = Reglages.PrixPizza;
            DureeCuissonActuelle = Reglages.DureeCuisson;
            NiveauFour = 0;
            RevenuJour = DepenseJour = RevenuTotal = DepenseTotal = 0;
            PizzasVendues = ClientsSatisfaits = ClientsInsatisfaits = 0;
            _attenteTotale = 0f;
            _historique.Clear();
        }

        /// <summary>Une addition payee : autant de pizzas parties d'un coup.</summary>
        public static void EnregistrerVente(int pizzas, int montant)
        {
            RevenuJour += montant;
            RevenuTotal += montant;
            PizzasVendues += pizzas;
        }

        public static void EnregistrerDepense(int montant)
        {
            if (montant <= 0) return;
            DepenseJour += montant;
            DepenseTotal += montant;
        }

        /// <summary>
        /// Un client s'en va : servi, avec le temps qu'il aura attendu, ou
        /// reparti lasse. C'est de la qu'on tire la satisfaction affichee.
        /// </summary>
        public static void EnregistrerDepart(bool servi, float tempsAttente)
        {
            if (servi) { ClientsSatisfaits++; _attenteTotale += tempsAttente; }
            else ClientsInsatisfaits++;
        }

        /// <summary>
        /// A minuit : le personnel est reellement paye — une sortie de caisse,
        /// pas une ligne d'affichage — puis le jour clos rejoint l'historique.
        /// </summary>
        public static void ClorreLaJournee(int jourEcoule)
        {
            EnregistrerDepense(Banque.Retirer(Personnel.MasseSalariale));
            _historique.Insert(0, new JourClos
            { Numero = jourEcoule, Revenu = RevenuJour, Depense = DepenseJour });
            RevenuJour = 0;
            DepenseJour = 0;
        }

        /// <summary>Ce que coute la prochaine amelioration du four.</summary>
        public static int PrixAmeliorationFour => 300 + NiveauFour * 250;

        /// <summary>
        /// Un four plus rapide, reellement : chaque niveau raccourcit la
        /// cuisson de 15%, jusqu'a trois fois. L'argent part pour de bon.
        /// </summary>
        public static bool AmeliorerFour()
        {
            if (NiveauFour >= NiveauFourMax) return false;
            int prix = PrixAmeliorationFour;
            if (Banque.Solde < prix) return false;
            Banque.Retirer(prix);
            EnregistrerDepense(prix);
            NiveauFour++;
            float facteur = 1f;
            for (int i = 0; i < NiveauFour; i++) facteur *= 0.85f;
            DureeCuissonActuelle = Reglages.DureeCuisson * facteur;
            return true;
        }

        /// <summary>Change ce que paie le client, entre 1 et 20 EUR la pizza.</summary>
        public static void DefinirPrixPizza(int prix)
            => PrixPizza = prix < 1 ? 1 : prix > 20 ? 20 : prix;
    }
}
