# PDFacile

Site d'outils PDF (fusionner, diviser, compresser, convertir, filigrane…) inspiré de la
présentation des grands services en ligne. **Tout le traitement se fait dans le navigateur**
avec [pdf-lib](https://pdf-lib.js.org/), [PDF.js](https://mozilla.github.io/pdf.js/) et
[JSZip](https://stuk.github.io/jszip/) : aucun fichier n'est envoyé à un serveur.

## Lancer en local

```bash
cd pdf-site
python3 -m http.server 8000
# puis ouvrir http://localhost:8000
```

Site 100 % statique : il peut être déployé tel quel sur GitHub Pages, Netlify, Vercel…

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
- `app.js` : interface (filtres, sélection de fichiers, glisser-déposer, routage `#/outil`)

Pour ajouter un outil, il suffit d'ajouter une entrée dans `TOOLS` (`tools.js`) avec
ses `options()` (HTML du formulaire) et sa fonction `run(files, options, progress)`.
