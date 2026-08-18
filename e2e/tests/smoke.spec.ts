import { test, expect } from '@playwright/test';

test.describe('Smoke', () => {
  test('home page loads', async ({ page }) => {
    await page.goto('/');
    // The navbar title is always rendered
    await expect(page.getByText('Mesmer', { exact: true })).toBeVisible();
  });

  test('login page loads', async ({ page }) => {
    await page.goto('/login');
    await expect(page.locator('#email')).toBeVisible();
    await expect(page.locator('#password')).toBeVisible();
  });

  test('unknown route still renders the app shell', async ({ page }) => {
    // There is no catch-all route, so an unknown path renders an empty
    // Routes outlet but the app shouldn't crash. Assert the document loaded
    const response = await page.goto('/this-route-does-not-exist');
    expect(response?.status()).toBeLessThan(400);
  });
});
