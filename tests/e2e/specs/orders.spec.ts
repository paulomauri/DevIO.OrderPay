import { test, expect } from "@playwright/test";
import { getApiToken, seedCustomerAndProduct, seedOrder } from "./helpers";

// Orders need an existing customer + product to select, so we seed them through
// the API. The order creation + status update themselves are driven via the UI.
let customerName: string;
let productName: string;
let seededOrderId: string;

test.beforeAll(async ({ request }) => {
  const stamp = Date.now();
  const token = await getApiToken(request);
  const seeded = await seedCustomerAndProduct(request, token, stamp);
  customerName = seeded.customerName;
  productName = seeded.productName;
  // A second, pre-made order used by the status-update test.
  seededOrderId = await seedOrder(request, token, seeded.customerId, seeded.productId);
});

test.describe("orders", () => {
  test("admin creates an order which appears with a Pending badge", async ({ page }) => {
    await page.goto("/orders");
    await page.getByRole("button", { name: /new order/i }).click();

    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Customer").selectOption({ label: customerName });
    await dialog.getByLabel("Product").selectOption({ label: productName });
    await dialog.getByLabel("Qty").fill("2");
    await dialog.getByLabel("Price").fill("19.90");
    await dialog.getByRole("button", { name: /create order/i }).click();

    // At least one Pending badge is now in the table.
    await expect(page.getByText("Pending").first()).toBeVisible();
  });

  test("admin updates an order status and the badge changes", async ({ page }) => {
    await page.goto("/orders");

    const row = page.getByRole("row", { name: new RegExp(seededOrderId.slice(0, 8)) });
    await expect(row).toBeVisible();
    await row.getByRole("button", { name: /^status$/i }).click();

    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("New status").selectOption({ label: "AwaitingPayment" });
    await dialog.getByRole("button", { name: /^update$/i }).click();

    // Badge renders the friendly label after the transition.
    await expect(row.getByText("Awaiting Payment")).toBeVisible();
  });
});
