using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La table de la salle. Un client servi s'y installe pour manger, s'il y
    /// a de la place ; sinon il emporte ses boites et s'en va. Une seule place
    /// est offerte : c'est ce qui fait la file dehors quand ca marche bien.
    /// </summary>
    public sealed class TableRepas : MonoBehaviour
    {
        public Transform Siege;

        Client _occupant;

        public Client Occupant => _occupant;
        public bool EstLibre => _occupant == null;

        /// <summary>Reserve la place. Faux si quelqu'un mange deja.</summary>
        public bool Accueillir(Client client)
        {
            if (_occupant != null || Siege == null) return false;
            _occupant = client;
            return true;
        }

        public void Liberer(Client client)
        {
            if (_occupant == client) _occupant = null;
        }

        public Vector3 Place => Siege != null ? Siege.position : transform.position;

        /// <summary>Ou regarder une fois assis : vers le plateau.</summary>
        public Vector3 VersLaTable(Vector3 depuis)
        {
            var d = transform.position - depuis;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
        }
    }
}
