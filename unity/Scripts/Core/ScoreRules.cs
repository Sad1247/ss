using System.Collections.Generic;
using System.Linq;

namespace BellaNotte.Core
{
    /// <summary>Verdict d'un service : ce que le client a pense, et ce qu'il a paye.</summary>
    public sealed class ServiceResult
    {
        public Order Commande;
        public bool BonneBase;
        public IReadOnlyList<IngredientId> Manquants = new List<IngredientId>();
        public IReadOnlyList<IngredientId> EnTrop = new List<IngredientId>();
        public EtatCuisson Cuisson;
        /// <summary>0 = refusee, 1 = parfaite.</summary>
        public float Ratio;
        public int Paiement;
        public int Pourboire;

        public bool EstParfaite => Ratio >= 1f;
        public bool EstRefusee => Paiement <= 0;
        public int Total => Paiement + Pourboire;
    }

    public static class ScoreRules
    {
        public static ServiceResult Evaluer(Order commande, Pizza pizza)
        {
            var voulu = new HashSet<IngredientId>(commande.Recette.Garnitures);
            var pose = new HashSet<IngredientId>(pizza.Garnitures);

            var r = new ServiceResult
            {
                Commande = commande,
                BonneBase = pizza.Base.HasValue && pizza.Base.Value == commande.Recette.Base,
                Manquants = voulu.Where(t => !pose.Contains(t)).ToList(),
                EnTrop = pose.Where(t => !voulu.Contains(t)).ToList(),
                Cuisson = pizza.Etat,
            };

            float ratio = 1f;
            ratio -= r.Manquants.Count * GameConfig.PenaliteManquant;
            ratio -= r.EnTrop.Count * GameConfig.PenaliteEnTrop;
            if (!r.BonneBase) ratio -= GameConfig.PenaliteMauvaiseBase;
            ratio *= pizza.ScoreCuisson;
            if (ratio < 0f) ratio = 0f;

            r.Ratio = ratio;
            r.Paiement = (int)System.Math.Round(commande.Recette.Prix * ratio);
            r.Pourboire = r.EstParfaite
                ? (int)System.Math.Round(commande.Recette.Prix * GameConfig.RatioPourboire)
                : 0;
            return r;
        }

        /// <summary>Phrase de compte rendu pour le journal de salle.</summary>
        public static string Resume(ServiceResult r)
        {
            var nom = r.Commande.Recette.Nom;
            var qui = r.Commande.Client;

            if (r.EstParfaite)
                return $"{qui} — {nom} : parfaite ! +{r.Total} € (dont {r.Pourboire} € de pourboire)";

            if (r.EstRefusee)
                return $"{qui} — {nom} : refusee, aucun paiement.";

            var reproches = new List<string>();
            if (!r.BonneBase) reproches.Add("mauvaise base");
            if (r.Manquants.Count > 0)
                reproches.Add("manque " + string.Join(", ", r.Manquants.Select(t => Ingredients.Nom(t).ToLower())));
            if (r.EnTrop.Count > 0)
                reproches.Add("en trop : " + string.Join(", ", r.EnTrop.Select(t => Ingredients.Nom(t).ToLower())));
            if (r.Cuisson == EtatCuisson.PasAssezCuite) reproches.Add("pas assez cuite");
            if (r.Cuisson == EtatCuisson.TropCuite) reproches.Add("trop cuite");

            return $"{qui} — {nom} : +{r.Paiement} € ({string.Join(" ; ", reproches)})";
        }
    }
}
