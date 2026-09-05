using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le plan de travail ou l'on met les pizzas en boite, adosse aux fenetres
    /// du fond, derriere la caisse. Meme facture que le comptoir : caisson bleu
    /// et dessus metallique.
    ///
    /// Il porte deux piles de cartons ou le caissier vient piocher. Elles se
    /// regarnissent toutes seules : une table vide bloquerait le service pour
    /// de bon.
    /// </summary>
    public sealed class Emballage : MonoBehaviour
    {
        public Pile Boites;      // pile de gauche
        public Pile Appoint;     // pile de droite
        public Transform Poste;  // ou le caissier se place pour emballer

        float _compteurReappro;

        public int Reserve => Nombre(Boites) + Nombre(Appoint);

        /// <summary>Ou le caissier doit se tenir pour travailler.</summary>
        public Vector3 PointDeTravail
            => Poste != null ? Poste.position : transform.position + new Vector3(0f, 0f, -1.4f);

        /// <summary>
        /// Remplit les deux piles. Appelee apres le cablage : au moment du
        /// Awake, elles ne sont pas encore branchees.
        /// </summary>
        public void Garnir()
        {
            foreach (var pile in new[] { Boites, Appoint })
            {
                if (pile == null) continue;
                pile.Max = Reglages.BoitesParPile;
                while (!pile.EstPleine) pile.Ajouter(true);
            }
        }

        void Awake() => Garnir();

        void Update()
        {
            var aRemplir = Manquante();
            if (aRemplir == null) return;

            _compteurReappro -= Time.deltaTime;
            if (_compteurReappro > 0f) return;
            _compteurReappro = Reglages.DelaiReappro;
            aRemplir.Ajouter(true);
        }

        /// <summary>Prend un carton, sur la plus haute des deux piles.</summary>
        public bool PrendreBoite()
        {
            var pile = Nombre(Boites) >= Nombre(Appoint) ? Boites : Appoint;
            if (pile == null || pile.EstVide) return false;
            pile.Retirer();
            return true;
        }

        Pile Manquante()
        {
            if (Boites != null && !Boites.EstPleine) return Boites;
            if (Appoint != null && !Appoint.EstPleine) return Appoint;
            return null;
        }

        static int Nombre(Pile p) => p != null ? p.Nombre : 0;
    }
}
