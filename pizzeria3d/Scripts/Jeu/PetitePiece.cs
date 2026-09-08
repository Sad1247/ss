using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La piece qui s'ouvre derriere la porte du fond, une fois payee. Elle ne
    /// se contente pas d'apparaitre : elle leve le seuil qui barrait la porte,
    /// repousse la limite du terrain jusqu'au fond de la piece, et ouvre le
    /// vantail. Sans ces trois gestes, on verrait une piece ou l'on ne peut
    /// pas entrer.
    /// </summary>
    public sealed class PetitePiece : MonoBehaviour
    {
        /// <summary>Numero de l'obstacle qui bouche le pas de la porte.</summary>
        public int Passage = -1;
        /// <summary>Le gond du vantail, qui pivote a l'ouverture.</summary>
        public Transform Gond;
        public float DemiTerrainZ = 11.3f;
        /// <summary>
        /// Positif : le vantail rentre dans la piece. Vers la salle, il venait
        /// se planter en travers du passage, blanc et bien visible.
        /// </summary>
        public float AngleOuvert = 96f;

        public Joueur Joueur;
        /// <summary>Le pas de la porte, en coordonnees du monde.</summary>
        public Vector3 Seuil;
        /// <summary>Distance a laquelle le battant s'ecarte devant le joueur.</summary>
        public float RayonOuverture = 2.4f;
        /// <summary>
        /// Passe cette profondeur, il est entre : la porte se referme derriere
        /// lui. Plus court, elle se rabattait alors qu'il etait encore dans
        /// l'encadrement.
        /// </summary>
        public float Profondeur = 1.3f;
        /// <summary>Vitesse du battant, en degres par seconde.</summary>
        public float VitesseBattant = 220f;

        float _angle;

        /// <summary>Vrai quand le battant est franchement ecarte.</summary>
        public bool Ouverte => _angle > AngleOuvert * 0.5f;

        void OnEnable()
        {
            Obstacles.Ouvrir(Passage);
            Obstacles.DefinirTerrain(Obstacles.DemiTerrainX, DemiTerrainZ);
            // Elle s'ouvre toute seule a l'approche : au repos elle reste
            // fermee, comme n'importe quelle porte de bureau.
            Poser();
        }

        void Update()
        {
            float voulu = DoitSOuvrir() ? AngleOuvert : 0f;
            _angle = Mathf.MoveTowards(_angle, voulu, VitesseBattant * Time.deltaTime);
            Poser();
        }

        /// <summary>
        /// Ouverte quand le patron s'en approche du dehors ; refermee des
        /// qu'il est dans le bureau, et bien sur quand il est loin.
        /// </summary>
        bool DoitSOuvrir()
        {
            if (Joueur == null) return false;
            var p = Joueur.Position;
            if (p.z > Seuil.z + Profondeur) return false;   // il est entre
            var d = p - Seuil;
            d.y = 0f;
            return d.sqrMagnitude <= RayonOuverture * RayonOuverture;
        }

        void Poser()
        {
            if (Gond != null) Gond.localRotation = Quaternion.Euler(0f, _angle, 0f);
        }
    }
}
