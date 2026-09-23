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

// Chaque catégorie est imprimée avec une encre : cyan, jaune, magenta, noir, et un ton direct.
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
      doc.setProducer('PDFacile'); doc.setModificationDate(new Date());
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
