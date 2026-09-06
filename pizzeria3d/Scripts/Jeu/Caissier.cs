using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// L'employe embauche a la caisse. Son travail, pizza par pizza : il prend
    /// un carton dans la reserve, le pose au rond rouge, le fait glisser au
    /// rond vert, va chercher UNE pizza au four, revient la mettre dedans. Une
    /// fois sa poignee de boites prete, il la porte au comptoir.
    ///
    /// Le trajet vers le comptoir passe par un point de relais : en ligne
    /// droite il le traverserait.
    /// </summary>
    public sealed class Caissier : MonoBehaviour
    {
        enum Etat { Poste, VersTable, Travaille, VersFour, Ramasse, VersComptoir }

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
        int _pleines;              // boites garnies qui attendent au rond vert

        public int Portees => _portee != null ? _portee.Nombre : 0;
        /// <summary>Vrai quand tout ce qu'il porte est en boite.</summary>
        public bool PorteeEmballee => _portee != null && !_portee.EstVide && _portee.ToutEmballe;
        /// <summary>Vrai quand il rapporte une pizza nue du four.</summary>
        public bool PorteeNue => _portee != null && !_portee.EstVide && !_portee.ToutEmballe;
        /// <summary>Boites garnies posees sur le plan, pas encore livrees.</summary>
        public int Pretes => _pleines;

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
                case Etat.VersTable:    Aller(PointTable(), Etat.Travaille); break;
                case Etat.Travaille:    Travailler(); break;
                case Etat.VersFour:     Aller(DevantFour(), Etat.Ramasse); break;
                case Etat.Ramasse:      Ramasser(); break;
                case Etat.VersComptoir: Aller(Poste, Etat.Poste); break;
            }
        }

        // ------------------------------------------------------------------

        void Attendre()
        {
            Decharger();
            if (!_portee.EstVide || Four == null || Comptoir == null || Table == null) return;

            bool stockBas = Comptoir.Stock.Nombre <= Reglages.SeuilRechargeComptoir;
            if (stockBas && !Four.Sortie.EstVide)
            {
                _etat = Etat.VersTable;
                _passeParRelais = true;
            }
        }

        /// <summary>
        /// Le geste, decompose. Un seul pas par appel, espace par le delai
        /// d'emballage : sinon tout se ferait dans la meme image et on ne
        /// verrait jamais le carton passer d'un rond a l'autre.
        /// </summary>
        void Travailler()
        {
            if (Table == null) { Livrer(); return; }
            if (_compteurTransfert > 0f) return;

            // 1. la pizza rapportee du four va dans la boite du rond vert
            if (!_portee.EstVide)
            {
                _portee.Retirer();
                _pleines++;
                _compteurTransfert = Reglages.DelaiEmballage;
                return;
            }

            // 2. la poignee est complete : au comptoir
            if (_pleines >= Reglages.CapacitePorteeCaissier) { Livrer(); return; }

            // 3. plus rien a cuire : il livre ce qu'il a, ou rentre bredouille
            if (Four == null || Four.Sortie.EstVide) { Livrer(); return; }

            // 4. le carton du rond rouge glisse au rond vert
            if (!Table.Preparation.EstVide)
            {
                Table.Preparation.Retirer();
                Table.Assemblage.Ajouter(true);
                _compteurTransfert = Reglages.DelaiEmballage;
                return;
            }

            // 5. une boite vide attend au rond vert : il part chercher sa pizza
            if (Table.Assemblage.Nombre > _pleines) { _etat = Etat.VersFour; return; }

            // 6. sinon il sort un carton neuf de la reserve, sur le rond rouge
            if (Table.PrendreBoite())
            {
                Table.Preparation.Ajouter(true);
                _compteurTransfert = Reglages.DelaiEmballage;
            }
        }

        /// <summary>Emporte les boites garnies et rend celle restee vide.</summary>
        void Livrer()
        {
            if (Table != null)
            {
                while (Table.Assemblage.Nombre > _pleines)
                {
                    Table.Assemblage.Retirer();
                    Table.Rendre();                 // le carton retourne en reserve
                }
                for (int i = 0; i < _pleines; i++)
                {
                    Table.Assemblage.Retirer();
                    _portee.Ajouter(true);
                }
            }
            _pleines = 0;
            _etat = Etat.VersComptoir;
            _passeParRelais = true;
        }

        /// <summary>Une seule pizza par voyage : elle a sa boite qui l'attend.</summary>
        void Ramasser()
        {
            if (Four == null || Four.Sortie.EstVide) { _etat = Etat.VersTable; return; }
            if (_compteurTransfert > 0f) return;

            Four.Sortie.Retirer();
            _portee.Ajouter();
            _compteurTransfert = Reglages.DelaiTransfert;
            _etat = Etat.VersTable;
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
