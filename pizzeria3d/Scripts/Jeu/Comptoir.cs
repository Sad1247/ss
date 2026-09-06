using System.Collections.Generic;
using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le comptoir : recoit les pizzas que le joueur depose, fait patienter les
    /// clients en file et les sert un par un. Tout ce qui s'y pose est en
    /// carton — donc tout ce qui en part aussi.
    /// </summary>
    public sealed class Comptoir : MonoBehaviour
    {
        public Pile Stock;
        public Joueur Joueur;
        public Transform PointFile;          // premiere place de la file
        public Transform Sortie;             // ou les clients s'en vont
        public Caisse Caisse;

        /// <summary>Vrai des qu'un employe tient la caisse a la place du joueur.</summary>
        public bool CaissierPresent;

        readonly List<Client> _file = new List<Client>();
        float _compteurArrivee;

        public int PlacesLibres => Reglages.FileMax - _file.Count;
        public int TailleFile => _file.Count;
        /// <summary>Le client actuellement au comptoir, s'il y en a un.</summary>
        public Client Premier => _file.Count > 0 ? _file[0] : null;

        void Awake()
        {
            if (Stock != null) Stock.Max = Reglages.StockComptoirMax;
        }

        void Update()
        {
            Recevoir();
            FaireVenir();
            Ranger();
            Servir();
        }

        /// <summary>Le joueur vide sa pile sur le comptoir, pizza par pizza.</summary>
        void Recevoir()
        {
            if (Joueur == null || Stock == null || Stock.EstPleine) return;
            if (Joueur.Portee.EstVide || !Joueur.PretPourTransfert) return;
            if (!Joueur.EstPres(transform.position, Reglages.RayonRamassage + 0.6f)) return;

            // Le comptoir ne recoit que des cartons : une pizza nue posee au
            // milieu des boites faisait une pile batarde, et le client repartait
            // avec un melange. Le joueur emballe donc en deposant.
            Joueur.Portee.Retirer();
            Stock.Ajouter(true);
            Joueur.ArmerTransfert();
        }

        void FaireVenir()
        {
            if (PlacesLibres <= 0) return;
            _compteurArrivee += Time.deltaTime;
            if (_compteurArrivee < Reglages.DelaiClient) return;
            _compteurArrivee = 0f;
            _file.Add(Client.Creer(this, _file.Count));
        }

        void Ranger()
        {
            for (int i = 0; i < _file.Count; i++) _file[i].RangA(i);
        }

        /// <summary>
        /// Quelqu'un doit tenir la caisse : le joueur en personne tant qu'aucun
        /// employe n'est embauche, l'employe ensuite.
        /// </summary>
        public bool CaisseTenue =>
            CaissierPresent ||
            (Joueur != null && Joueur.EstPres(transform.position, Reglages.RayonService));

        void Servir()
        {
            if (_file.Count == 0 || !CaisseTenue) return;
            var premier = _file[0];
            if (!premier.EstArrive) return;

            // Le client passe d'abord commande : le tiroir s'ouvre a cet
            // instant precis, et rien ne lui est remis avant la fin.
            if (!premier.CommandeCommencee && Caisse != null) Caisse.Ouvrir();
            premier.Commander(Time.deltaTime);
            if (!premier.CommandeFinie) return;

            if (Stock == null || Stock.EstVide) return;
            // le client repart avec ce qu'on lui tend : une boite si le
            // caissier est passe par la table, la pizza nue sinon
            if (!premier.Recevoir(Stock.SommetEmballe)) return;   // pas encore le moment
            Stock.Retirer();

            if (premier.EstServi)
            {
                // le client paie en lachant une liasse : au joueur d'aller la prendre
                Billet.Lacher(transform.position + Vector3.up * 1.2f,
                              premier.Pizzas * Reglages.PrixPizza, Joueur);
                premier.Partir();
                _file.RemoveAt(0);
                if (Caisse != null) Caisse.Fermer();   // commande terminee
            }
        }

        public Vector3 PlaceDeLaFile(int rang)
        {
            var depart = PointFile != null ? PointFile.position : transform.position + Vector3.back * 2f;
            return depart + new Vector3(0f, 0f, -1.35f * rang);
        }

        public Vector3 PointDeSortie => Sortie != null ? Sortie.position
                                                       : transform.position + new Vector3(10f, 0f, -6f);

        /// <summary>Un client parti sans etre servi : sa commande s'arrete la.</summary>
        public void Oublier(Client c)
        {
            bool etaitAuComptoir = _file.Count > 0 && _file[0] == c;
            _file.Remove(c);
            if (etaitAuComptoir && Caisse != null) Caisse.Fermer();
        }
    }
}
