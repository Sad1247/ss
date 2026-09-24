#!/usr/bin/env python3
"""Génère le site publié de FreePDF dans dist/.

Une vraie page par outil (/fusionner-pdf/, /signer-pdf/…) avec son titre, sa
description, un mode d'emploi et une FAQ lisibles par Google, plus
sitemap.xml, robots.txt, une page confidentialité et une page 404.

    python3 build.py                                  # adresse par défaut
    SITE_URL=https://freepdf.example python3 build.py     # avec votre domaine

index.html reste utilisable tel quel (mode « hash », une seule page).
"""
import hashlib
import html
import json
import os
import re
import shutil
from datetime import date
from pathlib import Path

SRC = Path(__file__).resolve().parent
OUT = SRC / "dist"
SITE_URL = os.environ.get("SITE_URL", "https://sad1247.github.io/ss").rstrip("/")
ASSETS = ["style.css", "tools.js", "app.js", "favicon.svg"]
VERSIONS = {a: hashlib.sha1((SRC / a).read_bytes()).hexdigest()[:10] for a in ASSETS}

esc = html.escape


def parse_tools():
    """Lit l'id, le nom, la catégorie et la description de chaque outil dans tools.js."""
    js = (SRC / "tools.js").read_text(encoding="utf-8")
    block = js.split("const SLUGS = {")[1].split("};")[0]
    slugs = dict(re.findall(r"'?([\w-]+)'?:\s*'([\w-]+)'", block))
    tools = []
    for m in re.finditer(r"id: '([\w-]+)', name: '([^']+)', cats: \['(\w+)'\].*?desc: '([^']+)'", js, re.S):
        tid, name, cat, desc = m.groups()
        if tid in slugs:
            tools.append({"id": tid, "name": name, "cat": cat, "desc": desc, "slug": slugs[tid]})
    return tools


CAT_LABELS = {"organize": "Organiser", "optimize": "Optimiser", "convert": "Convertir",
              "edit": "Modifier", "security": "Sécurité"}


def json_ld(data):
    txt = json.dumps(data, ensure_ascii=False, indent=1).replace("</", "<\\/")
    return f'<script type="application/ld+json">\n{txt}\n</script>'


def render(template, *, root, url, title, description, tool=None, article="", show="home",
           noindex=False, ld=()):
    h = template
    attrs = f' data-mode="pages" data-root="{root}"' + (f' data-tool="{tool["id"]}"' if tool else "")
    h = h.replace('<html lang="fr">', f'<html lang="fr"{attrs}>', 1)

    head = [
        f"<title>{esc(title)}</title>",
        f'<meta name="description" content="{esc(description)}">',
        f'<link rel="canonical" href="{url}">',
        '<meta property="og:type" content="website">',
        '<meta property="og:site_name" content="FreePDF">',
        '<meta property="og:locale" content="fr_FR">',
        f'<meta property="og:title" content="{esc(title)}">',
        f'<meta property="og:description" content="{esc(description)}">',
        f'<meta property="og:url" content="{url}">',
        '<meta name="twitter:card" content="summary">',
        '<meta name="theme-color" content="#141414">',
    ]
    if noindex:
        head.append('<meta name="robots" content="noindex">')
    if os.environ.get("GOOGLE_SITE_VERIFICATION"):
        head.append(f'<meta name="google-site-verification" content="{esc(os.environ["GOOGLE_SITE_VERIFICATION"])}">')
    head += [json_ld(d) for d in ld]
    h = re.sub(r"<title>.*?</title>\n\s*<meta name=\"description\"[^>]*>", "\n  ".join(head), h, count=1, flags=re.S)

    # Fichiers et liens internes : chemins relatifs à la racine du site.
    # Numéro de version = empreinte du fichier : chaque mise à jour force les navigateurs
    # (et les iframes, comme sur Blogger, qui ont leur propre cache) à la recharger.
    for asset in ("style.css", "tools.js", "app.js", "favicon.svg"):
        h = h.replace(f'"{asset}"', f'"{root}{asset}?v={VERSIONS[asset]}"')
    home = root or "./"
    h = h.replace('href="#accueil" data-filter="convert"', f'href="{root}#convertir" data-filter="convert"')
    h = h.replace('href="#accueil" data-filter="all"', f'href="{root}#outils" data-filter="all"')
    h = h.replace('href="#accueil"', f'href="{home}"')
    for t in TOOLS:
        h = h.replace(f'href="#{t["id"]}"', f'href="{root}{t["slug"]}/"')

    # Vue affichée dès le chargement (pas de clignotement, contenu lisible sans JavaScript).
    if show != "home":
        h = h.replace('<section id="home" class="view">', '<section id="home" class="view" hidden>', 1)
    if show == "tool":
        h = h.replace('<section id="tool" class="view" hidden>', '<section id="tool" class="view">', 1)
        h = h.replace('<h1 id="tool-title"></h1>', f'<h1 id="tool-title">{esc(tool["name"])}</h1>', 1)
        h = h.replace('<p id="tool-desc"></p>', f'<p id="tool-desc">{esc(tool["desc"])}</p>', 1)
        h = h.replace('<span id="tool-cat"></span>', f'<span id="tool-cat">{CAT_LABELS[tool["cat"]]}</span>', 1)
    if article:
        marker = "\n    </section>\n  </main>" if show == "tool" else "\n  </main>"
        h = h.replace(marker, f"\n{article}{marker}", 1)

    footer_links = f'<nav class="footer-links"><a href="{root}confidentialite/">Confidentialité</a></nav>'
    h = h.replace('<footer class="footer">', f'<footer class="footer">\n    {footer_links}', 1)
    return h


def tool_article(tool, seo, root):
    steps = "".join(f"<li>{esc(s)}</li>" for s in seo["steps"])
    faq = "".join(f"<details><summary>{esc(q)}</summary><p>{esc(a)}</p></details>" for q, a in seo["faq"])
    related = [t for t in TOOLS if t["cat"] == tool["cat"] and t["id"] != tool["id"]]
    related += [t for t in TOOLS if t["id"] in ("merge", "compress", "sign") and t not in related and t["id"] != tool["id"]]
    links = "".join(f'<li><a href="{root}{t["slug"]}/">{esc(t["name"])}</a></li>' for t in related[:4])
    return f"""      <article class="guide">
        <p class="guide-intro">{esc(seo["intro"])}</p>
        <div class="guide-cols">
          <section>
            <h2>Mode d’emploi</h2>
            <ol class="steps">{steps}</ol>
          </section>
          <section>
            <h2>Questions fréquentes</h2>
            <div class="faq">{faq}</div>
          </section>
        </div>
        <section class="related">
          <h2>Autres outils</h2>
          <ul>{links}</ul>
        </section>
      </article>"""


PRIVACY = """      <article class="guide legal">
        <p class="label">Confidentialité</p>
        <h1>Vos fichiers restent chez vous</h1>
        <p class="guide-intro">FreePDF traite vos PDF directement dans votre navigateur. Aucun fichier n’est envoyé sur un serveur, ni conservé, ni lu par qui que ce soit d’autre que vous.</p>
        <h2>Ce qui se passe quand vous utilisez un outil</h2>
        <p>Les fichiers que vous sélectionnez sont lus et transformés par votre ordinateur, grâce aux bibliothèques libres pdf-lib, PDF.js et JSZip. Le résultat est créé sur votre appareil et téléchargé depuis celui-ci. Fermer la page efface tout.</p>
        <h2>Ce que nous ne faisons pas</h2>
        <p>Pas de compte, pas de cookie publicitaire, pas de mesure d’audience, pas de revente de données. Le site mémorise seulement, dans votre navigateur, votre choix de thème clair ou sombre.</p>
        <h2>Services techniques utilisés</h2>
        <p>Comme la plupart des sites, FreePDF charge certaines ressources auprès de services tiers : les bibliothèques de traitement depuis cdnjs (Cloudflare), les polices depuis Google Fonts, et les pages depuis son hébergeur. Ces services voient l’adresse IP de votre connexion lors du chargement de la page, mais jamais vos fichiers.</p>
        <p class="small">Dernière mise à jour : {date}</p>
      </article>"""


def main():
    global TOOLS
    TOOLS = parse_tools()
    seo = json.loads((SRC / "seo.json").read_text(encoding="utf-8"))
    template = (SRC / "index.html").read_text(encoding="utf-8")
    missing = [t["id"] for t in TOOLS if t["id"] not in seo["tools"]]
    if missing:
        raise SystemExit(f"seo.json : contenu manquant pour {missing}")

    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir()
    for a in ASSETS:
        shutil.copy(SRC / a, OUT / a)
    (OUT / ".nojekyll").write_text("")

    app_ld = {"@context": "https://schema.org", "@type": "WebApplication", "name": "FreePDF",
              "url": f"{SITE_URL}/", "applicationCategory": "UtilitiesApplication",
              "operatingSystem": "Tous (navigateur web)", "inLanguage": "fr",
              "offers": {"@type": "Offer", "price": "0", "priceCurrency": "EUR"},
              "description": seo["home"]["description"]}
    pages = [f"{SITE_URL}/"]
    (OUT / "index.html").write_text(render(
        template, root="", url=f"{SITE_URL}/", title=seo["home"]["title"],
        description=seo["home"]["description"], ld=[app_ld]), encoding="utf-8")

    for t in TOOLS:
        s = seo["tools"][t["id"]]
        url = f"{SITE_URL}/{t['slug']}/"
        ld = [
            {**app_ld, "name": f"{t['name']} – FreePDF", "url": url, "description": s["description"]},
            {"@context": "https://schema.org", "@type": "FAQPage", "mainEntity": [
                {"@type": "Question", "name": q, "acceptedAnswer": {"@type": "Answer", "text": a}}
                for q, a in s["faq"]]},
            {"@context": "https://schema.org", "@type": "BreadcrumbList", "itemListElement": [
                {"@type": "ListItem", "position": 1, "name": "FreePDF", "item": f"{SITE_URL}/"},
                {"@type": "ListItem", "position": 2, "name": t["name"], "item": url}]},
        ]
        d = OUT / t["slug"]
        d.mkdir()
        (d / "index.html").write_text(render(
            template, root="../", url=url, title=f"{s['title']} | FreePDF", description=s["description"],
            tool=t, article=tool_article(t, s, "../"), show="tool", ld=ld), encoding="utf-8")
        pages.append(url)

    d = OUT / "confidentialite"
    d.mkdir()
    (d / "index.html").write_text(render(
        template, root="../", url=f"{SITE_URL}/confidentialite/", title="Confidentialité | FreePDF",
        description="FreePDF traite vos PDF dans votre navigateur : aucun fichier n’est envoyé ni conservé.",
        article=PRIVACY.replace("{date}", date.today().strftime("%d/%m/%Y")), show="page"), encoding="utf-8")
    pages.append(f"{SITE_URL}/confidentialite/")

    # 404 : servie à n'importe quelle profondeur, donc liens absolus depuis la racine du site.
    base = SITE_URL.split("://", 1)[1].split("/", 1)
    root_abs = "/" + (base[1] + "/" if len(base) > 1 and base[1] else "")
    notfound = f"""      <article class="guide legal">
        <p class="label">Erreur 404</p>
        <h1>Cette page n’existe pas</h1>
        <p class="guide-intro">Le lien est peut-être ancien ou incomplet. Tous les outils sont sur la <a href="{root_abs}">page d’accueil</a>.</p>
      </article>"""
    (OUT / "404.html").write_text(render(
        template, root=root_abs, url=f"{SITE_URL}/", title="Page introuvable | FreePDF",
        description="Cette page n’existe pas.", article=notfound, show="page", noindex=True), encoding="utf-8")

    today = date.today().isoformat()
    urls = "".join(f"  <url><loc>{u}</loc><lastmod>{today}</lastmod></url>\n" for u in pages)
    (OUT / "sitemap.xml").write_text(
        f'<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n{urls}</urlset>\n',
        encoding="utf-8")
    (OUT / "robots.txt").write_text(f"User-agent: *\nAllow: /\n\nSitemap: {SITE_URL}/sitemap.xml\n", encoding="utf-8")
    print(f"{len(pages)} pages générées dans {OUT} pour {SITE_URL}")


if __name__ == "__main__":
    main()
