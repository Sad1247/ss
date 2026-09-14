using System.Collections.Generic;
using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Les murs du jeu, vus de dessus : une liste de rectangles et les bornes
    /// du terrain. Le deplacement est resolu a la main plutot que par le moteur
    /// physique — pas de rigidbody a regler, pas de collider a caler, et un
    /// resultat identique a chaque partie, donc verifiable hors editeur.
    /// </summary>
    public static class Obstacles
    {
        struct Boite
        {
            public float xMin, xMax, zMin, zMax;
        }

        static readonly List<Boite> _boites = new List<Boite>();
        static float _terrainX = 999f, _terrainZ = 999f;

        public static int Nombre => _boites.Count;

        public static void Reinitialiser()
        {
            _boites.Clear();
            _terrainX = _terrainZ = 999f;
        }

        /// <summary>Le joueur ne sort pas de ce rectangle, centre sur l'origine.</summary>
        public static void DefinirTerrain(float demiX, float demiZ)
        {
            _terrainX = demiX;
            _terrainZ = demiZ;
        }

        /// <summary>Pose un mur et renvoie son numero, pour pouvoir le lever.</summary>
        public static int Ajouter(Vector3 centre, float largeurX, float largeurZ)
        {
            _boites.Add(new Boite
            {
                xMin = centre.x - largeurX * 0.5f, xMax = centre.x + largeurX * 0.5f,
                zMin = centre.z - largeurZ * 0.5f, zMax = centre.z + largeurZ * 0.5f,
            });
            return _boites.Count - 1;
        }

        /// <summary>
        /// Leve un obstacle pose plus tot — le seuil d'une porte qu'on ouvre.
        /// La boite est videe plutot que retiree : les numeros deja distribues
        /// resteraient sinon accroches au mauvais mur.
        /// </summary>
        public static void Ouvrir(int numero)
        {
            if (numero < 0 || numero >= _boites.Count) return;
            _boites[numero] = new Boite { xMin = 0f, xMax = 0f, zMin = 0f, zMax = 0f };
        }

        /// <summary>Le rectangle ou le joueur peut aller, pour l'agrandir.</summary>
        public static float DemiTerrainX => _terrainX;
        public static float DemiTerrainZ => _terrainZ;

        public static bool Bloque(Vector3 p, float rayon) => Dedans(p, rayon) >= 0;

        /// <summary>
        /// Vrai si l'on peut vraiment se tenir la : hors de tout meuble, et
        /// dans le terrain. Un point libre mais hors des bornes n'est pas une
        /// place — le joueur y serait ramene de force a l'image suivante.
        /// </summary>
        public static bool Praticable(Vector3 p, float rayon)
            => !Bloque(p, rayon) && Mathf.Abs(p.x) <= _terrainX && Mathf.Abs(p.z) <= _terrainZ;

        /// <summary>Le numero du premier obstacle ou ce point se trouve, -1 sinon.</summary>
        static int Dedans(Vector3 p, float rayon)
        {
            for (int i = 0; i < _boites.Count; i++)
            {
                var b = _boites[i];
                if (b.xMax <= b.xMin) continue;      // obstacle leve
                if (p.x > b.xMin - rayon && p.x < b.xMax + rayon &&
                    p.z > b.zMin - rayon && p.z < b.zMax + rayon) return i;
            }
            return -1;
        }

        /// <summary>
        /// Sort un point d'un meuble ou il se trouve deja, par le cote dont il
        /// est le plus pres. C'est le filet de securite du deplacement : une
        /// table qu'on vient de payer, une piece qui s'ouvre, un mur pose
        /// pendant la partie — quoi qu'il arrive sous ses pieds, le joueur
        /// n'est jamais enferme dedans. Plusieurs passes, parce que sortir
        /// d'une boite peut poser le pied dans sa voisine.
        /// </summary>
        public static Vector3 Degager(Vector3 p, float rayon)
        {
            const float pouce = 0.001f;              // pour rester franchement dehors
            for (int passe = 0; passe < 4; passe++)
            {
                int i = Dedans(p, rayon);
                if (i < 0) break;
                var b = _boites[i];

                float gauche = p.x - (b.xMin - rayon);
                float droite = (b.xMax + rayon) - p.x;
                float arriere = p.z - (b.zMin - rayon);
                float avant = (b.zMax + rayon) - p.z;

                float court = Mathf.Min(Mathf.Min(gauche, droite), Mathf.Min(arriere, avant));
                if (court == gauche) p.x = b.xMin - rayon - pouce;
                else if (court == droite) p.x = b.xMax + rayon + pouce;
                else if (court == arriere) p.z = b.zMin - rayon - pouce;
                else p.z = b.zMax + rayon + pouce;
            }
            p.x = Mathf.Clamp(p.x, -_terrainX, _terrainX);
            p.z = Mathf.Clamp(p.z, -_terrainZ, _terrainZ);
            return p;
        }

        /// <summary>
        /// Applique un pas en essayant les deux axes separement : bloque contre
        /// un mur, on continue de glisser le long au lieu de rester colle.
        /// </summary>
        public static Vector3 Resoudre(Vector3 depart, Vector3 pas, float rayon)
        {
            // Un meuble a pu apparaitre sous ses pieds depuis le dernier pas :
            // on le remet dehors avant de bouger, sinon chaque pas serait
            // refuse et il ne repartirait jamais.
            var p = Degager(depart, rayon);

            var enX = new Vector3(p.x + pas.x, p.y, p.z);
            if (!Bloque(enX, rayon)) p = enX;

            var enZ = new Vector3(p.x, p.y, p.z + pas.z);
            if (!Bloque(enZ, rayon)) p = enZ;

            p.x = Mathf.Clamp(p.x, -_terrainX, _terrainX);
            p.z = Mathf.Clamp(p.z, -_terrainZ, _terrainZ);
            return p;
        }
    }
}
