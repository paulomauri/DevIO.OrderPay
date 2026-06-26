import { test, expect } from "@playwright/test";
import { getApiToken, seedCustomerAndProduct, seedOrder } from "./helpers";

// A fresh (Pending) order to pay through the UI.
let seededOrderId: string;

test.beforeAll(async ({ request }) => {
  const stamp = Date.now();
  const token = await getApiToken(request);
  const seeded = await seedCustomerAndProduct(request, token, stamp);
  seededOrderId = await seedOrder(request, token, seeded.customerId, seeded.productId);
});

test.describe("payments", () => {
  test("admin pays an order by card and the badge becomes Payment Confirmed", async ({ page }) => {
    await page.goto("/orders");

    const row = page.getByRole("row", { name: new RegExp(seededOrderId.slice(0, 8)) });
    await expect(row).toBeVisible();
    await row.getByRole("button", { name: /^pay$/i }).click();

    const dialog = page.getByRole("dialog");
    await dialog.getByLabel(/card brand/i).fill("Visa");
    await dialog.getByLabel(/card number/i).fill("4242");
    await dialog.getByLabel(/expiry/i).fill("12/27");
    await dialog.getByRole("button", { name: /^pay \$/i }).click();

    // Capture advances the order; the row badge flips to the friendly label.
    await expect(row.getByText("Payment Confirmed")).toBeVisible();
  });
});
