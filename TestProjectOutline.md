SLOP WARNING: !!!!

# Test Project Outline

Based on the **143 unique reconstruction features** found in the RehabG Master Bank. Build these in FMOD Studio so you can learn how bank nodes map to project metadata.

Skip anything not on this list — the target bank does not use it.

## 0. Project skeleton

- Empty `.fspro` with folders: Events, Mixer, Parameters, Snapshots, Assets, Banks
- One Master Bank assignment
- Import a few short WAVs (some marked **stream**, some **in memory**)

## 1. Audio + simple event (baseline)

- 1–2 `AudioFile` assets
- 1 event with a **master track** + **waveform instrument**
- Route event → master bus
- Build once and compare `Metadata/AudioFile`, `Event`, instrument XML

## 2. Mixer graph

- **Master bus**
- 2–3 **group buses** (hierarchy)
- 1 **return bus**
- 1 **VCA** controlling a couple of strips
- Volume + pitch on strips
- Pre-fader and post-fader effects on a bus
- Event **input bus** routing into a group

## 3. Effects you’ll need

Put these on buses/tracks (the bank has them):

- Fader, Return
- Chorus, Flange, Delay, Distortion
- Lowpass Simple, Pitch Shift
- Convolution Reverb
- Multiband Dynamics
- Loudness Meter
- **Multiband EQ** (bank stores this as DSP type `36` / `FMOD_DSP_TYPE_MULTIBAND_EQ` in the older enum numbering)
- At least one **Send → Return** with a non-default send level

## 4. Event variants

Make several small events covering:

- Timeline present
- Max polyphony + polyphony limit behavior
- Priority
- Scheduling mode variants (bank uses `1` and `2`)
- Trigger cooldown
- Doppler scale
- Flags / 3D-ish settings if Studio exposes them for your version
- Snapshot reference on an event (snapshot event type or linked snapshot)

## 5. Instruments

- **Waveform** instruments (memory + stream loading)
- Volume / pitch / left trim / auto-pitch reference
- Trigger chance ≠ 100%
- Instrument polyphony limit behavior
- **Multi instrument** with playlist:
  - Play mode: **Smart Random**
  - Selection: **Select Normal**
  - Multiple weighted entries
- **Event instrument** referencing another event
  - Include **snapshot intensity** if you can set it

## 6. Timeline + transitions

- Trigger boxes (instruments on timeline)
- Time-locked trigger boxes
- Named markers
- **Transition region** to another marker/destination
- Parameter condition on a transition (even a simple one)

## 7. Parameters + layouts

- **Game-controlled** parameter (named, ranged)
- **Automatic Distance** parameter
- Put both on an event’s parameter sheet (`ParameterLayout`)
- Instruments placed on the parameter sheet (not only timeline)

## 8. Automation graph (important for reconstruction)

On volume/pitch (and one non-volume property):

- Draw automation curves (point types 0 and 1, with shapes)
- Confirm you get `Property` → `Controller` → `Curve` (+ `Mapping` where Studio uses it)
- Cover property indices seen in the bank (`0`, `1`, `4`, `1000–1002`) by automating common + effect params

## 9. Modulators

- **Random** modulator on volume (and one “normal” property)
- **ADSR** modulator on volume
- Local clock source (default is fine)

## 10. Snapshots

- Snapshot group with 2+ snapshots
- Blending enabled
- Intensity
- Priority
- Conflict resolution: **Average**
- Snapshot entries that touch bus/VCA/effect properties
- One event that references/triggers a snapshot

## 11. Build & study loop

For each section above:

1. Build banks
2. Diff `Metadata/*.xml` before vs after
3. Note which GUIDs/relationships Studio created
4. Map those back to bank node types

## Explicitly skip (not in the target bank)

- Scatterer / silence / programmer / command instruments
- Plugin effects
- Sidechain / spectral sidechain
- LFO / seek / envelope modulators
- Sustain points
- Tempo markers
- Transition lead-in/out curves
- Labeled parameters
- Plugin effect names

## Notes

### DSP type 36

In the older/full `FMOD_DSP_TYPE` enum (with plugin slots still present), **36 = `FMOD_DSP_TYPE_MULTIBAND_EQ`**.

`FModBankParser`’s `EDSPType` dropped old plugin entries and renumbers Multiband EQ to `0x20` (32), which is why the feature dump printed raw `36` instead of a named enum value.
