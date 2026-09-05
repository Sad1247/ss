using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La table de mise en boite, posee a cote du four. Elle tient une reserve
    /// de cartons ou le caissier vient piocher : tant qu'il n'est pas passe par
    /// la, rien ne part au comptoir.
    ///
    /// La reserve se regarnit toute seule, une boite a la fois. Sans cela, une
    /// table vide bloquerait le service pour de bon.
    /// </summary>
    public sealed class Emballage : MonoBehaviour
    {
        public Pile Boites;
        public Transform Poste;          // ou le caissier se place pour emballer

        float _compteurReappro;

        public int Reserve => Boites != null ? Boites.Nombre : 0;

        /// <summary>Ou le caissier doit se tenir pour travailler.</summary>
        public Vector3 PointDeTravail
            => Poste != null ? Poste.position : transform.position + new Vector3(0f, 0f, -1.4f);

        /// <summary>
        /// Remplit la reserve. Appelee apres le cablage : au moment du Awake,
        /// la pile n'est pas encore branchee.
        /// </summary>
        public void Garnir()
        {
            if (Boites == null) return;
            Boites.Max = Reglages.BoitesSurLaTable;
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

        /// <summary>Prend un carton dans la reserve. Faux s'il n'en reste plus.</summary>
        public bool PrendreBoite()
        {
            if (Boites == null || Boites.EstVide) return false;
            Boites.Retirer();
            return true;
        }
    }
}
