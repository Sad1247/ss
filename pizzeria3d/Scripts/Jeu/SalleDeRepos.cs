using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La salle de repos : des sieges en nombre limite, que les employes
    /// fatigues reservent le temps de recuperer. Une place reservee reste a
    /// son occupant jusqu'a ce qu'il la rende — deux employes ne se
    /// disputent jamais le meme siege.
    /// </summary>
    public sealed class SalleDeRepos : MonoBehaviour
    {
        /// <summary>Un siege par place, quel que soit le niveau atteint.</summary>
        public Transform[] Sieges = new Transform[0];

        bool[] _occupes;

        /// <summary>
        /// Sieges assigne apres coup (le composant s'eveille des l'ajout,
        /// avant que Batisseur n'ait rempli le tableau) : la taille se
        /// verifie a chaque usage plutot qu'une seule fois dans Awake.
        /// </summary>
        void AssurerTaille()
        {
            if (_occupes == null || _occupes.Length != Sieges.Length)
                _occupes = new bool[Sieges.Length];
        }

        void Update()
        {
            // Les sieges au-dela du niveau achete restent ranges, invisibles :
            // la salle grandit vraiment quand on l'ameliore, pas seulement
            // sur le papier.
            int ouvertes = PlacesOuvertes;
            for (int i = 0; i < Sieges.Length; i++)
                if (Sieges[i] != null) Sieges[i].gameObject.SetActive(i < ouvertes);
        }

        /// <summary>Places physiquement presentes dans le decor.</summary>
        public int NombreDeSieges => Sieges.Length;

        /// <summary>Combien de places sont ouvertes au niveau actuel de la salle.</summary>
        public int PlacesOuvertes => Mathf.Min(Sieges.Length, Comptabilite.CapaciteSalleRepos);

        public int PlacesOccupees
        {
            get
            {
                AssurerTaille();
                int n = 0;
                for (int i = 0; i < _occupes.Length && i < PlacesOuvertes; i++) if (_occupes[i]) n++;
                return n;
            }
        }

        public int PlacesLibres => Mathf.Max(0, PlacesOuvertes - PlacesOccupees);

        /// <summary>
        /// Reserve le premier siege ouvert et libre. Renvoie -1 si la salle
        /// est complete — l'appelant continue alors son travail au lieu
        /// d'attendre une place qui n'existe pas encore.
        /// </summary>
        public int Reserver()
        {
            AssurerTaille();
            int max = PlacesOuvertes;
            for (int i = 0; i < max && i < Sieges.Length; i++)
            {
                if (_occupes[i]) continue;
                _occupes[i] = true;
                return i;
            }
            return -1;
        }

        public Vector3 PositionDuSiege(int index)
            => index >= 0 && index < Sieges.Length ? Sieges[index].position : transform.position;

        public void Liberer(int index)
        {
            AssurerTaille();
            if (index < 0 || index >= _occupes.Length) return;
            _occupes[index] = false;
        }
    }
}
