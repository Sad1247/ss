using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La fumee du four : des bouffees qui montent, s'etalent puis se
    /// resorbent. Elles retrecissent au lieu de s'effacer — pas besoin de
    /// materiau transparent, qui se configure differemment selon le pipeline.
    /// </summary>
    public sealed class Fumee : MonoBehaviour
    {
        public float Intervalle = 0.5f;
        float _compteur;

        /// <summary>
        /// Gele les bouffees, comme Horloge.Figee gele l'heure : sert au banc
        /// d'essai, qui doit pouvoir avancer le temps sans que des bouffees
        /// tirees au hasard ne consomment le flux aleatoire dont dependent
        /// d'autres verifications, bien plus loin dans le fichier.
        /// </summary>
        public static bool Gelee;

        void Update()
        {
            if (Gelee) return;
            _compteur -= Time.deltaTime;
            if (_compteur > 0f) return;
            _compteur = Intervalle;

            var go = Bloc.Bille("Bouffee", transform, Vector3.zero, 0.1f,
                                Bloc.Couleur(0xD8D4CC)).SansCollision();
            var b = go.AddComponent<Bouffee>();
            b.Derive = new Vector3(Random.Range(-0.25f, 0.25f), 0f, Random.Range(-0.2f, 0.35f));
            b.Taille = Random.Range(0.45f, 0.75f);
            b.Duree = Random.Range(2.2f, 3.2f);
        }
    }

    public sealed class Bouffee : MonoBehaviour
    {
        public Vector3 Derive;
        public float Taille = 0.6f;
        public float Duree = 2.6f;

        float _age;

        void Update()
        {
            _age += Time.deltaTime;
            float t = _age / Duree;
            if (t >= 1f) { Destroy(gameObject); return; }

            transform.localPosition += (Vector3.up * 0.75f + Derive) * Time.deltaTime;

            // gonfle vite, se resorbe lentement
            float taille = Taille * (t < 0.3f ? t / 0.3f : 1f - (t - 0.3f) / 0.7f);
            transform.localScale = new Vector3(taille, taille, taille);
        }
    }
}
