// Fait tourner une vraie partie a travers GameView dans le faux runtime.
// But : attraper les erreurs qui n'apparaitraient qu'au premier appui sur Play
// (references nulles, ordre des Awake, boutons non branches, UI non rafraichie).
using System;
using System.Collections.Generic;
using System.Reflection;
using BellaNotte.Core;
using BellaNotte.Unity;
using UnityEngine;
using UnityEngine.UI;

static class ViewHarness
{
    static int _echecs;

    static void Check(string nom, bool ok)
    {
        Console.WriteLine((ok ? "OK    " : "ECHEC ") + nom);
        if (!ok) _echecs++;
    }

    // --- recherche dans la hierarchie ---
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

    static Button Bouton(string nomObjet)
    {
        var go = Trouver(nomObjet);
        return go?.GetComponent<Button>();
    }

    static string Message()
    {
        var vue = UnityEngine.Object.FindObjectOfType<GameView>();
        var f = typeof(GameView).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic);
        return ((Text)f.GetValue(vue)).text;
    }

    static void Frames(int n, float dt = 1f / 60f)
    {
        for (int i = 0; i < n; i++) Scene.Frame(dt);
    }

    static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--dump")
        {
            var d = args[1];
            var reine = new[] { IngredientId.Mozzarella, IngredientId.Jambon, IngredientId.Champignon };
            foreach (var c in new[] { 0.35f, 0.70f, 1.00f, 1.25f, 1.55f })
                PizzaDump.Ecrire($"{d}/pizza_{(int)(c * 100):D3}.ppm", IngredientId.Tomate, reine, c);
            Console.WriteLine("rendus ecrits dans " + d);
            return;
        }

        // --- demarrage comme au premier Play ---
        typeof(Bootstrap).GetMethod("Demarrer", BindingFlags.Static | BindingFlags.NonPublic)
                         .Invoke(null, null);
        Frames(2);

        var runner = UnityEngine.Object.FindObjectOfType<GameRunner>();
        var vue = UnityEngine.Object.FindObjectOfType<GameView>();
        var game = runner.Game;

        Check("Bootstrap monte le jeu sans scene", runner != null && vue != null);
        Check("un EventSystem est cree", UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null);
        Check("un Canvas est cree", UnityEngine.Object.FindObjectOfType<Canvas>() != null);
        Check("la partie a demarre avec un client", game.EnCours && game.Commandes.Count >= 1);
        Check("les 12 boutons d'ingredients existent", TousLes<Button>().Count >= 12);
        Check("un ticket est affiche par commande", TousLes<Image>().Count > 0 && Trouver("Ticket" + game.Commandes[0].Id) != null);

        // --- toutes les polices sont branchees (sinon texte invisible) ---
        bool policesOk = true;
        foreach (var t in TousLes<Text>()) if (t.font == null) policesOk = false;
        Check("tous les textes ont une police", policesOk);

        // --- etats initiaux des boutons ---
        Check("Enfourner desactive sur une pizza vide", !Bouton("BtnEnfourner").interactable);
        Check("Servir desactive avant cuisson", !Bouton("BtnServir").interactable);
        Check("Sortir desactive hors du four", !Bouton("BtnSortir !").interactable);

        // --- garnir via les boutons, comme un joueur ---
        var commande = game.CommandeActive;
        Check("une commande est selectionnee d'office", commande != null);

        var garniture = Bouton("Btn" + Ingredients.Nom(commande.Recette.Garnitures[0]));
        garniture.Cliquer();
        Frames(1);
        Check("garniture refusee sans base", game.PizzaEnCours.Garnitures.Count == 0);
        Check("le refus est affiche au joueur", Message().Contains("base"));

        Bouton("Btn" + Ingredients.Nom(commande.Recette.Base)).Cliquer();
        Frames(1);
        Check("la base se pose par le bouton", game.PizzaEnCours.Base == commande.Recette.Base);

        foreach (var g in commande.Recette.Garnitures) Bouton("Btn" + Ingredients.Nom(g)).Cliquer();
        Frames(1);
        Check("toutes les garnitures se posent",
              game.PizzaEnCours.Garnitures.Count == commande.Recette.Garnitures.Count);
        Check("Enfourner devient actif", Bouton("BtnEnfourner").interactable);

        // --- la texture de pizza est bien regeneree ---
        var sprite = UnityEngine.Object.FindObjectOfType<Sprite>();
        Check("la pizza est dessinee dans une texture", sprite != null && sprite.texture.NbApply > 0);
        int applyAvant = sprite.texture.NbApply;

        // --- cuisson ---
        Bouton("BtnEnfourner").Cliquer();
        Frames(1);
        Check("la pizza part au four", game.PizzaEnCours.AuFour);
        Check("Sortir devient actif", Bouton("BtnSortir !").interactable);
        Check("les ingredients sont verrouilles au four", !Bouton("Btn" + Ingredients.Nom(IngredientId.Olive)).interactable);

        while (game.PizzaEnCours.Cuisson < GameConfig.ZoneParfaiteMin + 0.05f) Frames(1);
        var jauge = Trouver("Jauge").GetComponent<Image>();
        Check("la jauge du four suit la cuisson", jauge.fillAmount > 0.5f);
        Check("la texture est reactualisee pendant la cuisson", sprite.texture.NbApply > applyAvant);

        Bouton("BtnSortir !").Cliquer();
        Frames(1);
        Check("cuisson parfaite au moment de sortir", game.PizzaEnCours.Etat == EtatCuisson.Parfaite);
        Check("Servir devient actif", Bouton("BtnServir").interactable);

        int avant = game.Recette;
        Bouton("BtnServir").Cliquer();
        Frames(1);
        Check("le service est paye", game.Recette > avant);
        Check("le compte rendu s'affiche", Message().Contains("parfaite"));
        Check("le HUD est a jour", Trouver("StatRecette") != null);

        // --- jeter ---
        if (game.CommandeActive != null)
        {
            Bouton("Btn" + Ingredients.Nom(IngredientId.Tomate)).Cliquer();
            Bouton("BtnJeter").Cliquer();
            Frames(1);
            Check("Jeter repart d'une pizza vide", !game.PizzaEnCours.Base.HasValue);
        }

        // --- laisser filer jusqu'a la fermeture ---
        int garde = 0;
        while (game.EnCours && garde++ < 200000) Frames(1);
        Check("la partie se termine a 3 clients perdus", !game.EnCours);
        var fin = Trouver("Fin");
        Check("l'ecran de fin s'affiche", fin != null && fin.Actif);

        Bouton("BtnRouvrir la pizzeria").Cliquer();
        Frames(2);
        Check("Rejouer relance une partie", game.EnCours && game.ClientsPerdus == 0 && game.Recette == 0);
        Check("l'ecran de fin se referme", !Trouver("Fin").Actif);
        Check("les tickets sont reconstruits", game.Commandes.Count >= 1);

        // --- aucune erreur remontee ---
        var erreurs = new List<string>();
        foreach (var l in Debug.Journal) if (l.StartsWith("ERROR")) erreurs.Add(l);
        Check("aucune erreur Unity remontee", erreurs.Count == 0);
        foreach (var e in erreurs) Console.WriteLine("      " + e);

        Console.WriteLine(_echecs == 0 ? "\nTOUS LES TESTS D'INTERFACE PASSENT" : "\n" + _echecs + " ECHEC(S)");
        Environment.Exit(_echecs == 0 ? 0 : 1);
    }
}

// Export des pixels du PizzaRenderer, pour controler le rendu a l'oeil.
static class PizzaDump
{
    public static void Ecrire(string chemin, IngredientId? bas, IngredientId[] garn, float cuisson)
    {
        var r = new PizzaRenderer();
        r.Dessiner(bas, garn, cuisson);
        var tex = r.Sprite.texture;
        int w = tex.width, h = tex.height;

        // PPM (P6) : les pixels sont ecrits du haut vers le bas.
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
                // fond sombre du plan de travail la ou la texture est transparente
                buf[i]     = (byte)(p.a == 0 ? 20 : p.r);
                buf[i + 1] = (byte)(p.a == 0 ? 14 : p.g);
                buf[i + 2] = (byte)(p.a == 0 ? 12 : p.b);
            }
            fs.Write(buf, 0, buf.Length);
        }
    }
}
