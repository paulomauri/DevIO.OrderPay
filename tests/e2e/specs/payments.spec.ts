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

    // Optimistic: on capture the row immediately shows the settling badge while the
    // PaymentConfirmed advance is still in flight (Outbox → broker, ~1-2 s lag).
    const settling = row.getByRole("status", { name: /payment processing/i });
    await expect(settling).toBeVisible();

    // The async advance lands → the settling badge clears and the real status shows.
    await expect(settling).toBeHidden({ timeout: 15_000 });
    await expect(row.getByText(/payment confirmed|^processing$/i)).toBeVisible();
  });
});
