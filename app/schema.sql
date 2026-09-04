-- Santinel — structure de la base du parc informatique.
-- Ce script est rejoué à chaque démarrage : tout doit être idempotent.

CREATE TABLE IF NOT EXISTS employe (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    nom            TEXT NOT NULL,
    service        TEXT,
    telephone      TEXT,
    poste_interne  TEXT,
    nom_utilisateur TEXT,
    courriel       TEXT,
    titre          TEXT,   -- « Directeur, Ventes »
    compagnie      TEXT,
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
    version_filemaker TEXT,  -- NULL = FileMaker non installé
    cpu              TEXT,
    type_appareil    TEXT,   -- Portable, Mini-PC…
    windows11        INTEGER -- 1 = migré, 0 = non, NULL = inconnu
);

-- Comptes d'accès à l'application. Les mots de passe ne sont jamais
-- stockés en clair : seule leur empreinte PBKDF2, avec un sel par compte.
CREATE TABLE IF NOT EXISTS compte (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    identifiant TEXT NOT NULL UNIQUE,   -- sans casse ni accents, sert aux recherches
    nom         TEXT NOT NULL,          -- tel qu'il s'affiche
    sel         TEXT NOT NULL,
    empreinte   TEXT NOT NULL,
    role        TEXT NOT NULL
                CHECK (role IN ('administrateur', 'modification', 'lecture'))
);

-- Cellulaire professionnel. Un employé en a au plus un.
-- Les mots de passe y sont en clair : c'est un carnet de notes du service,
-- pas un coffre-fort. Voir la mise en garde du README.
CREATE TABLE IF NOT EXISTS cellulaire (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    employe_id        INTEGER NOT NULL UNIQUE
                      REFERENCES employe(id) ON DELETE CASCADE,
    numero            TEXT,
    modele            TEXT,
    imei              TEXT,
    compte_icloud     TEXT,
    motdepasse_icloud TEXT,
    motdepasse_cell   TEXT
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
    e.nom_utilisateur,
    e.courriel,
    e.titre,
    e.compagnie,
    e.actif,
    e.suivi_manuel,
    o.nom_ordinateur,
    o.modele,
    o.numero_serie,
    o.mise_en_service,
    o.version_filemaker,
    o.cpu,
    o.type_appareil,
    o.windows11,
    c.numero            AS cell_numero,
    c.modele            AS cell_modele,
    c.imei              AS cell_imei,
    c.compte_icloud     AS cell_compte_icloud,
    c.motdepasse_icloud AS cell_motdepasse_icloud,
    c.motdepasse_cell   AS cell_motdepasse
FROM employe e
LEFT JOIN ordinateur o ON o.employe_id = e.id
LEFT JOIN cellulaire c ON c.employe_id = e.id;
