namespace Pizzeria3D
{
    /// <summary>
    /// La courbe productivite/fatigue, commune a tout employe qui en aurait
    /// besoin — le caissier aujourd'hui, un futur manager ou comptable
    /// demain. Une seule courbe a regler, pas une par metier.
    /// </summary>
    public static class Fatigue
    {
        /// <summary>
        /// Le rendement d'un employe, entre 0 et 1, selon sa fatigue :
        /// 100% en dessous de 40, 90% jusqu'a 70, 60% jusqu'a 90, 40% au-dela.
        /// </summary>
        public static float Productivite(float fatigue)
        {
            if (fatigue < 40f) return 1.0f;
            if (fatigue < 70f) return 0.9f;
            if (fatigue < 90f) return 0.6f;
            return 0.4f;
        }
    }
}
