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
        /// <summary>Le dessus du plateau : c'est la que se pose l'assiette.</summary>
        public Transform Plateau;

        Client _occupant;
        GameObject _pizza;
        GameObject _ordures;
        int _mangees;             // quartiers deja avales

        public Client Occupant => _occupant;
        public bool EstLibre => _occupant == null;

        /// <summary>
        /// Reserve la place. Faux si quelqu'un mange deja — ou si la table
        /// n'a pas ete debarrassee : personne ne s'assoit devant les restes
        /// du precedent.
        /// </summary>
        public bool Accueillir(Client client)
        {
            if (_occupant != null || Siege == null || _ordures != null) return false;
            _occupant = client;
            return true;
        }

        public void Liberer(Client client)
        {
            if (_occupant == client) _occupant = null;
        }

        public Vector3 Place => Siege != null ? Siege.position : transform.position;

        /// <summary>Quartiers encore dans l'assiette.</summary>
        public int Quartiers => _pizza == null ? 0 : Reglages.QuartiersParPizza - _mangees;
        public bool ADesOrdures => _ordures != null;

        /// <summary>
        /// Le client pose sa pizza en s'installant. La table est debarrassee
        /// au meme moment : les restes du precedent n'ont plus lieu d'etre.
        /// </summary>
        public void PoserPizza()
        {
            if (_pizza != null) { Destroy(_pizza); _pizza = null; }
            _mangees = 0;
            _pizza = new GameObject("PizzaTable");
            _pizza.transform.SetParent(Ancrage, false);
            _pizza.transform.localPosition = Vector3.zero;
            for (int i = 0; i < Reglages.QuartiersParPizza; i++) Pizza3D.Quartier(_pizza.transform, i);
        }

        /// <summary>Avale une part. Faux quand l'assiette est vide.</summary>
        public bool CroquerUnQuartier()
        {
            if (_pizza == null || _mangees >= Reglages.QuartiersParPizza) return false;
            foreach (var e in Enfants(_pizza.transform))
                if (e.gameObject.name == "Quartier" + _mangees)
                {
                    Destroy(e.gameObject);
                    break;
                }
            _mangees++;
            return true;
        }

        /// <summary>Fin du repas : il ne reste que la croute et la serviette.</summary>
        public void LaisserOrdures()
        {
            if (_pizza != null) { Destroy(_pizza); _pizza = null; }
            if (_ordures != null) return;

            _ordures = new GameObject("Ordures");
            _ordures.transform.SetParent(Ancrage, false);
            _ordures.transform.localPosition = Vector3.zero;
            var t = _ordures.transform;

            var croute = Bloc.Couleur(0xC58B3E);
            Bloc.Galet("Croute", t, new Vector3(-0.12f, 0.03f, 0.06f),
                       new Vector3(0.26f, 0.06f, 0.10f), croute).SansCollision();
            Bloc.Galet("Croute", t, new Vector3(0.10f, 0.03f, -0.04f),
                       new Vector3(0.22f, 0.06f, 0.09f), croute, 24f).SansCollision();
            Bloc.Boite("Serviette", t, new Vector3(0.04f, 0.02f, 0.14f),
                       new Vector3(0.20f, 0.03f, 0.16f), Bloc.Couleur(0xF2EFE6)).SansCollision();
        }

        /// <summary>
        /// Le caissier emporte les restes : la table les lache, a lui de les
        /// jeter. Renvoie null s'il n'y a rien a debarrasser.
        /// </summary>
        public GameObject EmporterOrdures()
        {
            var restes = _ordures;
            _ordures = null;
            return restes;
        }

        Transform Ancrage => Plateau != null ? Plateau : transform;

        /// <summary>Les enfants d'un transform, copies : on en detruit pendant.</summary>
        static System.Collections.Generic.List<Transform> Enfants(Transform t)
        {
            var liste = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < t.childCount; i++) liste.Add(t.GetChild(i));
            return liste;
        }

        /// <summary>Ou regarder une fois assis : vers le plateau.</summary>
        public Vector3 VersLaTable(Vector3 depuis)
        {
            var d = transform.position - depuis;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
        }
    }
}
