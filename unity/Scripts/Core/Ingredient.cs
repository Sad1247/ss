using System.Collections.Generic;

namespace BellaNotte.Core
{
    /// <summary>Identifiants stables des ingredients. L'ordre sert d'index UI.</summary>
    public enum IngredientId
    {
        Tomate, Creme,
        Mozzarella, Jambon, Champignon, Olive, Poivron,
        Ananas, Piment, Basilic, Anchois, Oeuf
    }

    public sealed class Ingredient
    {
        public IngredientId Id { get; }
        public string Nom { get; }
        /// <summary>Une base s'applique seule et remplace la precedente.</summary>
        public bool EstBase { get; }

        public Ingredient(IngredientId id, string nom, bool estBase)
        {
            Id = id; Nom = nom; EstBase = estBase;
        }
    }

    public static class Ingredients
    {
        public static readonly IReadOnlyList<Ingredient> Tous = new[]
        {
            new Ingredient(IngredientId.Tomate,     "Tomate",     true),
            new Ingredient(IngredientId.Creme,      "Creme",      true),
            new Ingredient(IngredientId.Mozzarella, "Mozzarella", false),
            new Ingredient(IngredientId.Jambon,     "Jambon",     false),
            new Ingredient(IngredientId.Champignon, "Champignon", false),
            new Ingredient(IngredientId.Olive,      "Olives",     false),
            new Ingredient(IngredientId.Poivron,    "Poivron",    false),
            new Ingredient(IngredientId.Ananas,     "Ananas",     false),
            new Ingredient(IngredientId.Piment,     "Piment",     false),
            new Ingredient(IngredientId.Basilic,    "Basilic",    false),
            new Ingredient(IngredientId.Anchois,    "Anchois",    false),
            new Ingredient(IngredientId.Oeuf,       "Oeuf",       false),
        };

        static readonly Dictionary<IngredientId, Ingredient> ParId = Build();

        static Dictionary<IngredientId, Ingredient> Build()
        {
            var d = new Dictionary<IngredientId, Ingredient>();
            foreach (var i in Tous) d[i.Id] = i;
            return d;
        }

        public static Ingredient Get(IngredientId id) => ParId[id];
        public static string Nom(IngredientId id) => ParId[id].Nom;
        public static bool EstBase(IngredientId id) => ParId[id].EstBase;
    }
}
