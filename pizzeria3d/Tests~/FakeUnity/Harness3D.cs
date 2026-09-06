// Joue une partie complete a travers le vrai code du jeu, dans le faux runtime.
// But : verifier la boucle — produire, porter, deposer, servir, encaisser,
// debloquer — avant de la lancer dans l'editeur.
using System;
using System.Collections.Generic;
using Pizzeria3D;
using UnityEngine;
using UnityEngine.UI;

static class Harness3D
{
    static int _echecs;

    static void Check(string nom, bool ok)
    {
        Console.WriteLine((ok ? "OK    " : "ECHEC ") + nom);
        if (!ok) _echecs++;
    }

    static void Frames(int n) { for (int i = 0; i < n; i++) Scene.Frame(1f / 60f); }
    static void Secondes(float s) => Frames((int)(s * 60f));

    static GameObject Trouver(string nom)
    {
        foreach (var o in UnityEngine.Object.Tous)
            if (o is GameObject go && !go.Detruit && go.name == nom) return go;
        return null;
    }

    static List<T> TousLes<T>() where T : Component
    {
        var r = new List<T>();
        foreach (var o in UnityEngine.Object.Tous)
            if (o is T t && !t.gameObject.Detruit) r.Add(t);
        return r;
    }

    /// <summary>Compte les enfants directs portant ce nom.</summary>
    static int Compte(Transform parent, string nom, bool exact)
    {
        int n = 0;
        foreach (var e in parent.Enfants)
            if (exact ? e.gameObject.name == nom : e.gameObject.name.Contains(nom)) n++;
        return n;
    }

    /// <summary>
    /// Les vitres portant ce prefixe couvrent-elles le mur d'un bout a
    /// l'autre ? On verifie qu'aucun troncon nu de plus de 4 ne subsiste.
    /// </summary>
    static bool Fenetres(string prefixe, float debut, float fin, bool surX = false)
    {
        var pos = new List<float>();
        foreach (var o in UnityEngine.Object.Tous)
            if (o is GameObject go && !go.Detruit && go.name.StartsWith(prefixe))
                pos.Add(surX ? go.transform.position.x : go.transform.position.z);
        if (pos.Count == 0) return false;

        pos.Sort();
        float precedent = debut;
        foreach (var v in pos)
        {
            if (v - precedent > 4f) return false;
            precedent = v;
        }
        return fin - precedent <= 4f;
    }

    /// <summary>Un enfant direct par son nom, ou null.</summary>
    static Transform Piece(Transform parent, string nom)
    {
        if (parent == null) return null;
        foreach (var e in parent.Enfants) if (e.gameObject.name == nom) return e;
        return null;
    }

    /// <summary>Aucun enfant pose de travers ? Une boite carree ne pardonne pas.</summary>
    static bool ToutDroit(Transform parent)
    {
        foreach (var e in parent.Enfants)
        {
            if (!e.gameObject.name.StartsWith("Boite")) continue;
            var r = e.localRotation;
            if (Mathf.Abs(r.x) > 0.001f || Mathf.Abs(r.y) > 0.001f ||
                Mathf.Abs(r.z) > 0.001f) return false;
        }
        return true;
    }

    /// <summary>Deux meubles sont-ils peints pareil ? Compare une piece a une piece.</summary>
    static bool MemeCouleur(Transform a, string nomA, Transform b, string nomB)
    {
        var ca = CouleurDe(a, nomA);
        var cb = CouleurDe(b, nomB);
        return ca.r == cb.r && ca.g == cb.g && ca.b == cb.b;
    }

    static Color CouleurDe(Transform parent, string nom)
    {
        foreach (var e in parent.Enfants)
            if (e.gameObject.name == nom)
            {
                var r = e.gameObject.GetComponent<MeshRenderer>();
                if (r != null) return r.sharedMaterial.color;
            }
        return Color.black;
    }

    /// <summary>Couleur du haut d'un client, pour verifier la variete.</summary>
    static Color CouleurHaut(Client c)
    {
        foreach (var e in c.transform.Enfants)
        {
            if (e.gameObject.name != "Corps") continue;
            foreach (var f in e.Enfants)
                if (f.gameObject.name == "Torse")
                    return f.gameObject.GetComponent<MeshRenderer>().sharedMaterial.color;
        }
        return Color.black;
    }

    /// <summary>Le texte affiche dans la bulle d'un client, ou null.</summary>
    static string TexteBulle(Client c)
    {
        foreach (var e in c.transform.Enfants)
        {
            if (e.gameObject.name != "Bulle") continue;
            if (!e.gameObject.Actif) return "";
            foreach (var f in e.Enfants)
                if (f.gameObject.name == "Nombre")
                    return f.gameObject.GetComponent<Text>().text;
        }
        return null;
    }

    /// <summary>Teleporte le joueur : les tests de deplacement sont separes.</summary>
    static void Placer(Joueur j, Vector3 p) => j.transform.position = p;

    /// <summary>
    /// Pousse le joueur vers un point en actionnant vraiment la manette : c'est
    /// le seul moyen de tester les collisions, qu'une teleportation ignore.
    /// L'ecran etant tourne de 45 degres, on reporte la direction du monde sur
    /// les axes de l'ecran avant d'agiter le doigt.
    /// </summary>
    static void PousserVers(Joueur j, Vector3 cible, float secondes)
    {
        var cam = UnityEngine.Object.FindObjectOfType<UnityEngine.Camera>().transform;
        var avant = cam.forward; avant.y = 0f; avant = avant.normalized;
        var droite = cam.right;  droite.y = 0f; droite = droite.normalized;

        var d = cible - j.transform.position; d.y = 0f;
        float ex = d.x * droite.x + d.z * droite.z;   // composante ecran horizontale
        float ey = d.x * avant.x + d.z * avant.z;     // composante ecran verticale
        float m = Mathf.Sqrt(ex * ex + ey * ey);
        if (m <= 0f) return;
        ex /= m; ey /= m;

        Input.mousePosition = new Vector3(500f, 500f, 0f);
        Input.Boutons[0] = true;
        Frames(1);
        Input.mousePosition = new Vector3(500f + ex * 200f, 500f + ey * 200f, 0f);
        Secondes(secondes);
        Input.Boutons[0] = false;
        Frames(2);
    }

    /// <summary>Exporte la texture de la pizza pour la controler a l'oeil.</summary>
    static void DumpPizza(string chemin)
    {
        var racine = Pizza3D.Creer(new GameObject("Bac").transform, 0);
        Texture2D tex = null;
        foreach (var e in racine.transform.Enfants)
            if (e.gameObject.name == "Garniture")
                tex = e.gameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture as Texture2D;

        int w = tex.width, h = tex.height;
        using (var fs = new System.IO.FileStream(chemin, System.IO.FileMode.Create))
        {
            var entete = System.Text.Encoding.ASCII.GetBytes($"P6\n{w} {h}\n255\n");
            fs.Write(entete, 0, entete.Length);
            var buf = new byte[w * h * 3];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = tex.Pixels[(h - 1 - y) * w + x];
                int i = (y * w + x) * 3;
                float a = p.a / 255f;   // compose sur blanc : on voit la decoupe
                buf[i]     = (byte)(p.r * a + 255 * (1 - a));
                buf[i + 1] = (byte)(p.g * a + 255 * (1 - a));
                buf[i + 2] = (byte)(p.b * a + 255 * (1 - a));
            }
            fs.Write(buf, 0, buf.Length);
        }
        Console.WriteLine("texture ecrite dans " + chemin);
    }

    static void Main()
    {
        if (Environment.GetEnvironmentVariable("DUMP_PIZZA") is string chemin && chemin.Length > 0)
        {
            DumpPizza(chemin);
            return;
        }

        // Une scene Unity neuve arrive avec une Main Camera et une lumiere :
        // on reproduit ces conditions, c'est ce que le joueur aura.
        var camScene = new GameObject("Main Camera").AddComponent<UnityEngine.Camera>();
        new GameObject("Directional Light").AddComponent<Light>();

        Batisseur.Monter();
        Frames(2);

        Check("la camera de la scene est reutilisee, pas doublee",
              TousLes<UnityEngine.Camera>().Count == 1);
        Check("la lumiere de la scene est reutilisee, pas doublee",
              TousLes<Light>().Count == 1);
        Check("c'est bien la camera d'origine qui sert",
              UnityEngine.Object.FindObjectOfType<UnityEngine.Camera>() == camScene);

        var joueur = UnityEngine.Object.FindObjectOfType<Joueur>();
        var four = UnityEngine.Object.FindObjectOfType<Four>();
        var comptoir = UnityEngine.Object.FindObjectOfType<Comptoir>();
        var zoneCaissier = UnityEngine.Object.FindObjectOfType<ZoneAchat>();
        var caisse = UnityEngine.Object.FindObjectOfType<Caisse>();
        var table = UnityEngine.Object.FindObjectOfType<Emballage>();
        var camera = UnityEngine.Object.FindObjectOfType<UnityEngine.Camera>();

        Check("la scene se monte sans editeur", joueur != null && four != null && comptoir != null);
        Check("camera isometrique orthographique", camera != null && camera.orthographic);
        Check("la caisse demarre au montant de mise au point",
              Banque.Solde == Reglages.ArgentDepart);
        Check("une zone d'embauche attend derriere la caisse",
              zoneCaissier != null && zoneCaissier.Restant == Reglages.PrixCaissier);

        // Elle se tient juste derriere le tiroir-caisse, et le joueur doit
        // pouvoir s'y placer : une dalle collee au comptoir serait injouable.
        var dalle = zoneCaissier != null ? zoneCaissier.transform.position : Vector3.zero;
        var tiroir = caisse != null ? caisse.transform.position : Vector3.zero;
        // Derriere le tiroir, mais assez en retrait pour ne pas etre mangee par
        // le comptoir a l'ecran : la vue isometrique cache ce qui le longe.
        Check("elle est derriere le tiroir-caisse, en retrait",
              Mathf.Abs(dalle.x - tiroir.x) < 0.6f &&
              dalle.z - tiroir.z > 2f && dalle.z - tiroir.z < 3f);
        // Resoudre ne repousse pas hors d'un obstacle, il refuse d'y entrer :
        // c'est donc Bloque qu'il faut interroger, sinon le test ne dit rien.
        Check("le joueur peut se tenir dessus", !Obstacles.Bloque(dalle, Reglages.RayonJoueur));

        var cadre = zoneCaissier != null ? Piece(zoneCaissier.transform, "Dalle") : null;
        Check("elle reste discrete", cadre != null && cadre.localScale.x <= 1.2f);
        Check("le caissier n'est pas encore la",
              Trouver("Caissier") != null && !Trouver("Caissier").activeSelf && !comptoir.CaissierPresent);
        Check("le tiroir-caisse est ferme au depart", caisse != null && !caisse.EstOuverte);

        // --- le decor ferme bien ---
        // Un mur qui s'arrete avant le bord du dallage laisse voir le vide :
        // on compare donc les deux etendues, pas l'allure generale.
        var murGauche = Trouver("MurGauche");
        var sol = Trouver("Sol");
        Check("le mur de gauche est monte", murGauche != null && sol != null);
        if (murGauche != null && sol != null)
        {
            float murDebut = murGauche.transform.position.z - murGauche.transform.localScale.z * 0.5f;
            float murFin   = murGauche.transform.position.z + murGauche.transform.localScale.z * 0.5f;
            float solDebut = sol.transform.position.z - sol.transform.localScale.z * 0.5f;
            float solFin   = sol.transform.position.z + sol.transform.localScale.z * 0.5f;
            Check("il court jusqu'au bout du dallage",
                  murDebut <= solDebut + 0.01f && murFin >= solFin - 0.01f);
            Check("le mur de gauche est vitre sur toute sa longueur",
                  Fenetres("VitreGauche", murDebut, murFin));
        }
        else
        {
            Check("il court jusqu'au bout du dallage", false);
            Check("le mur de gauche est vitre sur toute sa longueur", false);
        }

        var murFond = Trouver("MurFond");
        Check("le mur du fond est monte", murFond != null && sol != null);
        if (murFond != null && sol != null)
        {
            float murGauche2 = murFond.transform.position.x - murFond.transform.localScale.x * 0.5f;
            float murDroite  = murFond.transform.position.x + murFond.transform.localScale.x * 0.5f;
            float solGauche  = sol.transform.position.x - sol.transform.localScale.x * 0.5f;
            float solDroite  = sol.transform.position.x + sol.transform.localScale.x * 0.5f;
            Check("il court d'un bord a l'autre du dallage",
                  murGauche2 <= solGauche + 0.01f && murDroite >= solDroite - 0.01f);
            Check("il est vitre sur toute sa longueur",
                  Fenetres("VitreFond", murGauche2, murDroite, true));
        }
        else
        {
            Check("il court d'un bord a l'autre du dallage", false);
            Check("il est vitre sur toute sa longueur", false);
        }

        // Les deux ronds de travail : le rouge avant le vert, tous deux sur le
        // dessus du plan et non a cote.
        if (table != null)
        {
            var rouge = table.Preparation.transform.localPosition;
            var vert = table.Assemblage.transform.localPosition;
            Check("le plan a ses deux ronds de travail",
                  table.Preparation != null && table.Assemblage != null);
            Check("le rouge est avant le vert", rouge.x < vert.x);
            Check("les deux sont poses sur le dessus",
                  Mathf.Abs(rouge.y - vert.y) < 0.01f && rouge.y > 1f);
        }

        // La pile de cartons de la cour masquait ce mur : elle a ete retiree.
        Check("plus de pile de cartons dans la cour",
              Trouver("Caisse0") == null && Trouver("Caisse1") == null && Trouver("Caisse2") == null);
        Check("et plus d'obstacle invisible a sa place",
              Obstacles.Resoudre(new Vector3(6.6f, 0f, 4.2f), new Vector3(0f, 0f, 0.6f),
                                 Reglages.RayonJoueur).z > 4.7f);

        // --- la table de mise en boite ---
        Check("un plan de mise en boite est monte", table != null);
        Check("il est derriere la caisse",
              table != null && table.transform.position.z > comptoir.transform.position.z + 2f);
        // les vitres du fond sont a z 6,85 : le plan doit leur etre adosse
        var vitre = Trouver("VitreFond2");
        Check("il est adosse aux fenetres du fond",
              table != null && vitre != null &&
              Mathf.Abs(table.transform.position.z - vitre.transform.position.z) < 1.5f);

        // Meme facture que le comptoir : c'est ce qui a ete demande, et deux
        // meubles de styles differents jureraient dans une scene aussi petite.
        Check("il a la meme facture que la caisse",
              table != null && MemeCouleur(table.transform, "Plan", comptoir.transform, "Plan") &&
              MemeCouleur(table.transform, "Dessus", comptoir.transform, "Dessus"));

        Check("des boites a pizza y attendent",
              table != null && table.Reserve == Reglages.BoitesEnReserve);
        Check("elles forment un seul tas",
              table != null && Compte(table.Boites.transform, "Boite", false) == Reglages.BoitesEnReserve);

        // Un tas, c'est l'un sur l'autre : rien ne s'ecarte sur les cotes, et
        // chaque carton est plus haut que le precedent.
        float ecartLateral = 0f, hautTas = 0f;
        if (table != null)
            foreach (var b in table.Boites.transform.Enfants)
            {
                ecartLateral = Mathf.Max(ecartLateral, Mathf.Abs(b.localPosition.x));
                ecartLateral = Mathf.Max(ecartLateral, Mathf.Abs(b.localPosition.z));
                hautTas = Mathf.Max(hautTas, b.localPosition.y);
            }
        Check("les cartons sont poses l'un sur l'autre", ecartLateral < 0.01f);
        Check("le tas monte bien de la hauteur des cartons",
              Mathf.Abs(hautTas - (Reglages.BoitesEnReserve - 1) * Reglages.EpaisseurBoite) < 0.01f);
        Check("il reste plus bas que les personnages", hautTas < 0.6f);

        // Il est pousse dans un coin : au milieu, il mangeait tout le plan.
        var coin = table != null ? table.Boites.transform.localPosition : Vector3.zero;
        Check("le tas est pousse dans un coin", coin.x < -1f && coin.z > 0.1f);
        var carton = table != null && table.Boites.transform.Enfants.Count > 0
                   ? table.Boites.transform.Enfants[0] : null;
        var pastille = CouleurDe(carton, "Etiquette");
        Check("l'etiquette du carton n'est pas rouge sauce",
              carton != null &&
              !(pastille.r == Bloc.Sauce.r && pastille.g == Bloc.Sauce.g && pastille.b == Bloc.Sauce.b));

        Check("les cartons sont poses bien droits",
              table != null && ToutDroit(table.Boites.transform));
        Check("le caissier vient s'y placer devant, pas dedans",
              table != null && table.PointDeTravail.z < table.transform.position.z);
        // en marchant droit dessus, le joueur doit etre arrete avant le plateau
        var buteeTable = Obstacles.Resoudre(new Vector3(1.6f, 0f, 4.6f),
                                            new Vector3(0f, 0f, 0.5f), Reglages.RayonJoueur);
        Check("on ne traverse pas le plan", buteeTable.z < 5.05f);

        // --- manette flottante ---
        var manette = UnityEngine.Object.FindObjectOfType<Manette>();
        Check("la manette existe", manette != null);
        Check("la manette est cachee au repos", !manette.EstVisible);

        var depart = joueur.transform.position;
        Input.mousePosition = new Vector3(500f, 500f, 0f);
        Input.Boutons[0] = true;
        Frames(1);                                   // pose du doigt : origine
        Check("la manette apparait sous le doigt", manette.EstVisible);
        Check("elle est au repos tant qu'on n'a pas glisse",
              manette.Direction.x == 0f && manette.Direction.y == 0f);
        Input.mousePosition = new Vector3(500f, 800f, 0f);   // glisse vers le haut
        Frames(1);
        Check("la manette suit le glissement", manette.Direction.y > 0.5f);

        var demarche = joueur.GetComponent<Demarche>();
        Check("le joueur a une demarche", demarche != null);
        Check("sa course est bornee a un", manette.Direction.magnitude <= 1.001f);
        Secondes(0.5f);
        var apres = joueur.transform.position;
        Check("le doigt fait avancer le joueur", (apres - depart).magnitude > 1f);
        // Ce qui compte n'est pas le signe des coordonnees du monde mais ce
        // que voit le joueur : pousser vers le haut doit monter a l'ecran, et
        // ne pas deriver sur le cote.
        var vue = camera.transform;
        var camAvant = vue.forward; camAvant.y = 0f; camAvant = camAvant.normalized;
        var camDroite = vue.right;  camDroite.y = 0f; camDroite = camDroite.normalized;
        var course = apres - depart; course.y = 0f;
        float versLeHaut = course.x * camAvant.x + course.z * camAvant.z;
        float versLeCote = course.x * camDroite.x + course.z * camDroite.z;
        Check("pousser vers le haut monte a l'ecran", versLeHaut > 1f);
        Check("et ne derive pas sur le cote", Mathf.Abs(versLeCote) < 0.2f);
        Check("les membres se balancent quand il marche", demarche.Allure > 0.3f);
        float phaseAvant = demarche.Phase;
        Frames(10);
        Check("le pas avance", demarche.Phase > phaseAvant);

        // Les genoux plient pendant le pas, et JAMAIS a l'envers : une jambe
        // qui se casse vers l'avant se remarque tout de suite.
        float flexionMax = 0f;
        bool genouALEnvers = false;
        for (int i = 0; i < 120; i++)
        {
            Frames(1);
            foreach (var genou in new[] { demarche.GenouG, demarche.GenouD })
            {
                if (genou == null) continue;
                // x du quaternion : positif quand le pivot tourne vers l'arriere
                float x = genou.localRotation.x;
                if (x < -0.002f) genouALEnvers = true;
                flexionMax = Mathf.Max(flexionMax, x);
            }
        }
        Check("les genoux plient quand il marche", flexionMax > 0.05f);
        Check("et jamais a l'envers", !genouALEnvers);

        Input.Boutons[0] = false;
        Frames(2);
        Secondes(0.8f);
        Check("le balancement s'arrete quand il s'arrete", demarche.Allure < 0.05f);
        Check("la manette disparait au relachement", !manette.EstVisible);
        Check("et sa direction retombe a zero",
              manette.Direction.x == 0f && manette.Direction.y == 0f);
        var arret = joueur.transform.position;
        Secondes(0.3f);
        Check("le joueur s'arrete quand on lache", (joueur.transform.position - arret).magnitude < 0.01f);

        // --- collisions ---
        Check("le decor est solide", Obstacles.Nombre >= 5);
        Check("le comptoir barre le passage", Obstacles.Bloque(comptoir.transform.position, 0.05f));
        Check("le four barre le passage", Obstacles.Bloque(four.transform.position, 0.05f));

        Placer(joueur, comptoir.transform.position + new Vector3(0f, 0f, -3.5f));
        PousserVers(joueur, comptoir.transform.position, 2.5f);
        Check("le joueur n'entre pas dans le comptoir",
              !Obstacles.Bloque(joueur.transform.position, 0.02f));

        Placer(joueur, new Vector3(6.5f, 0f, 6f));
        PousserVers(joueur, new Vector3(40f, 0f, 40f), 3f);
        Check("le joueur ne quitte pas le dallage",
              Mathf.Abs(joueur.transform.position.x) <= 8.1f &&
              Mathf.Abs(joueur.transform.position.z) <= 7.1f);

        // le point capital : un four solide ne doit pas rendre le chargement
        // impossible, sinon la boucle du jeu casse.
        Placer(joueur, new Vector3(-4.2f, 0f, -1.5f));
        PousserVers(joueur, four.Sortie.transform.position, 2.5f);
        Check("le joueur atteint quand meme la pierre du four",
              joueur.EstPres(four.Sortie.transform.position, Reglages.RayonRamassage));

        // et il glisse le long d'un mur au lieu de s'y coller. On part a
        // l'ouest du plan de mise en boite, sinon on demarre dedans.
        Placer(joueur, new Vector3(-3f, 0f, 6f));
        var avantGlisse = joueur.transform.position;
        PousserVers(joueur, new Vector3(0f, 0f, 9f), 1.5f);   // en biais vers le mur du fond
        // glisser, c'est avancer lateralement TOUT EN etant plaque au mur :
        // verifier le seul deplacement en x laisserait passer un joueur qui
        // n'a jamais touche le mur.
        Check("le joueur glisse le long du mur",
              Mathf.Abs(joueur.transform.position.x - avantGlisse.x) > 0.5f &&
              joueur.transform.position.z > 6.3f);

        // --- le four produit ---
        Placer(joueur, new Vector3(0f, 0f, -6f));    // loin, pour laisser le stock monter
        Secondes(Reglages.DureeCuisson * 3.5f + 1f);
        Check("le four produit des pizzas", four.Sortie.Nombre >= 3);

        // Cadence mesuree en secondes reelles, pas en multiples du reglage :
        // sinon le test suivrait n'importe quel ralentissement. La fenetre est
        // longue pour que le reste de cuisson en cours ne fausse pas le compte.
        four.Sortie.Vider();
        Secondes(62f);
        Check("il en sort au moins dix en une minute", four.Sortie.Nombre >= 10);

        // on remet le four dans l'etat attendu par la suite des essais
        four.Sortie.Vider();
        Secondes(Reglages.DureeCuisson * 3.5f + 1f);

        // --- ramassage ---
        Placer(joueur, four.Sortie.transform.position);
        Secondes(1.2f);
        Check("le joueur charge la pile dans ses mains", joueur.Portee.Nombre >= 3);
        Check("le stock du four baisse d'autant", four.Sortie.Nombre < 3);

        // --- la pile se porte a la main, plus sur la tete ---
        var corpsJoueur = joueur.transform.Find("Corps");
        var mainsJoueur = corpsJoueur != null ? corpsJoueur.Find("Mains") : null;
        Check("le personnage a un point de portage au buste", mainsJoueur != null);

        var pileJoueur = joueur.Portee.transform;
        Check("la pile portee pend aux mains", mainsJoueur != null && pileJoueur.parent == mainsJoueur);
        Check("elle est plus bas que la tete", mainsJoueur != null && mainsJoueur.localPosition.y < 1.2f);
        Check("elle est devant le corps", mainsJoueur != null && mainsJoueur.localPosition.z > 0.3f);

        var demarcheJoueur = joueur.GetComponent<Demarche>();
        Check("les bras se figent quand il porte",
              demarcheJoueur != null && demarcheJoueur.BrasPortent);

        // Les paumes doivent tomber sur la pile : on refait le calcul de la
        // demarche a la main. Une pile posee a cote des mains serait pire que
        // sur la tete.
        if (mainsJoueur != null && corpsJoueur != null)
        {
            var epaule = corpsJoueur.Find("EpauleG");
            Transform paume = null;
            if (epaule != null)
                foreach (var e in epaule.Enfants) if (e.gameObject.name == "Main") paume = e;

            bool sousLesPaumes = false;
            if (epaule != null && paume != null)
            {
                var p = epaule.localPosition + epaule.localRotation * paume.localPosition;
                var ecart = p - mainsJoueur.localPosition;
                ecart.x = 0f;                       // les deux mains encadrent la pile
                sousLesPaumes = ecart.magnitude < 0.06f;
            }
            Check("la pile repose bien entre les paumes", sousLesPaumes);
        }
        else Check("la pile repose bien entre les paumes", false);

        int portees = joueur.Portee.Nombre;
        Secondes(Reglages.DureeCuisson * 2f);
        Check("la pile portee plafonne a la capacite",
              joueur.Portee.Nombre <= Reglages.CapacitePortee && joueur.Portee.Nombre >= portees);

        // --- depot au comptoir ---
        // Le stock ne s'accumule pas forcement : les clients deja en file le
        // vident au fur et a mesure. Ce qui se verifie, c'est que la pile
        // portee se transfere.
        int avantDepot = joueur.Portee.Nombre;
        Placer(joueur, comptoir.transform.position);
        Secondes(1.5f);
        Check("le joueur decharge sa pile au comptoir", joueur.Portee.Nombre < avantDepot);
        // Rien de nu sur le comptoir : une pizza posee au milieu des cartons
        // faisait une pile batarde, et le client repartait avec un melange.
        Check("ce qu'il pose part en boite", comptoir.Stock.Nombre > 0 && comptoir.Stock.ToutEmballe);
        Check("aucune pizza nue ne traine sur le comptoir",
              Compte(comptoir.Stock.transform, "Pizza", false) == 0);

        // --- un client arrive, patiente, est servi ---
        Check("des clients font la queue", comptoir.TailleFile >= 1 || TousLes<Client>().Count >= 1);

        // --- allure des clients ---
        var premierClient = TousLes<Client>()[0];
        Check("un client a la meme silhouette que le personnel",
              premierClient.transform.Find("Corps") != null &&
              premierClient.transform.Find("Corps").Find("HancheG") != null);
        Check("il marche au lieu de glisser", premierClient.GetComponent<Demarche>() != null);

        // On doit pouvoir dire dans quel sens marche un personnage : yeux
        // devant, fesses derriere.
        var corpsClient = premierClient.transform.Find("Corps");
        Transform oeil = null, fesse = null;
        int mains = 0;
        foreach (var e in corpsClient.Enfants)
        {
            if (e.gameObject.name == "Pupille") oeil = e;
            if (e.gameObject.name == "Fesse") fesse = e;
            // les mains pendent aux epaules, pas au buste
            if (e.gameObject.name.StartsWith("Epaule")) mains += Compte(e, "Main", true);
        }
        // --- les genoux ---
        var hancheClient = corpsClient.Find("HancheG");
        var genouClient = hancheClient != null ? hancheClient.Find("Genou") : null;
        Check("la jambe est coupee au genou", genouClient != null);
        Check("la cuisse pend a la hanche",
              hancheClient != null && Compte(hancheClient, "Cuisse", true) == 1);
        Check("le mollet et la chaussure pendent au genou",
              genouClient != null && Compte(genouClient, "Mollet", true) == 1 &&
              Compte(genouClient, "Chaussure", true) == 1);

        // Pas de trou a l'articulation : la cuisse doit descendre jusqu'au
        // pivot et le mollet remonter par-dessus, sinon la jambe s'ouvre en
        // deux des que le genou plie.
        var cuisse = Piece(hancheClient, "Cuisse");
        var mollet = Piece(genouClient, "Mollet");
        Check("la cuisse descend jusqu'au genou",
              cuisse != null && genouClient != null &&
              cuisse.localPosition.y - cuisse.localScale.y <= genouClient.localPosition.y + 0.001f);
        Check("le mollet remonte par-dessus le genou",
              mollet != null && mollet.localPosition.y + mollet.localScale.y >= -0.001f);
        Check("une rotule bouche l'articulation",
              genouClient != null && Compte(genouClient, "Rotule", true) == 1);

        Check("il a un visage", oeil != null);
        Check("les yeux regardent devant", oeil != null && oeil.localPosition.z > 0.1f);
        Check("il a des fesses", fesse != null);
        Check("elles sont bien derriere", fesse != null && fesse.localPosition.z < -0.05f);
        Check("il a deux mains", mains == 2);

        // on en observe plusieurs : ils ne doivent pas etre habilles pareil
        var vus = new List<Color>();
        for (int i = 0; i < 60 * 120 && vus.Count < 6; i++)
        {
            Frames(1);
            foreach (var cl in TousLes<Client>())
            {
                var c = CouleurHaut(cl);
                bool connu = false;
                foreach (var v in vus) if (v.r == c.r && v.g == c.g && v.b == c.b) connu = true;
                if (!connu) vus.Add(c);
            }
        }
        Check("les clients ne sont pas tous habilles pareil", vus.Count >= 3);

        // --- morphologies et coiffures ---
        var carrures = new List<float>();
        bool cheveuxLongs = false, cheveuxCourts = false;
        for (int i = 0; i < 60 * 150 && (carrures.Count < 3 || !cheveuxLongs || !cheveuxCourts); i++)
        {
            Frames(1);
            foreach (var cl in TousLes<Client>())
            {
                var corps = cl.transform.Find("Corps");
                if (corps == null) continue;
                foreach (var e in corps.Enfants)
                {
                    if (e.gameObject.name == "Torse")
                    {
                        float l = e.localScale.x;
                        bool connue = false;
                        foreach (var c in carrures) if (Mathf.Abs(c - l) < 0.03f) connue = true;
                        if (!connue) carrures.Add(l);
                    }
                    if (e.gameObject.name == "Natte") cheveuxLongs = true;
                }
                if (Compte(corps, "Natte", false) == 0) cheveuxCourts = true;
            }
        }
        Check("les carrures varient, des maigres aux corpulents", carrures.Count >= 3);
        Check("des femmes ont les cheveux longs", cheveuxLongs);
        Check("des hommes ont les cheveux courts", cheveuxCourts);

        // --- la commande dure trois secondes, tiroir ouvert ---
        // On guette un client QUI VIENT de commencer : en attraper un deja
        // engage fausserait la mesure du delai.
        // Tant qu'aucun caissier n'est embauche, le joueur doit tenir la
        // caisse lui-meme : on le laisse donc au comptoir.
        Placer(joueur, comptoir.transform.position);
        Client client = null;
        Client dernier = comptoir.Premier;
        for (int i = 0; i < 60 * 200 && client == null; i++)
        {
            Frames(1);
            var p = comptoir.Premier;
            if (p == null || p == dernier) continue;
            // nouveau client au comptoir : on le prend des sa premiere image
            for (int k = 0; k < 10 && !p.CommandeCommencee; k++) Frames(1);
            client = p;
        }
        Check("un nouveau client entame sa commande", client != null);
        Check("il annonce sa commande dans une bulle",
              TexteBulle(client) == "x" + client.Pizzas);

        // Un Text tronque n'affiche rien du tout : c'est ce qui rendait le
        // nombre invisible. On verifie le reglage, pas seulement le contenu.
        Text champ = null;
        foreach (var e in client.transform.Enfants)
            if (e.gameObject.name == "Bulle")
                foreach (var f in e.Enfants)
                    if (f.gameObject.name == "Nombre") champ = f.gameObject.GetComponent<Text>();
        Check("le nombre a une police", champ != null && champ.font != null);
        Check("le nombre ne peut pas etre tronque",
              champ.horizontalOverflow == HorizontalWrapMode.Overflow &&
              champ.verticalOverflow == VerticalWrapMode.Overflow);
        Check("la bulle demande entre une et trois pizzas",
              client.Pizzas >= Reglages.PizzasParClientMin && client.Pizzas <= Reglages.PizzasParClientMax);
        Check("le tiroir s'ouvre des le debut de la commande", caisse.EstOuverte);
        Check("la commande n'est pas finie tout de suite", !client.CommandeFinie);

        Secondes(Reglages.DureeCommande - 0.5f);
        Check("la commande dure bien les secondes prevues", !client.CommandeFinie);
        Secondes(1f);
        Check("la commande s'acheve apres le delai", client.CommandeFinie);

        // Les attentes suivent la cadence du four : en dur, elles cassent des
        // que l'equilibrage bouge.
        int avantVente = Banque.Solde;
        Placer(joueur, four.Sortie.transform.position);
        Secondes(Reglages.DureeCuisson * 4f);         // recharge
        Placer(joueur, comptoir.transform.position);
        Secondes(Reglages.DelaiClient + 10f);         // sert, encaisse les liasses sur place
        Check("servir des clients rapporte de l'argent", Banque.Solde > avantVente);
        // le tiroir se referme quand plus personne n'est au comptoir
        int garde1 = 0;
        while (comptoir.Premier != null && garde1++ < 60 * 90) Frames(1);
        Check("le tiroir se referme quand le client repart",
              comptoir.Premier != null || !caisse.EstOuverte);
        // la bulle suit ce qu'il reste a remettre, pour tous les clients
        bool bullesJustes = true;
        foreach (var cl in TousLes<Client>())
        {
            var texte = TexteBulle(cl);
            // un client qui s'en va n'a plus de commande a annoncer, servi ou
            // parti d'impatience
            if (cl.SEnVa) { if (texte != "") bullesJustes = false; continue; }
            if (texte != "x" + cl.Restant) bullesJustes = false;
        }
        Check("les bulles affichent ce qu'il reste a servir", bullesJustes);

        Check("le prix suit le bareme",
              (Banque.Solde - avantVente) % Reglages.PrixPizza == 0);

        // --- ramassage d'une liasse, teste isolement ---
        // La liasse tombe avec un decalage aleatoire : on se met assez loin
        // pour qu'aucun tirage ne la mette a portee.
        Placer(joueur, new Vector3(-9f, 0f, -12f));
        Secondes(0.2f);
        int avantLiasse = Banque.Solde;
        Billet.Lacher(new Vector3(-9f, 1f, -6f), 42, joueur);
        Secondes(0.5f);
        Check("la liasse attend au sol tant qu'on ne passe pas dessus",
              Banque.Solde == avantLiasse && TousLes<Billet>().Count >= 1);
        Placer(joueur, new Vector3(-9f, 0f, -6.7f));
        Secondes(1.5f);
        Check("marcher sur la liasse encaisse", Banque.Solde == avantLiasse + 42);
        Check("la liasse disparait une fois prise", TousLes<Billet>().Count == 0);

        // --- embauche du caissier ---
        Placer(joueur, new Vector3(0f, 0f, -9f));      // le joueur quitte le comptoir
        Secondes(1f);
        Banque.Encaisser(Reglages.PrixCaissier);
        int avantEmbauche = Banque.Solde;
        Placer(joueur, zoneCaissier.transform.position);
        Secondes(0.5f);
        Check("l'embauche preleve a la dalle", Banque.Solde < avantEmbauche);
        Check("un indice annonce ce qui reste a payer",
              Hud.Indice != null && Hud.Indice.Contains("reste"));

        Secondes(Reglages.PrixCaissier / Reglages.DebitAchat + 2f);
        var employe = Trouver("Caissier");
        Check("le caissier apparait une fois paye", employe != null && employe.activeSelf);
        Check("le comptoir sait qu'un caissier tient la caisse", comptoir.CaissierPresent);
        Check("la dalle d'embauche disparait", zoneCaissier.gameObject.Detruit);

        // --- le caissier fait la navette jusqu'au four ---
        var navetteur = employe.GetComponent<Caissier>();
        Placer(joueur, new Vector3(-9f, 0f, -9f));     // le joueur ne touche a rien
        comptoir.Stock.Vider();
        var pasDuCaissier = employe.GetComponent<Demarche>();
        // On observe les deux faits jusqu'a les avoir vus tous les deux :
        // s'arreter au premier ratait la marche quand le caissier portait deja
        // une pizza a la premiere image.
        bool aPorte = false, aMarche = false, aEmballe = false, aTouche = false, reserveEntamee = false;
        // le geste decompose : carton au rond rouge, puis au rond vert, puis
        // une pizza rapportee du four et glissee dedans
        bool auRouge = false, auVert = false, aRapporteUnePizza = false, aGarni = false;
        bool tropDePizzasNues = false;
        int porteeMax = 0, nuesMax = 0, pretesAvant = 0;
        for (int i = 0; i < 60 * 120 &&
             !(aPorte && aMarche && aEmballe && aTouche && auRouge && auVert && aGarni); i++)
        {
            Frames(1);
            porteeMax = Mathf.Max(porteeMax, navetteur.Portees);
            if (navetteur.Portees > 0) aPorte = true;
            if (pasDuCaissier != null && pasDuCaissier.Allure > 0.2f) aMarche = true;
            if (navetteur.PorteeEmballee) aEmballe = true;
            if (table.Reserve < Reglages.BoitesEnReserve) reserveEntamee = true;

            if (table.Preparation.Nombre > 0) auRouge = true;
            if (table.Assemblage.Nombre > 0) auVert = true;
            if (navetteur.PorteeNue)
            {
                aRapporteUnePizza = true;
                nuesMax = Mathf.Max(nuesMax, navetteur.Portees);
                if (navetteur.Portees > 1) tropDePizzasNues = true;
            }
            if (navetteur.Pretes > pretesAvant) aGarni = true;
            pretesAvant = navetteur.Pretes;

            var versTable = employe.transform.position - table.PointDeTravail;
            versTable.y = 0f;
            if (versTable.magnitude < 0.8f) aTouche = true;
        }
        Check("le caissier va chercher les pizzas au four", aPorte);
        // Trois en main, pas davantage : la valeur est ecrite en clair ici,
        // sinon le test suivrait le reglage au lieu de le tenir.
        Check("il n'en prend que trois a la fois",
              porteeMax == 3 && porteeMax == Reglages.CapacitePorteeCaissier);
        Check("il marche pour de bon, membres animes", aMarche);
        Check("il fait un detour par le plan de mise en boite", aTouche);
        Check("il sort un carton sur le rond rouge", auRouge);
        Check("il le fait glisser sur le rond vert", auVert);
        Check("il rapporte une pizza du four", aRapporteUnePizza);
        Check("une seule pizza a la fois, sa boite l'attend",
              !tropDePizzasNues && nuesMax == 1);
        Check("il la met dans la boite qui attendait", aGarni);
        Check("il pioche dans la reserve de cartons", reserveEntamee);
        Check("il porte des pizzas en boite en repartant", aEmballe);

        int deposees = comptoir.Stock.Nombre;
        for (int i = 0; i < 60 * 30 && comptoir.Stock.Nombre <= deposees; i++) Frames(1);
        Check("il les depose sur le comptoir", comptoir.Stock.Nombre > deposees);
        Check("ce qu'il depose est en boite", comptoir.Stock.SommetEmballe);
        // Empilees, mais au cordeau : c'est ce qui a ete demande. On attend
        // d'en avoir plusieurs — une pile d'une seule boite serait droite
        // meme avec un decalage par element.
        for (int i = 0; i < 60 * 40 && comptoir.Stock.Nombre < 3; i++) Frames(1);
        Check("le comptoir empile plusieurs boites", comptoir.Stock.Nombre >= 3);
        Check("les boites s'empilent bien droites au comptoir",
              ToutDroit(comptoir.Stock.transform));

        // --- le caissier sert sans le joueur ---
        // On garnit d'abord le comptoir, sinon le test passerait faute de stock
        // plutot que grace au caissier.
        Placer(joueur, four.Sortie.transform.position);
        Secondes(Reglages.DureeCuisson * 4f);
        Placer(joueur, comptoir.transform.position);
        Secondes(2f);
        Check("le comptoir a du stock avant le test", comptoir.Stock.Nombre > 0);

        Placer(joueur, new Vector3(-9f, 0f, -9f));     // le joueur s'en va vraiment
        Check("le joueur est bien hors de portee de la caisse",
              !joueur.EstPres(comptoir.transform.position, Reglages.RayonService));
        // Le caissier sert, mais les liasses restent au sol : c'est au joueur
        // d'aller les ramasser. Ce sont donc elles qu'il faut compter, pas le
        // solde, qui ne bouge qu'au ramassage.
        int liassesAvant = TousLes<Billet>().Count;
        int servisAvant = comptoir.Stock.Nombre;
        Secondes(Reglages.DelaiClient + Reglages.DureeCommande + 14f);
        // Un client servi par le caissier repart avec des cartons, pas avec
        // des pizzas nues posees les unes sur les autres.
        bool clientEnBoite = false, sacMelange = false, sacDeTravers = false;
        int sacLePlusGarni = 0;
        for (int i = 0; i < 60 * 40 && !(clientEnBoite && sacLePlusGarni >= 2); i++)
        {
            Frames(1);
            foreach (var cl in TousLes<Client>())
            {
                var sac = cl.transform.Find("Sac");
                if (sac == null) continue;
                int boites = Compte(sac, "Boite", false);
                if (boites > 0) clientEnBoite = true;
                if (Compte(sac, "Pizza", false) > 0) sacMelange = true;
                if (!ToutDroit(sac)) sacDeTravers = true;
                sacLePlusGarni = Mathf.Max(sacLePlusGarni, boites);
            }
        }
        Check("les clients repartent avec des boites", clientEnBoite);
        Check("un client en emporte plusieurs, l'une sur l'autre", sacLePlusGarni >= 2);
        Check("son sac ne melange jamais boites et pizzas nues", !sacMelange);
        Check("elles y sont empilees bien droites", !sacDeTravers);

        Check("le caissier sert les clients sans le joueur",
              TousLes<Billet>().Count > liassesAvant || comptoir.Stock.Nombre < servisAvant);

        // et le joueur encaisse en revenant marcher dessus
        int soldeAvant = Banque.Solde;
        var liasses = TousLes<Billet>();
        if (liasses.Count > 0)
        {
            Placer(joueur, liasses[0].transform.position);
            Secondes(1.5f);
        }
        Check("le joueur encaisse les liasses laissees par le caissier",
              liasses.Count == 0 || Banque.Solde > soldeAvant);

        var erreurs = new List<string>();
        foreach (var l in Debug.Journal) if (l.StartsWith("ERROR")) erreurs.Add(l);
        Check("aucune erreur Unity remontee", erreurs.Count == 0);
        foreach (var e in erreurs) Console.WriteLine("      " + e);

        Console.WriteLine(_echecs == 0 ? "\nTOUTE LA BOUCLE PASSE" : "\n" + _echecs + " ECHEC(S)");
        Environment.Exit(_echecs == 0 ? 0 : 1);
    }
}
