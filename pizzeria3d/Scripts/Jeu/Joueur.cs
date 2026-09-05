using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le pizzaiolo : deplacement au doigt facon manette flottante, et pile de
    /// pizzas portee a bout de bras. Le transfert vers/depuis les stations se fait
    /// pizza par pizza, par proximite — c'est le geste central du genre.
    /// </summary>
    public sealed class Joueur : MonoBehaviour
    {
        public Pile Portee;

        Transform _corps;
        Demarche _demarche;
        float _compteurTransfert;

        public Vector3 Position => transform.position;

        void Awake()
        {
            _corps = transform.Find("Corps");
            _demarche = GetComponent<Demarche>();
            if (Portee == null) Portee = Portage.Creer(transform, "PilePortee");
            Portee.Max = Reglages.CapacitePortee;
        }

        void Update()
        {
            Deplacer(Direction() * Reglages.VitesseJoueur * Time.deltaTime);
            if (_compteurTransfert > 0f) _compteurTransfert -= Time.deltaTime;
            // bras tendus tant qu'il tient quelque chose, pendants sinon
            if (_demarche != null) _demarche.BrasPortent = !Portee.EstVide;
        }

        Transform _camera;

        /// <summary>
        /// La direction vient de la manette, exprimee en repere ecran. Elle est
        /// reportee sur les axes de la CAMERA, et non sur des axes ecrits en
        /// dur : ceux-ci etaient tournes d'un quart de tour, si bien que
        /// pousser a droite faisait descendre le personnage a l'ecran.
        /// </summary>
        Vector3 Direction()
        {
            var m = Manette.Active;
            if (m == null) return Vector3.zero;
            var plan = m.Direction;
            if (plan.x == 0f && plan.y == 0f) return Vector3.zero;

            if (_camera == null)
            {
                var cam = Object.FindObjectOfType<Camera>();
                if (cam == null) return Vector3.zero;
                _camera = cam.transform;
            }

            var avant = _camera.forward; avant.y = 0f; avant = avant.normalized;
            var droite = _camera.right;  droite.y = 0f; droite = droite.normalized;
            return droite * plan.x + avant * plan.y;
        }

        void Deplacer(Vector3 pas)
        {
            if (pas.sqrMagnitude <= 0f) return;
            transform.position = Obstacles.Resoudre(transform.position, pas, Reglages.RayonJoueur);
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
