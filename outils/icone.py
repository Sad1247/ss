"""Fabrique ressources/santinel.ico à partir du logo.

    python outils/icone.py

Découpe l'emblème circulaire du logo (app/interface/santinel.png, blanc sur
fond transparent), le pose sur le bleu marine de l'application et écrit une
icône multi-tailles. construire.py l'utilise automatiquement si elle existe.
"""

from pathlib import Path

from PIL import Image

RACINE = Path(__file__).resolve().parent.parent
LOGO = RACINE / "app" / "interface" / "santinel.png"
ICONE = RACINE / "ressources" / "santinel.ico"
MARINE = (11, 47, 92, 255)          # --marine de style.css
TAILLES = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]


def embleme(logo: Image.Image) -> Image.Image:
    """Isole la partie gauche du logo, avant le premier grand blanc."""
    alpha = logo.split()[3]
    colonnes = [x for x in range(logo.width)
                if alpha.crop((x, 0, x + 1, logo.height)).getextrema()[1] > 30]
    fin = logo.width
    for precedent, suivant in zip(colonnes, colonnes[1:]):
        if suivant - precedent > 20:    # l'espace entre l'emblème et le mot
            fin = precedent
            break
    part = logo.crop((0, 0, fin + 1, logo.height))
    return part.crop(part.split()[3].getbbox())


def main() -> int:
    logo = Image.open(LOGO).convert("RGBA")
    marque = embleme(logo)

    cote = 256
    marge = int(cote * 0.16)
    fond = Image.new("RGBA", (cote, cote), MARINE)
    marque.thumbnail((cote - 2 * marge, cote - 2 * marge), Image.LANCZOS)
    fond.paste(marque,
               ((cote - marque.width) // 2, (cote - marque.height) // 2),
               marque)

    ICONE.parent.mkdir(exist_ok=True)
    fond.save(ICONE, sizes=TAILLES)
    print(f"Icône écrite : {ICONE}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
