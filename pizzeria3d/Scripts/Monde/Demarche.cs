using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Fait marcher un personnage : jambes et bras se balancent, le buste
    /// rebondit. L'allure est deduite du deplacement reel plutot que d'un
    /// ordre exterieur — le meme composant sert donc au joueur comme aux
    /// employes, sans qu'aucun n'ait a le prevenir.
    /// </summary>
    public sealed class Demarche : MonoBehaviour
    {
        public Transform Corps, HancheG, HancheD, EpauleG, EpauleD;
        public Transform GenouG, GenouD;

        /// <summary>Amplitude du balancement, en degres, a pleine vitesse.</summary>
        public float Amplitude = 56f;
        /// <summary>Flexion maximale du genou, en degres, a pleine vitesse.</summary>
        public float Genou = 34f;
        /// <summary>
        /// Inclinaison du buste vers l'avant, en degres, a pleine vitesse.
        /// C'est elle qui donne l'elan : jambes seules, le personnage avance
        /// tout droit comme un automate.
        /// </summary>
        public float Inclinaison = 13f;
        public float VitesseReference = 6f;

        /// <summary>
        /// Vrai quand le personnage porte quelque chose : les bras se figent
        /// alors devant lui. Sans cela la pile, posee au point de portage,
        /// resterait immobile pendant que les mains, elles, se balanceraient.
        /// </summary>
        public bool BrasPortent;

        /// <summary>
        /// Assis a table : cuisses a l'horizontale, tibias vers le sol. La
        /// demarche s'arrete entierement, sinon le pas repasserait par-dessus
        /// la pose a l'image suivante.
        /// </summary>
        public bool Assis;

        Vector3 _precedente;
        float _phase;
        float _allure;          // 0 a l'arret, 1 a pleine vitesse

        public float Phase => _phase;
        public float Allure => _allure;

        void Awake() => _precedente = transform.position;

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            var delta = transform.position - _precedente;
            delta.y = 0f;
            _precedente = transform.position;

            if (Assis) { Asseoir(); return; }

            float vitesse = delta.magnitude / dt;
            float voulue = Mathf.Clamp01(vitesse / VitesseReference);
            // lissage : sans lui, la moindre saccade ferait tressauter les jambes
            _allure = Mathf.Lerp(_allure, voulue, Mathf.Clamp01(dt * 10f));

            // cadence : le pas est court et rapide, comme dans la reference
            if (_allure > 0.02f) _phase += dt * (5f + _allure * 9f);
            else _phase = Mathf.Lerp(_phase, 0f, Mathf.Clamp01(dt * 8f));

            float angle = Mathf.Sin(_phase) * Amplitude * _allure;
            Tourner(HancheG, angle);
            Tourner(HancheD, -angle);

            // Le genou ne plie que vers l'arriere, et surtout quand la jambe
            // part derriere puis revient : plier dans l'autre sens donnerait
            // une jambe cassee a l'envers.
            Tourner(GenouG, Flexion(_phase));
            Tourner(GenouD, Flexion(_phase + Mathf.PI));

            if (BrasPortent)
            {
                Tourner(EpauleG, Personnage.AngleBrasPortant);
                Tourner(EpauleD, Personnage.AngleBrasPortant);
            }
            else
            {
                // les bras suivent de loin : ce sont les jambes qui marchent
                Tourner(EpauleG, -angle * 0.45f);
                Tourner(EpauleD, angle * 0.45f);
            }

            if (Corps == null) return;
            var p = Corps.localPosition;
            // rebond au pas quand il marche, respiration quand il attend
            p.y = _allure > 0.02f
                ? Mathf.Abs(Mathf.Sin(_phase)) * 0.07f * _allure
                : Mathf.Sin(Time.time * 1.6f) * 0.02f;
            Corps.localPosition = p;

            // Le buste penche dans le sens de la marche. Le cap, lui, est
            // porte par la racine du personnage : les deux ne se genent pas.
            Corps.localRotation = Quaternion.Euler(Inclinaison * _allure, 0f, 0f);
        }

        /// <summary>La pose assise : jambes pliees, buste droit, rien qui bouge.</summary>
        void Asseoir()
        {
            _allure = 0f;
            _phase = 0f;
            Tourner(HancheG, -78f);
            Tourner(HancheD, -78f);
            Tourner(GenouG, 78f);
            Tourner(GenouD, 78f);
            Tourner(EpauleG, -12f);
            Tourner(EpauleD, -12f);

            if (Corps == null) return;
            var p = Corps.localPosition;
            p.y = 0f;
            Corps.localPosition = p;
            Corps.localRotation = Quaternion.identity;
        }

        float Flexion(float phase)
            => Genou * _allure * Mathf.Clamp01(Mathf.Sin(phase - 0.6f));

        static void Tourner(Transform pivot, float angle)
        {
            if (pivot != null) pivot.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
