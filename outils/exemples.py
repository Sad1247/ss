"""Remplit la base avec quelques fiches de démonstration.

    python outils/exemples.py          # base par défaut
    SANTINEL_DB=/tmp/essai.db python outils/exemples.py

Le script est re-jouable : il ajoute les fiches absentes et met à jour les
champs de démonstration de celles qui existent déjà, sans rien supprimer.
"""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "app"))

import donnees  # noqa: E402

# Fiches renommées après coup : appliqué aux bases déjà remplies.
RENOMMAGES = {"Saad Anjar": "Saad"}

EMPLOYES = [
    {
        "nom": "Marie Tremblay", "service": "Comptabilité", "actif": 1,
        "telephone": "418 555-0142", "poste_interne": "221",
        "nom_utilisateur": "mtremblay", "courriel": "mtremblay@santinel.ca",
        "poste": ("COMPTA-01", "Dell OptiPlex 7010", "5KJ9L2Q", "2023-04-11", "22.0.1"),
        "equipements": [("Écran", "Dell P2422H 24 pouces", "CN0X1Y2Z"),
                        ("Imprimante", "Brother HL-L2350DW", "U64312J0N")],
    },
    {
        "nom": "Jean Bourgeois", "service": "Direction", "actif": 1,
        "telephone": "418 555-0118", "poste_interne": "201",
        "nom_utilisateur": "jbourgeois", "courriel": "jbourgeois@santinel.ca",
        "poste": ("DIR-PORT-03", "Lenovo ThinkPad T14", "PF3H8821", "2024-09-02", "21.1.3"),
        "equipements": [("Station d'accueil", "Lenovo ThinkPad Dock Gen 2", "1S40AS0090")],
    },
    {
        "nom": "Sophie Lavoie", "service": "Ressources humaines", "actif": 0,
        "telephone": "418 555-0177", "poste_interne": "245",
        "nom_utilisateur": "slavoie", "courriel": "slavoie@santinel.ca",
        "poste": ("RH-02", "HP EliteDesk 800", "CZC2410XYZ", "2022-01-20", None),
        "equipements": [("Écran", "HP E243 24 pouces", "6CM8241QRS"),
                        ("Téléphone IP", "Yealink T46S", "80AB12CD34")],
    },
    {
        "nom": "Alain Gagné", "service": "Entrepôt", "actif": 1,
        "telephone": "418 555-0190", "poste_interne": "310",
        "nom_utilisateur": "againe", "courriel": "againe@santinel.ca",
        "poste": ("ENTREPOT-01", "Dell OptiPlex 3000", "9WQ4T1B", "2025-02-17", "22.0.1"),
        "equipements": [("Lecteur code-barres", "Zebra DS2208", "18240522500123")],
    },
    {
        "nom": "Saad", "service": "Informatique", "actif": 1,
        "telephone": "418 555-0101", "poste_interne": "200",
        "nom_utilisateur": "saad", "courriel": "saad@santinel.ca",
        "cellulaire": {"numero": "418 555-0102", "modele": "iPhone 15",
                       "imei": "356789102345678",
                       "compte_icloud": "ti.santinel@icloud.com",
                       "motdepasse_icloud": "Parc-2026!", "motdepasse_cell": "204815"},
        "poste": ("TI-PORT-01", "Dell Latitude 5450", "H7X2M9P", "2025-06-09", "22.0.1"),
        "equipements": [("Écran", "Dell U2723QE 27 pouces", "CN0P4R5T"),
                        ("Écran", "Dell U2723QE 27 pouces", "CN0P4R5U"),
                        ("Station d'accueil", "Dell WD19S", "5J8K2L1M")],
    },
    {
        "nom": "Karine Doucet", "service": "Marketing", "actif": 1,
        "telephone": "418 555-0163", "poste_interne": "228",
        "nom_utilisateur": "kdoucet", "courriel": "kdoucet@santinel.ca",
        "poste": ("MARKET-04", "Lenovo ThinkCentre M70q", "MJ0AB1CD", "2023-11-06", "20.3.1"),
        "equipements": [("Écran", "Lenovo ThinkVision T24i", "V906C2XY"),
                        ("Casque", "Jabra Evolve2 40", "JB4471002")],
    },
    {
        "nom": "Luc Marchand", "service": "Expédition", "actif": 0,
        "telephone": "418 555-0184", "poste_interne": "305",
        "nom_utilisateur": "lmarchand", "courriel": "lmarchand@santinel.ca",
        "poste": ("EXPED-02", "HP ProDesk 400 G9", "8CG3120FGH", "2024-03-25", None),
        "equipements": [("Imprimante étiquettes", "Zebra ZD421", "D4J213900456"),
                        ("Téléphone IP", "Yealink T43U", "80CD34EF56")],
    },
]

CHAMPS = ("service", "telephone", "poste_interne",
          "nom_utilisateur", "courriel", "actif")


def main() -> int:
    donnees.initialiser()
    ajoutes = 0
    with donnees.connexion() as cx:
        for ancien, nouveau in RENOMMAGES.items():
            cx.execute("UPDATE employe SET nom = ? WHERE nom = ?", (nouveau, ancien))

        deja = {ligne["nom"]: ligne["id"] for ligne in
                cx.execute("SELECT id, nom FROM employe")}

        for e in EMPLOYES:
            valeurs = tuple(e[c] for c in CHAMPS)
            if e["nom"] in deja:
                # met à jour les champs de démonstration, y compris les
                # colonnes apparues après la création de la fiche
                cx.execute(
                    f"UPDATE employe SET {', '.join(c + ' = ?' for c in CHAMPS)} "
                    "WHERE id = ?",
                    (*valeurs, deja[e["nom"]]),
                )
                continue

            curseur = cx.execute(
                f"INSERT INTO employe (nom, {', '.join(CHAMPS)}) "
                f"VALUES ({', '.join('?' * (len(CHAMPS) + 1))})",
                (e["nom"], *valeurs),
            )
            employe_id = curseur.lastrowid
            cx.execute(
                "INSERT INTO ordinateur (employe_id, nom_ordinateur, modele, "
                "numero_serie, mise_en_service, version_filemaker) "
                "VALUES (?, ?, ?, ?, ?, ?)",
                (employe_id, *e["poste"]),
            )
            cx.executemany(
                "INSERT INTO equipement (employe_id, type, description, numero_serie) "
                "VALUES (?, ?, ?, ?)",
                [(employe_id, *equipement) for equipement in e["equipements"]],
            )
            if e.get("cellulaire"):
                donnees.definir_cellulaire(employe_id, e["cellulaire"])
            ajoutes += 1

    if ajoutes:
        print(f"{ajoutes} fiche(s) ajoutée(s) dans {donnees.chemin_base()}")
    else:
        print(f"Fiches d'exemple mises à jour dans {donnees.chemin_base()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
