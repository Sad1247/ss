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
        static Mesh[] _quartiers;
        static Sprite _icone;

        /// <summary>La meme garniture, en sprite, pour l'interface.</summary>
        public static Sprite Icone
        {
            get
            {
                if (_icone == null)
                {
                    var t = Texture();
                    _icone = Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                                           new Vector2(0.5f, 0.5f), 100f);
                }
                return _icone;
            }
        }

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
            pate.transform.localScale = new Vector3(Reglages.DiametrePizza,
                                                   Reglages.EpaisseurPizza * 0.5f,
                                                   Reglages.DiametrePizza);
            pate.GetComponent<Renderer>().sharedMaterial = _pate;
            pate.SansCollision();

            var dessus = new GameObject("Garniture", typeof(MeshFilter), typeof(MeshRenderer));
            dessus.transform.SetParent(racine.transform, false);
            dessus.transform.localPosition = new Vector3(0f, Reglages.EpaisseurPizza * 0.505f, 0f);
            // le disque laisse voir la tranche doree tout autour
            float rayonGarniture = Reglages.DiametrePizza * 0.42f;
            dessus.transform.localScale = new Vector3(rayonGarniture, 1f, rayonGarniture);
            dessus.GetComponent<MeshFilter>().sharedMesh = _disque;
            dessus.GetComponent<MeshRenderer>().sharedMaterial = _garniture;

            return racine;
        }

        static void Preparer()
        {
            if (_disque == null) _disque = Disque(Segments);
            if (_pate == null) _pate = Bloc.Peinture(Bloc.Couleur(0xD9A867));
            if (_garniture == null)
            {
                _garniture = Bloc.Peinture(Color.white);
                _garniture.mainTexture = Texture();
            }
        }

        /// <summary>
        /// Un quartier de pizza posee a plat : le quart numero <paramref
        /// name="quartier"/> d'une pizza entiere, pate et garniture. C'est ce
        /// qui permet de la manger part par part au lieu de la faire
        /// disparaitre d'un bloc.
        /// </summary>
        public static GameObject Quartier(Transform parent, int quartier)
        {
            Preparer();
            if (_quartiers == null) _quartiers = new Mesh[4];
            if (_quartiers[quartier] == null)
                _quartiers[quartier] = Secteur(Segments, quartier * 0.25f, (quartier + 1) * 0.25f);

            var racine = new GameObject("Quartier" + quartier);
            racine.transform.SetParent(parent, false);

            // la pate, en secteur elle aussi, pour que la tranche doree suive
            var pate = new GameObject("PatePart", typeof(MeshFilter), typeof(MeshRenderer));
            pate.transform.SetParent(racine.transform, false);
            float rayonPate = Reglages.DiametrePizza * 0.5f;
            pate.transform.localScale = new Vector3(rayonPate, 1f, rayonPate);
            pate.GetComponent<MeshFilter>().sharedMesh = _quartiers[quartier];
            pate.GetComponent<MeshRenderer>().sharedMaterial = _pate;

            var dessus = new GameObject("GarniturePart", typeof(MeshFilter), typeof(MeshRenderer));
            dessus.transform.SetParent(racine.transform, false);
            dessus.transform.localPosition = new Vector3(0f, Reglages.EpaisseurPizza * 0.4f, 0f);
            float rayonGarniture = Reglages.DiametrePizza * 0.42f;
            dessus.transform.localScale = new Vector3(rayonGarniture, 1f, rayonGarniture);
            dessus.GetComponent<MeshFilter>().sharedMesh = _quartiers[quartier];
            dessus.GetComponent<MeshRenderer>().sharedMaterial = _garniture;

            return racine;
        }

        /// <summary>
        /// Secteur de disque, de <paramref name="debut"/> a <paramref
        /// name="fin"/> de tour. Les UV restent ceux du disque entier : chaque
        /// part porte donc sa propre portion de garniture.
        /// </summary>
        static Mesh Secteur(int segments, float debut, float fin)
        {
            int pas = Mathf.Max(2, Mathf.CeilToInt(segments * (fin - debut)));
            var sommets = new Vector3[pas + 2];
            var uv = new Vector2[pas + 2];
            var normales = new Vector3[pas + 2];

            sommets[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            normales[0] = Vector3.up;

            for (int i = 0; i <= pas; i++)
            {
                float a = (debut + (fin - debut) * i / pas) * Mathf.PI * 2f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                sommets[i + 1] = new Vector3(c, 0f, s);
                uv[i + 1] = new Vector2(0.5f + c * 0.5f, 0.5f + s * 0.5f);
                normales[i + 1] = Vector3.up;
            }

            var tri = new int[pas * 3];
            for (int i = 0; i < pas; i++)
            {
                tri[i * 3] = 0;
                tri[i * 3 + 1] = i + 2;
                tri[i * 3 + 2] = i + 1;
            }

            var m = new Mesh { name = "QuartierPizza" };
            m.vertices = sommets;
            m.uv = uv;
            m.normals = normales;
            m.triangles = tri;
            return m;
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

            // couleurs relevees sur la photo de reference
            // Teintes desaturees, plus proches d'une vraie pizza cuite : le
            // rouge vif et le jaune d'or precedents faisaient jouet.
            var sauce       = Bloc.Couleur(0xB84A2C);
            var sauceSombre = Bloc.Couleur(0x8E3520);
            var fondu       = Bloc.Couleur(0xE8DAB0);   // mozzarella fondue, ivoire
            var fonduClair  = Bloc.Couleur(0xF2E8CE);
            var croute      = Bloc.Couleur(0xD9A867);
            var crouteDoree = Bloc.Couleur(0xB07A3E);

            const float c = Taille / 2f, R = Taille / 2f - 2f;
            const float FinGarniture = 0.68f;   // au-dela, la sauce puis la croute
            const float DebutCroute  = 0.75f;   // bourrelet large : un quart du rayon

            for (int y = 0; y < Taille; y++)
            for (int x = 0; x < Taille; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / R;

                float grain = Bruit(x * 0.07f, y * 0.07f);
                var couleur = Color.Lerp(sauceSombre, sauce, grain);

                // couche de fromage fondu, percee la ou la sauce remonte
                if (d < FinGarniture)
                {
                    float nappe = Bruit(x * 0.035f + 40f, y * 0.035f + 40f)
                                + Bruit(x * 0.12f + 7f, y * 0.12f + 7f) * 0.10f;
                    if (nappe > 0.37f)
                    {
                        float epaisseur = Mathf.Clamp01((nappe - 0.37f) * 6f);
                        var f = Color.Lerp(fondu, fonduClair, grain);
                        couleur = Color.Lerp(couleur, f, epaisseur);
                    }
                }

                // La croute est dans la texture, et pas seulement dans le
                // volume : c'est elle qui donne son epaisseur a la pizza vue
                // de dessus, comme sur la photo.
                if (d > DebutCroute)
                {
                    float t = Mathf.Clamp01((d - DebutCroute) / (1f - DebutCroute));
                    float cloques = Bruit(x * 0.09f + 130f, y * 0.09f + 130f);
                    var pate = Color.Lerp(croute, crouteDoree, Mathf.Clamp01((cloques - 0.45f) * 2.6f));
                    couleur = Color.Lerp(couleur, pate, Mathf.Clamp01(t * 3f));
                    // ombre du bourrelet, tout au bord
                    couleur = Color.Lerp(couleur, Color.Lerp(pate, Color.black, 0.35f),
                                         Mathf.Clamp01((t - 0.72f) / 0.28f));
                }

                // Hors du disque, transparent : le maillage 3D n'en montre
                // jamais les coins, mais l'icone de l'interface, si — et un
                // carre de sauce n'a rien d'un logo de pizza.
                couleur.a = Mathf.Clamp01((1f - d) * R);
                px[y * Taille + x] = couleur;
            }

            Mozzarella(px);
            Rondelles(px);
            Origan(px);

            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        /// <summary>
        /// Les morceaux de mozzarella. Sur la photo ce sont des taches blanches
        /// bien distinctes posees sur le fondu, pas une nappe uniforme.
        /// </summary>
        static void Mozzarella(Color32[] px)
        {
            const float c = Taille / 2f, R = Taille / 2f - 2f;
            const float AngleOr = 2.39996f;
            var blanc = Bloc.Couleur(0xF6EFDF);
            var ombre = Bloc.Couleur(0xD8C9AB);

            for (int i = 0; i < 16; i++)
            {
                float a = i * AngleOr + 0.6f;      // l'angle d'or, tel quel : le multiplier le detruit
                float rad = Mathf.Sqrt((i + 0.5f) / 16f) * R * 0.60f;
                float cx = c + Mathf.Cos(a) * rad, cy = c + Mathf.Sin(a) * rad;
                float r = R * (0.055f + Hash(i * 1.3f, 6.4f) * 0.045f);

                int x0 = Mathf.Max(0, (int)(cx - r - 2)), x1 = Mathf.Min(Taille - 1, (int)(cx + r + 2));
                int y0 = Mathf.Max(0, (int)(cy - r - 2)), y1 = Mathf.Min(Taille - 1, (int)(cy + r + 2));
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // contour bosselé : un cercle net ferait pastille
                    float bord = r * (0.82f + Bruit(x * 0.16f + i * 13f, y * 0.16f) * 0.36f);
                    if (d > bord) continue;
                    var col = Color.Lerp(blanc, ombre, Mathf.Clamp01((d / bord - 0.6f) / 0.4f));
                    col.a = px[y * Taille + x].a / 255f;
                    px[y * Taille + x] = col;
                }
            }
        }

        /// <summary>Pepperoni et basilic, par-dessus le fromage.</summary>
        static void Rondelles(Color32[] px)
        {
            const float c = Taille / 2f, R = Taille / 2f - 2f;
            const float AngleOr = 2.39996f;
            var pepperoni = Bloc.Couleur(0xA8402C);
            var bord = Bloc.Couleur(0x7A2A1C);
            var basilic = Bloc.Couleur(0x407A33);

            // Repartition en spirale d'angle d'or plutot qu'au hasard : tire au
            // sort, les rondelles s'agglutinent d'un cote et laissent des vides.
            for (int i = 0; i < 14; i++)
            {
                float a = i * AngleOr + Hash(i * 3.1f, 1.7f) * 0.35f;
                float rad = Mathf.Sqrt((i + 0.55f) / 14f) * R * 0.62f;
                Disque(px, c + Mathf.Cos(a) * rad, c + Mathf.Sin(a) * rad, R * 0.115f, pepperoni, bord);
            }

            for (int i = 0; i < 6; i++)
            {
                float a = i * AngleOr + 1.9f;
                float rad = Mathf.Sqrt((i + 0.35f) / 6f) * R * 0.55f;
                Feuille(px, c + Mathf.Cos(a) * rad, c + Mathf.Sin(a) * rad,
                        Hash(i * 1.9f, 3.3f) * Mathf.PI, basilic);
            }
        }

        /// <summary>Brins d'herbe seches, le detail qui fait "cuit" de pres.</summary>
        static void Origan(Color32[] px)
        {
            const float c = Taille / 2f, R = Taille / 2f - 2f;
            var herbe = Bloc.Couleur(0x6A6F3C);
            for (int i = 0; i < 150; i++)
            {
                float a = Hash(i * 1.37f, 9.1f) * Mathf.PI * 2f;
                float rad = Mathf.Sqrt(Hash(i * 2.11f, 5.7f)) * R * 0.72f;
                int x = (int)(c + Mathf.Cos(a) * rad), y = (int)(c + Mathf.Sin(a) * rad);
                if (x < 1 || y < 1 || x >= Taille - 1 || y >= Taille - 1) continue;
                var col = herbe; col.a = px[y * Taille + x].a / 255f;
                px[y * Taille + x] = col;
                if (Hash(i * 3.3f, 1.1f) > 0.5f) px[y * Taille + x + 1] = col;
                else px[(y + 1) * Taille + x] = col;
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
                var col = Color.Lerp(plein, bord, Mathf.Clamp01((d / r - 0.65f) / 0.35f));
                col.a = px[y * Taille + x].a / 255f;
                px[y * Taille + x] = col;
            }
        }

        /// <summary>
        /// Feuille de basilic : pointue aux deux bouts et parcourue d'une
        /// nervure claire. Une ellipse pleine ressemblait a une olive.
        /// </summary>
        static void Feuille(Color32[] px, float cx, float cy, float angle, Color couleur)
        {
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            float ra = Taille * 0.058f, rb = Taille * 0.026f;
            int r = (int)ra + 2;
            var sombre = Color.Lerp(couleur, Color.black, 0.35f);
            var nervure = Color.Lerp(couleur, Color.white, 0.30f);

            for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
            {
                int ax = (int)cx + x, ay = (int)cy + y;
                if (ax < 0 || ay < 0 || ax >= Taille || ay >= Taille) continue;

                float u = (x * cos + y * sin) / ra;      // le long de la feuille
                float v = (-x * sin + y * cos) / rb;     // en travers
                if (Mathf.Abs(u) > 1f) continue;
                // largeur qui s'annule aux deux pointes
                float largeur = Mathf.Sqrt(Mathf.Clamp01(1f - u * u));
                largeur *= largeur > 0f ? 1f : 0f;
                if (Mathf.Abs(v) > largeur) continue;

                float t = Mathf.Abs(v) / Mathf.Max(largeur, 0.0001f);
                var col = Color.Lerp(couleur, sombre, t * t);
                if (t < 0.14f) col = Color.Lerp(col, nervure, 0.6f);
                col.a = px[ay * Taille + ax].a / 255f;
                px[ay * Taille + ax] = col;
            }
        }
    }
}
