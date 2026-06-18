import type { APIRequestContext, Page } from "@playwright/test";
import { expect } from "@playwright/test";

export const ADMIN = { username: "admin@orderpay.com", password: "Mauri@22" };
export const CUSTOMER = { username: "user@orderpay.com", password: "User@123" };

export const ADMIN_STATE = "fixtures/.auth-admin.json";
export const CUSTOMER_STATE = "fixtures/.auth-customer.json";

const KEYCLOAK = "http://id.localhost/realms/orderpay";
const API = "http://www.localhost/api/v1";

/** Drive the real Keycloak login form, ending on /dashboard. */
export async function loginViaKeycloak(page: Page, username: string, password: string) {
  await page.goto("/login");
  await page.getByRole("button", { name: /sign in/i }).click();
  await page.waitForURL(/id\.localhost/, { timeout: 30_000 });
  await page.fill("#username", username);
  await page.fill("#password", password);
  await page.click("#kc-login");
  await page.waitForURL("**/dashboard", { timeout: 30_000 });
  await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
}

/** Direct-grant token from the swagger client (used only to seed test data via the API). */
export async function getApiToken(request: APIRequestContext): Promise<string> {
  const res = await request.post(`${KEYCLOAK}/protocol/openid-connect/token`, {
    form: {
      client_id: "orderpay-swagger",
      username: ADMIN.username,
      password: ADMIN.password,
      grant_type: "password",
    },
  });
  expect(res.ok(), `token request failed: ${res.status()}`).toBeTruthy();
  return (await res.json()).access_token as string;
}

type Seeded = { customerId: string; customerName: string; productId: string; productName: string };

/** Seed a customer + product through the API so order specs have something to select. */
export async function seedCustomerAndProduct(
  request: APIRequestContext,
  token: string,
  stamp: number,
): Promise<Seeded> {
  const auth = { Authorization: `Bearer ${token}` };

  const customerName = `E2E Customer ${stamp}`;
  const customerRes = await request.post(`${API}/customer`, {
    headers: auth,
    data: {
      name: customerName,
      email: `e2e_cust_${stamp}@test.com`,
      cpf: String(stamp).slice(-11),
      mobile: "11999990000",
    },
  });
  expect(customerRes.ok(), `seed customer failed: ${customerRes.status()}`).toBeTruthy();
  const customerId = (await customerRes.json()).id as string;

  const productName = `E2E Product ${stamp}`;
  const productRes = await request.post(`${API}/product`, {
    headers: auth,
    data: { name: productName, sku: `E2E-${stamp}`, description: "seeded by e2e" },
  });
  expect(productRes.ok(), `seed product failed: ${productRes.status()}`).toBeTruthy();
  const productId = (await productRes.json()).id as string;

  return { customerId, customerName, productId, productName };
}

/** Seed an order via the API and return its id (used by the status-update spec). */
export async function seedOrder(
  request: APIRequestContext,
  token: string,
  customerId: string,
  productId: string,
): Promise<string> {
  const res = await request.post(`${API}/order`, {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      customerId,
      details: "seeded by e2e",
      items: [{ productId, quantity: 1, price: 25, discount: 0 }],
    },
  });
  expect(res.ok(), `seed order failed: ${res.status()}`).toBeTruthy();
  return (await res.json()).id as string;
}
