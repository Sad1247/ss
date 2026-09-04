"""Formulaire « Équipement prêté » rempli à partir d'une fiche, en PDF.

Reprend la mise en page du document remis par le service : entête au logo,
deux colonnes d'ordinateur, cases d'accessoires, cellulaire, signature.
"""

import sys
import unicodedata
from datetime import date
from pathlib import Path

from fpdf import FPDF

BLEU = (11, 47, 92)
GRIS = (110, 122, 138)
TRAIT = (150, 160, 175)

# Cases à cocher du formulaire, et ce qui les déclenche dans les équipements
# de la fiche. La comparaison se fait sans casse ni accents.
ACCESSOIRES = [
    ("Écran 1", ("ecran", "moniteur")),
    ("Écran 2", ("ecran", "moniteur")),
    ("Souris avec fil", ("souris",)),
    ("Clavier avec fil", ("clavier",)),
    ("Souris et clavier sans fil USB", ("sans fil",)),
    ("Casque d'écoute", ("casque",)),
    ("Adaptateur de charge et cordon", ("adaptateur", "chargeur", "bloc d'alimentation")),
    ("Projecteur", ("projecteur",)),
    ("Sac de transport", ("sac", "mallette")),
    ("Webcam", ("webcam", "camera")),
]


# Les polices intégrées au PDF ne connaissent que le Latin-1. Les fiches,
# elles, peuvent contenir n'importe quoi : plutôt que d'échouer, on remplace.
REMPLACEMENTS = {
    "\u2014": "-", "\u2013": "-", "\u2018": "'", "\u2019": "'",
    "\u201c": '"', "\u201d": '"', "\u2026": "...", "\u00a0": " ",
    "\u0153": "oe", "\u0152": "OE", "\u20ac": "EUR", "\u2022": "-",
}


def texte_pdf(valeur) -> str:
    """Rend un texte écrivable par les polices intégrées du PDF.

    Les accents français passent tels quels ; seuls les caractères hors
    Latin-1 sont approchés (œ, tirets longs, guillemets courbes) ou écartés.
    """
    texte = str(valeur if valeur is not None else "")
    for source, cible in REMPLACEMENTS.items():
        texte = texte.replace(source, cible)

    sortie = []
    for caractere in texte:
        try:
            caractere.encode("latin-1")
            sortie.append(caractere)
        except UnicodeEncodeError:
            approche = unicodedata.normalize("NFKD", caractere)
            sortie.append("".join(
                c for c in approche
                if not unicodedata.combining(c) and c.encode("latin-1", "ignore")))
    return "".join(sortie)


def sans_accents(texte: str) -> str:
    decompose = unicodedata.normalize("NFD", (texte or "").lower())
    return "".join(c for c in decompose if not unicodedata.combining(c))


def dossier_documents() -> Path:
    """Où déposer les PDF produits."""
    documents = Path.home() / "Documents"
    base = documents if documents.is_dir() else Path.home()
    dossier = base / "Santinel"
    dossier.mkdir(parents=True, exist_ok=True)
    return dossier


def _repartir(fiche: dict) -> tuple[dict, list[str]]:
    """Coche les accessoires reconnus, renvoie le reste en toutes lettres."""
    coches = {libelle: False for libelle, _ in ACCESSOIRES}
    restants = []
    ecrans = 0

    for equipement in fiche.get("equipements", []):
        texte = sans_accents(f"{equipement.get('type', '')} {equipement.get('description', '')}")
        libelle = None

        if any(mot in texte for mot in ("ecran", "moniteur")):
            ecrans += 1
            libelle = "Écran 1" if ecrans == 1 else "Écran 2" if ecrans == 2 else None
            if libelle is None:                    # au-delà de deux écrans
                restants.append(_ligne(equipement))
                continue
        else:
            for candidat, mots in ACCESSOIRES:
                if candidat.startswith("Écran"):
                    continue
                if any(sans_accents(mot) in texte for mot in mots):
                    libelle = candidat
                    break

        if libelle and not coches[libelle]:
            coches[libelle] = True
        else:
            restants.append(_ligne(equipement))

    return coches, restants


def _ligne(equipement: dict) -> str:
    morceaux = [equipement.get("type"), equipement.get("description"),
                equipement.get("numero_serie")]
    return " - ".join(m for m in morceaux if m)


def _marque_et_modele(modele: str | None) -> tuple[str, str]:
    """« Dell Latitude 5450 » donne la marque Dell et le modèle Latitude 5450."""
    if not modele:
        return "", ""
    mots = modele.split()
    return (mots[0], " ".join(mots[1:])) if len(mots) > 1 else (modele, "")


class Formulaire(FPDF):
    def __init__(self, logo: Path | None):
        super().__init__(format="Letter", unit="mm")
        self.logo = logo
        self.set_auto_page_break(False)
        self.set_margins(18, 15, 18)

    # ----- briques de mise en page -----

    def titre(self, texte: str, taille: int = 11) -> None:
        self.set_font("Helvetica", "B", taille)
        self.set_text_color(*BLEU)
        self.cell(0, 6, texte_pdf(texte))
        self.ln(7)

    def champ(self, x: float, y: float, largeur: float,
              etiquette: str, valeur: str | None) -> None:
        """Étiquette, valeur, et le trait à remplir à la main si elle manque."""
        self.set_xy(x, y)
        self.set_font("Helvetica", "", 9)
        self.set_text_color(*GRIS)
        etiquette = texte_pdf(etiquette)
        self.cell(0, 5, etiquette)
        depart = x + self.get_string_width(etiquette) + 1.5

        self.set_draw_color(*TRAIT)
        self.set_line_width(0.2)
        self.line(depart, y + 4.6, x + largeur, y + 4.6)

        if valeur:
            place = x + largeur - depart
            valeur = texte_pdf(valeur)
            self.set_text_color(20, 30, 45)
            # une valeur longue — un processeur, par exemple — est réduite
            # plutôt que de déborder du trait
            taille = 9.5
            self.set_font("Helvetica", "B", taille)
            while self.get_string_width(valeur) > place and taille > 6.5:
                taille -= 0.5
                self.set_font("Helvetica", "B", taille)
            self.set_xy(depart, y)
            self.cell(place, 4.4, valeur)

    def case(self, x: float, y: float, libelle: str, cochee: bool) -> None:
        cote = 3.6
        self.set_draw_color(*TRAIT)
        self.set_line_width(0.25)
        self.rect(x, y, cote, cote)
        if cochee:
            self.set_draw_color(*BLEU)
            self.set_line_width(0.6)
            self.line(x + 0.8, y + 1.9, x + 1.5, y + 2.8)
            self.line(x + 1.5, y + 2.8, x + 2.9, y + 0.8)
        self.set_xy(x + cote + 2.5, y - 0.8)
        self.set_font("Helvetica", "B" if cochee else "", 9)
        self.set_text_color(*((20, 30, 45) if cochee else GRIS))
        self.cell(0, 5, texte_pdf(libelle))


def generer(fiche: dict, logo: Path | None = None) -> Path:
    """Écrit le formulaire rempli et renvoie le chemin du PDF."""
    pdf = Formulaire(logo)
    pdf.add_page()

    if logo and Path(logo).exists():
        pdf.image(str(logo), x=18, y=13, w=42)

    pdf.set_xy(18, 30)
    pdf.set_font("Helvetica", "B", 17)
    pdf.set_text_color(*BLEU)
    pdf.cell(0, 9, texte_pdf("ÉQUIPEMENT PRÊTÉ"), align="C")

    pdf.set_xy(18, 42)
    pdf.set_font("Helvetica", "", 9.5)
    pdf.set_text_color(*GRIS)
    pdf.cell(0, 5, texte_pdf(
        "Le matériel nécessaire pour l'exécution de son travail a été remis à :"))

    pdf.champ(18, 50, 180, "Employé :", fiche.get("nom"))

    # ----- les deux colonnes d'ordinateur -----
    portable = sans_accents(fiche.get("type_appareil") or "").startswith("portable")
    marque, modele = _marque_et_modele(fiche.get("modele"))
    poste = {
        "Marque :": marque,
        "Modèle :": modele,
        "# de série :": fiche.get("numero_serie"),
        "Specs :": fiche.get("cpu"),
        "Nom :": fiche.get("nom_ordinateur"),
    }

    gauche, droite, largeur = 18, 110, 88
    pdf.set_xy(gauche, 62)
    pdf.titre("ORDINATEUR PC" + ("" if portable else "   (X)"), 10)
    pdf.set_xy(droite, 62)
    pdf.titre("ORDINATEUR PORTABLE" + ("   (X)" if portable else ""), 10)

    y = 71
    for etiquette in poste:
        rempli = poste[etiquette]
        pdf.champ(gauche, y, largeur, etiquette, None if portable else rempli)
        pdf.champ(droite, y, largeur, etiquette, rempli if portable else None)
        y += 9

    # ----- accessoires et cellulaire -----
    coches, restants = _repartir(fiche)

    pdf.set_xy(gauche, y + 4)
    pdf.titre("ACCESSOIRES D'ORDINATEUR", 10)
    haut = y + 12
    for rang, (libelle, _) in enumerate(ACCESSOIRES):
        pdf.case(gauche, haut + rang * 7.5, libelle, coches[libelle])

    pdf.set_xy(droite, y + 4)
    pdf.titre("CELLULAIRE PROFESSIONNEL", 10)
    for rang, etiquette in enumerate(("Modèle :", "# de tél. :", "IMEI :")):
        pdf.champ(droite, haut + rang * 9, largeur, etiquette, None)

    # ----- autres équipements -----
    bas = haut + len(ACCESSOIRES) * 7.5 + 6
    pdf.set_xy(gauche, bas)
    pdf.titre("AUTRES ÉQUIPEMENTS", 10)
    for rang in range(3):
        pdf.champ(gauche, bas + 9 + rang * 9, 180,
                  "", restants[rang] if rang < len(restants) else None)

    # ----- signature -----
    signature = bas + 9 + 3 * 9 + 10
    pdf.champ(gauche, signature, 100, "Signature de l'employé :", None)
    pdf.champ(130, signature, 68, "Date :", date.today().isoformat())

    chemin = dossier_documents() / _nom_fichier(fiche)
    pdf.output(str(chemin))
    return chemin


def _nom_fichier(fiche: dict) -> str:
    propre = "".join(c if c.isalnum() or c in " -_" else "_"
                     for c in sans_accents(fiche.get("nom") or "employe"))
    return f"Equipement_prete_{propre.strip().replace(' ', '_')}_{date.today().isoformat()}.pdf"


def ouvrir(chemin: Path) -> bool:
    """Ouvre le PDF avec le lecteur du système. Sans échec bloquant."""
    try:
        if sys.platform == "win32":
            import os
            os.startfile(chemin)                              # noqa: S606
        else:
            import subprocess
            subprocess.Popen(["xdg-open", str(chemin)],
                             stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        return True
    except Exception:
        return False
