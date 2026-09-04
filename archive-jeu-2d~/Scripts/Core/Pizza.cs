using System.Collections.Generic;

namespace BellaNotte.Core
{
    public enum EtatCuisson { Crue, PasAssezCuite, Parfaite, TropCuite, Cramee }

    /// <summary>La pizza en cours de preparation sur le plan de travail.</summary>
    public sealed class Pizza
    {
        readonly List<IngredientId> _garnitures = new List<IngredientId>();

        public IngredientId? Base { get; private set; }
        public IReadOnlyList<IngredientId> Garnitures => _garnitures;
        /// <summary>0 = crue, 1 = cuite, au dela = brulee.</summary>
        public float Cuisson { get; private set; }
        public bool AuFour { get; private set; }
        public bool Sortie { get; private set; }

        public bool EstEnfournable => Base.HasValue && _garnitures.Count > 0 && !AuFour && !Sortie;
        public bool EstModifiable => !AuFour && !Sortie;

        /// <summary>Pose ou retire un ingredient. Renvoie false si le geste est refuse.</summary>
        public bool Basculer(IngredientId id, out string refus)
        {
            refus = null;
            if (!EstModifiable) { refus = "La pizza n'est plus modifiable."; return false; }

            if (Ingredients.EstBase(id))
            {
                Base = (Base == id) ? (IngredientId?)null : id;
                return true;
            }

            if (!Base.HasValue) { refus = "Mets d'abord une base."; return false; }

            if (_garnitures.Remove(id)) return true;

            if (_garnitures.Count >= GameConfig.MaxGarnituresParPizza)
            {
                refus = "Cinq garnitures maximum.";
                return false;
            }
            _garnitures.Add(id);
            return true;
        }

        public bool Enfourner()
        {
            if (!EstEnfournable) return false;
            AuFour = true;
            return true;
        }

        /// <summary>Avance la cuisson. Renvoie true si la pizza sort d'elle-meme (cramee).</summary>
        public bool Cuire(float deltaTemps)
        {
            if (!AuFour) return false;
            Cuisson += deltaTemps / GameConfig.DureeCuisson;
            if (Cuisson >= GameConfig.CuissonMax)
            {
                Cuisson = GameConfig.CuissonMax;
                Sortir();
                return true;
            }
            return false;
        }

        public void Sortir()
        {
            if (!AuFour) return;
            AuFour = false;
            Sortie = true;
        }

        public EtatCuisson Etat
        {
            get
            {
                if (Cuisson < GameConfig.SeuilCrue) return EtatCuisson.Crue;
                if (Cuisson < GameConfig.ZoneParfaiteMin) return EtatCuisson.PasAssezCuite;
                if (Cuisson <= GameConfig.ZoneParfaiteMax) return EtatCuisson.Parfaite;
                if (Cuisson < GameConfig.SeuilCramee) return EtatCuisson.TropCuite;
                return EtatCuisson.Cramee;
            }
        }

        /// <summary>Multiplicateur de paiement du a la seule cuisson.</summary>
        public float ScoreCuisson
        {
            get
            {
                switch (Etat)
                {
                    case EtatCuisson.Parfaite:      return GameConfig.ScoreCuissonParfaite;
                    case EtatCuisson.PasAssezCuite: return GameConfig.ScoreCuissonPale;
                    case EtatCuisson.TropCuite:     return GameConfig.ScoreCuissonTropCuite;
                    default:                        return 0f;
                }
            }
        }
    }
}
