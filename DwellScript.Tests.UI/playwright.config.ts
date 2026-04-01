import { defineConfig, devices } from '@playwright/test';

const BASE_URL = process.env.BASE_URL || 'http://localhost:5023';

// Chromium-based mobile emulation config (WebKit not supported on macOS 12)
const chromiumMobile = {
  browserName: 'chromium' as const,
  isMobile: true,
  hasTouch: true,
};

export default defineConfig({
  testDir: '.',
  fullyParallel: false,         // Sequential — shared DB / auth state
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  timeout: 30_000,
  reporter: [
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['list'],
  ],
  use: {
    baseURL: BASE_URL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'off',
  },

  projects: [
    // ── Auth setup (runs once, saves session) ──────────────────────────
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },

    // ── Desktop Chrome (all spec files) ───────────────────────────────
    {
      name: 'Desktop Chrome',
      use: { ...devices['Desktop Chrome'], storageState: '.auth/user.json' },
      dependencies: ['setup'],
      testMatch: /tests\/.*\.spec\.ts/,
    },

    // ── Responsive: only responsive.spec.ts ───────────────────────────
    {
      name: 'iPhone SE (375x667)',
      use: {
        ...chromiumMobile,
        viewport: { width: 375, height: 667 },
        userAgent:
          'Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1',
        storageState: '.auth/user.json',
      },
      dependencies: ['setup'],
      testMatch: /tests\/responsive\.spec\.ts/,
    },
    {
      name: 'iPhone 14 (390x844)',
      use: {
        ...chromiumMobile,
        viewport: { width: 390, height: 844 },
        userAgent:
          'Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1',
        storageState: '.auth/user.json',
      },
      dependencies: ['setup'],
      testMatch: /tests\/responsive\.spec\.ts/,
    },
    {
      name: 'iPhone 14 Pro Max (430x932)',
      use: {
        ...chromiumMobile,
        viewport: { width: 430, height: 932 },
        userAgent:
          'Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1',
        storageState: '.auth/user.json',
      },
      dependencies: ['setup'],
      testMatch: /tests\/responsive\.spec\.ts/,
    },
    {
      name: 'Samsung Galaxy S23 (393x851)',
      use: {
        ...chromiumMobile,
        viewport: { width: 393, height: 851 },
        userAgent:
          'Mozilla/5.0 (Linux; Android 13; SM-S911B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Mobile Safari/537.36',
        storageState: '.auth/user.json',
      },
      dependencies: ['setup'],
      testMatch: /tests\/responsive\.spec\.ts/,
    },
    {
      name: 'Pixel 7 (412x915)',
      use: {
        ...chromiumMobile,
        viewport: { width: 412, height: 915 },
        userAgent:
          'Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Mobile Safari/537.36',
        storageState: '.auth/user.json',
      },
      dependencies: ['setup'],
      testMatch: /tests\/responsive\.spec\.ts/,
    },
    {
      name: 'iPad (768x1024)',
      use: {
        browserName: 'chromium',
        viewport: { width: 768, height: 1024 },
        userAgent:
          'Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1',
        isMobile: true,
        hasTouch: true,
        storageState: '.auth/user.json',
      },
      dependencies: ['setup'],
      testMatch: /tests\/responsive\.spec\.ts/,
    },
    {
      name: 'iPad Pro (1024x1366)',
      use: {
        browserName: 'chromium',
        viewport: { width: 1024, height: 1366 },
        userAgent:
          'Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1',
        isMobile: false,
        hasTouch: true,
        storageState: '.auth/user.json',
      },
      dependencies: ['setup'],
      testMatch: /tests\/responsive\.spec\.ts/,
    },
  ],
});
