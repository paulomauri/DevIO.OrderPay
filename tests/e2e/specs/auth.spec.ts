import { test, expect } from "@playwright/test";

test.describe("authentication", () => {
  test("admin session lands on the dashboard", async ({ page }) => {
    await page.goto("/dashboard");
    await expect(page).toHaveURL(/\/dashboard/);
    await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
  });

  test.describe("without a session", () => {
    test.use({ storageState: { cookies: [], origins: [] } });

    test("unauthenticated user is redirected to login, then to Keycloak", async ({ page }) => {
      await page.goto("/dashboard");
      await expect(page).toHaveURL(/\/login/);

      await page.getByRole("button", { name: /sign in/i }).click();
      await expect(page).toHaveURL(/id\.localhost/);
      await expect(page.locator("#username")).toBeVisible();
    });
  });
});
