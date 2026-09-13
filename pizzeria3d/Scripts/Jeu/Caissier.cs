using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// L'employe embauche a la caisse. Son travail, pizza par pizza : il va
    /// chercher UNE pizza au four, revient au plan, sort directement un
    /// carton de la reserve sur le rond du plan et y depose sa pizza. Une
    /// fois sa poignee de boites prete, il la porte au comptoir.
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
                    VersSalle, VersPoubelle, VersRepos, SeRepose, SortDuRepos, SEnVa }

        public Comptoir Comptoir;
        public Four Four;
        public Emballage Table;
        /// <summary>La table de la salle, qu'il vient debarrasser.</summary>
        public TableRepas Salle;
        /// <summary>Ou il va recuperer une fois trop fatigue.</summary>
        public SalleDeRepos SalleRepos;
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
        int _livrees;             // boites deposees au comptoir, depuis l'embauche
        GameObject _ordures;      // les restes qu'il porte a la poubelle
        bool _plateauEnCours;     // un plateau est en preparation dans la fournee              // boites garnies qui attendent au rond vert
        float _fatigue;
        int _siegeRepos = -1;     // le siege reserve, le temps de la pause
        int _etapeRepos;          // ou il en est du chemin vers la salle de repos

        public int Portees => _portee != null ? _portee.Nombre : 0;
        /// <summary>Boites livrees au comptoir : sa productivite, affichee au bureau.</summary>
        public int Livrees => _livrees;
        /// <summary>Fatigue actuelle, de 0 a Reglages.FatigueMax.</summary>
        public float FatigueActuelle => _fatigue;
        /// <summary>Son rendement du moment, tel qu'il ralentit ses gestes.</summary>
        public float Productivite => Fatigue.Productivite(_fatigue);
        /// <summary>Vrai pendant qu'il marche vers la salle de repos ou s'y repose.</summary>
        public bool SeRepose => _etat == Etat.VersRepos || _etat == Etat.SeRepose
                                                || _etat == Etat.SortDuRepos;
        /// <summary>
        /// Gele la fatigue, comme Horloge.Figee gele l'heure : sert au banc
        /// d'essai, qui doit pouvoir eprouver le reste du service sans que
        /// des heures de travail simulees envoient le caissier en pause au
        /// milieu d'une verification qui n'a rien a voir.
        /// </summary>
        public bool FatigueGelee;
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

        /// <summary>
        /// Vrai une fois embauche, et pour de bon : il rentre chez lui chaque
        /// soir, ce qui eteint son objet, mais il reste de la maison.
        /// </summary>
        public bool Embauche { get; private set; }

        GameObject _sac;
        /// <summary>Vrai quand il a mis son sac a dos pour rentrer.</summary>
        public bool PorteSonSac => _sac != null;
        /// <summary>Vrai pendant qu'il quitte la pizzeria.</summary>
        public bool SEnVa => _etat == Etat.SEnVa;

        void OnEnable()
        {
            Embauche = true;
            if (Comptoir != null) Comptoir.CaissierPresent = true;
        }

        void OnDisable()
        {
            if (Comptoir != null) Comptoir.CaissierPresent = false;
        }

        /// <summary>Il revient prendre son service : sac range, retour au poste.</summary>
        public void Reprendre()
        {
            gameObject.SetActive(true);
            // Dit sans detour plutot que par OnEnable : la caisse doit etre
            // tenue des cette image, pas a la suivante.
            if (Comptoir != null) Comptoir.CaissierPresent = true;
            transform.position = Poste;
            _etat = Etat.Poste;
            _passeParRelais = false;
            _etapeRepos = 0;
            _fatigue = 0f;
            RangerLeSac();
        }

        /// <summary>
        /// Renvoye depuis le bureau : il pose tout ce qu'il porte et s'eteint
        /// pour de bon. Sans repasser par <see cref="Embauche"/> a faux, le
        /// comptoir le rappellerait le lendemain matin comme s'il rentrait
        /// simplement se coucher.
        /// </summary>
        public void Licencier()
        {
            RangerLeSac();
            if (_portee != null) _portee.Vider();
            if (Comptoir != null) Comptoir.CaissierPresent = false;
            // Le siege reserve ne doit pas rester bloque pour quelqu'un que
            // plus personne ne viendra liberer.
            if (SalleRepos != null && _siegeRepos >= 0) SalleRepos.Liberer(_siegeRepos);
            _siegeRepos = -1;
            _etapeRepos = 0;
            _fatigue = 0f;
            Embauche = false;
            _etat = Etat.Poste;
            _passeParRelais = false;
            gameObject.SetActive(false);
        }

        void PrendreLeSac()
        {
            if (_sac != null) return;
            _sac = new GameObject("SacADos");
            _sac.transform.SetParent(transform, false);
            _sac.transform.localPosition = new Vector3(0f, 0.95f, -0.30f);
            var toile = Bloc.Couleur(0x2E6B4F);
            var sangle = Bloc.Couleur(0x1F4A36);
            Bloc.Galet("Poche", _sac.transform, Vector3.zero,
                       new Vector3(0.46f, 0.56f, 0.28f), toile).SansCollision();
            Bloc.Boite("Rabat", _sac.transform, new Vector3(0f, 0.16f, -0.02f),
                       new Vector3(0.42f, 0.18f, 0.26f), sangle).SansCollision();
            foreach (float x in new[] { -0.14f, 0.14f })
                Bloc.Boite("Bretelle", _sac.transform, new Vector3(x, 0.02f, 0.20f),
                           new Vector3(0.07f, 0.52f, 0.06f), sangle).SansCollision();
        }

        void RangerLeSac()
        {
            if (_sac == null) return;
            Destroy(_sac);
            _sac = null;
        }

        void Awake()
        {
            _demarche = GetComponent<Demarche>();
            _portee = Portage.Creer(transform, "PilePortee");
            _portee.Max = Reglages.CapacitePorteeCaissier;
        }

        void Update()
        {
            // Fatigue, il enchaine les gestes plus lentement : le compte a
            // rebours avance moins vite, pas la duree du geste elle-meme.
            if (_compteurTransfert > 0f) _compteurTransfert -= Time.deltaTime * Productivite;
            if (_demarche != null) _demarche.BrasPortent = !_portee.EstVide || _ordures != null;

            MettreAJourFatigue();

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
                case Etat.VersRepos:    AllerAuRepos(); break;
                case Etat.SeRepose:     SeReposer(); break;
                case Etat.SortDuRepos:  QuitterLeRepos(); break;
                case Etat.SEnVa:        Rentrer(); break;
            }
        }

        /// <summary>
        /// La fatigue monte tant qu'il travaille pour de bon, et redescend
        /// pendant la pause, plus vite avec une salle de repos amelioree.
        /// Ni en sortant le soir, ni assis a rien faire au poste.
        /// </summary>
        void MettreAJourFatigue()
        {
            if (FatigueGelee) return;
            if (_etat == Etat.SeRepose)
            {
                float recuperation = Reglages.RecuperationParSeconde * Comptabilite.BonusRecuperation;
                _fatigue = Mathf.Max(0f, _fatigue - recuperation * Time.deltaTime);
            }
            else if (_etat != Etat.SEnVa)
            {
                _fatigue = Mathf.Min(Reglages.FatigueMax, _fatigue + Reglages.FatigueParSeconde * Time.deltaTime);
            }
        }

        // ------------------------------------------------------------------

        /// <summary>
        /// L'heure de rentrer. Il pose ce qu'il porte, met son sac et sort par
        /// la meme porte que les clients ; son objet s'eteint une fois dehors,
        /// et le comptoir le rappellera demain a l'ouverture.
        /// </summary>
        void Rentrer()
        {
            PrendreLeSac();
            var sortie = Comptoir != null ? Comptoir.PointDeSortie
                                          : transform.position + Vector3.back * 12f;
            // Le relais ne sert qu'a contourner le comptoir vers le four : en
            // sortant, il prend le chemin des clients.
            _passeParRelais = false;
            if (!Avancer(sortie)) return;
            if (Comptoir != null) Comptoir.CaissierPresent = false;
            gameObject.SetActive(false);
        }

        /// <summary>Vrai des que sa journee est finie.</summary>
        bool FinDeService => Horloge.Active != null
                          && Horloge.Active.Heures >= Reglages.HeureDepartCaissier;

        void Attendre()
        {
            // Sa journee est finie : il debarrasse ce qu'il a en main plus
            // tard, demain. Le service passe avant, pas apres.
            if (FinDeService) { Decharger(); _etat = Etat.SEnVa; return; }

            // Il pose d'abord ce qu'il porte : la fatigue attend la fin du
            // geste en cours, elle ne l'interrompt jamais en chemin.
            Decharger();
            if (!_portee.EstVide) return;

            // Trop fatigue, les mains enfin libres : il part se reposer avant
            // de repartir chercher du stock — sinon un comptoir qui manque
            // sans arret de pizzas le renvoyait au four a chaque image, et il
            // n'etait jamais assez « au repos » pour meme y songer. Sauf si
            // la salle de repos est complete, auquel cas il continue de
            // travailler en attendant une place.
            if (_fatigue >= Reglages.SeuilDepartRepos && SalleRepos != null)
            {
                int siege = SalleRepos.Reserver();
                if (siege >= 0)
                {
                    _siegeRepos = siege;
                    _etat = Etat.VersRepos;
                    _etapeRepos = 0;
                    _passeParRelais = false;
                    return;
                }
            }

            if (Four == null || Comptoir == null || Table == null) return;

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
        /// verrait jamais le carton se poser sur le plan.
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

            // 2. Pizza en main : de quoi la recevoir sort directement sur le
            //    rond du plan. Un plateau si quelqu'un mange sur place et n'a
            //    pas le sien — sa pizza ne passe pas par le carton — un
            //    carton sinon.
            if (Table.Assemblage.Nombre <= _pleines)
            {
                // Un plateau part seul : le client de la salle attend debout,
                // et son plateau n'a rien a faire au milieu d'une fournee de
                // cartons.
                bool pourLaSalle = Comptoir != null && Comptoir.UnPlateauManque
                                && !_plateauEnCours && _pleines == 0;
                if (pourLaSalle)
                {
                    // Pas de plateau propre sous la main : il attend qu'on en
                    // rende un. L'emballer serait pire — le client de la salle
                    // repartirait avec un carton, ou ne serait jamais servi.
                    if (!Table.PrendrePlateau()) return;

                    Table.Assemblage.Ajouter(Pile.Forme.PlateauVide);
                    _plateauEnCours = true;
                    _compteurTransfert = Reglages.DelaiEmballage;
                    return;
                }
                if (Table.PrendreBoite())
                {
                    Table.Assemblage.Ajouter(true);
                    _compteurTransfert = Reglages.DelaiEmballage;
                }
                return;                              // reserve vide : il attend
            }

            // 3. La pizza entre dans la boite ouverte — ou se pose sur le
            //    plateau, qui n'etait qu'un plateau vide jusque-la.
            _portee.Retirer();
            if (Table.Assemblage.SommetForme == Pile.Forme.PlateauVide)
                Table.Assemblage.ChangerSommet(Pile.Forme.Plateau);
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
                _livrees++;
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

        Vector3 PositionDuRepos()
            => SalleRepos != null ? SalleRepos.PositionDuSiege(_siegeRepos) : Poste;

        Vector3[] CheminDuRepos()
            => SalleRepos != null ? SalleRepos.Chemin : null;

        /// <summary>
        /// Il remonte le chemin de la salle etape par etape — le plan
        /// d'emballage et le mur du fond sont sur la ligne droite — puis
        /// rejoint son siege.
        /// </summary>
        void AllerAuRepos()
        {
            var chemin = CheminDuRepos();
            if (chemin != null && _etapeRepos < chemin.Length)
            {
                if (Avancer(chemin[_etapeRepos])) _etapeRepos++;
                return;
            }
            if (Avancer(PositionDuRepos())) _etat = Etat.SeRepose;
        }

        /// <summary>
        /// Assis, il recupere jusqu'a redescendre sous le seuil, puis rend
        /// son siege et ressort — a pied, comme n'importe quel autre retour
        /// de mission.
        /// </summary>
        void SeReposer()
        {
            if (_fatigue > Reglages.SeuilFinRepos) return;
            if (SalleRepos != null) SalleRepos.Liberer(_siegeRepos);
            _siegeRepos = -1;
            _etat = Etat.SortDuRepos;
        }

        /// <summary>Le meme chemin, redescendu a l'envers jusqu'a la salle.</summary>
        void QuitterLeRepos()
        {
            var chemin = CheminDuRepos();
            if (chemin != null && _etapeRepos > 0)
            {
                if (Avancer(chemin[_etapeRepos - 1])) _etapeRepos--;
                return;
            }
            _etat = Etat.VersComptoir;
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

            var pas = delta.normalized * Reglages.VitesseCaissier * Productivite * Time.deltaTime;
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
