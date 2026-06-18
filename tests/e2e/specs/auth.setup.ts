import { test as setup } from "@playwright/test";
import { ADMIN, CUSTOMER, ADMIN_STATE, CUSTOMER_STATE, loginViaKeycloak } from "./helpers";

// These run first (the "setup" project) and persist a logged-in browser session
// for each role, so the real specs skip the Keycloak login form.

setup("authenticate as admin", async ({ page }) => {
  await loginViaKeycloak(page, ADMIN.username, ADMIN.password);
  await page.context().storageState({ path: ADMIN_STATE });
});

setup("authenticate as customer", async ({ page }) => {
  await loginViaKeycloak(page, CUSTOMER.username, CUSTOMER.password);
  await page.context().storageState({ path: CUSTOMER_STATE });
});
