using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le logo de l'enseigne, dessine pixel par pixel a l'execution : disque
    /// orange, fond rouge, part de pizza et le nom dessous. Comme tout le
    /// reste du jeu, il ne vient d'aucun fichier a importer — le projet n'a
    /// pas un seul asset.
    /// </summary>
    public static class Logo
    {
        const int Taille = 128;

        static Material _materiau;

        /// <summary>La peinture imprimee du logo, partagee par tous les cartons.</summary>
        public static Material Materiau()
        {
            if (_materiau == null) _materiau = Bloc.Impression("logo", Dessiner());
            return _materiau;
        }

        static Color32[] _px;

        static Texture2D Dessiner()
        {
            var tex = new Texture2D(Taille, Taille, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            _px = new Color32[Taille * Taille];

            var fond     = Bloc.Couleur(0x1E1B26);
            var orange   = Bloc.Couleur(0xF5A623);
            var rouge    = Bloc.Couleur(0xE04A2F);
            var creme    = Bloc.Couleur(0xF2E3B3);
            var pepper   = Bloc.Couleur(0xC33A2A);
            var trait    = Bloc.Couleur(0x1E1B26);

            for (int i = 0; i < _px.Length; i++) _px[i] = fond;

            // la pastille : couronne orange, coeur rouge
            Disque(64f, 80f, 45f, orange);
            Disque(64f, 80f, 38f, rouge);

            // La part, cerclee de sombre : sans le trait, la creme et
            // l'orange se touchent et la pointe disparait.
            Triangle(64f, 118f, 33f, 45f, 95f, 45f, 2.5f, trait);
            Triangle(64f, 118f, 33f, 45f, 95f, 45f, 0f, creme);

            // le pepperoni, du plus haut au plus bas
            Pastille(71f, 99f, 4.5f, pepper, trait);
            Pastille(57f, 85f, 6.5f, pepper, trait);
            Pastille(76f, 71f, 5.5f, pepper, trait);
            Pastille(57f, 63f, 4.5f, pepper, trait);

            Ecrire("PIZZAMAX", 17, 12, 2, creme);

            tex.SetPixels32(_px);
            tex.Apply(false);
            _px = null;
            return tex;
        }

        static void Disque(float cx, float cy, float r, Color c)
        {
            var teinte = (Color32)c;
            for (int y = (int)(cy - r) - 1; y <= cy + r + 1; y++)
            for (int x = (int)(cx - r) - 1; x <= cx + r + 1; x++)
            {
                if (x < 0 || y < 0 || x >= Taille || y >= Taille) continue;
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                if (dx * dx + dy * dy <= r * r) _px[y * Taille + x] = teinte;
            }
        }

        /// <summary>Une rondelle de pepperoni : son cerne, puis sa chair.</summary>
        static void Pastille(float cx, float cy, float r, Color chair, Color cerne)
        {
            Disque(cx, cy, r + 1.6f, cerne);
            Disque(cx, cy, r, chair);
        }

        /// <summary>
        /// Un triangle plein, eventuellement grossi de <paramref name="marge"/>
        /// autour de son centre : c'est ainsi qu'on lui donne son contour.
        /// </summary>
        static void Triangle(float ax, float ay, float bx, float by, float cx, float cy,
                             float marge, Color couleur)
        {
            if (marge > 0f)
            {
                float gx = (ax + bx + cx) / 3f, gy = (ay + by + cy) / 3f;
                Ecarter(ref ax, ref ay, gx, gy, marge);
                Ecarter(ref bx, ref by, gx, gy, marge);
                Ecarter(ref cx, ref cy, gx, gy, marge);
            }

            var teinte = (Color32)couleur;
            int x0 = Mathf.Max(0, (int)Mathf.Min(ax, Mathf.Min(bx, cx)) - 1);
            int x1 = Mathf.Min(Taille - 1, (int)Mathf.Max(ax, Mathf.Max(bx, cx)) + 1);
            int y0 = Mathf.Max(0, (int)Mathf.Min(ay, Mathf.Min(by, cy)) - 1);
            int y1 = Mathf.Min(Taille - 1, (int)Mathf.Max(ay, Mathf.Max(by, cy)) + 1);

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float d1 = Cote(px, py, ax, ay, bx, by);
                float d2 = Cote(px, py, bx, by, cx, cy);
                float d3 = Cote(px, py, cx, cy, ax, ay);
                bool negatif = d1 < 0f || d2 < 0f || d3 < 0f;
                bool positif = d1 > 0f || d2 > 0f || d3 > 0f;
                if (!(negatif && positif)) _px[y * Taille + x] = teinte;
            }
        }

        static void Ecarter(ref float x, ref float y, float gx, float gy, float marge)
        {
            float dx = x - gx, dy = y - gy;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d <= 0.001f) return;
            x += dx / d * marge;
            y += dy / d * marge;
        }

        static float Cote(float px, float py, float ax, float ay, float bx, float by)
            => (px - bx) * (ay - by) - (ax - bx) * (py - by);

        // Une fonte 5x7, juste les lettres du nom : importer une police pour
        // huit caracteres serait le seul asset du projet.
        static readonly string[] Alphabet = { "P", "I", "Z", "A", "M", "X" };
        static readonly string[] Glyphes =
        {
            "11110" + "10001" + "10001" + "11110" + "10000" + "10000" + "10000",   // P
            "11111" + "00100" + "00100" + "00100" + "00100" + "00100" + "11111",   // I
            "11111" + "00001" + "00010" + "00100" + "01000" + "10000" + "11111",   // Z
            "01110" + "10001" + "10001" + "11111" + "10001" + "10001" + "10001",   // A
            "10001" + "11011" + "10101" + "10001" + "10001" + "10001" + "10001",   // M
            "10001" + "10001" + "01010" + "00100" + "01010" + "10001" + "10001",   // X
        };

        static void Ecrire(string mot, int x0, int y0, int echelle, Color couleur)
        {
            var teinte = (Color32)couleur;
            int x = x0;
            foreach (char lettre in mot)
            {
                int index = -1;
                for (int i = 0; i < Alphabet.Length; i++)
                    if (Alphabet[i][0] == lettre) index = i;
                if (index >= 0) Glyphe(Glyphes[index], x, y0, echelle, teinte);
                x += 6 * echelle;
            }
        }

        static void Glyphe(string forme, int x0, int y0, int echelle, Color32 teinte)
        {
            for (int l = 0; l < 7; l++)
            for (int c = 0; c < 5; c++)
            {
                if (forme[l * 5 + c] != '1') continue;
                // la premiere ligne du dessin est le HAUT de la lettre
                int bx = x0 + c * echelle, by = y0 + (6 - l) * echelle;
                for (int dy = 0; dy < echelle; dy++)
                for (int dx = 0; dx < echelle; dx++)
                {
                    int x = bx + dx, y = by + dy;
                    if (x < 0 || y < 0 || x >= Taille || y >= Taille) continue;
                    _px[y * Taille + x] = teinte;
                }
            }
        }
    }
}
