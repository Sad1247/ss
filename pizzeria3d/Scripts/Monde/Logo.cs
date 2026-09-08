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
        // 256 plutot que 128 : le logo occupe la moitie du couvercle, et a
        // 128 ses arrondis crenelaient des qu'on approchait la camera.
        const int Taille = 256;
        /// <summary>Le dessin a ete compose sur 128 : tout s'y rapporte.</summary>
        const float E = Taille / 128f;

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

            Disque(64f * E, 64f * E, 52f * E, trait);     // le cerne sombre
            Disque(64f * E, 64f * E, 47f * E, jaune);     // la couronne
            Disque(64f * E, 64f * E, 40f * E, orange);    // le coeur

            // La part, cerclee comme sur le dessin : pointe vers le haut a
            // droite, base vers le bas a gauche.
            Triangle(94f * E, 90f * E, 32f * E, 60f * E, 58f * E, 26f * E, 3f * E, trait);
            Triangle(94f * E, 90f * E, 32f * E, 60f * E, 58f * E, 26f * E, 0f, creme);

            Pastille(72f * E, 66f * E, 4f * E, pepper, trait);
            Pastille(58f * E, 54f * E, 4.5f * E, pepper, trait);
            Pastille(48f * E, 47f * E, 3.5f * E, pepper, trait);

            tex.SetPixels32(_px);
            tex.Apply(false);
            _px = null;
            return tex;
        }

        // Quatre echantillons par pixel : les bords sont adoucis au lieu de
        // monter en escalier. C'est ce qui separe un logo dessine d'un logo
        // pixelise, a la taille ou il apparait sur le couvercle.
        static readonly float[] Sous = { 0.25f, 0.75f };

        static void Disque(float cx, float cy, float r, Color c)
        {
            for (int y = (int)(cy - r) - 1; y <= cy + r + 1; y++)
            for (int x = (int)(cx - r) - 1; x <= cx + r + 1; x++)
            {
                if (x < 0 || y < 0 || x >= Taille || y >= Taille) continue;

                int dedans = 0;
                foreach (float sy in Sous)
                foreach (float sx in Sous)
                {
                    float dx = x + sx - cx, dy = y + sy - cy;
                    if (dx * dx + dy * dy <= r * r) dedans++;
                }
                Melanger(x, y, c, dedans / 4f);
            }
        }

        /// <summary>Pose une couleur sur le pixel, a la couverture donnee.</summary>
        static void Melanger(int x, int y, Color c, float couverture)
        {
            if (couverture <= 0f) return;
            int i = y * Taille + x;
            if (couverture >= 1f) { _px[i] = (Color32)c; return; }
            var fond = _px[i];
            _px[i] = (Color32)new Color(
                fond.r / 255f + (c.r - fond.r / 255f) * couverture,
                fond.g / 255f + (c.g - fond.g / 255f) * couverture,
                fond.b / 255f + (c.b - fond.b / 255f) * couverture,
                1f);
        }

        /// <summary>Une rondelle de pepperoni : son cerne, puis sa chair.</summary>
        static void Pastille(float cx, float cy, float r, Color chair, Color cerne)
        {
            Disque(cx, cy, r + 1.6f * E, cerne);
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

            int x0 = Mathf.Max(0, (int)Mathf.Min(ax, Mathf.Min(bx, cx)) - 1);
            int x1 = Mathf.Min(Taille - 1, (int)Mathf.Max(ax, Mathf.Max(bx, cx)) + 1);
            int y0 = Mathf.Max(0, (int)Mathf.Min(ay, Mathf.Min(by, cy)) - 1);
            int y1 = Mathf.Min(Taille - 1, (int)Mathf.Max(ay, Mathf.Max(by, cy)) + 1);

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int dedans = 0;
                foreach (float sy in Sous)
                foreach (float sx in Sous)
                {
                    float px = x + sx, py = y + sy;
                    float d1 = Cote(px, py, ax, ay, bx, by);
                    float d2 = Cote(px, py, bx, by, cx, cy);
                    float d3 = Cote(px, py, cx, cy, ax, ay);
                    bool negatif = d1 < 0f || d2 < 0f || d3 < 0f;
                    bool positif = d1 > 0f || d2 > 0f || d3 > 0f;
                    if (!(negatif && positif)) dedans++;
                }
                Melanger(x, y, couleur, dedans / 4f);
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
