using System.Collections.Generic;
using BellaNotte.Core;
using UnityEngine;

namespace BellaNotte.Unity
{
    /// <summary>
    /// Dessine la pizza dans une texture generee a la volee : aucun asset a
    /// importer. Rendu semi-realiste vu de dessus — pate texturee au bruit,
    /// croute boursouflee, sauce aux bords irreguliers, mozzarella fondue et
    /// gratinee, ingredients aux formes propres, ombres portees et reflets.
    ///
    /// La pizza est redessinee pendant toute la cuisson : tout ce qui ne
    /// depend pas du temps de cuisson — bruit fractal, geometrie, eclairage —
    /// est donc calcule une seule fois au chargement, et le rendu ne traverse
    /// les pixels qu'une fois.
    /// </summary>
    public sealed class PizzaRenderer
    {
        const int Taille = 384;
        /// <summary>Echelle par rapport aux valeurs calees a 512 px.</summary>
        const float K = Taille / 512f;
        const int Petit = 96;                       // resolution de generation du bruit

        const float Centre = Taille / 2f;
        const float RayonPizza = Taille / 2f - 10f * K;
        const float RayonCroute = RayonPizza - 34f * K;
        const float RayonSauce = RayonPizza - 46f * K;

        readonly Texture2D _tex;
        readonly Color[] _px = new Color[Taille * Taille];
        readonly Color32[] _sortie = new Color32[Taille * Taille];
        public Sprite Sprite { get; }

        // bruit fige
        readonly byte[] _grain, _bulles, _bordPate, _sauceGrain, _bordSauce;
        readonly byte[] _plaques, _grainFin, _tacheFour, _champFromage;

        // geometrie figee : position du pixel seulement, jamais la cuisson
        readonly byte[] _lumiere;      // eclairage combine, 128 = neutre
        readonly byte[] _croute;       // 0 hors croute, sinon galbe
        readonly byte[] _alphaSauce;   // couverture de la sauce, bord fondu
        readonly bool[] _dedans;

        public PizzaRenderer()
        {
            _tex = new Texture2D(Taille, Taille, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Bilinear;
            Sprite = Sprite.Create(_tex, new Rect(0, 0, Taille, Taille), new Vector2(0.5f, 0.5f), 100f);

            // Genere en 96x96 puis etire : invisible a l'oeil, 16 fois moins cher.
            _grain      = Champ(0.05f, 4);
            _bulles     = Champ(0.11f, 3, 40f);
            _bordPate   = Champ(0.012f, 2);
            _sauceGrain = Champ(0.06f, 4, 21f);
            _bordSauce  = Champ(0.018f, 3, 9f);
            _plaques    = Champ(0.030f, 3, 77f);
            _grainFin   = Champ(0.13f, 2, 5f);
            _tacheFour  = Champ(0.035f, 3, 130f);
            _champFromage = ChampFromage();

            int n = Taille * Taille;
            _lumiere = new byte[n]; _croute = new byte[n];
            _alphaSauce = new byte[n]; _dedans = new bool[n];
            PrecalculGeometrie();
        }

        // ------------------------------------------------------------------
        // bruit
        // ------------------------------------------------------------------

        static byte Octet(float f) => (byte)(Mathf.Clamp01(f) * 255f);
        static float Flottant(byte b) => b * (1f / 255f);

        static float Hash(float x, float y)
        {
            float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        static float Lisse(float t) => t * t * (3f - 2f * t);

        static float Bruit(float x, float y)
        {
            float xi = Mathf.Floor(x), yi = Mathf.Floor(y);
            float u = Lisse(x - xi), v = Lisse(y - yi);
            float a = Hash(xi, yi), b = Hash(xi + 1f, yi);
            float c = Hash(xi, yi + 1f), d = Hash(xi + 1f, yi + 1f);
            float haut = a + (b - a) * u, bas = c + (d - c) * u;
            return haut + (bas - haut) * v;
        }

        static float Fbm(float x, float y, int octaves)
        {
            float somme = 0f, amp = 0.5f, freq = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                somme += Bruit(x * freq, y * freq) * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2.05f;
            }
            return somme / norm;
        }

        /// <summary>Genere un champ en basse resolution puis l'etire en bilineaire.</summary>
        static byte[] Champ(float frequence, int octaves, float decalage = 0f)
        {
            float f = frequence / K * ((float)Taille / Petit);   // meme grain a l'ecran
            var petit = new float[Petit * Petit];
            for (int y = 0; y < Petit; y++)
            for (int x = 0; x < Petit; x++)
                petit[y * Petit + x] = Fbm(x * f + decalage, y * f + decalage, octaves);

            var grand = new byte[Taille * Taille];
            float ratio = (float)(Petit - 1) / (Taille - 1);
            for (int y = 0; y < Taille; y++)
            {
                float sy = y * ratio; int y0 = (int)sy; int y1 = Mathf.Min(y0 + 1, Petit - 1);
                float ty = sy - y0;
                for (int x = 0; x < Taille; x++)
                {
                    float sx = x * ratio; int x0 = (int)sx; int x1 = Mathf.Min(x0 + 1, Petit - 1);
                    float tx = sx - x0;
                    float h = petit[y0 * Petit + x0] + (petit[y0 * Petit + x1] - petit[y0 * Petit + x0]) * tx;
                    float b = petit[y1 * Petit + x0] + (petit[y1 * Petit + x1] - petit[y1 * Petit + x0]) * tx;
                    grand[y * Taille + x] = Octet(h + (b - h) * ty);
                }
            }
            return grand;
        }

        /// <summary>Nappe de mozzarella : bosses fusionnees, figee une fois pour toutes.</summary>
        static byte[] ChampFromage()
        {
            var resultat = new byte[Taille * Taille];
            var centres = new List<Vector2>();
            var rayons = new List<float>();
            for (int i = 0; i < 34; i++)
            {
                Position(131 + i, i, RayonSauce - 8f * K, out float px, out float py);
                centres.Add(new Vector2(px, py));
                rayons.Add((30f + Hash(i * 3.7f, 1.3f) * 26f) * K);
            }

            for (int y = 0; y < Taille; y++)
            for (int x = 0; x < Taille; x++)
            {
                float dxc = x + 0.5f - Centre, dyc = y + 0.5f - Centre;
                if (dxc * dxc + dyc * dyc > (RayonSauce + 4f) * (RayonSauce + 4f)) continue;

                float champ = 0f;
                for (int b = 0; b < centres.Count; b++)
                {
                    float dx = x + 0.5f - centres[b].x, dy = y + 0.5f - centres[b].y;
                    float r = rayons[b];
                    float d2 = (dx * dx + dy * dy) / (r * r);
                    if (d2 < 1f) champ += (1f - d2) * (1f - d2);
                }
                if (champ <= 0.14f) continue;
                resultat[y * Taille + x] = Octet(Mathf.Clamp01((champ - 0.14f) * 2.4f));
            }
            return resultat;
        }

        void PrecalculGeometrie()
        {
            for (int y = 0; y < Taille; y++)
            for (int x = 0; x < Taille; x++)
            {
                int i = y * Taille + x;
                float dx = x + 0.5f - Centre, dy = y + 0.5f - Centre;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float bord = RayonPizza * (1f + (Flottant(_bordPate[i]) - 0.5f) * 0.05f);
                if (d > bord) continue;
                _dedans[i] = true;

                if (d > RayonCroute)
                    _croute[i] = Octet(Mathf.Clamp01((d - RayonCroute) / (bord - RayonCroute)));

                float bordS = RayonSauce * (1f + (Flottant(_bordSauce[i]) - 0.5f) * 0.10f);
                if (d <= bordS) _alphaSauce[i] = Octet(Mathf.Clamp01((bordS - d) / 6f));

                float lum = 1f - Mathf.Clamp01((d / bord - 0.55f) / 0.45f) * 0.16f;
                float dn = d / RayonPizza;
                lum *= 1f - Mathf.Clamp01((dn - 0.6f) / 0.4f) * 0.22f;                      // vignettage
                lum *= 1f + Mathf.Clamp01(1f - (dx + dy + RayonPizza) / (RayonPizza * 1.6f)) * 0.10f;
                _lumiere[i] = (byte)(Mathf.Clamp01(lum * 0.5f) * 255f);                     // 128 = neutre
            }
        }

        // ------------------------------------------------------------------
        // couleurs
        // ------------------------------------------------------------------

        static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

        public static Color CouleurDe(IngredientId id)
        {
            switch (id)
            {
                case IngredientId.Tomate:     return Hex(0xB8341F);
                case IngredientId.Creme:      return Hex(0xF2E6CF);
                case IngredientId.Mozzarella: return Hex(0xF6E3A8);
                case IngredientId.Jambon:     return Hex(0xCC8078);
                case IngredientId.Champignon: return Hex(0xBFA684);
                case IngredientId.Olive:      return Hex(0x3B4525);
                case IngredientId.Poivron:    return Hex(0x4F9137);
                case IngredientId.Ananas:     return Hex(0xE8C33E);
                case IngredientId.Piment:     return Hex(0xC0281F);
                case IngredientId.Basilic:    return Hex(0x3E7E2E);
                case IngredientId.Anchois:    return Hex(0x93A1AA);
                case IngredientId.Oeuf:       return Hex(0xF7F1DF);
                default:                      return Color.magenta;
            }
        }

        static Color Fondre(Color fond, Color dessus, float a)
        {
            if (a <= 0f) return fond;
            if (a >= 1f) return dessus;
            return new Color(fond.r + (dessus.r - fond.r) * a,
                             fond.g + (dessus.g - fond.g) * a,
                             fond.b + (dessus.b - fond.b) * a,
                             fond.a + (1f - fond.a) * a);
        }

        // ------------------------------------------------------------------
        // rendu
        // ------------------------------------------------------------------

        public void Dessiner(IngredientId? basePizza, IReadOnlyList<IngredientId> garnitures, float cuisson)
        {
            float brulure = Mathf.Clamp01((cuisson - GameConfig.ZoneParfaiteMax) / 0.30f);
            float doree = Mathf.Clamp01(cuisson / GameConfig.ZoneParfaiteMin);

            bool fromage = false;
            for (int i = 0; i < garnitures.Count; i++)
                if (garnitures[i] == IngredientId.Mozzarella) fromage = true;

            Fond(basePizza, fromage, brulure, doree);

            // les garnitures reposent sur le fromage
            for (int k = 0; k < garnitures.Count; k++)
                if (garnitures[k] != IngredientId.Mozzarella) Morceaux(garnitures[k], k, brulure);

            for (int i = 0; i < _px.Length; i++) _sortie[i] = _px[i];
            _tex.SetPixels32(_sortie);
            _tex.Apply(false);
        }

        /// <summary>Pate, croute, sauce, fromage et taches de four : une seule traversee.</summary>
        void Fond(IngredientId? basePizza, bool fromage, float brulure, float doree)
        {
            Color crue = Hex(0xF8F0DC), cuite = Hex(0xC8913F), cramee = Hex(0x2A1810);
            Color pateBase = Color.Lerp(crue, cuite, doree);
            Color pateBulle = Color.Lerp(pateBase, cuite, 0.9f);

            bool creme = basePizza.HasValue && basePizza.Value == IngredientId.Creme;
            Color sauce = basePizza.HasValue
                ? Color.Lerp(CouleurDe(basePizza.Value), Hex(0x2E1A10), brulure * 0.9f)
                : default(Color);
            Color sauceSombre = Color.Lerp(sauce, Color.black, creme ? 0.12f : 0.30f);
            Color sauceClaire = Color.Lerp(sauce, Color.white, 0.25f);

            Color palePate = Color.Lerp(Hex(0xF4DFA2), Hex(0x2A1810), brulure * 0.85f);
            Color grillee = Color.Lerp(Hex(0xC8873A), Hex(0x241206), brulure * 0.9f);
            var suie = new Color(0.09f, 0.05f, 0.03f);
            var vide = new Color(0f, 0f, 0f, 0f);
            float seuilTache = 0.72f - brulure * 0.34f;

            for (int i = 0; i < _px.Length; i++)
            {
                if (!_dedans[i]) { _px[i] = vide; continue; }

                // --- pate ---
                float grain = Flottant(_grain[i]);
                float bulles = Flottant(_bulles[i]);
                Color c = Color.Lerp(pateBase, pateBulle, Mathf.Clamp01((bulles - 0.55f) * 3f) * doree);
                c = Color.Lerp(c, cramee, brulure * (0.55f + 0.45f * Mathf.Clamp01((bulles - 0.45f) * 2.5f)));
                float sombre = 1f - (1f - grain) * 0.063f;          // grain de la pate
                c = new Color(c.r * sombre, c.g * sombre, c.b * sombre, 1f);

                // --- croute bombee ---
                byte cr = _croute[i];
                if (cr != 0)
                {
                    float t = Flottant(cr);
                    c = Color.Lerp(c, Color.white, Mathf.Sin(t * Mathf.PI) * 0.20f);
                    c = Color.Lerp(c, Color.Lerp(c, Color.black, 0.5f), Mathf.Clamp01((t - 0.75f) / 0.25f) * 0.7f);
                }

                // --- sauce ---
                if (basePizza.HasValue)
                {
                    byte al = _alphaSauce[i];
                    if (al != 0)
                    {
                        float gs = Flottant(_sauceGrain[i]);
                        Color s = Color.Lerp(sauce, sauceSombre, 1f - gs);
                        s = Color.Lerp(s, sauceClaire, Mathf.Clamp01((gs - 0.62f) * 3f));
                        c = Color.Lerp(c, s, Flottant(al));
                    }
                }

                // --- mozzarella fondue ---
                if (fromage)
                {
                    byte epb = _champFromage[i];
                    if (epb != 0)
                    {
                        float ep = Flottant(epb);
                        float plaques = Flottant(_plaques[i]);
                        float fin = Flottant(_grainFin[i]);
                        float gratin = Mathf.Clamp01((plaques - 0.46f) * 3.4f) * (0.20f + doree * 1.1f)
                                     * (1f - ep * 0.30f);
                        Color f = Color.Lerp(palePate, grillee, Mathf.Clamp01(gratin));
                        f = Color.Lerp(f, Color.Lerp(f, grillee, 0.55f),
                                       Mathf.Clamp01((fin - 0.55f) * 2.5f) * doree * 0.7f);
                        float assombri = 1f - (1f - fin) * 0.066f;
                        f = new Color(f.r * assombri, f.g * assombri, f.b * assombri, 1f);
                        f = Color.Lerp(f, Color.Lerp(f, Color.white, 0.45f), Mathf.Clamp01(ep - 0.7f) * 0.6f);
                        c = Color.Lerp(c, f, Mathf.Clamp01(ep * 1.8f) * 0.95f);
                    }
                }

                // --- taches de four ---
                if (brulure > 0.05f)
                {
                    float force = Mathf.Clamp01((Flottant(_tacheFour[i]) - seuilTache) * 6f) * brulure;
                    if (force > 0f) c = Color.Lerp(c, suie, force * 0.7f);
                }

                float g = _lumiere[i] * (2f / 255f);
                _px[i] = new Color(c.r * g, c.g * g, c.b * g, 1f);
            }
        }

        // ------------------------------------------------------------------
        // garnitures
        // ------------------------------------------------------------------

        void Poser(int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= Taille || y >= Taille || a <= 0f) return;
            int i = y * Taille + x;
            if (_px[i].a <= 0f) return;                 // ne deborde pas de la pate
            _px[i] = Fondre(_px[i], c, a);
        }

        delegate void Peintre(int x, int y, float distance);

        void Boite(float cx, float cy, float rayon, Peintre peintre)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rayon));
            int x1 = Mathf.Min(Taille - 1, Mathf.CeilToInt(cx + rayon));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - rayon));
            int y1 = Mathf.Min(Taille - 1, Mathf.CeilToInt(cy + rayon));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > rayon) continue;
                peintre(x, y, d);
            }
        }

        void Ombre(float cx, float cy, float rayon)
        {
            float r = rayon * 1.15f;
            var noir = new Color(0.15f, 0.07f, 0.03f);
            Boite(cx + 3f * K, cy - 3f * K, r, (x, y, d) => Poser(x, y, noir, (1f - d / r) * 0.30f));
        }

        void Pastille(float cx, float cy, float rayon, Color couleur, float relief, float irregularite)
        {
            rayon *= K;
            Ombre(cx, cy, rayon);
            Boite(cx, cy, rayon + 1f, (x, y, d) =>
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float bord = rayon;
                if (irregularite > 0f)
                    bord *= 1f + (Flottant(_grain[y * Taille + x]) - 0.5f) * irregularite;
                if (d > bord) return;

                float t = d / bord;
                Color c = Color.Lerp(couleur, Color.Lerp(couleur, new Color(0.15f, 0.06f, 0.04f), 0.45f),
                                     Mathf.Clamp01((t - 0.6f) / 0.4f) * relief);
                float sx = dx + rayon * 0.3f, sy = dy + rayon * 0.3f;
                float spec = Mathf.Clamp01(1f - Mathf.Sqrt(sx * sx + sy * sy) / (rayon * 0.55f));
                c = Color.Lerp(c, Color.white, spec * 0.22f * relief);
                Poser(x, y, c, Mathf.Clamp01((bord - d) / 1.5f));
            });
        }

        void Anneau(float cx, float cy, float rExt, float rInt, Color couleur, float relief)
        {
            rExt *= K; rInt *= K;
            Ombre(cx, cy, rExt);
            Boite(cx, cy, rExt + 1f, (x, y, d) =>
            {
                if (d < rInt) return;
                float t = (d - rInt) / (rExt - rInt);
                Color c = Color.Lerp(couleur, Color.Lerp(couleur, Color.black, 0.4f),
                                     Mathf.Abs(t - 0.5f) * 2f * relief);
                Poser(x, y, c, Mathf.Clamp01((rExt - d) / 1.5f) * Mathf.Clamp01((d - rInt) / 1.5f + 0.5f));
            });
        }

        void Ellipse(float cx, float cy, float ra, float rb, float angle, Color couleur, float relief)
        {
            ra *= K; rb *= K;
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            float rmax = Mathf.Max((int)ra, (int)rb) + 2f;
            Ombre(cx, cy, rmax * 0.9f);
            Boite(cx, cy, rmax, (x, y, _) =>
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float u = (dx * cos + dy * sin) / ra;
                float v = (-dx * sin + dy * cos) / rb;
                float d = Mathf.Sqrt(u * u + v * v);
                if (d > 1f) return;
                Color c = Color.Lerp(couleur, Color.Lerp(couleur, Color.black, 0.45f),
                                     Mathf.Clamp01((d - 0.55f) / 0.45f) * relief);
                c = Color.Lerp(c, Color.white, Mathf.Clamp01(1f - d * 2.2f) * 0.18f * relief);
                Poser(x, y, c, Mathf.Clamp01((1f - d) * rmax));
            });
        }

        /// <summary>Position stable d'une garniture dans le disque de sauce.</summary>
        static void Position(int n, int i, float rayonMax, out float x, out float y)
        {
            float u = Hash(n * 0.731f, i * 1.317f);
            float v = Hash(n * 1.913f + 5f, i * 0.577f + 3f);
            float angle = u * Mathf.PI * 2f;
            float rad = Mathf.Sqrt(v) * rayonMax;
            x = Centre + Mathf.Cos(angle) * rad;
            y = Centre + Mathf.Sin(angle) * rad;
        }

        void Morceaux(IngredientId id, int k, float brulure)
        {
            Color c = Color.Lerp(CouleurDe(id), Hex(0x241206), brulure * 0.9f);
            int n = Nombre(id);
            float zone = RayonSauce - 22f * K;

            for (int i = 0; i < n; i++)
            {
                Position(k * 197 + i * 7, i + (int)id, zone, out float x, out float y);
                float angle = Hash(i * 2.3f, k * 4.1f) * Mathf.PI * 2f;
                Forme(id, x, y, angle, c, brulure);
            }
        }

        static int Nombre(IngredientId id)
        {
            switch (id)
            {
                case IngredientId.Jambon:     return 7;
                case IngredientId.Piment:     return 11;
                case IngredientId.Olive:      return 12;
                case IngredientId.Champignon: return 9;
                case IngredientId.Basilic:    return 7;
                case IngredientId.Anchois:    return 7;
                case IngredientId.Oeuf:       return 2;
                case IngredientId.Poivron:    return 8;
                case IngredientId.Ananas:     return 8;
                default:                      return 9;
            }
        }

        void Forme(IngredientId id, float x, float y, float angle, Color c, float brulure)
        {
            switch (id)
            {
                case IngredientId.Piment:      // rondelle bombee facon pepperoni
                    Pastille(x, y, 20f, c, 1f, 0.06f);
                    Pastille(x, y, 13f, Color.Lerp(c, Color.black, 0.18f), 0.4f, 0.15f);
                    break;

                case IngredientId.Olive:       // rondelle percee
                    Anneau(x, y, 15f, 6.5f, c, 1f);
                    break;

                case IngredientId.Jambon:      // tranche large, pliee, contour dechire
                    Ellipse(x, y, 30f, 22f, angle, c, 0.35f);
                    Ellipse(x + 5f * K, y + 3f * K, 19f, 7f, angle + 0.4f, Color.Lerp(c, Color.white, 0.20f), 0.3f);
                    Ellipse(x - 8f * K, y - 4f * K, 13f, 4f, angle - 0.5f, Color.Lerp(c, Color.black, 0.14f), 0.25f);
                    break;

                case IngredientId.Champignon:  // lamelle : chapeau, lames, pied
                    Ellipse(x, y, 19f, 13f, angle, c, 0.85f);
                    Ellipse(x, y, 12f, 7f, angle, Color.Lerp(c, Color.black, 0.14f), 0.35f);
                    Ellipse(x + Mathf.Cos(angle) * 11f * K, y + Mathf.Sin(angle) * 11f * K, 7f, 5f, angle,
                            Color.Lerp(c, Color.white, 0.28f), 0.6f);
                    break;

                case IngredientId.Poivron:     // laniere courbe
                    Ellipse(x, y, 21f, 6f, angle, c, 0.9f);
                    Ellipse(x + Mathf.Cos(angle + 1f) * 5f * K, y + Mathf.Sin(angle + 1f) * 5f * K, 16f, 4f,
                            angle + 0.35f, Color.Lerp(c, Color.white, 0.2f), 0.7f);
                    break;

                case IngredientId.Ananas:      // morceau anguleux
                    Ellipse(x, y, 14f, 12f, angle, c, 0.8f);
                    Ellipse(x, y, 9f, 7f, angle + 0.6f, Color.Lerp(c, Color.white, 0.3f), 0.5f);
                    break;

                case IngredientId.Basilic:     // feuille pointue avec nervure
                    Ellipse(x, y, 18f, 9f, angle, c, 0.85f);
                    Ellipse(x, y, 15f, 1.6f, angle, Color.Lerp(c, Color.white, 0.35f), 0.2f);
                    break;

                case IngredientId.Anchois:     // filet allonge
                    Ellipse(x, y, 22f, 5f, angle, c, 0.9f);
                    Ellipse(x, y, 19f, 1.4f, angle, Color.Lerp(c, Color.black, 0.35f), 0.2f);
                    break;

                case IngredientId.Oeuf:        // blanc irregulier + jaune bombe
                    Pastille(x, y, 40f, Color.Lerp(Hex(0xFBF6E8), Hex(0x241206), brulure * 0.8f), 0.35f, 0.28f);
                    Pastille(x, y, 17f, Color.Lerp(Hex(0xE8A72E), Hex(0x241206), brulure * 0.8f), 1f, 0.05f);
                    break;

                default:
                    Pastille(x, y, 16f, c, 0.8f, 0.2f);
                    break;
            }
        }
    }
}
