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

        void OnEnable()
        {
            Obstacles.Ouvrir(Passage);
            Obstacles.DefinirTerrain(Obstacles.DemiTerrainX, DemiTerrainZ);
            if (Gond != null) Gond.localRotation = Quaternion.Euler(0f, AngleOuvert, 0f);
        }
    }
}
