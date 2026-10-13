X:\Developments\motivation\BUGS\image.png  this © 2025 - POMS - Prosthetic, Orthotic & Spinal Patient Management System ිිැි section in every form is not allined propery and no fixed possition  fix it and check overall UI for the
  look . after we need user creation and access management module. add it . after that give a setting to change the theme of the system dark, light etc .but also keep the curent theme too.

## Issue 3 — Administration, reference data, and patient photos

- [x] **3.1 Reference data navigation:** Move each master-data category to its own page under Reference Data.
- [x] **3.2 Reference data maintenance:** Add search, status filtering, create, edit, deactivate/reactivate, and safe deletion with in-use protection.
- [x] **3.3 User administration:** Keep user creation and access management under Administration; create working Identity credentials, role controls, locking, profile editing, and password reset.
- [x] **3.4 Patient photo registration:** Restore optional JPG/PNG photo upload during patient registration with file validation.
- [x] **3.5 Patient photo presentation:** Replace initials with authenticated photo thumbnails in patient lists and show a larger photo on the patient record.

## Issue 4 — Professional UI redesign and PRD gap review (2026-10-13)

- [x] **4.1 Design system:** New tokens, typography, buttons, forms, tables, modals, toasts and empty states shared by every screen; the document upload row (`Requirement/image copy.png`) is now aligned.
- [x] **4.2 Themes:** Settings → Theme offers Light, Dark, Classic (the original navy sidebar) and System, plus Comfortable/Compact density. Applied before first paint.
- [x] **4.3 Footer:** One aligned footer at the bottom of the workspace on every page.
- [x] **4.4 Dashboard (PRD §5):** PRD title, Total patients / Active records / Active patients / Today's activities cards with drill-down, six quick actions, recent activities feed, and today's schedule without deliveries or follow-ups.
- [x] **4.5 Registration (PRD §6):** Five-step wizard with referral source (+ other), referrer, travel time/distance, multiple telephone numbers with date confirmed and person checked, guardian phone and mobile, nationality list, review with photo zoom, Enter / Alt+arrow navigation. Editing uses the same wizard.
- [x] **4.6 Patient folder (PRD §7):** Overview (identity, contacts, guardian, referral, registration), clinical records with quick fitting/delivery/follow-up actions, combined clinical history with print buttons, documents, print forms.
- [x] **4.7 Clinical Records page:** Active-records drill-down with PNO, location, assessment type, prosthesis/orthosis, last activity and stage.
- [x] **4.8 Access fix:** Clinical and management users can open the Patient Folder (previously 403 for non-registration roles).
- [x] **4.9 Edit fix:** Editing a patient linked to a staff account no longer fails with "The selected staff account is unavailable".
- [x] **4.10 Reports:** Grouped report list, Local/foreign, assessment type, prosthetic/orthotic type and year filters, in-table search, CSV and PDF export.
