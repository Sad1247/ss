-- Santinel — structure de la base du parc informatique.
-- Ce script est rejoué à chaque démarrage : tout doit être idempotent.

CREATE TABLE IF NOT EXISTS employe (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    nom           TEXT NOT NULL,
    service       TEXT,
    telephone     TEXT,
    poste_interne TEXT,
    actif         INTEGER NOT NULL DEFAULT 1,  -- 1 = en poste, 0 = inactif
    -- Présence dans l'onglet « À mettre à jour », décidée à la main :
    -- NULL = laisser la version FileMaker décider, 1 = toujours, 0 = jamais.
    suivi_manuel  INTEGER
);

-- Un employé a au plus un poste de travail principal.
CREATE TABLE IF NOT EXISTS ordinateur (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    employe_id       INTEGER NOT NULL UNIQUE
                     REFERENCES employe(id) ON DELETE CASCADE,
    nom_ordinateur   TEXT,
    modele           TEXT,
    numero_serie     TEXT,
    mise_en_service  TEXT,   -- AAAA-MM-JJ
    version_filemaker TEXT   -- NULL = FileMaker non installé
);

-- Écrans, imprimantes, docks, téléphones IP…
CREATE TABLE IF NOT EXISTS equipement (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    employe_id   INTEGER NOT NULL
                 REFERENCES employe(id) ON DELETE CASCADE,
    type         TEXT NOT NULL,
    description  TEXT,
    numero_serie TEXT
);

CREATE INDEX IF NOT EXISTS idx_equipement_employe ON equipement(employe_id);
CREATE INDEX IF NOT EXISTS idx_employe_nom        ON employe(nom);

-- Vue à plat consommée par donnees.py : une ligne = une fiche employé.
-- Reconstruite à chaque démarrage : une vue ne contient pas de données, et
-- « CREATE VIEW IF NOT EXISTS » laisserait une ancienne définition en place.
DROP VIEW IF EXISTS v_fiche;
CREATE VIEW v_fiche AS
SELECT
    e.id,
    e.nom,
    e.service,
    e.telephone,
    e.poste_interne,
    e.actif,
    e.suivi_manuel,
    o.nom_ordinateur,
    o.modele,
    o.numero_serie,
    o.mise_en_service,
    o.version_filemaker
FROM employe e
LEFT JOIN ordinateur o ON o.employe_id = e.id;
