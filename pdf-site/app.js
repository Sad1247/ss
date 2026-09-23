/* Interface : accueil, filtres, pages d'outil et routage par hash. */
'use strict';

const $ = (sel) => document.querySelector(sel);
const state = { tool: null, files: [], filter: 'all', downloadUrl: null };

/* ---------------------------------------------------------------- thème */

(function initTheme() {
  let saved = null;
  try { saved = localStorage.getItem('theme'); } catch {}
  if (saved) document.documentElement.dataset.theme = saved;
  $('.theme-toggle').addEventListener('click', () => {
    const dark = document.documentElement.dataset.theme
      ? document.documentElement.dataset.theme === 'dark'
      : matchMedia('(prefers-color-scheme: dark)').matches;
    const next = dark ? 'light' : 'dark';
    document.documentElement.dataset.theme = next;
    try { localStorage.setItem('theme', next); } catch {}
  });
})();

/* --------------------------------------------------------------- accueil */

function renderFilters() {
  $('.filters').innerHTML = CATEGORIES.map((c) =>
    `<button class="chip" role="tab" data-cat="${c.id}" aria-selected="${c.id === state.filter}">${c.label}</button>`).join('');
}

function renderGrid() {
  const list = TOOLS.filter((t) => state.filter === 'all' || t.cats.includes(state.filter))
    .sort((a, b) => !!a.soon - !!b.soon);
  $('#grid').innerHTML = list.map((t) => {
    const inner = `<div class="tool-icon">${iconSvg(t)}</div><h3>${t.name}</h3><p>${t.desc}</p>`;
    return t.soon
      ? `<div class="card soon" aria-disabled="true"><span class="badge">Bientôt</span>${inner}</div>`
      : `<a class="card" href="#${t.id}">${inner}</a>`;
  }).join('');
}

function setFilter(cat) {
  state.filter = cat;
  renderFilters();
  renderGrid();
}

$('.filters').addEventListener('click', (e) => {
  const chip = e.target.closest('.chip');
  if (chip) setFilter(chip.dataset.cat);
});
document.querySelectorAll('.mainnav [data-filter]').forEach((a) =>
  a.addEventListener('click', () => {
    setFilter(a.dataset.filter);
    setTimeout(() => $('.filters').scrollIntoView({ behavior: 'smooth', block: 'start' }), 0);
  }));

/* ----------------------------------------------------------- page outil */

function showStage(name) {
  for (const s of ['pick', 'work', 'busy', 'done']) $(`#stage-${s}`).hidden = s !== name;
}

function acceptAttr(tool) {
  return tool.accept === 'image' ? 'image/*' : 'application/pdf,.pdf';
}

function openTool(tool) {
  state.tool = tool;
  state.files = [];
  document.title = `${tool.name} — PDFacile`;
  $('#tool-icon').innerHTML = iconSvg(tool);
  $('#tool-title').textContent = tool.name;
  $('#tool-desc').textContent = tool.desc;
  for (const input of [$('#file-input'), $('#file-input-more')]) {
    input.accept = acceptAttr(tool);
    input.multiple = !!tool.multiple;
    input.value = '';
  }
  $('#add-more').hidden = !tool.multiple;
  $('#opt-title').textContent = tool.name;
  $('#run').textContent = tool.action;
  $('#error').hidden = true;
  $('.dropzone .btn').textContent = tool.multiple
    ? (tool.accept === 'image' ? 'Sélectionner des images' : 'Sélectionner des fichiers PDF')
    : 'Sélectionner le fichier PDF';
  showStage('pick');
}

function isAccepted(file) {
  if (state.tool.accept === 'image') return file.type.startsWith('image/');
  return file.type === 'application/pdf' || /\.pdf$/i.test(file.name);
}

function addFiles(list) {
  const incoming = [...list].filter(isAccepted);
  const rejected = list.length - incoming.length;
  if (!incoming.length) {
    if (rejected) showError(state.tool.accept === 'image' ? 'Seules les images sont acceptées.' : 'Seuls les fichiers PDF sont acceptés.');
    return;
  }
  state.files = state.tool.multiple ? state.files.concat(incoming) : [incoming[0]];
  const firstTime = $('#stage-work').hidden;
  renderFiles();
  if (firstTime) {
    const form = $('#opt-form');
    form.innerHTML = state.tool.options ? state.tool.options() : '';
    if (state.tool.prefill) state.tool.prefill(state.files, form).catch(() => {});
  }
  showStage('work');
  hideError();
  if (rejected) showError(`${rejected} fichier(s) ignoré(s) : format non pris en charge.`);
}

const thumbCache = new WeakMap();

function thumbFor(file) {
  if (thumbCache.has(file)) return thumbCache.get(file);
  let p;
  if (file.type.startsWith('image/')) {
    p = Promise.resolve({ el: Object.assign(new Image(), { src: URL.createObjectURL(file), alt: '' }), pages: null });
  } else {
    p = openWithPdfJs(file).then(async (pdf) => {
      const { canvas } = await renderPage(pdf, 1, 0.4);
      return { el: canvas, pages: pdf.numPages };
    }).catch(() => ({ el: Object.assign(document.createElement('span'), { textContent: 'PDF' }), pages: null }));
  }
  thumbCache.set(file, p);
  return p;
}

function renderFiles() {
  const box = $('#files');
  box.innerHTML = '';
  state.files.forEach((file, i) => {
    const card = document.createElement('div');
    card.className = 'file';
    const multi = state.tool.multiple && state.files.length > 1;
    card.innerHTML = `
      ${multi ? `<span class="order">${i + 1}</span>` : ''}
      <div class="actions">
        ${multi && i > 0 ? `<button type="button" data-act="left" data-i="${i}" title="Déplacer avant">◀</button>` : ''}
        ${multi && i < state.files.length - 1 ? `<button type="button" data-act="right" data-i="${i}" title="Déplacer après">▶</button>` : ''}
        <button type="button" data-act="remove" data-i="${i}" title="Retirer">✕</button>
      </div>
      <div class="thumb"></div>
      <div class="name" title="${file.name.replace(/"/g, '&quot;')}"></div>
      <div class="meta">${formatSize(file.size)}</div>`;
    card.querySelector('.name').textContent = file.name;
    box.appendChild(card);
    thumbFor(file).then(({ el, pages }) => {
      // Un canvas ne peut être affiché qu'à un seul endroit : on le clone si besoin.
      let node = el;
      if (el.tagName === 'CANVAS' && el.isConnected) {
        node = document.createElement('canvas');
        node.width = el.width; node.height = el.height;
        node.getContext('2d').drawImage(el, 0, 0);
      }
      card.querySelector('.thumb').replaceChildren(node);
      if (pages) card.querySelector('.meta').textContent = `${pages} page${pages > 1 ? 's' : ''} · ${formatSize(file.size)}`;
    });
  });
}

$('#files').addEventListener('click', (e) => {
  const btn = e.target.closest('button[data-act]');
  if (!btn) return;
  const i = +btn.dataset.i, f = state.files;
  if (btn.dataset.act === 'remove') f.splice(i, 1);
  if (btn.dataset.act === 'left') [f[i - 1], f[i]] = [f[i], f[i - 1]];
  if (btn.dataset.act === 'right') [f[i + 1], f[i]] = [f[i], f[i + 1]];
  if (!f.length) return openTool(state.tool);
  renderFiles();
});

$('#file-input').addEventListener('change', (e) => addFiles(e.target.files));
$('#file-input-more').addEventListener('change', (e) => { addFiles(e.target.files); e.target.value = ''; });

// Glisser-déposer : sur la zone de dépôt, ou n'importe où quand la liste est affichée.
const dz = $('#dropzone');
['dragenter', 'dragover'].forEach((ev) => document.addEventListener(ev, (e) => {
  if (!state.tool || $('#tool').hidden) return;
  e.preventDefault();
  dz.classList.add('over');
}));
['dragleave', 'drop'].forEach((ev) => document.addEventListener(ev, (e) => {
  if (!state.tool || $('#tool').hidden) return;
  e.preventDefault();
  dz.classList.remove('over');
  if (ev === 'drop' && e.dataTransfer.files.length && ($('#stage-pick').hidden === false || $('#stage-work').hidden === false)) {
    addFiles(e.dataTransfer.files);
  }
}));

function showError(msg) { const el = $('#error'); el.textContent = msg; el.hidden = false; }
function hideError() { $('#error').hidden = true; }

$('#run').addEventListener('click', async () => {
  const tool = state.tool;
  hideError();
  if (state.files.length < (tool.min || 1)) {
    return showError(`Ajoutez au moins ${tool.min} fichiers.`);
  }
  const opts = Object.fromEntries(new FormData($('#opt-form')));
  showStage('busy');
  $('#busy-msg').textContent = 'Traitement en cours…';
  try {
    const res = await tool.run(state.files, opts, (msg) => { $('#busy-msg').textContent = msg; });
    if (state.downloadUrl) URL.revokeObjectURL(state.downloadUrl);
    state.downloadUrl = URL.createObjectURL(res.blob);
    state.result = res;
    const a = $('#download');
    a.href = state.downloadUrl;
    a.download = res.filename;
    a.textContent = /\.zip$/.test(res.filename) ? 'Télécharger le ZIP' : `Télécharger ${res.filename.split('.').pop().toUpperCase()}`;
    $('#done-msg').textContent = res.message || '';
    showStage('done');
  } catch (err) {
    console.error(err);
    showStage('work');
    showError(err.message || 'Une erreur est survenue.');
  }
});

$('#restart').addEventListener('click', () => openTool(state.tool));

// Dans une page Artifact de claude.ai, les liens <a download> sont bloqués :
// on passe alors par la capacité « downloads » du lecteur.
let downloads = null;
if (window.claude?.use) window.claude.use('downloads').then((d) => { downloads = d; }, () => {});
$('#download').addEventListener('click', async (e) => {
  if (!downloads || !state.result) return;
  e.preventDefault();
  try {
    await downloads.save({ filename: state.result.filename, data: state.result.blob });
  } catch (err) {
    if (err?.code !== 'declined') $('#done-msg').textContent = 'Le téléchargement a échoué, réessayez.';
  }
});

/* ---------------------------------------------------------------- routage */

function route() {
  const id = location.hash.replace(/^#\/?/, '');
  const tool = TOOLS.find((t) => t.id === id && !t.soon);
  $('#home').hidden = !!tool;
  $('#tool').hidden = !tool;
  if (tool) {
    openTool(tool);
  } else {
    state.tool = null;
    document.title = 'PDFacile — outils PDF gratuits';
  }
  window.scrollTo(0, 0);
}

window.addEventListener('hashchange', route);
renderFilters();
renderGrid();
route();
