using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le plan de travail ou l'on met les pizzas en boite, adosse aux fenetres
    /// du fond, derriere la caisse. Meme facture que le comptoir : caisson bleu
    /// et dessus metallique.
    ///
    /// Il porte une rangee de cartons ou le caissier vient piocher. Elle se
    /// regarnit toute seule, carton par carton : une table vide bloquerait le
    /// service pour de bon.
    /// </summary>
    public sealed class Emballage : MonoBehaviour
    {
        public Pile Boites;      // la rangee posee sur le plan
        public Transform Poste;  // ou le caissier se place pour emballer

        float _compteurReappro;

        public int Reserve => Boites != null ? Boites.Nombre : 0;

        /// <summary>Ou le caissier doit se tenir pour travailler.</summary>
        public Vector3 PointDeTravail
            => Poste != null ? Poste.position : transform.position + new Vector3(0f, 0f, -1.4f);

        /// <summary>
        /// Remplit la rangee. Appelee apres le cablage : au moment du Awake, la
        /// pile n'est pas encore branchee.
        /// </summary>
        public void Garnir()
        {
            if (Boites == null) return;
            Boites.Sens = Pile.Disposition.Rangee;
            Boites.Max = Reglages.BoitesEnRangee;
            while (!Boites.EstPleine) Boites.Ajouter(true);
        }

        void Awake() => Garnir();

        void Update()
        {
            if (Boites == null || Boites.EstPleine) return;

            _compteurReappro -= Time.deltaTime;
            if (_compteurReappro > 0f) return;
            _compteurReappro = Reglages.DelaiReappro;
            Boites.Ajouter(true);
        }

        /// <summary>Prend un carton au bout de la rangee.</summary>
        public bool PrendreBoite()
        {
            if (Boites == null || Boites.EstVide) return false;
            Boites.Retirer();
            return true;
        }
    }
}
