# orderpay-web

Next.js 16 (App Router) frontend for the **DevIO.OrderPay** platform. It authenticates
against Keycloak (OIDC) and consumes the ASP.NET Core WebApi at `/api/v1/*`.

> Architecture, folder conventions, and layer rules are documented in
> [CLAUDE.md](./CLAUDE.md). This README covers how to run and build it.

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

## Run

The app is normally run as part of the full stack via Docker Compose at the repo root
(`docker compose up -d`), reachable at **http://www.localhost** through nginx.

To run the frontend on its own against an already-running backend + Keycloak:

```bash
npm install
cp .env.local.example .env.local   # if present; otherwise create it (see below)
npm run dev                        # http://localhost:3000
```

## Environment

Create `orderpay-web/.env.local`. For the Docker stack (reached via nginx) the URLs use the
`*.localhost` subdomains; for standalone `npm run dev` use `localhost:3000` / `localhost:8085`.

| Variable | Docker stack value | Notes |
|---|---|---|
| `NEXTAUTH_URL` | `http://www.localhost` | frontend base URL (`http://localhost:3000` for standalone) |
| `NEXTAUTH_SECRET` | any random string | session cookie encryption — `getToken()` needs it explicitly |
| `KEYCLOAK_CLIENT_ID` | `orderpay-web` | confidential client |
| `KEYCLOAK_CLIENT_SECRET` | from Keycloak → Clients → orderpay-web → Credentials | |
| `KEYCLOAK_ISSUER` | `http://id.localhost/realms/orderpay` | `http://localhost:8085/realms/orderpay` for standalone |
| `NEXT_PUBLIC_API_URL` | `` (empty) | empty = same-origin; nginx routes `/api/` to the webapi |

## Scripts

```bash
npm run dev       # dev server (Turbopack)
npm run build     # production build (runs the TypeScript type-check)
npm start         # serve the production build
npm test          # Jest + React Testing Library
npm run lint      # ESLint
```

## Build the Docker image

```bash
docker build \
  --build-arg NEXT_PUBLIC_API_URL="" \
  -t paulomauri/orderpay-web:latest \
  -f Dockerfile .
```

`NEXT_PUBLIC_API_URL` is baked at build time; empty string means same-origin API calls
routed by nginx.

## Notes

- **Routing protection:** `src/proxy.ts` (Next.js 16 renamed `middleware.ts` → `proxy.ts`)
  redirects unauthenticated users to `/login`.
- **Form validation mirrors the backend.** Each form's zod schema matches the API's
  FluentValidation rules (e.g. CPF is stripped of punctuation and required to be exactly 11
  digits). On error, `apiErrorMessage()` (`src/services/api.ts`) surfaces the backend's real
  `ValidationProblemDetails` message instead of a generic toast.
- **401 handling:** the Axios response interceptor signs the user out once (guarded against
  concurrent calls) and sends them to `/login`.
- **SSR styles:** collected via `src/lib/registry.tsx` (required for Styled Components v6 +
  App Router).
