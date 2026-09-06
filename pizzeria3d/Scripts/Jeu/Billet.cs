using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La liasse qui tombe quand un client paie. Elle reste au sol jusqu'a ce
    /// que le joueur passe dessus, puis file vers lui : c'est ce ramassage qui
    /// donne le rythme au genre.
    /// </summary>
    public sealed class Billet : MonoBehaviour
    {
        int _montant;
        Joueur _joueur;
        bool _attire;
        bool _encaisse;
        float _rebond;

        /// <summary>
        /// Lache une liasse. <paramref name="dispersion"/> a 0 la pose pile au
        /// point donne, et <paramref name="hauteur"/> dit a quelle altitude
        /// elle flotte — au sol, ou sur une table.
        /// </summary>
        public static Billet Lacher(Vector3 position, int montant, Joueur joueur,
                                    float dispersion = 1f, float hauteur = 0.35f)
        {
            var go = new GameObject("Billet");
            go.transform.position = position + new Vector3(Random.Range(-0.7f, 0.7f) * dispersion,
                                                          0f,
                                                          Random.Range(-1.2f, -0.3f) * dispersion);
            var b = go.AddComponent<Billet>();
            b._montant = montant;
            b._joueur = joueur;
            b._hauteur = hauteur;

            Bloc.Boite("Liasse", go.transform, Vector3.zero, new Vector3(0.55f, 0.16f, 0.34f), Bloc.Billet)
                .SansCollision();
            Bloc.Boite("Bande", go.transform, new Vector3(0f, 0.09f, 0f), new Vector3(0.2f, 0.04f, 0.36f),
                       Bloc.Couleur(0x2FA82F)).SansCollision();
            return b;
        }

        float _hauteur = 0.35f;

        void Update()
        {
            if (_joueur == null) { Destroy(gameObject); return; }

            if (!_attire)
            {
                // repose au sol en flottant legerement
                _rebond += Time.deltaTime * 4f;
                var p = transform.position;
                p.y = _hauteur + Mathf.Sin(_rebond) * 0.06f;
                transform.position = p;
                transform.Rotate(0f, 60f * Time.deltaTime, 0f);

                if (_joueur.EstPres(transform.position, Reglages.RayonRamassageBillet)) _attire = true;
                return;
            }

            var cible = _joueur.Position + Vector3.up * 1.2f;
            transform.position = Vector3.MoveTowards(transform.position, cible,
                                                     Reglages.VitesseBillet * Time.deltaTime);
            if ((transform.position - cible).sqrMagnitude < 0.05f && !_encaisse)
            {
                _encaisse = true;              // jamais deux fois, meme si l'objet survit une image
                Banque.Encaisser(_montant);
                Destroy(gameObject);
            }
        }
    }
}
