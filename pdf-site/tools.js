/* Définition des outils et traitement des PDF (100 % côté navigateur). */
'use strict';

const { PDFDocument, StandardFonts, rgb, degrees } = PDFLib;
pdfjsLib.GlobalWorkerOptions.workerSrc =
  'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';

/* ---------------------------------------------------------------- helpers */

const baseName = (name) => name.replace(/\.[^.]+$/, '');
const pdfBlob = (bytes) => new Blob([bytes], { type: 'application/pdf' });

async function loadPdf(file) {
  const bytes = await file.arrayBuffer();
  try {
    return await PDFDocument.load(bytes);
  } catch (e) {
    if (/encrypt/i.test(e.message)) throw new Error(`« ${file.name} » est protégé par un mot de passe.`);
    throw new Error(`« ${file.name} » n'est pas un PDF valide.`);
  }
}

function openWithPdfJs(file) {
  return file.arrayBuffer().then((buf) => pdfjsLib.getDocument({ data: buf }).promise);
}

function canvasToBlob(canvas, type, quality) {
  return new Promise((resolve) => canvas.toBlob(resolve, type, quality));
}

async function renderPage(pdf, pageNo, scale) {
  const page = await pdf.getPage(pageNo);
  const viewport = page.getViewport({ scale });
  const canvas = document.createElement('canvas');
  canvas.width = Math.ceil(viewport.width);
  canvas.height = Math.ceil(viewport.height);
  const ctx = canvas.getContext('2d');
  ctx.fillStyle = '#fff';
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  await page.render({ canvasContext: ctx, viewport }).promise;
  return { canvas, size: page.getViewport({ scale: 1 }) };
}

/** "1-3, 5, 8-" → tableau de groupes d'indices (base 0). */
function parseRanges(text, pageCount) {
  const groups = [];
  for (const raw of text.split(/[,;]+/)) {
    const part = raw.trim();
    if (!part) continue;
    const m = part.match(/^(\d*)\s*-\s*(\d*)$/);
    let from, to;
    if (m) {
      from = m[1] ? +m[1] : 1;
      to = m[2] ? +m[2] : pageCount;
    } else if (/^\d+$/.test(part)) {
      from = to = +part;
    } else {
      throw new Error(`Plage « ${part} » invalide. Exemple attendu : 1-3, 5, 8-10`);
    }
    if (from < 1 || to > pageCount || from > to) {
      throw new Error(`Plage « ${part} » hors du document (${pageCount} pages).`);
    }
    const g = [];
    for (let i = from; i <= to; i++) g.push(i - 1);
    groups.push(g);
  }
  if (!groups.length) throw new Error('Indiquez au moins une page.');
  return groups;
}

async function zipOrSingle(entries, zipName) {
  if (entries.length === 1) return { blob: entries[0].blob, filename: entries[0].name };
  const zip = new JSZip();
  for (const e of entries) zip.file(e.name, e.blob);
  return { blob: await zip.generateAsync({ type: 'blob' }), filename: zipName };
}

function hexToRgb(hex) {
  const n = parseInt(hex.slice(1), 16);
  return rgb(((n >> 16) & 255) / 255, ((n >> 8) & 255) / 255, (n & 255) / 255);
}

function formatSize(bytes) {
  if (bytes < 1024) return bytes + ' o';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(0) + ' Ko';
  return (bytes / 1024 / 1024).toFixed(1).replace('.', ',') + ' Mo';
}

async function copyPages(src, indices) {
  const out = await PDFDocument.create();
  const pages = await out.copyPages(src, indices);
  pages.forEach((p) => out.addPage(p));
  return out;
}

/* ------------------------------------------------------------- les outils */

// Chaque catégorie a sa couleur (classes CSS .c, .y, .m, .k, .spot).
const CATEGORIES = [
  { id: 'all', label: 'Tout' },
  { id: 'organize', label: 'Organiser', ink: 'c', inkName: 'Encre cyan' },
  { id: 'optimize', label: 'Optimiser', ink: 'y', inkName: 'Encre jaune' },
  { id: 'convert', label: 'Convertir', ink: 'm', inkName: 'Encre magenta' },
  { id: 'edit', label: 'Modifier', ink: 'k', inkName: 'Encre noire' },
  { id: 'security', label: 'Sécurité', ink: 'spot', inkName: 'Ton direct' },
];

// Ce qui entre et ce qui sort de chaque outil.
const IO = {
  merge: 'PDF + PDF → PDF', split: 'PDF → PDF × n', compress: 'PDF → PDF allégé',
  'pdf-word': 'PDF → DOCX', 'pdf-ppt': 'PDF → PPTX', 'pdf-excel': 'PDF → XLSX',
  'pdf-jpg': 'PDF → JPG × n', 'jpg-pdf': 'JPG / PNG → PDF', 'word-pdf': 'DOCX → PDF',
  'pdf-txt': 'PDF → TXT', rotate: 'PDF → PDF ↻', remove: 'PDF − pages → PDF',
  extract: 'PDF → pages choisies', watermark: 'PDF + texte → PDF', pagenum: 'PDF + 1, 2, 3 → PDF',
  metadata: 'PDF → PDF (titre, auteur)', protect: 'PDF + mot de passe', unlock: 'PDF − mot de passe',
};

// Adresse de la page de chaque outil sur le site publié (ex. : /fusionner-pdf/).
const SLUGS = {
  merge: 'fusionner-pdf', split: 'diviser-pdf', compress: 'compresser-pdf',
  'pdf-jpg': 'pdf-en-jpg', 'jpg-pdf': 'jpg-en-pdf', 'pdf-txt': 'pdf-en-texte',
  rotate: 'pivoter-pdf', remove: 'supprimer-pages-pdf', extract: 'extraire-pages-pdf',
  watermark: 'filigrane-pdf', pagenum: 'numeroter-pages-pdf', metadata: 'modifier-proprietes-pdf',
  sign: 'signer-pdf',
};

const TOOLS = [
  {
    id: 'merge', name: 'Fusionner PDF', cats: ['organize'], icon: 'merge',
    desc: 'Combinez plusieurs PDF en un seul document, dans l’ordre de votre choix.',
    accept: 'pdf', multiple: true, min: 2, action: 'Fusionner PDF',
    options: () => `<p class="note">Réorganisez les fichiers avec les flèches ◀ ▶. Ils seront fusionnés de gauche à droite.</p>`,
    async run(files, _o, progress) {
      const out = await PDFDocument.create();
      for (let i = 0; i < files.length; i++) {
        progress(`Ajout de ${files[i].name} (${i + 1}/${files.length})…`);
        const src = await loadPdf(files[i]);
        const pages = await out.copyPages(src, src.getPageIndices());
        pages.forEach((p) => out.addPage(p));
      }
      return { blob: pdfBlob(await out.save()), filename: 'fusion.pdf',
        message: `${files.length} fichiers fusionnés (${out.getPageCount()} pages).` };
    },
  },
  {
    id: 'split', name: 'Diviser PDF', cats: ['organize'], icon: 'split',
    desc: 'Séparez un PDF par plages de pages, ou transformez chaque page en fichier indépendant.',
    accept: 'pdf', multiple: false, action: 'Diviser PDF',
    options: () => `
      <div class="field"><div class="radio-group">
        <label><input type="radio" name="mode" value="each" checked><span>Chaque page<small>Un PDF par page</small></span></label>
        <label><input type="radio" name="mode" value="ranges"><span>Par plages<small>Un PDF par plage indiquée</small></span></label>
      </div></div>
      <div class="field"><label for="o-ranges">Plages</label>
        <input type="text" id="o-ranges" name="ranges" placeholder="1-3, 4-6, 7-">
        <p class="small">Utilisé uniquement en mode « Par plages ».</p></div>`,
    async run([file], o, progress) {
      const src = await loadPdf(file);
      const n = src.getPageCount();
      const groups = o.mode === 'ranges' ? parseRanges(o.ranges, n) : src.getPageIndices().map((i) => [i]);
      const entries = [];
      for (let g = 0; g < groups.length; g++) {
        progress(`Création du fichier ${g + 1}/${groups.length}…`);
        const doc = await copyPages(src, groups[g]);
        const first = groups[g][0] + 1, last = groups[g].at(-1) + 1;
        const suffix = first === last ? `page-${first}` : `pages-${first}-${last}`;
        entries.push({ name: `${baseName(file.name)}_${suffix}.pdf`, blob: pdfBlob(await doc.save()) });
      }
      const res = await zipOrSingle(entries, `${baseName(file.name)}_divise.zip`);
      return { ...res, message: `${entries.length} fichier(s) créé(s).` };
    },
  },
  {
    id: 'compress', name: 'Compresser PDF', cats: ['optimize'], icon: 'compress',
    desc: 'Réduisez la taille de votre PDF en conservant la meilleure qualité possible.',
    accept: 'pdf', multiple: false, action: 'Compresser PDF',
    options: () => `
      <div class="field"><div class="radio-group">
        <label><input type="radio" name="level" value="low"><span>Faible compression<small>Haute qualité</small></span></label>
        <label><input type="radio" name="level" value="medium" checked><span>Compression recommandée<small>Bon compromis qualité / taille</small></span></label>
        <label><input type="radio" name="level" value="high"><span>Forte compression<small>Qualité réduite, fichier minimal</small></span></label>
      </div></div>
      <p class="note">Les pages sont converties en images optimisées : le texte ne sera plus sélectionnable.</p>`,
    async run([file], o, progress) {
      const settings = { low: [2, 0.82], medium: [1.5, 0.65], high: [1.1, 0.45] }[o.level];
      const pdf = await openWithPdfJs(file);
      const out = await PDFDocument.create();
      for (let i = 1; i <= pdf.numPages; i++) {
        progress(`Compression de la page ${i}/${pdf.numPages}…`);
        const { canvas, size } = await renderPage(pdf, i, settings[0]);
        const jpg = await canvasToBlob(canvas, 'image/jpeg', settings[1]);
        const img = await out.embedJpg(await jpg.arrayBuffer());
        const page = out.addPage([size.width, size.height]);
        page.drawImage(img, { x: 0, y: 0, width: size.width, height: size.height });
      }
      const bytes = await out.save();
      if (bytes.length >= file.size) {
        return { blob: file, filename: file.name,
          message: `Ce fichier est déjà bien optimisé (${formatSize(file.size)}) : impossible de le réduire davantage.` };
      }
      const pct = Math.round((1 - bytes.length / file.size) * 100);
      return { blob: pdfBlob(bytes), filename: `${baseName(file.name)}_compresse.pdf`,
        message: `${formatSize(file.size)} → ${formatSize(bytes.length)} (−${pct} %).` };
    },
  },
  { id: 'pdf-word', name: 'PDF en Word', cats: ['convert'], icon: 'W', soon: true,
    desc: 'Convertissez vos PDF en documents DOCX faciles à modifier.' },
  { id: 'pdf-ppt', name: 'PDF en PowerPoint', cats: ['convert'], icon: 'P', soon: true,
    desc: 'Transformez vos PDF en présentations PPTX modifiables.' },
  { id: 'pdf-excel', name: 'PDF en Excel', cats: ['convert'], icon: 'X', soon: true,
    desc: 'Récupérez les tableaux de vos PDF dans des feuilles de calcul Excel.' },
  {
    id: 'pdf-jpg', name: 'PDF en JPG', cats: ['convert'], icon: 'image',
    desc: 'Convertissez chaque page de votre PDF en image JPG de haute qualité.',
    accept: 'pdf', multiple: false, action: 'Convertir en JPG',
    options: () => `
      <div class="field"><label for="o-dpi">Qualité</label>
        <select id="o-dpi" name="scale">
          <option value="1.4">Normale (100 ppp)</option>
          <option value="2.1" selected>Haute (150 ppp)</option>
          <option value="4.2">Très haute (300 ppp)</option>
        </select></div>`,
    async run([file], o, progress) {
      const pdf = await openWithPdfJs(file);
      const entries = [];
      for (let i = 1; i <= pdf.numPages; i++) {
        progress(`Conversion de la page ${i}/${pdf.numPages}…`);
        const { canvas } = await renderPage(pdf, i, +o.scale);
        entries.push({ name: `${baseName(file.name)}_page-${i}.jpg`, blob: await canvasToBlob(canvas, 'image/jpeg', 0.9) });
      }
      const res = await zipOrSingle(entries, `${baseName(file.name)}_images.zip`);
      return { ...res, message: `${entries.length} image(s) générée(s).` };
    },
  },
  {
    id: 'jpg-pdf', name: 'JPG en PDF', cats: ['convert'], icon: 'JPG',
    desc: 'Convertissez vos images JPG ou PNG en PDF. Choisissez le format et les marges.',
    accept: 'image', multiple: true, min: 1, action: 'Convertir en PDF',
    options: () => `
      <div class="field"><label for="o-size">Format de page</label>
        <select id="o-size" name="size">
          <option value="a4" selected>A4</option>
          <option value="letter">US Letter</option>
          <option value="fit">Taille de l’image</option>
        </select></div>
      <div class="field"><label for="o-orient">Orientation</label>
        <select id="o-orient" name="orient">
          <option value="auto" selected>Automatique</option>
          <option value="portrait">Portrait</option>
          <option value="landscape">Paysage</option>
        </select></div>
      <div class="field"><label for="o-margin">Marges</label>
        <select id="o-margin" name="margin">
          <option value="0">Aucune</option>
          <option value="20" selected>Petites</option>
          <option value="50">Grandes</option>
        </select></div>`,
    async run(files, o, progress) {
      const out = await PDFDocument.create();
      const sizes = { a4: [595.28, 841.89], letter: [612, 792] };
      for (let i = 0; i < files.length; i++) {
        progress(`Ajout de l’image ${i + 1}/${files.length}…`);
        const img = await embedImage(out, files[i]);
        const margin = +o.margin;
        let pw, ph;
        if (o.size === 'fit') {
          pw = img.width + 2 * margin; ph = img.height + 2 * margin;
        } else {
          [pw, ph] = sizes[o.size];
          const landscape = o.orient === 'landscape' || (o.orient === 'auto' && img.width > img.height);
          if (landscape) [pw, ph] = [ph, pw];
        }
        const scale = Math.min((pw - 2 * margin) / img.width, (ph - 2 * margin) / img.height, o.size === 'fit' ? 1 : Infinity);
        const w = img.width * scale, h = img.height * scale;
        out.addPage([pw, ph]).drawImage(img, { x: (pw - w) / 2, y: (ph - h) / 2, width: w, height: h });
      }
      return { blob: pdfBlob(await out.save()), filename: files.length === 1 ? `${baseName(files[0].name)}.pdf` : 'images.pdf',
        message: `${files.length} image(s) converties en PDF.` };
    },
  },
  { id: 'word-pdf', name: 'Word en PDF', cats: ['convert'], icon: 'W', soon: true,
    desc: 'Convertissez vos documents DOC et DOCX en PDF fidèles à l’original.' },
  {
    id: 'pdf-txt', name: 'PDF en texte', cats: ['convert'], icon: 'TXT',
    desc: 'Extrayez tout le texte d’un PDF dans un simple fichier .txt.',
    accept: 'pdf', multiple: false, action: 'Extraire le texte',
    options: () => `<p class="note">Fonctionne sur les PDF contenant du vrai texte (pas les scans).</p>`,
    async run([file], _o, progress) {
      const pdf = await openWithPdfJs(file);
      const parts = [];
      for (let i = 1; i <= pdf.numPages; i++) {
        progress(`Lecture de la page ${i}/${pdf.numPages}…`);
        const content = await (await pdf.getPage(i)).getTextContent();
        let line = '', text = '';
        for (const item of content.items) {
          line += item.str;
          if (item.hasEOL) { text += line + '\n'; line = ''; }
        }
        parts.push(`--- Page ${i} ---\n${text}${line}`);
      }
      const txt = parts.join('\n\n');
      const empty = txt.replace(/--- Page \d+ ---|\s/g, '') === '';
      return { blob: new Blob([txt], { type: 'text/plain;charset=utf-8' }), filename: `${baseName(file.name)}.txt`,
        message: empty ? 'Aucun texte trouvé : ce PDF est probablement une image scannée.' : `Texte de ${pdf.numPages} page(s) extrait.` };
    },
  },
  {
    id: 'rotate', name: 'Pivoter PDF', cats: ['organize'], icon: 'rotate',
    desc: 'Faites pivoter les pages de votre PDF. Toutes à la fois ou seulement certaines.',
    accept: 'pdf', multiple: true, min: 1, action: 'Pivoter PDF',
    options: () => `
      <div class="field"><label for="o-angle">Rotation</label>
        <select id="o-angle" name="angle">
          <option value="90" selected>90° vers la droite</option>
          <option value="180">180°</option>
          <option value="270">90° vers la gauche</option>
        </select></div>
      <div class="field"><label for="o-pages">Pages</label>
        <input type="text" id="o-pages" name="pages" placeholder="Toutes (ex. : 1, 3-5)">
        <p class="small">Laissez vide pour pivoter toutes les pages.</p></div>`,
    async run(files, o, progress) {
      const entries = [];
      for (const file of files) {
        progress(`Rotation de ${file.name}…`);
        const doc = await loadPdf(file);
        const n = doc.getPageCount();
        const targets = o.pages.trim() ? new Set(parseRanges(o.pages, n).flat()) : null;
        doc.getPages().forEach((p, i) => {
          if (!targets || targets.has(i)) p.setRotation(degrees((p.getRotation().angle + +o.angle) % 360));
        });
        entries.push({ name: `${baseName(file.name)}_pivote.pdf`, blob: pdfBlob(await doc.save()) });
      }
      const res = await zipOrSingle(entries, 'pdf_pivotes.zip');
      return { ...res, message: `${files.length} fichier(s) pivoté(s).` };
    },
  },
  {
    id: 'remove', name: 'Supprimer des pages', cats: ['organize'], icon: 'trash',
    desc: 'Retirez les pages dont vous n’avez pas besoin de votre document PDF.',
    accept: 'pdf', multiple: false, action: 'Supprimer les pages',
    options: () => `
      <div class="field"><label for="o-pages">Pages à supprimer</label>
        <input type="text" id="o-pages" name="pages" placeholder="ex. : 2, 5-7"></div>`,
    async run([file], o) {
      const src = await loadPdf(file);
      const n = src.getPageCount();
      const drop = new Set(parseRanges(o.pages, n).flat());
      const keep = src.getPageIndices().filter((i) => !drop.has(i));
      if (!keep.length) throw new Error('Vous ne pouvez pas supprimer toutes les pages.');
      const doc = await copyPages(src, keep);
      return { blob: pdfBlob(await doc.save()), filename: `${baseName(file.name)}_modifie.pdf`,
        message: `${drop.size} page(s) supprimée(s), ${keep.length} restante(s).` };
    },
  },
  {
    id: 'extract', name: 'Extraire des pages', cats: ['organize'], icon: 'extract',
    desc: 'Récupérez uniquement les pages qui vous intéressent dans un nouveau PDF.',
    accept: 'pdf', multiple: false, action: 'Extraire les pages',
    options: () => `
      <div class="field"><label for="o-pages">Pages à extraire</label>
        <input type="text" id="o-pages" name="pages" placeholder="ex. : 1, 4-6"></div>`,
    async run([file], o) {
      const src = await loadPdf(file);
      const pages = parseRanges(o.pages, src.getPageCount()).flat();
      const doc = await copyPages(src, pages);
      return { blob: pdfBlob(await doc.save()), filename: `${baseName(file.name)}_extrait.pdf`,
        message: `${pages.length} page(s) extraite(s).` };
    },
  },
  {
    id: 'watermark', name: 'Ajouter un filigrane', cats: ['edit'], icon: 'watermark',
    desc: 'Apposez un texte en filigrane sur toutes les pages de votre PDF.',
    accept: 'pdf', multiple: false, action: 'Ajouter le filigrane',
    options: () => `
      <div class="field"><label for="o-text">Texte</label>
        <input type="text" id="o-text" name="text" value="CONFIDENTIEL"></div>
      <div class="field"><label for="o-size">Taille : <output id="o-size-out">60</output> pt</label>
        <input type="range" id="o-size" name="size" min="12" max="140" value="60" oninput="this.form['o-size-out'].value=this.value"></div>
      <div class="field"><label for="o-opacity">Opacité : <output id="o-opacity-out">25</output> %</label>
        <input type="range" id="o-opacity" name="opacity" min="5" max="100" value="25" oninput="this.form['o-opacity-out'].value=this.value"></div>
      <div class="field"><label for="o-color">Couleur</label>
        <input type="color" id="o-color" name="color" value="#d23b3b"></div>
      <div class="field"><label for="o-angle">Inclinaison</label>
        <select id="o-angle" name="angle"><option value="45" selected>Diagonale</option><option value="0">Horizontale</option></select></div>`,
    async run([file], o) {
      if (!o.text.trim()) throw new Error('Saisissez le texte du filigrane.');
      const doc = await loadPdf(file);
      const font = await doc.embedFont(StandardFonts.HelveticaBold);
      const size = +o.size, angle = +o.angle;
      let width;
      try { width = font.widthOfTextAtSize(o.text, size); } catch {
        throw new Error('Ce texte contient des caractères non pris en charge (emoji, alphabet non latin…).');
      }
      const rad = (angle * Math.PI) / 180;
      for (const page of doc.getPages()) {
        const { width: pw, height: ph } = page.getSize();
        // centre le texte en tenant compte de la rotation
        const x = pw / 2 - (width / 2) * Math.cos(rad) + (size / 3) * Math.sin(rad);
        const y = ph / 2 - (width / 2) * Math.sin(rad) - (size / 3) * Math.cos(rad);
        page.drawText(o.text, { x, y, size, font, color: hexToRgb(o.color), opacity: +o.opacity / 100, rotate: degrees(angle) });
      }
      return { blob: pdfBlob(await doc.save()), filename: `${baseName(file.name)}_filigrane.pdf`,
        message: `Filigrane ajouté sur ${doc.getPageCount()} page(s).` };
    },
  },
  {
    id: 'pagenum', name: 'Numéros de page', cats: ['edit'], icon: 'number',
    desc: 'Numérotez les pages de votre PDF en choisissant la position et le format.',
    accept: 'pdf', multiple: false, action: 'Numéroter les pages',
    options: () => `
      <div class="field"><label for="o-pos">Position</label>
        <select id="o-pos" name="pos">
          <option value="bc" selected>En bas, au centre</option>
          <option value="br">En bas, à droite</option>
          <option value="bl">En bas, à gauche</option>
          <option value="tc">En haut, au centre</option>
          <option value="tr">En haut, à droite</option>
        </select></div>
      <div class="field"><label for="o-format">Format</label>
        <select id="o-format" name="format">
          <option value="n" selected>1</option>
          <option value="nt">1 / 10</option>
          <option value="page">Page 1 sur 10</option>
        </select></div>
      <div class="field"><label for="o-start">Premier numéro</label>
        <input type="number" id="o-start" name="start" value="1" min="0"></div>`,
    async run([file], o) {
      const doc = await loadPdf(file);
      const font = await doc.embedFont(StandardFonts.Helvetica);
      const pages = doc.getPages(), total = pages.length, start = parseInt(o.start, 10) || 0;
      const size = 11, pad = 28;
      pages.forEach((page, i) => {
        const n = start + i, last = start + total - 1;
        const label = o.format === 'nt' ? `${n} / ${last}` : o.format === 'page' ? `Page ${n} sur ${last}` : `${n}`;
        const { width, height } = page.getSize();
        const w = font.widthOfTextAtSize(label, size);
        const x = o.pos[1] === 'c' ? (width - w) / 2 : o.pos[1] === 'r' ? width - pad - w : pad;
        const y = o.pos[0] === 'b' ? pad - 8 : height - pad;
        page.drawText(label, { x, y, size, font, color: rgb(0.2, 0.2, 0.2) });
      });
      return { blob: pdfBlob(await doc.save()), filename: `${baseName(file.name)}_numerote.pdf`,
        message: `${total} page(s) numérotée(s).` };
    },
  },
  {
    id: 'metadata', name: 'Modifier les propriétés', cats: ['edit'], icon: 'info',
    desc: 'Changez le titre, l’auteur et le sujet enregistrés dans votre fichier PDF.',
    accept: 'pdf', multiple: false, action: 'Enregistrer',
    options: () => `
      <div class="field"><label for="o-title">Titre</label><input type="text" id="o-title" name="title"></div>
      <div class="field"><label for="o-author">Auteur</label><input type="text" id="o-author" name="author"></div>
      <div class="field"><label for="o-subject">Sujet</label><input type="text" id="o-subject" name="subject"></div>`,
    async prefill([file], form) {
      const doc = await loadPdf(file);
      form.title.value = doc.getTitle() || '';
      form.author.value = doc.getAuthor() || '';
      form.subject.value = doc.getSubject() || '';
    },
    async run([file], o) {
      const doc = await loadPdf(file);
      doc.setTitle(o.title); doc.setAuthor(o.author); doc.setSubject(o.subject);
      doc.setProducer('FreePDF'); doc.setModificationDate(new Date());
      return { blob: pdfBlob(await doc.save()), filename: file.name, message: 'Propriétés mises à jour.' };
    },
  },
  { id: 'protect', name: 'Protéger PDF', cats: ['security'], icon: 'lock', soon: true,
    desc: 'Protégez vos fichiers PDF avec un mot de passe pour empêcher les accès non autorisés.' },
  { id: 'unlock', name: 'Déverrouiller PDF', cats: ['security'], icon: 'unlock', soon: true,
    desc: 'Retirez le mot de passe de vos PDF pour les utiliser librement.' },
];

async function embedImage(doc, file) {
  const bytes = await file.arrayBuffer();
  if (file.type === 'image/jpeg') return doc.embedJpg(bytes);
  if (file.type === 'image/png') return doc.embedPng(bytes);
  // Autres formats (WebP, GIF…) : on passe par un canvas.
  const bmp = await createImageBitmap(file);
  const c = document.createElement('canvas');
  c.width = bmp.width; c.height = bmp.height;
  c.getContext('2d').drawImage(bmp, 0, 0);
  return doc.embedPng(await (await canvasToBlob(c, 'image/png')).arrayBuffer());
}

/* ---------------------------------------------------------------- icônes */

// Pictogrammes au trait (viewBox 24), dessinés à l'encre de la page.
const PAGE = 'M6 3h9l4 4v14H6z M15 3v4h4';
const GLYPHS = {
  merge: 'M3 3h7v7H3z M14 3h7v7h-7z M6.5 10v2.5h11V10 M12 12.5V15 M8.5 15h7v6h-7z',
  split: 'M8.5 3h7v6h-7z M12 9v2.5 M6.5 14v-2.5h11V14 M3 14h7v7H3z M14 14h7v7h-7z',
  compress: 'M6 3h12v18H6z M12 5.5v4 M9.5 7.5l2.5 2.5 2.5-2.5 M12 18.5v-4 M9.5 16.5l2.5-2.5 2.5 2.5',
  rotate: 'M4 9h10v12H4z M9 4.5a8.5 8.5 0 0 1 10.5 8 M17 10.5l2.5 2.5 2.5-2.5',
  trash: 'M4 7h16 M9.5 7V4h5v3 M6.5 7l1 14h9l1-14 M10 11v6 M14 11v6',
  extract: 'M13 3H6v18h6 M13 3l5 5v3 M13 3v5h5 M14 17h7 M18 14l3 3-3 3',
  watermark: PAGE + ' M9 17l7-7 M9 13l3-3',
  number: PAGE + ' M9.5 12h6.5 M9.5 16h6.5 M11.5 10l-.8 8 M15 10l-.8 8',
  info: PAGE + ' M9 11h7 M9 14h7 M9 17h4',
  image: 'M3 5h18v14H3z M3 16l5-5 4 4 3-3 6 6 M15.5 8.5h1',
  lock: 'M5 11h14v10H5z M8 11V7.5a4 4 0 0 1 8 0V11 M12 15v2',
  unlock: 'M5 11h14v10H5z M8 11V7.5a4 4 0 0 1 7.6-1.8 M12 15v2',
};

function iconSvg(tool) {
  const stroke = 'fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"';
  if (GLYPHS[tool.icon]) {
    return `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="${GLYPHS[tool.icon]}" ${stroke}/></svg>`;
  }
  // Formats (W, P, X, JPG, TXT) : une page portant l'extension.
  const size = tool.icon.length > 1 ? 5.2 : 7.5;
  return `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="${PAGE}" ${stroke}/>` +
    `<text x="12.5" y="${tool.icon.length > 1 ? 16.5 : 17.5}" text-anchor="middle" fill="currentColor" ` +
    `font-family="IBM Plex Mono, monospace" font-weight="600" font-size="${size}">${tool.icon}</text></svg>`;
}

function inkOf(tool) {
  return CATEGORIES.find((c) => c.id === tool.cats[0]).ink;
}

/* ------------------------------------------------------------ signature */

// État de l'outil « Signer » : la page affichée, la position de la signature
// (en fractions de la page telle qu'on la voit) et l'image de la signature.
const SIGN = { pdf: null, page: 1, fx: 0.58, fy: 0.78, fw: 0.3, img: null, strokes: [], stage: null };

const SIGN_FONTS = {
  dancing: '"Dancing Script", cursive',
  caveat: 'Caveat, cursive',
  vibes: '"Great Vibes", cursive',
};

function signOptions() {
  return `<div class="sign-opts">
    <div class="field"><span class="label">Votre signature</span>
      <div class="seg" role="radiogroup" aria-label="Type de signature">
        <label><input type="radio" name="mode" value="draw" checked><span>Dessiner</span></label>
        <label><input type="radio" name="mode" value="type"><span>Taper</span></label>
        <label><input type="radio" name="mode" value="image"><span>Image</span></label>
      </div>
    </div>
    <div class="sig-panel" data-mode="draw">
      <div class="pad"><canvas id="sig-pad" aria-label="Zone de dessin de la signature"></canvas>
        <span class="pad-line" aria-hidden="true"></span>
        <button type="button" class="pad-clear" id="sig-clear">Effacer</button></div>
      <p class="small">Signez avec la souris, le doigt ou un stylet.</p>
    </div>
    <div class="sig-panel" data-mode="type" hidden>
      <div class="field"><label for="o-name">Nom</label>
        <input type="text" id="o-name" name="name" placeholder="Camille Martin" autocomplete="name"></div>
      <div class="field"><label for="o-font">Écriture</label>
        <select id="o-font" name="font">
          <option value="dancing">Dancing Script</option>
          <option value="caveat">Caveat</option>
          <option value="vibes">Great Vibes</option>
        </select></div>
    </div>
    <div class="sig-panel" data-mode="image" hidden>
      <div class="field"><label for="o-img">Photo ou scan de votre signature</label>
        <input type="file" id="o-img" name="img" accept="image/*"></div>
      <label class="check"><input type="checkbox" name="clearbg" checked> Retirer le fond blanc</label>
    </div>
    <div class="field"><span class="label">Encre</span>
      <div class="inks">
        <label><input type="radio" name="ink" value="#1a1f36" checked><span style="--sw:#1a1f36">Noir</span></label>
        <label><input type="radio" name="ink" value="#1f3fb4"><span style="--sw:#1f3fb4">Bleu</span></label>
      </div>
    </div>
    <div class="field"><label for="o-where">Pages à signer</label>
      <select id="o-where" name="where">
        <option value="current" selected>Page affichée uniquement</option>
        <option value="last">Dernière page</option>
        <option value="all">Toutes les pages</option>
      </select></div>
    <label class="check"><input type="checkbox" name="date"> Ajouter « Signé le ${new Date().toLocaleDateString('fr-FR')} »</label>
  </div>`;
}

/* --- fabrication de l'image de la signature --- */

function drawStrokes(ctx, strokes, scale, ox, oy, color) {
  ctx.strokeStyle = color;
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.lineWidth = 2.6 * scale;
  for (const s of strokes) {
    ctx.beginPath();
    ctx.moveTo((s[0].x - ox) * scale, (s[0].y - oy) * scale);
    if (s.length === 1) ctx.lineTo((s[0].x - ox) * scale + 0.1, (s[0].y - oy) * scale);
    for (let i = 1; i < s.length - 1; i++) {
      const mx = (s[i].x + s[i + 1].x) / 2, my = (s[i].y + s[i + 1].y) / 2;
      ctx.quadraticCurveTo((s[i].x - ox) * scale, (s[i].y - oy) * scale, (mx - ox) * scale, (my - oy) * scale);
    }
    if (s.length > 1) ctx.lineTo((s.at(-1).x - ox) * scale, (s.at(-1).y - oy) * scale);
    ctx.stroke();
  }
}

async function buildSignature(form) {
  const mode = form.mode.value, color = form.ink.value;
  const c = document.createElement('canvas');
  const ctx = c.getContext('2d');

  if (mode === 'draw') {
    const pts = SIGN.strokes.flat();
    if (!pts.length) return null;
    const pad = 6, scale = 4;
    const minX = Math.min(...pts.map((p) => p.x)) - pad, minY = Math.min(...pts.map((p) => p.y)) - pad;
    const maxX = Math.max(...pts.map((p) => p.x)) + pad, maxY = Math.max(...pts.map((p) => p.y)) + pad;
    c.width = Math.ceil((maxX - minX) * scale); c.height = Math.ceil((maxY - minY) * scale);
    drawStrokes(ctx, SIGN.strokes, scale, minX, minY, color);
  } else if (mode === 'type') {
    const name = form.name.value.trim();
    if (!name) return null;
    const family = SIGN_FONTS[form.font.value];
    try { await document.fonts.load(`120px ${family}`, name); } catch {}
    const font = `120px ${family}`;
    ctx.font = font;
    const m = ctx.measureText(name);
    c.width = Math.ceil(m.width + 40); c.height = 190;
    ctx.font = font; ctx.fillStyle = color; ctx.textBaseline = 'alphabetic';
    ctx.fillText(name, 20, 140);
  } else {
    const file = form.img.files[0];
    if (!file) return null;
    const bmp = await createImageBitmap(file);
    const k = Math.min(1, 1600 / bmp.width);
    c.width = Math.round(bmp.width * k); c.height = Math.round(bmp.height * k);
    ctx.drawImage(bmp, 0, 0, c.width, c.height);
    if (form.clearbg.checked) {
      const data = ctx.getImageData(0, 0, c.width, c.height);
      const d = data.data;
      for (let i = 0; i < d.length; i += 4) {
        const lum = 0.299 * d[i] + 0.587 * d[i + 1] + 0.114 * d[i + 2];
        // blanc → transparent, avec un fondu pour garder des bords propres
        d[i + 3] = Math.min(d[i + 3], Math.max(0, Math.min(255, (235 - lum) * 6)));
      }
      ctx.putImageData(data, 0, 0);
    }
  }
  return { url: c.toDataURL('image/png'), aspect: c.height / c.width };
}

/* --- pavé de dessin --- */

function setupPad(canvas, onChange) {
  const ctx = canvas.getContext('2d');
  const color = () => canvas.closest('form')?.ink.value || '#1a1f36';
  function resize() {
    if (!canvas.isConnected) return;
    const r = canvas.getBoundingClientRect();
    const dpr = window.devicePixelRatio || 1;
    canvas.width = Math.round(r.width * dpr); canvas.height = Math.round(r.height * dpr);
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    drawStrokes(ctx, SIGN.strokes, dpr, 0, 0, color());
  }
  let current = null;
  const pos = (e) => { const r = canvas.getBoundingClientRect(); return { x: e.clientX - r.left, y: e.clientY - r.top }; };
  canvas.addEventListener('pointerdown', (e) => {
    canvas.setPointerCapture(e.pointerId);
    current = [pos(e)];
    SIGN.strokes.push(current);
    resize();
  });
  canvas.addEventListener('pointermove', (e) => {
    if (!current) return;
    current.push(pos(e));
    resize();
  });
  const end = () => { if (current) { current = null; onChange(); } };
  canvas.addEventListener('pointerup', end);
  canvas.addEventListener('pointercancel', end);
  new ResizeObserver(resize).observe(canvas);
  return { redraw: resize, clear() { SIGN.strokes = []; resize(); onChange(); } };
}

/* --- zone de placement sur la page --- */

async function renderSignPage(stage) {
  const page = await SIGN.pdf.getPage(SIGN.page);
  const base = page.getViewport({ scale: 1 });
  // La page doit tenir en entier à l'écran, en largeur comme en hauteur.
  const maxH = Math.max(420, window.innerHeight - 180);
  const width = Math.min(stage.parentElement.clientWidth, 760, maxH * base.width / base.height);
  const dpr = window.devicePixelRatio || 1;
  const viewport = page.getViewport({ scale: (width / base.width) * dpr });
  const canvas = stage.querySelector('canvas');
  canvas.width = Math.round(viewport.width); canvas.height = Math.round(viewport.height);
  stage.style.aspectRatio = `${base.width} / ${base.height}`;
  stage.style.width = '100%';
  stage.style.maxWidth = `${width}px`;
  const ctx = canvas.getContext('2d');
  ctx.fillStyle = '#fff'; ctx.fillRect(0, 0, canvas.width, canvas.height);
  await page.render({ canvasContext: ctx, viewport }).promise;
  stage.closest('.signer').querySelector('.page-no').textContent = `Page ${SIGN.page} / ${SIGN.pdf.numPages}`;
  placeBox(stage);
}

function boxHeightFraction(stage) {
  const r = stage.getBoundingClientRect();
  const aspect = SIGN.img ? SIGN.img.aspect : 0.33;
  return r.height ? (SIGN.fw * r.width * aspect) / r.height : 0.1;
}

function clampBox(stage) {
  SIGN.fw = Math.min(Math.max(SIGN.fw, 0.06), 1);
  const fh = boxHeightFraction(stage);
  SIGN.fx = Math.min(Math.max(SIGN.fx, 0), 1 - SIGN.fw);
  SIGN.fy = Math.min(Math.max(SIGN.fy, 0), Math.max(0, 1 - fh));
}

function placeBox(stage) {
  clampBox(stage);
  const box = stage.querySelector('.sig-box');
  box.style.left = `${SIGN.fx * 100}%`;
  box.style.top = `${SIGN.fy * 100}%`;
  box.style.width = `${SIGN.fw * 100}%`;
  box.style.height = `${boxHeightFraction(stage) * 100}%`;
  const img = box.querySelector('img');
  img.hidden = !SIGN.img;
  if (SIGN.img) img.src = SIGN.img.url;
  box.querySelector('.sig-ph').hidden = !!SIGN.img;
}

function setupStage(stage) {
  const box = stage.querySelector('.sig-box');
  let drag = null;
  const start = (e, kind) => {
    e.preventDefault(); e.stopPropagation();
    box.setPointerCapture(e.pointerId);
    drag = { kind, x: e.clientX, y: e.clientY, fx: SIGN.fx, fy: SIGN.fy, fw: SIGN.fw };
  };
  box.addEventListener('pointerdown', (e) => start(e, e.target.classList.contains('sig-handle') ? 'resize' : 'move'));
  box.addEventListener('pointermove', (e) => {
    if (!drag) return;
    const r = stage.getBoundingClientRect();
    const dx = (e.clientX - drag.x) / r.width, dy = (e.clientY - drag.y) / r.height;
    if (drag.kind === 'move') { SIGN.fx = drag.fx + dx; SIGN.fy = drag.fy + dy; }
    else { SIGN.fw = Math.min(drag.fw + dx, 1 - drag.fx); }
    placeBox(stage);
  });
  const stop = () => { drag = null; };
  box.addEventListener('pointerup', stop);
  box.addEventListener('pointercancel', stop);
  // Cliquer sur la page y amène la signature.
  stage.addEventListener('pointerdown', (e) => {
    if (e.target.closest('.sig-box')) return;
    const r = stage.getBoundingClientRect();
    SIGN.fx = (e.clientX - r.left) / r.width - SIGN.fw / 2;
    SIGN.fy = (e.clientY - r.top) / r.height - boxHeightFraction(stage) / 2;
    placeBox(stage);
  });
  box.addEventListener('keydown', (e) => {
    const step = e.shiftKey ? 0.05 : 0.01;
    const moves = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, -step], ArrowDown: [0, step] };
    if (e.key === '+' || e.key === '-') { SIGN.fw += e.key === '+' ? 0.02 : -0.02; }
    else if (moves[e.key]) { SIGN.fx += moves[e.key][0]; SIGN.fy += moves[e.key][1]; }
    else return;
    e.preventDefault();
    placeBox(stage);
  });
}

/* --- report sur le PDF --- */

// Convertit un point de la page telle qu'affichée (origine en haut à gauche)
// en coordonnées PDF, en tenant compte de la rotation de la page.
function displayToPdf(page, rot, dx, dy) {
  const { x: x0, y: y0, width: W, height: H } = page.getCropBox();
  if (rot === 90) return { x: x0 + dy, y: y0 + dx };
  if (rot === 180) return { x: x0 + W - dx, y: y0 + dy };
  if (rot === 270) return { x: x0 + W - dy, y: y0 + H - dx };
  return { x: x0 + dx, y: y0 + H - dy };
}

const SIGN_TOOL = {
  id: 'sign', name: 'Signer PDF', cats: ['security'], icon: 'sign',
  desc: 'Dessinez, tapez ou importez votre signature, puis placez-la où vous voulez sur le document.',
  accept: 'pdf', multiple: false, action: 'Signer le PDF', tableLabel: 'Placez votre signature',
  options: signOptions,

  async mount({ area, file, form, firstTime, reset }) {
    if (firstTime) {
      Object.assign(SIGN, { page: 1, fx: 0.58, fy: 0.78, fw: 0.3, img: null, strokes: [] });
      const refresh = async () => {
        SIGN.img = await buildSignature(form);
        if (SIGN.stage) placeBox(SIGN.stage);
      };
      // Les écouteurs vont sur le bloc d'options (recréé à chaque ouverture),
      // pas sur le formulaire, partagé par tous les outils.
      const opts = form.querySelector('.sign-opts');
      const pad = setupPad(form.querySelector('#sig-pad'), refresh);
      form.querySelector('#sig-clear').addEventListener('click', () => pad.clear());
      opts.addEventListener('input', (e) => {
        if (e.target.name === 'mode') {
          form.querySelectorAll('.sig-panel').forEach((p) => { p.hidden = p.dataset.mode !== form.mode.value; });
          pad.redraw();
        }
        if (e.target.name === 'ink') pad.redraw();
        if (['mode', 'ink', 'name', 'font', 'clearbg'].includes(e.target.name)) refresh();
      });
      opts.addEventListener('change', (e) => { if (e.target.name === 'img') refresh(); });
    }
    area.innerHTML = `
      <div class="signer">
        <div class="signer-bar">
          <button type="button" class="nav prev" aria-label="Page précédente">◀</button>
          <span class="page-no mono">Page 1</span>
          <button type="button" class="nav next" aria-label="Page suivante">▶</button>
          <button type="button" class="change">Changer de fichier</button>
        </div>
        <div class="page-stage">
          <canvas></canvas>
          <div class="sig-box" tabindex="0" role="button"
               aria-label="Signature : glissez pour déplacer, flèches pour ajuster, + et − pour la taille">
            <img alt="" hidden><span class="sig-ph">Votre signature</span><span class="sig-handle" aria-hidden="true"></span>
          </div>
        </div>
        <p class="small mono">Glissez la signature, tirez le coin pour l’agrandir, ou cliquez sur la page pour l’y déposer.</p>
      </div>`;
    const stage = area.querySelector('.page-stage');
    SIGN.stage = stage;
    area.querySelector('.change').addEventListener('click', reset);
    SIGN.pdf = await openWithPdfJs(file);
    SIGN.page = Math.min(SIGN.page, SIGN.pdf.numPages);
    const go = (d) => {
      const n = SIGN.page + d;
      if (n < 1 || n > SIGN.pdf.numPages) return;
      SIGN.page = n;
      renderSignPage(stage);
    };
    area.querySelector('.prev').addEventListener('click', () => go(-1));
    area.querySelector('.next').addEventListener('click', () => go(1));
    setupStage(stage);
    await renderSignPage(stage);
  },

  async run([file], o, progress) {
    if (!SIGN.img) throw new Error('Créez d’abord votre signature : dessinez-la, tapez votre nom ou importez une image.');
    progress('Apposition de la signature…');
    const doc = await loadPdf(file);
    const png = await doc.embedPng(SIGN.img.url);
    const font = o.date ? await doc.embedFont(StandardFonts.Helvetica) : null;
    const pages = doc.getPages();
    const targets = o.where === 'all' ? pages.map((_, i) => i) : o.where === 'last' ? [pages.length - 1] : [SIGN.page - 1];
    const [r, g, b] = [1, 3, 5].map((i) => parseInt(o.ink.slice(i, i + 2), 16) / 255);
    for (const i of targets) {
      const page = pages[i];
      const rot = ((page.getRotation().angle % 360) + 360) % 360;
      const { width: W, height: H } = page.getCropBox();
      const [dw, dh] = rot === 90 || rot === 270 ? [H, W] : [W, H];
      const w = SIGN.fw * dw, h = w * SIGN.img.aspect;
      const left = SIGN.fx * dw, top = Math.min(SIGN.fy * dh, dh - h);
      const at = displayToPdf(page, rot, left, top + h);
      page.drawImage(png, { x: at.x, y: at.y, width: w, height: h, rotate: degrees(rot) });
      if (font) {
        const size = Math.max(7, Math.min(11, w / 14));
        const t = displayToPdf(page, rot, left, Math.min(top + h + size * 1.3, dh - 2));
        page.drawText(`Signé le ${new Date().toLocaleDateString('fr-FR')}`,
          { x: t.x, y: t.y, size, font, color: rgb(r, g, b), rotate: degrees(rot) });
      }
    }
    return { blob: pdfBlob(await doc.save()), filename: `${baseName(file.name)}_signe.pdf`,
      message: `Signature apposée sur ${targets.length} page(s).` };
  },
};

TOOLS.splice(TOOLS.findIndex((t) => t.id === 'protect'), 0, SIGN_TOOL);
IO.sign = 'PDF + signature → PDF';
GLYPHS.sign = 'M3 20.5h18 M15 4l5 5L9.5 19.5H4.5v-5z M12.5 6.5l5 5';
