"""Empaquetage de Santinel en exécutable Windows autonome.

    python construire.py              # version normale, sans console
    python construire.py --console    # garde la console noire ouverte : à
                                      # utiliser si l'exe se ferme sans rien dire

Produit  dist/Santinel.exe  : un seul fichier, sans installation de Python
sur les postes. À lancer depuis Windows (PyInstaller ne fait pas de
compilation croisée).
"""

import shutil
import subprocess
import sys
from pathlib import Path

RACINE = Path(__file__).parent
APP = RACINE / "app"


def separateur() -> str:
    """PyInstaller sépare source et destination par ; sous Windows, : ailleurs."""
    return ";" if sys.platform == "win32" else ":"


def construire(console: bool = False) -> int:
    if sys.platform != "win32":
        print("Attention : hors Windows, l'exécutable produit ne sera pas un .exe.")

    for dossier in ("build", "dist"):
        shutil.rmtree(RACINE / dossier, ignore_errors=True)

    s = separateur()
    commande = [
        sys.executable, "-m", "PyInstaller",
        "--noconfirm",
        "--onefile",
        "--console" if console else "--windowed",
        "--name", "Santinel",
        "--paths", str(APP),                # main.py fait « import donnees »
        "--add-data", f"{APP / 'interface'}{s}interface",
        "--add-data", f"{APP / 'schema.sql'}{s}.",
        str(APP / "main.py"),
    ]

    icone = RACINE / "ressources" / "santinel.ico"
    if icone.exists():
        commande[-1:-1] = ["--icon", str(icone)]

    print(" ".join(commande), "\n")
    resultat = subprocess.run(commande)
    if resultat.returncode == 0:
        print(f"\nTerminé : {RACINE / 'dist' / 'Santinel.exe'}")
    return resultat.returncode


if __name__ == "__main__":
    sys.exit(construire(console="--console" in sys.argv))
