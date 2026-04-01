import { Page, APIRequestContext } from '@playwright/test';

const BASE_URL = process.env.BASE_URL || 'http://localhost:5023';

export const FREE_USER_EMAIL = 'e2e-free@dwellscript.test';
export const PRO_USER_EMAIL  = 'e2e-pro@dwellscript.test';

/** Sign in a user via the dev-only TestLogin endpoint. */
export async function signIn(request: APIRequestContext, email: string): Promise<void> {
  const res = await request.post(`${BASE_URL}/Auth/TestLogin`, {
    form: { email },
  });
  if (!res.ok()) throw new Error(`TestLogin failed for ${email}: ${await res.text()}`);
}

/** Set a user's subscription tier via the dev-only TestSetTier endpoint. */
export async function setTier(
  request: APIRequestContext,
  email: string,
  tier: 'Free' | 'Starter' | 'Pro',
): Promise<void> {
  const res = await request.post(`${BASE_URL}/Auth/TestSetTier`, {
    form: { email, tier },
  });
  if (!res.ok()) throw new Error(`TestSetTier failed: ${await res.text()}`);
}

/** Navigate to a page, waiting until network is idle. */
export async function goto(page: Page, path: string): Promise<void> {
  await page.goto(`${BASE_URL}${path}`, { waitUntil: 'networkidle' });
}
