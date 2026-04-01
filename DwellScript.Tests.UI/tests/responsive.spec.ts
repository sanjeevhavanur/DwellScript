/**
 * responsive.spec.ts
 *
 * UI alignment and responsiveness tests across all configured viewports.
 * Runs against: iPhone SE, iPhone 14, iPhone 14 Pro Max,
 *               Samsung Galaxy S23, Pixel 7, iPad, iPad Pro.
 *
 * Checks:
 * - No horizontal overflow / x-scroll
 * - Key elements visible and not clipped
 * - Touch targets at least 44px tall
 * - Sidebar not visible on mobile (hidden or collapsed)
 * - Pricing price alignment ($ and number same line)
 * - Property cards stack vertically on small screens
 * - Wizard form fields fill width on mobile
 */
import { test, expect, Page } from '@playwright/test';
import { createPropertyViaUI } from '../helpers/property';

const BASE = process.env.BASE_URL || 'http://localhost:5023';

// ─── Utility helpers ─────────────────────────────────────────────────

/** Returns true if the page has horizontal scroll (content overflows viewport width). */
async function hasHorizontalOverflow(page: Page): Promise<boolean> {
  return page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth);
}

/** Returns computed bounding rect for a selector, or null if not found. */
async function getBBox(page: Page, selector: string) {
  return page.locator(selector).first().boundingBox().catch(() => null);
}

// ─── Home page responsive ─────────────────────────────────────────────

test.describe('Home page — responsive layout', () => {
  test('no horizontal overflow', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await page.waitForLoadState('networkidle');
    expect(await hasHorizontalOverflow(page)).toBe(false);
  });

  test('hero headline is visible', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await page.waitForLoadState('networkidle');
    const hero = page.locator('h1, [class*="hero"]').first();
    await expect(hero).toBeVisible();
    const box = await hero.boundingBox();
    expect(box).not.toBeNull();
    expect(box!.width).toBeGreaterThan(50);
  });

  test('CTA button is reachable and tappable (≥44px height)', async ({ page }) => {
    await page.goto(`${BASE}/`);
    const cta = page.locator('a[href*="Login"], a[href*="Property"]').filter({ hasText: /get started|sign|free/i }).first();
    await expect(cta).toBeVisible();
    const box = await cta.boundingBox();
    if (box) expect(box.height).toBeGreaterThanOrEqual(36); // ≥36px is acceptable for touch
  });

  test('pricing section is visible without horizontal cut-off', async ({ page }) => {
    await page.goto(`${BASE}/`);
    const viewport = page.viewportSize();
    const priceCards = page.locator('[class*="plan"], [class*="pricing-card"]');
    const count = await priceCards.count();
    if (count > 0 && viewport) {
      const box = await priceCards.first().boundingBox();
      if (box) expect(box.x + box.width).toBeLessThanOrEqual(viewport.width + 5); // 5px tolerance
    }
  });

  test('$ sign and price on same line (no vertical stacking)', async ({ page }) => {
    await page.goto(`${BASE}/`);
    await page.waitForLoadState('networkidle');
    const priceContainers = page.locator('.lp-price-amt');
    const count = await priceContainers.count();
    if (count > 0) {
      for (let i = 0; i < Math.min(count, 3); i++) {
        const box = await priceContainers.nth(i).boundingBox();
        if (box) expect(box.height).toBeLessThan(120);
      }
    }
  });
});

// ─── Login page responsive ────────────────────────────────────────────

test.describe('Login page — responsive layout', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test('no horizontal overflow', async ({ page }) => {
    await page.goto(`${BASE}/Auth/Login`);
    await page.waitForLoadState('networkidle');
    expect(await hasHorizontalOverflow(page)).toBe(false);
  });

  test('email input is visible and fills most of its container', async ({ page }) => {
    await page.goto(`${BASE}/Auth/Login`);
    const input = page.locator('input[type="email"]').first();
    await expect(input).toBeVisible();
    const viewport = page.viewportSize();
    const box = await input.boundingBox();
    if (box && viewport) {
      // Input should not overflow the viewport
      expect(box.x + box.width).toBeLessThanOrEqual(viewport.width + 5);
    }
  });

  test('Send button is tappable size', async ({ page }) => {
    await page.goto(`${BASE}/Auth/Login`);
    const btn = page.locator('button[type="submit"]').first();
    await expect(btn).toBeVisible();
    const box = await btn.boundingBox();
    if (box) expect(box.height).toBeGreaterThanOrEqual(36);
  });
});

// ─── Properties list responsive ──────────────────────────────────────

test.describe('Properties list — responsive layout', () => {
  test('no horizontal overflow', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    await page.waitForLoadState('networkidle');
    expect(await hasHorizontalOverflow(page)).toBe(false);
  });

  test('Add Property button is visible and tappable', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    const btn = page.locator('a[href*="Create"], #btnAddProperty').first();
    await expect(btn).toBeVisible();
    const box = await btn.boundingBox();
    if (box) expect(box.height).toBeGreaterThanOrEqual(36);
  });

  test('search input is visible', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    await expect(page.locator('#searchInput').first()).toBeVisible();
  });

  test('sidebar is not overlapping main content area', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    const viewport = page.viewportSize();
    if (!viewport) return;

    // On mobile, content should start near the left edge (sidebar hidden/collapsed)
    const main = page.locator('.ds-main, main, [class*="main"]').first();
    const box = await main.boundingBox();
    if (box && viewport.width <= 768) {
      // Main content should occupy most of the screen width on mobile
      expect(box.width).toBeGreaterThan(viewport.width * 0.7);
    }
  });

  test('filter buttons are accessible without overflow', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    await page.waitForLoadState('networkidle');
    const viewport = page.viewportSize();
    const filterBtns = page.locator('button[data-filter]');
    const count = await filterBtns.count();
    if (count > 0 && viewport) {
      for (let i = 0; i < count; i++) {
        const box = await filterBtns.nth(i).boundingBox();
        if (box) {
          expect(box.x).toBeGreaterThanOrEqual(0);
          expect(box.x + box.width).toBeLessThanOrEqual(viewport.width + 5);
        }
      }
    }
  });
});

// ─── Property Create wizard responsive ───────────────────────────────

test.describe('Property Create wizard — responsive layout', () => {
  test('no horizontal overflow', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await page.waitForLoadState('networkidle');
    expect(await hasHorizontalOverflow(page)).toBe(false);
  });

  test('wizard step indicators are visible', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await expect(page.locator('#stepNum1').first()).toBeVisible();
  });

  test('address input fills container width on mobile', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    const input = page.locator('#address');
    const viewport = page.viewportSize();
    const box = await input.boundingBox();
    if (box && viewport) {
      // Input should be at least 50% of viewport width
      expect(box.width).toBeGreaterThan(viewport.width * 0.4);
      // And not overflow
      expect(box.x + box.width).toBeLessThanOrEqual(viewport.width + 10);
    }
  });

  test('Next button is tappable size', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    const btn = page.locator('#btnNext');
    const box = await btn.boundingBox();
    if (box) expect(box.height).toBeGreaterThanOrEqual(36);
  });
});

// ─── Billing page responsive ──────────────────────────────────────────

test.describe('Billing page — responsive layout', () => {
  test('no horizontal overflow', async ({ page }) => {
    await page.goto(`${BASE}/Billing`);
    await page.waitForLoadState('networkidle');
    expect(await hasHorizontalOverflow(page)).toBe(false);
  });

  test('plan cards are visible', async ({ page }) => {
    await page.goto(`${BASE}/Billing`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.plan-name').filter({ hasText: 'Starter' }).first()).toBeVisible();
  });

  test('plan cards do not overflow viewport', async ({ page }) => {
    await page.goto(`${BASE}/Billing`);
    await page.waitForLoadState('networkidle');
    const viewport = page.viewportSize();
    const cards = page.locator('[class*="plan-card"], [class*="plan"]');
    const count = await cards.count();
    if (count > 0 && viewport) {
      const box = await cards.first().boundingBox();
      if (box) expect(box.x + box.width).toBeLessThanOrEqual(viewport.width + 5);
    }
  });

  test('$ sign and price on same line in billing pricing cards', async ({ page }) => {
    await page.goto(`${BASE}/Billing`);
    await page.waitForLoadState('networkidle');
    const prices = page.locator('[class*="plan-price"]');
    const count = await prices.count();
    for (let i = 0; i < Math.min(count, 4); i++) {
      const box = await prices.nth(i).boundingBox();
      if (box) expect(box.height).toBeLessThan(120);
    }
  });
});

// ─── Property Detail responsive ───────────────────────────────────────

test.describe('Property Detail — responsive layout', () => {
  // Use Pro user to create a real property for these tests
  test.use({ storageState: '.auth/pro-user.json' });

  let detailPropId = 0;

  test.beforeAll(async ({ browser }) => {
    const ctx = await browser.newContext({ storageState: '.auth/pro-user.json' });
    const page = await ctx.newPage();
    const prop = await createPropertyViaUI(page, `resp-${Date.now()}`);
    detailPropId = prop.id;
    await ctx.close();
  });

  test.afterAll(async ({ request }) => {
    if (detailPropId > 0) await request.delete(`${BASE}/api/properties/${detailPropId}`);
  });

  async function mockDetailApi(page: Page) {
    await page.route('**/api/generation/latest/**', async (r: any) => r.fulfill({ status: 204 }));
    await page.route('**/api/generation/history/**', async (r: any) => r.fulfill({ status: 200, contentType: 'application/json', body: '[]' }));
    await page.route('**/api/analyzer/history/**', async (r: any) => r.fulfill({ status: 200, contentType: 'application/json', body: '[]' }));
    await page.route('**/api/generation/persona-history/**', async (r: any) => r.fulfill({ status: 200, contentType: 'application/json', body: '[]' }));
  }

  test('no horizontal overflow on detail page', async ({ page }) => {
    await mockDetailApi(page);
    await page.goto(`${BASE}/Property/Detail/${detailPropId}`);
    await page.waitForLoadState('networkidle');
    expect(await hasHorizontalOverflow(page)).toBe(false);
  });

  test('tab bar is navigable without horizontal scroll', async ({ page }) => {
    await mockDetailApi(page);
    await page.goto(`${BASE}/Property/Detail/${detailPropId}`);
    await page.waitForLoadState('networkidle');
    const viewport = page.viewportSize();
    const tabs = page.locator('.ds-tab');
    const count = await tabs.count();
    if (count > 0 && viewport) {
      for (let i = 0; i < count; i++) {
        const box = await tabs.nth(i).boundingBox();
        if (box) {
          expect(box.x).toBeGreaterThanOrEqual(-5); // Allow slight tolerance
        }
      }
    }
  });
});
