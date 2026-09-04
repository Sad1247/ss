using System;
using System.Collections.Generic;
using System.Linq;

namespace BellaNotte.Core
{
    /// <summary>
    /// Le moteur de jeu, sans aucune dependance a Unity : on l'alimente avec
    /// Tick(deltaTemps) et des appels d'intention, il emet des evenements.
    /// Toute l'UI se branche sur les evenements, jamais l'inverse.
    /// </summary>
    public sealed class PizzeriaGame
    {
        readonly List<Order> _commandes = new List<Order>();
        readonly Random _rng;
        int _prochainId = 1;
        float _delaiProchainClient;
        int _serviesDansLeNiveau;

        public IReadOnlyList<Order> Commandes => _commandes;
        public Order CommandeActive { get; private set; }
        public Pizza PizzaEnCours { get; private set; }

        public int Recette { get; private set; }
        public int Niveau { get; private set; } = 1;
        public int PizzasServies { get; private set; }
        public int ClientsPerdus { get; private set; }
        public bool EnCours { get; private set; }

        // --- evenements pour l'UI ---
        public event Action<Order> CommandeArrivee;
        public event Action<Order> CommandePartie;      // client parti sans etre servi
        public event Action<Order> CommandeSelectionnee;
        public event Action PizzaChangee;               // composition ou cuisson modifiee
        public event Action<ServiceResult> PizzaServie;
        public event Action<int> NiveauMonte;
        public event Action<string> Message;            // retour immediat au joueur
        public event Action PartieTerminee;

        public PizzeriaGame(int? graine = null)
        {
            _rng = graine.HasValue ? new Random(graine.Value) : new Random();
        }

        public void Demarrer()
        {
            _commandes.Clear();
            CommandeActive = null; PizzaEnCours = null;
            Recette = 0; Niveau = 1; PizzasServies = 0; ClientsPerdus = 0;
            _prochainId = 1; _serviesDansLeNiveau = 0;
            EnCours = true;

            FaireEntrerUnClient();
            _delaiProchainClient = GameConfig.DelaiEntreClients(Niveau);
        }

        public void Tick(float dt)
        {
            if (!EnCours) return;

            // patience des clients
            for (int i = _commandes.Count - 1; i >= 0; i--)
            {
                var o = _commandes[i];
                o.Attendre(dt);
                if (o.EstPerdue)
                {
                    ClientsPerdus++;
                    CommandePartie?.Invoke(o);
                    Retirer(o);
                }
            }

            // arrivees
            _delaiProchainClient -= dt;
            if (_delaiProchainClient <= 0f)
            {
                FaireEntrerUnClient();
                _delaiProchainClient = GameConfig.DelaiEntreClients(Niveau);
            }

            // four
            if (PizzaEnCours != null && PizzaEnCours.AuFour)
            {
                bool cramee = PizzaEnCours.Cuire(dt);
                PizzaChangee?.Invoke();
                if (cramee) Message?.Invoke("Carbonisee ! Il faut la jeter.");
            }

            if (ClientsPerdus >= GameConfig.ClientsPerdusMax) Terminer();
        }

        // --- intentions du joueur ---

        public void Selectionner(int commandeId)
        {
            if (!EnCours) return;
            if (PizzaEnCours != null && PizzaEnCours.AuFour)
            {
                Message?.Invoke("La pizza est au four, ne quitte pas le poste.");
                return;
            }
            var o = _commandes.FirstOrDefault(c => c.Id == commandeId);
            if (o == null) return;

            CommandeActive = o;
            PizzaEnCours = new Pizza();
            CommandeSelectionnee?.Invoke(o);
            PizzaChangee?.Invoke();
        }

        public void Basculer(IngredientId id)
        {
            if (!EnCours || PizzaEnCours == null) return;
            if (PizzaEnCours.Basculer(id, out string refus)) PizzaChangee?.Invoke();
            else if (refus != null) Message?.Invoke(refus);
        }

        public void Enfourner()
        {
            if (!EnCours || PizzaEnCours == null) return;
            if (PizzaEnCours.Enfourner())
            {
                Message?.Invoke("Au four ! Sors-la dans la zone marquee.");
                PizzaChangee?.Invoke();
            }
            else Message?.Invoke("Il faut une base et au moins une garniture.");
        }

        public void SortirDuFour()
        {
            if (!EnCours || PizzaEnCours == null || !PizzaEnCours.AuFour) return;
            PizzaEnCours.Sortir();
            PizzaChangee?.Invoke();
            switch (PizzaEnCours.Etat)
            {
                case EtatCuisson.Parfaite:      Message?.Invoke("Cuisson parfaite ! Sers vite."); break;
                case EtatCuisson.PasAssezCuite: Message?.Invoke("Un peu palotte, mais mangeable."); break;
                case EtatCuisson.TropCuite:     Message?.Invoke("Trop cuite sur les bords."); break;
                case EtatCuisson.Cramee:        Message?.Invoke("Cramee."); break;
                default:                        Message?.Invoke("Pate crue, le client va tiquer."); break;
            }
        }

        public void Servir()
        {
            if (!EnCours || CommandeActive == null || PizzaEnCours == null) return;
            if (!PizzaEnCours.Sortie) { Message?.Invoke("Fais-la cuire d'abord."); return; }

            var r = ScoreRules.Evaluer(CommandeActive, PizzaEnCours);
            Recette += r.Total;
            PizzasServies++;
            _serviesDansLeNiveau++;
            if (r.EstRefusee) ClientsPerdus++;

            PizzaServie?.Invoke(r);
            Retirer(CommandeActive);
            MonterEnNiveauSiBesoin();
            if (ClientsPerdus >= GameConfig.ClientsPerdusMax) Terminer();
        }

        public void Jeter()
        {
            if (!EnCours || PizzaEnCours == null) return;
            PizzaEnCours = new Pizza();
            Message?.Invoke("Pizza jetee. On recommence.");
            PizzaChangee?.Invoke();
        }

        // --- interne ---

        void FaireEntrerUnClient()
        {
            if (_commandes.Count >= GameConfig.MaxCommandesEnAttente) return;

            var recette = Recipes.Carte[_rng.Next(Recipes.Carte.Count)];
            var client = Recipes.Prenoms[_rng.Next(Recipes.Prenoms.Count)];
            var o = new Order(_prochainId++, client, recette, GameConfig.PatienceMax(Niveau));
            _commandes.Add(o);
            CommandeArrivee?.Invoke(o);

            if (CommandeActive == null) Selectionner(o.Id);
        }

        void Retirer(Order o)
        {
            _commandes.Remove(o);
            if (CommandeActive == o)
            {
                CommandeActive = null;
                PizzaEnCours = null;
                if (_commandes.Count > 0) Selectionner(_commandes[0].Id);
                else PizzaChangee?.Invoke();
            }
        }

        void MonterEnNiveauSiBesoin()
        {
            if (_serviesDansLeNiveau < GameConfig.PizzasPourNiveauSuivant(Niveau)) return;
            _serviesDansLeNiveau = 0;
            Niveau++;
            NiveauMonte?.Invoke(Niveau);
        }

        void Terminer()
        {
            if (!EnCours) return;
            EnCours = false;
            PartieTerminee?.Invoke();
        }
    }
}
