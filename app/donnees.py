"""Accès aux données du parc informatique Santinel."""

import os
import sqlite3
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
    """Crée les tables si la base est neuve."""
    schema = (Path(__file__).parent / "schema.sql").read_text(encoding="utf-8")
    with connexion() as cx:
        cx.executescript(schema)


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
        "nom_ordinateur": ligne["nom_ordinateur"],
        "modele": ligne["modele"],
        "numero_serie": ligne["numero_serie"],
        "mise_en_service": ligne["mise_en_service"],
        "version_filemaker": ligne["version_filemaker"] or "Non installé",
        "filemaker_ok": a_jour(ligne["version_filemaker"]),
        "equipements": [dict(e) for e in equipements],
    }
