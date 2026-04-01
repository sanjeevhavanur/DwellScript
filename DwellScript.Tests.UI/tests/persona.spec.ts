/**
 * persona.spec.ts
 *
 * Tests for the Persona Targeting tab (Pro-gated):
 * - Free user sees Pro gate when clicking Persona tab
 * - Pro user: persona cards render, select, generate, refine, copy, regen
 * - History panel: loads, shows badge count, load/delete entries
 * - Edge cases: generate without selecting persona, FHA clean indicator
 */
import { test, expect, Browser, BrowserContext } from '@playwright/test';
import { createPropertyViaUI } from '../helpers/property';
import {
  mockPersonaGenerate,
  mockPersonaRefine,
  mockPersonaHistoryEmpty,
  mockGetLatest,
  mockHistoryEmpty,
  mockAnalyzerHistoryEmpty,
  MOCK_PERSONA_OUTPUT,
} from '../helpers/mock-api';

const BASE = process.env.BASE_URL || 'http://localhost:5023';

// Default to Pro user (property owner). Individual describes override for gate tests.
test.use({ storageState: '.auth/pro-user.json' });

// ── Shared property created once for all persona tests ────────────────
let testPropertyId = 0;

test.beforeAll(async ({ browser }: { browser: Browser }) => {
  const ctx = await browser.newContext({ storageState: '.auth/pro-user.json' });
  const page = await ctx.newPage();
  const prop = await createPropertyViaUI(page, `persona-${Date.now()}`);
  testPropertyId = prop.id;
  await ctx.close();
});

test.afterAll(async ({ request }) => {
  if (testPropertyId > 0) await request.delete(`${BASE}/api/properties/${testPropertyId}`);
});

// Helper to mock all non-persona routes that fire on detail page load
async function mockDetailPageRoutes(page: any, propId: number) {
  await page.route('**/api/generation/latest/**', async (route: any) => {
    await route.fulfill({ status: 204 }); // no previous generation
  });
  await mockHistoryEmpty(page);
  await mockAnalyzerHistoryEmpty(page);
  await mockPersonaHistoryEmpty(page);
}

// ── Pro context fixture ──────────────────────────────────────────────
let proContext: BrowserContext;

// ── Free user — Pro gate ──────────────────────────────────────────────

test.describe('Persona tab — Free user gate', () => {
  test.use({ storageState: '.auth/user.json' });

  let freePropId = 0;

  test.beforeAll(async ({ browser }) => {
    const ctx = await browser.newContext({ storageState: '.auth/user.json' });
    const page = await ctx.newPage();
    await page.request.post(`${BASE}/Auth/TestSetTier`, { form: { email: 'e2e-free@dwellscript.test', tier: 'Starter' } });
    // Clean up any leftover E2E properties from previous failed runs
    const listRes = await page.request.get(`${BASE}/api/properties`);
    if (listRes.ok()) {
      const props: Array<{ id: number; address: string }> = await listRes.json();
      for (const p of props) {
        if (p.address?.startsWith('[E2E]')) {
          await page.request.delete(`${BASE}/api/properties/${p.id}`);
        }
      }
    }
    const prop = await createPropertyViaUI(page, `free-persona-gate-${Date.now()}`);
    freePropId = prop.id;
    await page.request.post(`${BASE}/Auth/TestSetTier`, { form: { email: 'e2e-free@dwellscript.test', tier: 'Free' } });
    await ctx.close();
  });

  test.afterAll(async ({ browser }) => {
    if (freePropId > 0) {
      const ctx = await browser.newContext({ storageState: '.auth/user.json' });
      const page = await ctx.newPage();
      await page.request.post(`${BASE}/Auth/TestSetTier`, { form: { email: 'e2e-free@dwellscript.test', tier: 'Starter' } });
      await page.request.delete(`${BASE}/api/properties/${freePropId}`);
      await page.request.post(`${BASE}/Auth/TestSetTier`, { form: { email: 'e2e-free@dwellscript.test', tier: 'Free' } });
      await ctx.close();
    }
  });

  test('clicking Persona tab shows Pro upgrade toast for Free user', async ({ page }) => {
    await mockDetailPageRoutes(page, freePropId);
    await page.goto(`${BASE}/Property/Detail/${freePropId}`);
    await page.waitForLoadState('networkidle');

    await page.click('[data-tab="persona"]');

    await expect(
      page.locator('.toast-warning').or(page.locator('.toast-error')).first()
    ).toBeVisible({ timeout: 6_000 });

    // Persona tab panel must remain hidden
    await expect(page.locator('#tabPersona')).toHaveClass(/d-none/);
  });
});

// ── Pro user — full feature ───────────────────────────────────────────

test.describe('Persona tab — Pro user', () => {
  test.use({ storageState: '.auth/pro-user.json' });

  test('Persona tab opens for Pro user', async ({ page }) => {
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');

    await page.click('[data-tab="persona"]');
    await expect(page.locator('#tabPersona')).not.toHaveClass(/d-none/);
  });

  test('all 6 persona cards render', async ({ page }) => {
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');

    await expect(page.locator('#tabPersona')).not.toHaveClass(/d-none/);
    const cards = page.locator('.persona-card');
    await expect(cards).toHaveCount(6);
  });

  test('persona card names are correct', async ({ page }) => {
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');

    const expectedNames = [
      'Remote Worker', 'Pet Owner', 'Commuter',
      'Outdoor Enthusiast', 'Urban Lifestyle', 'Long-Term Resident',
    ];
    for (const name of expectedNames) {
      await expect(page.locator(`text=${name}`).first()).toBeVisible();
    }
  });

  test('selecting a persona card activates it', async ({ page }) => {
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');

    await page.click('#pcard-remote-worker');
    await expect(page.locator('#pcard-remote-worker')).toHaveClass(/selected/);
  });

  test('only one persona card is selected at a time', async ({ page }) => {
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');

    await page.click('#pcard-remote-worker');
    await page.click('#pcard-pet-owner');

    await expect(page.locator('#pcard-remote-worker')).not.toHaveClass(/selected/);
    await expect(page.locator('#pcard-pet-owner')).toHaveClass(/selected/);
  });

  test('Generate button is disabled until a persona is selected', async ({ page }) => {
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');

    await expect(page.locator('#btnPersonaGenerate')).toBeDisabled();
  });

  test('Generate button enables after selecting a persona', async ({ page }) => {
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');

    await page.click('#pcard-commuter');
    await expect(page.locator('#btnPersonaGenerate')).toBeEnabled();
  });

  test('generating a persona listing shows the output card', async ({ page }) => {
    await mockPersonaGenerate(page);
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');

    await page.click('#pcard-remote-worker');
    await page.click('#btnPersonaGenerate');

    await expect(page.locator('#personaOutputCard')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('#personaOutputBody')).toContainText('remote workers', { timeout: 10_000 });
  });

  test('generated persona output shows word count', async ({ page }) => {
    await mockPersonaGenerate(page);
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');
    await page.click('#pcard-remote-worker');
    await page.click('#btnPersonaGenerate');

    await expect(page.locator('#personaWordCount')).toBeVisible({ timeout: 10_000 });
    const wc = await page.locator('#personaWordCount').textContent();
    expect(wc).toMatch(/\d+\s*words/i);
  });

  test('FHA strip appears after generation', async ({ page }) => {
    await mockPersonaGenerate(page);
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');
    await page.click('#pcard-remote-worker');
    await page.click('#btnPersonaGenerate');

    await page.locator('#personaOutputCard').waitFor({ state: 'visible', timeout: 10_000 });
    await expect(page.locator('#personaFhaStrip')).toBeVisible();
  });

  test('Refine drawer toggles open/closed', async ({ page }) => {
    await mockPersonaGenerate(page);
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');
    await page.click('#pcard-remote-worker');
    await page.click('#btnPersonaGenerate');
    await page.locator('#personaOutputCard').waitFor({ state: 'visible', timeout: 10_000 });

    const drawer = page.locator('#personaRefineDrawer');
    await expect(drawer).not.toHaveClass(/open/);

    // Click the refine button (find it within the output card)
    await page.locator('button[onclick*="togglePersonaRefine"]').first().click();
    await expect(drawer).toHaveClass(/open/);

    await page.locator('button[onclick*="togglePersonaRefine"]').first().click();
    await expect(drawer).not.toHaveClass(/open/);
  });

  test('submitting a refine instruction updates output', async ({ page }) => {
    await mockPersonaGenerate(page);
    await mockPersonaRefine(page);
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');
    await page.click('#pcard-remote-worker');
    await page.click('#btnPersonaGenerate');
    await page.locator('#personaOutputCard').waitFor({ state: 'visible', timeout: 10_000 });

    await page.locator('button[onclick*="togglePersonaRefine"]').first().click();
    await page.fill('#personaRefineInput', 'Make it more concise');
    await page.click('button[onclick*="submitPersonaRefine"]');

    await expect(page.locator('#personaOutputBody')).toContainText('Refined', { timeout: 10_000 });
  });

  test('submitting empty refine instruction shows warning', async ({ page }) => {
    await mockPersonaGenerate(page);
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');
    await page.click('#pcard-remote-worker');
    await page.click('#btnPersonaGenerate');
    await page.locator('#personaOutputCard').waitFor({ state: 'visible', timeout: 10_000 });

    await page.locator('button[onclick*="togglePersonaRefine"]').first().click();
    await expect(page.locator('#personaRefineDrawer')).toHaveClass(/open/, { timeout: 3_000 });
    // Leave input empty and submit
    await page.click('button[onclick*="submitPersonaRefine"]');

    await expect(page.locator('.toast-warning').first()).toBeVisible({ timeout: 5_000 });
  });

  test('Enter key in refine input triggers refinement', async ({ page }) => {
    await mockPersonaGenerate(page);
    await mockPersonaRefine(page);
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');
    await page.click('#pcard-remote-worker');
    await page.click('#btnPersonaGenerate');
    await page.locator('#personaOutputCard').waitFor({ state: 'visible', timeout: 10_000 });

    await page.locator('button[onclick*="togglePersonaRefine"]').first().click();
    await page.fill('#personaRefineInput', 'Add more details');
    await page.press('#personaRefineInput', 'Enter');

    await expect(page.locator('#personaOutputBody')).toContainText('Refined', { timeout: 10_000 });
  });
});

// ── Persona History ───────────────────────────────────────────────────

test.describe('Persona history panel — Pro user', () => {
  test.use({ storageState: '.auth/pro-user.json' });

  test('history panel is visible in persona tab', async ({ page }) => {
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');

    await expect(page.locator('#personaHistoryList')).toBeVisible();
  });

  test('empty history shows placeholder text', async ({ page }) => {
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');
    await page.waitForLoadState('networkidle');

    await expect(
      page.locator('#personaHistoryList').locator('text=/no persona|get started/i').first()
    ).toBeVisible({ timeout: 5_000 });
  });

  test('badge shows 0 for empty history', async ({ page }) => {
    await mockDetailPageRoutes(page, testPropertyId);
    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('#personaHistBadge')).toHaveText('0');
  });

  test('history panel updates badge and shows entry after generation', async ({ page }) => {
    await mockPersonaGenerate(page);

    // First call returns empty, second returns one item
    let callCount = 0;
    await page.route('**/api/generation/persona-history/**', async (route) => {
      callCount++;
      if (callCount === 1) {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      } else {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{
            id: 99,
            personaKey: 'remote-worker',
            type: 'PersonaGeneration',
            createdAt: new Date().toISOString(),
            wordCount: 42,
          }]),
        });
      }
    });
    await page.route('**/api/generation/latest/**', async (route) => route.fulfill({ status: 204 }));
    await mockHistoryEmpty(page);
    await mockAnalyzerHistoryEmpty(page);

    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');
    await page.click('#pcard-remote-worker');
    await page.click('#btnPersonaGenerate');

    await page.locator('#personaOutputCard').waitFor({ state: 'visible', timeout: 10_000 });

    await expect(page.locator('#personaHistBadge')).toHaveText('1', { timeout: 5_000 });
    await expect(page.locator('.persona-hist-item').first()).toBeVisible();
    await expect(page.locator('text=Remote Worker').first()).toBeVisible();
  });

  test('clicking Load button on history item restores output', async ({ page }) => {
    await page.route('**/api/generation/persona-history/**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([{
          id: 100,
          personaKey: 'pet-owner',
          type: 'PersonaGeneration',
          createdAt: new Date().toISOString(),
          wordCount: 30,
        }]),
      });
    });
    await page.route('**/api/generation/persona/100', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 100,
          personaKey: 'pet-owner',
          type: 'PersonaGeneration',
          createdAt: new Date().toISOString(),
          output: 'Pet-friendly paradise awaits you and your furry friends!',
          wordCount: 9,
        }),
      });
    });
    await page.route('**/api/generation/latest/**', async (route) => route.fulfill({ status: 204 }));
    await mockHistoryEmpty(page);
    await mockAnalyzerHistoryEmpty(page);

    await page.goto(`${BASE}/Property/Detail/${testPropertyId}`);
    await page.waitForLoadState('networkidle');
    await page.click('[data-tab="persona"]');
    await page.waitForLoadState('networkidle');

    await page.locator('.persona-hist-item button[title="Load"]').first().click();

    await expect(page.locator('#personaOutputBody')).toContainText('Pet-friendly', { timeout: 8_000 });
  });
});
