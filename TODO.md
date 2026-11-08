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
- [ ] Site update delete: limit Owner (role 4) to their own company (`SiteUpdatesController.cs:204`)
- [ ] Decide whether Admin (role 1) is a platform admin or a company admin. If it's a company admin, add company filters to `UsersController` delete, role change and profile reads
- [x] Put `GET /api/auth/2fa-status` behind auth, or remove it (it reveals which accounts exist)

## 3. Security: medium

- [ ] Re-check the user's role from the database on sensitive actions, or shorten the token lifetime and add refresh tokens
- [ ] Move the JWT out of `localStorage` (Electron `safeStorage` through preload, or an httpOnly cookie)
- [ ] Review `dangerouslySetInnerHTML` in `settings.tsx:299` and sanitise it or remove it
- [ ] Encrypt `TwoFactorSecret` at rest (Data Protection API)
- [ ] Require a password or a current code to disable 2FA
- [ ] Tighten CORS: list exact origins instead of matching prefixes like `http://localhost*`, `file://` and `app://`
- [ ] Strengthen password rules (length 12+, check against breached-password lists)
- [ ] Remove `/api/webrtc/test` and turn off `EnableDetailedErrors` outside development
- [ ] Check the Electron settings in `index.ts`: `contextIsolation`, `nodeIntegration: false`, `sandbox`, CSP

## 4. Database

- [ ] Add foreign keys to `ProjectUpdates`, `UpdateReactions` and `UpdateComments` (to Projects and Users)
- [ ] Add indexes on `ProjectUpdates.ProjectId`, `UpdateReactions.UpdateId` and `UpdateComments.UpdateId`
- [ ] Add a unique index on `CompanyInvites.Code`
- [ ] Review the 15 cascade deletes and switch to `Restrict` or soft delete where losing data would be a problem (clients, projects)
- [ ] Choose one place to store `TotalPaid` and `Outstanding` (Client or Project), work out the other from it, and set decimal precision
- [ ] Wrap multi-step writes in transactions (for example, register plus marking the invite used)
- [ ] Add soft delete (`DeletedAt`) and an audit trail for key records
- [ ] Decide how migrations run in production (in the deploy step or at startup)

## 5. Repo hygiene

- [ ] Replace the blanket `*.json` in `.gitignore` with specific `appsettings.*.json` rules
- [ ] Untrack `TestResults/`, `coveragereport/`, `full_test_report.txt` and `*.lscache`, and ignore them
- [ ] Merge the duplicate DTOs (`ClientCreateDto`/`CreateClientDto`, `TaskCreateDto`/`CreateTaskDto`)
- [ ] Merge `Verify2FA.tsx`/`Verify2FAPage.tsx` and the two `ICallAuthorization*` interfaces
- [x] Hide the sidebar on `/register-company` (`App.tsx`)

## 6. Design system and Hallmark polish

- [ ] Move all raw colours and fonts into the `:root` tokens (448 findings; start with `Clients.css`, `WorkerClientAddModal.css`, `CallOverlay.css`, `dashboard.css`)
- [ ] Add system colour tokens: success, warning, error, info, disabled, border, control-border, in light and dark
- [ ] Put spacing, radius and type sizes on a token scale (84 spacing findings)
- [ ] Replace `transition: all` with named properties (AddButton, FilterButton, SortButton, CallOverlay, history, Clients)
- [ ] Replace `100vw` and `overflow-x: hidden` with `100%` and `overflow-x: clip`
- [ ] Replace the emoji icons (👍, ☎, ⚠️) with one icon set
- [ ] Remove the "ARCHIVED SECURELY" eyebrow and use sentence case for table heads and `.project-tag`
- [ ] Remove em dashes from copy (AddProjectMemberModal, DiscussionTab, single-project-view)
- [ ] Change "View All →" to a plain verb label
- [ ] Video grid: use `minmax(0, 1fr)`, swap pure `#000` for a token, make autoplay video muted
- [ ] Add `aria-hidden` or a label to the splash `<canvas>`

## 7. Accessibility (WCAG 2.2 AA)

- [ ] Fix contrast: login and signup cancel buttons (2.32:1), modal default buttons (2.32 to 2.86:1), Clients warning button (3.19:1), error text (4.36:1)
- [ ] Add `:focus-visible` outlines to every interactive element (only 2 exist)
- [ ] Use an outline for the input focus state, not a border change
- [ ] Add a `prefers-reduced-motion` fallback for all animation
- [ ] Add roles, labels and states (expanded, selected, busy, invalid) to custom controls
- [ ] Check modals trap focus and return it on close
- [ ] Test by hand with the keyboard, a screen reader and text scaling

## 8. Required UI patterns (CLAUDE.md)

- [ ] Skeleton loaders on every page that loads data
- [ ] Stale-while-revalidate data fetching (for example, TanStack Query)
- [ ] Optimistic updates for reactions, comments and task status, rolling back on failure
- [ ] Idempotency keys on every create or submit request (with a backend check)
- [ ] Undo toasts instead of the 5 `confirm()` dialogs, where it's safe
- [ ] Busy states on buttons: loading label and spinner, block repeat clicks, keep input after a failure
- [ ] Press feedback: `transform: scale(0.97)`
- [ ] View transitions between pages
- [ ] Ctrl+K command menu with shortcut hints
- [ ] Blurry image placeholders for site update photos
- [ ] `font-variant-numeric: tabular-nums` on money, counts and tables
- [ ] `text-wrap: balance` on headings
- [ ] Prefetch likely next pages (for example, the project view from the dashboard)

## 9. Verification still to do

- [ ] Install `frontend/node_modules`, run the app and re-run Hallmark with `--render` for screenshots
- [ ] Check layouts at 320, 375, 414 and 768px
- [ ] Run backend and frontend tests after each fix batch (2 backend tests were already failing: TasksController not-found, ClientsController other-company; frontend has 864 existing type errors)
