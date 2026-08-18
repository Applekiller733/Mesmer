import { test, expect } from '../fixtures/auth';
import { NavBar } from '../pages/NavBar';

test.describe('Navigation', () => {
  test('logged-out visitor sees only public nav entries', async ({ page }) => {
    await page.goto('/');
    const nav = new NavBar(page);
    await nav.expectLoggedOut();
    await expect(nav.libraryLink).toHaveCount(0);
    await expect(nav.forYouLink).toHaveCount(0);
  });

  test('logged-in user can reach Library and For You', async ({ authedPage }) => {
    await authedPage.goto('/');
    const nav = new NavBar(authedPage);
    await nav.expectLoggedIn();

    await nav.goToLibrary();
    await nav.goToForYou();
  });

  test('non-admin user does not see the Admin entry', async ({ authedPage }) => {
    await authedPage.goto('/');
    const nav = new NavBar(authedPage);
    await nav.expectLoggedIn();
    await expect(nav.adminLink).toHaveCount(0);
  });

  test('admin user sees the Admin entry', async ({ adminPage }) => {
    await adminPage.goto('/');
    const nav = new NavBar(adminPage);
    await expect(nav.adminLink).toBeVisible();
  });
});
