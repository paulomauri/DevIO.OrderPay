import { defineConfig, devices } from "@playwright/test";

// Full running stack required (docker compose up -d). BASE_URL defaults to the
// nginx entry point; the containerised runner reaches it via network aliases.
const BASE_URL = process.env.BASE_URL ?? "http://www.localhost";

export default defineConfig({
  testDir: "./specs",
  // Tests mutate a shared database, so run them serially.
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  timeout: 30_000,
  expect: { timeout: 10_000 },
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: BASE_URL,
    headless: true,
    screenshot: "only-on-failure",
    trace: "on-first-retry",
  },
  projects: [
    // 1) Log in once as admin + customer and persist the browser session.
    { name: "setup", testMatch: /auth\.setup\.ts/ },

    // 2) All specs run with the admin session by default; individual specs
    //    override storageState when they need the customer session or none.
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"],
        storageState: "fixtures/.auth-admin.json",
      },
      dependencies: ["setup"],
    },
  ],
});
