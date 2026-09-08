using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le logo imprime sur le couvercle des cartons, dessine pixel par pixel
    /// a l'execution : cerne sombre, couronne jaune, coeur orange et la part
    /// de pizza dessus. Comme tout le reste du jeu, il ne vient d'aucun
    /// fichier a importer — le projet n'a pas un seul asset.
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

            var trait   = Bloc.Couleur(0x1E1B26);
            var jaune   = Bloc.Couleur(0xF0A93C);
            var orange  = Bloc.Couleur(0xE8562A);
            var creme   = Bloc.Couleur(0xF2DFA8);
            var pepper  = Bloc.Couleur(0xC33A2A);

            // Le fond est celui du couvercle : l'etiquette est une plaque
            // carree, et hors du disque elle doit se confondre avec le carton.
            for (int i = 0; i < _px.Length; i++) _px[i] = Bloc.CartonClair;

            Disque(64f, 64f, 52f, trait);     // le cerne sombre
            Disque(64f, 64f, 47f, jaune);     // la couronne
            Disque(64f, 64f, 40f, orange);    // le coeur

            // La part, cerclee comme sur le dessin : pointe vers le haut a
            // droite, base vers le bas a gauche.
            Triangle(94f, 90f, 32f, 60f, 58f, 26f, 3f, trait);
            Triangle(94f, 90f, 32f, 60f, 58f, 26f, 0f, creme);

            Pastille(72f, 66f, 4f, pepper, trait);
            Pastille(58f, 54f, 4.5f, pepper, trait);
            Pastille(48f, 47f, 3.5f, pepper, trait);

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

    }
}
