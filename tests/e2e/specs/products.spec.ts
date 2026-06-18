import { test, expect } from "@playwright/test";

test.describe("products", () => {
  test("admin creates then deletes a product", async ({ page }) => {
    const stamp = Date.now();
    const name = `E2E Product ${stamp}`;
    const sku = `E2E-${stamp}`;

    await page.goto("/products");

    // Create
    await page.getByRole("button", { name: /new product/i }).click();
    const createDialog = page.getByRole("dialog");
    await createDialog.getByLabel("Name").fill(name);
    await createDialog.getByLabel("SKU").fill(sku);
    await createDialog.getByLabel("Description").fill("created by e2e");
    await createDialog.getByRole("button", { name: /^create$/i }).click();

    const row = page.getByRole("row", { name: new RegExp(sku) });
    await expect(row).toBeVisible();

    // Delete → confirm in the dialog
    await row.getByRole("button", { name: /^delete$/i }).click();
    const confirm = page.getByRole("dialog");
    await expect(confirm.getByText(/cannot be undone/i)).toBeVisible();
    await confirm.getByRole("button", { name: /^delete$/i }).click();

    await expect(page.getByRole("row", { name: new RegExp(sku) })).toHaveCount(0);
  });
});
