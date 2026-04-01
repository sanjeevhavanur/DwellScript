/**
 * property.ts
 *
 * Helpers for creating and cleaning up test properties via the UI wizard.
 */
import { Page, APIRequestContext, expect } from '@playwright/test';

const BASE_URL = process.env.BASE_URL || 'http://localhost:5023';

export interface TestProperty {
  id: number;
  address: string;
}

/**
 * Creates a property through the 4-step wizard UI.
 * Returns the property ID parsed from the redirect URL.
 */
export async function createPropertyViaUI(page: Page, suffix: string): Promise<TestProperty> {
  const address = `[E2E] 100 Test Street ${suffix}`;

  await page.goto(`${BASE_URL}/Property/Create`);
  await page.waitForLoadState('networkidle');

  // Step 1 — Location
  await page.fill('#address', address);
  await page.fill('#city', 'Test City');
  await page.fill('#state', 'TX');
  await page.fill('#zip', '78701');
  await page.click('#btnNext');

  // Step 2 — Details
  await page.waitForSelector('#panel2:not(.d-none)', { timeout: 5000 });
  await page.selectOption('#propertyType', 'Apartment');
  await page.fill('#bedrooms', '2');
  await page.fill('#bathrooms', '1');
  await page.click('#btnNext');

  // Step 3 — Amenities (skip)
  await page.waitForSelector('#panel3:not(.d-none)', { timeout: 5000 });
  await page.click('#btnNext');

  // Step 4 — Platforms: select LTR so the save doesn't submit an empty array
  await page.waitForSelector('#panel4:not(.d-none)', { timeout: 5000 });
  await page.locator('#platformsGroup .chip-label').first().click();

  // Wait for button to say "Save Property" then click
  await page.waitForFunction(() => {
    const btn = document.getElementById('btnNext');
    return btn && btn.textContent?.includes('Save');
  }, { timeout: 5000 });
  await page.click('#btnNext');

  // Wait for AJAX to complete and redirect
  await page.waitForURL(/\/Property\/Detail\/\d+/, { timeout: 25_000 });

  const match = page.url().match(/\/Property\/Detail\/(\d+)/);
  const id = match ? parseInt(match[1], 10) : 0;
  return { id, address };
}

/**
 * Deletes a property by ID via the API (authenticated request).
 */
export async function deletePropertyViaAPI(request: APIRequestContext, propertyId: number): Promise<void> {
  // We can't easily get CSRF token here; use page.request from within a page context
  // instead call the property delete through the properties API
  const res = await request.delete(`${BASE_URL}/api/properties/${propertyId}`);
  // 200 or 204 both acceptable; silently ignore if already gone
}

/**
 * Deletes all E2E test properties for the current user by finding them through the API.
 */
export async function cleanupTestProperties(request: APIRequestContext): Promise<void> {
  const res = await request.get(`${BASE_URL}/api/properties`);
  if (!res.ok()) return;
  const props: Array<{ id: number; address: string }> = await res.json();
  for (const p of props) {
    if (p.address?.startsWith('[E2E]')) {
      await deletePropertyViaAPI(request, p.id);
    }
  }
}
