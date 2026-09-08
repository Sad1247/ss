using System.Collections.Generic;
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

            // Le joueur est monte avant le decor : la petite piece a besoin de
            // lui pour savoir quand escamoter ses murs.
            var joueur = Pizzaiolo(racine.transform);
            _joueur = joueur;

            Decor(racine.transform);
            Lumiere(racine.transform);
            Camera(racine.transform, joueur.transform);

            var comptoir = Comptoir(racine.transform, joueur);
            // adosse au mur du fond : la salle se degage devant lui
            var four = Four(racine.transform, joueur, new Vector3(-4.2f, 0f, 5.88f), "Four1", true);

            // Le plan de mise en boite, adosse aux fenetres du fond, derriere
            // la caisse : le caissier y passe entre le four et le comptoir.
            var table = Table(racine.transform, new Vector3(1.3f, 0f, 6.0f));

            // L'embauche se paie juste derriere le tiroir-caisse, a portee du
            // joueur qui longe le comptoir.
            var caissier = Caissier(racine.transform, comptoir, four.GetComponent<Four>(),
                                    table, new Vector3(3.6f, 0f, 2.05f));
            comptoir.Employe = caissier.GetComponent<Caissier>();
            Zone(racine.transform, joueur, caissier, new Vector3(2.5f, 0f, 3.0f),
                 Reglages.PrixCaissier, "Embaucher un caissier");
            Zone(racine.transform, joueur, _piece, _pieceEtSaDalle,
                 Reglages.PrixPetitePiece, "Ouvrir la petite piece");

            // La table de la salle : les clients servis viennent y manger.
            var tableSalle = Salle(racine.transform, new Vector3(-2.6f, 0f, -3.2f));
            comptoir.Table = tableSalle;

            // La poubelle, ou le caissier vide les restes des repas.
            // Loin de l'endroit ou passait la pile de cartons : le joueur doit
            // pouvoir traverser la cour sans buter dessus.
            var poubelle = new Vector3(7.6f, 0f, 2.4f);
            Poubelle(racine.transform, poubelle);

            var employe = caissier.GetComponent<Caissier>();
            employe.Salle = tableSalle;
            employe.Poubelle = poubelle + new Vector3(-1.0f, 0f, 0f);   // il s'arrete devant

            // Le registre du personnel, et l'ecran du bureau qui le lit. Le
            // caissier n'y figure en poste qu'une fois embauche.
            Personnel.Vider();
            Personnel.Inscrire("Vous", "Pizzaiolo", Reglages.SalairePatron);
            // Embauche, et non presence : il rentre chez lui chaque soir, il
            // n'en est pas moins de la maison.
            var employeur = caissier.GetComponent<Caissier>();
            Personnel.Inscrire("Caissier", "Comptoir", Reglages.SalaireCaissier,
                               () => employeur != null && employeur.Embauche);

            // L'horloge avant le Hud : celui-ci lit l'heure des sa premiere image.
            racine.AddComponent<Horloge>();
            racine.AddComponent<Acceleration>();
            racine.AddComponent<Hud>();
            var ecran = racine.AddComponent<EcranBureau>();
            ecran.Joueur = joueur;
            // Il n'apparait qu'une fois le capot leve : l'ordinateur est
            // ferme tant que personne n'est assis.
            ecran.Portable = _portable;
            racine.AddComponent<Manette>();
            Debug.Log("Pizzeria : scene prete. Maintiens le clic et glisse pour te deplacer.");
        }

        // ------------------------------------------------------------------

        // La piece du fond et l'emplacement de sa dalle, poses pendant le decor
        // et repris au moment d'installer les zones d'achat.
        static GameObject _piece;
        static OrdinateurPortable _portable;
        static Vector3 _pieceEtSaDalle;
        static Joueur _joueur;

        /// <summary>Une ouverture dans un mur : porte ou baie vitree.</summary>
        struct Baie
        {
            public float Centre, Largeur, Bas, Haut;
            public Baie(float centre, float largeur, float bas, float haut)
            {
                Centre = centre; Largeur = largeur; Bas = bas; Haut = haut;
            }
        }

        /// <summary>
        /// Monte un mur perce d'ouvertures : des trumeaux entre les baies, une
        /// allege sous chacune et un linteau au-dessus. Un mur plein derriere
        /// une vitre ne laisse rien voir — c'est le trou qui fait la fenetre,
        /// pas le verre.
        /// </summary>
        static List<GameObject> MurPerce(string nom, Transform parent, bool selonX, float fixe,
                                         float debut, float fin, float hauteur, float epaisseur,
                                         Color couleur, List<Baie> baies)
        {
            var pans = new List<GameObject>();
            float curseur = debut;
            int n = 0;

            foreach (var baie in baies)
            {
                float g = baie.Centre - baie.Largeur * 0.5f;
                float d = baie.Centre + baie.Largeur * 0.5f;

                if (g - curseur > 0.01f)
                    pans.Add(Pan(nom + n++, parent, selonX, fixe, curseur, g, 0f, hauteur,
                                 epaisseur, couleur));
                if (baie.Bas > 0.01f)
                    pans.Add(Pan(nom + "Allege" + n, parent, selonX, fixe, g, d, 0f, baie.Bas,
                                 epaisseur, couleur));
                if (hauteur - baie.Haut > 0.01f)
                    pans.Add(Pan(nom + "Linteau" + n, parent, selonX, fixe, g, d, baie.Haut, hauteur,
                                 epaisseur, couleur));
                curseur = d;
            }
            if (fin - curseur > 0.01f)
                pans.Add(Pan(nom + n, parent, selonX, fixe, curseur, fin, 0f, hauteur,
                             epaisseur, couleur));
            return pans;
        }

        /// <summary>Un morceau de mur, entre deux abscisses et deux hauteurs.</summary>
        static GameObject Pan(string nom, Transform parent, bool selonX, float fixe,
                              float a, float b, float bas, float haut, float epaisseur, Color couleur)
        {
            var centre = selonX
                ? new Vector3((a + b) * 0.5f, (bas + haut) * 0.5f, fixe)
                : new Vector3(fixe, (bas + haut) * 0.5f, (a + b) * 0.5f);
            var taille = selonX
                ? new Vector3(b - a, haut - bas, epaisseur)
                : new Vector3(epaisseur, haut - bas, b - a);
            return Bloc.Boite(nom, parent, centre, taille, couleur).SansCollision();
        }

        /// <summary>Un objet du decor deja pose, par son nom.</summary>
        static GameObject Trouver(Transform parent, string nom)
        {
            var t = parent.Find(nom);
            return t != null ? t.gameObject : null;
        }

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
            // Un seul pan donnait l'impression d'un decor pose sur un terrain
            // vague. Les murs sont perces : sans trou derriere elles, les
            // vitres n'auraient rien laisse voir.
            const float xPorte = 4.56f, largeurPorte = 1.70f, hauteurPorte = 2.38f;
            const float basBaie = 1.15f, hautBaie = 2.65f, largeurBaie = 1.8f;
            float finGauche = xPorte - largeurPorte * 0.5f;
            float debutDroite = xPorte + largeurPorte * 0.5f;

            var baiesFond = new List<Baie>();
            for (int i = 0; i < 6; i++)
            {
                if (i == 4) continue;              // sa place revient a la porte
                baiesFond.Add(new Baie(-7.6f + i * 3.04f, largeurBaie, basBaie, hautBaie));
                Bloc.Verre("VitreFond" + i, parent, new Vector3(-7.6f + i * 3.04f, 1.9f, 6.85f),
                           new Vector3(largeurBaie, hautBaie - basBaie, 0.15f),
                           Bloc.Couleur(0xBFE8F2)).SansCollision();
            }
            baiesFond.Add(new Baie(xPorte, largeurPorte, 0f, hauteurPorte));
            baiesFond.Sort((a, b) => a.Centre.CompareTo(b.Centre));

            var pansFond = MurPerce("MurFond", parent, true, 7.2f, -8.5f, 8.5f, 3.2f, 0.6f,
                                    Bloc.MachineBis, baiesFond);

            // Ceux qui se dressent entre la camera et la piece : a droite de la
            // porte, plus le linteau au-dessus d'elle.
            var cachentLaPiece = new List<GameObject>();
            foreach (var pan in pansFond)
                if (pan.transform.localPosition.x > xPorte - 0.01f) cachentLaPiece.Add(pan);
            cachentLaPiece.Add(Trouver(parent, "VitreFond5"));

            // Les deux pans du mur, de part et d'autre de la porte, barrent le
            // passage ; le pas de la porte, lui, a son obstacle a part : c'est
            // celui-la que l'achat de la piece leve.
            Obstacles.Ajouter(new Vector3((-8.5f + finGauche) * 0.5f, 0f, 7.2f),
                              finGauche + 8.5f, 0.6f);
            Obstacles.Ajouter(new Vector3((debutDroite + 8.5f) * 0.5f, 0f, 7.2f),
                              8.5f - debutDroite, 0.6f);
            int seuil = Obstacles.Ajouter(new Vector3(xPorte, 0f, 7.2f), largeurPorte, 0.6f);

            // juste a cote du plan de mise en boite, qui s'arrete a x 3,5
            var gond = Porte(parent, new Vector3(xPorte, 0f, 7.2f));

            // La piece derriere, et la dalle qui l'ouvre, devant la porte.
            // Elle occupe tout le coin du batiment : son pan droit rejoint le
            // bout du mur du fond, sinon ce dernier depassait dans le vide.
            var piece = Piece(parent, new Vector3(5.35f, 0f, 9.7f), gond, seuil, _joueur,
                              cachentLaPiece);
            _pieceEtSaDalle = new Vector3(xPorte, 0f, 5.6f);
            _piece = piece;

            // Le mur va d'un bout a l'autre du dallage : il s'arretait 1,2 avant
            // le bord, et la pizzeria semblait ouverte sur le vide.
            var baiesGauche = new List<Baie>();
            for (int i = 0; i < 5; i++)
            {
                baiesGauche.Add(new Baie(-6.4f + i * 2.9f, largeurBaie, basBaie, hautBaie));
                Bloc.Verre("VitreGauche" + i, parent, new Vector3(-7.65f, 1.9f, -6.4f + i * 2.9f),
                           new Vector3(0.15f, hautBaie - basBaie, largeurBaie),
                           Bloc.Couleur(0xBFE8F2)).SansCollision();
            }
            MurPerce("MurGauche", parent, false, -8.0f, -7.5f, 7.5f, 3.2f, 0.6f,
                     Bloc.MachineBis, baiesGauche);
            Obstacles.Ajouter(new Vector3(-8.0f, 0f, 0f), 0.6f, 15f);

            // La cour reste degagee : la palette et la pile de cartons qui s'y
            // trouvaient encombraient le sol sans rien apporter.

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
        /// La porte vitree du mur du fond, a cote du plan de mise en boite.
        /// Le cadre est fait de deux montants et d'une imposte — un panneau
        /// plein bouchait l'ouverture, et c'est ce blanc qu'on voyait a la
        /// place du passage. Le vantail pend a un gond et s'ouvre du cote de
        /// la piece. Renvoie ce gond.
        /// </summary>
        static Transform Porte(Transform parent, Vector3 position)
        {
            var go = new GameObject("Porte");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var t = go.transform;

            var blanc = Bloc.Couleur(0xF4F1EA);
            var vitre = Bloc.Couleur(0xBFE8F2);

            foreach (float cote in new[] { -1f, 1f })
                Bloc.Boite("Jambage", t, new Vector3(cote * 0.82f, 1.15f, -0.30f),
                           new Vector3(0.16f, 2.38f, 0.20f), blanc).SansCollision();
            Bloc.Boite("Imposte", t, new Vector3(0f, 2.30f, -0.30f),
                       new Vector3(1.80f, 0.16f, 0.20f), blanc).SansCollision();

            var gond = new GameObject("Gond");
            gond.transform.SetParent(t, false);
            gond.transform.localPosition = new Vector3(0.72f, 0f, -0.26f);

            Bloc.Verre("Battant", gond.transform, new Vector3(-0.70f, 1.12f, 0f),
                       new Vector3(1.36f, 2.06f, 0.10f), vitre).SansCollision();
            Bloc.Boite("Barre", gond.transform, new Vector3(-0.70f, 1.05f, -0.06f),
                       new Vector3(1.10f, 0.10f, 0.06f), blanc).SansCollision();
            Bloc.Boite("Poignee", gond.transform, new Vector3(-1.22f, 1.05f, -0.09f),
                       new Vector3(0.16f, 0.14f, 0.08f), Bloc.Metal).SansCollision();

            return gond.transform;
        }

        /// <summary>
        /// La petite piece derriere la porte : dallage, trois pans de mur, et
        /// de quoi s'asseoir. Elle reste cachee jusqu'a l'achat.
        /// </summary>
        static GameObject Piece(Transform parent, Vector3 centre, Transform gond, int seuil,
                                Joueur joueur, List<GameObject> cachent)
        {
            var go = new GameObject("PetitePiece");
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            // Eteinte avant d'etre batie, pour la meme raison que l'employe :
            // sinon elle liberait le pas de sa porte et repoussait la limite
            // du terrain des la construction, avant tout achat.
            go.SetActive(false);
            var t = go.transform;

            // Les pans de cote descendent jusqu'au mur du fond, qui est a
            // 2,5 d'ici : un jour de quelques centimetres suffisait a laisser
            // voir la pelouse au raccord.
            const float demiX = 2.95f, demiZ = 2.5f;

            // Le dallage va jusqu'aux faces exterieures des murs, et mord sur
            // celui de la salle : arrete aux axes, il laissait une marche de
            // 20 cm dans le beige et un liseré d'herbe sous les murs.
            // large de l'epaisseur des murs de part et d'autre, et mordant de
            // 0,3 sur le dallage de la salle au sud
            Bloc.Boite("SolPiece", t, new Vector3(0f, -0.2f, -0.15f),
                       new Vector3(demiX * 2f + 0.4f, 0.4f, demiZ * 2f + 0.3f),
                       Bloc.Sol).SansCollision();

            Mur(t, new Vector3(-demiX, 1.6f, 0f), new Vector3(0.4f, 3.2f, demiZ * 2f), centre);
            var panDroit = Mur(t, new Vector3(demiX, 1.6f, 0f), new Vector3(0.4f, 3.2f, demiZ * 2f),
                               centre);
            Mur(t, new Vector3(0f, 1.6f, demiZ - 0.2f), new Vector3(demiX * 2f + 0.4f, 3.2f, 0.4f),
                centre);

            // Le bureau du patron, cale contre le mur de droite. Son fauteuil
            // est derriere, adosse au fond : assis, il regarde la porte — donc
            // la camera. Devant le plan, on ne verrait que son dos.
            AmenagerBureau(t, new Vector3(1.25f, 0f, 0.55f), centre, joueur);

            // La fontaine a eau, dans le coin gauche du fond : le bureau tient
            // le cote droit, c'est le seul mur libre.
            Fontaine(t, new Vector3(-2.05f, 0f, 1.65f), centre);

            var p = go.AddComponent<PetitePiece>();
            p.Passage = seuil;
            p.Gond = gond;
            p.Joueur = joueur;
            // le pas de la porte : le porteur du gond, et non le gond lui-meme,
            // qui est decale sur le montant
            p.Seuil = gond.parent != null ? gond.parent.position : gond.position;
            p.DemiTerrainZ = centre.z + demiZ - 0.85f;

            // Les pans qui s'interposent entre la camera et la piece : son
            // cote droit, et tout ce qui borde la porte cote salle — vitres
            // comprises, sinon elles flottent seules au-dessus de la pelouse.
            var discrets = go.AddComponent<MursDiscrets>();
            discrets.Joueur = joueur;
            var aEffacer = new List<GameObject> { panDroit };
            aEffacer.AddRange(cachent);
            discrets.Murs = aEffacer.ToArray();
            discrets.Centre = centre;
            discrets.DemiX = demiX;
            discrets.DemiZ = demiZ;

            return go;
        }

        /// <summary>Un pan de mur de la piece, avec sa collision.</summary>
        static GameObject Mur(Transform parent, Vector3 local, Vector3 taille, Vector3 centre)
        {
            var pan = Bloc.Boite("MurPiece", parent, local, taille, Bloc.MachineBis).SansCollision();
            Obstacles.Ajouter(centre + new Vector3(local.x, 0f, local.z), taille.x, taille.z);
            return pan;
        }

        /// <summary>
        /// Le style repose sur des couleurs franches et des ombres douces. Sans
        /// ces reglages, l'eclairage par defaut delave tout et les bords crenent.
        /// </summary>
        static void Qualite()
        {
            QualitySettings.antiAliasing = 8;              // bords nets, meme sur les arretes obliques
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            // La camera voit toute la pizzeria d'un coup : une portee d'ombre
            // courte les faisait disparaitre au fond de la salle.
            QualitySettings.shadowDistance = 45f;
            QualitySettings.shadowCascades = 4;
            // trois lumieres par pixel : sans cela, la lumiere d'appoint et
            // celle de contre-jour passent en calcul par sommet et disparaissent
            QualitySettings.pixelLightCount = 4;
            // Le nom en vigueur depuis Unity 2022. L'ancien etant obsolete,
            // l'editeur reecrivait le fichier a chaque ouverture du projet,
            // ce qui bloquait les mises a jour du depot.
            QualitySettings.globalTextureMipmapLimit = 0;   // textures a pleine taille
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;

            // Ambiante en degrade plutot qu'uniforme : ciel clair et frais au
            // -dessus, rebond chaud du sol en dessous. C'est ce qui donne du
            // volume aux faces a l'ombre, la ou une ambiante plate aplatit tout.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.60f, 0.60f, 0.60f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.36f, 0.28f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = false;                    // le style ne supporte pas la brume
        }

        /// <summary>
        /// Un eclairage a trois lumieres, comme en studio : le soleil qui
        /// porte les ombres, un appoint froid du cote oppose pour que les
        /// faces a l'ombre ne virent pas au noir, et un contre-jour qui
        /// detache les silhouettes du fond. Une seule directionnelle donnait
        /// des aplats sans relief.
        /// </summary>
        static void Lumiere(Transform parent)
        {
            // Une scene neuve contient deja une lumiere directionnelle : en
            // ajouter une seconde surexpose tout. On reprend celle qui existe.
            var soleil = Object.FindObjectOfType<Light>();
            if (soleil == null) soleil = NouvelleLumiere(parent, "Soleil");

            soleil.type = LightType.Directional;
            soleil.color = Bloc.Couleur(0xFFF3DC);         // soleil legerement chaud
            soleil.intensity = 1.25f;
            soleil.shadows = LightShadows.Soft;            // les ombres portees font le relief
            soleil.shadowStrength = 0.55f;
            // Biais serres : trop laches, l'ombre se decolle du pied des murs
            // et l'objet parait flotter.
            soleil.shadowBias = 0.02f;
            soleil.shadowNormalBias = 0.15f;
            soleil.renderMode = LightRenderMode.ForcePixel;
            soleil.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            var appoint = NouvelleLumiere(parent, "Appoint");
            appoint.type = LightType.Directional;
            appoint.color = Bloc.Couleur(0xBFD4F2);        // rebond du ciel, franchement froid
            appoint.intensity = 0.38f;
            appoint.shadows = LightShadows.None;           // une seule source porte les ombres
            appoint.renderMode = LightRenderMode.ForcePixel;
            appoint.transform.rotation = Quaternion.Euler(28f, 155f, 0f);

            var contre = NouvelleLumiere(parent, "ContreJour");
            contre.type = LightType.Directional;
            contre.color = Bloc.Couleur(0xFFE3B8);
            contre.intensity = 0.22f;
            contre.shadows = LightShadows.None;
            contre.renderMode = LightRenderMode.ForcePixel;
            // de derriere et de haut : c'est le liset qui detoure les tetes
            contre.transform.rotation = Quaternion.Euler(18f, 118f, 0f);
        }

        static Light NouvelleLumiere(Transform parent, string nom)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            return go.AddComponent<Light>();
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
            // Eteint AVANT d'etre equipe. Unity eveille un composant des qu'on
            // l'ajoute a un objet actif : monte puis eteint, l'employe avait
            // deja pris son poste et le comptoir le croyait embauche — il
            // etait la sans qu'on ait paye la dalle verte.
            go.SetActive(false);
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
            // Le meuble est plus long que le comptoir : il porte quatre
            // emplacements — la reserve, les deux ronds, la pile de plateaux —
            // et sur 3,80 ils se touchaient.
            Bloc.Boite("Plan", t, new Vector3(0f, 0.55f, 0f), new Vector3(4.2f, 1.1f, 1.2f),
                       Bloc.Machine).SansCollision();
            Bloc.Boite("Dessus", t, new Vector3(0f, 1.15f, 0f), new Vector3(4.4f, 0.16f, 1.4f),
                       Bloc.Metal).Poli(0.5f, 0.75f).SansCollision();
            Bloc.Boite("Bandeau", t, new Vector3(0f, 0.20f, -0.61f), new Vector3(4.2f, 0.30f, 0.04f),
                       Bloc.MachineBis).SansCollision();

            Obstacles.Ajouter(position, 4.4f, 1.4f);

            // Un seul tas de cartons, dans le coin arriere gauche du plan :
            // etales cote a cote, ils occupaient tout le meuble.
            //
            // Les quatre emplacements du dessus — reserve, rond rouge, rond
            // vert, plateaux — sont espaces d'au moins une largeur de carton :
            // trop proches, le carton pose sur le rond rouge rentrait dans le
            // tas de la reserve, et l'on voyait deux cartons s'interpenetrer.
            var rangee = new GameObject("Boites");
            rangee.transform.SetParent(t, false);
            rangee.transform.localPosition = new Vector3(-1.74f, 1.23f, 0.26f);

            // Les deux ronds de travail, sur le dessus : le carton sort de la
            // reserve, se pose au premier, glisse au second, et c'est la qu'il
            // recoit sa pizza.
            // la pile de plateaux propres, a l'autre bout du plan
            var plateaux = new GameObject("PlateauxPropres");
            plateaux.transform.SetParent(t, false);
            plateaux.transform.localPosition = new Vector3(1.60f, 1.23f, 0.10f);

            var rouge = new GameObject("Preparation");
            rouge.transform.SetParent(t, false);
            rouge.transform.localPosition = new Vector3(-0.68f, 1.23f, 0f);

            // ce qui est pret attend au milieu du plan, entre les deux reserves
            var vert = new GameObject("Assemblage");
            vert.transform.SetParent(t, false);
            vert.transform.localPosition = new Vector3(0.35f, 1.23f, 0f);

            var poste = new GameObject("Poste");
            poste.transform.SetParent(t, false);
            poste.transform.localPosition = new Vector3(0f, 0f, -1.40f);

            var e = go.AddComponent<Emballage>();
            e.Boites = rangee.AddComponent<Pile>();
            e.PlateauxEnPile = plateaux.AddComponent<Pile>();
            e.Preparation = rouge.AddComponent<Pile>();
            e.Assemblage = vert.AddComponent<Pile>();
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
                       Bloc.Metal).Poli(0.5f, 0.75f).SansCollision();
            var caisse = Caisse.Creer(go.transform, new Vector3(-1.05f, 1.15f, 0f));

            Obstacles.Ajouter(go.transform.position, 3.9f, 2.1f);

            var pile = new GameObject("Stock");
            pile.transform.SetParent(go.transform, false);
            pile.transform.localPosition = new Vector3(0.6f, 1.25f, 0f);

            // le coin des plateaux, a l'autre bout du dessus
            var plateaux = new GameObject("Plateaux");
            plateaux.transform.SetParent(go.transform, false);
            plateaux.transform.localPosition = new Vector3(1.5f, 1.25f, 0f);

            // Le panneau du service, pose sur le coin du comptoir, face aux
            // clients qui arrivent — donc face a la camera.
            var pied = new GameObject("Enseigne");
            pied.transform.SetParent(go.transform, false);
            pied.transform.localPosition = new Vector3(-1.35f, 1.23f, -0.72f);
            Bloc.Boite("Montant", pied.transform, new Vector3(0f, 0.16f, 0f),
                       new Vector3(0.06f, 0.32f, 0.06f), Bloc.Taupe).SansCollision();
            var panneau = Bloc.Boite("Panneau", pied.transform, new Vector3(0f, 0.46f, 0f),
                                     new Vector3(0.72f, 0.34f, 0.05f), Bloc.Couleur(0x36B04A))
                              .SansCollision();
            var enseigne = pied.AddComponent<Enseigne>();
            enseigne.Panneau = panneau.GetComponent<MeshRenderer>();

            var file = new GameObject("PointFile");
            file.transform.SetParent(go.transform, false);
            file.transform.localPosition = new Vector3(0f, 0f, -2.2f);

            var sortie = new GameObject("Sortie");
            sortie.transform.SetParent(go.transform, false);
            sortie.transform.localPosition = new Vector3(9f, 0f, -6f);

            var c = go.AddComponent<Comptoir>();
            c.Stock = pile.AddComponent<Pile>();
            c.Plateaux = plateaux.AddComponent<Pile>();
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

            // Toute la maconnerie pend a un porteur mis a l'echelle : la pile
            // de sortie, elle, reste a taille normale, sinon les pizzas
            // retreciraient en se posant sur la pierre.
            const float echelle = 0.85f;
            var maconnerie = new GameObject("Maconnerie");
            maconnerie.transform.SetParent(go.transform, false);
            maconnerie.transform.localScale = new Vector3(echelle, echelle, echelle);
            var t = maconnerie.transform;

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
            Obstacles.Ajouter(position, 2.8f * echelle, 2.4f * echelle);
            Obstacles.Ajouter(position + new Vector3(0f, 0f, -1.62f * echelle),
                              2.5f * echelle, 1.3f * echelle);

            var pile = new GameObject("Sortie");
            pile.transform.SetParent(go.transform, false);
            pile.transform.localPosition = new Vector3(0f, 1.09f * echelle, -1.72f * echelle);

            var f = go.AddComponent<Four>();
            f.Sortie = pile.AddComponent<Pile>();
            f.Joueur = joueur;

            go.SetActive(actif);
            return go;
        }

        /// <summary>
        /// La table de la salle et ses deux chaises : plateau rouge arrondi sur
        /// un pied central, assises bleues, montants sombres. Un seul couvert
        /// est reellement servi — la seconde chaise est la pour l'allure.
        /// </summary>
        static TableRepas Salle(Transform parent, Vector3 position)
        {
            var go = new GameObject("TableSalle");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var t = go.transform;

            var rouge = Bloc.Couleur(0xE2503A);
            var sombre = Bloc.Couleur(0x2B303A);
            var assise = Bloc.Couleur(0x3D8BE0);

            Bloc.Galet("Plateau", t, new Vector3(0f, 0.78f, 0f), new Vector3(1.35f, 0.30f, 1.35f),
                       rouge).SansCollision();
            Bloc.Boite("Pied", t, new Vector3(0f, 0.36f, 0f), new Vector3(0.34f, 0.72f, 0.34f),
                       sombre).SansCollision();
            Bloc.Boite("Socle", t, new Vector3(0f, 0.06f, 0f), new Vector3(0.72f, 0.12f, 0.72f),
                       sombre).SansCollision();

            var siege = Chaise(t, new Vector3(-1.20f, 0f, 0f), 90f, sombre, assise);
            Chaise(t, new Vector3(1.20f, 0f, 0f), -90f, sombre, assise);

            Obstacles.Ajouter(position, 2.9f, 1.5f);

            // l'assiette se pose sur le plateau, pas au pied de la table
            var plateau = new GameObject("Assiette");
            plateau.transform.SetParent(t, false);
            plateau.transform.localPosition = new Vector3(0f, 0.90f, 0f);

            var table = go.AddComponent<TableRepas>();
            table.Siege = siege;
            table.Plateau = plateau.transform;
            return table;
        }

        /// <summary>
        /// La poubelle de la pizzeria : caisson bleu, couvercle rouge, et le
        /// pictogramme blanc. C'est la que finissent les restes des repas.
        /// </summary>
        static void Poubelle(Transform parent, Vector3 position)
        {
            var go = new GameObject("Poubelle");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var t = go.transform;

            Bloc.Boite("Caisson", t, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.9f),
                       Bloc.Machine).SansCollision();
            Bloc.Boite("Couvercle", t, new Vector3(0f, 0.96f, 0f), new Vector3(1.0f, 0.14f, 1.0f),
                       Bloc.Casquette).SansCollision();
            Bloc.Boite("Fente", t, new Vector3(0f, 1.04f, 0f), new Vector3(0.5f, 0.06f, 0.34f),
                       Bloc.Couleur(0x1A1A1E)).SansCollision();
            // le pictogramme, sur la face avant
            Bloc.Boite("Sigle", t, new Vector3(0f, 0.52f, -0.46f), new Vector3(0.34f, 0.40f, 0.04f),
                       Bloc.Couleur(0xF4F1EA)).SansCollision();

            Obstacles.Ajouter(position, 1.0f, 1.0f);
        }

        /// <summary>Une chaise : quatre montants, une assise, un dossier.</summary>
        /// <summary>
        /// Le bureau du patron : un plan de bois sombre sur deux caissons a
        /// tiroirs, sa lampe, ses papiers — et le fauteuil de direction ou le
        /// joueur vient s'asseoir. La piece n'etait qu'un volume vide.
        /// </summary>
        static Bureau AmenagerBureau(Transform parent, Vector3 local, Vector3 centrePiece,
                                     Joueur joueur)
        {
            var go = new GameObject("Bureau");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var t = go.transform;

            var bois = Bloc.Couleur(0x6B4423);
            var boisSombre = Bloc.Couleur(0x4A2E17);
            var laiton = Bloc.Couleur(0xD9A441);

            Bloc.Boite("Plateau", t, new Vector3(0f, 0.76f, 0f), new Vector3(2.00f, 0.10f, 0.85f),
                       bois).SansCollision();
            // le champ clair : sans lui, le plan se confond avec les caissons
            Bloc.Boite("Chant", t, new Vector3(0f, 0.70f, -0.44f), new Vector3(2.00f, 0.04f, 0.03f),
                       laiton).Poli(0.62f, 0.9f).SansCollision();

            foreach (float x in new[] { -0.68f, 0.68f })
            {
                Bloc.Boite("Caisson", t, new Vector3(x, 0.35f, 0.02f),
                           new Vector3(0.58f, 0.71f, 0.78f), boisSombre).SansCollision();
                // trois tiroirs, marques par leur poignee
                for (int i = 0; i < 3; i++)
                {
                    float y = 0.16f + i * 0.22f;
                    Bloc.Boite("Tiroir", t, new Vector3(x, y, -0.38f),
                               new Vector3(0.50f, 0.18f, 0.03f), bois).SansCollision();
                    Bloc.Boite("Poignee", t, new Vector3(x, y, -0.41f),
                               new Vector3(0.22f, 0.04f, 0.03f), laiton).SansCollision();
                }
            }

            // De quoi faire un bureau plutot qu'une table : une lampe, une
            // pile de papiers, un sous-main. La lampe se tient a l'autre bout
            // que le fauteuil : au milieu, elle passait devant le visage de
            // celui qui s'assoit.
            Bloc.Boite("Papiers", t, new Vector3(0.62f, 0.83f, 0.06f),
                       new Vector3(0.30f, 0.06f, 0.40f), Bloc.Couleur(0xF6F1E6)).SansCollision();

            Ordinateur(t, new Vector3(-0.05f, 0.81f, 0.02f));
            Bloc.Disque("PiedLampe", t, new Vector3(-0.80f, 0.83f, 0.18f), 0.20f, 0.05f,
                        laiton).SansCollision();
            Bloc.Boite("TigeLampe", t, new Vector3(-0.80f, 0.98f, 0.18f),
                       new Vector3(0.04f, 0.30f, 0.04f), laiton).SansCollision();
            Bloc.Galet("AbatJour", t, new Vector3(-0.80f, 1.14f, 0.18f),
                       new Vector3(0.30f, 0.18f, 0.30f), Bloc.Couleur(0x2F6B4F)).SansCollision();

            // Le meuble barre le passage ; le fauteuil, non — on doit pouvoir
            // aller s'y asseoir.
            Obstacles.Ajouter(centrePiece + local, 2.20f, 0.95f);

            var siege = FauteuilDeDirection(t, new Vector3(0f, 0f, 0.90f));

            var bureau = go.AddComponent<Bureau>();
            bureau.Joueur = joueur;
            bureau.Siege = siege;
            if (_portable != null) _portable.Bureau = bureau;
            return bureau;
        }

        /// <summary>
        /// La fontaine a eau : socle noir, colonne d'inox cannelee, collier
        /// sombre en haut et la bonbonne cylindrique posee dessus. La niche
        /// de service est creusee dans la colonne — robinets chaud et froid
        /// au fond, grille d'egouttage sous eux.
        /// </summary>
        static Transform Fontaine(Transform parent, Vector3 local, Vector3 centrePiece)
        {
            var go = new GameObject("Fontaine");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var t = go.transform;

            var inox = Bloc.Metal;
            var inoxClair = Bloc.Couleur(0xD5DBE1);
            var noir = Bloc.Couleur(0x22262B);
            var creux = Bloc.Couleur(0x14161A);
            var eau = Bloc.Couleur(0x86C6EA);
            var eauClaire = Bloc.Couleur(0xB8E0F5);

            // Le socle deborde de la colonne : c'est lui qui pose l'appareil
            // au sol au lieu de le faire flotter.
            Bloc.Boite("Socle", t, new Vector3(0f, 0.05f, 0f), new Vector3(0.56f, 0.10f, 0.52f),
                       noir).SansCollision();
            Bloc.Boite("Colonne", t, new Vector3(0f, 0.60f, 0f), new Vector3(0.46f, 1.00f, 0.42f),
                       inox).Poli(0.55f, 0.85f).SansCollision();
            // les cannelures verticales de la tole brossee
            foreach (float x in new[] { -0.16f, -0.05f, 0.06f, 0.17f })
                Bloc.Boite("Cannelure", t, new Vector3(x, 0.60f, -0.212f),
                           new Vector3(0.02f, 0.98f, 0.01f), inoxClair)
                    .Poli(0.6f, 0.85f).SansCollision();

            // Le collier noir sur lequel repose la bonbonne.
            Bloc.Boite("Collier", t, new Vector3(0f, 1.20f, 0f), new Vector3(0.54f, 0.20f, 0.50f),
                       noir).SansCollision();

            // La niche de service, creusee dans la colonne : c'est ce
            // renfoncement, plus que les robinets, qui fait reconnaitre
            // l'appareil de loin.
            Bloc.Boite("Niche", t, new Vector3(0f, 0.86f, -0.13f),
                       new Vector3(0.30f, 0.34f, 0.18f), creux).SansCollision();
            Bloc.Boite("RobinetChaud", t, new Vector3(-0.07f, 0.97f, -0.20f),
                       new Vector3(0.05f, 0.10f, 0.04f), Bloc.Couleur(0xC0392B)).SansCollision();
            Bloc.Boite("RobinetFroid", t, new Vector3(0.07f, 0.97f, -0.20f),
                       new Vector3(0.05f, 0.10f, 0.04f), Bloc.Couleur(0x2F6FB5)).SansCollision();
            // au fond de la niche, et non en saillie : c'est une grille
            // d'egouttage, pas une tablette
            Bloc.Boite("Grille", t, new Vector3(0f, 0.71f, -0.135f),
                       new Vector3(0.26f, 0.02f, 0.11f), inoxClair)
                .Poli(0.5f, 0.8f).SansCollision();

            // La bonbonne : un cylindre pose sur le collier, coiffe de son
            // bouchon.
            Bloc.Disque("Bonbonne", t, new Vector3(0f, 1.58f, 0f), 0.46f, 0.56f, eau)
                .SansCollision();
            Bloc.Disque("Bouchon", t, new Vector3(0f, 1.87f, 0f), 0.18f, 0.06f, eauClaire)
                .SansCollision();

            Obstacles.Ajouter(centrePiece + local, 0.60f, 0.55f);
            return t;
        }

        /// <summary>
        /// L'ordinateur portable du bureau : coque d'aluminium, clavier
        /// sombre et pave tactile, capot releve. Il est pose droit, dans
        /// l'axe du meuble, l'ecran tourne vers le mur — donc vers celui qui
        /// s'assoit, comme un portable pose sur un vrai bureau.
        /// </summary>
        static Transform Ordinateur(Transform parent, Vector3 local)
        {
            var go = new GameObject("Ordinateur");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            // Droit dans l'axe du bureau, ecran vers le mur : le demi-tour
            // met le pave tactile du cote du fauteuil et l'ecran en face.
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            // Un portable, pas une station de travail : a pleine taille il
            // mangeait la moitie du plan.
            go.transform.localScale = new Vector3(0.82f, 0.82f, 0.82f);
            var t = go.transform;

            var alu = Bloc.Couleur(0xC9CDD2);
            var aluSombre = Bloc.Couleur(0x9AA0A8);
            var dalle = Bloc.Couleur(0x1B2027);
            var fond = Bloc.Couleur(0x2E6FB7);
            var clair = Bloc.Couleur(0xD7DCE4);

            // la coque posee a plat, son clavier creuse et son pave tactile
            Bloc.Boite("Socle", t, new Vector3(0f, 0.018f, 0f),
                       new Vector3(0.74f, 0.035f, 0.52f), alu).Poli(0.62f, 0.9f).SansCollision();
            Bloc.Boite("Clavier", t, new Vector3(0f, 0.038f, 0.07f),
                       new Vector3(0.62f, 0.006f, 0.24f), dalle).SansCollision();
            Bloc.Boite("PaveTactile", t, new Vector3(0f, 0.038f, -0.14f),
                       new Vector3(0.22f, 0.006f, 0.13f), clair).SansCollision();
            Bloc.Boite("Charniere", t, new Vector3(0f, 0.035f, 0.265f),
                       new Vector3(0.74f, 0.03f, 0.04f), aluSombre).Poli(0.55f, 0.9f).SansCollision();

            // Tout le capot pend a un pivot pose sur la charniere : c'est lui
            // qu'on fait tourner pour ouvrir et fermer le portable.
            var pivot = new GameObject("Capot");
            pivot.transform.SetParent(t, false);
            pivot.transform.localPosition = new Vector3(0f, 0.05f, 0.265f);
            var cp = pivot.transform;

            // Le dos d'aluminium, puis l'ecran pose devant. D'une seule boite,
            // l'affichage aurait l'epaisseur du capot.
            Bloc.Boite("Dos", cp, new Vector3(0f, 0.23f, 0.010f),
                       new Vector3(0.74f, 0.48f, 0.025f), alu).Poli(0.62f, 0.9f).SansCollision();
            Bloc.Boite("Ecran", cp, new Vector3(0f, 0.235f, -0.005f),
                       new Vector3(0.70f, 0.44f, 0.008f), dalle).SansCollision();
            Bloc.Boite("Affichage", cp, new Vector3(0f, 0.235f, -0.010f),
                       new Vector3(0.66f, 0.38f, 0.006f), fond).SansCollision();
            // barre des menus et dock : c'est a cela qu'on le reconnait d'un
            // coup d'oeil, a la taille ou il apparait a l'ecran
            Bloc.Boite("BarreMenus", cp, new Vector3(0f, 0.410f, -0.011f),
                       new Vector3(0.66f, 0.03f, 0.006f), clair).SansCollision();
            Bloc.Boite("Dock", cp, new Vector3(0f, 0.068f, -0.011f),
                       new Vector3(0.34f, 0.035f, 0.006f), clair).SansCollision();

            var portable = go.AddComponent<OrdinateurPortable>();
            portable.Capot = cp;
            _portable = portable;
            return t;
        }

        /// <summary>
        /// Le fauteuil de direction : cuir capitonne, accoudoirs, pietement
        /// etoile a roulettes. Il tourne le dos au mur du fond — celui qui s'y
        /// assoit fait face au bureau, et donc a la camera.
        /// </summary>
        static Transform FauteuilDeDirection(Transform parent, Vector3 local)
        {
            var go = new GameObject("Fauteuil");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var t = go.transform;

            var cuir = Bloc.Couleur(0x2A2E38);
            var cuirClair = Bloc.Couleur(0x3B414F);
            var acier = Bloc.Couleur(0x9AA3AD);
            var laiton = Bloc.Couleur(0xD9A441);

            // le pietement etoile : cinq branches et leurs roulettes
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                float sx = Mathf.Sin(a), sz = Mathf.Cos(a);
                var branche = Bloc.Boite("Branche", t,
                                         new Vector3(sx * 0.22f, 0.09f, sz * 0.22f),
                                         new Vector3(0.09f, 0.07f, 0.46f), acier);
                branche.transform.localRotation = Quaternion.Euler(0f, i * 72f, 0f);
                branche.Poli(0.5f, 0.85f).SansCollision();
                Bloc.Bille("Roulette", t, new Vector3(sx * 0.43f, 0.06f, sz * 0.43f), 0.12f,
                           cuir).SansCollision();
            }

            Bloc.Disque("Verin", t, new Vector3(0f, 0.28f, 0f), 0.13f, 0.40f, acier)
                .Poli(0.5f, 0.85f).SansCollision();
            Bloc.Disque("Embase", t, new Vector3(0f, 0.44f, 0f), 0.34f, 0.06f, cuir).SansCollision();

            // assise capitonnee, cerclee de laiton
            Bloc.Galet("Assise", t, new Vector3(0f, 0.48f, 0f),
                       new Vector3(0.68f, 0.22f, 0.66f), cuir).SansCollision();
            Bloc.Boite("Passepoil", t, new Vector3(0f, 0.50f, -0.31f),
                       new Vector3(0.62f, 0.04f, 0.04f), laiton).SansCollision();

            // dossier haut, legerement incline, et son appuie-tete
            var dossier = Bloc.Boite("Dossier", t, new Vector3(0f, 1.03f, 0.31f),
                                     new Vector3(0.66f, 0.98f, 0.16f), cuir);
            dossier.transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            dossier.SansCollision();
            var coussin = Bloc.Galet("Capiton", t, new Vector3(0f, 1.00f, 0.23f),
                                     new Vector3(0.54f, 0.80f, 0.12f), cuirClair);
            coussin.transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            coussin.SansCollision();
            Bloc.Galet("AppuieTete", t, new Vector3(0f, 1.54f, 0.36f),
                       new Vector3(0.46f, 0.24f, 0.18f), cuir).SansCollision();

            foreach (float x in new[] { -0.37f, 0.37f })
            {
                Bloc.Boite("Montant", t, new Vector3(x, 0.66f, 0.10f),
                           new Vector3(0.07f, 0.30f, 0.07f), acier).SansCollision();
                Bloc.Galet("Accoudoir", t, new Vector3(x, 0.82f, -0.02f),
                           new Vector3(0.13f, 0.10f, 0.46f), cuir).SansCollision();
            }

            return t;
        }

        static Transform Chaise(Transform parent, Vector3 local, float cap, Color bois, Color assise)
        {
            var go = new GameObject("Chaise");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(0f, cap, 0f);
            var t = go.transform;

            Bloc.Boite("Assise", t, new Vector3(0f, 0.46f, 0f), new Vector3(0.52f, 0.08f, 0.52f),
                       assise).SansCollision();
            foreach (float x in new[] { -0.21f, 0.21f })
                foreach (float z in new[] { -0.21f, 0.21f })
                    Bloc.Boite("PiedChaise", t, new Vector3(x, 0.23f, z),
                               new Vector3(0.08f, 0.46f, 0.08f), bois).SansCollision();
            Bloc.Boite("Dossier", t, new Vector3(0f, 0.78f, -0.24f), new Vector3(0.52f, 0.56f, 0.08f),
                       bois).SansCollision();

            return t;
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
