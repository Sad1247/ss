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
        /// <summary>Le coin des plateaux : ce qui se mange sur place ne s'emballe pas.</summary>
        public Pile Plateaux;
        public Joueur Joueur;
        public Transform PointFile;          // premiere place de la file
        public Transform Sortie;             // ou les clients s'en vont
        public Caisse Caisse;
        /// <summary>La table de la salle, ou les clients servis s'attablent.</summary>
        public TableRepas Table;

        /// <summary>Vrai des qu'un employe tient la caisse a la place du joueur.</summary>
        public bool CaissierPresent;

        /// <summary>
        /// L'employe, meme rentre chez lui : c'est le comptoir qui le rappelle
        /// a l'ouverture, lui-meme etant eteint et incapable de le faire.
        /// </summary>
        public Caissier Employe;

        readonly List<Client> _file = new List<Client>();
        float _compteurArrivee;

        /// <summary>Combien de clients ont pousse la porte depuis l'ouverture.</summary>
        public int Arrivees { get; private set; }

        public int PlacesLibres => Reglages.FileMax - _file.Count;
        public int TailleFile => _file.Count;
        /// <summary>Le client actuellement au comptoir, s'il y en a un.</summary>
        public Client Premier => _file.Count > 0 ? _file[0] : null;

        void Awake()
        {
            if (Stock != null) Stock.Max = Reglages.StockComptoirMax;
            if (Plateaux != null) Plateaux.Max = Reglages.PlateauxAuComptoir;
        }

        /// <summary>
        /// Rouvre : l'employe rentre de sa nuit. Un objet eteint n'execute
        /// rien, c'est donc au comptoir de le reveiller.
        /// </summary>
        void RappelerLEmploye()
        {
            if (Employe == null || !Employe.Embauche) return;
            if (Employe.gameObject.activeSelf) return;
            if (Horloge.Active == null || !Horloge.Active.Ouverte) return;
            Employe.Reprendre();
        }

        void Update()
        {
            RappelerLEmploye();
            Recevoir();
            FaireVenir();
            Ranger();
            Servir();
        }

        /// <summary>
        /// Le joueur vide sa pile sur le comptoir, pizza par pizza. Il dresse
        /// un plateau si quelqu'un mange sur place et qu'aucun n'attend —
        /// sinon il emballe : le comptoir ne recoit rien de nu, une pizza
        /// posee au milieu des boites faisait une pile batarde.
        /// </summary>
        void Recevoir()
        {
            if (Joueur == null || Stock == null) return;
            if (Joueur.Portee.EstVide || !Joueur.PretPourTransfert) return;
            if (!Joueur.EstPres(transform.position, Reglages.RayonRamassage + 0.6f)) return;

            if (UnPlateauManque)
            {
                Joueur.Portee.Retirer();
                Plateaux.Ajouter(Pile.Forme.Plateau);
                Joueur.ArmerTransfert();
                return;
            }

            if (Stock.EstPleine) return;
            Joueur.Portee.Retirer();
            Stock.Ajouter(true);
            Joueur.ArmerTransfert();
        }

        /// <summary>
        /// Vrai quand un client mange sur place, attend encore, et qu'aucun
        /// plateau n'est pret : c'est le signal pour en dresser un.
        /// </summary>
        public bool UnPlateauManque
        {
            get
            {
                if (Plateaux == null || Plateaux.EstPleine) return false;
                foreach (var c in _file)
                    if (c.SurPlace && !c.EstServi) return Plateaux.EstVide;
                return false;
            }
        }

        void FaireVenir()
        {
            // Passe l'heure de fermeture, plus personne n'entre. Ceux qui sont
            // deja dans la file, eux, sont servis jusqu'au dernier.
            if (Horloge.Active != null && !Horloge.Active.Ouverte) return;
            if (PlacesLibres <= 0) return;
            _compteurArrivee += Time.deltaTime;
            if (_compteurArrivee < Reglages.DelaiClient) return;
            _compteurArrivee = 0f;
            _file.Add(Client.Creer(this, _file.Count));
            Arrivees++;
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

            // Celui qui mange sur place prend son plateau, jamais une boite :
            // sa pizza n'a pas ete emballee. Les autres prennent au stock.
            if (premier.SurPlace)
            {
                if (Plateaux == null || Plateaux.EstVide) return;   // il attend son plateau
                if (!premier.Recevoir(Pile.Forme.Plateau)) return;
                Plateaux.Retirer();
            }
            else
            {
                if (Stock == null || Stock.EstVide) return;
                if (!premier.Recevoir(Stock.SommetEmballe ? Pile.Forme.Boite : Pile.Forme.Nue))
                    return;
                Stock.Retirer();
            }

            if (premier.EstServi)
            {
                // Le client paie en lachant une liasse : au joueur d'aller la
                // prendre. Celui qui mange sur place, lui, laisse son argent
                // sur la table en partant.
                if (!premier.SurPlace)
                {
                    int montant = premier.Pizzas * Comptabilite.PrixPizza;
                    Billet.Lacher(transform.position + Vector3.up * 1.2f, montant, Joueur);
                    Comptabilite.EnregistrerVente(premier.Pizzas, montant);
                }
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
