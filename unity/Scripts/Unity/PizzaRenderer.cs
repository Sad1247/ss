using System.Collections.Generic;
using BellaNotte.Core;
using UnityEngine;

namespace BellaNotte.Unity
{
    /// <summary>
    /// Dessine la pizza dans une texture generee a la volee : pas de sprite a
    /// importer, et la coloration suit la cuisson. Meme rendu que le prototype.
    /// </summary>
    public sealed class PizzaRenderer
    {
        const int Taille = 256;

        readonly Texture2D _tex;
        readonly Color32[] _pixels = new Color32[Taille * Taille];
        public Sprite Sprite { get; }

        public PizzaRenderer()
        {
            _tex = new Texture2D(Taille, Taille, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Bilinear;
            Sprite = Sprite.Create(_tex, new Rect(0, 0, Taille, Taille), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Color CouleurDe(IngredientId id)
        {
            switch (id)
            {
                case IngredientId.Tomate:     return Hex(0xC8452F);
                case IngredientId.Creme:      return Hex(0xF2E6CF);
                case IngredientId.Mozzarella: return Hex(0xF7E7A8);
                case IngredientId.Jambon:     return Hex(0xE79A9A);
                case IngredientId.Champignon: return Hex(0xC9B295);
                case IngredientId.Olive:      return Hex(0x3F4A26);
                case IngredientId.Poivron:    return Hex(0x5F9E42);
                case IngredientId.Ananas:     return Hex(0xE8C33E);
                case IngredientId.Piment:     return Hex(0xD0342C);
                case IngredientId.Basilic:    return Hex(0x4C8B3A);
                case IngredientId.Anchois:    return Hex(0x9AA8B0);
                case IngredientId.Oeuf:       return Hex(0xF5F0E0);
                default:                      return Color.magenta;
            }
        }

        static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

        /// <summary>Positions stables : la meme garniture retombe toujours au meme endroit.</summary>
        static void Graine(int n, int i, out float u, out float v)
        {
            float a = Mathf.Sin(n * 127.1f + i * 311.7f) * 43758.5453f;
            float b = Mathf.Sin(n * 269.5f + i * 183.3f) * 43758.5453f;
            u = a - Mathf.Floor(a);
            v = b - Mathf.Floor(b);
        }

        public void Dessiner(IngredientId? basePizza, IReadOnlyList<IngredientId> garnitures, float cuisson)
        {
            // Le brunissage demarre des la sortie de la zone parfaite : une pizza
            // trop cuite doit se voir immediatement, c'est un signal de jeu.
            float brulure = Mathf.Clamp01((cuisson - GameConfig.ZoneParfaiteMax) / 0.30f);
            float doree = Mathf.Clamp01(cuisson / GameConfig.ZoneParfaiteMin);

            var vide = new Color32(0, 0, 0, 0);
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = vide;

            const float cx = Taille / 2f, cy = Taille / 2f;
            const float R = Taille / 2f - 4f;
            float bordure = R - 9f;
            float interieur = R - 16f;

            // L'ecart pale -> dore doit etre franc : c'est le seul indice visuel
            // qui distingue une pizza crue d'une pizza a point.
            Color pateCuite = Color.Lerp(Hex(0xD9AF6B), Hex(0x2A1810), brulure);
            Color pate = Color.Lerp(Hex(0xFBF5E7), pateCuite, doree);
            Color croute = Color.Lerp(Hex(0xF7EFDC), Color.Lerp(Hex(0xCE9B4F), Hex(0x2A1810), brulure), doree);
            Color? sauce = basePizza.HasValue
                ? Color.Lerp(CouleurDe(basePizza.Value), Hex(0x2E1A10), brulure * 0.9f)
                : (Color?)null;

            // pate + croute + sauce
            for (int y = 0; y < Taille; y++)
            {
                for (int x = 0; x < Taille; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > R) continue;

                    Color c = d > bordure ? croute : pate;
                    if (sauce.HasValue && d <= interieur) c = sauce.Value;
                    _pixels[y * Taille + x] = c;
                }
            }

            // garnitures
            for (int k = 0; k < garnitures.Count; k++)
            {
                var id = garnitures[k];
                Color c = Color.Lerp(CouleurDe(id), Hex(0x3B2414), brulure * 0.7f);
                int n = id == IngredientId.Mozzarella ? 14 : 9;
                float rayon = id == IngredientId.Mozzarella ? 10f : 7f;

                for (int i = 0; i < n; i++)
                {
                    Graine(k * 97 + i, ((int)id) * 13 + i, out float u, out float v);
                    float ang = u * Mathf.PI * 2f;
                    float rad = Mathf.Sqrt(v) * (R - 34f);
                    Disque(cx + Mathf.Cos(ang) * rad, cy + Mathf.Sin(ang) * rad, rayon, c);
                }
            }

            // voile de brulure
            if (brulure > 0.35f)
            {
                var voile = new Color(0.06f, 0.03f, 0.02f, 0.30f * brulure);
                for (int y = 0; y < Taille; y++)
                for (int x = 0; x < Taille; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy > R * R) continue;
                    int idx = y * Taille + x;
                    _pixels[idx] = Color.Lerp(_pixels[idx], voile, voile.a);
                }
            }

            _tex.SetPixels32(_pixels);
            _tex.Apply(false);
        }

        void Disque(float cx, float cy, float rayon, Color couleur)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rayon));
            int x1 = Mathf.Min(Taille - 1, Mathf.CeilToInt(cx + rayon));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - rayon));
            int y1 = Mathf.Min(Taille - 1, Mathf.CeilToInt(cy + rayon));
            float r2 = rayon * rayon;

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                if (dx * dx + dy * dy > r2) continue;
                int idx = y * Taille + x;
                if (_pixels[idx].a == 0) continue;   // ne deborde pas de la pate
                _pixels[idx] = couleur;
            }
        }
    }
}
