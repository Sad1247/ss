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

        Demarche _demarche;
        float _compteurTransfert;

        public Vector3 Position => transform.position;

        void Awake()
        {
            _demarche = GetComponent<Demarche>();
            if (Portee == null) Portee = Portage.Creer(transform, "PilePortee");
            Portee.Max = Reglages.CapacitePortee;
        }

        /// <summary>
        /// Assis au fauteuil du bureau. La manette le remet debout : c'est le
        /// seul moyen d'en sortir, et il n'y a rien a apprendre pour cela.
        /// </summary>
        public bool Assis { get; private set; }

        void Update()
        {
            var direction = Direction();
            if (Assis)
            {
                if (direction.sqrMagnitude <= 0f) return;
                SeLever();
            }

            Deplacer(direction * Reglages.VitesseJoueur * Time.deltaTime);
            // Un meuble a pu s'allumer sous ses pieds — une table qu'on vient
            // de payer, une piece qui s'ouvre. Il en ressort de lui-meme,
            // meme sans toucher a la manette : rien ne l'y enferme.
            if (!Assis) transform.position = Obstacles.Degager(transform.position, Reglages.RayonJoueur);
            if (_compteurTransfert > 0f) _compteurTransfert -= Time.deltaTime;
            // bras tendus tant qu'il tient quelque chose, pendants sinon
            if (_demarche != null) _demarche.BrasPortent = !Portee.EstVide;
        }

        /// <summary>Le pose sur un siege, le regard tourne vers son bureau.</summary>
        public void Asseoir(Vector3 place, Vector3 regard)
        {
            Assis = true;
            transform.position = new Vector3(place.x, Reglages.HauteurAssise, place.z);
            if (regard.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(regard.normalized, Vector3.up);
            if (_demarche != null) _demarche.Assis = true;
        }

        /// <summary>Le remet sur ses pieds, la ou etait le siege.</summary>
        public void SeLever()
        {
            Assis = false;
            var p = transform.position;
            transform.position = new Vector3(p.x, 0f, p.z);
            if (_demarche != null) _demarche.Assis = false;
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
            // Le cap est porte par la racine, pas par le buste : la demarche
            // penche le buste vers l'avant, et les deux se marcheraient dessus.
            var cible = Quaternion.LookRotation(pas.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, cible,
                                                  Reglages.VitesseRotation * Time.deltaTime);
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
