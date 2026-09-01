/* Interface Santinel — dialogue avec Python via pywebview.api. */

const $ = (sel) => document.querySelector(sel);

let session = null;      // {utilisateur, role} une fois connecté
let vue = "tous";        // "tous" | "retardataires"
let fiches = [];
let choisie = null;
let edition = null;      // null | "coordonnees" | "poste" | "equipements"
let brouillon = [];      // équipements en cours de saisie

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

/** Fiche suivie dont FileMaker n'est pas à jour : celles de l'onglet. */
function aRelancer(f) {
  return f.suivi && !f.filemaker_ok;
}

function peutModifier() {
  return session !== null && session.role === "administrateur";
}

/* ---------- liste ---------- */

function dessinerListe() {
  const liste = $("#liste");
  if (!fiches.length) {
    liste.innerHTML = '<p class="vide">Aucun résultat.</p>';
    $("#detail").innerHTML = '<p class="vide">Aucune fiche à afficher.</p>';
    return;
  }
  liste.innerHTML = fiches.map((f) => `
    <article class="entree ${aRelancer(f) ? "retard" : ""} ${f.id === choisie ? "choisie" : ""}"
             data-id="${f.id}">
      <div class="entree-texte">
        <div class="nom">${echapper(f.nom)}</div>
        <div class="meta">${valeur(f.service)} · ${valeur(f.nom_ordinateur)}</div>
      </div>
      <span class="etat-emploi ${f.actif ? "oui" : "non"}">${f.actif ? "Actif" : "Inactif"}</span>
    </article>`).join("");

  liste.querySelectorAll(".entree").forEach((el) => {
    el.onclick = () => {
      edition = null;
      choisie = Number(el.dataset.id);
      dessinerListe();
      dessinerDetail();
    };
  });
}

/* ---------- fiche ---------- */

function champ(etiquette, v) {
  return `<div class="champ">
            <div class="etiquette">${echapper(etiquette)}</div>
            <div class="valeur">${valeur(v)}</div>
          </div>`;
}

function champSaisie(etiquette, id, v, indication = "") {
  return `<div class="champ">
            <div class="etiquette">${echapper(etiquette)}</div>
            <input id="${id}" class="saisie" value="${echapper(v ?? "")}"
                   placeholder="${echapper(indication)}">
          </div>`;
}

/** Boutons Modifier, ou Annuler / Enregistrer, dans l'en-tête d'un bloc. */
function actionsBloc(cle) {
  if (!peutModifier()) return "";
  if (edition === cle) {
    return `<span class="actions-bloc">
              <button type="button" class="lien-action" data-annuler="${cle}">Annuler</button>
              <button type="button" class="lien-action fort" data-enregistrer="${cle}">Enregistrer</button>
            </span>`;
  }
  return edition === null
    ? `<button type="button" class="lien-action" data-modifier="${cle}">Modifier</button>`
    : "";
}

function blocCoordonnees(f) {
  const corps = edition === "coordonnees"
    ? champSaisie("Téléphone", "saisie-telephone", f.telephone) +
      champSaisie("Poste interne", "saisie-poste", f.poste_interne)
    : champ("Téléphone", f.telephone) + champ("Poste interne", f.poste_interne);
  return `<section class="bloc">
            <h2>Coordonnées ${actionsBloc("coordonnees")}</h2>
            <div class="champs">${corps}</div>
          </section>`;
}

function blocPoste(f) {
  const corps = edition === "poste"
    ? champSaisie("Nom de l'ordinateur", "saisie-ordinateur", f.nom_ordinateur) +
      champSaisie("Modèle", "saisie-modele", f.modele) +
      champSaisie("Numéro de série", "saisie-serie", f.numero_serie) +
      champSaisie("Mise en service", "saisie-date", f.mise_en_service, "AAAA-MM-JJ") +
      champSaisie("FileMaker", "saisie-filemaker",
                  f.version_filemaker === "Non installé" ? "" : f.version_filemaker,
                  "vide si non installé")
    : champ("Nom de l'ordinateur", f.nom_ordinateur) +
      champ("Modèle", f.modele) +
      champ("Numéro de série", f.numero_serie) +
      champ("Mise en service", f.mise_en_service) +
      `<div class="champ">
         <div class="etiquette">FileMaker</div>
         <div class="valeur">
           <span class="etat-fm ${!f.suivi ? "hors" : f.filemaker_ok ? "ok" : "retard"}">
             ${echapper(f.version_filemaker)}${
               !f.suivi ? " — hors suivi" : f.filemaker_ok ? "" : " — à mettre à jour"}
           </span>
         </div>
         ${peutModifier() && edition === null
           ? `<button type="button" id="basculer-suivi" class="lien-action petit"
                      title="Inclure ou retirer cette fiche de l'onglet « À mettre à jour »">
                ${f.suivi ? "Retirer du suivi" : "Remettre au suivi"}
              </button>`
           : ""}
       </div>`;
  return `<section class="bloc">
            <h2>Poste de travail ${actionsBloc("poste")}</h2>
            <div class="champs">${corps}</div>
          </section>`;
}

function blocEquipements(f) {
  let corps;
  if (edition === "equipements") {
    corps = `
      <table id="tableau-equipements">
        <thead>
          <tr><th>Type</th><th>Description</th><th>Numéro de série</th><th></th></tr>
        </thead>
        <tbody>
          ${brouillon.map((e, i) => `
            <tr>
              <td><input class="saisie eq-type" value="${echapper(e.type ?? "")}"></td>
              <td><input class="saisie eq-description" value="${echapper(e.description ?? "")}"></td>
              <td><input class="saisie eq-serie" value="${echapper(e.numero_serie ?? "")}"></td>
              <td><button type="button" class="bouton-supprimer" data-ligne="${i}"
                          title="Retirer cet équipement">×</button></td>
            </tr>`).join("")}
        </tbody>
      </table>
      <button type="button" id="ajouter-equipement" class="lien-action">+ Ajouter un équipement</button>`;
  } else if (f.equipements.length) {
    corps = `
      <table>
        <tr><th>Type</th><th>Description</th><th>Numéro de série</th></tr>
        ${f.equipements.map((e) => `
          <tr><td>${valeur(e.type)}</td><td>${valeur(e.description)}</td>
              <td>${valeur(e.numero_serie)}</td></tr>`).join("")}
      </table>`;
  } else {
    corps = '<p class="vide" style="margin:0">Aucun équipement enregistré.</p>';
  }
  return `<section class="bloc">
            <h2>Équipements ${actionsBloc("equipements")}</h2>
            ${corps}
          </section>`;
}

function dessinerDetail() {
  const f = fiches.find((x) => x.id === choisie);
  if (!f) {
    $("#detail").innerHTML = '<p class="vide">Sélectionnez une fiche à gauche.</p>';
    return;
  }

  $("#detail").innerHTML = `
    <div class="entete-fiche">
      <div>
        <h1>${echapper(f.nom)}</h1>
        <p class="sous-titre">${valeur(f.service)}</p>
      </div>
      <div class="controle-etat">
        <span class="etat-emploi ${f.actif ? "oui" : "non"}">${f.actif ? "Actif" : "Inactif"}</span>
        ${peutModifier() && edition === null
          ? `<button type="button" id="basculer-etat" class="bouton-etat">
               ${f.actif ? "Marquer inactif" : "Réactiver"}
             </button>`
          : ""}
      </div>
    </div>
    ${blocCoordonnees(f)}
    ${blocPoste(f)}
    ${blocEquipements(f)}`;

  brancherFiche(f);
}

function brancherFiche(f) {
  if (!peutModifier()) return;

  const basculer = $("#basculer-etat");
  if (basculer) basculer.onclick = () => basculerEtat(f);

  const suivi = $("#basculer-suivi");
  if (suivi) suivi.onclick = () => basculerSuivi(f);

  document.querySelectorAll("[data-modifier]").forEach((b) => {
    b.onclick = () => {
      edition = b.dataset.modifier;
      if (edition === "equipements") {
        brouillon = f.equipements.map((e) => ({ ...e }));
        if (!brouillon.length) brouillon.push({ type: "", description: "", numero_serie: "" });
      }
      dessinerDetail();
    };
  });

  if (edition === null) return;

  document.querySelectorAll("[data-annuler]").forEach((b) => {
    b.onclick = () => { edition = null; dessinerDetail(); };
  });
  document.querySelectorAll("[data-enregistrer]").forEach((b) => {
    b.onclick = () => enregistrer(f);
  });

  const ajouter = $("#ajouter-equipement");
  if (ajouter) {
    ajouter.onclick = () => {
      brouillon = lireEquipements();
      brouillon.push({ type: "", description: "", numero_serie: "" });
      dessinerDetail();
    };
  }
  document.querySelectorAll("[data-ligne]").forEach((b) => {
    b.onclick = () => {
      brouillon = lireEquipements();
      brouillon.splice(Number(b.dataset.ligne), 1);
      dessinerDetail();
    };
  });

  const saisies = document.querySelectorAll(".bloc .saisie");
  if (saisies.length) saisies[0].focus();
  saisies.forEach((entree) => {
    entree.onkeydown = (evt) => {
      if (evt.key === "Enter") enregistrer(f);
      if (evt.key === "Escape") { edition = null; dessinerDetail(); }
    };
  });
}

function lireEquipements() {
  return [...document.querySelectorAll("#tableau-equipements tbody tr")].map((tr) => ({
    type: tr.querySelector(".eq-type").value,
    description: tr.querySelector(".eq-description").value,
    numero_serie: tr.querySelector(".eq-serie").value,
  }));
}

async function enregistrer(f) {
  const bouton = document.querySelector("[data-enregistrer]");
  bouton.disabled = true;
  const bloc = edition;
  try {
    if (bloc === "coordonnees") {
      const v = await pywebview.api.definir_coordonnees(
        f.id, $("#saisie-telephone").value, $("#saisie-poste").value);
      Object.assign(f, v);
    } else if (bloc === "poste") {
      const v = await pywebview.api.definir_poste(f.id, {
        nom_ordinateur: $("#saisie-ordinateur").value,
        modele: $("#saisie-modele").value,
        numero_serie: $("#saisie-serie").value,
        mise_en_service: $("#saisie-date").value,
        version_filemaker: $("#saisie-filemaker").value,
      });
      Object.assign(f, v);
      // le libellé et l'état FileMaker sont recalculés par Python
      const frais = (await pywebview.api.chercher(f.nom)).find((x) => x.id === f.id);
      if (frais) Object.assign(f, frais);
    } else {
      f.equipements = await pywebview.api.definir_equipements(f.id, lireEquipements());
    }
    edition = null;
    dessinerListe();
    dessinerDetail();
    await majCompteur();
    etat(`Fiche de ${f.nom} enregistrée.`);
  } catch (err) {
    bouton.disabled = false;
    etat("Enregistrement refusé : " + err);
  }
}

async function basculerSuivi(f) {
  const bouton = $("#basculer-suivi");
  bouton.disabled = true;
  try {
    f.suivi = await pywebview.api.definir_suivi(f.id, !f.suivi);
    dessinerListe();
    dessinerDetail();
    await majCompteur();
    etat(f.suivi
      ? `${f.nom} est de nouveau suivi pour FileMaker.`
      : `${f.nom} ne figurera plus dans « À mettre à jour ».`);
  } catch (err) {
    bouton.disabled = false;
    etat("Modification refusée : " + err);
  }
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

/* ---------- données ---------- */

async function charger() {
  edition = null;
  const terme = $("#terme").value.trim();
  try {
    if (terme) {
      fiches = await pywebview.api.chercher(terme);
      if (vue === "retardataires") fiches = fiches.filter(aRelancer);
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
    session = await pywebview.api.connexion($("#utilisateur").value, $("#motdepasse").value);
    if (session) {
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
    $("#compte-role").textContent = infos.role_libelle;
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
  session = null;
  edition = null;
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
