#!/usr/bin/env python3
"""
lofi_jazz.py - generateur de jazz "vintage noir" facon stream YouTube 24/7.

Tout est synthetise a la main avec numpy : piano Rhodes (FM), contrebasse,
batterie aux balais, trompette bouchee, plus la couche "lofi" (bruit de vinyle,
wow/flutter de bande, filtre passe-bas, saturation, reverb).

    python3 lofi_jazz.py --minutes 5 --out velvet_cat.wav
"""

import argparse
import wave

import numpy as np

SR = 44100
TWO_PI = 2.0 * np.pi


# ---------------------------------------------------------------- DSP helpers

def _butter_mag(freqs, cutoff, order, kind):
    """Butterworth magnitude response (zero-phase, applique en frequentiel)."""
    f = np.maximum(freqs, 1e-6)
    if kind == "low":
        return 1.0 / np.sqrt(1.0 + (f / cutoff) ** (2 * order))
    return 1.0 / np.sqrt(1.0 + (cutoff / f) ** (2 * order))


def spectral_filter(x, lo=None, hi=None, order=2):
    """Passe-bas / passe-haut / passe-bande sans boucle Python."""
    n = len(x)
    if n < 8:
        return x
    spec = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(n, 1.0 / SR)
    mask = np.ones_like(freqs)
    if hi is not None:
        mask *= _butter_mag(freqs, hi, order, "low")
    if lo is not None:
        mask *= _butter_mag(freqs, lo, order, "high")
    return np.fft.irfft(spec * mask, n)


def fft_convolve(x, h):
    """Convolution overlap-add (utilisee pour la reverb)."""
    n, m = len(x), len(h)
    block = 1 << 15
    size = 1 << (block + m - 1).bit_length()
    H = np.fft.rfft(h, size)
    out = np.zeros(n + m - 1)
    for i in range(0, n, block):
        seg = np.fft.irfft(np.fft.rfft(x[i:i + block], size) * H, size)
        end = min(i + len(seg), len(out))
        out[i:end] += seg[:end - i]
    return out[:n]


def make_reverb_ir(rng, seconds=2.2, decay=4.0, damp=4200.0):
    """RI synthetique : bruit decroissant + amortissement des aigus."""
    n = int(seconds * SR)
    t = np.arange(n) / SR
    ir = rng.standard_normal(n) * np.exp(-decay * t)
    ir[: int(0.012 * SR)] *= np.linspace(0, 1, int(0.012 * SR))  # pre-delay doux
    ir = spectral_filter(ir, lo=90.0, hi=damp, order=2)
    return ir / (np.sqrt(np.sum(ir ** 2)) + 1e-9)


def tape_wow(x, depth_ms=2.2, wow_hz=0.55, flutter_ms=0.35, flutter_hz=6.7):
    """Instabilite de vitesse d'une vieille bande / d'un 33 tours fatigue."""
    n = len(x)
    t = np.arange(n) / SR
    mod = (depth_ms * 1e-3 * SR) * np.sin(TWO_PI * wow_hz * t)
    mod += (flutter_ms * 1e-3 * SR) * np.sin(TWO_PI * flutter_hz * t + 1.1)
    idx = np.clip(np.arange(n) + mod, 0, n - 1)
    return np.interp(idx, np.arange(n), x)


def saturate(x, drive=1.6):
    return np.tanh(x * drive) / np.tanh(drive)


def adsr(n, a=0.005, d=0.08, s=0.6, r=0.25):
    """Enveloppe ADSR echantillonnee sur n echantillons."""
    a_n, d_n, r_n = (max(1, int(v * SR)) for v in (a, d, r))
    sus_n = max(1, n - a_n - d_n - r_n)
    env = np.concatenate([
        np.linspace(0.0, 1.0, a_n),
        np.linspace(1.0, s, d_n),
        np.full(sus_n, s),
        np.linspace(s, 0.0, r_n),
    ])
    return env[:n] if len(env) >= n else np.pad(env, (0, n - len(env)))


def midi_hz(note):
    return 440.0 * 2.0 ** ((note - 69) / 12.0)


# ------------------------------------------------------------------- voix

def rhodes(note, dur, amp, rng):
    """Piano electrique : FM ratio 1 (corps) + partielle 14 (cloche)."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    f = midi_hz(note) * (1.0 + rng.normal(0, 0.0012))
    body_env = np.exp(-t * 2.1)
    idx_env = np.exp(-t * 9.0) * (2.4 + 1.6 * amp)
    y = np.sin(TWO_PI * f * t + idx_env * np.sin(TWO_PI * f * t)) * body_env
    y += 0.14 * np.sin(TWO_PI * f * 14.0 * t) * np.exp(-t * 22.0)
    y *= adsr(n, a=0.004, d=0.5, s=0.35, r=min(0.4, dur * 0.5))
    return y * amp


def upright_bass(note, dur, amp, rng):
    """Contrebasse : attaque legerement desaccordee + corps sourd."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    f = midi_hz(note) * (1.0 + rng.normal(0, 0.0015))
    glide = f * (1.0 + 0.06 * np.exp(-t * 60.0))  # petit "pull" de corde
    ph = TWO_PI * np.cumsum(glide) / SR
    y = np.sin(ph) + 0.32 * np.sin(2 * ph) + 0.10 * np.sin(3 * ph)
    y *= np.exp(-t * 2.6)
    click = rng.standard_normal(n) * np.exp(-t * 90.0) * 0.25
    y += spectral_filter(click, lo=400.0, hi=2600.0)
    y *= adsr(n, a=0.006, d=0.25, s=0.45, r=min(0.25, dur * 0.5))
    return spectral_filter(y, hi=2200.0, order=2) * amp


def muted_trumpet(note, dur, amp, rng):
    """Lead sourdine : harmoniques impaires + vibrato tardif."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    f = midi_hz(note) * (1.0 + rng.normal(0, 0.001))
    vib = 1.0 + 0.004 * np.sin(TWO_PI * 5.2 * t) * np.clip((t - 0.18) * 4, 0, 1)
    ph = TWO_PI * np.cumsum(f * vib) / SR
    y = sum(a * np.sin(k * ph) for k, a in
            ((1, 1.0), (2, 0.45), (3, 0.30), (4, 0.12), (5, 0.08)))
    breath = spectral_filter(rng.standard_normal(n), lo=1500.0, hi=5000.0) * 0.05
    y = (y + breath) * adsr(n, a=0.045, d=0.15, s=0.72, r=min(0.35, dur * 0.6))
    return spectral_filter(y, hi=3800.0, order=2) * amp


def brush_sweep(dur, amp, rng):
    """Balai qui frotte la caisse claire : bruit filtre en vague."""
    n = int(dur * SR)
    t = np.linspace(0, np.pi, n)
    y = rng.standard_normal(n) * (np.sin(t) ** 2)
    return spectral_filter(y, lo=900.0, hi=5200.0, order=2) * amp


def brush_hit(amp, rng):
    n = int(0.22 * SR)
    t = np.arange(n) / SR
    y = rng.standard_normal(n) * np.exp(-t * 16.0)
    y = spectral_filter(y, lo=1400.0, hi=6000.0, order=2)
    y += 0.25 * np.sin(TWO_PI * 190.0 * t) * np.exp(-t * 24.0)
    return y * amp


def kick(amp):
    n = int(0.35 * SR)
    t = np.arange(n) / SR
    f = 48.0 + 55.0 * np.exp(-t * 28.0)
    y = np.sin(TWO_PI * np.cumsum(f) / SR) * np.exp(-t * 8.0)
    return y * amp


def ride_tick(amp, rng):
    n = int(0.18 * SR)
    t = np.arange(n) / SR
    y = rng.standard_normal(n) * np.exp(-t * 26.0)
    return spectral_filter(y, lo=4500.0, hi=11000.0, order=2) * amp


def vinyl_noise(n, rng, amp=0.035):
    """Souffle + craquements aleatoires facon 78 tours."""
    hiss = spectral_filter(rng.standard_normal(n), lo=300.0, hi=5200.0) * 0.35
    crackle = np.zeros(n)
    n_pops = int(n / SR * 55)
    pos = rng.integers(0, n - 400, size=n_pops)
    t = np.arange(400) / SR
    for p in pos:
        crackle[p:p + 400] += (rng.standard_normal(400)
                               * np.exp(-t * 900.0)
                               * rng.uniform(0.4, 1.8))
    crackle = spectral_filter(crackle, lo=800.0, hi=8000.0)
    return (hiss + crackle) * amp


# ------------------------------------------------------------- harmonie

QUALITIES = {
    "m9":       [0, 3, 7, 10, 14],
    "m11":      [0, 3, 7, 10, 17],
    "maj7s11":  [0, 4, 7, 11, 18],
    "7b9":      [0, 4, 7, 10, 13],
    "m7b5":     [0, 3, 6, 10, 14],
    "maj9":     [0, 4, 7, 11, 14],
    "13":       [0, 4, 9, 10, 14],
}

SCALES = {
    "m9":      [0, 2, 3, 5, 7, 9, 10],    # dorien
    "m11":     [0, 2, 3, 5, 7, 9, 10],
    "maj7s11": [0, 2, 4, 6, 7, 9, 11],    # lydien
    "7b9":     [0, 1, 4, 5, 7, 8, 10],    # alteree "espagnole"
    "m7b5":    [0, 2, 3, 5, 6, 8, 10],    # locrien #2
    "maj9":    [0, 2, 4, 5, 7, 9, 11],
    "13":      [0, 2, 4, 5, 7, 9, 10],    # mixolydien
}

# Boucles de 8 mesures en re mineur : ambiance polar / club enfume.
PROGRESSIONS = [
    [(62, "m9"), (62, "m9"), (55, "m11"), (55, "m11"),
     (58, "maj7s11"), (58, "maj7s11"), (57, "7b9"), (57, "7b9")],
    [(62, "m9"), (62, "m9"), (64, "m7b5"), (57, "7b9"),
     (62, "m9"), (67, "m11"), (60, "maj9"), (57, "7b9")],
    [(65, "maj9"), (65, "maj9"), (62, "m9"), (55, "13"),
     (58, "maj7s11"), (58, "maj7s11"), (64, "m7b5"), (57, "7b9")],
]


def voice_chord(root, quality, rng, lo=57, hi=79):
    """Voicing sans fondamentale, ramene dans le registre main droite."""
    tones = QUALITIES[quality][1:]
    notes = []
    for iv in tones:
        n = root + iv
        while n < lo:
            n += 12
        while n > hi:
            n -= 12
        notes.append(n)
    notes = sorted(set(notes))
    if len(notes) > 4 and rng.random() < 0.5:
        notes.pop(rng.integers(0, len(notes)))
    return notes


def walking_bass(root, quality, next_root, rng):
    """Quatre noires : fondamentale, notes de l'accord, approche chromatique."""
    base = root - 24
    while base < 33:
        base += 12
    tones = [base + iv for iv in QUALITIES[quality][:4]]
    beats = [base]
    beats.append(tones[rng.integers(1, len(tones))])
    beats.append(tones[rng.integers(1, len(tones))])
    target = next_root - 24
    while target < 33:
        target += 12
    beats.append(target + rng.choice([-1, 1]))
    return beats


# ------------------------------------------------------------- arrangement

class Track:
    """Buffer mono + placement d'evenements a une position en secondes."""

    def __init__(self, n):
        self.buf = np.zeros(n)

    def add(self, when, sig):
        i = int(when * SR)
        if i >= len(self.buf) or i < 0:
            return
        end = min(i + len(sig), len(self.buf))
        self.buf[i:end] += sig[:end - i]


def render(minutes, bpm, seed, lead=True):
    rng = np.random.default_rng(seed)
    beat = 60.0 / bpm
    bar = 4 * beat
    swing = 0.62                       # placement du contretemps
    total = int(minutes * 60 * SR) + int(3 * SR)

    keys = Track(total)
    bass = Track(total)
    drums = Track(total)
    horn = Track(total)

    prog = PROGRESSIONS[0]
    n_bars = int((minutes * 60) / bar) + 2

    for b in range(n_bars):
        if b % 8 == 0:                                  # nouvelle boucle
            prog = PROGRESSIONS[rng.integers(0, len(PROGRESSIONS))] \
                if b else PROGRESSIONS[0]
        root, quality = prog[b % 8]
        next_root = prog[(b + 1) % 8][0]
        t0 = b * bar
        intensity = 0.75 + 0.25 * np.sin(TWO_PI * b / 32.0)

        # --- Rhodes : accord pose + relances syncopees
        chord = voice_chord(root, quality, rng)
        for k, note in enumerate(chord):
            keys.add(t0 + k * 0.012,
                     rhodes(note, bar * 0.9, 0.20 * intensity, rng))
        if rng.random() < 0.6:
            hit = t0 + (2 + swing) * beat
            for note in voice_chord(root, quality, rng):
                keys.add(hit, rhodes(note, beat * 1.4, 0.12 * intensity, rng))
        if rng.random() < 0.35:                          # arpege qui s'echappe
            for k, note in enumerate(sorted(chord)[::-1]):
                keys.add(t0 + 3 * beat + k * beat * 0.16,
                         rhodes(note + 12, beat, 0.09, rng))

        # --- Contrebasse
        for k, note in enumerate(walking_bass(root, quality, next_root, rng)):
            bass.add(t0 + k * beat,
                     upright_bass(note, beat * 0.95, 0.34, rng))

        # --- Batterie aux balais
        for k in range(4):
            drums.add(t0 + k * beat, brush_sweep(beat * 0.9, 0.035, rng))
            drums.add(t0 + k * beat, ride_tick(0.05, rng))
            if rng.random() < 0.75:                      # contretemps swingue
                drums.add(t0 + (k + swing) * beat, ride_tick(0.028, rng))
        drums.add(t0 + beat, brush_hit(0.085, rng))
        drums.add(t0 + 3 * beat, brush_hit(0.085, rng))
        drums.add(t0, kick(0.28))
        if rng.random() < 0.45:
            drums.add(t0 + (2 + swing) * beat, kick(0.16))

        # --- Lead : phrases eparses, silences assumes
        if lead and rng.random() < 0.45:
            scale = SCALES[quality]
            deg = rng.integers(0, len(scale))
            pos = rng.choice([0.0, 1.0, 2.0]) * beat
            for _ in range(rng.integers(3, 7)):
                if pos > bar - beat * 0.5:
                    break
                note = root + 12 + scale[deg % len(scale)] + 12 * (deg // len(scale))
                dur = beat * rng.choice([0.5, 0.5, 1.0, 1.5])
                horn.add(t0 + pos, muted_trumpet(note, dur * 0.95, 0.14, rng))
                pos += dur if rng.random() < 0.8 else dur + beat * 0.5
                deg += rng.choice([-2, -1, -1, 1, 1, 2])
                deg = int(np.clip(deg, 0, len(scale) + 2))

    return keys.buf, bass.buf, drums.buf, horn.buf, total


def mix(stems, total, rng, wet=0.28):
    keys, bass, drums, horn = stems

    def pan(sig, p):                        # p = -1 (G) .. +1 (D)
        l = np.sqrt((1 - p) / 2)
        r = np.sqrt((1 + p) / 2)
        return sig * l, sig * r

    left = np.zeros(total)
    right = np.zeros(total)
    for sig, p in ((keys, -0.28), (bass, 0.0), (drums, 0.22), (horn, 0.12)):
        l, r = pan(sig, p)
        left += l
        right += r

    ir = make_reverb_ir(rng)
    left = (1 - wet) * left + wet * 1.6 * fft_convolve(left, ir)
    right = (1 - wet) * right + wet * 1.6 * fft_convolve(right, ir[::-1] * 0.9)

    out = []
    for ch, drift in ((left, 0.0), (right, 0.7)):
        ch = tape_wow(ch, wow_hz=0.55 + drift * 0.06)
        ch = spectral_filter(ch, lo=55.0, hi=7200.0, order=3)   # chaleur lofi
        ch = saturate(ch, drive=1.7)
        ch += vinyl_noise(total, rng)
        out.append(ch)

    stereo = np.stack(out, axis=1)
    fade = int(2.0 * SR)
    stereo[:fade] *= np.linspace(0, 1, fade)[:, None]
    stereo[-fade:] *= np.linspace(1, 0, fade)[:, None]
    stereo /= max(np.max(np.abs(stereo)), 1e-9)
    return stereo * 0.89                                        # ~ -1 dBFS


def write_wav(path, stereo):
    data = (np.clip(stereo, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as f:
        f.setnchannels(2)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(data.tobytes())


def main():
    ap = argparse.ArgumentParser(description="Generateur de jazz lofi 'vintage noir'.")
    ap.add_argument("--minutes", type=float, default=3.0, help="duree en minutes")
    ap.add_argument("--bpm", type=float, default=72.0, help="tempo (60-90 conseille)")
    ap.add_argument("--seed", type=int, default=None, help="graine aleatoire")
    ap.add_argument("--no-lead", action="store_true", help="sans trompette solo")
    ap.add_argument("--out", default="velvet_cat_jazz.wav")
    args = ap.parse_args()

    seed = args.seed if args.seed is not None else int(np.random.SeedSequence().entropy % 10**6)
    print(f"seed={seed} bpm={args.bpm} duree={args.minutes} min")

    stems = render(args.minutes, args.bpm, seed, lead=not args.no_lead)
    *tracks, total = stems
    stereo = mix(tracks, total, np.random.default_rng(seed + 1))
    write_wav(args.out, stereo)
    print(f"ecrit : {args.out} ({len(stereo) / SR:.1f} s)")


if __name__ == "__main__":
    main()
