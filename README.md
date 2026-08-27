# velvet-cat-jazz

Générateur de jazz lofi « vintage noir » — le genre de nappe qu'on trouve sur les
streams YouTube 24/7. Tout est **synthétisé** en Python/numpy, aucun échantillon,
aucune boucle préenregistrée : chaque rendu est une prise différente.

```bash
pip install numpy
python3 lofi_jazz.py --minutes 5 --out velvet_cat.wav
```

## Options

| Option | Défaut | Rôle |
| --- | --- | --- |
| `--minutes` | `3` | durée du morceau |
| `--bpm` | `72` | tempo (60–90 conseillé) |
| `--seed` | aléatoire | rejouer exactement le même morceau |
| `--no-lead` | — | supprime la trompette solo (pur fond de travail) |
| `--out` | `velvet_cat_jazz.wav` | fichier WAV 44,1 kHz stéréo 16 bits |

## Ce qu'il y a dedans

**Instruments** (synthèse pure) :

- *Rhodes* — FM à deux opérateurs (ratio 1 pour le corps, partielle 14 pour la cloche),
  index de modulation qui décroît vite pour l'attaque en « cloche ».
- *Contrebasse* — sinus + harmoniques 2 et 3, léger glissando d'attaque, bruit de doigt filtré.
- *Batterie aux balais* — bruit filtré en vague (frottement), frappe sur 2 et 4, ride swingué,
  grosse caisse à balayage de fréquence.
- *Trompette bouchée* — harmoniques impaires, vibrato retardé, souffle en bande passante.

**Harmonie** : trois grilles de 8 mesures en ré mineur (Dm9 – Gm11 – B♭maj7♯11 – A7♭9, etc.),
voicings sans fondamentale tirés au sort, walking bass avec approche chromatique vers l'accord
suivant, solo construit sur le mode de chaque accord (dorien / lydien / altéré) avec des silences.

**Chaîne lofi** : réverbération par convolution (RI synthétique), *wow & flutter* de bande
(rééchantillonnage modulé), passe-bas 7,2 kHz, saturation `tanh`, souffle et craquements de
vinyle, fondus d'entrée/sortie.

## Notes

Le rendu est hors-ligne (≈ 10 s de calcul par minute de musique). Pour un stream continu,
enchaînez plusieurs rendus avec des seeds différentes.
