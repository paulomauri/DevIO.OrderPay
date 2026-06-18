# orderpay-web — Frontend

Next.js 16 (App Router) frontend for the OrderPay platform. Consumes the ASP.NET Core WebApi at `/api/v1/*`.

## Stack

- **Framework:** Next.js 16 (App Router, Turbopack)
- **Language:** TypeScript
- **Styling:** Styled Components v6 + design tokens (`src/styles/theme.ts`)
- **Auth:** NextAuth.js v4 — Keycloak OIDC (`orderpay-web` confidential client)
- **Server state:** React Query (`@tanstack/react-query`)
- **Client state:** Redux Toolkit (`uiSlice` + `cartSlice`)
- **HTTP:** Axios with JWT interceptor
- **Forms:** react-hook-form + zod
- **Tests:** Jest + React Testing Library

## Folder structure

```
src/
├── app/                          # Next.js routing only — thin pages
│   ├── (auth)/login/             # Public — login page
│   ├── (protected)/              # Requires session — sidebar + header layout
│   │   ├── dashboard/
│   │   ├── customers/[id]/
│   │   ├── products/
│   │   └── orders/[id]/
│   └── api/auth/[...nextauth]/   # NextAuth API handler
├── components/
│   ├── ui/                       # Primitives: Button, Input, Badge, Modal, Table, Card, Spinner
│   └── layout/                   # Sidebar, Header
├── features/                     # Domain-scoped components + hooks
│   ├── customers/
│   ├── products/
│   └── orders/
├── services/                     # Axios API calls — no React, no hooks
│   ├── api.ts                    # Axios instance + JWT interceptor
│   ├── customers.ts
│   ├── products.ts
│   └── orders.ts
├── store/                        # Redux Toolkit
│   ├── index.ts
│   ├── uiSlice.ts                # sidebar open/close, active modal
│   └── cartSlice.ts              # draft order items before submission
├── styles/
│   ├── theme.ts                  # Design tokens — colors, spacing, typography, shadows
│   ├── GlobalStyle.ts            # CSS reset + base styles
│   └── styled.d.ts               # DefaultTheme augmentation for full TS inference
├── types/
│   ├── next-auth.d.ts            # Session + JWT type augmentation
│   ├── customer.ts
│   ├── product.ts
│   └── order.ts
├── lib/
│   ├── auth.ts                   # NextAuth options — Keycloak provider + jwt/session callbacks
│   ├── providers.tsx             # Client wrapper — SessionProvider + ThemeProvider + GlobalStyle
│   └── registry.tsx              # Styled Components SSR registry for App Router
└── proxy.ts                      # Route protection — redirects unauthenticated users to /login
```

## Architecture rules

| Layer | Responsibility | Must NOT |
|---|---|---|
| `app/` | Routing, page composition | Contain business logic or API calls directly |
| `features/` | Domain components, hooks, forms | Import from other features |
| `components/ui/` | Generic primitives | Know about domain types |
| `services/` | Typed API calls via Axios | Import React or hooks |
| `store/` | UI + cart state only | Store server data (use React Query instead) |

## State management rule

- **React Query** — all server data (customers, orders, products fetched from the API)
- **Redux** — UI state (sidebar, modals) and cart (draft order being built)
- Never put API response data into Redux

## Forms & error handling

- Each form's zod schema **mirrors the backend FluentValidation rules** — keep them in sync or
  the API returns 400. Example: CPF is `.transform()`-stripped of punctuation and `.refine()`d
  to exactly 11 digits (matching `CustomerRequestValidator`: `Length(11)` + digits-only).
- On a failed mutation, surface the server's real message with `apiErrorMessage(err, fallback)`
  (`src/services/api.ts`) — it reads the first `ValidationProblemDetails.errors` entry, then
  `detail`/`title`, before falling back. Don't show a generic toast that hides the cause.
- A `401` from any API call triggers a single guarded `signOut()` → `/login` (see the Axios
  response interceptor in `src/services/api.ts`).

## Auth flow

```
User visits any route
  → proxy.ts checks NextAuth session cookie
  → No session → redirect to /login
  → Has session → allow through

/login → "Sign in with Keycloak" button
  → NextAuth redirects to Keycloak (orderpay-web client)
  → Keycloak authenticates → callback to /api/auth/callback/keycloak
  → NextAuth stores access token in encrypted session cookie
  → Redirect to /dashboard

API calls (Step 4)
  → Axios interceptor reads session.accessToken
  → Attaches as Authorization: Bearer <token>
```

## Environment variables

See `.env.example` at repo root (frontend section). Copy to `orderpay-web/.env.local`.

| Variable | Description |
|---|---|
| `NEXTAUTH_URL` | Frontend base URL |
| `NEXTAUTH_SECRET` | Random string for session cookie encryption |
| `KEYCLOAK_CLIENT_ID` | `orderpay-web` (confidential client) |
| `KEYCLOAK_CLIENT_SECRET` | From Keycloak Admin → Clients → orderpay-web → Credentials |
| `KEYCLOAK_ISSUER` | Docker stack: `http://id.localhost/realms/orderpay` · standalone `npm run dev`: `http://localhost:8085/realms/orderpay` |
| `NEXT_PUBLIC_API_URL` | Backend API base URL — empty string = same-origin (nginx routes `/api/`) |

> In the Docker stack `NEXTAUTH_URL=http://www.localhost` and `KEYCLOAK_ISSUER` uses the
> `id.localhost` subdomain so server-side token exchange and the browser hit the same issuer.

## Commands

```bash
# Development
npm run dev

# Production build
npm run build && npm start

# Tests
npm test

# Lint
npm run lint
```

## Build notes

- Styled Components compiler enabled via `next.config.ts` (`compiler.styledComponents: true`)
- `proxy.ts` at `src/proxy.ts` — Next.js 16 renamed middleware from `middleware.ts` to `proxy.ts`; exports `proxy` function + `config.matcher`
- SSR styles collected via `src/lib/registry.tsx` (required for Styled Components v6 + App Router)

## Test setup (Step 10-A)

- `jest.config.ts` — `ts-jest` transform (CommonJS), `jsdom` env, `@/` path alias; `ts-node` required so Jest can read the TS config
- `jest.setup.ts` — registers `@testing-library/jest-dom` + `jest-styled-components` matchers (`toHaveStyleRule` for asserting styled-components styles)
- `src/test/test-utils.tsx` — `renderWithProviders` wraps Redux + React Query (retry off) + `ThemeProvider`; re-exports RTL
- Tests colocated as `*.test.tsx`; `next-auth/react` `useSession` and service modules are mocked per-suite

## Phase 6 steps

| Step | Status |
|---|---|
| 1 — Bootstrap + folder structure | ✅ Done |
| 2 — Design tokens + Styled Components | ✅ Done |
| 3 — Authentication (NextAuth + Keycloak) | ✅ Done |
| 4 — API layer (Axios + React Query) | ✅ Done |
| 5 — Redux Toolkit (uiSlice + cartSlice) | ✅ Done |
| 6 — Pages + layout | ✅ Done |
| 7 — Components (ui primitives) | ✅ Done |
| 8 — Forms + validation | ✅ Done |
| 9 — Error handling + loading states | ✅ Done |
| 10-A — Unit/component tests (Jest + RTL) | ✅ Done |
| 10-B — E2E tests (Playwright) | ✅ Done — `tests/e2e/` at solution root, containerised runner |
