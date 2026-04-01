/**
 * auth.setup.ts
 *
 * Runs once before all tests. Signs in the Free test user and a Pro test user
 * via the dev-only /Auth/TestLogin endpoint, saving session cookies so tests
 * don't have to re-authenticate.
 */
import { test as setup, expect } from '@playwright/test';
import path from 'path';

const BASE_URL = process.env.BASE_URL || 'http://localhost:5023';

export const FREE_USER_EMAIL = 'e2e-free@dwellscript.test';
export const PRO_USER_EMAIL  = 'e2e-pro@dwellscript.test';

export const FREE_USER_STATE = path.join(__dirname, '.auth/user.json');
export const PRO_USER_STATE  = path.join(__dirname, '.auth/pro-user.json');

setup('sign in free user', async ({ page }) => {
  const loginRes = await page.request.post(`${BASE_URL}/Auth/TestLogin`, {
    form: { email: FREE_USER_EMAIL },
  });
  expect(loginRes.ok(), `TestLogin failed: ${await loginRes.text()}`).toBeTruthy();

  // Ensure user is on Free tier for gate/quota tests
  await page.request.post(`${BASE_URL}/Auth/TestSetTier`, {
    form: { email: FREE_USER_EMAIL, tier: 'Free' },
  });

  await page.goto(`${BASE_URL}/Property`);
  await page.waitForURL(/\/Property/);
  await page.context().storageState({ path: FREE_USER_STATE });
});

setup('sign in pro user', async ({ page }) => {
  const loginRes = await page.request.post(`${BASE_URL}/Auth/TestLogin`, {
    form: { email: PRO_USER_EMAIL },
  });
  expect(loginRes.ok()).toBeTruthy();

  // Elevate to Pro tier
  const tierRes = await page.request.post(`${BASE_URL}/Auth/TestSetTier`, {
    form: { email: PRO_USER_EMAIL, tier: 'Pro' },
  });
  expect(tierRes.ok()).toBeTruthy();

  await page.goto(`${BASE_URL}/Property`);
  await page.waitForURL(/\/Property/);
  await page.context().storageState({ path: PRO_USER_STATE });
});
