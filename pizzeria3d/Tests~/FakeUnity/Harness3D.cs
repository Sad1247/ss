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
    static bool Fenetres(string prefixe, float debut, float fin, bool surX = false,
                         string aussi = null)
    {
        var pos = new List<float>();
        foreach (var o in UnityEngine.Object.Tous)
            if (o is GameObject go && !go.Detruit &&
                (go.name.StartsWith(prefixe) || (aussi != null && go.name == aussi)))
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

    /// <summary>
    /// L'etendue d'un mur, tous ses morceaux reunis : un mur perce n'est plus
    /// un bloc unique mais des trumeaux, des alleges et des linteaux.
    /// </summary>
    static bool Etendue(string prefixe, bool surX, out float min, out float max, out float haut,
                        out int morceaux)
    {
        min = 9999f; max = -9999f; haut = -9999f; morceaux = 0;
        foreach (var o in UnityEngine.Object.Tous)
        {
            if (!(o is GameObject go) || go.Detruit || !go.name.StartsWith(prefixe)) continue;
            if (go.GetComponent<MeshRenderer>() == null) continue;
            var p = go.transform.position; var e = go.transform.localScale;
            float a = (surX ? p.x : p.z) - (surX ? e.x : e.z) * 0.5f;
            float b = (surX ? p.x : p.z) + (surX ? e.x : e.z) * 0.5f;
            min = Mathf.Min(min, a); max = Mathf.Max(max, b);
            haut = Mathf.Max(haut, p.y + e.y * 0.5f);
            morceaux++;
        }
        return morceaux > 0;
    }

    /// <summary>Compte a n'importe quelle profondeur : une pizza posee sur un
    /// plateau est petite-fille de la pile, pas fille.</summary>
    /// <summary>
    /// Position, taille et cap d'un objet, rotations et echelles des parents
    /// comprises : le faux Transform, lui, les ignore. Sans cette composition,
    /// un objet retourne par son porteur se mesurait a sa place d'origine.
    /// </summary>
    static void Composer(Transform t, out Vector3 p, out Vector3 e, out Quaternion rot)
    {
        p = t.localPosition;
        e = t.localScale;
        rot = t.localRotation;
        for (var q = t.parent; q != null; q = q.parent)
        {
            var s = q.localScale;
            p = new Vector3(p.x * s.x, p.y * s.y, p.z * s.z);
            p = q.localRotation * p;
            rot = q.localRotation * rot;
            e = new Vector3(e.x * s.x, e.y * s.y, e.z * s.z);
            p = new Vector3(p.x + q.localPosition.x, p.y + q.localPosition.y,
                            p.z + q.localPosition.z);
        }
    }

    /// <summary>Un pixel d'une texture, compare a une couleur attendue.</summary>
    static bool Pixel(Texture2D tex, int x, int y, int rgb)
    {
        if (tex == null || tex.Pixels == null) return false;
        var p = tex.Pixels[y * tex.width + x];
        return p.r == ((rgb >> 16) & 255) && p.g == ((rgb >> 8) & 255) && p.b == (rgb & 255);
    }

    /// <summary>Pose l'aiguille sur l'heure demandee, sans attendre le temps reel.</summary>
    static void PoserLHeure(Horloge horloge, int heure)
    {
        float but = heure * 60f;
        if (horloge.MinutesDepuisMinuit > but) but += 24f * 60f;
        horloge.Avancer(but - horloge.MinutesDepuisMinuit);
        Frames(2);
    }

    /// <summary>Un de ces textes contient-il ce fragment ?</summary>
    static bool Contient(List<Text> textes, string fragment)
    {
        foreach (var t in textes) if (t.text.Contains(fragment)) return true;
        return false;
    }

    /// <summary>Ou se trouve vraiment un objet, une fois tout compose.</summary>
    static Vector3 PositionReelle(Transform t)
    {
        Composer(t, out var p, out _, out _);
        return p;
    }

    /// <summary>
    /// L'emprise au sol de tout ce qui pend a un point du plan : c'est elle
    /// qui dit si deux tas se rentrent dedans, pas la distance entre leurs
    /// piquets.
    /// </summary>
    static bool Empreinte(Transform t, out float x0, out float x1, out float z0, out float z1)
    {
        x0 = 9999f; x1 = -9999f; z0 = 9999f; z1 = -9999f;
        bool trouve = false;
        foreach (var o in UnityEngine.Object.Tous)
        {
            if (!(o is GameObject go) || go.Detruit) continue;
            if (go.GetComponent<MeshRenderer>() == null) continue;
            bool sien = false;
            for (var m = go.transform; m != null; m = m.parent) if (m == t) { sien = true; break; }
            if (!sien) continue;

            Composer(go.transform, out var p, out var e, out var rot);

            // Les huit coins, tournes puis projetes : la formule en cosinus
            // ne valait que pour un cap autour de Y.
            foreach (float sx in new[] { -0.5f, 0.5f })
            foreach (float sy in new[] { -0.5f, 0.5f })
            foreach (float sz in new[] { -0.5f, 0.5f })
            {
                var coin = p + rot * new Vector3(e.x * sx, e.y * sy, e.z * sz);
                x0 = Mathf.Min(x0, coin.x); x1 = Mathf.Max(x1, coin.x);
                z0 = Mathf.Min(z0, coin.z); z1 = Mathf.Max(z1, coin.z);
            }

            trouve = true;
        }
        return trouve;
    }

    /// <summary>Deux emplacements du plan qui empietent l'un sur l'autre.</summary>
    static bool SeChevauchent(Transform a, Transform b)
    {
        if (!Empreinte(a, out float ax0, out float ax1, out float az0, out float az1)) return false;
        if (!Empreinte(b, out float bx0, out float bx1, out float bz0, out float bz1)) return false;
        return ax0 < bx1 && bx0 < ax1 && az0 < bz1 && bz0 < az1;
    }

    static int Dedans(Transform parent, string nom)
    {
        int n = 0;
        foreach (var e in parent.Enfants)
        {
            if (e.gameObject.name.Contains(nom)) n++;
            n += Dedans(e, nom);
        }
        return n;
    }

    /// <summary>Combien d'objets vivants portent ce prefixe.</summary>
    static int Objets(string prefixe)
    {
        int n = 0;
        foreach (var o in UnityEngine.Object.Tous)
            if (o is GameObject go && !go.Detruit && go.name.StartsWith(prefixe)) n++;
        return n;
    }

    /// <summary>N'importe quel morceau d'un mur, pour son plan et son epaisseur.</summary>
    static GameObject UnPan(string prefixe)
    {
        foreach (var o in UnityEngine.Object.Tous)
            if (o is GameObject go && !go.Detruit && go.name.StartsWith(prefixe) &&
                go.GetComponent<MeshRenderer>() != null) return go;
        return null;
    }

    /// <summary>Le morceau de mur qui passe au-dessus d'un point donne.</summary>
    static GameObject PanAuDessus(string prefixe, float x, float hauteurMini)
    {
        foreach (var o in UnityEngine.Object.Tous)
        {
            if (!(o is GameObject go) || go.Detruit || !go.name.StartsWith(prefixe)) continue;
            var p = go.transform.position; var e = go.transform.localScale;
            if (Mathf.Abs(p.x - x) > e.x * 0.5f) continue;
            if (p.y - e.y * 0.5f < hauteurMini - 0.05f) continue;
            return go;
        }
        return null;
    }

    /// <summary>Distance en x du plus proche objet portant ce prefixe.</summary>
    static float EcartX(string prefixe, float x)
    {
        float mini = 999f;
        foreach (var o in UnityEngine.Object.Tous)
            if (o is GameObject go && !go.Detruit && go.name.StartsWith(prefixe))
                mini = Mathf.Min(mini, Mathf.Abs(go.transform.position.x - x));
        return mini;
    }

    /// <summary>Un objet de ce nom figure-t-il parmi les murs escamotables ?</summary>
    static bool Escamote(MursDiscrets d, string prefixe)
    {
        if (d == null) return false;
        foreach (var m in d.Murs)
            if (m != null && m.name.StartsWith(prefixe)) return true;
        return false;
    }

    static bool TousVisibles(MursDiscrets d)
    {
        if (d == null) return false;
        foreach (var m in d.Murs)
            if (m == null || !m.GetComponent<MeshRenderer>().enabled) return false;
        return true;
    }

    static bool AucunVisible(MursDiscrets d)
    {
        if (d == null) return false;
        foreach (var m in d.Murs)
            if (m == null || m.GetComponent<MeshRenderer>().enabled) return false;
        return true;
    }

    /// <summary>Le sac d'un client : il pend a ses mains, comme le personnel.</summary>
    static Transform SacDe(Client c)
    {
        var mains = c.transform.Find("Corps/Mains/Sac");
        return mains != null ? mains : c.transform.Find("Sac");
    }

    /// <summary>La dalle d'achat d'un prix donne, ou null.</summary>
    static ZoneAchat ZoneDePrix(int prix)
    {
        foreach (var z in TousLes<ZoneAchat>()) if (z.Prix == prix) return z;
        return null;
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
        DumpTexture(tex, chemin);
    }

    /// <summary>
    /// La couleur sous laquelle dessiner un materiau : la sienne, ou la
    /// moyenne de son image quand il en porte une.
    /// </summary>
    static Color Teinte(Material m)
    {
        if (!(m.mainTexture is Texture2D tex) || tex.Pixels == null) return m.color;

        float r = 0f, v = 0f, b = 0f, poids = 0f;
        foreach (var p in tex.Pixels)
        {
            float a = p.a / 255f;
            r += p.r * a; v += p.g * a; b += p.b * a; poids += a;
        }
        if (poids <= 0f) return m.color;
        return new Color(r / poids / 255f, v / poids / 255f, b / poids / 255f, 1f);
    }

    /// <summary>
    /// Ecrit la mise en page d'un canevas : une ligne par element, avec son
    /// cadre resolu en pixels. Le faux moteur ne calcule aucune mise en page —
    /// on refait ici les regles d'ancrage d'Unity, les seules qu'on utilise.
    /// </summary>
    static void DumpUI(string chemin)
    {
        var canvas = Trouver(Environment.GetEnvironmentVariable("DUMP_UI_CIBLE") ?? "EcranBureauCanvas");
        using (var f = new System.IO.StreamWriter(chemin))
        {
            f.WriteLine("1080 1920");
            EcrireUI(f, canvas.transform, 0f, 0f, 1080f, 1920f);
        }
        Console.WriteLine("interface ecrite dans " + chemin);
    }

    static void EcrireUI(System.IO.StreamWriter f, Transform t, float cx, float cy,
                         float largeur, float hauteur)
    {
        foreach (var e in t.Enfants)
        {
            if (!e.gameObject.ActifDansHierarchie) continue;
            var rt = e as RectTransform;
            float w = largeur, h = hauteur, x = cx, y = cy;
            if (rt != null)
            {
                w = (rt.anchorMax.x - rt.anchorMin.x) * largeur + rt.sizeDelta.x;
                h = (rt.anchorMax.y - rt.anchorMin.y) * hauteur + rt.sizeDelta.y;
                float ax = (rt.anchorMin.x + rt.anchorMax.x) * 0.5f - 0.5f;
                float ay = (rt.anchorMin.y + rt.anchorMax.y) * 0.5f - 0.5f;
                x = cx + ax * largeur + rt.anchoredPosition.x + (0.5f - rt.pivot.x) * w;
                y = cy + ay * hauteur + rt.anchoredPosition.y + (0.5f - rt.pivot.y) * h;
            }

            var image = e.gameObject.GetComponent<Image>();
            var texte = e.gameObject.GetComponent<Text>();
            if (image != null)
            {
                var c = image.color;
                f.WriteLine($"boite|{x}|{y}|{w}|{h}|{Hex(c)}|0|");
            }
            if (texte != null && texte.text.Length > 0)
                f.WriteLine($"texte|{x}|{y}|{w}|{h}|{Hex(texte.color)}|{texte.fontSize}|"
                            + (int)texte.alignment + "|" + texte.text);

            EcrireUI(f, e, x, y, w, h);
        }
    }

    static string Hex(Color c)
        => $"{(int)(c.r * 255):x2}{(int)(c.g * 255):x2}{(int)(c.b * 255):x2}";

    /// <summary>Ecrit une texture en PPM, pour la regarder hors du jeu.</summary>
    static void DumpTexture(Texture2D tex, string chemin)
    {
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

    /// <summary>
    /// Ecrit la geometrie de la scene, pour la dessiner hors du jeu et
    /// regarder ce que le joueur verra. Une ligne par objet visible :
    /// forme, position, taille, rotation en Y, couleur.
    /// </summary>
    static void DumpScene(string chemin)
    {
        using (var f = new System.IO.StreamWriter(chemin))
            foreach (var o in UnityEngine.Object.Tous)
            {
                if (!(o is GameObject go) || go.Detruit || !go.ActifDansHierarchie) continue;
                var r = go.GetComponent<MeshRenderer>();
                if (r == null || !r.enabled || r.sharedMaterial == null) continue;

                Composer(go.transform, out var p, out var e, out var rot);

                var c2 = Teinte(r.sharedMaterial);
                int rgb = ((int)(c2.r * 255) << 16) | ((int)(c2.g * 255) << 8) | (int)(c2.b * 255);
                int alpha = (int)(Mathf.Clamp01(c2.a) * 255);
                // La rotation entiere, en quaternion : un cap autour de Y ne
                // suffit plus depuis qu'un capot d'ordinateur se rabat.
                f.WriteLine($"{go.name}|{go.Primitive}|{p.x:0.###} {p.y:0.###} {p.z:0.###}|" +
                            $"{e.x:0.###} {e.y:0.###} {e.z:0.###}|" +
                            $"{rot.x:0.####} {rot.y:0.####} {rot.z:0.####} {rot.w:0.####}|" +
                            $"{rgb:x6}{alpha:x2}");
            }
        Console.WriteLine("scene ecrite dans " + chemin);
    }

    /// <summary>L'angle en Y d'un quaternion, seul axe utilise par le decor.</summary>
    static float Angle(Quaternion q)
        => 2f * Mathf.Rad2Deg * (float)Math.Atan2(q.y, q.w);

    static void Main()
    {
        if (Environment.GetEnvironmentVariable("DUMP_PIZZA") is string chemin && chemin.Length > 0)
        {
            DumpPizza(chemin);
            return;
        }

        if (Environment.GetEnvironmentVariable("DUMP_LOGO") is string cheminLogo
            && cheminLogo.Length > 0)
        {
            DumpTexture(Logo.Materiau().mainTexture as Texture2D, cheminLogo);
            return;
        }

        var dumpUI = Environment.GetEnvironmentVariable("DUMP_UI");

        bool dumpScene = Environment.GetEnvironmentVariable("DUMP_SCENE") is string ds && ds.Length > 0;

        // Une scene Unity neuve arrive avec une Main Camera et une lumiere :
        // on reproduit ces conditions, c'est ce que le joueur aura.
        var camScene = new GameObject("Main Camera").AddComponent<UnityEngine.Camera>();
        new GameObject("Directional Light").AddComponent<Light>();

        Batisseur.Monter();
        Frames(2);

        // Le temps est arrete pour la plus grande partie des essais : sinon la
        // journee defile sous eux, la pizzeria ferme a vingt et une heures et
        // les clients cessent d'arriver au milieu d'une verification. Le
        // service, lui, s'eprouve plus bas, l'horloge en main.
        var pendule = UnityEngine.Object.FindObjectOfType<Horloge>();
        if (pendule != null) pendule.Figee = true;

        Check("la camera de la scene est reutilisee, pas doublee",
              TousLes<UnityEngine.Camera>().Count == 1);
        // Trois lumieres : le soleil qui porte les ombres, un appoint froid,
        // un contre-jour. Une seule donnait des aplats sans relief.
        var lumieres = TousLes<Light>();
        Check("la scene est eclairee a trois lumieres", lumieres.Count == 3);
        int porteuses = 0;
        foreach (var l in lumieres) if (l.shadows != LightShadows.None) porteuses++;
        Check("une seule d'entre elles porte les ombres", porteuses == 1);
        // C'est bien celle de la scene qui sert de soleil : creee en double,
        // l'ancienne resterait sans ombres dans son coin.
        Check("la lumiere de la scene est reutilisee, pas doublee",
              UnityEngine.Object.FindObjectOfType<Light>().shadows == LightShadows.Soft);

        // --- l'horloge de la pizzeria ---
        var horloge = UnityEngine.Object.FindObjectOfType<Horloge>();
        Check("la pizzeria a son horloge", horloge != null);
        Check("la pizzeria ouvre le matin",
              Reglages.HeureOuverture >= 6 && Reglages.HeureOuverture <= 11);
        Check("et l'horloge part de la, au premier jour",
              horloge != null && horloge.Jour == 1
              && horloge.Heures == Reglages.HeureOuverture && horloge.Minutes == 0);
        Check("l'heure s'ecrit en heures et minutes, sans les secondes",
              horloge != null && horloge.Heure.Length == 5 && horloge.Heure[2] == ':');

        // La vitesse annoncee, mesuree sur trente secondes de manette.
        float avantHorloge = horloge.SecondesDepuisMinuit;
        horloge.Figee = false;
        Secondes(30f);
        horloge.Figee = true;
        float ecoule = horloge.SecondesDepuisMinuit - avantHorloge;
        Check("le temps s'ecoule a la vitesse annoncee",
              Mathf.Abs(ecoule - 30f * Reglages.SecondesParSeconde) < 5f);
        // Une journee entiere en dix minutes de manette au plus : au-dela, on
        // attend son soir au lieu de le jouer.
        Check("une journee tient en dix minutes de manette",
              24f * 3600f / Reglages.SecondesParSeconde <= 600.5f);
        Check("et il en faut tout de meme plusieurs pour la traverser",
              24f * 3600f / Reglages.SecondesParSeconde > 120f);

        // L'heure s'affiche en haut de l'ecran, et c'est bien celle-la.
        var cadran = Trouver("Pendule");
        Check("l'heure s'affiche en haut de l'ecran", cadran != null);
        var texteHeure = cadran != null ? Piece(cadran.transform, "Texte") : null;
        var luHeure = texteHeure != null ? texteHeure.gameObject.GetComponent<Text>() : null;
        Check("elle est calee en haut, au centre",
              cadran != null
              && cadran.GetComponent<RectTransform>().anchorMin.y == 1f
              && Mathf.Abs(cadran.GetComponent<RectTransform>().anchorMin.x - 0.5f) < 0.01f);
        // La pendule et la carte de la caisse ne se marchent pas dessus.
        var carteCaisse = Trouver("CarteCaisse");
        if (cadran != null && carteCaisse != null)
        {
            var rCad = cadran.GetComponent<RectTransform>();
            var rCar = carteCaisse.GetComponent<RectTransform>();
            // la pendule est centree en haut, la carte accrochee au coin droit
            float droiteCadran = 540f + rCad.anchoredPosition.x + rCad.sizeDelta.x * 0.5f;
            float gaucheCarte = 1080f + rCar.anchoredPosition.x - rCar.sizeDelta.x;
            Check("elle ne chevauche pas la carte de la caisse", droiteCadran < gaucheCarte);
        }
        else Check("elle ne chevauche pas la carte de la caisse", false);

        Check("et c'est bien l'heure de l'horloge",
              luHeure != null && luHeure.text == horloge.Affichage);

        // minuit passe, on change de jour
        // On pousse l'aiguille jusqu'a la barre plutot que d'attendre douze
        // minutes de manette : c'est le meme chemin de code que l'ecoulement.
        int jourAvantMinuit = horloge.Jour;
        horloge.Avancer(24f * 60f - horloge.MinutesDepuisMinuit - 0.5f);
        Check("avant minuit, on est encore le meme jour",
              horloge.Jour == jourAvantMinuit && horloge.Heures == 23);
        horloge.Avancer(1f);
        Check("passe minuit, le jour suivant commence", horloge.Jour == jourAvantMinuit + 1);
        Check("et l'heure repart du debut", horloge.Heures == 0);

        // On remet l'aiguille a l'ouverture pour la suite : le reste des
        // essais se joue en plein service, horloge arretee.
        horloge.Avancer(Reglages.HeureOuverture * 60f);

        // Sans EventSystem, aucun bouton d'interface ne recoit de clic : le
        // bouton d'avance rapide etait bien la, mais mort sous le doigt.
        Check("la scene a un EventSystem",
              UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null);
        Check("et son module d'entree",
              UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.StandaloneInputModule>() != null);

        // --- l'avance rapide ---
        var avance = UnityEngine.Object.FindObjectOfType<Acceleration>();
        Check("le jeu a son avance rapide", avance != null);
        Check("il tourne a vitesse normale au depart",
              !avance.Rapide && Time.timeScale == 1f);

        // La touche bascule, et c'est tout le jeu qui accelere : le temps
        // d'Unity lui-meme, donc le four et les clients avec l'horloge.
        Input.Enfoncer(avance.Touche);
        Frames(1);
        Input.Relacher(avance.Touche);
        Frames(1);
        Check("une touche passe le jeu en avance rapide",
              avance.Rapide && Mathf.Abs(Time.timeScale - 4f) < 0.01f);

        // Mesure de face : en avance rapide, l'horloge avance quatre fois plus.
        horloge.Figee = false;
        float avantRapide = horloge.SecondesDepuisMinuit;
        Secondes(10f);
        float enRapide = horloge.SecondesDepuisMinuit - avantRapide;

        Input.Enfoncer(avance.Touche);
        Frames(1);
        Input.Relacher(avance.Touche);
        Frames(1);
        Check("la meme touche revient a la vitesse normale",
              !avance.Rapide && Time.timeScale == 1f);

        float avantNormal = horloge.SecondesDepuisMinuit;
        Secondes(10f);
        float enNormal = horloge.SecondesDepuisMinuit - avantNormal;
        horloge.Figee = true;
        // Tolerance relative : une image d'avance ou de retard pese
        // maintenant plusieurs secondes de jeu.
        Check("dix secondes en rapide valent quatre fois dix secondes normales",
              Mathf.Abs(enRapide - 4f * enNormal) < 0.02f * enRapide);

        // Plus de bouton de vitesse a l'ecran : la touche seule commande.
        Check("aucun bouton de vitesse n'encombre le Hud", Trouver("BoutonVitesse") == null);


        Check("l'anticrenelage est pousse", QualitySettings.antiAliasing >= 8);
        Check("les ombres portent jusqu'au fond de la salle",
              QualitySettings.shadowDistance >= 40f);
        Check("et se decoupent finement",
              QualitySettings.shadowResolution == ShadowResolution.VeryHigh
              && QualitySettings.shadowCascades >= 4);
        // Sans assez de lumieres par pixel, l'appoint et le contre-jour
        // passent en calcul par sommet : ils disparaissent.
        Check("les trois lumieres sont calculees par pixel",
              QualitySettings.pixelLightCount >= 3);
        Check("l'ambiante est un degrade, pas un aplat",
              RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Trilight);
        Check("son ciel est plus froid que le rebond du sol",
              RenderSettings.ambientSkyColor.b > RenderSettings.ambientGroundColor.b);

        // Le metal a sa matiere : peint du meme fini mat que le carton, un
        // plan de travail en inox ne ressemble a rien.
        var inox = Trouver("Comptoir");
        var dessusInox = inox != null ? Piece(inox.transform, "Dessus") : null;
        var matiere = dessusInox != null
            ? dessusInox.gameObject.GetComponent<MeshRenderer>().sharedMaterial : null;
        Check("le plan de travail a un fini metallique",
              matiere != null && matiere.GetFloat("_Metallic") > 0.5f
                              && matiere.GetFloat("_Smoothness") > 0.3f);
        Check("mais le decor peint reste mat",
              Bloc.Peinture(Bloc.Machine).GetFloat("_Smoothness") < 0.2f);
        Check("et le metal poli ne deteint pas sur la peinture partagee",
              matiere != Bloc.Peinture(Bloc.Metal));
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
        var sol = Trouver("Sol");
        float gMin, gMax, gHaut; int gPans;
        bool murGauche = Etendue("MurGauche", false, out gMin, out gMax, out gHaut, out gPans);
        Check("le mur de gauche est monte", murGauche && sol != null);
        if (murGauche && sol != null)
        {
            float solDebut = sol.transform.position.z - sol.transform.localScale.z * 0.5f;
            float solFin   = sol.transform.position.z + sol.transform.localScale.z * 0.5f;
            Check("il court jusqu'au bout du dallage",
                  gMin <= solDebut + 0.01f && gMax >= solFin - 0.01f);
            Check("le mur de gauche est vitre sur toute sa longueur",
                  Fenetres("VitreGauche", gMin, gMax));
            // Perce : sans trous, les vitres ne montrent que du mur. Un mur
            // perce a des alleges sous ses baies et des linteaux dessus.
            Check("il est perce de baies",
                  Objets("MurGaucheAllege") >= 5 && Objets("MurGaucheLinteau") >= 5);
        }
        else
        {
            Check("il court jusqu'au bout du dallage", false);
            Check("le mur de gauche est vitre sur toute sa longueur", false);
            Check("il est perce de baies", false);
        }

        float fMin, fMax, fHaut; int fPans;
        bool murFond = Etendue("MurFond", true, out fMin, out fMax, out fHaut, out fPans);
        Check("le mur du fond est monte", murFond && sol != null);
        if (murFond && sol != null)
        {
            float solGauche = sol.transform.position.x - sol.transform.localScale.x * 0.5f;
            float solDroite = sol.transform.position.x + sol.transform.localScale.x * 0.5f;
            Check("il court d'un bord a l'autre du dallage",
                  fMin <= solGauche + 0.01f && fMax >= solDroite - 0.01f);
            Check("il est ouvert sur toute sa longueur",
                  Fenetres("VitreFond", fMin, fMax, true, "Porte"));
            Check("il est perce de baies",
                  Objets("MurFondAllege") >= 5 && Objets("MurFondLinteau") >= 5);
        }
        else
        {
            Check("il court d'un bord a l'autre du dallage", false);
            Check("il est ouvert sur toute sa longueur", false);
            Check("il est perce de baies", false);
        }

        // --- la porte, a cote du plan de mise en boite ---
        var porte = Trouver("Porte");
        var panFond = UnPan("MurFond");
        Check("une porte est posee dans le mur du fond", porte != null && panFond != null);
        if (porte != null && panFond != null && table != null)
        {
            var jambage = Piece(porte.transform, "Jambage");
            var imposte = Piece(porte.transform, "Imposte");
            Check("son cadre a deux montants et une imposte",
                  Compte(porte.transform, "Jambage", true) == 2 && imposte != null);

            float bas = porte.transform.position.y + jambage.localPosition.y
                      - jambage.localScale.y * 0.5f;
            Check("elle descend jusqu'au sol", Mathf.Abs(bas) < 0.05f);
            Check("elle est plus haute qu'une fenetre", jambage.localScale.y > 1.8f);

            // Le cadre ne doit rien boucher : un panneau plein a la place du
            // passage, c'est le blanc qu'on voyait au lieu de la piece.
            bool passageLibre = true;
            foreach (var e in porte.transform.Enfants)
            {
                if (e.gameObject.name == "Gond") continue;
                bool auMilieu = Mathf.Abs(e.localPosition.x) < 0.6f;
                bool aHauteurDePassage = e.localPosition.y > 0.2f && e.localPosition.y < 2.2f;
                if (auMilieu && aHauteurDePassage) passageLibre = false;
            }
            Check("le cadre laisse le passage libre", passageLibre);
            // rien ne traine non plus au sol en travers du pas de la porte
            Check("et le pas de la porte est nu",
                  Piece(porte.transform, "Seuil") == null);

            Check("elle est plaquee sur la face interieure du mur",
                  porte.transform.position.z + jambage.localPosition.z
                      < panFond.transform.position.z);
            Check("aucune vitre ne lui passe dessus",
                  EcartX("VitreFond", porte.transform.position.x) > 1.5f);

            // a cote du plan, pas a l'autre bout de la salle
            float bordDuPlan = table.transform.position.x + 1.9f;
            Check("elle jouxte le plan de mise en boite",
                  porte.transform.position.x > bordDuPlan &&
                  porte.transform.position.x - bordDuPlan < 1.5f);

            // Le pan de mur au-dessus : la coupure montait jusqu'au toit.
            // le morceau de mur qui passe au-dessus de la porte
            var linteau = PanAuDessus("MurFond", porte.transform.position.x, 2f);
            float basLinteau = linteau != null
                ? linteau.transform.position.y - linteau.transform.localScale.y * 0.5f : -9f;
            float hautLinteau = linteau != null
                ? linteau.transform.position.y + linteau.transform.localScale.y * 0.5f : -9f;
            float hautMur = fHaut;
            Check("un linteau ferme le mur au-dessus de la porte", linteau != null);
            Check("il repose sur le haut de la porte",
                  Mathf.Abs(basLinteau - (porte.transform.position.y + imposte.localPosition.y
                                          + imposte.localScale.y * 0.5f)) < 0.2f);
            Check("et il monte jusqu'au haut du mur", Mathf.Abs(hautLinteau - hautMur) < 0.05f);
        }
        else
        {
            Check("son cadre a deux montants et une imposte", false);
            Check("elle descend jusqu'au sol", false);
            Check("elle est plus haute qu'une fenetre", false);
            Check("le cadre laisse le passage libre", false);
            Check("et le pas de la porte est nu", false);
            Check("elle est plaquee sur la face interieure du mur", false);
            Check("aucune vitre ne lui passe dessus", false);
            Check("elle jouxte le plan de mise en boite", false);
            Check("un linteau ferme le mur au-dessus de la porte", false);
            Check("il repose sur le haut de la porte", false);
            Check("et il monte jusqu'au haut du mur", false);
        }

        Check("on ne passe pas la porte avant de l'avoir payee",
              porte != null && Obstacles.Bloque(porte.transform.position, Reglages.RayonJoueur));
        // Elle occupe le coin du batiment : son pan droit et le mur du fond
        // finissent au meme endroit, sinon le mur depasse dans le vide.
        var pieceCachee = Trouver("PetitePiece");
        if (pieceCachee != null && murFond)
        {
            float boutDuMur = fMax;
            float boutDeLaPiece = -99f;
            foreach (var e in pieceCachee.transform.Enfants)
                if (e.gameObject.name == "MurPiece")
                    boutDeLaPiece = Mathf.Max(boutDeLaPiece,
                                              pieceCachee.transform.position.x + e.localPosition.x
                                              + e.localScale.x * 0.5f);
            Check("la piece va jusqu'au bout du batiment",
                  Mathf.Abs(boutDeLaPiece - boutDuMur) < 0.05f);
        }
        else Check("la piece va jusqu'au bout du batiment", false);

        // Les deux dallages doivent se rejoindre franchement : a un cheveu
        // pres, une marche apparait dans le beige et de l'herbe passe sous
        // les murs.
        var solPiece = Trouver("SolPiece");
        if (solPiece != null && sol != null && pieceCachee != null)
        {
            var ps = solPiece.transform.position; var es = solPiece.transform.localScale;
            var pg = sol.transform.position; var eg = sol.transform.localScale;
            Check("les deux sols sont a la meme hauteur",
                  Mathf.Abs((ps.y + es.y * 0.5f) - (pg.y + eg.y * 0.5f)) < 0.01f);
            Check("celui de la piece rejoint celui de la salle",
                  ps.z - es.z * 0.5f < pg.z + eg.z * 0.5f);
            Check("et il finit au meme bord",
                  Mathf.Abs((ps.x + es.x * 0.5f) - (pg.x + eg.x * 0.5f)) < 0.01f);

            float murOuest = 99f, murEst = -99f;
            foreach (var e in pieceCachee.transform.Enfants)
                if (e.gameObject.name == "MurPiece")
                {
                    murOuest = Mathf.Min(murOuest, pieceCachee.transform.position.x
                                                   + e.localPosition.x - e.localScale.x * 0.5f);
                    murEst = Mathf.Max(murEst, pieceCachee.transform.position.x
                                               + e.localPosition.x + e.localScale.x * 0.5f);
                }
            Check("il passe sous les murs de la piece",
                  ps.x - es.x * 0.5f <= murOuest + 0.01f && ps.x + es.x * 0.5f >= murEst - 0.01f);
        }
        else
        {
            Check("les deux sols sont a la meme hauteur", false);
            Check("celui de la piece rejoint celui de la salle", false);
            Check("et il finit au meme bord", false);
            Check("il passe sous les murs de la piece", false);
        }

        Check("la piece attend, cachee",
              Trouver("PetitePiece") != null && !Trouver("PetitePiece").activeSelf);
        // Cachee ne suffit pas : batie sans etre eteinte d'abord, elle liberait
        // le pas de sa porte et repoussait la limite du terrain des la
        // construction — on entrait dans une piece qu'on n'avait pas payee.
        Check("et son pas de porte est encore barre",
              Obstacles.Bloque(new Vector3(4.56f, 0f, 7.2f), Reglages.RayonJoueur));
        Check("le terrain s'arrete avant elle", Obstacles.DemiTerrainZ < 9f);

        var dallePiece = ZoneDePrix(Reglages.PrixPetitePiece);
        Check("une dalle a 400 attend devant la porte",
              dallePiece != null && dallePiece.Restant == Reglages.PrixPetitePiece);
        if (dallePiece != null && porte != null)
        {
            var d = dallePiece.transform.position;
            Check("elle est devant la porte, cote salle",
                  Mathf.Abs(d.x - porte.transform.position.x) < 0.6f &&
                  d.z < porte.transform.position.z && porte.transform.position.z - d.z < 2.5f);
            Check("le joueur peut s'y tenir", !Obstacles.Bloque(d, Reglages.RayonJoueur));
        }
        else
        {
            Check("elle est devant la porte, cote salle", false);
            Check("le joueur peut s'y tenir", false);
        }

        // La cour a ete debarrassee : la pile de cartons masquait le mur du
        // fond, la palette trainait au sol. Ni l'une ni l'autre ne doit
        // laisser d'obstacle invisible derriere elle.
        Check("plus de pile de cartons dans la cour",
              Trouver("Caisse0") == null && Trouver("Caisse1") == null && Trouver("Caisse2") == null);
        Check("et plus d'obstacle invisible a sa place",
              Obstacles.Resoudre(new Vector3(6.6f, 0f, 4.2f), new Vector3(0f, 0f, 0.6f),
                                 Reglages.RayonJoueur).z > 4.7f);
        Check("plus de palette au sol", Trouver("Palette") == null);
        Check("ni d'obstacle a sa place",
              !Obstacles.Bloque(new Vector3(-7f, 0f, 5.6f), Reglages.RayonJoueur));

        // --- les vitres laissent passer le regard ---
        // Un alpha ne suffit pas en URP : sans les reglages de surface, la
        // vitre reste opaque. On controle donc le materiau, pas la couleur.
        int vitrees = 0;
        bool vitreOpaque = false;
        foreach (var o in UnityEngine.Object.Tous)
        {
            if (!(o is GameObject go) || go.Detruit) continue;
            if (!go.name.StartsWith("Vitre") && go.name != "Battant") continue;
            var r = go.GetComponent<MeshRenderer>();
            if (r == null || r.sharedMaterial == null) continue;

            var m = r.sharedMaterial;
            vitrees++;
            bool transparente = m.color.a < 0.9f
                             && m.GetFloat("_Surface") == 1f
                             && m.GetFloat("_ZWrite") == 0f
                             && m.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")
                             && m.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent;
            if (!transparente) vitreOpaque = true;
        }
        Check("la pizzeria a ses vitres", vitrees >= 10);
        Check("on voit a travers toutes les vitres", !vitreOpaque);

        // --- la poubelle ---
        var poubelle = Trouver("Poubelle");
        Check("une poubelle est posee dans la salle", poubelle != null);
        Check("on ne la traverse pas",
              poubelle != null &&
              Obstacles.Bloque(poubelle.transform.position, Reglages.RayonJoueur));

        // --- la table de la salle ---
        var tableSalle = UnityEngine.Object.FindObjectOfType<TableRepas>();
        Check("une table est dressee dans la salle", tableSalle != null);
        if (tableSalle != null)
        {
            // Meme facture que le comptoir de la caisse : caisson bleu et
            // dessus metallique. C'est un comptoir ou l'on mange, plus une
            // table ronde.
            var caisson = CouleurDe(tableSalle.transform, "Caisson");
            Check("son caisson est bleu comme celui de la caisse",
                  caisson.r == Bloc.Machine.r && caisson.g == Bloc.Machine.g
                  && caisson.b == Bloc.Machine.b);
            var dessus = tableSalle != null ? Piece(tableSalle.transform, "Plateau") : null;
            var matiereDessus = dessus != null
                ? dessus.gameObject.GetComponent<MeshRenderer>().sharedMaterial : null;
            Check("son dessus est metallique",
                  matiereDessus != null && matiereDessus.GetFloat("_Metallic") > 0.5f);
            Check("il a ses deux tabourets", Compte(tableSalle.transform, "Chaise", true) == 2);
            Check("elle offre une place assise", tableSalle.Siege != null);
            Check("elle est libre au depart", tableSalle.EstLibre);
            Check("on ne traverse pas la table",
                  Obstacles.Bloque(tableSalle.transform.position, Reglages.RayonJoueur));
            Check("elle ne gene pas la file",
                  Mathf.Abs(tableSalle.transform.position.x - comptoir.PlaceDeLaFile(0).x) > 2f);
        }
        else
        {
            Check("son caisson est bleu comme celui de la caisse", false);
            Check("son dessus est metallique", false);
            Check("il a ses deux tabourets", false);
            Check("elle offre une place assise", false);
            Check("elle est libre au depart", false);
            Check("on ne traverse pas la table", false);
            Check("elle ne gene pas la file", false);
        }

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

        // Les deux ronds de travail : le rouge d'abord, le vert au milieu du
        // plan — c'est la que ce qui est pret attend d'etre emporte.
        if (table != null && table.Preparation != null && table.Assemblage != null)
        {
            var rouge = table.Preparation.transform.localPosition;
            var vert = table.Assemblage.transform.localPosition;
            Check("le plan a ses deux ronds de travail", true);
            Check("le rouge est avant le vert", rouge.x < vert.x);
            // Au milieu, mais pas au centimetre : les quatre emplacements du
            // dessus doivent tenir cote a cote sans se toucher, et c'est cette
            // contrainte-la qui fixe le pas.
            Check("le vert est au milieu du plan", Mathf.Abs(vert.x) < 0.6f);
            Check("les deux sont poses sur le dessus",
                  Mathf.Abs(rouge.y - vert.y) < 0.01f && vert.y > 1f);
        }
        else
        {
            Check("le plan a ses deux ronds de travail", false);
            Check("le rouge est avant le vert", false);
            Check("le vert est au milieu du plan", false);
            Check("les deux sont poses sur le dessus", false);
        }

        Check("des plateaux propres attendent sur le plan",
              table != null && table.PlateauxEnPile != null &&
              table.PlateauxEnPile.Nombre == Reglages.PlateauxSurLePlan);
        // Propres, donc vides : une pizza n'apparait qu'une fois posee dessus.
        Check("ils sont vides tant qu'on n'y a rien pose",
              table != null && Dedans(table.PlateauxEnPile.transform, "Pizza") == 0);
        Check("ils attendent a l'autre bout du plan, cote droit",
              table != null &&
              table.PlateauxEnPile.transform.localPosition.x > 1f &&
              table.PlateauxEnPile.transform.localPosition.x
                  > table.Boites.transform.localPosition.x);
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
        float flexionMax = 0f, pencheMax = 0f;
        bool genouALEnvers = false;
        var buste = joueur.transform.Find("Corps");
        for (int i = 0; i < 120; i++)
        {
            Frames(1);
            if (buste != null) pencheMax = Mathf.Max(pencheMax, buste.localRotation.x);
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

        // Le buste penche dans le sens de la marche, et c'est la racine qui
        // porte le cap : si les deux etaient sur le meme objet, l'inclinaison
        // ecraserait la direction du regard a chaque image. On releve le
        // maximum sur la course : arrive au bord du dallage, il s'arrete et
        // se redresse.
        Check("le buste penche vers l'avant en marchant", pencheMax > 0.02f);
        Check("le cap est porte par la racine, pas par le buste",
              Mathf.Abs(joueur.transform.rotation.y) > 0.001f &&
              Mathf.Abs(buste.localRotation.y) < 0.001f);

        Input.Boutons[0] = false;
        Frames(2);
        Secondes(0.8f);
        Check("le balancement s'arrete quand il s'arrete", demarche.Allure < 0.05f);
        Check("et il se redresse", buste != null && buste.localRotation.x < 0.01f);
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

        // Et il glisse le long d'un mur au lieu de s'y coller. On part dans le
        // couloir entre le mur de gauche et le four, seul endroit du fond qui
        // ne soit ni dans le four ni dans le plan de mise en boite.
        Placer(joueur, new Vector3(-7.2f, 0f, 6f));
        var avantGlisse = joueur.transform.position;
        PousserVers(joueur, new Vector3(-5.8f, 0f, 9f), 1.5f);   // en biais vers le mur du fond
        // glisser, c'est avancer lateralement TOUT EN etant plaque au mur :
        // verifier le seul deplacement en x laisserait passer un joueur qui
        // n'a jamais touche le mur.
        Check("le joueur glisse le long du mur",
              Mathf.Abs(joueur.transform.position.x - avantGlisse.x) > 0.5f &&
              joueur.transform.position.z > 6.3f);

        // --- taille et place du four ---
        var maconnerie = Piece(four.transform, "Maconnerie");
        var socleFour = maconnerie != null ? Piece(maconnerie, "Socle") : null;
        Check("le four a rapetisse", maconnerie != null && maconnerie.localScale.x < 0.95f);
        var planFond = UnPan("MurFond");
        if (socleFour != null && planFond != null)
        {
            float dosDuFour = four.transform.position.z
                            + (socleFour.localPosition.z + socleFour.localScale.z * 0.5f)
                              * maconnerie.localScale.z;
            float faceDuMur = planFond.transform.position.z
                            - planFond.transform.localScale.z * 0.5f;
            Check("il est adosse au mur du fond", faceDuMur - dosDuFour < 0.2f && dosDuFour < faceDuMur);
        }
        else Check("il est adosse au mur du fond", false);

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
        // ses boites pendent a ses mains, comme celles du personnel
        var mainsClient = premierClient.transform.Find("Corps/Mains");
        Check("il porte ses pizzas dans les mains",
              mainsClient != null && SacDe(premierClient) != null &&
              SacDe(premierClient).parent == mainsClient);

        // Ils flanent : le pas ne doit jamais partir a fond, sinon ils ont
        // l'air de courir vers le comptoir.
        float allureClientMax = 0f, allureClientEnMarche = 0f;
        bool clientDeTravers = false, clientOriente = false;
        var ouEtaient = new Dictionary<Client, Vector3>();
        var deTravers = new Dictionary<Client, int>();
        for (int i = 0; i < 240; i++)
        {
            Frames(1);
            foreach (var cl in TousLes<Client>())
            {
                var pas = cl.GetComponent<Demarche>();
                if (pas == null) continue;
                allureClientMax = Mathf.Max(allureClientMax, pas.Allure);
                if (pas.Allure > 0.05f)
                    allureClientEnMarche = Mathf.Max(allureClientEnMarche, pas.Allure);

                // Marcher de profil ou a reculons se voit tout de suite : le
                // regard doit suivre le deplacement.
                if (ouEtaient.TryGetValue(cl, out var avant2))
                {
                    var course2 = cl.transform.position - avant2;
                    course2.y = 0f;
                    if (course2.magnitude > 0.02f && !cl.Attable)
                    {
                        var vise = cl.transform.rotation * Vector3.forward;
                        vise.y = 0f;
                        float accord = Vector3.Dot(vise.normalized, course2.normalized);
                        if (accord > 0.9f) clientOriente = true;

                        // Un demi-tour prend quelques images : ce qui compte
                        // est de ne pas marcher de travers DURABLEMENT.
                        int compte;
                        deTravers.TryGetValue(cl, out compte);
                        compte = accord < 0.3f ? compte + 1 : 0;
                        deTravers[cl] = compte;
                        if (compte > 45) clientDeTravers = true;
                    }
                }
                ouEtaient[cl] = cl.transform.position;
            }
        }
        // Le temps passe : celui qu'on observait a pu etre servi et repartir.
        var encoreLa = TousLes<Client>();
        Check("la file ne s'est pas videe pendant l'observation", encoreLa.Count > 0);
        if (encoreLa.Count > 0) premierClient = encoreLa[0];

        Check("les clients marchent pour de bon", allureClientEnMarche > 0.25f);
        Check("ils regardent ou ils vont", !clientDeTravers && clientOriente);
        Check("mais ils ne courent pas", allureClientMax < 0.8f);
        Check("ils avancent moins vite que le personnel",
              Reglages.VitesseClient < Reglages.VitesseCaissier &&
              Reglages.VitesseClient < Reglages.VitesseJoueur);

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
        // On suit CETTE liasse : d'autres trainent maintenant sur la table de
        // la salle, laissees par les clients qui mangent sur place.
        var liasse = Billet.Lacher(new Vector3(-9f, 1f, -6f), 42, joueur);
        Secondes(0.5f);
        Check("la liasse attend au sol tant qu'on ne passe pas dessus",
              Banque.Solde == avantLiasse && !liasse.gameObject.Detruit);
        Placer(joueur, new Vector3(-9f, 0f, -6.7f));
        Secondes(1.5f);
        Check("marcher sur la liasse encaisse", Banque.Solde == avantLiasse + 42);
        Check("la liasse disparait une fois prise", liasse.gameObject.Detruit);

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

        // --- achat de la petite piece ---
        var avantPiece = Trouver("PetitePiece");
        float terrainAvant = Obstacles.DemiTerrainZ;
        var gondAvant = avantPiece != null ? avantPiece.GetComponent<PetitePiece>().Gond : null;
        float angleAvant = gondAvant != null ? gondAvant.localRotation.y : 0f;

        Banque.Encaisser(Reglages.PrixPetitePiece);
        Placer(joueur, dallePiece.transform.position);
        Secondes(Reglages.PrixPetitePiece / Reglages.DebitAchat + 2f);

        var laPiece = Trouver("PetitePiece");
        Check("la piece s'ouvre une fois payee", laPiece != null && laPiece.activeSelf);
        Check("la dalle de la piece disparait", dallePiece.gameObject.Detruit);
        Check("le pas de la porte se libere",
              porte != null && !Obstacles.Bloque(porte.transform.position, Reglages.RayonJoueur));
        Check("le terrain s'etend jusqu'au fond de la piece",
              Obstacles.DemiTerrainZ > terrainAvant + 2f);
        Check("le vantail s'ouvre",
              gondAvant != null && Mathf.Abs(gondAvant.localRotation.y - angleAvant) > 0.1f);
        // Il doit rentrer dans la piece : vers la salle, il se plantait en
        // travers du passage, blanc et bien visible.
        var battant = gondAvant != null ? Piece(gondAvant, "Battant") : null;
        float zBattant = battant != null
            ? gondAvant.localPosition.z + (gondAvant.localRotation * battant.localPosition).z : -9f;
        Check("il s'ouvre du cote de la piece", zBattant > 0.3f);

        // et l'on peut vraiment y entrer
        var dansLaPiece = new Vector3(porte.transform.position.x, 0f,
                                      laPiece.transform.position.z);
        Check("on peut se tenir dans la piece",
              !Obstacles.Bloque(dansLaPiece, Reglages.RayonJoueur));
        // le pan du fond, a l'aplomb du centre : il ferme la piece quelle que
        // soit sa largeur
        Check("mais pas traverser ses murs",
              Obstacles.Bloque(new Vector3(laPiece.transform.position.x, 0f,
                                           laPiece.transform.position.z + 2.3f),
                               Reglages.RayonJoueur));

        // --- les murs qui cachent la piece s'escamotent ---
        // La vue est fixe : sans cela, le joueur passe la porte et disparait
        // derriere le mur, on joue a l'aveugle.
        var discrets = laPiece.GetComponent<MursDiscrets>();
        Check("la piece sait escamoter ses murs", discrets != null);
        Check("elle escamote ceux du cote de la camera",
              discrets != null && discrets.Murs.Length >= 4);
        // la vitre du pan escamote doit partir avec lui, sinon elle flotte
        Check("la vitre du pan de droite en fait partie", Escamote(discrets, "VitreFond"));

        Placer(joueur, new Vector3(0f, 0f, 0f));       // bien dans la salle
        Frames(2);
        Check("depuis la salle, les murs sont bien la", TousVisibles(discrets));

        // Devant la porte, cote salle, la piece doit encore etre entiere :
        // effaces trop tot, ses murs donnaient une piece amputee.
        Placer(joueur, new Vector3(porte.transform.position.x, 0f,
                                   porte.transform.position.z - 0.8f));
        Frames(2);
        Check("et devant la porte aussi", TousVisibles(discrets));

        Placer(joueur, dansLaPiece);
        Frames(2);
        Check("une fois dedans, ils s'effacent", AucunVisible(discrets));
        Check("mais la piece reste, elle", laPiece.activeSelf);

        // --- le bureau du patron, et son fauteuil ---
        var bureau = laPiece.GetComponent<Bureau>() ?? Trouver("Bureau")?.GetComponent<Bureau>();
        Check("la piece a son bureau", bureau != null);
        Check("et son fauteuil de direction", bureau != null && bureau.Siege != null);
        Check("le fauteuil a son pietement a roulettes",
              bureau != null && Dedans(bureau.Siege, "Roulette") == 5);
        Check("et son dossier haut",
              bureau != null && Dedans(bureau.Siege, "Dossier") == 1
                             && Dedans(bureau.Siege, "AppuieTete") == 1);
        // Le meuble arrete, le fauteuil non : sinon on ne peut pas aller s'y
        // asseoir.
        Check("le bureau barre le passage",
              bureau != null && Obstacles.Bloque(bureau.transform.position, Reglages.RayonJoueur));
        Check("mais on peut se tenir au fauteuil",
              bureau != null && !Obstacles.Bloque(bureau.Place, Reglages.RayonJoueur));

        // Il s'installe en s'arretant devant, sans rien avoir a apprendre.
        joueur.Portee.Vider();
        Placer(joueur, bureau.Place + new Vector3(0f, 0f, -0.5f));
        Secondes(0.5f);
        Check("le patron s'assoit a son bureau", joueur.Assis);
        var assiette = joueur.transform.position;
        Check("il est bien pose sur le fauteuil",
              Mathf.Abs(assiette.x - bureau.Place.x) < 0.05f &&
              Mathf.Abs(assiette.z - bureau.Place.z) < 0.05f &&
              Mathf.Abs(assiette.y - Reglages.HauteurAssise) < 0.01f);
        var pasDuPatron = joueur.GetComponent<Demarche>();
        Check("et il en a la pose", pasDuPatron != null && pasDuPatron.Assis);
        // Assis, il regarde son bureau — donc la camera : de dos, on ne
        // verrait rien de lui.
        var regard = joueur.transform.rotation * Vector3.forward;
        var versLePlan = bureau.transform.position - bureau.Place;
        Check("il fait face a son plan de travail",
              regard.x * versLePlan.x + regard.z * versLePlan.z > 0.9f * versLePlan.magnitude);

        // et la manette le remet debout : c'est la seule facon d'en sortir
        PousserVers(joueur, bureau.Place + new Vector3(-1.5f, 0f, -1.2f), 0.6f);
        Check("la manette le remet debout", !joueur.Assis);
        Check("il ne reste pas assis en l'air", joueur.transform.position.y == 0f);
        Check("il a quitte le fauteuil", !joueur.EstPres(bureau.Place, 0.3f));

        // Les mains pleines, il reste debout : la pile resterait suspendue.
        joueur.Portee.Ajouter();
        Placer(joueur, bureau.Place);
        Secondes(0.5f);
        Check("les mains pleines, il ne s'assoit pas", !joueur.Assis);
        joueur.Portee.Vider();

        // --- l'ecran de l'ordinateur, quand le patron s'assoit ---
        var ecranBureau = UnityEngine.Object.FindObjectOfType<EcranBureau>();
        Check("le bureau a son ecran d'ordinateur", ecranBureau != null);
        Check("ferme tant qu'il n'est pas assis", ecranBureau != null && !ecranBureau.Ouvert);

        var portable = UnityEngine.Object.FindObjectOfType<OrdinateurPortable>();
        Check("le bureau a son portable", portable != null);
        Check("il reste ferme tant que personne ne s'assoit",
              portable != null && portable.Ferme);

        joueur.Portee.Vider();
        Placer(joueur, bureau.Place);
        Secondes(0.2f);
        Check("assis, le capot n'est pas encore leve", joueur.Assis && !portable.Ouvert);
        Check("et l'ecran attend le capot", !ecranBureau.Ouvert);

        // Il l'ouvre de la main : le bras se tend avant que le capot bouge.
        bool mainVue = false, capotBougeApresLaMain = false, capotEnChemin = false;
        for (int i = 0; i < 60 * 3; i++)
        {
            Frames(1);
            if (portable.MainTendue) mainVue = true;
            if (portable.Ouverture > 0.01f && !mainVue) capotBougeApresLaMain = true;
            // Il se leve, il ne saute pas : sans cela on ne verrait jamais le
            // geste, seulement un ecran apparu d'un coup.
            if (portable.Ouverture > 0.05f && portable.Ouverture < 0.95f) capotEnChemin = true;
        }
        Check("il tend la main vers le capot", mainVue);
        Check("le capot ne se leve pas avant la main", !capotBougeApresLaMain);
        Check("le capot se leve progressivement", capotEnChemin);
        Check("le capot finit leve", portable.Ouvert);
        Check("et l'ecran s'affiche alors", joueur.Assis && ecranBureau.Ouvert);
        Check("il montre une ligne par employe",
              ecranBureau.Lignes == Personnel.Fiches.Count && ecranBureau.Lignes >= 2);

        // Ce que la fenetre affiche vient du registre, et non d'une copie
        // ecrite a la main quelque part.
        var lignesUI = new List<Text>();
        var fenetreUI = Trouver("Fenetre");
        if (fenetreUI != null)
            foreach (var e in fenetreUI.transform.Enfants)
            {
                var txt = e.gameObject.GetComponent<Text>();
                if (txt != null && txt.text.Length > 0) lignesUI.Add(txt);
            }
        Check("les salaires y sont ecrits",
              Contient(lignesUI, Reglages.SalaireCaissier + " / jour"));
        Check("et la masse salariale aussi",
              Contient(lignesUI, "Masse salariale : " + Personnel.MasseSalariale + " / jour"));

        // Chaque ligne dit ce que dit le registre, et rien d'autre.
        bool statutsFideles = true;
        for (int i = 0; i < Personnel.Fiches.Count; i++)
        {
            var attendu = Personnel.Fiches[i].EstEmbauche ? "En poste" : "Poste vacant";
            if (ecranBureau.Statut(i) != attendu) statutsFideles = false;
        }
        Check("chaque ligne affiche le statut du registre", statutsFideles);
        Check("le caissier embauche est en poste", Contient(lignesUI, "En poste"));
        Check("et la paie le compte",
              Personnel.MasseSalariale == Reglages.SalairePatron + Reglages.SalaireCaissier);

        // La regle de la paie, eprouvee de face sur une fiche d'essai : un
        // poste vacant ne se paie pas.
        int paieAvant = Personnel.MasseSalariale;
        bool posteTenu = false;
        Personnel.Inscrire("Essai", "Poste d'essai", 30, () => posteTenu);
        Check("un poste vacant ne compte pas dans la paie",
              Personnel.MasseSalariale == paieAvant);
        posteTenu = true;
        Check("une fois pourvu, il s'y ajoute",
              Personnel.MasseSalariale == paieAvant + 30);
        Personnel.Retirer("Essai");
        Check("la fiche d'essai est bien retiree", Personnel.MasseSalariale == paieAvant);

        // Les colonnes d'une meme ligne ne se marchent pas dessus : a
        // l'ecran, deux cadres qui se recouvrent, c'est du texte par-dessus
        // du texte.
        bool colonnesQuiSeCroisent = false;
        for (int a1 = 0; a1 < lignesUI.Count; a1++)
        for (int b1 = a1 + 1; b1 < lignesUI.Count; b1++)
        {
            var ra = lignesUI[a1].GetComponent<RectTransform>();
            var rb = lignesUI[b1].GetComponent<RectTransform>();
            if (Mathf.Abs(ra.anchoredPosition.y - rb.anchoredPosition.y) > 1f) continue;
            float ax0 = ra.anchoredPosition.x, ax1 = ax0 + ra.sizeDelta.x;
            float bx0 = rb.anchoredPosition.x, bx1 = bx0 + rb.sizeDelta.x;
            if (ax0 < bx1 && bx0 < ax1) colonnesQuiSeCroisent = true;
        }
        Check("ses colonnes ne se chevauchent pas", !colonnesQuiSeCroisent);

        // et il se referme des qu'il se leve
        PousserVers(joueur, bureau.Place + new Vector3(-1.5f, 0f, -1.2f), 0.6f);
        Frames(2);
        Check("il se referme quand il se leve", !joueur.Assis && !ecranBureau.Ouvert);
        Secondes(1.5f);
        Check("et le capot se rabat derriere lui", portable.Ferme);

        // --- l'ordinateur du bureau ---
        var ordi = Trouver("Ordinateur");
        Check("le bureau a son ordinateur", ordi != null);
        Check("avec son ecran, son clavier et son pave tactile",
              ordi != null && Dedans(ordi.transform, "Ecran") == 1
                           && Dedans(ordi.transform, "Clavier") == 1
                           && Dedans(ordi.transform, "PaveTactile") == 1);
        // Un portable, pas une tour ni un ecran sur pied : le capot se releve
        // au-dessus de la coque, il ne tient pas sur une colonne.
        Check("c'est un portable, capot releve",
              ordi != null && Dedans(ordi.transform, "Capot") == 1
                           && Dedans(ordi.transform, "Charniere") == 1);
        // Pose sur le plan : ni encastre dans le bois, ni debordant dans le
        // vide a cote du meuble.
        var coque = ordi != null ? Piece(ordi.transform, "Socle") : null;
        Check("il repose sur le dessus du bureau",
              coque != null && PositionReelle(coque).y > bureau.transform.position.y + 0.80f
                            && PositionReelle(coque).y < bureau.transform.position.y + 0.90f);
        if (ordi != null && Empreinte(ordi.transform, out float ox0, out float ox1,
                                      out float oz0, out float oz1))
        {
            var plan = bureau.transform.position;
            Check("et tient entierement sur le plan",
                  ox0 > plan.x - 1.0f && ox1 < plan.x + 1.0f &&
                  oz0 > plan.z - 0.425f && oz1 < plan.z + 0.425f);
            // Pose droit : de biais, son emprise s'elargit des deux cotes a
            // la fois. Et assez petit pour ne pas manger le plan.
            Check("il est pose droit dans l'axe du meuble", ox1 - ox0 < 0.66f);
            Check("et il ne mange pas le plan", (ox1 - ox0) < 0.40f * 2.0f);
        }
        else
        {
            Check("et tient entierement sur le plan", false);
            Check("il est pose droit dans l'axe du meuble", false);
            Check("et il ne mange pas le plan", false);
        }
        // Ecran vers le mur, pave tactile vers le fauteuil : c'est celui qui
        // s'assoit qui doit voir l'affichage, pas la pelouse.
        var capot = ordi != null ? Piece(ordi.transform, "Capot") : null;
        var pave = ordi != null ? Piece(ordi.transform, "PaveTactile") : null;
        Check("l'ecran est tourne vers le mur",
              capot != null && pave != null
              && PositionReelle(capot).z < PositionReelle(pave).z);
        // et plus de sous-main noir sur le bois
        Check("le plan n'a plus son sous-main",
              Piece(bureau.transform, "SousMain") == null);

        // --- la porte du bureau s'ouvre a l'approche, et se referme derriere ---
        var porteDuBureau = laPiece.GetComponent<PetitePiece>();
        var devantLaPorte = new Vector3(porte.transform.position.x, 0f,
                                        porte.transform.position.z - 1.2f);
        Placer(joueur, new Vector3(0f, 0f, 0f));        // au milieu de la salle
        Secondes(1.5f);
        Check("la porte du bureau reste fermee au repos", !porteDuBureau.Ouverte);
        Placer(joueur, devantLaPorte);
        Secondes(1.5f);
        Check("elle s'ouvre quand le patron s'approche", porteDuBureau.Ouverte);
        // Un pas au-dela du seuil : il est encore tout pres de la porte, et
        // pourtant elle doit se refermer derriere lui.
        Placer(joueur, new Vector3(porte.transform.position.x, 0f,
                                   porte.transform.position.z + porteDuBureau.Profondeur + 0.3f));
        Secondes(1.5f);
        Check("et se referme derriere lui une fois entre", !porteDuBureau.Ouverte);
        Placer(joueur, bureau.Place);
        Secondes(1.5f);
        Check("elle reste fermee pendant qu'il est a son bureau", !porteDuBureau.Ouverte);
        Placer(joueur, devantLaPorte);
        Secondes(1.5f);
        Check("elle se rouvre pour le laisser ressortir", porteDuBureau.Ouverte);
        // Elle pivote, elle ne saute pas d'une image a l'autre.
        Placer(joueur, new Vector3(0f, 0f, 0f));
        Frames(2);
        Check("le battant pivote au lieu de claquer", porteDuBureau.Ouverte);
        Secondes(1.5f);
        Check("puis se referme", !porteDuBureau.Ouverte);
        // Il s'est assis en chemin : ecarte du fauteuil, il doit se relever
        // tout seul — sinon il traverse la suite des essais dans la pose du
        // fauteuil.
        Check("ecarte du fauteuil, il se releve", !joueur.Assis);

        Placer(joueur, new Vector3(0f, 0f, 0f));
        Frames(2);
        Check("et ils reviennent quand on ressort", TousVisibles(discrets));

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
        bool boiteVideAbandonnee = false;
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

            // Aucune boite vide ne doit attendre toute seule : un carton ne
            // sort qu'une fois la pizza en main, et il la recoit aussitot.
            int cartons = table.Preparation.Nombre + table.Assemblage.Nombre;
            if (cartons > navetteur.Pretes + (navetteur.PorteeNue ? 1 : 0))
                boiteVideAbandonnee = true;
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
        Check("aucune boite vide ne traine sur le plan", !boiteVideAbandonnee);
        Check("il pioche dans la reserve de cartons", reserveEntamee);
        Check("il porte des pizzas en boite en repartant", aEmballe);

        int deposees = comptoir.Stock.Nombre;
        for (int i = 0; i < 60 * 30 && comptoir.Stock.Nombre <= deposees; i++) Frames(1);
        Check("il les depose sur le comptoir", comptoir.Stock.Nombre > deposees);
        Check("ce qu'il depose est en boite", comptoir.Stock.SommetEmballe);
        // Empilees, mais au cordeau : c'est ce qui a ete demande. Il en faut
        // plusieurs — une pile d'une seule boite serait droite meme avec un
        // decalage par element. C'est le joueur qui garnit : le caissier, lui,
        // livre au compte-gouttes, une boite par voyage, et les clients les
        // reprennent au fur et a mesure.
        Placer(joueur, four.Sortie.transform.position);
        Secondes(Reglages.DureeCuisson * 5f);
        Placer(joueur, comptoir.transform.position);
        Secondes(3f);
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
                var sac = SacDe(cl);
                if (sac == null) continue;
                int boites = Compte(sac, "Boite", false);
                int nues = Compte(sac, "Pizza", false);
                if (boites > 0) clientEnBoite = true;
                // melanger, c'est porter les deux : celui qui mange sur place
                // repart avec une pizza nue, et c'est voulu
                if (boites > 0 && nues > 0) sacMelange = true;
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

        // --- les clients servis passent a table ---
        // La table peut trainer sale d'un essai precedent : on attend que le
        // caissier soit passe, sinon on mesure une salle condamnee.
        for (int i = 0; i < 60 * 120 && tableSalle.ADesOrdures; i++) Frames(1);
        Check("le caissier finit par debarrasser la table", !tableSalle.ADesOrdures);

        // Une seule place : le deuxieme servi doit repartir avec ses boites.
        bool unAttable = false, deuxALaFois = false, tableRendue = false, aEteOccupee = false;
        bool assisSurLaChaise = false, poseAssise = false;
        // sur place : une seule pizza, et pas de carton
        bool attableGourmand = false, attableEnBoite = false, attableSurPlateau = false;
        int repasServis = 0;
        bool pizzaEntiere = false, orduresLaissees = false, liasseSurLaTable = false;
        bool tableNettoyee = false, caissierEmporte = false, attableSurLesRestes = false;
        bool plateauSurLaTable = false, plateauAuComptoir = false;
        bool plateauEmballe = false, plateauTourne = false;
        bool plateauAuPlan = false, caissierPorteLePlateau = false, plateauNePasVide = false;
        bool plateauAvecDesBoites = false;
        bool mainsPleinesEnMangeant = false, vuMangerALaTable = false;
        var vueEntamee = new bool[Reglages.QuartiersParPizza + 1];
        // On observe sans s'arreter au premier repas : ce sont les clients
        // suivants qui disent si la regle tient, pas le premier.
        for (int i = 0; i < 60 * 150; i++)
        {
            Frames(1);
            int attables = 0;
            foreach (var cl in TousLes<Client>())
            {
                // on surveille la reservation, pas seulement l'assiette :
                // c'est des la commande que la place est retenue
                if (cl.SurPlace && cl.Pizzas != 1) attableGourmand = true;

                // Ce qu'on lui a servi se juge AVANT qu'il ne s'attable :
                // une fois assis, sa pizza est passee dans l'assiette.
                if (cl.SurPlace && !cl.Attable)
                {
                    var main = SacDe(cl);
                    if (main != null && Compte(main, "Boite", false) > 0) attableEnBoite = true;
                    if (main != null && Compte(main, "Plateau", false) > 0)
                    {
                        attableSurPlateau = true;
                        // dans la longueur : un quart de tour, face a celui
                        // qui le porte, et non en travers de son torse
                        foreach (var e in main.Enfants)
                            if (e.gameObject.name.StartsWith("Plateau") &&
                                Mathf.Abs(e.localRotation.y) > 0.5f) plateauTourne = true;
                    }
                }
                if (!cl.Attable) continue;
                attables++;
                unAttable = true;

                var ecart = cl.transform.position - tableSalle.Place;
                ecart.y = 0f;
                if (ecart.magnitude < 0.3f) assisSurLaChaise = true;
                var pas = cl.GetComponent<Demarche>();
                if (pas != null && pas.Assis) poseAssise = true;

                // en mangeant, il a les mains vides : sa pizza est dans
                // l'assiette, pas restee accrochee a ses paumes
                var paumes = SacDe(cl);
                if (paumes != null && paumes.childCount > 0) mainsPleinesEnMangeant = true;
                if (tableSalle.Quartiers > 0) vuMangerALaTable = true;

                if (cl.Pizzas != 1) attableGourmand = true;
            }
            if (attables > 1) deuxALaFois = true;

            // la pizza posee sur la table, puis mangee part par part
            if (Trouver("PlateauTable") != null) plateauSurLaTable = true;
            if (comptoir.Plateaux.Nombre > 0) plateauAuComptoir = true;
            // il se dresse la ou l'on emballe, puis voyage dans ses mains
            if (table.Preparation.SommetForme == Pile.Forme.PlateauVide ||
                table.Assemblage.SommetForme == Pile.Forme.PlateauVide) plateauAuPlan = true;
            // Un plateau qui attend sa pizza reste vide : il ne s'en cree pas
            // une par magie en sortant de la pile.
            if (table.Preparation.SommetForme == Pile.Forme.PlateauVide &&
                Dedans(table.Preparation.transform, "Pizza") > 0) plateauNePasVide = true;
            if (navetteur.PortePlateau)
            {
                caissierPorteLePlateau = true;
                // le plateau voyage seul : pas de cartons dans la meme fournee
                if (navetteur.Portees > 1) plateauAvecDesBoites = true;
            }
            // meme regle sur le plan : un plateau ne rejoint pas une fournee
            // de cartons deja commencee
            var enPreparation = table.Assemblage.SommetForme;
            if ((enPreparation == Pile.Forme.Plateau || enPreparation == Pile.Forme.PlateauVide)
                && table.Assemblage.Nombre > 1) plateauAvecDesBoites = true;
            // le plateau qui attend au comptoir n'est pas un carton deguise
            if (comptoir.Plateaux.SommetEmballe) plateauEmballe = true;
            int reste = tableSalle.Quartiers;
            if (reste == Reglages.QuartiersParPizza) pizzaEntiere = true;
            if (reste > 0 && reste < Reglages.QuartiersParPizza) vueEntamee[reste] = true;
            if (tableSalle.ADesOrdures)
            {
                orduresLaissees = true;
                // Personne ne mange devant les restes du precedent. Le
                // mangeur quitte sa chaise AVANT de les laisser : les deux ne
                // se chevauchent jamais d'une image.
                if (attables > 0) attableSurLesRestes = true;
            }
            else if (orduresLaissees) tableNettoyee = true;

            if (navetteur.PorteDesOrdures) caissierEmporte = true;

            // l'argent du repas se ramasse a la table, pas a la caisse
            foreach (var bl in TousLes<Billet>())
            {
                var ecartTable = bl.transform.position - tableSalle.transform.position;
                ecartTable.y = 0f;
                if (ecartTable.magnitude < 1.2f) liasseSurLaTable = true;
            }

            if (!tableSalle.EstLibre) aEteOccupee = true;
            else if (aEteOccupee) { tableRendue = true; aEteOccupee = false; repasServis++; }
        }
        Check("un client servi s'attable", unAttable);
        Check("seul celui qui a commande une pizza mange sur place", !attableGourmand);
        Check("et on la lui sert sur un plateau, pas en boite",
              !attableEnBoite && attableSurPlateau);
        Check("sa pizza n'est jamais passee par un carton", !plateauEmballe);
        Check("le comptoir dresse des plateaux a part", plateauAuComptoir);
        Check("le plateau se prepare au plan, avec les cartons", plateauAuPlan);
        Check("il reste vide jusqu'a ce qu'on y pose la pizza", !plateauNePasVide);
        Check("le caissier le porte lui-meme au comptoir", caissierPorteLePlateau);
        Check("et il le porte seul, sans cartons", !plateauAvecDesBoites);
        Check("porte, le plateau se presente dans la longueur", plateauTourne);
        Check("il est bien assis sur la chaise", assisSurLaChaise);
        Check("et il en a la pose", poseAssise);
        Check("jamais deux a la fois", !deuxALaFois);
        Check("il pose sa pizza entiere sur la table", pizzaEntiere);
        Check("elle est servie sur son plateau", plateauSurLaTable);
        Check("elle est bien dans l'assiette, plus dans ses mains",
              vuMangerALaTable && !mainsPleinesEnMangeant);
        Check("elle s'en va part par part",
              vueEntamee[3] && vueEntamee[2] && vueEntamee[1]);
        Check("et il ne laisse que des restes", orduresLaissees);
        Check("il laisse aussi l'addition sur la table", liasseSurLaTable);
        Check("personne ne mange devant les restes", !attableSurLesRestes);

        // La regle elle-meme, verifiee de face : la simulation ne tombe pas
        // forcement sur le cas pendant la fenetre d'observation.
        var clients = TousLes<Client>();
        tableSalle.LaisserOrdures();
        Check("une table sale n'accueille personne",
              clients.Count > 0 && !tableSalle.Accueillir(clients[0]));
        for (int i = 0; i < 60 * 120 && tableSalle.ADesOrdures; i++) Frames(1);
        Check("et le caissier vient la nettoyer", !tableSalle.ADesOrdures);
        Check("une fois propre, elle accueille de nouveau",
              clients.Count > 0 && tableSalle.EstLibre);
        Check("le caissier emporte les restes", caissierEmporte);
        Check("et la table redevient propre", tableNettoyee);
        Check("rien ne traine plus sur la table a la fin",
              !tableSalle.ADesOrdures || caissierEmporte);
        Check("la table se libere apres le repas", tableRendue);
        // Elle ressert : une place rendue une seule fois pourrait n'etre
        // qu'un client parti sans manger.
        Check("et elle ressert au suivant", repasServis >= 2);

        // --- le plan se regarnit des deux mains ---
        // Le compte a rebours est commun aux deux piles. Separe, il restait
        // fige des qu'une des deux etait pleine : l'autre ne revenait alors
        // plus jamais, et le caissier cherchait ses boites pour rien.
        bool plateauxRevenus = false, cartonsRevenus = false;
        table.PlateauxEnPile.Vider();
        table.Boites.Vider();
        for (int i = 0; i < 60 * 60 && !(plateauxRevenus && cartonsRevenus); i++)
        {
            Frames(1);
            if (table.PlateauxEnPile.EstPleine) plateauxRevenus = true;
            if (table.Boites.EstPleine) cartonsRevenus = true;
        }
        Check("les plateaux laves reviennent sur le plan", plateauxRevenus);
        Check("et les cartons se regarnissent aussi", cartonsRevenus);

        // --- plus un plateau propre : il attend, il n'emballe pas ---
        // Sans plateau sous la main il partait prendre un carton : le client
        // de la salle voyait sa pizza emboitee, ou n'etait jamais servi.
        bool cartonAuLieuDuPlateau = false;
        bool preparationVide = table.Preparation.EstVide;
        for (int i = 0; i < 60 * 60; i++)
        {
            table.PlateauxEnPile.Vider();       // tous les plateaux sont dehors
            // l'etat se lit AVANT l'image : c'est lui qui a decide du geste
            bool manque = comptoir.UnPlateauManque;
            bool fourneeVierge = table.Assemblage.EstVide;   // donc aucune boite garnie
            Frames(1);
            if (preparationVide && manque && fourneeVierge
                && table.Preparation.SommetForme == Pile.Forme.Boite)
                cartonAuLieuDuPlateau = true;
            preparationVide = table.Preparation.EstVide;
        }
        Check("sans plateau propre, il attend au lieu d'emballer", !cartonAuLieuDuPlateau);
        table.Garnir();

        // --- les quatre emplacements du plan se tiennent a distance ---
        // Le carton pose sur le rond rouge touchait le tas de la reserve :
        // on voyait deux cartons passer l'un dans l'autre.
        var emplacements = new[] { table.Boites.transform, table.PlateauxEnPile.transform,
                                   table.Preparation.transform, table.Assemblage.transform };
        bool sInterpenetrent = false, deborde = false;
        bool ronsGarnis = false;
        for (int i = 0; i < 60 * 90; i++)
        {
            Frames(1);
            if (!table.Preparation.EstVide && !table.Assemblage.EstVide) ronsGarnis = true;
            for (int a1 = 0; a1 < emplacements.Length; a1++)
            {
                if (Empreinte(emplacements[a1], out float x0, out float x1, out float z0, out float z1))
                {
                    // le dessus du plan : 4,4 sur 1,4, centre sur le meuble
                    var centre = table.transform.position;
                    if (x0 < centre.x - 2.2f || x1 > centre.x + 2.2f ||
                        z0 < centre.z - 0.7f || z1 > centre.z + 0.7f) deborde = true;
                }
                for (int b1 = a1 + 1; b1 < emplacements.Length; b1++)
                    if (SeChevauchent(emplacements[a1], emplacements[b1])) sInterpenetrent = true;
            }
        }
        Check("on voit le caissier emballer, les deux ronds garnis", ronsGarnis);
        Check("rien ne s'interpenetre sur le plan d'emballage", !sInterpenetrent);
        Check("et rien ne deborde du plan", !deborde);

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

        // --- la fermeture, le soir ---
        // Tout ce qui suit se joue l'horloge en main : on la pose a l'heure
        // voulue au lieu d'attendre douze minutes de manette.
        Placer(joueur, new Vector3(-9f, 0f, -9f));      // il ne gene rien
        var enseigne = UnityEngine.Object.FindObjectOfType<Enseigne>();
        Check("le comptoir a son panneau de service", enseigne != null);

        PoserLHeure(horloge, Reglages.HeureFermeture - 1);
        Check("une heure avant la fermeture, c'est encore ouvert",
              horloge.Ouverte && enseigne.MontreOuvert);
        int arriveesAvant = comptoir.Arrivees;
        Secondes(Reglages.DelaiClient * 2f);
        Check("et les clients continuent d'arriver", comptoir.Arrivees > arriveesAvant);

        PoserLHeure(horloge, Reglages.HeureFermeture);
        Check("a vingt et une heures, la pizzeria est fermee", !horloge.Ouverte);
        Check("le panneau passe au rouge", !enseigne.MontreOuvert);
        var panneauFerme = Piece(enseigne.transform, "Panneau");
        var couleurFermee = panneauFerme.gameObject.GetComponent<MeshRenderer>().sharedMaterial.color;
        Check("et il est bien rouge, pas vert",
              couleurFermee.r > couleurFermee.g + 0.2f && couleurFermee.r > couleurFermee.b + 0.2f);
        Check("le Hud l'annonce aussi", Contient(TousLes<Text>(), "FERME"));

        arriveesAvant = comptoir.Arrivees;
        Secondes(Reglages.DelaiClient * 3f);
        Check("plus personne ne pousse la porte", comptoir.Arrivees == arriveesAvant);

        // --- le caissier rentre chez lui ---
        var employeCaissier = employe.GetComponent<Caissier>();
        Check("a la fermeture, le caissier est encore la",
              employe.activeSelf && !employeCaissier.PorteSonSac);

        PoserLHeure(horloge, Reglages.HeureDepartCaissier);
        bool sacVu = false;
        for (int i = 0; i < 60 * 40 && employe.activeSelf; i++)
        {
            Frames(1);
            if (employeCaissier.PorteSonSac) sacVu = true;
        }
        Check("a vingt-deux heures il prend son sac a dos", sacVu);
        Check("et il s'en va", !employe.activeSelf);
        Check("le comptoir n'est plus tenu par personne", !comptoir.CaissierPresent);
        // Parti pour la nuit, il n'est pas licencie pour autant.
        Check("il reste embauche au registre", employeCaissier.Embauche);

        PoserLHeure(horloge, Reglages.HeureOuverture);
        Frames(4);
        Check("le lendemain a l'ouverture, il est de retour", employe.activeSelf);
        Check("sans son sac", !employeCaissier.PorteSonSac);
        Check("et de nouveau a son poste",
              (employe.transform.position - employeCaissier.Poste).magnitude < 0.5f);
        Check("le panneau repasse au vert", enseigne.MontreOuvert);
        horloge.Figee = true;

        var erreurs = new List<string>();
        foreach (var l in Debug.Journal) if (l.StartsWith("ERROR")) erreurs.Add(l);
        if (dumpUI != null && dumpUI.Length > 0)
        {
            Placer(joueur, bureau.Place);
            Secondes(1.5f);
            DumpUI(dumpUI);
        }
        if (dumpScene) DumpScene(Environment.GetEnvironmentVariable("DUMP_SCENE"));

        // --- le logo de l'enseigne, imprime sur les cartons ---
        // Fabrique a part et detruit aussitot : un carton egare au milieu de
        // la salle fausserait les comptes des autres controles.
        var bacLogo = new GameObject("BacLogo");
        var cartonTemoin = Boite3D.Creer(bacLogo.transform, 0);
        var etiquette = Piece(cartonTemoin.transform, "Etiquette");
        Check("le carton porte une etiquette", etiquette != null);
        var peinture = etiquette != null
            ? etiquette.gameObject.GetComponent<MeshRenderer>().sharedMaterial : null;
        var image = peinture != null ? peinture.mainTexture as Texture2D : null;
        Check("elle est imprimee, et non peinte a plat", image != null);
        // Le dessin lui-meme, releve au pixel : le fond sombre dans un coin,
        // la couronne orange, le rouge du disque, la creme de la part et
        // celle du nom dessous.
        // Releves exprimes en 128es : la texture peut grandir sans que les
        // points de controle se retrouvent ailleurs sur le dessin.
        int T = image != null ? image.width / 128 : 1;
        Check("le logo est dessine assez finement", image != null && image.width >= 256);
        Check("le logo a son cerne sombre", Pixel(image, 64 * T, 114 * T, 0x1E1B26));
        Check("sa couronne jaune", Pixel(image, 64 * T, 108 * T, 0xF0A93C));
        Check("son coeur orange", Pixel(image, 30 * T, 64 * T, 0xE8562A));
        Check("sa part de pizza", Pixel(image, 66 * T, 58 * T, 0xF2DFA8));

        // Les bords sont adoucis : sans cela, les arrondis du logo montent en
        // escalier des qu'on approche la camera du couvercle.
        int melanges = 0;
        if (image != null && image.Pixels != null)
            foreach (var q in image.Pixels)
            {
                int rgb = (q.r << 16) | (q.g << 8) | q.b;
                if (rgb != 0xCB3A2A && rgb != 0x1E1B26 && rgb != 0xF0A93C
                    && rgb != 0xE8562A && rgb != 0xF2DFA8 && rgb != 0xC33A2A) melanges++;
            }
        Check("ses bords sont adoucis", melanges > 400);

        // Le carton du modele est rouge, et l'etiquette est une plaque
        // carree : hors du disque, elle doit se confondre avec le couvercle,
        // sans quoi on verrait un cadre pose dessus.
        var couvercle = Piece(cartonTemoin.transform, "Couvercle");
        var teinteCouvercle = couvercle != null
            ? couvercle.gameObject.GetComponent<MeshRenderer>().sharedMaterial.color
            : Color.white;
        Check("le carton est rouge",
              teinteCouvercle.r > 0.6f && teinteCouvercle.g < 0.35f && teinteCouvercle.b < 0.30f);
        Check("le fond de l'etiquette est celui du couvercle",
              Pixel(image, 4 * T, 4 * T, ((int)(teinteCouvercle.r * 255) << 16)
                               | ((int)(teinteCouvercle.g * 255) << 8)
                               | (int)(teinteCouvercle.b * 255)));
        // Une texture posee sur la peinture blanche partagee se serait
        // retrouvee sur tout ce qui est blanc dans la pizzeria.
        Check("le logo ne deteint pas sur la peinture blanche",
              Bloc.Peinture(Color.white).mainTexture == null);
        UnityEngine.Object.Destroy(bacLogo);

        Check("aucune erreur Unity remontee", erreurs.Count == 0);
        foreach (var e in erreurs) Console.WriteLine("      " + e);

        Console.WriteLine(_echecs == 0 ? "\nTOUTE LA BOUCLE PASSE" : "\n" + _echecs + " ECHEC(S)");
        Environment.Exit(_echecs == 0 ? 0 : 1);
    }
}
