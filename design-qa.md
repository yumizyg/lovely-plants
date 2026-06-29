# Design QA

- Source visual truth: `src/DesktopGarden/Assets` and the selected Fluent Plant Inspector direction
- Design references: Microsoft Fluent popover/drawer guidance and the existing watercolor asset set
- Implementation screenshot: `artifacts/ui-preview-v2.png`
- Garden-only screenshot: `artifacts/garden-preview-v2.png`
- Viewport: 1120 × 720 comparison board; three-pot garden at 72% scale
- State: stage-one Alocasia at 2 hours 18 minutes, hover card and right-click inspector visible

**Full-View Comparison Evidence**

The selected soft-white, charcoal, sage, and coral system is applied consistently. The hover card remains compact and separate from the editable inspector. The inspector uses one flat surface, aligned property rows, restrained separators, and no nested cards.

**Focused Region Comparison Evidence**

The UI preview renders the hover panel and inspector at readable size. Plant preview cropping, progress treatment, warning copy, custom scale slider, rounded buttons, and source-asset transparency were inspected directly.

**Required Fidelity Surfaces**

- Fonts and typography: Microsoft YaHei UI provides a clear 8/9/11 pt hierarchy; Chinese labels fit without clipping.
- Spacing and layout rhythm: 18 px panel padding, aligned label/value/action columns, consistent row separators, and 6–8 px radii.
- Colors and visual tokens: soft-white surfaces, charcoal text, sage progress/primary actions, coral reset warning, and muted gray-green dividers match the selected direction.
- Image quality and asset fidelity: original PNGs are used directly, alpha edges remain clean, previews crop transparent padding, and aspect ratios are preserved.
- Copy and content: hover information includes plant name, stage, elapsed time, progress, and remaining time; the inspector explicitly warns that replacing a plant resets growth.

**Findings**

- No actionable P0, P1, or P2 visual findings.

**Patches Made During QA**

- Replaced native square buttons and blue TrackBar with themed rounded controls.
- Cropped transparent padding from the inspector plant preview.
- Made the inspector and hover card owned windows so animated pots cannot overlap them.
- Replaced live ambient rendering with cached animation frames to reduce idle CPU usage.

**Follow-up Polish**

- P3: future custom sound assets could match the watercolor personality better than the optional Windows system sound.

final result: passed
