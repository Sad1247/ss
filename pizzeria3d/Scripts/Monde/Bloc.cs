using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Fabrique de formes : tout le decor est bati avec des primitives en
    /// couleurs plates, sans aucun asset a importer. C'est ce qui donne le
    /// look low-poly des jeux du genre.
    /// </summary>
    public static class Bloc
    {
        static Shader _shader;

        /// <summary>Trouve un shader eclaire, quel que soit le pipeline du projet.</summary>
        static Shader Shader()
        {
            if (_shader != null) return _shader;
            string[] noms = {
                "Universal Render Pipeline/Lit",   // URP
                "Standard",                        // built-in
                "Legacy Shaders/Diffuse",
            };
            foreach (var n in noms)
            {
                var s = UnityEngine.Shader.Find(n);
                if (s != null) { _shader = s; return s; }
            }
            Debug.LogError("Aucun shader utilisable trouve.");
            return null;
        }

        static readonly System.Collections.Generic.Dictionary<int, Material> _peintures =
            new System.Collections.Generic.Dictionary<int, Material>();

        /// <summary>
        /// Un materiau par couleur, partage entre tous les objets : le decor
        /// compte des centaines de pieces, en creer un chacun multiplierait les
        /// appels de rendu sans aucun gain.
        /// </summary>
        public static Material Peinture(Color couleur)
        {
            int cle = ((int)(couleur.r * 255) << 16) | ((int)(couleur.g * 255) << 8) | (int)(couleur.b * 255);
            if (_peintures.TryGetValue(cle, out var connu) && connu != null) return connu;

            var m = new Material(Shader());
            m.color = couleur;
            // surfaces mates : le style ne supporte pas les reflets
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.05f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            _peintures[cle] = m;
            return m;
        }

        static readonly System.Collections.Generic.Dictionary<string, Material> _impressions =
            new System.Collections.Generic.Dictionary<string, Material>();

        /// <summary>
        /// Une peinture qui porte une image. Elle ne passe surtout pas par le
        /// cache des couleurs : celui-ci partage un materiau par teinte, et y
        /// poser une texture l'imprimerait sur tout ce qui est de la meme
        /// couleur — le blanc, par exemple, sert deja ailleurs.
        /// </summary>
        public static Material Impression(string nom, Texture2D image)
        {
            if (_impressions.TryGetValue(nom, out var connu) && connu != null) return connu;

            var m = new Material(Shader());
            m.color = Color.white;
            m.mainTexture = image;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.05f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            _impressions[nom] = m;
            return m;
        }

        static readonly System.Collections.Generic.Dictionary<int, Material> _vitrages =
            new System.Collections.Generic.Dictionary<int, Material>();

        /// <summary>
        /// Une peinture translucide, pour les vitres. En URP, un materiau
        /// n'est pas transparent parce que sa couleur a un alpha : il faut lui
        /// dire de passer en surface transparente, choisir le melange, cesser
        /// d'ecrire dans le tampon de profondeur et rejoindre la file de rendu
        /// des transparents. A defaut, la vitre resterait opaque.
        /// </summary>
        public static Material Vitrage(Color couleur, float opacite)
        {
            int cle = ((int)(couleur.r * 255) << 16) | ((int)(couleur.g * 255) << 8)
                    | (int)(couleur.b * 255) | ((int)(opacite * 255) << 24);
            if (_vitrages.TryGetValue(cle, out var connu) && connu != null) return connu;

            var m = new Material(Shader());
            m.color = new Color(couleur.r, couleur.g, couleur.b, opacite);

            // URP
            m.SetFloat("_Surface", 1f);              // 0 opaque, 1 transparent
            m.SetFloat("_Blend", 0f);                // melange alpha classique
            // built-in Standard, si le projet n'est pas en URP : le meme
            // reglage s'y appelle autrement, et sans lui la vitre reste opaque
            m.SetFloat("_Mode", 3f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_AlphaClip", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.SetShaderPassEnabled("ShadowCaster", false);   // une vitre ne porte pas d'ombre
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);

            _vitrages[cle] = m;
            return m;
        }

        /// <summary>Une vitre : une boite peinte d'un vitrage translucide.</summary>
        public static GameObject Verre(string nom, Transform parent, Vector3 pos, Vector3 taille,
                                       Color couleur, float opacite = 0.32f)
        {
            var go = Boite(nom, parent, pos, taille, couleur);
            go.GetComponent<Renderer>().sharedMaterial = Vitrage(couleur, opacite);
            return go;
        }

        public static GameObject Forme(PrimitiveType type, string nom, Transform parent,
                                       Vector3 position, Vector3 taille, Color couleur)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = nom;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = taille;
            go.GetComponent<Renderer>().sharedMaterial = Peinture(couleur);
            return go;
        }

        public static GameObject Boite(string nom, Transform parent, Vector3 pos, Vector3 taille, Color c)
            => Forme(PrimitiveType.Cube, nom, parent, pos, taille, c);

        public static GameObject Disque(string nom, Transform parent, Vector3 pos, float diametre, float epaisseur, Color c)
            => Forme(PrimitiveType.Cylinder, nom, parent, pos, new Vector3(diametre, epaisseur * 0.5f, diametre), c);

        public static GameObject Capsule(string nom, Transform parent, Vector3 pos, Vector3 taille, Color c)
            => Forme(PrimitiveType.Capsule, nom, parent, pos, taille, c);

        public static GameObject Bille(string nom, Transform parent, Vector3 pos, float diametre, Color c)
            => Forme(PrimitiveType.Sphere, nom, parent, pos, new Vector3(diametre, diametre, diametre), c);

        /// <summary>Cylindre couche, face tournee vers l'avant : arches et rondelles.</summary>
        public static GameObject Rondelle(string nom, Transform parent, Vector3 pos,
                                          float diametre, float epaisseur, Color c)
        {
            var go = Forme(PrimitiveType.Cylinder, nom, parent, pos,
                           new Vector3(diametre, epaisseur * 0.5f, diametre), c);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return go;
        }

        /// <summary>Sphere aplatie ou etiree : la brique des formes arrondies.</summary>
        public static GameObject Galet(string nom, Transform parent, Vector3 pos, Vector3 taille,
                                       Color c, float inclinaison = 0f)
        {
            var go = Forme(PrimitiveType.Sphere, nom, parent, pos, taille, c);
            if (inclinaison != 0f) go.transform.localRotation = Quaternion.Euler(inclinaison, 0f, 0f);
            return go;
        }

        static readonly System.Collections.Generic.Dictionary<int, Material> _polis =
            new System.Collections.Generic.Dictionary<int, Material>();

        /// <summary>
        /// Donne a une piece un fini metallique : l'inox d'un plan de travail,
        /// l'aluminium d'un portable. Tout le decor est mat a dessein, mais
        /// peindre le metal du meme fini que le carton lui otait sa matiere —
        /// c'est le seul reflet qui manque au style.
        /// </summary>
        public static GameObject Poli(this GameObject go, float brillance = 0.55f,
                                      float metal = 0.80f)
        {
            var rendu = go.GetComponent<Renderer>();
            if (rendu == null || rendu.sharedMaterial == null) return go;

            var c = rendu.sharedMaterial.color;
            int cle = ((int)(c.r * 255) << 24) | ((int)(c.g * 255) << 16) | ((int)(c.b * 255) << 8)
                    | (int)(Mathf.Clamp01(brillance) * 15f) | ((int)(Mathf.Clamp01(metal) * 15f) << 4);
            if (!_polis.TryGetValue(cle, out var connu) || connu == null)
            {
                connu = new Material(Shader());
                connu.color = c;
                if (connu.HasProperty("_Smoothness")) connu.SetFloat("_Smoothness", brillance);
                if (connu.HasProperty("_Glossiness")) connu.SetFloat("_Glossiness", brillance);
                if (connu.HasProperty("_Metallic")) connu.SetFloat("_Metallic", metal);
                _polis[cle] = connu;
            }
            rendu.sharedMaterial = connu;
            return go;
        }

        /// <summary>Retire le collider : la plupart des objets sont purement decoratifs.</summary>
        public static GameObject SansCollision(this GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
            return go;
        }

        // --- palette, relevee sur la reference ---
        public static readonly Color Sol        = Couleur(0xF6DFA6);
        public static readonly Color SolBordure = Couleur(0xE4E7EA);
        public static readonly Color Herbe      = Couleur(0x7DD44F);
        public static readonly Color Machine    = Couleur(0x2C6BE8);
        // four a bois
        public static readonly Color Brique      = Couleur(0xC2542F);
        public static readonly Color BriqueClaire= Couleur(0xD86F44);
        public static readonly Color Pierre      = Couleur(0xE9DFC6);
        public static readonly Color PierreOmbre = Couleur(0x8E8674);
        public static readonly Color Braise      = Couleur(0xFF6A12);
        public static readonly Color Flamme      = Couleur(0xFFC93C);
        public static readonly Color Foyer       = Couleur(0x2A1712);
        // caisse enregistreuse
        public static readonly Color Taupe       = Couleur(0x4A423B);
        public static readonly Color TaupeClair  = Couleur(0x6B6157);
        public static readonly Color Ecran       = Couleur(0x24282D);
        public static readonly Color MachineBis = Couleur(0x1B49B5);
        public static readonly Color Metal      = Couleur(0xB9C2CC);
        public static readonly Color Pate       = Couleur(0xF5B942);
        public static readonly Color Sauce      = Couleur(0xE8452B);
        public static readonly Color Croute     = Couleur(0xE8A33D);
        // Le carton a pizza est rouge : corps sombre, couvercle plus clair,
        // rainure presque noire — comme la boite du modele.
        public static readonly Color Carton     = Couleur(0x9E2C20);
        public static readonly Color CartonClair= Couleur(0xCB3A2A);
        public static readonly Color CartonOmbre= Couleur(0x7C1F17);
        public static readonly Color Billet     = Couleur(0x5CD65C);
        public static readonly Color Zone       = Couleur(0x4AE04A);
        public static readonly Color Casquette  = Couleur(0xE23B2E);
        public static readonly Color Tablier    = Couleur(0x4BA3E3);
        public static readonly Color Pantalon   = Couleur(0x14161C);
        public static readonly Color Peau       = Couleur(0x2A2320);
        public static readonly Color Client     = Couleur(0xAFC0CC);

        public static Color Couleur(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }
}
