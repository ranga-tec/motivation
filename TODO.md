# POMS TODO

## Done — shipped in `3f3ad71` (2026-09-02)

All nine items below were requested against the registration screen shown in
`Requirement/image.png` and are live at
<https://poms-motivation-production.up.railway.app>.

- [x] **Forms should fit on screen; headers and sections take too much vertical space.**
      The wizard named the current step three times over (modal header, sticky stepper, and a
      per-step icon plus "STEP N" eyebrow), leaving ~715px of chrome above the first input.
      Removed the duplicated layer and compacted the optional photo card — about 175px reclaimed.
      *Asked whether to add an admin font-size setting: recommended against it.* Scaling type down
      shrinks labels and inputs too, which is worse on shared clinic screens, and browsers already
      offer per-site zoom. The problem was duplicated structure, not type size.
- [x] **Fix tab order and shortcuts; tab should not start on the page header text.**
      Step changes and modal opens focused a heading. Both now focus the first real field, skipping
      the visually-hidden photo input. Added `Enter` to advance a step and `Alt`+arrow to move
      between steps, with a hint in the wizard footer.
- [x] **Final confirmation page should show the patient image, click to zoom.**
      The review step shows the photo; clicking enlarges it, `Esc` or click-away closes.
- [x] **Email address should support N/A.** Accepts `N/A`, `n/a`, and `NA`, still rejecting
      malformed addresses. Stored canonically as `N/A`.
- [x] **Identification Type should support N/A and disable the number field.**
      Added `IdentificationType.NotApplicable`. Selecting it clears and disables the number field.
      Duplicate detection now ignores blank identification numbers, which would otherwise have
      matched every such patient to every other.
- [x] **"Records" should be "Clinical Records".** Renamed in navigation, page titles, and the
      patient folder tab.
- [x] **Subtype text only visible when relevant.** The "Subtype" label no longer lingers on screen
      after its select is hidden.
- [x] **New Appointment: patient search on type; record dropdown should show more detail.**
      New `SearchPatients` endpoint drives a type-ahead picker instead of a select containing every
      patient. The record dropdown now shows the centre and assessment/fitting/delivery counts
      rather than a bare date.
- [x] **Handled By should be a searchable dropdown of available clinicians.**
      Now a searchable list with prosthetists and orthotists first. It still accepts a typed name
      so staff without an account can be recorded.

## Open

- [ ] **Verify appointment patient search against real data.** The endpoint returns 200 and valid
      JSON, but the local development database has no patients, so matching is unexercised.
- [ ] **Remove the hard-coded seed passwords.** They are committed to this repository and work on
      the live site today. This is the top blocker before real patient data. See
      [`AGENT_HANDOVER.md`](AGENT_HANDOVER.md) section 6.
- [ ] **`ComponentCatalog` has no admin screen** — the same gap the device catalogue had before it
      was added.
- [ ] **Decide whether the modal header merges into the sticky stepper bar** for roughly 70px more,
      if the current density still feels tight in daily use.

See [`AGENT_HANDOVER.md`](AGENT_HANDOVER.md) section 8 for the wider list of known issues
(no EF migrations on PostgreSQL, no backups, shallow `/health`, AutoMapper advisory, hosting
region).
