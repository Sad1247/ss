# PDFacile

Site d'outils PDF (fusionner, diviser, compresser, convertir, filigrane…) inspiré de la
présentation des grands services en ligne. **Tout le traitement se fait dans le navigateur**
avec [pdf-lib](https://pdf-lib.js.org/), [PDF.js](https://mozilla.github.io/pdf.js/) et
[JSZip](https://stuk.github.io/jszip/) : aucun fichier n'est envoyé à un serveur.

## Lancer en local

```bash
cd pdf-site
python3 -m http.server 8000
# puis ouvrir http://localhost:8000 (version une seule page)
```

## Site publié (une page par outil)

`build.py` génère dans `dist/` le site destiné à être mis en ligne : une vraie page par
outil (`/fusionner-pdf/`, `/signer-pdf/`…) avec titre, description, mode d'emploi et
questions fréquentes pour les moteurs de recherche (textes dans `seo.json`), plus
`sitemap.xml`, `robots.txt`, une page confidentialité et une page 404.

```bash
python3 build.py                               # adresse par défaut : https://sad1247.github.io/ss
SITE_URL=https://mon-domaine.fr python3 build.py
python3 -m http.server 8000 -d dist
```

Le workflow `.github/workflows/pages.yml` le publie automatiquement sur GitHub Pages à
chaque modification de la branche par défaut (à activer une fois dans
*Settings → Pages → Source : GitHub Actions*). Pour valider le site dans Google Search
Console, créez la variable `GOOGLE_SITE_VERIFICATION` (*Settings → Secrets and variables →
Actions → Variables*) avec le code fourni par Google.

## Outils disponibles

| Catégorie | Outils |
| --- | --- |
| Organiser | Fusionner, Diviser, Pivoter, Supprimer des pages, Extraire des pages |
| Optimiser | Compresser (pages converties en JPEG optimisé) |
| Convertir | PDF → JPG, JPG/PNG → PDF, PDF → texte |
| Modifier | Filigrane, Numéros de page, Propriétés (titre, auteur, sujet) |
| Sécurité | Signer (signature dessinée, tapée ou importée, placée par glisser-déposer) |

Marqués « Bientôt » (nécessitent un serveur ou une bibliothèque de chiffrement) :
PDF ↔ Word / PowerPoint / Excel, Protéger et Déverrouiller un PDF.

La signature ajoutée par « Signer PDF » est une signature visuelle (image apposée sur la page),
pas une signature électronique certifiée avec certificat numérique.

## Structure

- `index.html` : squelette de la page (accueil + page outil)
- `style.css` : styles, thèmes clair/sombre, responsive
- `tools.js` : liste des outils, catégories, icônes et traitements PDF
- `app.js` : interface (filtres, sélection de fichiers, glisser-déposer, routage)
- `seo.json` : textes des pages publiées (titres, descriptions, modes d'emploi, FAQ)
- `build.py` : génération du site publié dans `dist/`

Pour ajouter un outil : une entrée dans `TOOLS` (`tools.js`) avec ses `options()` (HTML du
formulaire) et sa fonction `run(files, options, progress)`, son adresse dans `SLUGS` et ses
textes dans `seo.json` (`build.py` refuse de générer le site s'ils manquent).
