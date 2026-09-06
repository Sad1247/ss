using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Monte toute la scene au lancement : sol, decor, four, comptoir, zone
    /// d'achat, joueur, camera isometrique et lumiere. Rien a assembler dans
    /// l'editeur — ouvre une scene vide et appuie sur Play.
    /// </summary>
    public static class Batisseur
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Monter()
        {
            if (Object.FindObjectOfType<Joueur>() != null) return;   // deja monte a la main

            // Trace de demarrage : si ce message n'apparait pas dans la Console
            // au lancement, c'est que les scripts ne sont pas dans le projet.
            Debug.Log("Pizzeria : construction de la scene...");

            Banque.Reinitialiser();

            Qualite();
            Obstacles.Reinitialiser();

            var racine = new GameObject("Pizzeria");
            Decor(racine.transform);
            Lumiere(racine.transform);

            var joueur = Pizzaiolo(racine.transform);
            Camera(racine.transform, joueur.transform);

            var comptoir = Comptoir(racine.transform, joueur);
            var four = Four(racine.transform, joueur, new Vector3(-4.2f, 0f, 3.2f), "Four1", true);

            // Le plan de mise en boite, adosse aux fenetres du fond, derriere
            // la caisse : le caissier y passe entre le four et le comptoir.
            var table = Table(racine.transform, new Vector3(1.6f, 0f, 6.0f));

            // L'embauche se paie juste derriere le tiroir-caisse, a portee du
            // joueur qui longe le comptoir.
            var caissier = Caissier(racine.transform, comptoir, four.GetComponent<Four>(),
                                    table, new Vector3(3.6f, 0f, 2.05f));
            Zone(racine.transform, joueur, caissier, new Vector3(2.5f, 0f, 3.0f),
                 Reglages.PrixCaissier, "Embaucher un caissier");

            racine.AddComponent<Hud>();
            racine.AddComponent<Manette>();
            Debug.Log("Pizzeria : scene prete. Maintiens le clic et glisse pour te deplacer.");
        }

        // ------------------------------------------------------------------

        static void Decor(Transform parent)
        {
            Bloc.Boite("Herbe", parent, new Vector3(0f, -0.6f, 0f), new Vector3(34f, 0.4f, 34f), Bloc.Herbe)
                .SansCollision();
            Bloc.Boite("Sol", parent, new Vector3(0f, -0.2f, 0f), new Vector3(17f, 0.4f, 15f), Bloc.Sol)
                .SansCollision();
            // le joueur reste sur le dallage, une marge le tenant loin du bord
            Obstacles.DefinirTerrain(8.5f - 0.6f, 7.5f - 0.6f);
            Bloc.Boite("Trottoir", parent, new Vector3(0f, -0.35f, -8.8f), new Vector3(19f, 0.5f, 3f),
                       Bloc.SolBordure).SansCollision();

            // Le batiment ferme deux cotes : le fond et la gauche de l'ecran.
            // Un seul pan donnait l'impression d'un decor pose sur un terrain vague.
            // Comme le mur de gauche, il va d'un bord a l'autre du dallage.
            Bloc.Boite("MurFond", parent, new Vector3(0f, 1.6f, 7.2f), new Vector3(17f, 3.2f, 0.6f),
                       Bloc.MachineBis).SansCollision();
            Obstacles.Ajouter(new Vector3(0f, 0f, 7.2f), 17f, 0.6f);
            for (int i = 0; i < 6; i++)
                Bloc.Boite("VitreFond" + i, parent, new Vector3(-7.6f + i * 3.04f, 1.9f, 6.85f),
                           new Vector3(1.8f, 1.5f, 0.15f), Bloc.Couleur(0xBFE8F2)).SansCollision();

            // Le mur va d'un bout a l'autre du dallage : il s'arretait 1,2 avant
            // le bord, et la pizzeria semblait ouverte sur le vide.
            Bloc.Boite("MurGauche", parent, new Vector3(-8.0f, 1.6f, 0f), new Vector3(0.6f, 3.2f, 15f),
                       Bloc.MachineBis).SansCollision();
            Obstacles.Ajouter(new Vector3(-8.0f, 0f, 0f), 0.6f, 15f);
            for (int i = 0; i < 5; i++)
                Bloc.Boite("VitreGauche" + i, parent, new Vector3(-7.65f, 1.9f, -6.4f + i * 2.9f),
                           new Vector3(0.15f, 1.5f, 1.8f), Bloc.Couleur(0xBFE8F2)).SansCollision();

            // De quoi remplir la cour sans encombrer le passage : la pile de
            // cartons qui se dressait a droite masquait le mur du fond.
            Bloc.Boite("Palette", parent, new Vector3(-7f, 0.2f, 5.6f), new Vector3(2.2f, 0.4f, 2.2f),
                       Bloc.Metal).SansCollision();
            Obstacles.Ajouter(new Vector3(-7f, 0f, 5.6f), 2.2f, 2.2f);

            float[] xs = { -12f, -9.5f, 11.5f, 13f, -13.5f };
            float[] zs = { 9.5f, -6.5f, 8f, -3.5f, 2f };
            for (int i = 0; i < xs.Length; i++)
            {
                Bloc.Boite("Tronc" + i, parent, new Vector3(xs[i], 0.6f, zs[i]),
                           new Vector3(0.4f, 1.2f, 0.4f), Bloc.Couleur(0x8A5A2B)).SansCollision();
                Bloc.Bille("Feuillage" + i, parent, new Vector3(xs[i], 1.8f, zs[i]), 2.2f,
                           Bloc.Couleur(0x4FA83A)).SansCollision();
            }
        }

        /// <summary>
        /// Le style repose sur des couleurs franches et des ombres douces. Sans
        /// ces reglages, l'eclairage par defaut delave tout et les bords crenent.
        /// </summary>
        static void Qualite()
        {
            QualitySettings.antiAliasing = 4;              // bords nets
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;

            // Lumiere ambiante chaude et uniforme : elle remonte les faces a
            // l'ombre sans grisailler les couleurs, comme dans la reference.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.74f, 0.78f);
        }

        static void Lumiere(Transform parent)
        {
            // Une scene neuve contient deja une lumiere directionnelle : en
            // ajouter une seconde surexpose tout. On reprend celle qui existe.
            var l = Object.FindObjectOfType<Light>();
            if (l == null)
            {
                var go = new GameObject("Soleil");
                go.transform.SetParent(parent, false);
                l = go.AddComponent<Light>();
            }
            l.type = LightType.Directional;
            l.color = Bloc.Couleur(0xFFF6E2);              // soleil legerement chaud
            l.intensity = 1.35f;
            l.shadows = LightShadows.Soft;                 // les ombres portees font le relief
            l.shadowStrength = 0.45f;
            l.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        }

        static void Camera(Transform parent, Transform cible)
        {
            // Meme chose pour la camera : une scene neuve a deja sa Main Camera.
            // En creer une deuxieme donne une image superposee et illisible.
            var cam = Object.FindObjectOfType<UnityEngine.Camera>();
            GameObject go;
            if (cam == null)
            {
                go = new GameObject("Camera");
                go.transform.SetParent(parent, false);
                cam = go.AddComponent<UnityEngine.Camera>();
            }
            else go = cam.gameObject;

            cam.orthographic = true;                 // le genre est toujours en vue isometrique
            cam.orthographicSize = 7.6f;                  // assez pres pour que l'action remplisse l'ecran
            cam.backgroundColor = Bloc.Couleur(0x9BDCF0);
            go.transform.rotation = Quaternion.Euler(38f, -45f, 0f);
            go.transform.position = -(go.transform.rotation * Vector3.forward) * 30f;

            var suivi = go.GetComponent<Suivi>();
            if (suivi == null) suivi = go.AddComponent<Suivi>();
            suivi.Cible = cible;
            suivi.Decalage = go.transform.position;
        }

        static Joueur Pizzaiolo(Transform parent)
        {
            var go = new GameObject("Joueur");
            go.transform.SetParent(parent, false);
            go.transform.position = Vector3.zero;
            var membres = Personnage.Construire(go.transform, Bloc.Tablier, Bloc.Casquette);
            Marche(go, membres, Reglages.VitesseJoueur);
            return go.AddComponent<Joueur>();
        }

        /// <summary>Donne sa demarche a un personnage : le meme pas pour tous.</summary>
        static void Marche(GameObject go, Personnage.Membres m, float vitesseMax)
        {
            var d = go.AddComponent<Demarche>();
            d.VitesseReference = vitesseMax;   // pas ample a sa propre allure
            d.Corps = m.Corps;
            d.HancheG = m.HancheG; d.HancheD = m.HancheD;
            d.GenouG = m.GenouG;   d.GenouD = m.GenouD;
            d.EpauleG = m.EpauleG; d.EpauleD = m.EpauleD;
        }

        /// <summary>L'employe de caisse, cache tant qu'il n'est pas embauche.</summary>
        static GameObject Caissier(Transform parent, Comptoir comptoir, Four four,
                                   Emballage table, Vector3 position)
        {
            var go = new GameObject("Caissier");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var membres = Personnage.Construire(go.transform, Bloc.Couleur(0x7BC86B),
                                               Bloc.Couleur(0xF4F1EA));
            Marche(go, membres, Reglages.VitesseCaissier);

            var c = go.AddComponent<Caissier>();
            c.Comptoir = comptoir;
            c.Four = four;
            c.Table = table;
            c.Poste = position;
            // en ligne droite, il traverserait le comptoir
            c.Relais = new Vector3(0.5f, 0f, 3.2f);

            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// Le plan de mise en boite. Il reprend la facture du comptoir —
        /// caisson bleu, dessus metallique qui deborde — pour que les deux
        /// meubles de la pizzeria se ressemblent au lieu de jurer.
        /// </summary>
        static Emballage Table(Transform parent, Vector3 position)
        {
            var go = new GameObject("TableEmballage");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var t = go.transform;

            // memes cotes que le caisson du comptoir : les deux meubles se
            // repondent au lieu de sembler pris dans deux jeux differents
            Bloc.Boite("Plan", t, new Vector3(0f, 0.55f, 0f), new Vector3(3.6f, 1.1f, 1.2f),
                       Bloc.Machine).SansCollision();
            Bloc.Boite("Dessus", t, new Vector3(0f, 1.15f, 0f), new Vector3(3.8f, 0.16f, 1.4f),
                       Bloc.Metal).SansCollision();
            Bloc.Boite("Bandeau", t, new Vector3(0f, 0.20f, -0.61f), new Vector3(3.6f, 0.30f, 0.04f),
                       Bloc.MachineBis).SansCollision();

            Obstacles.Ajouter(position, 3.8f, 1.4f);

            // Un seul tas de cartons, dans le coin arriere gauche du plan :
            // etales cote a cote, ils occupaient tout le meuble.
            var rangee = new GameObject("Boites");
            rangee.transform.SetParent(t, false);
            rangee.transform.localPosition = new Vector3(-1.58f, 1.23f, 0.26f);

            var poste = new GameObject("Poste");
            poste.transform.SetParent(t, false);
            poste.transform.localPosition = new Vector3(0f, 0f, -1.40f);

            var e = go.AddComponent<Emballage>();
            e.Boites = rangee.AddComponent<Pile>();
            e.Poste = poste.transform;
            e.Garnir();
            return e;
        }

        static Comptoir Comptoir(Transform parent, Joueur joueur)
        {
            var go = new GameObject("Comptoir");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(3.6f, 0f, 0.6f);

            Bloc.Boite("Plan", go.transform, new Vector3(0f, 0.55f, 0f), new Vector3(3.6f, 1.1f, 1.8f),
                       Bloc.Machine).SansCollision();
            Bloc.Boite("Dessus", go.transform, new Vector3(0f, 1.15f, 0f), new Vector3(3.8f, 0.16f, 2f),
                       Bloc.Metal).SansCollision();
            var caisse = Caisse.Creer(go.transform, new Vector3(-1.05f, 1.15f, 0f));

            Obstacles.Ajouter(go.transform.position, 3.9f, 2.1f);

            var pile = new GameObject("Stock");
            pile.transform.SetParent(go.transform, false);
            pile.transform.localPosition = new Vector3(0.6f, 1.25f, 0f);

            var file = new GameObject("PointFile");
            file.transform.SetParent(go.transform, false);
            file.transform.localPosition = new Vector3(0f, 0f, -2.2f);

            var sortie = new GameObject("Sortie");
            sortie.transform.SetParent(go.transform, false);
            sortie.transform.localPosition = new Vector3(9f, 0f, -6f);

            var c = go.AddComponent<Comptoir>();
            c.Stock = pile.AddComponent<Pile>();
            c.Joueur = joueur;
            c.PointFile = file.transform;
            c.Sortie = sortie.transform;
            c.Caisse = caisse;
            return c;
        }

        /// <summary>
        /// Four a bois traditionnel : socle en briques, coupole, arche de pierre
        /// claire, foyer allume et cheminee. La pierre du four, devant, sert de
        /// plan de sortie : c'est la que les pizzas s'empilent.
        /// </summary>
        static GameObject Four(Transform parent, Joueur joueur, Vector3 position, string nom, bool actif)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var t = go.transform;

            // socle
            Bloc.Boite("Socle", t, new Vector3(0f, 0.45f, 0f), new Vector3(2.7f, 0.9f, 2.3f),
                       Bloc.Brique).SansCollision();
            Bloc.Boite("Corniche", t, new Vector3(0f, 0.94f, 0f), new Vector3(2.9f, 0.14f, 2.5f),
                       Bloc.Pierre).SansCollision();
            Bloc.Bille("Aeration", t, new Vector3(0f, 0.45f, -1.17f), 0.22f, Bloc.Foyer).SansCollision();

            // coupole : une demi-sphere, sa moitie basse disparait dans le socle
            Bloc.Galet("Coupole", t, new Vector3(0f, 1.0f, 0.1f), new Vector3(2.5f, 2.3f, 2.3f),
                       Bloc.BriqueClaire).SansCollision();

            // bouche : arche claire, cerne sombre, foyer
            Bloc.Rondelle("Arche", t, new Vector3(0f, 1.5f, -1.02f), 1.75f, 0.24f, Bloc.Pierre)
                .SansCollision();
            Bloc.Rondelle("Cerne", t, new Vector3(0f, 1.47f, -1.10f), 1.35f, 0.18f, Bloc.PierreOmbre)
                .SansCollision();
            Bloc.Rondelle("Foyer", t, new Vector3(0f, 1.45f, -1.16f), 1.1f, 0.14f, Bloc.Foyer)
                .SansCollision();
            Bloc.Galet("Braises", t, new Vector3(0f, 1.2f, -1.22f), new Vector3(0.7f, 0.42f, 0.28f),
                       Bloc.Braise).SansCollision();
            Bloc.Galet("Flamme", t, new Vector3(0f, 1.42f, -1.24f), new Vector3(0.34f, 0.42f, 0.22f),
                       Bloc.Flamme).SansCollision();

            // cheminee
            Bloc.Forme(PrimitiveType.Cylinder, "Cheminee", t, new Vector3(0f, 2.35f, 0.5f),
                       new Vector3(0.5f, 0.5f, 0.5f), Bloc.Brique).SansCollision();
            Bloc.Disque("Couronne", t, new Vector3(0f, 2.86f, 0.5f), 0.66f, 0.22f, Bloc.Pierre)
                .SansCollision();

            var fumee = new GameObject("Fumee");
            fumee.transform.SetParent(t, false);
            fumee.transform.localPosition = new Vector3(0f, 3.0f, 0.5f);
            fumee.AddComponent<Fumee>();

            // pierre du four : le plan ou les pizzas sortent
            Bloc.Boite("PierreDuFour", t, new Vector3(0f, 1.0f, -1.62f), new Vector3(2.5f, 0.16f, 1.3f),
                       Bloc.Pierre).SansCollision();

            // le corps du four et sa pierre barrent le passage ; la pile de
            // sortie reste a portee du joueur arrete devant
            Obstacles.Ajouter(position, 2.8f, 2.4f);
            Obstacles.Ajouter(position + new Vector3(0f, 0f, -1.62f), 2.5f, 1.3f);

            var pile = new GameObject("Sortie");
            pile.transform.SetParent(t, false);
            pile.transform.localPosition = new Vector3(0f, 1.09f, -1.72f);

            var f = go.AddComponent<Four>();
            f.Sortie = pile.AddComponent<Pile>();
            f.Joueur = joueur;

            go.SetActive(actif);
            return go;
        }

        /// <summary>
        /// Une dalle a peine posee sur le sol : un carre trop epais ou trop
        /// large ressemble a un objet pose la, pas a un emplacement a acheter.
        /// </summary>
        static void Zone(Transform parent, Joueur joueur, GameObject achat, Vector3 position,
                         int prix, string libelle)
        {
            var go = new GameObject("ZoneAchat");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            Bloc.Boite("Cadre", go.transform, new Vector3(0f, 0.012f, 0f),
                       new Vector3(1.24f, 0.024f, 1.24f), Color.white).SansCollision();
            Bloc.Boite("Dalle", go.transform, new Vector3(0f, 0.018f, 0f),
                       new Vector3(1.10f, 0.024f, 1.10f), Bloc.Zone).SansCollision();

            var z = go.AddComponent<ZoneAchat>();
            z.Joueur = joueur;
            z.Achat = achat;
            z.Prix = prix;
            z.Libelle = libelle;
        }
    }

    /// <summary>La camera suit le joueur sans jamais tourner : vue isometrique fixe.</summary>
    public sealed class Suivi : MonoBehaviour
    {
        public Transform Cible;
        public Vector3 Decalage;

        void LateUpdate()
        {
            if (Cible == null) return;
            var voulu = Cible.position + Decalage;
            transform.position = Vector3.Lerp(transform.position, voulu, 6f * Time.deltaTime);
        }
    }
}
