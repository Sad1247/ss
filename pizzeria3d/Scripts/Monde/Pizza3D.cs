using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La pizza du jeu : un cylindre de pate dore, coiffe d'un disque portant
    /// une texture generee — sauce, mozzarella fondue, pepperoni, basilic.
    ///
    /// Le choix de la texture plutot que d'empiler des primitives tient au
    /// nombre : jusqu'a trente pizzas coexistent (tete du joueur, four,
    /// comptoir, sacs des clients). Une garniture en volume couterait des
    /// centaines d'objets ; ici chaque pizza en compte deux, et toutes
    /// partagent les memes materiaux.
    /// </summary>
    public static class Pizza3D
    {
        const int Taille = 256;
        const int Segments = 40;

        static Material _pate, _garniture;
        static Mesh _disque;

        public static GameObject Creer(Transform parent, int index)
        {
            Preparer();

            var racine = new GameObject("Pizza" + index);
            racine.transform.SetParent(parent, false);
            racine.transform.localPosition = new Vector3(0f, index * Reglages.EpaisseurPizza, 0f);
            // chaque pizza est posee de travers : une pile parfaitement alignee
            // trahit le decor genere
            racine.transform.localRotation = Quaternion.Euler(0f, (index * 37f) % 360f, 0f);

            // la pate : sa tranche fait la croute doree qui deborde du disque
            var pate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pate.name = "Pate";
            pate.transform.SetParent(racine.transform, false);
            pate.transform.localScale = new Vector3(0.92f, Reglages.EpaisseurPizza * 0.5f, 0.92f);
            pate.GetComponent<Renderer>().sharedMaterial = _pate;
            pate.SansCollision();

            var dessus = new GameObject("Garniture", typeof(MeshFilter), typeof(MeshRenderer));
            dessus.transform.SetParent(racine.transform, false);
            dessus.transform.localPosition = new Vector3(0f, Reglages.EpaisseurPizza * 0.52f, 0f);
            dessus.transform.localScale = new Vector3(0.40f, 1f, 0.40f);
            dessus.GetComponent<MeshFilter>().sharedMesh = _disque;
            dessus.GetComponent<MeshRenderer>().sharedMaterial = _garniture;

            return racine;
        }

        static void Preparer()
        {
            if (_disque == null) _disque = Disque(Segments);
            if (_pate == null) _pate = Bloc.Peinture(Bloc.Couleur(0xE8B45C));
            if (_garniture == null)
            {
                _garniture = Bloc.Peinture(Color.white);
                _garniture.mainTexture = Texture();
            }
        }

        /// <summary>Disque plat de rayon 1, face vers le haut, texture centree.</summary>
        static Mesh Disque(int segments)
        {
            var sommets = new Vector3[segments + 1];
            var uv = new Vector2[segments + 1];
            var normales = new Vector3[segments + 1];
            sommets[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            normales[0] = Vector3.up;

            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                sommets[i + 1] = new Vector3(c, 0f, s);
                uv[i + 1] = new Vector2(0.5f + c * 0.5f, 0.5f + s * 0.5f);
                normales[i + 1] = Vector3.up;
            }

            var tri = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                tri[i * 3] = 0;
                tri[i * 3 + 1] = 1 + (i + 1) % segments;
                tri[i * 3 + 2] = 1 + i;
            }

            var m = new Mesh { name = "DisquePizza" };
            m.vertices = sommets;
            m.uv = uv;
            m.normals = normales;
            m.triangles = tri;
            return m;
        }

        // ------------------------------------------------------------------
        // la texture, calculee une seule fois
        // ------------------------------------------------------------------

        static float Hash(float x, float y)
        {
            float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        static float Bruit(float x, float y)
        {
            float xi = Mathf.Floor(x), yi = Mathf.Floor(y);
            float u = x - xi, v = y - yi;
            u = u * u * (3f - 2f * u);
            v = v * v * (3f - 2f * v);
            float a = Hash(xi, yi), b = Hash(xi + 1f, yi);
            float c = Hash(xi, yi + 1f), d = Hash(xi + 1f, yi + 1f);
            float haut = a + (b - a) * u, bas = c + (d - c) * u;
            return haut + (bas - haut) * v;
        }

        static Texture2D Texture()
        {
            var tex = new Texture2D(Taille, Taille, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[Taille * Taille];

            var sauce = Bloc.Couleur(0xD2452A);
            var sauceSombre = Bloc.Couleur(0xA82E1B);
            var fromage = Bloc.Couleur(0xFAF3E0);
            var fromageDore = Bloc.Couleur(0xE0B96A);
            var pate = Bloc.Couleur(0xE8B45C);
            var pepperoni = Bloc.Couleur(0xB93225);
            var pepperoniBord = Bloc.Couleur(0x83201A);
            var basilic = Bloc.Couleur(0x59A63C);

            const float c = Taille / 2f, R = Taille / 2f - 2f;

            for (int y = 0; y < Taille; y++)
            for (int x = 0; x < Taille; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                // fond : sauce mouchetee, cernee d'un bord de pate
                float grain = Bruit(x * 0.06f, y * 0.06f);
                var couleur = Color.Lerp(sauceSombre, sauce, grain);
                // un lisere de pate au bord, la ou la sauce s'arrete
                if (d > R * 0.94f)
                    couleur = Color.Lerp(couleur, pate, Mathf.Clamp01((d - R * 0.94f) / (R * 0.06f)));

                // Mozzarella : elle couvre l'essentiel de la pizza, la sauce ne
                // remontant que par endroits. Un seuil trop haut donnait des
                // flaques isolees sur un fond rouge.
                float flaques = Bruit(x * 0.032f + 40f, y * 0.032f + 40f);
                float bord = Bruit(x * 0.11f + 7f, y * 0.11f + 7f) * 0.08f;
                if (flaques + bord > 0.40f && d < R * 0.95f)
                {
                    float epaisseur = Mathf.Clamp01((flaques + bord - 0.40f) * 7f);
                    var f = Color.Lerp(fromage, fromageDore,
                                       Mathf.Clamp01((Bruit(x * 0.05f + 90f, y * 0.05f + 90f) - 0.55f) * 4f));
                    couleur = Color.Lerp(couleur, f, epaisseur);
                }

                px[y * Taille + x] = couleur;
            }

            Rondelles(px, pepperoni, pepperoniBord, basilic);
            Origan(px);

            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        /// <summary>Pepperoni, basilic et brins d'herbe, poses sur la sauce.</summary>
        static void Rondelles(Color32[] px, Color pepperoni, Color bord, Color basilic)
        {
            const float c = Taille / 2f, R = Taille / 2f - 2f;

            // Repartition en spirale d'angle d'or plutot qu'au hasard : tire au
            // sort, les rondelles s'agglutinent d'un cote et laissent des vides.
            const float AngleOr = 2.39996f;

            for (int i = 0; i < 10; i++)
            {
                float a = i * AngleOr + Hash(i * 3.1f, 1.7f) * 0.5f;
                float rad = Mathf.Sqrt((i + 0.6f) / 10f) * R * 0.74f;
                Disque(px, c + Mathf.Cos(a) * rad, c + Mathf.Sin(a) * rad, R * 0.135f, pepperoni, bord);
            }

            for (int i = 0; i < 5; i++)
            {
                float a = i * AngleOr + 1.1f;
                float rad = Mathf.Sqrt((i + 0.4f) / 5f) * R * 0.66f;
                Feuille(px, c + Mathf.Cos(a) * rad, c + Mathf.Sin(a) * rad,
                        Hash(i * 1.9f, 3.3f) * Mathf.PI, basilic);
            }
        }

        /// <summary>Brins d'herbe seches, le detail qui fait "cuit" de pres.</summary>
        static void Origan(Color32[] px)
        {
            const float c = Taille / 2f, R = Taille / 2f - 2f;
            var herbe = Bloc.Couleur(0x6E7A3A);
            for (int i = 0; i < 150; i++)
            {
                float a = Hash(i * 1.37f, 9.1f) * Mathf.PI * 2f;
                float rad = Mathf.Sqrt(Hash(i * 2.11f, 5.7f)) * R * 0.9f;
                int x = (int)(c + Mathf.Cos(a) * rad), y = (int)(c + Mathf.Sin(a) * rad);
                if (x < 1 || y < 1 || x >= Taille - 1 || y >= Taille - 1) continue;
                px[y * Taille + x] = herbe;
                if (Hash(i * 3.3f, 1.1f) > 0.5f) px[y * Taille + x + 1] = herbe;
                else px[(y + 1) * Taille + x] = herbe;
            }
        }

        static void Disque(Color32[] px, float cx, float cy, float r, Color plein, Color bord)
        {
            int x0 = Mathf.Max(0, (int)(cx - r)), x1 = Mathf.Min(Taille - 1, (int)(cx + r) + 1);
            int y0 = Mathf.Max(0, (int)(cy - r)), y1 = Mathf.Min(Taille - 1, (int)(cy + r) + 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > r) continue;
                // rebord plus fonce : c'est ce qui fait lire "rondelle"
                px[y * Taille + x] = Color.Lerp(plein, bord, Mathf.Clamp01((d / r - 0.65f) / 0.35f));
            }
        }

        static void Feuille(Color32[] px, float cx, float cy, float angle, Color couleur)
        {
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            float ra = Taille * 0.055f, rb = Taille * 0.028f;
            int r = (int)ra + 2;
            for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
            {
                int px_ = (int)cx + x, py_ = (int)cy + y;
                if (px_ < 0 || py_ < 0 || px_ >= Taille || py_ >= Taille) continue;
                float u = (x * cos + y * sin) / ra, v = (-x * sin + y * cos) / rb;
                float d = Mathf.Sqrt(u * u + v * v);
                if (d > 1f) continue;
                px[py_ * Taille + px_] = Color.Lerp(couleur, Color.Lerp(couleur, Color.black, 0.35f), d);
            }
        }
    }
}
