# Test Project Outline

A short FMOD Studio project that covers what the RehabG Master Bank actually uses. Follow the steps in order. Do not invent extra events.

**Target size:** 3 audio files, 5 events, 2 group buses, 1 return, 1 VCA, 2 parameters, 2 snapshots.

Official docs used for terminology:
- [Authoring Events](https://www.fmod.com/docs/2.03/studio/authoring-events.html)
- [Working with Instruments](https://www.fmod.com/docs/2.03/studio/working-with-instruments.html)
- [Mixing](https://www.fmod.com/docs/2.03/studio/mixing.html)
- [Event Macros](https://www.fmod.com/docs/2.03/studio/event-macro-controls-reference.html)
- [Instrument Reference](https://www.fmod.com/docs/2.03/studio/instrument-reference.html)
- [Modulator Reference](https://www.fmod.com/docs/2.03/studio/modulator-reference.html)

---

## Step 1. New project and assets

1. File → New Project. Save it somewhere easy to find.
2. Open the **Assets** browser.
3. Import **3** short WAV files (a few seconds each is fine). Call them `A`, `B`, and `C`.
4. Select `C`. In the deck / asset properties, set its loading mode to **Stream**.
5. Leave `A` and `B` on the default non-streaming mode (Compressed or Decompressed; either is fine).

You now have memory-loaded and streamed assets, which matches the bank’s waveform loading modes.

---

## Step 2. Mixer routing

Open Window → Mixer (or the Mixer tab).

1. In the **routing browser** (left side of the mixer), right-click empty space → **New Group**. Name it `SFX`.
2. Right-click again → **New Group**. Name it `Weapons`.
3. Drag `Weapons` onto `SFX` so routing is: `Weapons` → `SFX` → **Master Bus**.
4. A new project already has a return bus named something like **Reverb**. Keep that one return. If you deleted it, right-click → **New Return** and name it `Reverb`.
5. In the **VCAs** browser, right-click → **New VCA**. Name it `SFX_VCA`.
6. Drag `SFX` and `Weapons` onto `SFX_VCA` (or right-click each bus → **Assign To VCA** → `SFX_VCA`).
7. Select `SFX` in the mixer.
   - The tall **dB slider** on the channel strip is **Volume**. Drag it slightly off `0 dB` (for example `-1 dB`).
   - **Pitch** is not that slider. Look in the deck on the right for the bus **macros** (often a drawer labeled macros / MCR). Set **Pitch** slightly off `0 st` (for example `+1 st`).
   - Alternate: on the mixing desk, enable the strip option **MCR**. That shows pitch (and max instances / stealing) as number boxes on the group bus channel strip.

---

## Step 3. Effects and a send

Still in the **Mixer** window.

**Where things are:**
- Left side: **routing browser** (tree of buses: Master, `SFX`, `Weapons`, Reverb, …)
- Center: **mixing desk** with vertical **mixer strips** (one column per bus; fader + meters). That is what “bus strips” means.
- Bottom: the **deck**. Select a bus (click its name in the routing browser, or click its mixer strip) and the deck shows that bus’s **signal chain**.

To add effects:
1. Click the `SFX` mixer strip (or click `SFX` in the routing browser).
2. Look at the **deck** at the bottom.
3. Right-click empty space in the deck → **Add Effect** / **Insert Effect**.
4. Repeat for the other buses as needed.

Put **one** of each effect somewhere on `SFX`, `Weapons`, or `Reverb`. One instance each is enough:

| Effect | Where | Notes |
|---|---|---|
| Multiband EQ | `SFX` | Required. Bank raw DSP id `36` in 2018-era numbering. |
| Chorus | `Weapons` | |
| Flange | `Weapons` | |
| Delay | `Weapons` | |
| Distortion | `Weapons` | |
| Lowpass Simple | `Weapons` | |
| Pitch Shifter | `Weapons` | May be listed as **Pitch Shifter** or **Pitchshift**. |
| Loudness Meter | `SFX` | |
| Convolution Reverb | `Reverb` return | May already exist on the default Reverb return; keep/replace as needed. |
| Channel Mix | `Weapons` | Optional. Bank has **1** effect with raw id `33`, which in 2018 Core API terms is **Channel Mix**, not Multiband Dynamics. |

**Do not add Multiband Dynamics.** That effect only exists in FMOD Studio **2.03+**. Your Master Bank is file version `0x64` (2018-era). The feature dump’s `MULTIBAND_DYNAMICS` label is a parser enum mismatch: `FModBankParser`’s modern `EDSPType` renamed raw value `33`, which was **Channel Mix** in the 2018 `FMOD_DSP_TYPE` list.

Then add a send:

1. Select `Weapons`.
2. In its signal chain, add a **Send** to the `Reverb` return.
3. Set the send level to something other than silence (for example -6 dB).

Fader and Return modules already exist on buses; you do not need to add extras.

---

## Step 4. Event `E_Simple` (baseline)

1. In the **Events** browser, right-click → **New Event** → **2D Event**. Name it `E_Simple`.
2. Open its **timeline** sheet (default sheet for a new event).
3. Drag asset `A` onto the **master track**. That creates a **single instrument** (colored trigger region).
4. Select the instrument. In the deck, set:
   - **Volume** off default
   - **Pitch** off default
   - **Start Offset** to a small non-zero value (this is the trim/offset control)
   - Expand **Trigger Behavior** (vertical label on the deck) and set **Probability** to 50%
5. In the event’s **macros** drawer (usually bottom of the event editor; look for macros / event properties):
   - **Max Instances** = 2
   - **Stealing** = None
   - **Priority** = High
   - **Cooldown** = 100 ms
6. Still in macros, enable **Doppler** and set **Doppler Scale** to something other than 100% (for example 50%).
7. Route the event into the mixer: in the Events browser or mixer routing, set `E_Simple` to output into group bus `Weapons` (not straight to Master).

Assign `E_Simple` to the Master Bank (or your only bank): right-click event → **Assign to Bank**.

---

## Step 5. Modulators and automation on `E_Simple`

Select the single instrument on `E_Simple` again.

### Random modulator

1. Right-click the instrument’s **Volume** dial in the deck.
2. Choose **Add Modulator** → **Random**.
3. Leave the amount at a small audible range.

### AHDSR modulator

Studio calls this **AHDSR** (not ADSR).

1. Right-click the instrument’s **Pitch** number box.
2. Choose **Add Modulator** → **AHDSR**.
3. Give Attack and Release short non-zero times so the envelope is obvious in metadata.

### Automation

1. Right-click the master track **Volume** fader (or the instrument volume) → **Add Automation**.
2. Studio adds an automation curve on the current sheet. Draw **2** points with different shapes if the curve editor lets you (right-click a point to change curve shape).
3. Add automation on **one Multiband EQ band Gain** on the `SFX` bus (select the effect in the mixer, right-click Gain → Add Automation). Draw a tiny curve.

That gives you Property / Controller / Curve style data without chasing every property index.

---

## Step 6. Event `E_Multi` (multi instrument + stream)

1. New **2D Event** named `E_Multi`.
2. On the timeline master track, right-click → **Add Multi Instrument**.
3. Select the multi instrument. In the deck playlist:
   - Drag assets `A` and `B` into the playlist (**2** entries).
   - Set **Playlist Selection Mode** to **Shuffle** (this is Studio’s “smart random”; it avoids immediate repeats when there are enough entries).
4. Add a second single instrument on the same timeline using streamed asset `C`.
5. Assign `E_Multi` to the bank. Route it to `Weapons`.

---

## Step 7. Parameters and a parameter sheet

1. In the **Parameters** / presets browser (or Event editor → add parameter), create:
   - `Param_Game`: type **User** / game-controlled, range **0 to 1**
   - Keep or add built-in **Distance** (Automatic Distance). If your Studio version lists it as a built-in parameter, add that to the event.
2. Open `E_Multi`.
3. Add a **parameter sheet** for `Param_Game` (sheet tabs near the timeline; “+” / add sheet → choose the parameter).
4. On that parameter sheet, place **1** single instrument (drag asset `A`) so it triggers only when `Param_Game` is in some mid range (for example 0.4–0.6).

You now have both a timeline sheet and a parameter sheet, which is what bank `ParameterLayout` data corresponds to.

---

## Step 8. Event `E_Timeline` (markers + transition)

1. New **2D Event** named `E_Timeline`.
2. On the timeline, place **2** single instruments at different times (two separate trigger regions).
3. For one of them, leave **Async** off (button not yellow). That is a **synchronous** instrument: it plays the part of the waveform under the playhead. This is the Studio side of “time-locked” timeline behavior.
4. For the other, turn **Async** on (yellow). Leave **Cut** off unless you specifically want it to stop when untriggered.
5. On the logic track above the timeline, add:
   - **2** destination markers named `Mark_A` and `Mark_B` (right-click logic track → Add Destination Marker, or equivalent marker tools in your version)
   - **1** **transition region** covering a short range near `Mark_A`, destination set to `Mark_B`
6. Select the transition region. In the deck, under trigger conditions, **Add Parameter Condition** using `Param_Game` (for example only transition when `Param_Game` > 0.5).
7. Add `Param_Game` to this event too if Studio requires the parameter to exist on the event for the condition.
8. Assign to bank; route to `Weapons`.

---

## Step 9. Snapshots and event `E_Snap`

1. Open the Mixer → **Snapshots** browser.
2. Create **1** snapshot group if needed, then **2** snapshots: `Snap_A` and `Snap_B`.
3. Edit `Snap_A`: scope in the `SFX` bus volume and set it quieter.
4. Edit `Snap_B`: scope in `SFX_VCA` volume and set it quieter.
5. On each snapshot’s deck / macros:
   - Turn on blending if there is a blend / intensity control
   - Set **Intensity** off 100% on one of them
   - Set **Priority** differently on the two if the control exists
   - If you see conflict resolution / resolution method, set it to **Average**
6. New event `E_Snap`.
7. On its timeline, right-click a track → **Add Snapshot Instrument**, or drag `Snap_A` onto a track.
8. Select the snapshot instrument. Set its **Intensity** off default if the control is there.
9. Also add **1** **event instrument** that references `E_Simple` (right-click track → Add Event Instrument, or drag `E_Simple` onto the track).
10. Assign `E_Snap` to the bank; route to `SFX`.

---

## Step 10. Build once

1. Confirm every event is assigned to a bank.
2. File → **Build…**
3. Open the project folder’s `Metadata` directory and skim:
   - `AudioFile`
   - `Event`
   - `MixerGroup` / related mixer XML
   - `Snapshot`
   - Anything that looks like automation / modulator related files

Compare those objects to bank node types (`EventNode`, `BaseBusNode`, `Single`/`Multi` instruments, `SnapshotNode`, etc.).

Do not rebuild after every earlier step unless something is broken.

---

## Final checklist

| Item | Count |
|---|---|
| Audio files | 3 (`A`, `B` memory; `C` stream) |
| Events | 4 (`E_Simple`, `E_Multi`, `E_Timeline`, `E_Snap`) |
| Group buses | 2 (`SFX`, `Weapons`) |
| Return buses | 1 (`Reverb`) |
| VCAs | 1 (`SFX_VCA`) |
| User parameters | 1 (`Param_Game`) |
| Built-in Distance parameter | 1 if available |
| Snapshots | 2 |
| Multi instrument | 1 (Shuffle, 2 entries) |
| Snapshot instrument | 1 |
| Event instrument | 1 |
| Transition region + parameter condition | 1 |
| Random modulator | 1 |
| AHDSR modulator | 1 |
| Automation curves | 2 |

---

## Skip on purpose

These appear in FMOD Studio but **not** in the RehabG bank feature dump:

- Scatterer, silence, programmer, command instruments
- Plugin effects
- Sidechain / spectral sidechain
- LFO, seek, and envelope modulators
- Sustain points
- Tempo markers
- Transition timelines with lead-in / lead-out curves
- Labeled / enumeration parameters

---

## Terminology cheat sheet

| Bank / old wording | FMOD Studio UI term |
|---|---|
| Waveform instrument | **Single instrument** |
| Smart random playlist | Multi instrument **Playlist Selection Mode = Shuffle** |
| Polyphony limit behavior | Event macros **Max Instances** + **Stealing** |
| ADSR | **AHDSR** modulator |
| Time-locked instrument | Timeline instrument with **Async** off (synchronous) |
| Parameter layout | Event **parameter sheet** |
| Snapshot reference on an event | **Snapshot instrument** |
| DSP type 36 | **Multiband EQ** |

---

## Note: DSP ids vs parser names

Your Master Bank is **file version `0x64`** (2018-era Studio). Multiband Dynamics did not exist yet.

In the 2018 `FMOD_DSP_TYPE` enum (still including old plugin slots):

| Raw id in bank | 2018 meaning | Count in bank | What `FModBankParser` prints |
|---|---|---|---|
| 36 | **Multiband EQ** | 11 | unnamed `36` |
| 33 | **Channel Mix** | 1 | wrongly `MULTIBAND_DYNAMICS` |

So for the test project: add **Multiband EQ**, optionally **Channel Mix**, and ignore Multiband Dynamics entirely.
