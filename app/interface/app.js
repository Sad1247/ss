/* Interface Santinel — dialogue avec Python via pywebview.api. */

const $ = (sel) => document.querySelector(sel);

let vue = "tous";      // "tous" | "retardataires"
let fiches = [];
let choisie = null;

function echapper(v) {
  return String(v ?? "").replace(/[&<>"]/g, (c) =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
}

function valeur(v) {
  return v === null || v === undefined || v === "" ? "—" : echapper(v);
}

function etat(message) {
  $("#etat").textContent = message;
}

/* ---------- rendu ---------- */

function dessinerListe() {
  const liste = $("#liste");
  if (!fiches.length) {
    liste.innerHTML = '<p class="vide">Aucun résultat.</p>';
    $("#detail").innerHTML = '<p class="vide">Aucune fiche à afficher.</p>';
    return;
  }
  liste.innerHTML = fiches.map((f) => `
    <article class="entree ${f.filemaker_ok ? "" : "retard"} ${f.id === choisie ? "choisie" : ""}"
             data-id="${f.id}">
      <div class="entree-texte">
        <div class="nom">${echapper(f.nom)}</div>
        <div class="meta">${valeur(f.service)} · ${valeur(f.nom_ordinateur)}</div>
      </div>
      <span class="etat-emploi ${f.actif ? "oui" : "non"}">${f.actif ? "Actif" : "Inactif"}</span>
    </article>`).join("");

  liste.querySelectorAll(".entree").forEach((el) => {
    el.onclick = () => {
      choisie = Number(el.dataset.id);
      dessinerListe();
      dessinerDetail();
    };
  });
}

function dessinerDetail() {
  const f = fiches.find((x) => x.id === choisie);
  if (!f) {
    $("#detail").innerHTML = '<p class="vide">Sélectionnez une fiche à gauche.</p>';
    return;
  }
  const equipements = f.equipements.length
    ? `<table>
         <tr><th>Type</th><th>Description</th><th>Numéro de série</th></tr>
         ${f.equipements.map((e) => `
           <tr><td>${valeur(e.type)}</td><td>${valeur(e.description)}</td>
               <td>${valeur(e.numero_serie)}</td></tr>`).join("")}
       </table>`
    : '<p class="vide" style="margin:0">Aucun équipement enregistré.</p>';

  $("#detail").innerHTML = `
    <div class="entete-fiche">
      <div>
        <h1>${echapper(f.nom)}</h1>
        <p class="sous-titre">${valeur(f.service)}</p>
      </div>
      <div class="controle-etat">
        <span class="etat-emploi ${f.actif ? "oui" : "non"}">${f.actif ? "Actif" : "Inactif"}</span>
        <button type="button" id="basculer-etat" class="bouton-etat">
          ${f.actif ? "Marquer inactif" : "Réactiver"}
        </button>
      </div>
    </div>

    <section class="bloc">
      <h2>Coordonnées</h2>
      <div class="champs">
        ${champ("Téléphone", f.telephone)}
        ${champ("Poste interne", f.poste_interne)}
      </div>
    </section>

    <section class="bloc">
      <h2>Poste de travail</h2>
      <div class="champs">
        ${champ("Nom de l'ordinateur", f.nom_ordinateur)}
        ${champ("Modèle", f.modele)}
        ${champ("Numéro de série", f.numero_serie)}
        ${champ("Mise en service", f.mise_en_service)}
        <div class="champ">
          <div class="etiquette">FileMaker</div>
          <div class="valeur">
            <span class="etat-fm ${f.filemaker_ok ? "ok" : "retard"}">
              ${echapper(f.version_filemaker)}${f.filemaker_ok ? "" : " — à mettre à jour"}
            </span>
          </div>
        </div>
      </div>
    </section>

    <section class="bloc">
      <h2>Équipements</h2>
      ${equipements}
    </section>`;

  $("#basculer-etat").onclick = () => basculerEtat(f);
}

async function basculerEtat(f) {
  const bouton = $("#basculer-etat");
  bouton.disabled = true;
  try {
    f.actif = await pywebview.api.definir_actif(f.id, !f.actif);
    dessinerListe();
    dessinerDetail();
    etat(`${f.nom} est désormais ${f.actif ? "actif" : "inactif"}.`);
  } catch (err) {
    bouton.disabled = false;
    etat("Modification refusée : " + err);
  }
}

function champ(etiquette, v) {
  return `<div class="champ">
            <div class="etiquette">${echapper(etiquette)}</div>
            <div class="valeur">${valeur(v)}</div>
          </div>`;
}

/* ---------- données ---------- */

async function charger() {
  const terme = $("#terme").value.trim();
  try {
    if (terme) {
      fiches = await pywebview.api.chercher(terme);
      if (vue === "retardataires") fiches = fiches.filter((f) => !f.filemaker_ok);
    } else {
      fiches = vue === "retardataires"
        ? await pywebview.api.retardataires()
        : await pywebview.api.tous();
    }
  } catch (err) {
    fiches = [];
    etat("Erreur : " + err);
    dessinerListe();
    return;
  }
  if (!fiches.some((f) => f.id === choisie)) choisie = fiches.length ? fiches[0].id : null;
  dessinerListe();
  dessinerDetail();
  etat(`${fiches.length} fiche${fiches.length > 1 ? "s" : ""} affichée${fiches.length > 1 ? "s" : ""}`);
}

async function majCompteur() {
  const retard = await pywebview.api.retardataires();
  const p = $("#compteur-retard");
  p.textContent = retard.length;
  p.dataset.zero = retard.length ? "non" : "oui";
}

/* ---------- connexion ---------- */

async function ouvrirSession() {
  $("#connexion").hidden = true;
  $("#appli").hidden = false;
  await charger();
  await majCompteur();
  $("#terme").focus();
}

function refuser(message) {
  const erreur = $("#erreur");
  erreur.textContent = message;
  erreur.hidden = false;
  const panneau = $(".panneau");
  panneau.classList.remove("refus");
  void panneau.offsetWidth;          // relance l'animation
  panneau.classList.add("refus");
  $("#motdepasse").value = "";
  $("#motdepasse").focus();
}

$("#formulaire").addEventListener("submit", async (evt) => {
  evt.preventDefault();
  const bouton = $(".bouton-hud");
  bouton.disabled = true;
  try {
    const ok = await pywebview.api.connexion($("#utilisateur").value, $("#motdepasse").value);
    if (ok) {
      $("#erreur").hidden = true;
      await ouvrirSession();
    } else {
      refuser("Identifiants incorrects. Accès au parc refusé.");
    }
  } catch (err) {
    refuser("Erreur de connexion : " + err);
  } finally {
    bouton.disabled = false;
  }
});

const menu = $("#menu-compte");
const boutonCompte = $("#bouton-compte");

function ouvrirMenu(ouvert) {
  menu.hidden = !ouvert;
  boutonCompte.setAttribute("aria-expanded", ouvert ? "true" : "false");
}

boutonCompte.onclick = (evt) => {
  evt.stopPropagation();
  ouvrirMenu(menu.hidden);
};

document.addEventListener("click", () => ouvrirMenu(false));
document.addEventListener("keydown", (evt) => {
  if (evt.key === "Escape") {
    ouvrirMenu(false);
    $("#fenetre-compte").hidden = true;
  }
});

$("#voir-compte").onclick = async () => {
  ouvrirMenu(false);
  try {
    const infos = await pywebview.api.compte();
    $("#compte-utilisateur").textContent = infos.utilisateur;
    $("#compte-base").textContent = infos.base;
    $("#fenetre-compte").hidden = false;
  } catch (err) {
    etat("Compte indisponible : " + err);
  }
};

$("#fermer-compte").onclick = () => { $("#fenetre-compte").hidden = true; };
$("#fenetre-compte").onclick = (evt) => {
  if (evt.target === $("#fenetre-compte")) $("#fenetre-compte").hidden = true;
};

$("#deconnexion").onclick = async () => {
  await pywebview.api.deconnexion();
  ouvrirMenu(false);
  $("#fenetre-compte").hidden = true;
  fiches = [];
  choisie = null;
  $("#appli").hidden = true;
  $("#connexion").hidden = false;
  $("#formulaire").reset();
  $("#erreur").hidden = true;
  $("#utilisateur").focus();
};

/* ---------- évènements ---------- */

let minuterie;
$("#terme").addEventListener("input", () => {
  clearTimeout(minuterie);
  minuterie = setTimeout(charger, 180);
});

document.querySelectorAll(".onglet").forEach((b) => {
  b.onclick = () => {
    document.querySelectorAll(".onglet").forEach((x) => x.classList.remove("actif"));
    b.classList.add("actif");
    vue = b.dataset.vue;
    charger();
  };
});

window.addEventListener("pywebviewready", () => {
  $("#utilisateur").focus();
});
