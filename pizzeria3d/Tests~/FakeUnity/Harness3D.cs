// Joue une partie complete a travers le vrai code du jeu, dans le faux runtime.
// But : verifier la boucle — produire, porter, deposer, servir, encaisser,
// debloquer — avant de la lancer dans l'editeur.
using System;
using System.Collections.Generic;
using Pizzeria3D;
using UnityEngine;

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

    /// <summary>Teleporte le joueur : les tests de deplacement sont separes.</summary>
    static void Placer(Joueur j, Vector3 p) => j.transform.position = p;

    static void Main()
    {
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
        var camera = UnityEngine.Object.FindObjectOfType<UnityEngine.Camera>();

        Check("la scene se monte sans editeur", joueur != null && four != null && comptoir != null);
        Check("camera isometrique orthographique", camera != null && camera.orthographic);
        Check("la caisse demarre a zero", Banque.Solde == 0);
        Check("une zone d'embauche attend derriere la caisse",
              zoneCaissier != null && zoneCaissier.Restant == Reglages.PrixCaissier);
        Check("le caissier n'est pas encore la",
              Trouver("Caissier") != null && !Trouver("Caissier").activeSelf && !comptoir.CaissierPresent);
        Check("le tiroir-caisse est ferme au depart", caisse != null && !caisse.EstOuverte);

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
        Check("sa course est bornee a un", manette.Direction.magnitude <= 1.001f);
        Secondes(0.5f);
        var apres = joueur.transform.position;
        Check("le doigt fait avancer le joueur", (apres - depart).magnitude > 1f);
        Check("le deplacement suit l'axe isometrique",
              apres.x > depart.x + 0.5f && apres.z > depart.z + 0.5f);
        Input.Boutons[0] = false;
        Frames(2);
        Check("la manette disparait au relachement", !manette.EstVisible);
        Check("et sa direction retombe a zero",
              manette.Direction.x == 0f && manette.Direction.y == 0f);
        var arret = joueur.transform.position;
        Secondes(0.3f);
        Check("le joueur s'arrete quand on lache", (joueur.transform.position - arret).magnitude < 0.01f);

        // --- le four produit ---
        Placer(joueur, new Vector3(0f, 0f, -6f));    // loin, pour laisser le stock monter
        Secondes(Reglages.DureeCuisson * 3.5f + 1f);
        Check("le four produit des pizzas", four.Sortie.Nombre >= 3);

        // --- ramassage ---
        Placer(joueur, four.Sortie.transform.position);
        Secondes(1.2f);
        Check("le joueur charge la pile sur sa tete", joueur.Portee.Nombre >= 3);
        Check("le stock du four baisse d'autant", four.Sortie.Nombre < 3);

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

        // --- un client arrive, patiente, est servi ---
        Check("des clients font la queue", comptoir.TailleFile >= 1 || TousLes<Client>().Count >= 1);

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
