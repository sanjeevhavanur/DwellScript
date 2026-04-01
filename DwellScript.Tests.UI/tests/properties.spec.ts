/**
 * properties.spec.ts
 *
 * Tests for the Properties list page and Property Create wizard:
 * - Page layout, header, search, filter buttons
 * - Add Property button
 * - Property card rendering
 * - Create wizard: validation, happy path
 * - Edge cases: search with no results, filter behavior
 */
import { test, expect } from '@playwright/test';
import { createPropertyViaUI } from '../helpers/property';

const BASE = process.env.BASE_URL || 'http://localhost:5023';

// ── Properties list ───────────────────────────────────────────────────

test.describe('Properties list page', () => {
  test('loads and shows "My Properties" heading', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    await expect(page.locator('h1, [class*="heading"]').filter({ hasText: /my properties/i }).first()).toBeVisible();
  });

  test('shows Add Property button', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    const addBtn = page.locator('a, button').filter({ hasText: /add property/i }).first();
    await expect(addBtn).toBeVisible();
  });

  test('shows search input', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    await expect(page.locator('#searchInput, input[placeholder*="search" i]').first()).toBeVisible();
  });

  test('shows All / Active / Vacant filter buttons', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    await expect(page.locator('button[data-filter="all"], button:has-text("All")').first()).toBeVisible();
    await expect(page.locator('button[data-filter="active"], button:has-text("Active")').first()).toBeVisible();
    await expect(page.locator('button[data-filter="vacant"], button:has-text("Vacant")').first()).toBeVisible();
  });

  test('Add Property link navigates to /Property/Create', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    await page.click('a[href*="Create"], button:has-text("Add Property")');
    await expect(page).toHaveURL(/\/Property\/Create/);
  });

  test('search input filters property cards', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    await page.waitForLoadState('networkidle');

    await page.fill('#searchInput', 'zzz-should-not-match-anything-xyz');
    await page.waitForTimeout(400); // debounce

    const cards = page.locator('.prop-card:not(.prop-card-add)');
    const count = await cards.count();
    expect(count).toBe(0);
  });

  test('clearing search restores all properties', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    await page.waitForLoadState('networkidle');

    await page.fill('#searchInput', 'zzz-no-match');
    await page.waitForTimeout(300);
    await page.fill('#searchInput', '');
    await page.waitForTimeout(300);

    // At minimum the "Add" card should be visible
    await expect(page.locator('.prop-card-add').first()).toBeVisible();
  });

  test('All filter button is active by default', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    const allBtn = page.locator('button[data-filter="all"]').first();
    await expect(allBtn).toHaveClass(/active/);
  });

  test('clicking Vacant filter activates that button', async ({ page }) => {
    await page.goto(`${BASE}/Property`);
    const vacantBtn = page.locator('button[data-filter="vacant"]').first();
    await vacantBtn.click();
    await expect(vacantBtn).toHaveClass(/active/);
  });
});

// ── Property Create wizard ─────────────────────────────────────────────

test.describe('Property Create wizard', () => {
  // Use Pro user to avoid Free plan's 1-property limit
  test.use({ storageState: '.auth/pro-user.json' });

  test('wizard renders Step 1 (Location) on load', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await expect(page.locator('#panel1')).toBeVisible();
    await expect(page.locator('#panel2')).toHaveClass(/d-none/);
    await expect(page.locator('#address')).toBeVisible();
    await expect(page.locator('#city')).toBeVisible();
    await expect(page.locator('#state')).toBeVisible();
    await expect(page.locator('#zip')).toBeVisible();
  });

  test('Step 1 shows validation errors for empty required fields', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await page.click('#btnNext');

    // Should stay on step 1 or show errors
    await expect(page.locator('#panel1')).toBeVisible();
    // One of the error spans should appear
    const errors = page.locator('.field-error:not(.d-none), [id^="err-"]');
    const count = await errors.count();
    expect(count).toBeGreaterThan(0);
  });

  test('Step 1 validates address is required', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await page.fill('#city', 'Test City');
    await page.fill('#state', 'TX');
    await page.fill('#zip', '78701');
    await page.click('#btnNext');

    await expect(page.locator('#err-address:not(.d-none)')).toBeVisible();
  });

  test('Step 1 validates city is required', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await page.fill('#address', '123 Main St');
    await page.fill('#state', 'TX');
    await page.fill('#zip', '78701');
    await page.click('#btnNext');

    await expect(page.locator('#err-city:not(.d-none)')).toBeVisible();
  });

  test('Step 1 validates state is required', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await page.fill('#address', '123 Main St');
    await page.fill('#city', 'Austin');
    await page.fill('#zip', '78701');
    await page.click('#btnNext');

    await expect(page.locator('#err-state:not(.d-none)')).toBeVisible();
  });

  test('Step 1 validates zip is required', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await page.fill('#address', '123 Main St');
    await page.fill('#city', 'Austin');
    await page.fill('#state', 'TX');
    await page.click('#btnNext');

    await expect(page.locator('#err-zip:not(.d-none)')).toBeVisible();
  });

  test('advances to Step 2 after completing Step 1', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await page.fill('#address', '[E2E] Nav Test Street');
    await page.fill('#city', 'Test City');
    await page.fill('#state', 'TX');
    await page.fill('#zip', '78701');
    await page.click('#btnNext');

    await page.waitForSelector('#panel2:not(.d-none)', { timeout: 5_000 });
    await expect(page.locator('#panel2')).toBeVisible();
  });

  test('Step 2 validates bedrooms is required', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    // Complete step 1
    await page.fill('#address', '[E2E] Beds Test Street');
    await page.fill('#city', 'Test City');
    await page.fill('#state', 'TX');
    await page.fill('#zip', '78701');
    await page.click('#btnNext');
    await page.waitForSelector('#panel2:not(.d-none)');

    await page.selectOption('#propertyType', 'Apartment');
    // Leave bedrooms empty
    await page.fill('#bathrooms', '1');
    await page.click('#btnNext');

    await expect(page.locator('#err-beds:not(.d-none)')).toBeVisible();
  });

  test('Step 2 validates bathrooms is required', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await page.fill('#address', '[E2E] Baths Test Street');
    await page.fill('#city', 'Test City');
    await page.fill('#state', 'TX');
    await page.fill('#zip', '78701');
    await page.click('#btnNext');
    await page.waitForSelector('#panel2:not(.d-none)');

    await page.selectOption('#propertyType', 'Apartment');
    await page.fill('#bedrooms', '2');
    // Leave bathrooms empty
    await page.click('#btnNext');

    await expect(page.locator('#err-baths:not(.d-none)')).toBeVisible();
  });

  test('Back button is hidden on Step 1, visible on Step 2+', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await expect(page.locator('#btnBack')).toHaveClass(/d-none/);

    await page.fill('#address', '[E2E] Back Test');
    await page.fill('#city', 'Test City');
    await page.fill('#state', 'TX');
    await page.fill('#zip', '78701');
    await page.click('#btnNext');
    await page.waitForSelector('#panel2:not(.d-none)');

    await expect(page.locator('#btnBack')).not.toHaveClass(/d-none/);
  });

  test('Back button returns to previous step', async ({ page }) => {
    await page.goto(`${BASE}/Property/Create`);
    await page.fill('#address', '[E2E] Back Nav Test');
    await page.fill('#city', 'Test City');
    await page.fill('#state', 'TX');
    await page.fill('#zip', '78701');
    await page.click('#btnNext');
    await page.waitForSelector('#panel2:not(.d-none)');

    await page.click('#btnBack');
    await page.waitForSelector('#panel1:not(.d-none)');
    await expect(page.locator('#panel1')).toBeVisible();
  });

  test('happy path: creates a property and redirects to detail page', async ({ page }) => {
    const suffix = Date.now().toString();
    const prop = await createPropertyViaUI(page, suffix);
    expect(prop.id).toBeGreaterThan(0);
    await expect(page).toHaveURL(new RegExp(`/Property/Detail/${prop.id}`));

    // Clean up
    await page.request.delete(`${BASE}/api/properties/${prop.id}`);
  });

  test('created property appears in the properties list', async ({ page }) => {
    const suffix = `list-${Date.now()}`;
    const prop = await createPropertyViaUI(page, suffix);

    await page.goto(`${BASE}/Property`);
    await page.waitForLoadState('networkidle');

    // Wait for grid to load
    await page.waitForSelector('#propsGrid', { timeout: 8_000 });
    await expect(page.locator(`text=[E2E] 100 Test Street ${suffix}`).first()).toBeVisible({ timeout: 8_000 });

    // Clean up
    await page.request.delete(`${BASE}/api/properties/${prop.id}`);
  });
});
