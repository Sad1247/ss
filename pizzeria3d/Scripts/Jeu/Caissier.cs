using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// L'employe embauche a la caisse. Il ne se contente pas de tenir le
    /// comptoir : quand le stock baisse, il va chercher des pizzas au four,
    /// passe par la table pour les mettre en boite, puis les depose au
    /// comptoir. Le joueur n'a plus qu'a ramasser l'argent.
    ///
    /// Son trajet passe par un point de relais : en ligne droite il traverserait
    /// le comptoir.
    /// </summary>
    public sealed class Caissier : MonoBehaviour
    {
        enum Etat { Poste, VersFour, Charge, VersTable, Emballe, VersComptoir }

        public Comptoir Comptoir;
        public Four Four;
        public Emballage Table;
        public Vector3 Poste;
        public Vector3 Relais;

        Pile _portee;
        Transform _corps;
        Demarche _demarche;
        Etat _etat = Etat.Poste;
        bool _passeParRelais;
        float _compteurTransfert;

        public int Portees => _portee != null ? _portee.Nombre : 0;
        /// <summary>Vrai quand tout ce qu'il porte est en boite.</summary>
        public bool PorteeEmballee => _portee != null && !_portee.EstVide && _portee.ToutEmballe;

        void OnEnable()
        {
            if (Comptoir != null) Comptoir.CaissierPresent = true;
        }

        void Awake()
        {
            _corps = transform.Find("Corps");
            _demarche = GetComponent<Demarche>();
            _portee = Portage.Creer(transform, "PilePortee");
            _portee.Max = Reglages.CapacitePorteeCaissier;
        }

        void Update()
        {
            if (_compteurTransfert > 0f) _compteurTransfert -= Time.deltaTime;
            if (_demarche != null) _demarche.BrasPortent = !_portee.EstVide;

            switch (_etat)
            {
                case Etat.Poste:        Attendre(); break;
                case Etat.VersFour:     Aller(DevantFour(), Etat.Charge); break;
                case Etat.Charge:       Charger(); break;
                case Etat.VersTable:    Aller(PointTable(), Etat.Emballe); break;
                case Etat.Emballe:      Emballer(); break;
                case Etat.VersComptoir: Aller(Poste, Etat.Poste); break;
            }
        }

        // ------------------------------------------------------------------

        void Attendre()
        {
            Decharger();
            if (!_portee.EstVide || Four == null || Comptoir == null) return;

            bool stockBas = Comptoir.Stock.Nombre <= Reglages.SeuilRechargeComptoir;
            if (stockBas && !Four.Sortie.EstVide)
            {
                _etat = Etat.VersFour;
                _passeParRelais = true;
            }
        }

        void Charger()
        {
            if (Four == null) { _etat = Etat.VersComptoir; _passeParRelais = true; return; }
            if (_portee.EstPleine || Four.Sortie.EstVide)
            {
                // rien ne part au comptoir sans passer par la table
                _etat = _portee.EstVide ? Etat.VersComptoir : Etat.VersTable;
                _passeParRelais = _portee.EstVide;
                return;
            }
            if (_compteurTransfert > 0f) return;

            Four.Sortie.Retirer();
            _portee.Ajouter();
            _compteurTransfert = Reglages.DelaiTransfert;
        }

        /// <summary>
        /// Une pizza dans un carton, au rythme du geste. Il ne repart qu'une
        /// fois tout emballe : c'est la condition pour servir un client.
        /// </summary>
        void Emballer()
        {
            if (Table == null || _portee.ToutEmballe)
            {
                _etat = Etat.VersComptoir;
                _passeParRelais = true;
                return;
            }
            if (_compteurTransfert > 0f) return;
            if (!Table.PrendreBoite()) return;      // reserve vide : il attend le reappro

            _portee.EmballerUne();
            _compteurTransfert = Reglages.DelaiEmballage;
        }

        Vector3 PointTable() => Table != null ? Table.PointDeTravail : Poste;

        void Decharger()
        {
            if (_portee.EstVide || Comptoir == null || Comptoir.Stock.EstPleine) return;
            if (_compteurTransfert > 0f) return;

            bool emballe = _portee.SommetEmballe;
            _portee.Retirer();
            Comptoir.Stock.Ajouter(emballe);
            _compteurTransfert = Reglages.DelaiTransfert;
        }

        /// <summary>Marche vers un point, en passant d'abord par le relais.</summary>
        void Aller(Vector3 cible, Etat arrivee)
        {
            var but = _passeParRelais ? Relais : cible;
            if (Avancer(but))
            {
                if (_passeParRelais) _passeParRelais = false;
                else _etat = arrivee;
            }
        }

        bool Avancer(Vector3 but)
        {
            var delta = but - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.05f) return true;

            var pas = delta.normalized * Reglages.VitesseCaissier * Time.deltaTime;
            transform.position += pas;
            if (_corps != null)
                _corps.rotation = Quaternion.Slerp(_corps.rotation,
                                                   Quaternion.LookRotation(delta.normalized, Vector3.up),
                                                   Reglages.VitesseRotation * Time.deltaTime);
            return false;
        }

        Vector3 DevantFour()
        {
            if (Four == null) return Poste;
            var p = Four.Sortie.transform.position;
            return new Vector3(p.x, 0f, p.z - 1.0f);   // devant la pierre, pas dedans
        }

    }
}
