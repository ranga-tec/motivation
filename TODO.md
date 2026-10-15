# POMS TODO

Status: 2026-10-15. Production: <https://169-58-173-191.sslip.io> (Contabo).

## Open

- [ ] **Organisation domain.** Point a domain at `169.58.173.191` and set `POMS_DOMAIN`
      ([`DEPLOY_CONTABO.md`](DEPLOY_CONTABO.md) §8).
- [ ] **Off-site backups.** Copy `/var/backups/poms` off the Contabo server on a schedule, and
      rehearse one restore.
- [ ] **Automated deployment.** GitHub Actions: test, then SSH to the server and rebuild.
- [ ] **PRD confirmations (§25).** PNO format, Colombo flag code, yearly reset rule, final Main
      Problem, Cause/Pathology and Spinal Orthosis lists.
- [ ] **Prescription-type filter** on reports (PRD §17.3).
- [ ] **Self-service password change** for staff.
- [ ] **`ComponentCatalog` admin screen.**
- [ ] **Remove legacy `Poms.Web` MVC UI** once it is no longer needed for reference.

## Done

- [x] 2026-10-15 — Previous cloud hosts removed from code and documentation; Contabo is the only host.
- [x] 2026-10-14 — Contabo hosting with HTTPS, nightly backups, firewall; fixed HTTP/3 blank page.
- [x] 2026-10-13 — UI redesign, themes, PRD dashboard/registration/folder gaps, access and edit
      fixes. Details in [`BUGS/bugs.md`](BUGS/bugs.md) Issue 4.
- [x] Hard-coded demonstration passwords are never created in production; the first
      administrator comes from secrets.
