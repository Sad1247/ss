/* Interface Santinel — dialogue avec Python via pywebview.api. */

const $ = (sel) => document.querySelector(sel);

let session = null;      // {utilisateur, role} une fois connecté
let vue = "tous";        // "tous" | "retardataires"
let fiches = [];
let choisie = null;
let edition = null;      // null | "coordonnees" | "poste" | "equipements"
let brouillon = [];      // équipements en cours de saisie
let confirmation = false;  // suppression en attente de confirmation

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

/** Fiche figurant dans l'onglet « À mettre à jour ». */
function aRelancer(f) {
  return f.suivi;
}

/** Peut modifier les fiches : administrateur ou compte de modification. */
function peutModifier() {
  return session !== null && session.role !== "lecture";
}

/** Peut gérer les comptes d'accès : administrateur seulement. */
function peutGererComptes() {
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
        <div class="meta">${[f.service, f.nom_ordinateur]
          .filter(Boolean).map(echapper).join(" · ") || "—"}</div>
      </div>
      <span class="etat-emploi ${f.actif ? "oui" : "non"}">${f.actif ? "Actif" : "Inactif"}</span>
    </article>`).join("");

  liste.querySelectorAll(".entree").forEach((el) => {
    el.onclick = () => {
      edition = null;
      confirmation = false;
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

function blocEmploi(f) {
  const corps = edition === "emploi"
    ? champSaisie("Titre", "saisie-titre", f.titre) +
      champSaisie("Compagnie", "saisie-compagnie", f.compagnie) +
      champSaisie("Département", "saisie-service", f.service)
    : champ("Titre", f.titre) +
      champ("Compagnie", f.compagnie) +
      champ("Département", f.service);
  return `<section class="bloc">
            <h2>Emploi ${actionsBloc("emploi")}</h2>
            <div class="champs">${corps}</div>
          </section>`;
}

function blocCoordonnees(f) {
  const corps = edition === "coordonnees"
    ? champSaisie("Téléphone", "saisie-telephone", f.telephone) +
      champSaisie("Poste interne", "saisie-poste", f.poste_interne) +
      champSaisie("Nom d'utilisateur", "saisie-utilisateur", f.nom_utilisateur) +
      champSaisie("Courriel", "saisie-courriel", f.courriel, "nom@santinel.ca")
    : champ("Téléphone", f.telephone) +
      champ("Poste interne", f.poste_interne) +
      champ("Nom d'utilisateur", f.nom_utilisateur) +
      champ("Courriel", f.courriel);
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
      champSaisie("Processeur", "saisie-cpu", f.cpu) +
      champSaisie("Type d'appareil", "saisie-type", f.type_appareil, "Portable, Mini-PC…") +
      champSaisie("Mise en service", "saisie-date", f.mise_en_service, "AAAA-MM-JJ") +
      champSaisie("FileMaker", "saisie-filemaker",
                  f.version_filemaker === "Non installé" ? "" : f.version_filemaker,
                  "vide si non installé") +
      `<div class="champ">
         <div class="etiquette">Windows 11</div>
         <select id="saisie-windows11" class="saisie">
           <option value=""  ${f.windows11 === null ? "selected" : ""}>Inconnu</option>
           <option value="1" ${f.windows11 === true ? "selected" : ""}>Migré</option>
           <option value="0" ${f.windows11 === false ? "selected" : ""}>Non migré</option>
         </select>
       </div>`
    : champ("Nom de l'ordinateur", f.nom_ordinateur) +
      champ("Modèle", f.modele) +
      champ("Numéro de série", f.numero_serie) +
      champ("Processeur", f.cpu) +
      champ("Type d'appareil", f.type_appareil) +
      champ("Mise en service", f.mise_en_service) +
      champ("Windows 11", f.windows11 === null ? null
            : f.windows11 ? "Migré" : "Non migré") +
      `<div class="champ">
         <div class="etiquette">FileMaker</div>
         <div class="valeur">
           <span class="etat-fm ${f.suivi ? "retard" : f.filemaker_ok ? "ok" : "hors"}">
             ${echapper(f.version_filemaker)}${
               f.suivi ? " — à mettre à jour" : f.filemaker_ok ? "" : " — hors suivi"}
           </span>
         </div>
         ${peutModifier() && edition === null
           ? `<div class="actions-suivi">
                <button type="button" id="basculer-suivi" class="lien-action petit"
                        title="Inclure ou retirer cette fiche de l'onglet « À mettre à jour »">
                  ${f.suivi ? "Retirer du suivi" : "Mettre au suivi"}
                </button>
                ${f.suivi_choisi
                  ? `<button type="button" id="suivi-auto" class="lien-action petit"
                             title="Laisser de nouveau la version FileMaker décider">
                       Automatique
                     </button>`
                  : ""}
              </div>`
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
        <p class="sous-titre">${[f.titre, f.compagnie, f.service]
          .filter(Boolean).map(echapper).join(" · ") || "—"}</p>
      </div>
      <div class="controle-etat">
        <span class="etat-emploi ${f.actif ? "oui" : "non"}">${f.actif ? "Actif" : "Inactif"}</span>
        ${edition === null
          ? `<button type="button" id="document" class="bouton-etat"
                     title="Formulaire d'équipement prêté, rempli, en PDF">
               Équipement prêté
             </button>`
          : ""}
        ${peutModifier() && edition === null
          ? `<button type="button" id="basculer-etat" class="bouton-etat">
               ${f.actif ? "Marquer inactif" : "Réactiver"}
             </button>
             <button type="button" id="supprimer" class="bouton-etat danger">Supprimer</button>`
          : ""}
      </div>
    </div>
    ${confirmation ? `
      <div class="confirmation">
        <div>
          <strong>Supprimer définitivement la fiche de ${echapper(f.nom)} ?</strong>
          <p>Son poste de travail et ses équipements partent avec. C'est
             irréversible. Pour un départ, « Marquer inactif » conserve
             l'historique.</p>
        </div>
        <div class="actions-bloc">
          <button type="button" id="annuler-suppression" class="lien-action">Annuler</button>
          <button type="button" id="confirmer-suppression" class="lien-action danger">
            Supprimer
          </button>
        </div>
      </div>` : ""}
    ${blocEmploi(f)}
    ${blocCoordonnees(f)}
    ${blocPoste(f)}
    ${blocEquipements(f)}`;

  brancherFiche(f);
}

function brancherFiche(f) {
  if (!peutModifier()) return;

  const basculer = $("#basculer-etat");
  if (basculer) basculer.onclick = () => basculerEtat(f);

  const bouton = $("#document");
  if (bouton) bouton.onclick = () => genererDocument(f);

  const supprimer = $("#supprimer");
  if (supprimer) supprimer.onclick = () => { confirmation = true; dessinerDetail(); };

  const annuler = $("#annuler-suppression");
  if (annuler) annuler.onclick = () => { confirmation = false; dessinerDetail(); };

  const confirmer = $("#confirmer-suppression");
  if (confirmer) confirmer.onclick = () => supprimerFiche(f);

  const suivi = $("#basculer-suivi");
  if (suivi) suivi.onclick = () => basculerSuivi(f);

  const auto = $("#suivi-auto");
  if (auto) auto.onclick = () => rendreAutomatique(f);

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
    if (bloc === "emploi") {
      const v = await pywebview.api.definir_emploi(f.id, {
        titre: $("#saisie-titre").value,
        compagnie: $("#saisie-compagnie").value,
        service: $("#saisie-service").value,
      });
      Object.assign(f, v);
    } else if (bloc === "coordonnees") {
      const v = await pywebview.api.definir_coordonnees(f.id, {
        telephone: $("#saisie-telephone").value,
        poste_interne: $("#saisie-poste").value,
        nom_utilisateur: $("#saisie-utilisateur").value,
        courriel: $("#saisie-courriel").value,
      });
      Object.assign(f, v);
    } else if (bloc === "poste") {
      const v = await pywebview.api.definir_poste(f.id, {
        nom_ordinateur: $("#saisie-ordinateur").value,
        modele: $("#saisie-modele").value,
        numero_serie: $("#saisie-serie").value,
        cpu: $("#saisie-cpu").value,
        type_appareil: $("#saisie-type").value,
        mise_en_service: $("#saisie-date").value,
        version_filemaker: $("#saisie-filemaker").value,
        windows11: $("#saisie-windows11").value,
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

async function genererDocument(f) {
  const bouton = $("#document");
  bouton.disabled = true;
  etat(`Préparation du formulaire de ${f.nom}…`);
  try {
    const resultat = await pywebview.api.generer_document(f.id);
    etat(resultat.ouvert
      ? `Formulaire ouvert : ${resultat.chemin}`
      : `Formulaire enregistré : ${resultat.chemin}`);
  } catch (err) {
    etat("Formulaire impossible : " + err);
  } finally {
    bouton.disabled = false;
  }
}

async function supprimerFiche(f) {
  const bouton = $("#confirmer-suppression");
  bouton.disabled = true;
  try {
    const nom = await pywebview.api.supprimer(f.id);
    confirmation = false;
    choisie = null;
    await charger();
    etat(`Fiche de ${nom} supprimée.`);
  } catch (err) {
    bouton.disabled = false;
    etat("Suppression refusée : " + err);
  }
}

async function basculerSuivi(f) {
  const bouton = $("#basculer-suivi");
  bouton.disabled = true;
  try {
    f.suivi = await pywebview.api.definir_suivi(f.id, !f.suivi);
    f.suivi_choisi = true;
    await rafraichir();
    etat(f.suivi
      ? `${f.nom} figure maintenant dans « À mettre à jour ».`
      : `${f.nom} ne figure plus dans « À mettre à jour ».`);
  } catch (err) {
    bouton.disabled = false;
    etat("Modification refusée : " + err);
  }
}

async function rendreAutomatique(f) {
  const bouton = $("#suivi-auto");
  bouton.disabled = true;
  try {
    await pywebview.api.suivi_automatique(f.id);
    f.suivi_choisi = false;
    f.suivi = !f.filemaker_ok;
    await rafraichir();
    etat(`${f.nom} suit de nouveau la version de FileMaker.`);
  } catch (err) {
    bouton.disabled = false;
    etat("Modification refusée : " + err);
  }
}

/** Redessine, et retire la fiche de la vue si elle n'y a plus sa place. */
async function rafraichir() {
  if (vue === "retardataires") {
    await charger();
  } else {
    dessinerListe();
    dessinerDetail();
    await majCompteur();
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
  confirmation = false;
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
  $("#nom-compte").textContent = session.utilisateur;
  $("#nouvel-employe").hidden = !peutModifier();
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

/* La vérification dure quelques dixièmes de seconde ; ce plancher laisse
   à l'animation le temps d'être vue plutôt que de clignoter. */
const ATTENTE_MINIMALE = 1500;

const pause = (ms) => new Promise((suite) => setTimeout(suite, ms));

$("#formulaire").addEventListener("submit", async (evt) => {
  evt.preventDefault();
  const bouton = $(".bouton-hud");
  bouton.disabled = true;
  $("#erreur").hidden = true;
  montrerAttente(true);
  const debut = Date.now();
  try {
    const [compte] = await Promise.all([
      pywebview.api.connexion($("#utilisateur").value, $("#motdepasse").value),
      pause(ATTENTE_MINIMALE),
    ]);
    session = compte;
    if (session) {
      await ouvrirSession();
    } else {
      refuser("Identifiants incorrects. Accès au parc refusé.");
    }
  } catch (err) {
    await pause(Math.max(0, ATTENTE_MINIMALE - (Date.now() - debut)));
    refuser("Erreur de connexion : " + err);
  } finally {
    montrerAttente(false);
    bouton.disabled = false;
  }
});

/** Remplace le panneau de connexion par celui de la vérification. */
function montrerAttente(attente) {
  $("#formulaire").hidden = attente;
  $("#attente").hidden = !attente;
}

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
    fermerNouvelEmploye();
  }
});

$("#voir-compte").onclick = async () => {
  ouvrirMenu(false);
  try {
    const infos = await pywebview.api.compte();
    $("#compte-utilisateur").textContent = infos.utilisateur;
    $("#compte-role").textContent = infos.role_libelle;
    $("#compte-base").textContent = infos.base;
    $("#gestion-comptes").hidden = !peutGererComptes();
    messageComptes("");
    if (peutGererComptes()) await dessinerComptes();
    $("#fenetre-compte").hidden = false;
  } catch (err) {
    etat("Compte indisponible : " + err);
  }
};

/* ---------- gestion des comptes ---------- */

function messageComptes(texte, reussite = false) {
  const message = $("#message-comptes");
  message.textContent = texte;
  message.hidden = !texte;
  message.classList.toggle("ok", reussite);
}

async function dessinerComptes() {
  const comptes = await pywebview.api.lister_comptes();
  const moi = $("#compte-utilisateur").textContent;

  $("#tableau-comptes").innerHTML = comptes.map((c) => {
    const soi = c.utilisateur === moi;
    return `
      <tr data-compte="${echapper(c.utilisateur)}">
        <td>${echapper(c.utilisateur)}${soi ? " <small>(vous)</small>" : ""}</td>
        <td>
          <select class="saisie role" ${soi ? "disabled" : ""}>
            <option value="lecture" ${c.role === "lecture" ? "selected" : ""}>Lecture seule</option>
            <option value="modification" ${c.role === "modification" ? "selected" : ""}>Modification</option>
            <option value="administrateur" ${c.role === "administrateur" ? "selected" : ""}>Administrateur</option>
          </select>
        </td>
        <td><input class="saisie mdp" type="password" placeholder="Mot de passe"
                   autocomplete="new-password"></td>
        <td>${soi ? "" : `<button type="button" class="bouton-supprimer" title="Supprimer ce compte">×</button>`}</td>
      </tr>`;
  }).join("");

  $("#tableau-comptes").querySelectorAll("tr").forEach((tr) => {
    const nom = tr.dataset.compte;

    const role = tr.querySelector(".role");
    role.onchange = () => agir(
      () => pywebview.api.definir_role_compte(nom, role.value),
      `Rôle de ${nom} modifié.`);

    const mdp = tr.querySelector(".mdp");
    mdp.onkeydown = (evt) => {
      if (evt.key !== "Enter") return;
      evt.preventDefault();
      agir(() => pywebview.api.definir_motdepasse_compte(nom, mdp.value),
           `Mot de passe de ${nom} changé.`);
    };

    const supprimer = tr.querySelector(".bouton-supprimer");
    if (supprimer) {
      supprimer.onclick = () => agir(
        () => pywebview.api.supprimer_compte(nom), `Compte ${nom} supprimé.`);
    }
  });
}

/** Exécute une action sur les comptes, puis redessine la liste. */
async function agir(action, reussite) {
  try {
    await action();
    await dessinerComptes();
    messageComptes(reussite, true);
  } catch (err) {
    await dessinerComptes();
    messageComptes(String(err).replace(/^Error:\s*/, ""));
  }
}

$("#nouveau-compte").addEventListener("submit", async (evt) => {
  evt.preventDefault();
  const nom = $("#nouveau-nom").value;
  await agir(
    () => pywebview.api.creer_compte(nom, $("#nouveau-mdp").value, $("#nouveau-role").value),
    `Compte ${nom.trim()} créé.`);
  if (!$("#message-comptes").classList.contains("ok")) return;
  $("#nouveau-compte").reset();
});

/* ---------- nouvelle fiche ---------- */

function fermerNouvelEmploye() {
  $("#fenetre-employe").hidden = true;
  $("#formulaire-employe").reset();
  $("#message-employe").hidden = true;
}

$("#nouvel-employe").onclick = () => {
  ouvrirMenu(false);
  $("#message-employe").hidden = true;
  $("#fenetre-employe").hidden = false;
  $("#nom-employe").focus();
};

$("#annuler-employe").onclick = fermerNouvelEmploye;
$("#fenetre-employe").onclick = (evt) => {
  if (evt.target === $("#fenetre-employe")) fermerNouvelEmploye();
};

$("#formulaire-employe").addEventListener("submit", async (evt) => {
  evt.preventDefault();
  const nom = $("#nom-employe").value;
  try {
    const id = await pywebview.api.creer_employe(nom);
    fermerNouvelEmploye();
    $("#terme").value = "";
    vue = "tous";
    document.querySelectorAll(".onglet").forEach((b) =>
      b.classList.toggle("actif", b.dataset.vue === "tous"));
    choisie = id;                       // la nouvelle fiche s'ouvre aussitôt
    await charger();
    etat(`Fiche de ${nom.trim()} créée. Complétez-la avec « Modifier ».`);
  } catch (err) {
    const message = $("#message-employe");
    message.textContent = String(err).replace(/^Error:\s*/, "");
    message.hidden = false;
  }
});

$("#fermer-compte").onclick = () => { $("#fenetre-compte").hidden = true; };
$("#fenetre-compte").onclick = (evt) => {
  if (evt.target === $("#fenetre-compte")) $("#fenetre-compte").hidden = true;
};

$("#deconnexion").onclick = async () => {
  await pywebview.api.deconnexion();
  session = null;
  edition = null;
  confirmation = false;
  $("#nom-compte").textContent = "Compte";
  ouvrirMenu(false);
  $("#fenetre-compte").hidden = true;
  fermerNouvelEmploye();
  fiches = [];
  choisie = null;
  $("#appli").hidden = true;
  $("#connexion").hidden = false;
  $("#formulaire").reset();
  montrerAttente(false);
  $("#erreur").hidden = true;
  $("#utilisateur").focus();
};

/* ---------- thème ---------- */

const CROISSANT = `<svg viewBox="0 0 24 24" width="17" height="17" fill="currentColor"
  aria-hidden="true"><path d="M12.3 3a9 9 0 1 0 8.7 11.3A7.2 7.2 0 0 1 12.3 3Z"/></svg>`;

const SOLEIL = `<svg viewBox="0 0 24 24" width="17" height="17" fill="currentColor"
  aria-hidden="true"><path d="M12 7a5 5 0 1 0 0 10 5 5 0 0 0 0-10Zm0-6 1.6 3.2h-3.2L12 1Zm0
  22-1.6-3.2h3.2L12 23ZM1 12l3.2-1.6v3.2L1 12Zm22 0-3.2 1.6v-3.2L23 12ZM4.2 4.2l3.5.6-1.8
  1.8-1.7-2.4Zm15.6 15.6-3.5-.6 1.8-1.8 1.7 2.4ZM19.8 4.2l-1.7 2.4-1.8-1.8 3.5-.6ZM4.2
  19.8l1.7-2.4 1.8 1.8-3.5.6Z"/></svg>`;

const boutonTheme = $("#theme");

function appliquerTheme(sombre) {
  document.documentElement.classList.toggle("sombre", sombre);
  boutonTheme.innerHTML = sombre ? SOLEIL : CROISSANT;
  boutonTheme.title = sombre ? "Repasser en mode clair" : "Mode sombre";
  try {
    localStorage.setItem("santinel-theme", sombre ? "sombre" : "clair");
  } catch (err) {
    /* stockage indisponible : le choix vaut pour la session en cours */
  }
}

function themeAuDemarrage() {
  try {
    return localStorage.getItem("santinel-theme") !== "clair";   // sombre par défaut
  } catch (err) {
    return true;
  }
}

boutonTheme.onclick = () =>
  appliquerTheme(!document.documentElement.classList.contains("sombre"));

appliquerTheme(themeAuDemarrage());

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
