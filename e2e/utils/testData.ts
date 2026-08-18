export const API_URL =
  process.env.E2E_API_URL ?? 'http://localhost:4000';

/** A standard verified, non-admin user used by most logged-in flows. */
export const TEST_USER = {
  email: process.env.E2E_USER_EMAIL ?? 'e2e.user@mesmer.test',
  password: process.env.E2E_USER_PASSWORD ?? 'e2ePassword123',
} as const;

/** An admin account, used only by the admin-visibility check. */
export const ADMIN_USER = {
  email: process.env.E2E_ADMIN_EMAIL ?? 'e2e.admin@mesmer.test',
  password: process.env.E2E_ADMIN_PASSWORD ?? 'e2ePassword123',
} as const;

/**
 * Generates a unique registration payload so the registration test can run
 * repeatedly without colliding on a taken email or username. The timestamp
 * plus random suffix keeps it unique across parallel-ish reruns
 */
export function uniqueSignup() {
  const suffix = `${Date.now()}${Math.floor(Math.random() * 1000)}`;
  return {
    email: `e2e.signup.${suffix}@mesmer.test`,
    username: `e2e_signup_${suffix}`,
    password: 'e2ePassword123',
  };
}
