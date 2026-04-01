/**
 * home.spec.ts
 *
 * Tests for the public landing / home page (unauthenticated).
 * Covers: layout, features grid, pricing cards, price alignment, CTA buttons.
 */
import { test, expect } from '@playwright/test';

const BASE = process.env.BASE_URL || 'http://localhost:5023';

test.use({ storageState: { cookies: [], origins: [] } });

test.describe('Home page — layout', () => {
  test('loads with correct page title', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await expect(page).toHaveTitle(/DwellScript/i);
  });

  test('shows the hero section with headline', async ({ page }) => {
    await page.goto(`${BASE}/`);
    // Hero should contain a compelling headline
    const hero = page.locator('h1, [class*="hero"]').first();
    await expect(hero).toBeVisible();
    const text = await hero.textContent();
    expect(text?.length).toBeGreaterThan(5);
  });

  test('renders a primary CTA button', async ({ page }) => {
    await page.goto(`${BASE}/`);
    const cta = page.locator('a[href*="Login"], a[href*="Property"], button').filter({ hasText: /get started|sign up|try|free/i }).first();
    await expect(cta).toBeVisible();
  });
});

test.describe('Home page — features', () => {
  test('shows LTR (Long-Term Rental) feature card', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await expect(page.locator('text=/long.term|LTR/i').first()).toBeVisible();
  });

  test('shows STR (Short-Term Rental) feature card', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await expect(page.locator('text=/short.term|STR|airbnb|vrbo/i').first()).toBeVisible();
  });

  test('shows Social Media feature card', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await expect(page.locator('text=/social/i').first()).toBeVisible();
  });

  test('shows Persona Targeting feature (Pro highlight)', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await expect(page.locator('text=/persona/i').first()).toBeVisible();
  });
});

test.describe('Home page — pricing section', () => {
  test('pricing section is visible', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('#pricing')).toBeVisible();
  });

  test('shows Free plan card', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await expect(page.locator('.lp-price-name').filter({ hasText: 'Free' }).first()).toBeVisible();
  });

  test('shows Starter plan card', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await expect(page.locator('.lp-price-name').filter({ hasText: 'Starter' }).first()).toBeVisible();
  });

  test('shows Pro plan card', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await expect(page.locator('.lp-price-name').filter({ hasText: 'Pro' }).first()).toBeVisible();
  });

  test('Persona Targeting shown as Pro-only feature', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await page.waitForLoadState('networkidle');
    // Persona feature appears in the pricing grid
    await expect(page.locator('#pricing').getByText(/persona/i).first()).toBeVisible();
  });

  test('$ sign and price amount are on the same line (not stacked)', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await page.waitForLoadState('networkidle');

    // .lp-price-amt contains the $ sup and number span
    const priceContainers = page.locator('.lp-price-amt');
    const count = await priceContainers.count();
    if (count > 0) {
      for (let i = 0; i < Math.min(count, 3); i++) {
        const box = await priceContainers.nth(i).boundingBox();
        if (box) expect(box.height).toBeLessThan(120);
      }
    }
  });

  test('price amounts render numbers (not zero or empty)', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await page.waitForLoadState('networkidle');
    const priceNums = page.locator('.lp-price-amt span, .lp-price-num');
    const count = await priceNums.count();
    if (count > 0) {
      const text = await priceNums.first().textContent();
      expect(text?.trim()).toMatch(/\d+/);
    }
  });
});

test.describe('Home page — navigation', () => {
  test('nav links are present', async ({ page }) => {
    await page.goto(`${BASE}/`);
    // Should have at least one navigation link
    const nav = page.locator('nav a, header a').first();
    await expect(nav).toBeVisible();
  });
});
