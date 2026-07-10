# FMOD Studio 1.10 Test Project Guide (from feature dump)

Your banks expose **143 unique reconstruction features**. Build this exact small project in **FMOD Studio 1.10.x**, then use the saved `Metadata/*.xml` as decompiler reference.

> UI labels match Studio **1.10**. Do **not** use 2.x-only features (action sheets, magnet regions, FMOD Panner effect module, Sequential Global/Local playlist scopes).

---

## Master name list

Use these exact names. Rename after creating if Studio auto-names differently.

### Project / folders / bank

| Item | Exact name |
|---|---|
| Project | `FeatureCoverage` |
| Event folder | `Coverage` |
| Bank | `Master Bank` (default is fine) |

### WAV assets (import, then rename in Assets browser)

| Exact asset name | Source | Stream button | Used by |
|---|---|---|---|
| `wav_memory_a` | Short mono or stereo click/one-shot (~0.5–2s) | **Off** | `evt_core`, `evt_multi`, `evt_timeline`, `evt_param` |
| `wav_memory_b` | Different short one-shot | **Off** | `evt_multi` playlist entry 2 |
| `wav_memory_c` | Different short one-shot | **Off** | `evt_multi` playlist entry 3 |
| `wav_child` | Short one-shot | **Off** | `evt_child` |
| `wav_stream_loop` | Longer bed/loop (~8–15s) | **On** | `evt_stream` |

Any quiet/test tones are fine — names matter more than content.

### Events

| Exact event name | Type | Purpose |
|---|---|---|
| `evt_core` | 3D Event | Macros, single instrument, automation, AHDSR, Random |
| `evt_stream` | 2D Event | Streamed waveform |
| `evt_multi` | 2D Event | Multi Instrument Shuffle playlist |
| `evt_param` | 3D Event | Game parameter sheet + Distance + Auto Pitch |
| `evt_timeline` | 2D Event | Markers, transition region, sync instrument |
| `evt_child` | 2D Event | Nested target for Event Instrument |
| `evt_nested` | 2D Event | Event Instrument → `evt_child` |
| `evt_snapshot_ref` | 2D Event | Snapshot Instrument → `snap_blend_sfx` |

### Tracks inside events (rename track heads)

| Event | Exact track name |
|---|---|
| `evt_core` | `trk_core_audio` |
| `evt_stream` | `trk_stream_audio` |
| `evt_multi` | `trk_multi_audio` |
| `evt_param` | `trk_param_audio` |
| `evt_timeline` | `trk_timeline_audio` |
| `evt_child` | `trk_child_audio` |
| `evt_nested` | `trk_nested_audio` |
| `evt_snapshot_ref` | `trk_snapshot_audio` |

### Instruments (what to place)

| Exact placement | Instrument type | Content |
|---|---|---|
| `evt_core` / `trk_core_audio` | Single Instrument | `wav_memory_a` |
| `evt_stream` / `trk_stream_audio` | Single Instrument | `wav_stream_loop` |
| `evt_multi` / `trk_multi_audio` | Multi Instrument | playlist: `wav_memory_a`, `wav_memory_b`, `wav_memory_c` |
| `evt_param` / `trk_param_audio` on **param sheet** | Single Instrument | `wav_memory_a` |
| `evt_timeline` / `trk_timeline_audio` | Single Instrument (Async **off**) | `wav_memory_a` |
| `evt_child` / `trk_child_audio` | Single Instrument | `wav_child` |
| `evt_nested` / `trk_nested_audio` | Event Instrument | references `evt_child` |
| `evt_snapshot_ref` / `trk_snapshot_audio` | Snapshot Instrument | references `snap_blend_sfx`, Intensity ≠ 0 |

### Markers / transitions (`evt_timeline` only)

| Exact name | Type |
|---|---|
| `mark_dest_a` | Destination Marker |
| `trans_to_a` | Transition Region → destination `mark_dest_a` |

### Parameters

| Exact name | Type | Where |
|---|---|---|
| `param_intensity` | Game-controlled, range **0 → 1**, default **0** | Add to `evt_param`, `evt_timeline`, and `evt_core` |
| `Distance` | Automatic Distance (built-in) | Add on `evt_param` (3D) |

### Mixer

| Exact name | Type |
|---|---|
| `bus_sfx` | Group Bus |
| `bus_reverb_return` | Return Bus (rename default Reverb return if present) |
| `vca_sfx` | VCA (assign `bus_sfx`) |

### Snapshots

| Exact name | Type |
|---|---|
| `snap_group_mix` | Snapshot group folder |
| `snap_blend_sfx` | **Blending** Snapshot inside `snap_group_mix` |

### Effects (leave Studio effect type names; place as follows)

| Exact bus / strip | Effect to add | Pre or post fader |
|---|---|---|
| `bus_sfx` | **Compressor** | Pre |
| `bus_sfx` | **Three EQ** | Pre |
| `bus_sfx` | **Multiband EQ** | Post |
| `bus_sfx` | **Channel Mix** | Post |
| `bus_sfx` | **Send** → `bus_reverb_return` | Pre (send level ≠ 0) |
| `bus_reverb_return` | **SFX Reverb** | Pre |
| `evt_core` Master track | **Chorus** | Pre |
| `evt_core` Master track | **Flange** | Pre |
| `evt_core` Master track | **Distortion** | Post |
| `evt_core` Master track | **Tremolo** | Post |
| `evt_core` Master track | **Pitch Shifter** | Post |
| `evt_param` Master track | **3D Panner** (keep/add if creating 3D event) | — |

Built-in strip **Fader** and bus/event **Panner** stay; do not add a separate “Fader” effect.

---

## Setup

1. **File → New Project…** → `FeatureCoverage`.
2. Events browser → create folder `Coverage`.
3. Assets browser → import 5 WAVs → rename exactly to:
   - `wav_memory_a`
   - `wav_memory_b`
   - `wav_memory_c`
   - `wav_child`
   - `wav_stream_loop`
4. Select `wav_stream_loop` → turn **Stream** **On**. Leave the other four **Stream Off**.
5. After each section: **File → Save Project**, then inspect:
   - `Metadata/AudioFile/`
   - `Metadata/Event/`
   - `Metadata/Mixer*` / bus metadata
   - `Metadata/Snapshot/`
   - `Metadata/Parameter/`

---

## 1. Mixer first (`bus_sfx`, return, VCA, effects)

**Window → Mixer**

1. Create group bus → rename **`bus_sfx`**.
2. Create/rename return → **`bus_reverb_return`**.
3. On `bus_sfx`: set Volume ≠ 0 dB if needed, set **Pitch** ≠ 0.
4. On `bus_sfx` signal chain, add exactly:
   - Pre-fader: **Compressor**, **Three EQ**, **Send** → `bus_reverb_return` (level ≠ 0)
   - Post-fader: **Multiband EQ**, **Channel Mix**
5. On `bus_reverb_return`: add **SFX Reverb** (pre-fader).
6. VCA browser tab → new VCA → rename **`vca_sfx`** → assign `bus_sfx`.
7. Optionally mute once then unmute on `bus_sfx` if you need a bus flag in XML (`Bus.Flags`).

---

## 2. `evt_core` (macros + single instrument + modulators)

1. In `Coverage` → **New 3D Event** → rename **`evt_core`**.
2. Rename audio track → **`trk_core_audio`**.
3. Drag **`wav_memory_a`** onto `trk_core_audio` (Single Instrument).
4. Route event output to **`bus_sfx`**.
5. Click empty editor space → **Event Macros** deck:
   - **Max Instances** = `4`
   - **Stealing** = `Quietest`
   - **Priority** = `Highest`
   - **Cooldown** = `0.25` (or any ≠ 0)
   - Enable **Doppler**, set **Doppler Scale** = `1.5`
   - Enable **Persistent**
6. Select the instrument:
   - **Volume** = `-2` dB
   - **Pitch** = `2` st
   - **Start Offset** = `10` (%)
   - Enable **Loop**
   - Trigger behavior → **Probability** On = `80` (%)
7. Master track effects on `evt_core` (exact set from name list):
   - Pre: **Chorus**, **Flange**
   - Post: **Distortion**, **Tremolo**, **Pitch Shifter**
8. Right-click instrument **Volume** → **Add Automation** (Timeline) → add linear + curved points.
9. Add parameter **`param_intensity`** (0–1) to this event → automate Volume vs `param_intensity` too.
10. Right-click Volume → **Add Modulation → AHDSR**.
11. Right-click Volume → **Add Modulation → Random**.

Covers: Event shell/macros/flags/doppler, waveform memory instrument, automation/curves/modulators, several effect types.

---

## 3. `evt_stream`

1. **New Event** → **`evt_stream`**.
2. Track → **`trk_stream_audio`**.
3. Drag **`wav_stream_loop`** onto track.
4. Route to **`bus_sfx`**.

Covers: `WaveformLoadingMode_StreamFromDisk`.

---

## 4. `evt_multi`

1. **New Event** → **`evt_multi`**.
2. Track → **`trk_multi_audio`**.
3. Right-click track → **Add Multi Instrument**.
4. Playlist entries (in order):
   1. `wav_memory_a`
   2. `wav_memory_b`
   3. `wav_memory_c`
5. Playlist Selection Mode → **Shuffle**.
6. Route to **`bus_sfx`**.

Covers: Multi + `SmartRandom` (Shuffle) + playlist entries.

---

## 5. `evt_param` (parameter sheet + Distance + Auto Pitch)

1. **New 3D Event** → **`evt_param`**.
2. Track → **`trk_param_audio`**.
3. Click **+** beside Timeline → **New Parameter…** → name **`param_intensity`**, min `0`, max `1`.
4. Add automatic **`Distance`** parameter (3D builtins).
5. On the **`param_intensity`** sheet: place Single Instrument with **`wav_memory_a`**.
6. Select that instrument → right-click **Pitch** → **Add Modulation → Auto Pitch**.
7. Keep/ensure **3D Panner** on Master.
8. Route to **`bus_sfx`**.

Covers: ParameterLayout, game parameter, Distance, AutoPitch, PAN/3D panner.

---

## 6. `evt_timeline` (markers + transition)

1. **New Event** → **`evt_timeline`**.
2. Track → **`trk_timeline_audio`**.
3. Place Single Instrument with **`wav_memory_a`** spanning part of the timeline.
4. Select instrument → set **Async** **Off** (time-locked / synchronous).
5. On marker track:
   - Add Destination Marker → rename **`mark_dest_a`** (place later on timeline).
   - Add Transition Region → rename **`trans_to_a`**, set destination to **`mark_dest_a`**, give it non-zero length.
6. Add/ensure **`param_intensity`** on this event.
7. Select `trans_to_a` → deck → **Add Condition** → **`param_intensity`** (any mid range, e.g. 0.5–1.0).
8. Route to **`bus_sfx`**.

Covers: NamedMarkers, TransitionRegion, Destination, StartEnd, LegacyParameterConditions, TimeLockedTriggerBoxes.

---

## 7. `evt_child` + `evt_nested`

1. **New Event** → **`evt_child`**.
2. Track → **`trk_child_audio`** → asset **`wav_child`**.
3. Route to **`bus_sfx`**.
4. **New Event** → **`evt_nested`**.
5. Track → **`trk_nested_audio`**.
6. Right-click → **Add Event Instrument** → choose **`evt_child`**.
7. Route `evt_nested` to **`bus_sfx`**.

Covers: EventInstrument / EventReference.

---

## 8. Snapshot + `evt_snapshot_ref`

1. Mixer → Snapshots tab → create group folder **`snap_group_mix`**.
2. Inside it: **New Blending Snapshot** → **`snap_blend_sfx`**.
3. Scope `bus_sfx` Volume (and optionally SFX Reverb wet) into the snapshot; set values different from base mix.
4. With snapshot selected, set **Intensity** = `0.75`.
5. Ensure group resolution behavior is **Average** if exposed for the group.
6. **New Event** → **`evt_snapshot_ref`**.
7. Track → **`trk_snapshot_audio`**.
8. Add **Snapshot Instrument** → target **`snap_blend_sfx`** → Intensity = `0.5`.
9. Route to **`bus_sfx`**.

Covers: Snapshot blending/entries/intensity/priority/group average + Event.SnapshotReference + SnapshotIntensity.

---

## Build order checklist

Do in this order:

1. Assets: `wav_memory_a`, `wav_memory_b`, `wav_memory_c`, `wav_child`, `wav_stream_loop` (+ Stream on last)
2. Mixer: `bus_sfx`, `bus_reverb_return`, `vca_sfx`, effects + send
3. `evt_core`
4. `evt_stream`
5. `evt_multi`
6. `evt_param`
7. `evt_timeline`
8. `evt_child` → `evt_nested`
9. `snap_group_mix` / `snap_blend_sfx` → `evt_snapshot_ref`
10. Save → optionally Build banks → re-run feature dump

---

## After you build

1. **File → Save Project**.
2. Optionally **File → Build…**, point BANK2FSPRO at the banks, re-run the feature dump.
3. Prefer this project’s **Metadata XML** as golden reference.
4. Keep XML snippets per family next to these exact names.

## Feature dump source

Produced by `CollectedBank.Debug()` during `Decompiler.Decompile()` from:

`C:\Users\Computery\Desktop\StreamingAssets\Master Bank*.bank`
