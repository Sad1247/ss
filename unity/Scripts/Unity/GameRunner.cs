using BellaNotte.Core;
using UnityEngine;

namespace BellaNotte.Unity
{
    /// <summary>
    /// Pont entre le moteur de jeu (C# pur) et Unity : fait avancer le temps,
    /// route les entrees, relaie les evenements. Aucune regle de jeu ici.
    /// Pose ce composant sur un GameObject vide nomme "Game".
    /// </summary>
    public sealed class GameRunner : MonoBehaviour
    {
        [Tooltip("Graine fixe pour rejouer la meme partie. 0 = aleatoire.")]
        [SerializeField] int graine = 0;

        [Tooltip("Branche ici le composant qui dessine l'interface.")]
        [SerializeField] MonoBehaviour vue; // doit implementer IGameView

        public PizzeriaGame Game { get; private set; }
        IGameView _vue;

        void Awake()
        {
            _vue = vue as IGameView;
            if (vue != null && _vue == null)
                Debug.LogError("Le champ 'vue' doit referencer un composant qui implemente IGameView.");

            Game = graine != 0 ? new PizzeriaGame(graine) : new PizzeriaGame();
            Brancher();
        }

        void Start() => Game.Demarrer();

        void Update()
        {
            Game.Tick(Time.deltaTime);
        }

        void Brancher()
        {
            if (_vue == null) return;
            Game.CommandeArrivee      += _vue.OnCommandesChangees;
            Game.CommandePartie       += o => { _vue.OnClientParti(o); _vue.OnCommandesChangees(o); };
            Game.CommandeSelectionnee += _vue.OnCommandesChangees;
            Game.PizzaChangee         += _vue.OnPizzaChangee;
            Game.PizzaServie          += _vue.OnPizzaServie;
            Game.NiveauMonte          += _vue.OnNiveauMonte;
            Game.Message              += _vue.OnMessage;
            Game.PartieTerminee       += _vue.OnPartieTerminee;
        }

        // --- a brancher sur les boutons de l'UI ---
        public void UiSelectionner(int commandeId) => Game.Selectionner(commandeId);
        public void UiBasculer(int ingredientIndex) => Game.Basculer((IngredientId)ingredientIndex);
        public void UiEnfourner() => Game.Enfourner();
        public void UiSortirDuFour() => Game.SortirDuFour();
        public void UiServir() => Game.Servir();
        public void UiJeter() => Game.Jeter();
    }

    /// <summary>Ce que l'affichage doit savoir faire. Implemente-le sur ton script d'UI.</summary>
    public interface IGameView
    {
        void OnCommandesChangees(Order commande);
        void OnClientParti(Order commande);
        void OnPizzaChangee();
        void OnPizzaServie(ServiceResult resultat);
        void OnNiveauMonte(int niveau);
        void OnMessage(string texte);
        void OnPartieTerminee();
    }
}
