using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// L'employe embauche a la caisse. Son travail, pizza par pizza : il va
    /// chercher UNE pizza au four, revient au plan, sort un carton de la
    /// reserve sur le rond rouge, le fait glisser au rond vert et y depose sa
    /// pizza. Une fois sa poignee de boites prete, il la porte au comptoir.
    ///
    /// Sauf pour qui mange sur place : celle-la ne passe pas par le carton,
    /// elle est dressee sur un plateau au comptoir.
    ///
    /// Le carton ne sort qu'une fois la pizza en main : sinon une boite vide
    /// restait posee de cote pendant tout l'aller-retour au four.
    ///
    /// Le trajet vers le comptoir passe par un point de relais : en ligne
    /// droite il le traverserait.
    /// </summary>
    public sealed class Caissier : MonoBehaviour
    {
        enum Etat { Poste, VersTable, Travaille, VersFour, Ramasse, VersComptoir,
                    VersSalle, VersPoubelle }

        public Comptoir Comptoir;
        public Four Four;
        public Emballage Table;
        /// <summary>La table de la salle, qu'il vient debarrasser.</summary>
        public TableRepas Salle;
        public Vector3 Poste;
        public Vector3 Relais;
        /// <summary>Ou il jette les restes.</summary>
        public Vector3 Poubelle;

        Pile _portee;
        Demarche _demarche;
        Etat _etat = Etat.Poste;
        bool _passeParRelais;
        float _compteurTransfert;
        int _pleines;
        GameObject _ordures;      // les restes qu'il porte a la poubelle
        bool _plateauEnCours;     // un plateau est en preparation dans la fournee              // boites garnies qui attendent au rond vert

        public int Portees => _portee != null ? _portee.Nombre : 0;
        /// <summary>Vrai quand tout ce qu'il porte est en boite.</summary>
        public bool PorteeEmballee => _portee != null && !_portee.EstVide && _portee.ToutEmballe;
        /// <summary>Vrai quand il rapporte une pizza nue du four.</summary>
        public bool PorteeNue => _portee != null && !_portee.EstVide && !_portee.ToutEmballe;
        /// <summary>Boites garnies posees sur le plan, pas encore livrees.</summary>
        public int Pretes => _pleines;
        /// <summary>Vrai quand il porte un plateau dresse pour la salle.</summary>
        public bool PortePlateau => _portee != null && _portee.SommetForme == Pile.Forme.Plateau;

        /// <summary>Vrai quand il transporte les restes d'un repas.</summary>
        public bool PorteDesOrdures => _ordures != null;

        void OnEnable()
        {
            if (Comptoir != null) Comptoir.CaissierPresent = true;
        }

        void Awake()
        {
            _demarche = GetComponent<Demarche>();
            _portee = Portage.Creer(transform, "PilePortee");
            _portee.Max = Reglages.CapacitePorteeCaissier;
        }

        void Update()
        {
            if (_compteurTransfert > 0f) _compteurTransfert -= Time.deltaTime;
            if (_demarche != null) _demarche.BrasPortent = !_portee.EstVide || _ordures != null;

            switch (_etat)
            {
                case Etat.Poste:        Attendre(); break;
                case Etat.VersTable:    Aller(PointTable(), Etat.Travaille); break;
                case Etat.Travaille:    Travailler(); break;
                case Etat.VersFour:     Aller(DevantFour(), Etat.Ramasse); break;
                case Etat.Ramasse:      Ramasser(); break;
                case Etat.VersComptoir: Aller(Poste, Etat.Poste); break;
                case Etat.VersSalle:    Aller(DevantLaSalle(), Etat.VersPoubelle, Debarrasser); break;
                case Etat.VersPoubelle: Aller(Poubelle, Etat.Poste, Jeter); break;
            }
        }

        // ------------------------------------------------------------------

        void Attendre()
        {
            Decharger();
            if (!_portee.EstVide || Four == null || Comptoir == null || Table == null) return;

            // Une table sale bloque la salle : plus personne ne peut manger
            // sur place tant qu'elle n'est pas debarrassee. Cela passe donc
            // avant le reappro du comptoir, qui, lui, n'a pas de fin.
            if (Salle != null && Salle.ADesOrdures)
            {
                _etat = Etat.VersSalle;
                _passeParRelais = true;
                return;
            }

            bool stockBas = Comptoir.Stock.Nombre <= Reglages.SeuilRechargeComptoir;
            if (stockBas && !Four.Sortie.EstVide)
            {
                _etat = Etat.VersFour;               // rien a faire au plan sans pizza
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

            // 1. les mains vides : il repart chercher une pizza, ou il livre
            if (_portee.EstVide)
            {
                if (_pleines >= Reglages.CapacitePorteeCaissier) { Livrer(); return; }
                // un plateau ne se fait pas attendre : le client est au comptoir
                if (_plateauEnCours && _pleines > 0) { Livrer(); return; }
                if (Four == null || Four.Sortie.EstVide) { Livrer(); return; }
                _etat = Etat.VersFour;
                return;
            }

            // 3. Pizza en main : de quoi la recevoir sort sur le rond rouge.
            //    Un plateau si quelqu'un mange sur place et n'a pas le sien —
            //    sa pizza ne passe pas par le carton — un carton sinon.
            if (Table.Preparation.EstVide && Table.Assemblage.Nombre <= _pleines)
            {
                bool pourLaSalle = Comptoir != null && Comptoir.UnPlateauManque && !_plateauEnCours;
                if (pourLaSalle && Table.PrendrePlateau())
                {
                    Table.Preparation.Ajouter(Pile.Forme.Plateau);
                    _plateauEnCours = true;
                    _compteurTransfert = Reglages.DelaiEmballage;
                    return;
                }
                if (Table.PrendreBoite())
                {
                    Table.Preparation.Ajouter(true);
                    _compteurTransfert = Reglages.DelaiEmballage;
                }
                return;                              // reserve vide : il attend
            }

            // 4. le carton glisse du rond rouge au rond vert
            if (!Table.Preparation.EstVide)
            {
                var forme = Table.Preparation.SommetForme;
                Table.Preparation.Retirer();
                Table.Assemblage.Ajouter(forme);
                _compteurTransfert = Reglages.DelaiEmballage;
                return;
            }

            // 5. la pizza entre dans la boite ouverte
            _portee.Retirer();
            _pleines++;
            _compteurTransfert = Reglages.DelaiEmballage;
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
                    var forme = Table.Assemblage.SommetForme;
                    Table.Assemblage.Retirer();
                    _portee.Ajouter(forme);
                }
            }
            _pleines = 0;
            _plateauEnCours = false;
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
            if (_portee.EstVide || Comptoir == null) return;
            if (_compteurTransfert > 0f) return;

            var forme = _portee.SommetForme;
            if (forme == Pile.Forme.Plateau)
            {
                // le plateau ne se range pas avec les cartons : il attend
                // dans son coin, pret a etre tendu
                if (Comptoir.Plateaux == null || Comptoir.Plateaux.EstPleine) return;
                _portee.Retirer();
                Comptoir.Plateaux.Ajouter(Pile.Forme.Plateau);
            }
            else
            {
                if (Comptoir.Stock.EstPleine) return;
                _portee.Retirer();
                Comptoir.Stock.Ajouter(forme);
            }
            _compteurTransfert = Reglages.DelaiTransfert;
        }

        /// <summary>Il prend les restes sur la table et les met dans ses mains.</summary>
        void Debarrasser()
        {
            if (Salle == null) return;
            var restes = Salle.EmporterOrdures();
            if (restes == null) return;

            _ordures = restes;
            var mains = transform.Find("Corps/Mains");
            _ordures.transform.SetParent(mains != null ? mains : transform, false);
            _ordures.transform.localPosition = Vector3.zero;
            _passeParRelais = true;
        }

        void Jeter()
        {
            if (_ordures != null) { Destroy(_ordures); _ordures = null; }
            _passeParRelais = true;
        }

        Vector3 DevantLaSalle()
        {
            if (Salle == null) return Poste;
            var p = Salle.transform.position;
            return new Vector3(p.x, 0f, p.z - 1.4f);   // devant la table, pas dessus
        }

        /// <summary>Marche vers un point, en passant d'abord par le relais.</summary>
        void Aller(Vector3 cible, Etat arrivee, System.Action arrive = null)
        {
            var but = _passeParRelais ? Relais : cible;
            if (Avancer(but))
            {
                if (_passeParRelais) _passeParRelais = false;
                else { _etat = arrivee; if (arrive != null) arrive(); }
            }
        }

        bool Avancer(Vector3 but)
        {
            var delta = but - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.05f) return true;

            var pas = delta.normalized * Reglages.VitesseCaissier * Time.deltaTime;
            transform.position += pas;
            transform.rotation = Quaternion.Slerp(transform.rotation,
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
