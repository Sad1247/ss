using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// L'heure de la pizzeria. Elle ne suit pas la montre du joueur : une
    /// minute de jeu passe deux fois plus vite qu'une minute reelle, sinon une
    /// journee de service durerait une journee.
    /// </summary>
    public sealed class Horloge : MonoBehaviour
    {
        public static Horloge Active { get; private set; }

        const float SecondesParJour = 24f * 60f * 60f;

        /// <summary>Le jour de service, a partir de 1.</summary>
        public int Jour { get; private set; } = 1;

        /// <summary>Secondes ecoulees depuis minuit, ce jour-la.</summary>
        public float SecondesDepuisMinuit { get; private set; }

        /// <summary>Les memes, comptees en minutes.</summary>
        public float MinutesDepuisMinuit => SecondesDepuisMinuit / 60f;

        public int Heures => Mathf.FloorToInt(SecondesDepuisMinuit / 3600f) % 24;
        public int Minutes => Mathf.FloorToInt(SecondesDepuisMinuit / 60f) % 60;
        /// <summary>
        /// L'heure telle qu'elle s'ecrit a l'ecran. Sans les secondes : a ce
        /// rythme elles defileraient trop vite pour se lire, et la minute
        /// change deja plus de deux fois par seconde.
        /// </summary>
        public string Heure => Heures.ToString("00") + ":" + Minutes.ToString("00");
        public string Affichage => "Jour " + Jour + "   " + Heure;

        /// <summary>Vrai pendant le service : la pizzeria accueille du monde.</summary>
        public bool Ouverte => Heures >= Reglages.HeureOuverture
                            && Heures < Reglages.HeureFermeture;

        /// <summary>
        /// Arrete le temps. Sert au banc d'essai, qui doit pouvoir eprouver le
        /// service sans que la journee defile sous lui — et servira a la pause.
        /// </summary>
        public bool Figee;

        void Awake()
        {
            Active = this;
            SecondesDepuisMinuit = Reglages.HeureOuverture * 3600f;
        }

        void Update()
        {
            if (Figee) return;
            AvancerSecondes(Reglages.SecondesParSeconde * Time.deltaTime);
        }

        /// <summary>
        /// Fait avancer l'heure. Une boucle et non un simple test pour le
        /// passage de minuit : a vitesse doublee, une image longue peut sauter
        /// la barre des vingt-quatre heures.
        /// </summary>
        public void Avancer(float minutes) => AvancerSecondes(minutes * 60f);

        public void AvancerSecondes(float secondes)
        {
            SecondesDepuisMinuit += secondes;
            while (SecondesDepuisMinuit >= SecondesParJour)
            {
                SecondesDepuisMinuit -= SecondesParJour;
                Jour++;
            }
        }
    }
}
