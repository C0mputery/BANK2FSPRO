# FMOD Studio 1.10 Test Project Guide

Build one small “kitchen sink” project in **FMOD Studio 1.10** that covers every category `CollectedBank.Debug()` can emit, then open `Metadata/` to see the XML shapes.

This matches your bank parser’s **legacy DSP layout** (`EDSPTypeLegacy` / bank file version around `0x65`). Do **not** author the reference project in Studio 2.x — effect menus, XML `serializationModel`, and some modulators differ.

## Workflow

1. Create a **new project in FMOD Studio 1.10** (not 2.00+).
2. Import a few short WAVs (mono + stereo) via the **Assets** browser.
3. Build features in the order below (mixer → parameters → events → snapshots → banks).
4. Save, then inspect XML under `Metadata/` (Event, AudioFile, Bank, Mixer, SnapshotGroup, etc.).
5. Note the real `serializationModel` on the root `<objects>` element — use that as ground truth for BANK2FSPRO (the stub currently hardcodes `Studio.02.02.00`, which is a 2.x string).
6. **File → Build…** → run BANK2FSPRO → compare the feature dump to this checklist.

Use **one event per major feature family** so XML stays readable (`evt_timeline`, `evt_multi`, `evt_scatterer`, …).

---

## 1. Mixer / buses (`Bus.*`)

Open **Window → Mixer**. Use the **routing browser** and the **deck**.

| Studio action | Features you hit |
|---|---|
| Keep the default **Master Bus** | `Bus.MasterBusNode`, `Bus.BusBody`, `Bus.Routable` |
| Routing browser → **New Group** (e.g. SFX, Music) | `Bus.GroupBusNode` |
| Use the default **Reverb** return, or add another return | `Bus.ReturnBusNode` |
| Route events/groups; set volume/pitch ≠ default | `Bus.MixerStrip.Volume`, `Bus.MixerStrip.Pitch` |
| **VCAs** → **New VCA**; assign buses (**Assign To VCA** / drag) | `Bus.MixerStrip.VCAs`, `VCA`, `VCA.Strips` |
| On a bus deck: effects **left of** and **right of** the fader | `Bus.PreFaderEffects`, `Bus.PostFaderEffects` |
| Bus / event **max instances** + **stealing** | `Bus.MaximumPolyphony`, `Bus.PolyphonyLimitBehavior=…` |
| Channel-format effects (Channel Mix, Spatializer, Object Pan / Object Spatializer, Convolution Reverb) | `Bus.InputChannelLayout`, related layout / object-pan fields |

**Sends:** deck → **Add Send** / **Insert Send** → target a return → set send volume. Left of fader = pre-fader; right = post-fader.

> **Ports:** if your 1.10 build has **New Port** in the routing browser, add one and set its type for `Bus.OutputPortNode` / `Bus.PortType=…`. If the menu is missing, skip — that feature may be later than your build.

---

## 2. Effects (`Effect.*`) — critical for 1.10 decompile

Put effects on buses **and** event tracks. Deck empty space → **Add Effect** / **Insert Effect**.

### Add every built-in you can place — no substitutions

The bank stores a concrete `DSPType` on `BuiltInEffectNode`. In 1.10 that uses the **legacy** enum (`EDSPTypeLegacy`). Multiband EQ is **not** a stand-in for Lowpass: different type IDs, different XML.

In 1.10, Lowpass / Highpass / Echo / etc. are still in the UI (API docs may mark some “deprecated,” but they still author and still build into banks). **Add them all** so your decompiler sees real XML for each `Effect.BuiltIn.DSPType=…`.

| Add this in Studio 1.10 | Legacy `EDSPTypeLegacy` |
|---|---|
| **Lowpass** | `FMOD_DSP_TYPE_LOWPASS` |
| **Lowpass Simple** | `FMOD_DSP_TYPE_LOWPASS_SIMPLE` |
| **Highpass** | `FMOD_DSP_TYPE_HIGHPASS` |
| **Highpass Simple** | `FMOD_DSP_TYPE_HIGHPASS_SIMPLE` |
| **Parametric EQ** / **EQ** | `FMOD_DSP_TYPE_PARAMEQ` |
| **3-EQ** | `FMOD_DSP_TYPE_THREE_EQ` |
| **Multiband EQ** | `FMOD_DSP_TYPE_MULTIBAND_EQ` |
| **Echo** | `FMOD_DSP_TYPE_ECHO` |
| **Delay** | `FMOD_DSP_TYPE_DELAY` |
| **Flange** / **Flanger** | `FMOD_DSP_TYPE_FLANGE` |
| **Distortion** | `FMOD_DSP_TYPE_DISTORTION` |
| **Normalize** | `FMOD_DSP_TYPE_NORMALIZE` |
| **Limiter** | `FMOD_DSP_TYPE_LIMITER` |
| **Pitch Shifter** / **Pitch Shift** | `FMOD_DSP_TYPE_PITCHSHIFT` |
| **Chorus** | `FMOD_DSP_TYPE_CHORUS` |
| **Tremolo** | `FMOD_DSP_TYPE_TREMOLO` |
| **Compressor** | `FMOD_DSP_TYPE_COMPRESSOR` |
| **SFX Reverb** / **Reverb** | `FMOD_DSP_TYPE_SFXREVERB` |
| **Convolution Reverb** | `FMOD_DSP_TYPE_CONVOLUTIONREVERB` |
| **Channel Mix** | `FMOD_DSP_TYPE_CHANNELMIX` |
| **Panner** / pan | `FMOD_DSP_TYPE_PAN` |
| **Spatializer** | (spatializer; related to pan/3D path) |
| **Object Pan** / **Object Spatializer** | `FMOD_DSP_TYPE_OBJECTPAN` |
| **Transceiver** | `FMOD_DSP_TYPE_TRANSCEIVER` |
| **FFT** / spectrum analyzer (if listed) | `FMOD_DSP_TYPE_FFT` |
| **Loudness Meter** (if listed) | `FMOD_DSP_TYPE_LOUDNESS_METER` |
| **Envelope Follower** (if listed) | `FMOD_DSP_TYPE_ENVELOPEFOLLOWER` |
| **Send** | `FMOD_DSP_TYPE_SEND` (+ `SendEffectNode`) |

Exact menu labels can vary slightly by 1.10.x patch; match by DSP behavior, then confirm with the feature dump’s `Effect.BuiltIn.DSPType=…`.

Also hit non-default wet/dry/input gain where the effect exposes them → `Effect.EffectBody`, `Effect.WetMix`, `WetLevel`, `DryLevel`, `InputGain`, `Effect.Parameterized`, parameter buffers.

### Special effect nodes

| Studio action | Features |
|---|---|
| **Add Send** → return; set level | `Effect.SendEffectNode`, `ReturnGuid`, `SendLevel` |
| **Add Sidechain** + compressor sidechain input and/or **Add Modulation → Sidechain** on a property | `Effect.SideChainEffectNode`, Targets/Level; `Effect.Parameterized.SideChainEnabled` |
| **Add Effect → Plug-in…** (VST / etc.) | `Effect.PluginEffectNode`, `PluginName=…` |

### Not available in 1.10 (skip)

These are **2.03+** and will not appear in a 1.10 kitchen-sink project:

- Spectral Sidechain effect / modulator
- Multiband Dynamics
- Seek **modulator** on arbitrary properties (in 1.10, seek speed lives on **parameters**, not as a general modulator)

If your game banks were also built with 1.10, you should not need those for round-trip. If a dump ever shows them, that bank is newer than 1.10.

---

## 3. Parameters (`Parameter.*`, layouts, automation)

| Parameter setup | Features |
|---|---|
| Continuous game param: name, min/max, default | `Parameter`, `Type=GameControlled`, `Name`, `Range`, `DefaultValue` |
| Labeled / discrete param | `Parameter.Labels` |
| Parameter **velocity** | `Parameter.Velocity` |
| Parameter **seek speed** (and down, if separate in your build) | `Parameter.SeekSpeed`, `SeekSpeedDown` |
| Built-in automatics: Distance, Direction, Elevation, Cone Angle, Orientation, Speed, … | `Parameter.Type=Automatic*` |

Then:

1. Add params to an event → `Event.ParameterIds`, `Event.ParameterLayouts`, `ParameterLayout.*`
2. Place instruments on a **parameter sheet** → `ParameterLayout.Instruments`, `TriggerBoxes`
3. Right-click property → **Add Automation** → `Curve`, `Mapping`, `Controller`, `Property.*`
4. Optional: **action sheet** (concurrent / consecutive) for playlist-style triggering

---

## 4. Modulators (`Modulator.*`)

Right-click a property → **Add Modulation** (1.10 set):

| Modulator | Features |
|---|---|
| **AHDSR** | `Modulator.ADSR` |
| **Random** | `Modulator.Random` |
| **Sidechain** (needs a Sidechain effect) | `Modulator.Envelope` / sidechain path |
| **LFO** (try several shapes) | `Modulator.LFO`, `Shape=…` |
| **Autopitch** (instrument on a parameter sheet) | instrument autopitch body fields |

Skip Spectral Sidechain / Seek modulators (2.03+).

---

## 5. Events — macros (`Event.*`)

On the event macros / master deck, set non-defaults:

| Control | Feature |
|---|---|
| Timeline sheet | `Event.Timeline` |
| Extra audio tracks | `Event.NonMasterTracks` |
| Routing + master track | `Event.MasterTrack`, `Event.InputBus` |
| **Max Instances**, **Priority**, **Stealing** | `MaximumPolyphony`, `Priority`, `PolyphonyLimitBehavior` |
| **Cooldown** | `TriggerCooldown` |
| **Doppler** + scale | `DopplerScale` |
| **Min / Max Distance** (with Spatializer) | `DistanceAttenuation` |
| User properties (float + string) | `UserPropertyFloat`, `UserPropertyString` |

---

## 6. Timeline / transitions (`Timeline.*`, `Transition.*`)

Event: `evt_timeline`

On the timeline **logic track** (right-click):

1. Instruments on tracks → `Timeline`, `TriggerBoxes`
2. **Tempo marker** → `TempoMarkers`
3. **Destination marker** (+ loop region if useful)
4. **Sustain point** + conditions → `SustainPoints`, `Evaluators`
5. **Transition region** → destination, probability ≠ 100%, quantization (needs tempo), parameter conditions → `Transition.*`
6. Right-click transition → **Add Transition Timeline**; add lead-in/out / fades / curves → `TransitionTimeline`, `LeadIn`/`LeadOut`, curves, fade overrides

---

## 7. Instruments

Right-click track / playlist / action sheet:

| Menu | Event | Features |
|---|---|---|
| **Add Single Instrument** (or drag WAV) | `evt_waveform` | Waveform + body knobs; set asset loading mode in Assets browser |
| **Add Multi Instrument** | `evt_multi` | Playlist modes (Shuffle / Randomize / Sequential variants), Loop Playlist |
| **Add Scatterer Instrument** | `evt_scatterer` | Spawn interval / rate / total / stealing + playlist |
| Drag event / **Add Event Instrument** | `evt_eventref` | Nested event |
| Drag snapshot / **Add Snapshot Instrument** | (any) | `Event.SnapshotReference`, intensity |
| **Add Silence Instrument** (playlist / consecutive action sheet) | `evt_silence` | Duration |
| **Add Programmer Instrument** | `evt_programmer` | Name/key |
| **Add Plug-in Instrument** | `evt_plugininst` | Bank `EffectInstrument` / plug-in instrument path |
| **Add Command Instrument** | `evt_command` | Command Type / Target / Value |

Asset loading modes in 1.10 Assets browser: **Compressed**, **Decompressed**, **Stream** (labels may say “advanced loading mode”) → `WaveformResource.LoadingMode=…`.

---

## 8. Snapshots (`Snapshot.*`)

Snapshots browser:

1. **New Overriding Snapshot** and **New Blending Snapshot**
2. **New Group**; put snapshots in a group
3. Scope in properties; reorder for priority
4. Set intensity; optional AHDSR on intensity
5. Trigger with a snapshot instrument on an event

→ `Snapshot`, `Entries`, `Blending`, `Priority`, `Intensity`, `GroupResolutionMethod=…`

---

## 9. Banks / audio files

- Multiple assets with different loading modes
- **Banks** browser → **New Bank**; **Assign to Bank…**
- Keep a **Master Bank**; put most events on a non-master bank
- Unique sample names
- **File → Build…**

---

## Suggested minimal event list

| Event | Purpose |
|---|---|
| `evt_waveform` | Single + loading modes + instrument body |
| `evt_multi` | Playlist modes |
| `evt_scatterer` | Scatterer spawn controls |
| `evt_timeline` | Markers, sustain, transitions, transition timeline |
| `evt_param` | Parameter sheets, automation, modulators |
| `evt_nested` | Event / programmer / silence / command / plug-in instrument |
| `evt_3d` | Spatializer, distance, Doppler, built-in params |
| `evt_effects` | One track/bus chain that hosts **every** built-in DSP from the table above |

Plus mixer: Master, 2 groups, Reverb (+ optional 2nd return), 1 VCA, sends, one VST if available.

---

## How this ties to decompile

1. Studio 1.10 writes XML for each effect you actually placed.
2. Building banks produces `BuiltInEffectNode.DSPType` values in the **legacy** numbering your `EDSPTypeLegacy` / `DSPTypeResolver` already treat as 1.10-era.
3. Your decompiler should map each legacy DSP type → the XML class/properties you copied from this project’s `Metadata/`.
4. Do **not** map Lowpass → Multiband EQ in the decompiler unless you intentionally want a lossy upgrade; with a 1.10 reference project you can keep a 1:1 mapping.

---

## Validation loop

1. Build banks from the 1.10 test project.
2. Run BANK2FSPRO; capture the feature dump.
3. Confirm you see many distinct `Effect.BuiltIn.DSPType=…` lines (Lowpass, Highpass, Echo, …), not only Multiband EQ.
4. For each missing feature, change that one Studio control and rebuild.

## Notes vs Studio 2.x docs

Earlier drafts of this guide used Studio **2.03** manual wording. For your plan, treat **1.10 UI + this project’s Metadata XML** as source of truth. 2.x docs are still useful for concepts (sends, transition timelines, AHDSR) but not for which effects exist or for `serializationModel`.
