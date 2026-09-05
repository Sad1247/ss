using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// L'employe embauche a la caisse. Il ne se contente pas de tenir le
    /// comptoir : quand le stock baisse, il va chercher des pizzas au four,
    /// les rapporte et les depose. Le joueur n'a plus qu'a ramasser l'argent.
    ///
    /// Son trajet passe par un point de relais : en ligne droite il traverserait
    /// le comptoir.
    /// </summary>
    public sealed class Caissier : MonoBehaviour
    {
        enum Etat { Poste, VersFour, Charge, VersComptoir }

        public Comptoir Comptoir;
        public Four Four;
        public Vector3 Poste;
        public Vector3 Relais;

        Pile _portee;
        Transform _corps;
        Etat _etat = Etat.Poste;
        bool _passeParRelais;
        float _balancement;
        float _compteurTransfert;

        public int Portees => _portee != null ? _portee.Nombre : 0;

        void OnEnable()
        {
            if (Comptoir != null) Comptoir.CaissierPresent = true;
        }

        void Awake()
        {
            _corps = transform.Find("Corps");
            var p = new GameObject("PilePortee");
            p.transform.SetParent(transform, false);
            p.transform.localPosition = new Vector3(0f, Reglages.HauteurTete, 0f);
            _portee = p.AddComponent<Pile>();
            _portee.Max = Reglages.CapacitePorteeCaissier;
        }

        void Update()
        {
            if (_compteurTransfert > 0f) _compteurTransfert -= Time.deltaTime;
            Balancer();

            switch (_etat)
            {
                case Etat.Poste:        Attendre(); break;
                case Etat.VersFour:     Aller(DevantFour(), Etat.Charge); break;
                case Etat.Charge:       Charger(); break;
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
            if (Four == null) { _etat = Etat.VersComptoir; return; }
            if (_portee.EstPleine || Four.Sortie.EstVide)
            {
                _etat = Etat.VersComptoir;
                _passeParRelais = true;
                return;
            }
            if (_compteurTransfert > 0f) return;

            Four.Sortie.Retirer();
            _portee.Ajouter();
            _compteurTransfert = Reglages.DelaiTransfert;
        }

        void Decharger()
        {
            if (_portee.EstVide || Comptoir == null || Comptoir.Stock.EstPleine) return;
            if (_compteurTransfert > 0f) return;

            _portee.Retirer();
            Comptoir.Stock.Ajouter();
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

        void Balancer()
        {
            if (_corps == null || _etat != Etat.Poste) return;
            // un leger balancement : plante raide, il aurait l'air d'un decor
            _balancement += Time.deltaTime * 1.6f;
            var p = _corps.localPosition;
            p.y = Mathf.Sin(_balancement) * 0.03f;
            _corps.localPosition = p;
        }
    }
}
