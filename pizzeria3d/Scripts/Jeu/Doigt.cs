using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Pizzeria3D
{
    /// <summary>
    /// Lecture du doigt ou de la souris, quel que soit le systeme d'entrees du
    /// projet. Unity 6 active par defaut le nouveau systeme, dans lequel
    /// l'ancienne classe Input leve une exception au lieu de repondre : sans
    /// cette couche, le joystick reste muet et la Console se remplit d'erreurs.
    ///
    /// Appeler Lire() une fois par image, avant de consulter les proprietes.
    /// </summary>
    public static class Doigt
    {
        public static bool Tenu { get; private set; }
        public static bool Presse { get; private set; }    // enfonce a cette image
        public static bool Relache { get; private set; }   // relache a cette image
        public static Vector3 Position { get; private set; }

        public static void Lire()
        {
            bool tenu = EstTenu();
            Presse = tenu && !Tenu;
            Relache = !tenu && Tenu;
            Tenu = tenu;
            if (tenu) Position = LirePosition();
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        static bool EstTenu()
        {
            var ecran = Touchscreen.current;
            if (ecran != null && ecran.primaryTouch.press.isPressed) return true;
            var souris = Mouse.current;
            return souris != null && souris.leftButton.isPressed;
        }

        static Vector3 LirePosition()
        {
            var ecran = Touchscreen.current;
            if (ecran != null && ecran.primaryTouch.press.isPressed)
            {
                var t = ecran.primaryTouch.position.ReadValue();
                return new Vector3(t.x, t.y, 0f);
            }
            var souris = Mouse.current;
            if (souris == null) return Vector3.zero;
            var p = souris.position.ReadValue();
            return new Vector3(p.x, p.y, 0f);
        }
#else
        static bool EstTenu() => Input.GetMouseButton(0);
        static Vector3 LirePosition() => Input.mousePosition;
#endif
    }
}
