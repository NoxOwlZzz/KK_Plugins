# Material Editor UI accessibility

## Evidence boundary

This is a source-level accessibility inventory for commit `8c36e8df`. It is
not an accessibility certification. No screen reader, keyboard-only, color
vision, high-DPI, motion, low-vision or rendered-contrast session has been run
in Koikatu or CharaStudio. Those checks remain **PENDING**.

## Implemented source accommodations

### Text and names

- Primary text is 16, secondary text 14, and long property/category/selection
  labels have a 12-unit best-fit floor.
- Dropdown captions/options and Vector component text have a 12-unit floor.
- Truncated renderer, material, shader, category and property names retain the
  complete real name in a standard tooltip where the model supplies it.
- Property metadata remains available as the shader-hint tooltip; a friendly
  display name never replaces the stable internal property name.
- Labels that already support `LabelClick` retain the same clickable object.

### Contrast and non-color cues

- Source tests require Primary/Window contrast >=4.5:1 and
  Secondary/Window and Disabled/Window >=3:1. Token calculations are about
  15.44:1, 8.25:1 and 3.93:1.
- Selected renderer/material entries use a toggle/check state plus selected
  surface; selection is not intended to rely on color alone.
- Category state combines glyph, surface/marker and font weight.
- When existing ChangedState and interactability permit reset, changed property
  state exposes the `↺` action in addition to the changed surface.
- Mixed values retain textual `Mixed`/component state where the model exposes
  it; Disabled, Mixed and Modified tokens are distinct.
- Fold/collapse state uses directional or plus/minus glyphs and tooltips.

### Focus, dismissal and accidental activation

- The row-action menu selects its first enabled shared button when opened.
- Escape is forwarded from the selected shared action button; outside click
  dismisses the surface and the dismiss layer blocks click-through.
- Confirmed actions clear menu focus/context before invoking callbacks that may
  rebuild or recycle rows. Escape/outside dismissal restores the anchor when
  it remains valid.
- The global menu closes on Escape only while open.
- Disabled Paste is visibly non-interactable and is re-evaluated from live copy
  state each time the menu opens.
- Tooltips and passive indicators do not capture raycasts.

### Responsive reachability

- Real Canvas dimensions drive layout at scale 1..3 and tested 16:9, 4:3 and
  21:9 shapes.
- When the category panel cannot coexist with a 500-unit center, it collapses
  visually without changing the preference. The disabled expansion controls
  explain that more space or lower scale is required.
- Collapsed left/right rails retain explicit expansion controls.
- At the 138-unit minimum height, selection and Rename viewports retain at
  least one complete 22-unit row by source geometry.

## Tooltips

There is one reusable Canvas tooltip panel. Standard tooltips appear on hover
when enabled. Holding Shift selects shader-hint text when shader hints are
enabled; dragging suppresses tooltip display. The panel is clamped to the
Canvas, is 280 units wide, is capped at 360 units high and does not accept
raycasts.

This tooltip path is mouse-oriented. No equivalent screen-reader announcement
or guaranteed keyboard-focus tooltip has been implemented.

## Known accessibility limitations

| Limitation | Status |
| --- | --- |
| Built-in Arial only; no user font, font weight or localization choice | Confirmed |
| Fixed 22-unit rows and 20-unit compact buttons are below general 44-pixel touch-target guidance | Confirmed desktop-oriented design |
| Tooltip text size is 11 | Confirmed; visual readability pending |
| Generic input best-fit floor is 2 | Confirmed token; must be checked with long/extreme numeric text |
| No formal tab order or keyboard-only completion matrix | **PENDING** |
| Several menu buttons intentionally use `Navigation.None` | Confirmed; direct mouse and Cancel paths exist |
| No screen-reader labels/roles or tested assistive-technology bridge in Unity uGUI | Confirmed absence |
| Contrast gate covers core text/window pairs, not every hover/pressed/disabled composite | Partial source gate; **PENDING rendered review** |
| Text glyphs depend on built-in font rendering | **PENDING** |
| No high-contrast, light, color-blind or reduced-density theme | Confirmed absence |
| No animation was added, so there is no new motion preference; host/game motion is outside this UI scope | Confirmed scope |

## Required manual review

Use cases V43 and V48-V56 in `USER_VISUAL_REVIEW_CHECKLIST.md` for dropdown
text, color-picker/swatch behavior, contrast, long text, keyboard/focus,
glyphs, tooltips, high scale and aspect-ratio checks. Record the game,
resolution, UI scale, relevant config, screenshot and log excerpt. A case
remains **PENDING** until both the interaction and requested evidence are
attached.
