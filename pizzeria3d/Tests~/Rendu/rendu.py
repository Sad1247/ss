"""Dessine la scene dumpee par le banc d'essai, dans la vue isometrique du jeu.

Ce n'est pas Unity : pas d'ombres, pas de lissage, une couleur par face.
Cela suffit a repondre a "qu'est-ce que le joueur voit la ?".
"""
import math, sys
from PIL import Image, ImageDraw

CAM = (38.0, -45.0)          # les angles de la camera du jeu


def axes():
    rx, ry = math.radians(CAM[0]), math.radians(CAM[1])
    cx, sx = math.cos(rx), math.sin(rx)
    cy, sy = math.cos(ry), math.sin(ry)
    # R = Ry * Rx applique aux axes du repere camera
    right = (cy, 0.0, -sy)
    up = (sy * sx, cx, cy * sx)
    fwd = (sy * cx, -sx, cy * cx)
    return right, up, fwd


RIGHT, UP, FWD = axes()
dot = lambda a, b: a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def tourner(q, v):
    """Applique un quaternion (x, y, z, w) a un vecteur."""
    qx, qy, qz, qw = q
    ix = qw * v[0] + qy * v[2] - qz * v[1]
    iy = qw * v[1] + qz * v[0] - qx * v[2]
    iz = qw * v[2] + qx * v[1] - qy * v[0]
    iw = -qx * v[0] - qy * v[1] - qz * v[2]
    return (ix * qw + iw * -qx + iy * -qz - iz * -qy,
            iy * qw + iw * -qy + iz * -qx - ix * -qz,
            iz * qw + iw * -qz + ix * -qy - iy * -qx)


def boite(p, s, q):
    coins = []
    for dx in (-.5, .5):
        for dy in (-.5, .5):
            for dz in (-.5, .5):
                r = tourner(q, (dx * s[0], dy * s[1], dz * s[2]))
                coins.append((p[0] + r[0], p[1] + r[1], p[2] + r[2]))
    # 6 faces, par indices dans la liste ci-dessus (ordre dx,dy,dz)
    faces = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1),
             (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    return coins, faces


def teinte(rgb, k):
    r, g, b = (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255
    return tuple(max(0, min(255, int(v * k))) for v in (r, g, b))


def main(source, sortie, largeur=1100, hauteur=900, zoom=None,
         centre=None, cacher=()):
    objets = []
    for ligne in open(source):
        ligne = ligne.strip()
        if not ligne:
            continue
        nom, forme, pos, taille, rot, coul = ligne.split("|")
        if any(h in nom for h in cacher):
            continue
        p = tuple(float(v) for v in pos.split())
        s = tuple(float(v) for v in taille.split())
        alpha = int(coul[6:8], 16) / 255 if len(coul) == 8 else 1.0
        q = tuple(float(v) for v in rot.split())
        objets.append((nom, forme, p, s, q, int(coul[:6], 16), alpha))

    # cadrage
    pts = []
    for _, _, p, s, q, _, _a in objets:
        coins, _f = boite(p, s, q)
        pts += [(dot(c, RIGHT), dot(c, UP)) for c in coins]
    if centre:
        cx, cy = centre
    else:
        cx = (min(x for x, _ in pts) + max(x for x, _ in pts)) / 2
        cy = (min(y for _, y in pts) + max(y for _, y in pts)) / 2
    if zoom is None:
        ex = max(x for x, _ in pts) - min(x for x, _ in pts)
        ey = max(y for _, y in pts) - min(y for _, y in pts)
        zoom = min(largeur / (ex + 2), hauteur / (ey + 2))

    img = Image.new("RGB", (largeur, hauteur), (150, 205, 120))
    d = ImageDraw.Draw(img)
    ecran = lambda c: (largeur / 2 + (dot(c, RIGHT) - cx) * zoom,
                       hauteur / 2 - (dot(c, UP) - cy) * zoom)

    # peintre : du plus loin au plus proche
    dessins = []
    for nom, forme, p, s, q, coul, alpha in objets:
        coins, faces = boite(p, s, q)
        prof = max(dot(c, FWD) for c in coins)
        dessins.append((prof, nom, forme, coins, faces, coul, alpha))
    dessins.sort(key=lambda o: -o[0])

    for _prof, nom, forme, coins, faces, coul, alpha in dessins:
        # une vitre se peint par-dessus, en laissant voir le fond
        if alpha < 0.99:
            calque = Image.new("RGBA", img.size, (0, 0, 0, 0))
            dc = ImageDraw.Draw(calque)
            for i, f in enumerate(faces):
                pts3 = [coins[k] for k in f]
                if dot(normale(pts3), FWD) >= 0:
                    continue
                r, g, b = teinte(coul, (.72, 1.0, .86, .86, .93, .93)[i])
                dc.polygon([ecran(c) for c in pts3], fill=(r, g, b, int(alpha * 255)))
            img = Image.alpha_composite(img.convert("RGBA"), calque).convert("RGB")
            d = ImageDraw.Draw(img)
            ecran = lambda c: (largeur / 2 + (dot(c, RIGHT) - cx) * zoom,
                               hauteur / 2 - (dot(c, UP) - cy) * zoom)
            continue

        if forme in ("Sphere", "Capsule", "Cylinder"):
            xs = [ecran(c)[0] for c in coins]
            ys = [ecran(c)[1] for c in coins]
            d.ellipse([min(xs), min(ys), max(xs), max(ys)],
                      fill=teinte(coul, 1.0), outline=teinte(coul, .75))
            continue
        for i, f in enumerate(faces):
            pts3 = [coins[k] for k in f]
            n = normale(pts3)
            if dot(n, FWD) >= 0:          # face tournee vers le fond
                continue
            k = (.72, 1.0, .86, .86, .93, .93)[i]
            d.polygon([ecran(c) for c in pts3], fill=teinte(coul, k))
    img.save(sortie)
    print("image ecrite dans", sortie)


def normale(q):
    a, b, c = q[0], q[1], q[2]
    u = (b[0] - a[0], b[1] - a[1], b[2] - a[2])
    v = (c[0] - a[0], c[1] - a[1], c[2] - a[2])
    return (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0])


if __name__ == "__main__":
    args = dict(a.split("=", 1) for a in sys.argv[3:] if "=" in a)
    main(sys.argv[1], sys.argv[2],
         zoom=float(args["zoom"]) if "zoom" in args else None,
         centre=tuple(float(v) for v in args["centre"].split(",")) if "centre" in args else None,
         cacher=tuple(args["cacher"].split(",")) if "cacher" in args else ())
