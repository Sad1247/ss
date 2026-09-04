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

        public static Material Peinture(Color couleur)
        {
            var m = new Material(Shader());
            m.color = couleur;
            // surfaces mates : le style ne supporte pas les reflets
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.05f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
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
        public static readonly Color MachineBis = Couleur(0x1B49B5);
        public static readonly Color Metal      = Couleur(0xB9C2CC);
        public static readonly Color Pate       = Couleur(0xF5B942);
        public static readonly Color Sauce      = Couleur(0xE8452B);
        public static readonly Color Croute     = Couleur(0xE8A33D);
        public static readonly Color Carton     = Couleur(0xF0A93C);
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
