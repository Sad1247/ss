using System.Collections.Generic;

namespace BellaNotte.Core
{
    public sealed class Recipe
    {
        public string Nom { get; }
        public IngredientId Base { get; }
        public IReadOnlyList<IngredientId> Garnitures { get; }

        /// <summary>Prix affiche sur le ticket : plus la pizza est chargee, plus elle rapporte.</summary>
        public int Prix => GameConfig.PrixDeBase + Garnitures.Count * GameConfig.PrixParGarniture;

        public Recipe(string nom, IngredientId basePizza, params IngredientId[] garnitures)
        {
            Nom = nom; Base = basePizza; Garnitures = garnitures;
        }
    }

    public static class Recipes
    {
        public static readonly IReadOnlyList<Recipe> Carte = new[]
        {
            new Recipe("Margherita",   IngredientId.Tomate, IngredientId.Mozzarella, IngredientId.Basilic),
            new Recipe("Reine",        IngredientId.Tomate, IngredientId.Mozzarella, IngredientId.Jambon, IngredientId.Champignon),
            new Recipe("Napolitaine",  IngredientId.Tomate, IngredientId.Mozzarella, IngredientId.Anchois, IngredientId.Olive),
            new Recipe("Vegetarienne", IngredientId.Tomate, IngredientId.Mozzarella, IngredientId.Poivron, IngredientId.Champignon, IngredientId.Olive),
            new Recipe("Hawaienne",    IngredientId.Tomate, IngredientId.Mozzarella, IngredientId.Jambon, IngredientId.Ananas),
            new Recipe("Diavola",      IngredientId.Tomate, IngredientId.Mozzarella, IngredientId.Piment, IngredientId.Olive),
            new Recipe("Bianca",       IngredientId.Creme,  IngredientId.Mozzarella, IngredientId.Champignon),
            new Recipe("Savoyarde",    IngredientId.Creme,  IngredientId.Mozzarella, IngredientId.Jambon, IngredientId.Oeuf),
            new Recipe("Forestiere",   IngredientId.Creme,  IngredientId.Mozzarella, IngredientId.Champignon, IngredientId.Basilic),
        };

        public static readonly IReadOnlyList<string> Prenoms = new[]
        {
            "Marco","Elodie","Giulia","Karim","Sofia","Theo","Nina","Paolo",
            "Lucie","Enzo","Amina","Hugo","Clara","Bruno","Ines","Matteo"
        };
    }
}
