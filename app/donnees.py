"""Accès aux données du parc informatique Santinel."""

import os
import sqlite3
from datetime import date
from pathlib import Path

# Version de FileMaker considérée à jour. À ajuster lors des montées de version.
FILEMAKER_CIBLE = "22"


def chemin_base() -> Path:
    """Emplacement du fichier de données.

    SANTINEL_DB permet de pointer vers un partage réseau sans recompiler,
    par exemple  SANTINEL_DB=\\\\serveur\\ti\\parc.db
    """
    if os.environ.get("SANTINEL_DB"):
        return Path(os.environ["SANTINEL_DB"])
    base = Path(os.environ.get("LOCALAPPDATA", Path.home())) / "Santinel"
    base.mkdir(parents=True, exist_ok=True)
    return base / "parc.db"


def connexion() -> sqlite3.Connection:
    cx = sqlite3.connect(chemin_base())
    cx.row_factory = sqlite3.Row
    cx.execute("PRAGMA foreign_keys = ON")
    return cx


def initialiser() -> None:
    """Crée les tables si la base est neuve, et met à niveau les anciennes."""
    schema = (Path(__file__).parent / "schema.sql").read_text(encoding="utf-8")
    with connexion() as cx:
        _migrer(cx)
        cx.executescript(schema)


def _migrer(cx: sqlite3.Connection) -> None:
    """Ajoute les colonnes apparues après coup aux bases existantes.

    Tourne avant le schéma : la vue v_fiche s'appuie sur ces colonnes, et
    CREATE TABLE IF NOT EXISTS ne touche pas une table déjà là.
    """
    tables = {ligne[0] for ligne in
              cx.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}
    if "employe" not in tables:
        return                      # base neuve : le schéma s'en charge
    colonnes = {ligne["name"] for ligne in cx.execute("PRAGMA table_info(employe)")}
    if "actif" not in colonnes:
        cx.execute("ALTER TABLE employe ADD COLUMN actif INTEGER NOT NULL DEFAULT 1")


def a_jour(version: str | None) -> bool:
    return bool(version) and version.strip().startswith(FILEMAKER_CIBLE)


def chercher(terme: str) -> list[dict]:
    """Recherche sur le nom, le poste, le numéro de série ou le service."""
    terme = (terme or "").strip()
    if not terme:
        return []
    motif = f"%{terme}%"
    with connexion() as cx:
        lignes = cx.execute(
            """
            SELECT * FROM v_fiche
            WHERE nom LIKE ? OR nom_ordinateur LIKE ?
               OR numero_serie LIKE ? OR service LIKE ?
            ORDER BY nom
            """,
            (motif, motif, motif, motif),
        ).fetchall()
        return [_fiche(cx, l) for l in lignes]


def tous() -> list[dict]:
    with connexion() as cx:
        lignes = cx.execute("SELECT * FROM v_fiche ORDER BY nom").fetchall()
        return [_fiche(cx, l) for l in lignes]


def retardataires() -> list[dict]:
    """Postes dont FileMaker n'est pas à la version cible."""
    return [f for f in tous() if not f["filemaker_ok"]]


def definir_actif(employe_id: int, actif: bool) -> bool:
    """Marque un employé actif ou inactif. Renvoie l'état enregistré."""
    with connexion() as cx:
        curseur = cx.execute(
            "UPDATE employe SET actif = ? WHERE id = ?",
            (1 if actif else 0, employe_id),
        )
        if curseur.rowcount == 0:
            raise ValueError(f"Aucun employé avec l'identifiant {employe_id}.")
    return bool(actif)


def definir_coordonnees(employe_id: int, telephone: str, poste_interne: str) -> dict:
    """Met à jour le téléphone et le poste interne. Renvoie les valeurs gardées.

    Un champ laissé vide est enregistré comme absent (NULL) plutôt que comme
    une chaîne vide, pour que la fiche affiche « — » comme ailleurs.
    """
    valeurs = {
        "telephone": (telephone or "").strip() or None,
        "poste_interne": (poste_interne or "").strip() or None,
    }
    with connexion() as cx:
        curseur = cx.execute(
            "UPDATE employe SET telephone = ?, poste_interne = ? WHERE id = ?",
            (valeurs["telephone"], valeurs["poste_interne"], employe_id),
        )
        if curseur.rowcount == 0:
            raise ValueError(f"Aucun employé avec l'identifiant {employe_id}.")
    return valeurs


CHAMPS_POSTE = ("nom_ordinateur", "modele", "numero_serie",
                "mise_en_service", "version_filemaker")


def definir_poste(employe_id: int, poste: dict) -> dict:
    """Enregistre le poste de travail d'un employé, en le créant au besoin."""
    valeurs = {c: ((poste or {}).get(c) or "").strip() or None for c in CHAMPS_POSTE}

    if valeurs["mise_en_service"]:
        try:
            date.fromisoformat(valeurs["mise_en_service"])
        except ValueError:
            raise ValueError(
                "La mise en service doit être une date au format AAAA-MM-JJ."
            ) from None

    with connexion() as cx:
        _exiger_employe(cx, employe_id)
        cx.execute(
            """
            INSERT INTO ordinateur
                (employe_id, nom_ordinateur, modele, numero_serie,
                 mise_en_service, version_filemaker)
            VALUES (?, ?, ?, ?, ?, ?)
            ON CONFLICT(employe_id) DO UPDATE SET
                nom_ordinateur    = excluded.nom_ordinateur,
                modele            = excluded.modele,
                numero_serie      = excluded.numero_serie,
                mise_en_service   = excluded.mise_en_service,
                version_filemaker = excluded.version_filemaker
            """,
            (employe_id, *(valeurs[c] for c in CHAMPS_POSTE)),
        )
    return valeurs


def definir_equipements(employe_id: int, equipements: list) -> list:
    """Remplace la liste des équipements d'un employé.

    Les lignes entièrement vides sont ignorées ; le type est obligatoire dès
    qu'une ligne porte quelque chose.
    """
    propres = []
    for equipement in equipements or []:
        type_ = (equipement.get("type") or "").strip()
        description = (equipement.get("description") or "").strip() or None
        serie = (equipement.get("numero_serie") or "").strip() or None
        if not type_ and description is None and serie is None:
            continue
        if not type_:
            raise ValueError("Chaque équipement doit avoir un type.")
        propres.append({"type": type_, "description": description,
                        "numero_serie": serie})
    propres.sort(key=lambda e: e["type"])

    with connexion() as cx:
        _exiger_employe(cx, employe_id)
        cx.execute("DELETE FROM equipement WHERE employe_id = ?", (employe_id,))
        cx.executemany(
            "INSERT INTO equipement (employe_id, type, description, numero_serie) "
            "VALUES (?, ?, ?, ?)",
            [(employe_id, e["type"], e["description"], e["numero_serie"])
             for e in propres],
        )
    return propres


def _exiger_employe(cx: sqlite3.Connection, employe_id: int) -> None:
    if cx.execute("SELECT 1 FROM employe WHERE id = ?", (employe_id,)).fetchone() is None:
        raise ValueError(f"Aucun employé avec l'identifiant {employe_id}.")


def _fiche(cx: sqlite3.Connection, ligne: sqlite3.Row) -> dict:
    equipements = cx.execute(
        "SELECT type, description, numero_serie FROM equipement "
        "WHERE employe_id = ? ORDER BY type",
        (ligne["id"],),
    ).fetchall()
    return {
        "id": ligne["id"],
        "nom": ligne["nom"],
        "service": ligne["service"],
        "telephone": ligne["telephone"],
        "poste_interne": ligne["poste_interne"],
        "actif": bool(ligne["actif"]),
        "nom_ordinateur": ligne["nom_ordinateur"],
        "modele": ligne["modele"],
        "numero_serie": ligne["numero_serie"],
        "mise_en_service": ligne["mise_en_service"],
        "version_filemaker": ligne["version_filemaker"] or "Non installé",
        "filemaker_ok": a_jour(ligne["version_filemaker"]),
        "equipements": [dict(e) for e in equipements],
    }
