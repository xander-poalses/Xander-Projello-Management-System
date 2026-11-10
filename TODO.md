# Projello TODO

Taken from the November 2026 audit (security, database, Hallmark and CLAUDE.md). Work top to bottom.

## 1. Security: critical

- [x] Fix the 2FA bypass: `POST /api/auth/verify-2fa` accepts only an email and a code. Have login return a short-lived 2FA challenge token and require it at verify (`AuthController.cs:225`)
- [x] Add rate limiting to login, verify-2fa and register (10 per minute per IP)
- [x] Add Identity lockout after repeated failed logins and 2FA codes (5 failures, 15 minutes)
- [x] Rotate the JWT key and move it out of `appsettings.json` into environment variables (Render generates `Jwt__Key`; local dev uses user-secrets)
- [x] Remove the hardcoded fallback JWT key in `Program.cs` and fail at startup if none is set
- [x] Move the Postgres password in `database/docker-compose.yml` into an `.env` file kept out of git

## 2. Security: high

- [x] `GET /api/siteupdates`: check project membership before returning updates (`SiteUpdatesController.cs:29`)
- [x] Site update delete: limit Owner (role 4) to their own company (`SiteUpdatesController.cs:204`)
- [x] Admin (role 1) is a company admin: every controller now scopes to the caller's company (`Services/CompanyScope.cs`), and nobody can change their own role or assign one at or above their own
- [x] Put `GET /api/auth/2fa-status` behind auth, or remove it (it reveals which accounts exist)

## 3. Security: medium

- [x] Re-check the user's role from the database on every request (deleted users and role changes take effect immediately)
- [x] Move the JWT out of `localStorage` (Electron `safeStorage` through preload; old tokens move over on first read)
- [x] Review `dangerouslySetInnerHTML` in `settings.tsx:299` and sanitise it or remove it (now an `<img>` like the other avatars)
- [x] Encrypt `TwoFactorSecret` at rest (AES-GCM, key from `TwoFactor__EncryptionKey`)
- [x] Require a password or a current code to disable 2FA
- [x] Tighten CORS: list exact origins instead of matching prefixes like `http://localhost*`, `file://` and `app://`
- [x] Strengthen password rules: minimum length 12
- [-] Check new passwords against a breached-password list (needs a decision: Have I Been Pwned's k-anonymity API sends 5 characters of a hash to a third party)
- [x] Remove `/api/webrtc/test` and turn off `EnableDetailedErrors` outside development
- [x] Check the Electron settings in `index.ts`: `contextIsolation`, `nodeIntegration: false`, `sandbox`, CSP (set explicitly; DevTools no longer open in the packaged app)
- [x] Drop `'unsafe-eval'` from the production CSP once the one `new Function` in the bundle is traced to its library

## 4. Database

- [x] Add foreign keys to `ProjectUpdates`, `UpdateReactions` and `UpdateComments` (to Projects and Users)
- [x] Add indexes on `ProjectUpdates.ProjectId`, `UpdateReactions.UpdateId` and `UpdateComments.UpdateId`
- [x] Add a unique index on `CompanyInvites.Code`
- [x] Review the 15 cascade deletes: Client -> Projects and Company -> Clients are now `Restrict`; project contents still cascade on a deliberate project delete (soft delete is below)
- [x] Choose one place to store `TotalPaid` and `Outstanding` (Client or Project), work out the other from it, and set decimal precision
- [x] Wrap multi-step writes in transactions (for example, register plus marking the invite used)
- [x] Add soft delete (`DeletedAt`) and an audit trail for key records
- [x] Decide how migrations run in production (in the deploy step or at startup)

## 5. Repo hygiene

- [x] Replace the blanket `*.json` in `.gitignore` with specific `appsettings.*.json` rules
- [x] Untrack `TestResults/`, `coveragereport/`, `full_test_report.txt` and `*.lscache`, and ignore them
- [x] Merge the duplicate DTOs (`ClientCreateDto`/`CreateClientDto`, `TaskCreateDto`/`CreateTaskDto`)
- [x] Merge `Verify2FA.tsx`/`Verify2FAPage.tsx` and the two `ICallAuthorization*` interfaces
- [x] Hide the sidebar on `/register-company` (`App.tsx`)

## 6. Design system and Hallmark polish

- [x] Move all raw colours and fonts into the `:root` tokens (448 findings; start with `Clients.css`, `WorkerClientAddModal.css`, `CallOverlay.css`, `dashboard.css`)
- [x] Add system colour tokens: success, warning, error, info, disabled, border, control-border, in light and dark
- [ ] Put spacing, radius and type sizes on a token scale (84 spacing findings)
- [x] Replace `transition: all` with named properties (AddButton, FilterButton, SortButton, CallOverlay, history, Clients)
- [x] Replace `100vw` and `overflow-x: hidden` with `100%` and `overflow-x: clip`
- [x] Replace the emoji icons (👍, ☎, ⚠️) with one icon set
- [x] Remove the "ARCHIVED SECURELY" eyebrow and use sentence case for table heads and `.project-tag`
- [x] Remove em dashes from copy (AddProjectMemberModal, DiscussionTab, single-project-view)
- [x] Change "View All →" to a plain verb label
- [x] Video grid: use `minmax(0, 1fr)`, swap pure `#000` for a token, make autoplay video muted
- [x] Add `aria-hidden` or a label to the splash `<canvas>`

## 7. Accessibility (WCAG 2.2 AA)

- [x] Fix contrast: login and signup cancel buttons (2.32:1), modal default buttons (2.32 to 2.86:1), Clients warning button (3.19:1), error text (4.36:1)
- [x] Add `:focus-visible` outlines to every interactive element (only 2 exist)
- [x] Use an outline for the input focus state, not a border change
- [x] Add a `prefers-reduced-motion` fallback for all animation
- [ ] Add roles, labels and states (expanded, selected, busy, invalid) to custom controls
- [x] Check modals trap focus and return it on close
- [ ] Test by hand with the keyboard, a screen reader and text scaling

## 8. Required UI patterns (CLAUDE.md)

- [x] Skeleton loaders on every page that loads data
- [x] Stale-while-revalidate data fetching (own small cache on Jello Jobs and the project view; other pages still fetch fresh)
- [x] Optimistic updates for reactions, rolling back on failure
- [x] Optimistic updates for comments and task status
- [x] Idempotency keys on every create or submit request (with a backend check)
- [x] Undo toasts instead of the 5 `confirm()` dialogs, where it's safe
- [x] Busy states on buttons: loading labels and repeat-click blocking exist on sign-in, create and post; no spinners yet
- [x] Press feedback: `transform: scale(0.97)`
- [x] View transitions between pages
- [x] Ctrl+K command menu with shortcut hints
- [x] Blurry image placeholders for site update photos
- [x] `font-variant-numeric: tabular-nums` on money, counts and tables
- [x] `text-wrap: balance` on headings
- [x] Prefetch likely next pages (for example, the project view from the dashboard)

## 9. Verification still to do

- [ ] Install `frontend/node_modules`, run the app and re-run Hallmark with `--render` for screenshots
- [ ] Check layouts at 320, 375, 414 and 768px
- [ ] Run backend and frontend tests after each fix batch (1 backend test was already failing: TasksController not-found; frontend has 864 existing type errors)
- [ ] Fix the jest setup: add a `TextEncoder` polyfill so Clients, History and Workers suites run (react-router needs it)
- [ ] Update the 5 failing ManagementPage tests (they still expect payments in the project edit modal)
