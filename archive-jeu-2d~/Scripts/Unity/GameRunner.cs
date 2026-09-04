using BellaNotte.Core;
using UnityEngine;

namespace BellaNotte.Unity
{
    /// <summary>
    /// Pont entre le moteur de jeu (C# pur) et Unity : fait avancer le temps,
    /// route les entrees, relaie les evenements. Aucune regle de jeu ici.
    /// </summary>
    public sealed class GameRunner : MonoBehaviour
    {
        [Tooltip("Graine fixe pour rejouer exactement la meme partie. 0 = aleatoire.")]
        [SerializeField] int graine = 0;

        [Tooltip("Optionnel : un composant qui implemente IGameView. Sinon la vue se branche seule.")]
        [SerializeField] MonoBehaviour vue;

        PizzeriaGame _game;
        IGameView _branchee;
        bool _demarree;

        /// <summary>Cree le moteur au premier acces : l'ordre des Awake n'a plus d'importance.</summary>
        public PizzeriaGame Game
        {
            get
            {
                if (_game == null) _game = graine != 0 ? new PizzeriaGame(graine) : new PizzeriaGame();
                return _game;
            }
        }

        void Awake()
        {
            if (vue is IGameView v) Brancher(v);
            else if (vue != null) Debug.LogError("Le champ 'vue' doit implementer IGameView.");
        }

        void Start()
        {
            if (_demarree) return;
            _demarree = true;
            Game.Demarrer();
        }

        void Update() => Game.Tick(Time.deltaTime);

        /// <summary>Abonne un affichage aux evenements du moteur. Un seul a la fois.</summary>
        public void Brancher(IGameView v)
        {
            if (v == null || _branchee != null) return;
            _branchee = v;

            Game.CommandeArrivee      += v.OnCommandesChangees;
            Game.CommandeSelectionnee += v.OnCommandesChangees;
            Game.CommandePartie       += o => { v.OnClientParti(o); v.OnCommandesChangees(o); };
            Game.PizzaChangee         += v.OnPizzaChangee;
            Game.PizzaServie          += v.OnPizzaServie;
            Game.NiveauMonte          += v.OnNiveauMonte;
            Game.Message              += v.OnMessage;
            Game.PartieTerminee       += v.OnPartieTerminee;
        }

        /// <summary>Rejoue une partie depuis zero avec le meme affichage.</summary>
        public void Rejouer() => Game.Demarrer();

        // --- a brancher sur les boutons ---
        public void UiSelectionner(int commandeId) => Game.Selectionner(commandeId);
        public void UiBasculer(int ingredientIndex) => Game.Basculer((IngredientId)ingredientIndex);
        public void UiEnfourner() => Game.Enfourner();
        public void UiSortirDuFour() => Game.SortirDuFour();
        public void UiServir() => Game.Servir();
        public void UiJeter() => Game.Jeter();
    }

    /// <summary>Ce que l'affichage doit savoir faire.</summary>
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
