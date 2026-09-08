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

        const float MinutesParJour = 24f * 60f;

        /// <summary>Le jour de service, a partir de 1.</summary>
        public int Jour { get; private set; } = 1;

        /// <summary>Minutes ecoulees depuis minuit, ce jour-la.</summary>
        public float MinutesDepuisMinuit { get; private set; }

        public int Heures => Mathf.FloorToInt(MinutesDepuisMinuit / 60f) % 24;
        public int Minutes => Mathf.FloorToInt(MinutesDepuisMinuit % 60f);

        /// <summary>L'heure telle qu'elle s'ecrit a l'ecran.</summary>
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
            MinutesDepuisMinuit = Reglages.HeureOuverture * 60f;
        }

        void Update()
        {
            if (Figee) return;
            Avancer(Reglages.MinutesParSeconde * Time.deltaTime);
        }

        /// <summary>
        /// Fait avancer l'heure. Une boucle et non un simple test pour le
        /// passage de minuit : a vitesse doublee, une image longue peut sauter
        /// la barre des vingt-quatre heures.
        /// </summary>
        public void Avancer(float minutes)
        {
            MinutesDepuisMinuit += minutes;
            while (MinutesDepuisMinuit >= MinutesParJour)
            {
                MinutesDepuisMinuit -= MinutesParJour;
                Jour++;
            }
        }
    }
}
