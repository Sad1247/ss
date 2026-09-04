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
            Hud.Indice = null;

            Qualite();

            var racine = new GameObject("Pizzeria");
            Decor(racine.transform);
            Lumiere(racine.transform);

            var joueur = Pizzaiolo(racine.transform);
            Camera(racine.transform, joueur.transform);

            Comptoir(racine.transform, joueur);
            Four(racine.transform, joueur, new Vector3(-4.2f, 0f, 3.2f), "Four1", true);
            var second = Four(racine.transform, joueur, new Vector3(-4.2f, 0f, -1.2f), "Four2", false);
            Zone(racine.transform, joueur, second);

            racine.AddComponent<Hud>();
            Debug.Log("Pizzeria : scene prete. Maintiens le clic et glisse pour te deplacer.");
        }

        // ------------------------------------------------------------------

        static void Decor(Transform parent)
        {
            Bloc.Boite("Herbe", parent, new Vector3(0f, -0.6f, 0f), new Vector3(34f, 0.4f, 34f), Bloc.Herbe)
                .SansCollision();
            Bloc.Boite("Sol", parent, new Vector3(0f, -0.2f, 0f), new Vector3(17f, 0.4f, 15f), Bloc.Sol)
                .SansCollision();
            Bloc.Boite("Trottoir", parent, new Vector3(0f, -0.35f, -8.8f), new Vector3(19f, 0.5f, 3f),
                       Bloc.SolBordure).SansCollision();

            // batiment du fond, purement decoratif
            Bloc.Boite("Mur", parent, new Vector3(-1f, 1.6f, 7.2f), new Vector3(14f, 3.2f, 0.6f),
                       Bloc.MachineBis).SansCollision();
            for (int i = 0; i < 4; i++)
                Bloc.Boite("Vitre" + i, parent, new Vector3(-5.4f + i * 2.9f, 1.9f, 6.85f),
                           new Vector3(1.8f, 1.5f, 0.15f), Bloc.Couleur(0xBFE8F2)).SansCollision();

            // De quoi remplir la cour : sans ces caisses et ces arbustes, le sol
            // parait vide et la scene ne ressemble a rien.
            for (int i = 0; i < 3; i++)
                Bloc.Boite("Caisse" + i, parent, new Vector3(6.6f, 0.45f + i * 0.9f, 5.4f - i * 0.15f),
                           new Vector3(1.5f, 0.9f, 1.5f), Bloc.Carton).SansCollision();
            Bloc.Boite("Palette", parent, new Vector3(-7f, 0.2f, 5.6f), new Vector3(2.2f, 0.4f, 2.2f),
                       Bloc.Metal).SansCollision();

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
            go.transform.position = new Vector3(0f, 0f, 0f);

            var corps = new GameObject("Corps");
            corps.transform.SetParent(go.transform, false);
            var t = corps.transform;

            // Jambes separees plutot qu'un bloc : c'est ce qui donne la
            // silhouette du personnage, meme immobile.
            Bloc.Capsule("JambeG", t, new Vector3(-0.15f, 0.42f, 0f),
                         new Vector3(0.26f, 0.30f, 0.26f), Bloc.Pantalon).SansCollision();
            Bloc.Capsule("JambeD", t, new Vector3(0.15f, 0.42f, 0f),
                         new Vector3(0.26f, 0.30f, 0.26f), Bloc.Pantalon).SansCollision();
            Bloc.Galet("PiedG", t, new Vector3(-0.15f, 0.09f, 0.06f),
                       new Vector3(0.28f, 0.18f, 0.40f), Bloc.Pantalon).SansCollision();
            Bloc.Galet("PiedD", t, new Vector3(0.15f, 0.09f, 0.06f),
                       new Vector3(0.28f, 0.18f, 0.40f), Bloc.Pantalon).SansCollision();

            // T-shirt : un galet large et court, manches courtes marquees
            Bloc.Galet("Torse", t, new Vector3(0f, 0.95f, 0f),
                       new Vector3(0.62f, 0.62f, 0.46f), Bloc.Tablier).SansCollision();
            Bloc.Galet("MancheG", t, new Vector3(-0.28f, 1.02f, 0f),
                       new Vector3(0.26f, 0.30f, 0.28f), Bloc.Tablier).SansCollision();
            Bloc.Galet("MancheD", t, new Vector3(0.28f, 1.02f, 0f),
                       new Vector3(0.26f, 0.30f, 0.28f), Bloc.Tablier).SansCollision();

            // Bras nus, legerement ecartes du corps
            Bloc.Capsule("BrasG", t, new Vector3(-0.32f, 0.76f, 0f),
                         new Vector3(0.19f, 0.20f, 0.19f), Bloc.Peau).SansCollision();
            Bloc.Capsule("BrasD", t, new Vector3(0.32f, 0.76f, 0f),
                         new Vector3(0.19f, 0.20f, 0.19f), Bloc.Peau).SansCollision();

            Bloc.Bille("Tete", t, new Vector3(0f, 1.42f, 0f), 0.52f, Bloc.Peau).SansCollision();

            // Casquette : une calotte bombee qui coiffe le crane, plus large que
            // lui, et une visiere inclinee vers l'avant.
            Bloc.Galet("Calotte", t, new Vector3(0f, 1.52f, -0.01f),
                       new Vector3(0.60f, 0.44f, 0.60f), Bloc.Casquette).SansCollision();
            Bloc.Galet("Visiere", t, new Vector3(0f, 1.46f, 0.26f),
                       new Vector3(0.46f, 0.10f, 0.34f), Bloc.Casquette, -12f).SansCollision();

            return go.AddComponent<Joueur>();
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
            Bloc.Boite("Caisse", go.transform, new Vector3(-1.3f, 1.45f, 0f), new Vector3(0.7f, 0.5f, 0.6f),
                       Bloc.Pantalon).SansCollision();

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

            // pierre du four : le plan ou les pizzas sortent
            Bloc.Boite("PierreDuFour", t, new Vector3(0f, 1.0f, -1.62f), new Vector3(2.5f, 0.16f, 1.3f),
                       Bloc.Pierre).SansCollision();

            var pile = new GameObject("Sortie");
            pile.transform.SetParent(t, false);
            pile.transform.localPosition = new Vector3(0f, 1.09f, -1.72f);

            var f = go.AddComponent<Four>();
            f.Sortie = pile.AddComponent<Pile>();
            f.Joueur = joueur;

            go.SetActive(actif);
            return go;
        }

        static void Zone(Transform parent, Joueur joueur, GameObject achat)
        {
            var go = new GameObject("ZoneAchat");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-1.2f, 0f, -3.2f);

            Bloc.Boite("Dalle", go.transform, new Vector3(0f, 0.03f, 0f), new Vector3(2.6f, 0.06f, 2.6f),
                       Bloc.Zone).SansCollision();
            Bloc.Boite("Cadre", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(2.9f, 0.04f, 2.9f),
                       Color.white).SansCollision();

            var z = go.AddComponent<ZoneAchat>();
            z.Joueur = joueur;
            z.Achat = achat;
            z.Prix = Reglages.PrixSecondFour;
            z.Libelle = "Nouveau four";
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
