import { test, expect } from "@playwright/test";
import { CUSTOMER_STATE } from "./helpers";

test.describe("customers", () => {
  test("admin creates a customer and the row appears in the table", async ({ page }) => {
    const stamp = Date.now();
    const name = `E2E Customer ${stamp}`;

    await page.goto("/customers");
    await page.getByRole("button", { name: /new customer/i }).click();

    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Name").fill(name);
    await dialog.getByLabel("Email").fill(`e2e_${stamp}@test.com`);
    await dialog.getByLabel("CPF").fill(String(stamp).slice(-11));
    await dialog.getByLabel("Mobile").fill("11988887777");
    await dialog.getByRole("button", { name: /^create$/i }).click();

    // Row shows up after the mutation invalidates the query.
    await expect(page.getByRole("cell", { name, exact: true })).toBeVisible();
  });

  test.describe("customer role (AdminOnly gating)", () => {
    test.use({ storageState: CUSTOMER_STATE });

    test("sees the table but no Create or Delete buttons", async ({ page }) => {
      await page.goto("/customers");
      await expect(page.getByRole("heading", { name: "Customers", level: 1 })).toBeVisible();

      await expect(page.getByRole("button", { name: /new customer/i })).toHaveCount(0);
      await expect(page.getByRole("button", { name: /^delete$/i })).toHaveCount(0);
    });
  });
});
