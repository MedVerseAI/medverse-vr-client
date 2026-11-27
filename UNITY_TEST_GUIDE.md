# MedVerse VR Client — Unity & Meta Quest 3 Test Guide

This guide walks our VR track step-by-step through importing the new **`quest3_lod0`** anatomical `.fbx`, `manifest.json`, and fixed C# scripts into Unity LTS + URP + Meta XR SDK and verifying performance on real Meta Quest 3 hardware.

> **Why no `ProjectSettings/` or `.meta` files are in this branch yet:** Unity Editor must generate `ProjectSettings/`, `Packages/manifest.json`, and `.meta` GUIDs natively on a workstation with Unity Hub installed. Never hand-fabricate `.meta` files.

---

## 1. Download the Pre-Built Quest 3 Asset Pack

1. Open the **GitHub Draft Release** on [`MedVerseAI/medverse-asset-ingestion`](https://github.com/MedVerseAI/medverse-asset-ingestion/releases) tagged **`v0.2-proposal-unity-test`**.
2. Download:
   - `anatomical_model_quest3_lod0.fbx` *(decimated strictly `< 180,000` triangles, consolidated lobe/group palette materials, `-Z` forward / `Y` up, zero `LM_*` helper empties)*
   - `manifest.json` *(canonical FMA / Brainnetome entity catalog + vertex/triangle counts + SHA-256 checksums)*

---

## 2. Import Into Your Unity LTS + URP + Meta XR Project

1. Open your **Unity LTS** project configured with **Universal Render Pipeline (URP)** and **Meta XR All-in-One SDK**.
2. Copy the downloaded assets and the fixed scripts from this branch (`proposal/vr-scripts-unity-test`):
   - `anatomical_model_quest3_lod0.fbx` → `Assets/Models/Anatomy/`
   - `manifest.json` → `Assets/StreamingAssets/Anatomy/manifest.json`
   - `Scripts/BrainDisplayController.cs` → `Assets/Scripts/Anatomy/BrainDisplayController.cs`
   - `Scripts/AnatomyPartSelector.cs` → `Assets/Scripts/Anatomy/AnatomyPartSelector.cs`
3. Select `anatomical_model_quest3_lod0.fbx` in the Project window and verify **Model Import Settings**:
   - **Scale Factor:** `1` *(already exported in real meters: `1 unit = 1 m`)*
   - **Convert Units:** `Checked`
   - **Materials → Material Creation Mode:** `Import via MaterialDescription` *(works out-of-the-box with URP Lit — zero texture extraction or custom vertex-color shader needed!)*
   - Click **Apply**.
4. Drag `anatomical_model_quest3_lod0.fbx` into your XR scene under an empty root GameObject (`AnatomyRoot`).
5. Attach:
   - `MedVerse.VR.BrainDisplayController` to `AnatomyRoot` (assign your XR CenterEyeAnchor `Camera` or leave `targetCamera` empty to auto-find `Camera.main`; default `distance = 1.0f`, `verticalOffset = -0.1f`, `brainScale = 1.0f`).
   - `MedVerse.VR.AnatomyPartSelector` to your UI / Manager GameObject (assign `commandInput` to your `TMP_InputField` and `brainRoot` to `AnatomyRoot`).

---

## 3. Five-Point Meta Quest 3 Verification Checklist

Run the scene in Play Mode and on an actual **Meta Quest 3** headset, checking all five items below:

- [ ] **1. Scale & Axis Orientation:** The full CNS model (brain + spinal column) appears upright (`Y` up, facing the viewer cleanly along `-Z` forward) at realistic human scale (~0.65 m tall) with **zero `LM_*` landmark empties** cluttering the Hierarchy.
- [ ] **2. Polygon & Material / Draw-Call Budget:** Open the Unity **Stats** window (or RenderDoc / OVR Metrics Tool on Quest 3):
  - Total rendered triangles strictly **`< 180,000`** (down from 1,217,908 in v1).
  - Total unique materials consolidated to **~10–18** shared palette materials (down from 224), keeping Quest 3 smoothly at **72–90 FPS**.
- [ ] **3. Arm's-Reach Horizontal Framing (`BrainDisplayController`):** Launch while looking slightly downward toward the floor — confirm the model spawns **level at arm's reach (~1.0 m ahead, -0.1 m vertical)** instead of inside the floor, and verify calling `Recenter()` twice does **not** shrink or double-scale nested meshes.
- [ ] **4. Bilateral & Multi-Mesh Highlight (`AnatomyPartSelector`):** Type `hippocampus` (or `Hipp` / `vertebra` / `thalamus`) into the search box and trigger `SelectPart()` — confirm **both Left and Right** matching structures highlight simultaneously in `highlightColor` without affecting sibling meshes sharing the same palette material.
- [ ] **5. Empty-Search Clear & Shader Preservation:** Clear the search input to empty (`""`) and trigger `SelectPart()` — confirm **all previous highlights clear immediately** (restoring original lobe/group colors) while preserving any unrelated `MaterialPropertyBlock` properties.

---

## 4. Next Step After Verification

Once verified on Quest 3, commit the Unity Editor–generated `Assets/`, `Packages/manifest.json`, and `ProjectSettings/` (with `.gitignore` excluding `Library/`, `Temp/`, `Obj/`, and Git LFS / Release downloads for `.fbx` binaries) and open a PR merging into `main` (closing Issues #2 and #3).
