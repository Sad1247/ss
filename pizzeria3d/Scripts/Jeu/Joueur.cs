using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le pizzaiolo : deplacement au doigt facon manette flottante, et pile de
    /// pizzas portee sur la tete. Le transfert vers/depuis les stations se fait
    /// pizza par pizza, par proximite — c'est le geste central du genre.
    /// </summary>
    public sealed class Joueur : MonoBehaviour
    {
        public Pile Portee;

        Transform _corps;
        float _compteurTransfert;

        public Vector3 Position => transform.position;

        void Awake()
        {
            _corps = transform.Find("Corps");
            if (Portee == null)
            {
                var p = new GameObject("PilePortee");
                p.transform.SetParent(transform, false);
                p.transform.localPosition = new Vector3(0f, Reglages.HauteurTete, 0f);
                Portee = p.AddComponent<Pile>();
            }
            Portee.Max = Reglages.CapacitePortee;
        }

        void Update()
        {
            Deplacer(Direction() * Reglages.VitesseJoueur * Time.deltaTime);
            if (_compteurTransfert > 0f) _compteurTransfert -= Time.deltaTime;
        }

        /// <summary>
        /// La direction vient de la manette a l'ecran ; l'ecran etant tourne de
        /// 45 degres par rapport au monde, on la reporte sur les axes
        /// isometriques pour que "vers le haut" aille vraiment vers le fond.
        /// </summary>
        Vector3 Direction()
        {
            var m = Manette.Active;
            if (m == null) return Vector3.zero;
            var plan = m.Direction;
            if (plan.x == 0f && plan.y == 0f) return Vector3.zero;

            var avant = new Vector3(1f, 0f, 1f).normalized;
            var droite = new Vector3(1f, 0f, -1f).normalized;
            return droite * plan.x + avant * plan.y;
        }

        void Deplacer(Vector3 pas)
        {
            if (pas.sqrMagnitude <= 0f) return;
            transform.position += pas;
            if (_corps != null)
            {
                var cible = Quaternion.LookRotation(pas.normalized, Vector3.up);
                _corps.rotation = Quaternion.Slerp(_corps.rotation, cible,
                                                   Reglages.VitesseRotation * Time.deltaTime);
            }
        }

        public bool PretPourTransfert => _compteurTransfert <= 0f;
        public void ArmerTransfert() => _compteurTransfert = Reglages.DelaiTransfert;

        public bool EstPres(Vector3 point, float rayon)
        {
            var d = transform.position - point;
            d.y = 0f;
            return d.sqrMagnitude <= rayon * rayon;
        }
    }
}
