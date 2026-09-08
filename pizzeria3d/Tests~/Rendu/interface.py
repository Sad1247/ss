"""Dessine la mise en page d'un canevas dumpee par le banc d'essai.

Ce n'est pas Unity : pas de police du jeu, pas de sprite. Cela suffit a
repondre a « ou tombe cet element, et de quelle couleur ? ».
"""
import sys
from PIL import Image, ImageDraw, ImageFont

ANCRES = {0: "ul", 1: "uc", 2: "ur", 3: "ml", 4: "mc", 5: "mr",
          6: "ll", 7: "lc", 8: "lr"}


def police(taille):
    for chemin in ("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
                   "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(chemin, taille)
        except OSError:
            pass
    return ImageFont.load_default()


def main(source, sortie, echelle=0.5):
    lignes = [l.rstrip("\n") for l in open(source) if l.strip()]
    W, H = (int(v) for v in lignes[0].split())
    img = Image.new("RGB", (int(W * echelle), int(H * echelle)), (40, 40, 45))
    d = ImageDraw.Draw(img)

    ecran = lambda x, y: ((W / 2 + x) * echelle, (H / 2 - y) * echelle)

    for ligne in lignes[1:]:
        p = ligne.split("|")
        genre, x, y, w, h, coul = p[0], float(p[1]), float(p[2]), float(p[3]), float(p[4]), p[5]
        rgb = tuple(int(coul[i:i + 2], 16) for i in (0, 2, 4))
        if genre == "boite":
            x0, y0 = ecran(x - w / 2, y + h / 2)
            x1, y1 = ecran(x + w / 2, y - h / 2)
            d.rectangle([x0, y0, x1, y1], fill=rgb)
        else:
            taille = max(8, int(float(p[6]) * echelle))
            ancre = ANCRES.get(int(p[7]), "mc")
            texte = "|".join(p[8:])
            gx = x - w / 2 if ancre[1] == "l" else (x + w / 2 if ancre[1] == "r" else x)
            gy = y + h / 2 if ancre[0] == "u" else (y - h / 2 if ancre[0] == "l" else y)
            px, py = ecran(gx, gy)
            pil = {"l": "left", "c": "center", "r": "right"}[ancre[1]]
            pily = {"u": "top", "m": "middle", "l": "bottom"}[ancre[0]]
            d.text((px, py), texte, fill=rgb, font=police(taille),
                   anchor={"left": "l", "center": "m", "right": "r"}[pil]
                          + {"top": "a", "middle": "m", "bottom": "d"}[pily])

    img.save(sortie)
    print("image ecrite dans", sortie)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2],
         float(sys.argv[3]) if len(sys.argv) > 3 else 0.5)
