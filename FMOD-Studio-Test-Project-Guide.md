# FMOD Studio Test Project Guide

Build one small “kitchen sink” FMOD Studio project that covers every category `CollectedBank.Debug()` can emit, then open `Metadata/` to see the XML shapes.

Verified against the [FMOD Studio 2.03 User Manual](https://www.fmod.com/docs/2.03/studio/) (Mixing, Authoring Events, Instrument Reference, Effect Reference, Modulator Reference, Managing Assets, Event Macros).

## Workflow

1. Create a new FMOD Studio project (match your target Studio version if you can; BANK2FSPRO stubs use `serializationModel="Studio.02.02.00"`).
2. Import a few short WAVs (mono + stereo) via the **Assets** browser.
3. Build features in the order below (mixer → parameters → events → snapshots → banks).
4. Save, then inspect XML under:
   - `Metadata/Event/`
   - `Metadata/AudioFile/`
   - `Metadata/Bank/`
   - `Metadata/Mixer.xml` / `Master.xml`
   - `Metadata/SnapshotGroup/`
   - `Metadata/ParameterPreset/` (if you use presets)
5. **File → Build…** → run BANK2FSPRO → compare the feature dump to this checklist. Missing lines = missing Studio setup (or a bank-version quirk).

Use **one event per major feature family** so XML stays readable. Name them like `evt_timeline`, `evt_multi`, `evt_scatterer`, etc.

---

## 1. Mixer / buses (`Bus.*`)

Open **Window → Mixer**. Work in the **routing browser** (left) and the **deck** (bottom) when a bus is selected.

| Studio action | Features you hit |
|---|---|
| Keep the default **Master Bus** | `Bus.MasterBusNode`, `Bus.BusBody`, `Bus.Routable` |
| Routing browser empty space → **New Group** (e.g. SFX, Music) | `Bus.GroupBusNode` |
| Use the default **Reverb** return, or create another return from the routing browser | `Bus.ReturnBusNode` |
| Drag events/groups onto parent buses to route; set volume/pitch ≠ default | `Bus.MixerStrip.Volume`, `Bus.MixerStrip.Pitch` |
| **VCAs** tab → empty space → **New VCA**; then bus → **Assign To VCA → …** (or drag onto the VCA) | `Bus.MixerStrip.VCAs`, `VCA`, `VCA.Strips` |
| Select a bus → deck empty space → **Add Effect** / **Insert Effect** both **left of** and **right of** the fader | `Bus.PreFaderEffects`, `Bus.PostFaderEffects` |
| Enable **MCR** on the mixing desk (or use bus macros) and set **max instances** / **stealing** | `Bus.MaximumPolyphony`, `Bus.PolyphonyLimitBehavior=…` |
| Routing browser → **New Port**; select it → deck **port macros → type** (Controller Speaker, Vibration, etc.) | `Bus.OutputPortNode`, `Bus.PortType=…` |
| Change speaker format / channel-format-changing effects (Channel Mix, Spatializer, Object Spatializer, Convolution Reverb) | `Bus.InputChannelLayout`, `Bus.Pre/PostFaderInputChannelLayouts`, `Bus.ObjectPannerIndex` |

Also tweak the VCA’s volume (and pitch if shown) → `VCA.MixerStrip.*`.

**Sends:** on a bus or event track deck → **Add Send** / **Insert Send**, target a return, set send **Volume**. Place the send left of the fader for pre-fader, right for post-fader.

---

## 2. Effects (`Effect.*`)

Put effects on Master/group/return buses **and** on event master/audio tracks.

Deck empty space → **Add Effect** / **Insert Effect** / **Add Send** / **Add Sidechain** (and spectral sidechain where available).

### Built-in effects (Studio 2.03 names)

Add **every effect the current Studio menu still exposes**. Do **not** substitute one effect for another “because it sounds similar.”

The bank stores a concrete `DSPType` on `BuiltInEffectNode`. Your decompiler must map that enum value to the matching Studio XML class/properties. Multiband EQ is `FMOD_DSP_TYPE_MULTIBAND_EQ`; Lowpass is a different type (`FMOD_DSP_TYPE_LOWPASS` / `_SIMPLE`). Building only Multiband EQ teaches you Multiband EQ XML — it does **not** give you Lowpass/Highpass XML, and it will not round-trip a bank that contains those DSP types.

| Studio effect | Typical bank DSP |
|---|---|
| **3-EQ** | `THREE_EQ` |
| **Multiband EQ** | `MULTIBAND_EQ` |
| **Parametric EQ** | `PARAMEQ` (if still offered) |
| **Channel Mix** | `CHANNELMIX` |
| **Chorus** / **Flanger** / **Distortion** / **Delay** | `CHORUS` / `FLANGE` / `DISTORTION` / `DELAY` |
| **Reverb** / **Convolution Reverb** | `SFXREVERB` / `CONVOLUTIONREVERB` |
| **Compressor** / **Limiter** / **Gain** | `COMPRESSOR` / `LIMITER` / (gain module) |
| **Panner** / **Spatializer** / **Object Spatializer** | `PAN` / spatializer / `OBJECTPAN` |
| **Transceiver** | `TRANSCEIVER` |
| **Send** | `SEND` (also `SendEffectNode`) |
| **Sidechain** / **Spectral Sidechain** | sidechain effect nodes |
| **Multiband Dynamics** (if present) | `MULTIBAND_DYNAMICS` |

That covers `Effect.BuiltInEffectNode`, `Effect.EffectBody`, `Effect.Parameterized`, wet/dry/input gain, flags, and parameter buffers for **modern** Studio-authored banks.

#### Effects missing from the modern UI

Types like **Lowpass**, **Highpass**, **Echo**, **Tremolo**, **Pitch Shift**, **Normalize**, **FFT**, **Loudness Meter**, **Envelope Follower** may still appear in dumps from older banks (`Effect.BuiltIn.DSPType=…`), but current Studio may no longer let you place them.

For decompile that means:

1. This kitchen-sink project is the ground truth for **effects Studio can still author** → XML you can copy.
2. For **legacy-only DSP types**, you need either an old Studio project/bank that still contains them, or an explicit decompiler mapping (e.g. Lowpass → Multiband EQ low-shelf/LPF band, or a synthetic XML shape). That mapping is a decompiler decision — do not pretend the test project “covers” those types by adding Multiband EQ.

### Special effect nodes

| Studio action | Features |
|---|---|
| **Add Send** → target return; set send Volume | `Effect.SendEffectNode`, `Effect.Send`, `ReturnGuid`, `SendLevel` |
| **Add Sidechain** on a source track/bus; on a compressor set **Sidechain** dropdown, or add **Add Modulation → Sidechain** on a property | `Effect.SideChainEffectNode`, Targets/Level; `Effect.Parameterized.SideChainEnabled` |
| **Add Spectral Sidechain** + **Add Modulation → Spectral Sidechain** on a property (Mode: **RMS** or **Spectral Centroid**) | `Effect.SpectralSideChainEffectNode`; `Modulator.SpectralSidechain` |
| Deck → **Add Effect → Plug-in effects → …** (any VST/third-party) | `Effect.PluginEffectNode`, `PluginName=…`, optional `Name=…` |

---

## 3. Parameters (`Parameter.*`, layouts, automation)

In the event editor / parameters browser, create:

| Parameter | Features |
|---|---|
| Continuous game parameter with min/max/default + name | `Parameter`, `Type=GameControlled`, `Name`, `Range`, `DefaultValue` |
| Labeled / discrete parameter | `Parameter.Labels` |
| Velocity on the parameter; **Add Modulation → Seek** on the parameter value (asymmetric ascending/descending if you want both seek speeds) | `Parameter.Velocity`, `SeekSpeed`, `SeekSpeedDown` |
| Built-in parameters: Distance, Distance (Normalized), Direction, Elevation, Event Cone Angle, Event Orientation, Listener Orientation, Speed, Speed (Absolute), etc. | `Parameter.Type=Automatic*` |

Then:

1. Add parameters to an event → `Event.ParameterIds`, `Event.ParameterLayouts`, `ParameterLayout.*`
2. Open a **parameter sheet** (not only the timeline) and place instruments → `ParameterLayout.Instruments`, `TriggerBoxes`
3. Right-click a property → **Add Automation**; draw curves → `Curve`, `Curve.Owner`, `Curve.Points`, `Curve.Point.Type=…`, `Shape`
4. That also pulls `Property.*`, `Mapping` / `Mapping.Points`, `Controller.*`

Also try an **action sheet** (concurrent or consecutive) for fire-and-forget playlists of instruments.

---

## 4. Modulators (`Modulator.*`)

Right-click a property in the deck → **Add Modulation → …**

| Studio menu | Features |
|---|---|
| **AHDSR** | `Modulator.ADSR` (bank name is ADSR; UI is AHDSR) |
| **Random** | `Modulator.Random` |
| **Sidechain** (needs a **Sidechain** effect elsewhere) | `Modulator.Envelope` / sidechain envelope-follower style |
| **LFO** (try Shape: Sine, Square, Triangle, Saw Up/Down, Noise) | `Modulator.LFO`, `Shape=…` |
| **Seek** | `Modulator.Seek` |
| **Spectral Sidechain** (needs a **Spectral Sidechain** effect; Mode RMS / Spectral Centroid) | `Modulator.SpectralSidechain`, ThresholdMapping |
| **Autopitch** (only on instruments on a parameter sheet) | Related instrument autopitch body fields |

Also note `PropertyType`, `ClockSource` (Instance / Global), `Owner`, `PropertyIndex`.

---

## 5. Events — shared event properties (`Event.*`)

Select the event’s **master track** / macros drawer (deck) and set non-defaults:

| Studio control (Event Macros) | Feature |
|---|---|
| Timeline sheet present | `Event.Timeline` |
| Add extra **audio tracks** beyond master | `Event.NonMasterTracks` |
| Route the event in the mixer routing browser; master track exists by default | `Event.MasterTrack`, `Event.InputBus` |
| **Max Instances**, **Priority**, **Stealing** | `MaximumPolyphony`, `Priority`, `PolyphonyLimitBehavior` |
| **Cooldown** | `TriggerCooldown` |
| **Doppler** on + **Doppler Scale** ≠ default | `DopplerScale` |
| **Min and Max Distance** (needs Spatializer / Object Spatializer / Distance Normalized, etc.) | `DistanceAttenuation` |
| User properties (float + string) on the event | `UserPropertyFloat`, `UserPropertyString` |
| Instruments with event-state / start-stop style trigger conditions | `EventTriggeredInstruments` |

> `Event.SchedulingMode=…` is a bank field; there may be no single identically named control in the macros drawer. Cover related behavior with streaming assets, async instruments, and timeline logic, then check what the dump reports.

---

## 6. Timeline / transitions (`Timeline.*`, `Transition.*`)

Event: `evt_timeline`

On the **timeline** sheet, right-click a **logic track**:

1. Place instruments on audio tracks → `Timeline`, `TriggerBoxes`
2. Add a **tempo marker** → `Timeline.TempoMarkers`
3. Add a **destination marker** (and optionally a destination/loop region) → destination / named-marker style data
4. Add a **sustain point**; give it parameter/event conditions in the deck → `SustainPoints` + `Evaluators`
5. Add a **transition region** (or transition marker) aimed at a destination marker/loop region:
   - destination + region length → `Transition.Destination`, `StartEnd`
   - deck **probability** toggle + chance ≠ 100% → `ChancePercent`
   - deck **quantization** (bars / notes; needs a tempo marker) → `Quantization`, `Unit=…`
   - **parameter conditions** via the logic/condition UI → evaluators
6. Right-click the transition → **Add Transition Timeline**; double-click to open it; add lead content / fades / curves → `TransitionTimeline`, `Length`, `LeadIn`/`LeadOut`, `LeadInCurves`, `LeadOutCurves`, `CurveMapping`, `FadeOverrides`, time-locked / triggered boxes

---

## 7. Instruments (one event each)

Right-click an audio track / playlist / action sheet and use the exact menu names below.

### Single (waveform) — `evt_waveform`

- **Add Single Instrument**, or drag a WAV onto a track
- In the **Assets** browser, set loading mode per asset: **Compressed**, **Decompressed**, or **Stream** (Stream button / advanced loading mode) → `Instrument.WaveformInstrumentNode`, `LoadingMode=…`, `WaveformResource`, `AudioFile`
- Non-default volume/pitch, loop / play count, trigger chance, trigger delay, quantization, start offset, 3D offset, routing, polyphony-style limits → `Instrument.InstrumentBody` + matching body flags

### Multi — `evt_multi`

- **Add Multi Instrument** with ≥2 playlist entries (drag assets or **Add Single Instrument** into the playlist)
- **Playlist Selection Mode**: Shuffle, Randomize, Sequential - play scope, Sequential - global scope, Sequential - instance scope → `Playlist.PlayMode=…`
- **Loop Playlist** toggle → related play-mode / selection behavior (`SelectionMode=…` in the dump)

### Scatterer — `evt_scatterer`

- **Add Scatterer Instrument**
- Set **Min & Max Spawn Interval**, **Spawn Rate**, **Spawn Total**, **Spawn Stealing**, playlist selection mode, optional spawn quantization → all `Instrument.Scatterer.*`

### Event reference — `evt_eventref`

- Drag another event onto a track, or **Add Event Instrument**
- For snapshots: drag a snapshot onto a track, or **Add Snapshot Instrument**; set **intensity** → `EventReference`, `SnapshotIntensity`, `ParameterStubs`

### Silence — `evt_silence`

- On a multi/scatterer playlist or consecutive action sheet: **Add Silence Instrument**; set **Duration** → `Silence`, `Duration`

### Programmer — `evt_programmer`

- **Add Programmer Instrument**; set the key/name → `Programmer`, `Name`

### Plug-in instrument — `evt_plugininst`

- **Add Plug-in Instrument → …** (bundled trials or your own generator DSP)
- This is the Studio UI for bank `EffectInstrumentNode` / `Instrument.Effect` (not a mixer plug-in *effect*)

### Command — `evt_command`

- **Add Command Instrument**
- Set **Command Type**, **Target**, **Value** / **Delta Value** → `Command.Type=…`, `Target`, `Value`

---

## 8. Snapshots (`Snapshot.*`, `Event.SnapshotReference`)

In the Mixer **snapshots browser**:

1. Empty space → **New Overriding Snapshot** and **New Blending Snapshot**
2. Empty space → **New Group**; put snapshots in the group (equal priority / averaged)
3. Scope in bus/effect properties; reorder snapshots in the browser to change **priority**
4. Adjust snapshot **intensity**; optionally **Add Modulation → AHDSR** on intensity
5. Trigger via **Add Snapshot Instrument** / drag snapshot onto an event track → `Event.SnapshotReference`

| Studio concept | Feature |
|---|---|
| Overriding vs blending snapshot type | `Snapshot.Blending` |
| Browser order / groups | `Priority`, `GroupResolutionMethod=…` |
| Scoped property values | `Snapshot.Entries` |
| Intensity | `Snapshot.Intensity` |

> Bank `GroupResolutionMethod` values (Least / Greatest / Additive / Average / Multiply / Override) may not all map 1:1 to a single Studio dropdown; covering overriding + blending + grouped snapshots is the practical UI coverage.

---

## 9. Audio files / banks (`AudioFile.*`, waveform loading)

- Multiple WAVs with different **Assets** browser loading modes (**Compressed** / **Decompressed** / **Stream**) → `WaveformResource.LoadingMode=…`
- **Banks** browser → **New Bank**; right-click events → **Assign to Bank…** (or drag onto a bank)
- Keep at least one **Master Bank** (**Mark as Master Bank**); put most events on a non-master bank
- Keep sample/asset names unique (the collector currently assumes unique sound names)
- **File → Build…** (and ensure Master + `.strings` banks are produced)

---

## Suggested minimal event list

| Event | Purpose |
|---|---|
| `evt_waveform` | Single instrument + asset loading modes + instrument body knobs |
| `evt_multi` | Playlist selection modes + Loop Playlist |
| `evt_scatterer` | Spawn interval/rate/total/stealing + quantization |
| `evt_timeline` | Tempo/destination/sustain + transition region + transition timeline |
| `evt_param` | Parameter sheets, automation curves, modulators |
| `evt_nested` | Event instrument + programmer + silence + command + plug-in instrument |
| `evt_3d` | Spatializer + min/max distance + Doppler + built-in distance/direction params |
| `snap_*` via snapshot instruments | Overriding + blending snapshots |

Plus mixer: Master, 2 groups, Reverb (and maybe a second return), 1 VCA, 1 port, sends, built-in effects, one plug-in effect if available.

---

## How to read the XML after save

Studio writes one (or a few) objects per file. Patterns you’ll see:

- `object class="Event"` with `relationship` to timeline, tracks, parameters
- `SingleSound` / `MultiSound` / `ScattererSound` / `EventSound` / `SilenceSound` / `ProgrammerSound` / `CommandInstrument` / plug-in instrument classes
- `MixerBus` / `MixerReturn` / `MixerGroup` / `MixerVCA` / port bus objects
- `Snapshot` / `SnapshotGroup`
- `GameParameter` / automatable objects / automation curves / modulator classes

BANK2FSPRO’s `XmlBuilder` already mirrors that shape (`property` / `relationship` / `destination`). Use this kitchen-sink project as ground truth for property names and nesting.

---

## Validation loop

1. **File → Build…** from the test project.
2. Run BANK2FSPRO and capture the feature dump.
3. Diff against this checklist.
4. For each missing `Feature.X`, change that one Studio control and rebuild — don’t change everything at once.

## Doc references

- [Mixing](https://www.fmod.com/docs/2.03/studio/mixing.html)
- [Authoring Events](https://www.fmod.com/docs/2.03/studio/authoring-events.html)
- [Working with Instruments](https://www.fmod.com/docs/2.03/studio/working-with-instruments.html)
- [Instrument Reference](https://www.fmod.com/docs/2.03/studio/instrument-reference.html)
- [Effect Reference](https://www.fmod.com/docs/2.03/studio/effect-reference.html)
- [Modulator Reference](https://www.fmod.com/docs/2.03/studio/modulator-reference.html)
- [Event Macros Drawer Reference](https://www.fmod.com/docs/2.03/studio/event-macro-controls-reference.html)
- [Managing Assets](https://www.fmod.com/docs/2.03/studio/managing-assets.html)
- [Getting Events into Your Game](https://www.fmod.com/docs/2.03/studio/getting-events-into-your-game.html)
