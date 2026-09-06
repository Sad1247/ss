using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le plan de travail ou l'on met les pizzas en boite, adosse aux fenetres
    /// du fond, derriere la caisse. Meme facture que le comptoir : caisson bleu
    /// et dessus metallique.
    ///
    /// Il porte un tas de cartons, dans un coin du plan, ou le caissier vient
    /// piocher. Il se regarnit tout seul, carton par carton : une table vide
    /// bloquerait le service pour de bon.
    /// </summary>
    public sealed class Emballage : MonoBehaviour
    {
        public Pile Boites;        // la reserve, dans le coin du plan
        public Pile Preparation;   // le rond rouge : le carton qu'on vient de sortir
        public Pile Assemblage;    // le rond vert : les boites qu'on garnit
        public Transform Poste;    // ou le caissier se place pour travailler

        float _compteurReappro;

        public int Reserve => Boites != null ? Boites.Nombre : 0;

        /// <summary>Ou le caissier doit se tenir pour travailler.</summary>
        public Vector3 PointDeTravail
            => Poste != null ? Poste.position : transform.position + new Vector3(0f, 0f, -1.4f);

        /// <summary>
        /// Remplit le tas. Appelee apres le cablage : au moment du Awake, la
        /// pile n'est pas encore branchee.
        /// </summary>
        public void Garnir()
        {
            if (Preparation != null) Preparation.Max = 1;
            if (Assemblage != null) Assemblage.Max = Reglages.CapacitePorteeCaissier + 1;
            if (Boites == null) return;
            Boites.Max = Reglages.BoitesEnReserve;
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

        /// <summary>Prend le carton du dessus de la reserve.</summary>
        public bool PrendreBoite()
        {
            if (Boites == null || Boites.EstVide) return false;
            Boites.Retirer();
            return true;
        }

        /// <summary>Repose un carton inutilise : il ne se perd pas.</summary>
        public void Rendre()
        {
            if (Boites != null && !Boites.EstPleine) Boites.Ajouter(true);
        }
    }
}
