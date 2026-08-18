import { test as base, type Page, request as playwrightRequest } from '@playwright/test';
import { API_URL, TEST_USER, ADMIN_USER } from '../utils/testData';

/**
 * The app persists the authenticated user under this localStorage key. On
 * startup App.tsx reads it, rehydrates the Redux slice, and starts the
 * refresh-token timer, so writing a valid object here is enough to land in
 * a logged-in state without driving the login form. Keep this key in sync
 * with STORAGE_KEY in frontend/src/utils/helpers/userhelpers.ts.
 */
const STORAGE_KEY = 'currentuser';

type Credentials = { email: string; password: string };

/**
 * Authenticates against the real backend and returns the user object the
 * frontend expects to find in localStorage. We go through the actual
 * /authenticate endpoint (rather than minting a token ourselves) so the
 * JWT is genuine and the app's refresh logic behaves normally.
 */
async function fetchAuthedUser(creds: Credentials) {
  const ctx = await playwrightRequest.newContext();
  const res = await ctx.post(`${API_URL}/accounts/authenticate`, {
    data: creds,
    headers: { 'Content-Type': 'application/json' },
  });
  if (!res.ok()) {
    const body = await res.text();
    throw new Error(
      `E2E login failed for ${creds.email} (${res.status()}). ` +
        `Confirm the account exists and is verified. Response: ${body}`,
    );
  }
  const user = await res.json();
  await ctx.dispose();
  if (!user || typeof user.jwtToken !== 'string') {
    throw new Error(
      `Authenticate response for ${creds.email} had no jwtToken. Got: ${JSON.stringify(user)}`,
    );
  }
  return user;
}

/**
 * Seeds the given user into localStorage before any app code runs, so the
 * very first render is already authenticated. addInitScript runs on every
 * navigation in the context, which is what we want: the session survives
 * the page reloads these tests perform.
 */
async function seedSession(page: Page, user: unknown) {
  await page.addInitScript(
    ([key, value]) => {
      window.localStorage.setItem(key as string, value as string);
    },
    [STORAGE_KEY, JSON.stringify(user)],
  );
}

/**
 * Fixtures:
 *  - authedPage: a page already logged in as the standard test user.
 *  - adminPage:  a page already logged in as the admin test user.
 *
 * Both are worker-cached at the credential level via module state below, so
 * we only hit /authenticate once per user per worker rather than once per
 * test.
 */
type AuthFixtures = {
  authedPage: Page;
  adminPage: Page;
};

let cachedUser: unknown | undefined;
let cachedAdmin: unknown | undefined;

export const test = base.extend<AuthFixtures>({
  authedPage: async ({ page }, use) => {
    cachedUser ??= await fetchAuthedUser(TEST_USER);
    await seedSession(page, cachedUser);
    await use(page);
  },
  adminPage: async ({ page }, use) => {
    cachedAdmin ??= await fetchAuthedUser(ADMIN_USER);
    await seedSession(page, cachedAdmin);
    await use(page);
  },
});

export { expect } from '@playwright/test';
