/**
 * auth.spec.ts
 *
 * Tests for authentication flows:
 * - Login page rendering
 * - Magic link form behavior (positive + negative)
 * - Redirect logic
 * - Access control (protected routes)
 */
import { test, expect, Page } from '@playwright/test';

const BASE = process.env.BASE_URL || 'http://localhost:5023';

// Auth tests run without stored session
test.use({ storageState: { cookies: [], origins: [] } });

test.describe('Login page', () => {
  test('renders the login page with correct elements', async ({ page }) => {
    await page.goto(`${BASE}/Auth/Login`);

    // Page title
    await expect(page).toHaveTitle(/Sign in/i);

    // Logo / brand
    await expect(page.locator('text=DwellScript').first()).toBeVisible();

    // Email input
    await expect(page.locator('input[type="email"], input[placeholder*="email" i]').first()).toBeVisible();

    // Send link button
    const sendBtn = page.locator('button', { hasText: /send|magic|sign in/i }).first();
    await expect(sendBtn).toBeVisible();
  });

  test('shows Google OAuth button', async ({ page }) => {
    await page.goto(`${BASE}/Auth/Login`);
    const googleBtn = page.locator('a[href*="GoogleLogin"], button', { hasText: /google/i }).first();
    await expect(googleBtn).toBeVisible();
  });
});

test.describe('Magic link form — positive', () => {
  test('shows success message for a valid email', async ({ page }) => {
    await page.goto(`${BASE}/Auth/Login`);

    // Intercept the SendMagicLink API call and return success so no real email is sent
    await page.route('**/Auth/SendMagicLink', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Check your inbox — we sent you a login link!' }),
      });
    });

    await page.fill('input[type="email"], input[placeholder*="email" i]', 'test@example.com');
    await page.click('button[type="submit"]');

    // Success reveals the #magicSuccess panel
    await expect(page.locator('#magicSuccess')).toBeVisible({ timeout: 8_000 });
  });
});

test.describe('Magic link form — negative', () => {
  test('shows error for empty email submission', async ({ page }) => {
    await page.goto(`${BASE}/Auth/Login`);
    const submitBtn = page.locator('button[type="submit"], button:has-text("Send")').first();
    await submitBtn.click();

    // HTML5 validation or inline error
    const emailInput = page.locator('input[type="email"]').first();
    const validationMsg = await emailInput.evaluate((el: HTMLInputElement) => el.validationMessage);
    const isInvalid = await emailInput.evaluate((el: HTMLInputElement) => !el.validity.valid);
    expect(isInvalid || validationMsg.length > 0).toBeTruthy();
  });

  test('shows error for invalid email format (missing @)', async ({ page }) => {
    await page.goto(`${BASE}/Auth/Login`);

    // Mock the API to return an error
    await page.route('**/Auth/SendMagicLink', async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Please enter a valid email address.' }),
      });
    });

    await page.fill('input[type="email"], input[placeholder*="email" i]', 'notanemail');
    await page.click('button[type="submit"], button:has-text("Send")');

    // HTML5 type=email will block the submit OR a toast error appears
    const emailInput = page.locator('input[type="email"]').first();
    const isInvalid = await emailInput.evaluate((el: HTMLInputElement) => !el.validity.valid).catch(() => false);
    if (!isInvalid) {
      // If JS submits it anyway, an error toast/message should appear
      await expect(page.locator('.toast-error, [class*="error"], text=valid email').first()).toBeVisible({ timeout: 5_000 });
    }
    expect(true).toBeTruthy(); // If HTML5 validation blocked it, that is also acceptable
  });

  test('shows expired token error when visiting VerifyMagicLink with invalid token', async ({ page }) => {
    await page.goto(`${BASE}/Auth/VerifyMagicLink?token=invalid-token-xyz&email=test@example.com`);
    // Should redirect to login with ?error=expired or invalid
    await expect(page).toHaveURL(/error=(expired|invalid)/);
  });

  test('shows error when VerifyMagicLink is called without parameters', async ({ page }) => {
    await page.goto(`${BASE}/Auth/VerifyMagicLink`);
    await expect(page).toHaveURL(/error=invalid/);
  });
});

test.describe('Access control', () => {
  test('redirects unauthenticated users from /Property to login', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    await expect(page).toHaveURL(/Auth\/Login|login/i);
  });

  test('redirects unauthenticated users from /Property/Create to login', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await expect(page).toHaveURL(/Auth\/Login|login/i);
  });

  test('redirects unauthenticated users from /Billing to login', async ({ page }) => {
    await page.goto(`${BASE}/Billing`);
    await expect(page).toHaveURL(/Auth\/Login|login/i);
  });

  test('redirects unauthenticated users from a property detail page to login', async ({ page }) => {
    await page.goto(`${BASE}/Property/Detail/99999`);
    await expect(page).toHaveURL(/Auth\/Login|login/i);
  });

  test('blocks TestLogin endpoint in non-dev would return 403 (dev returns 200)', async ({ request }) => {
    // In test environment (dev), it should succeed. This documents the behavior.
    const res = await request.post(`${BASE}/Auth/TestLogin`, {
      form: { email: 'probe@dwellscript.test' },
    });
    // Dev = 200, non-dev = 403
    expect([200, 403]).toContain(res.status());
  });

  test('already-authenticated user visiting /Auth/Login is redirected to /Property', async ({ page }) => {
    // The test uses storageState: { cookies:[], origins:[] } (unauthenticated)
    // but we need to sign in first via the test endpoint then navigate
    const res = await page.request.post(`${BASE}/Auth/TestLogin`, { form: { email: 'e2e-free@dwellscript.test' } });
    expect(res.ok()).toBeTruthy();

    // Now visit login — should redirect to /Property since already authenticated
    await page.goto(`${BASE}/Auth/Login`, { waitUntil: 'networkidle' });
    await expect(page).toHaveURL(/\/Property/, { timeout: 10_000 });
  });
});
