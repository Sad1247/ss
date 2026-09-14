using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>L'etat d'une table, tel que le registre le lit.</summary>
    public enum EtatTable
    {
        /// <summary>Pas encore payee : elle n'existe pas pour les clients.</summary>
        Verrouillee,
        /// <summary>Debloquee et sans occupant : un groupe peut s'y installer.</summary>
        Libre,
        /// <summary>Un groupe y mange, ou ses restes attendent le caissier.</summary>
        Occupee,
    }

    /// <summary>
    /// Une table de la salle. Un client servi s'y installe pour manger, s'il y
    /// a de la place ; sinon il emporte ses boites et s'en va. Une table
    /// n'accueille qu'un groupe a la fois — c'est ce qui fait la file dehors
    /// quand ca marche bien — mais elle compte plusieurs chaises, et sait
    /// lesquelles sont encore libres.
    ///
    /// Verrouillee tant que son objet est eteint : c'est la dalle verte qui
    /// l'allume, une fois payee. Rien d'autre n'a besoin d'etre prevenu.
    /// </summary>
    public sealed class TableRepas : MonoBehaviour
    {
        /// <summary>La premiere chaise. Conservee pour qui ne pose qu'un siege.</summary>
        public Transform Siege;
        /// <summary>Toutes les chaises, celle de <see cref="Siege"/> comprise.</summary>
        public Transform[] Sieges = new Transform[0];
        /// <summary>Le dessus du plateau : c'est la que se pose l'assiette.</summary>
        public Transform Plateau;

        /// <summary>Emprise au sol, posee quand la table s'allume.</summary>
        public float EmpriseX = 2.9f, EmpriseZ = 1.5f;

        Client _occupant;
        bool _emprisePosee;
        int _siegePris = -1;
        GameObject _pizza;
        GameObject _ordures;
        int _mangees;             // quartiers deja avales

        public Client Occupant => _occupant;
        public bool EstLibre => _occupant == null;

        /// <summary>
        /// Une table verrouillee ne barre rien : sans cela, son emprise
        /// empecherait le joueur de fouler la dalle qui l'ouvre, posee a
        /// l'endroit meme ou elle apparaitra. Elle la pose donc en
        /// s'allumant, une seule fois.
        /// </summary>
        void OnEnable()
        {
            if (_emprisePosee) return;
            _emprisePosee = true;
            Obstacles.Ajouter(transform.position, EmpriseX, EmpriseZ);
            EcarterLeJoueur();
        }

        /// <summary>
        /// Le joueur vient de payer la dalle posee ici meme : la table
        /// apparait sous ses pieds. Sans cela il resterait enferme dans son
        /// emprise. Il est pousse dehors, par le cote le plus proche.
        /// </summary>
        void EcarterLeJoueur()
        {
            var joueur = Object.FindObjectOfType<Joueur>();
            if (joueur == null) return;

            var p = joueur.Position;
            var c = transform.position;
            float marge = Reglages.RayonJoueur + 0.05f;
            float demiX = EmpriseX * 0.5f + marge, demiZ = EmpriseZ * 0.5f + marge;
            float dx = p.x - c.x, dz = p.z - c.z;
            if (Mathf.Abs(dx) >= demiX || Mathf.Abs(dz) >= demiZ) return;   // deja dehors

            // Par le cote dont il est le plus pres : le moins de chemin, et
            // jamais au travers du meuble.
            if (demiX - Mathf.Abs(dx) <= demiZ - Mathf.Abs(dz))
                p.x = c.x + (dx < 0f ? -demiX : demiX);
            else
                p.z = c.z + (dz < 0f ? -demiZ : demiZ);
            joueur.transform.position = p;
        }

        /// <summary>
        /// Les chaises reellement posees : le tableau s'il est rempli, sinon
        /// le siege unique. Assigne apres coup par le batisseur, il se relit
        /// a chaque usage plutot qu'une fois dans Awake.
        /// </summary>
        Transform[] Chaises
        {
            get
            {
                if (Sieges != null && Sieges.Length > 0) return Sieges;
                return Siege != null ? new[] { Siege } : new Transform[0];
            }
        }

        public int NombreDeChaises => Chaises.Length;

        /// <summary>
        /// Combien de chaises restent libres. Une table prise par un groupe
        /// ne se partage pas : les autres chaises sont a lui.
        /// </summary>
        public int ChaisesLibres => _occupant != null ? 0 : NombreDeChaises;

        /// <summary>
        /// Verrouillee tant que l'objet est eteint, occupee tant qu'un groupe
        /// y mange ou que ses restes trainent, libre sinon.
        /// </summary>
        public EtatTable Etat
        {
            get
            {
                if (!gameObject.activeInHierarchy) return EtatTable.Verrouillee;
                return _occupant != null || _ordures != null ? EtatTable.Occupee : EtatTable.Libre;
            }
        }

        /// <summary>Vrai quand un client peut vraiment s'y attabler.</summary>
        public bool PeutAccueillir => Etat == EtatTable.Libre && NombreDeChaises > 0;

        /// <summary>
        /// Reserve la place et retient la chaise prise. Faux si quelqu'un
        /// mange deja, si la table est encore verrouillee — ou si elle n'a pas
        /// ete debarrassee : personne ne s'assoit devant les restes du
        /// precedent.
        /// </summary>
        public bool Accueillir(Client client)
        {
            if (!PeutAccueillir) return false;
            _occupant = client;
            _siegePris = 0;
            return true;
        }

        public void Liberer(Client client)
        {
            if (_occupant != client) return;
            _occupant = null;
            _siegePris = -1;
        }

        /// <summary>La chaise retenue par ce client, ou la table a defaut.</summary>
        public Vector3 PlaceDe(Client client)
        {
            var chaises = Chaises;
            if (_occupant == client && _siegePris >= 0 && _siegePris < chaises.Length)
                return chaises[_siegePris].position;
            return Place;
        }

        public Vector3 Place
        {
            get
            {
                var chaises = Chaises;
                return chaises.Length > 0 ? chaises[0].position : transform.position;
            }
        }

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

            // Le plateau d'abord, les parts posees dessus : c'est le plateau
            // que le client rapporte du comptoir.
            _pizza = Plateau3D.Creer(Ancrage, 0, false);
            _pizza.name = "PlateauTable";
            for (int i = 0; i < Reglages.QuartiersParPizza; i++)
                Plateau3D.PoserDessus(Pizza3D.Quartier(_pizza.transform, i));
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
            if (_ordures != null) return;

            // Le plateau reste : ce sont lui et ses restes que le caissier
            // vient debarrasser.
            if (_pizza != null)
            {
                foreach (var e in Enfants(_pizza.transform))
                    if (e.gameObject.name.StartsWith("Quartier")) Destroy(e.gameObject);
                _ordures = _pizza;
                _ordures.name = "Ordures";
                _pizza = null;
            }
            else
            {
                _ordures = new GameObject("Ordures");
                _ordures.transform.SetParent(Ancrage, false);
                _ordures.transform.localPosition = Vector3.zero;
            }
            var t = _ordures.transform;

            float dessus = Plateau3D.Hauteur * 0.75f;
            var croute = Bloc.Couleur(0xC58B3E);
            Bloc.Galet("Croute", t, new Vector3(-0.12f, dessus + 0.03f, 0.06f),
                       new Vector3(0.26f, 0.06f, 0.10f), croute).SansCollision();
            Bloc.Galet("Croute", t, new Vector3(0.10f, dessus + 0.03f, -0.04f),
                       new Vector3(0.22f, 0.06f, 0.09f), croute, 24f).SansCollision();
            Bloc.Boite("Serviette", t, new Vector3(0.04f, dessus + 0.02f, 0.14f),
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
