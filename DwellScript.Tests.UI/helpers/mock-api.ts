/**
 * mock-api.ts
 *
 * Playwright route mocks for AI-backed API endpoints. Using these prevents
 * real calls to Anthropic Claude during tests — keeping tests fast,
 * deterministic, and free.
 */
import { Page, Route } from '@playwright/test';

// ── Canonical mock payloads ────────────────────────────────────────────

export const MOCK_GEN_ID = 8888;

export const MOCK_GENERATION = {
  id: MOCK_GEN_ID,
  propertyId: 0,       // overridden per test
  type: 'Full',
  ltrOutput:
    'Welcome to this stunning 2-bedroom, 1-bathroom apartment in Test City. ' +
    'Featuring hardwood floors, updated kitchen with stainless appliances, and ' +
    'in-unit washer/dryer. Convenient parking and pet-friendly. $1,500/mo.',
  strOutput:
    'Escape to this charming Test City retreat! Beautifully appointed 2BR/1BA ' +
    'unit with modern updates. Perfect for both short and extended stays.',
  socialOutput:
    '✨ Now Available! Stunning 2BR/1BA in Test City. Modern updates, great ' +
    'location. DM for details! #RentalsAvailable #TestCity',
  headlinesJson: JSON.stringify([
    'Modern 2BR in the Heart of Test City',
    'Updated Kitchen & In-Unit Laundry',
    'Prime Location — Pet Friendly',
  ]),
  createdAt: new Date().toISOString(),
  usageUnitsConsumed: 1.0,
  fairHousingViolations: null,
};

export const MOCK_PERSONA_OUTPUT =
  'Calling all remote workers! This 2BR/1BA Test City apartment is your perfect ' +
  'work-from-home sanctuary. Enjoy blazing-fast fiber internet, a dedicated home ' +
  'office nook, and a quiet neighborhood ideal for deep focus. $1,500/mo.';

export const MOCK_ANALYSIS = {
  score: 72,
  insights: [
    {
      title: 'Listing copy lacks urgency',
      detail: 'Your LTR description does not include any call-to-action.',
      severity: 'medium',
      suggestedFix: 'Add a strong call-to-action at the end of the LTR copy.',
      section: 'ltr',
    },
    {
      title: 'No pet policy mentioned in STR',
      detail: 'Short-term listing should specify pet policy clearly.',
      severity: 'low',
      suggestedFix: 'Mention the pet policy explicitly in the STR description.',
      section: 'str',
    },
  ],
};

// ── Route mock helpers ─────────────────────────────────────────────────

/** Mock POST /api/generation/generate */
export async function mockGenerate(page: Page, propertyId?: number): Promise<void> {
  await page.route('**/api/generation/generate', async (route: Route) => {
    const body = { ...MOCK_GENERATION, propertyId: propertyId ?? MOCK_GENERATION.propertyId };
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
  });
}

/** Mock GET /api/generation/latest/:propertyId */
export async function mockGetLatest(page: Page, propertyId?: number): Promise<void> {
  await page.route('**/api/generation/latest/**', async (route: Route) => {
    const body = { ...MOCK_GENERATION, propertyId: propertyId ?? MOCK_GENERATION.propertyId };
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
  });
}

/** Mock POST /api/generation/regen-section */
export async function mockRegenSection(page: Page): Promise<void> {
  await page.route('**/api/generation/regen-section', async (route: Route) => {
    const body = {
      ...MOCK_GENERATION,
      id: MOCK_GEN_ID + 1,
      type: 'SectionRegen',
      regeneratedSection: 'LTR',
      usageUnitsConsumed: 0.25,
    };
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
  });
}

/** Mock POST /api/generation/persona */
export async function mockPersonaGenerate(page: Page): Promise<void> {
  await page.route('**/api/generation/persona', async (route: Route) => {
    const body = {
      output: MOCK_PERSONA_OUTPUT,
      wordCount: MOCK_PERSONA_OUTPUT.split(/\s+/).length,
      fhaClean: true,
      generationId: MOCK_GEN_ID + 2,
    };
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
  });
}

/** Mock POST /api/generation/persona-refine */
export async function mockPersonaRefine(page: Page): Promise<void> {
  await page.route('**/api/generation/persona-refine', async (route: Route) => {
    const refined = MOCK_PERSONA_OUTPUT + ' [Refined: more details added.]';
    const body = {
      output: refined,
      wordCount: refined.split(/\s+/).length,
      fhaClean: true,
      generationId: MOCK_GEN_ID + 3,
    };
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
  });
}

/** Mock POST /api/analyzer/analyze */
export async function mockAnalyze(page: Page): Promise<void> {
  await page.route('**/api/analyzer/analyze', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_ANALYSIS),
    });
  });
}

/** Mock GET /api/generation/history/:propertyId → empty array */
export async function mockHistoryEmpty(page: Page): Promise<void> {
  await page.route('**/api/generation/history/**', async (route: Route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });
}

/** Mock GET /api/generation/persona-history/:propertyId → empty array */
export async function mockPersonaHistoryEmpty(page: Page): Promise<void> {
  await page.route('**/api/generation/persona-history/**', async (route: Route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });
}

/** Mock GET /api/analyzer/history/:propertyId → empty array */
export async function mockAnalyzerHistoryEmpty(page: Page): Promise<void> {
  await page.route('**/api/analyzer/history/**', async (route: Route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });
}

/** Mock POST /api/generation/generate to return 402 quota exceeded */
export async function mockGenerateQuotaExceeded(page: Page): Promise<void> {
  await page.route('**/api/generation/generate', async (route: Route) => {
    await route.fulfill({
      status: 402,
      contentType: 'application/json',
      body: JSON.stringify({ message: "You've reached your monthly generation limit. Upgrade to Starter for unlimited generations." }),
    });
  });
}
