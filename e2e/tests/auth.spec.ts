import { test, expect } from '../fixtures/auth';
import { LoginPage } from '../pages/LoginPage';
import { SignupPage } from '../pages/SignupPage';
import { NavBar } from '../pages/NavBar';
import { TEST_USER, uniqueSignup } from '../utils/testData';

test.describe('Authentication', () => {
  test('valid credentials log the user in and redirect home', async ({ page }) => {
    const login = new LoginPage(page);
    await login.goto();
    await login.login(TEST_USER.email, TEST_USER.password);
    await login.expectLoginSuccess();

    // The navbar should now reflect a logged-in session
    const nav = new NavBar(page);
    await nav.expectLoggedIn();
  });

  test('invalid credentials surface an error and keep the user out', async ({ page }) => {
    const login = new LoginPage(page);
    await login.goto();
    await login.login(TEST_USER.email, 'definitely-the-wrong-password');
    await login.expectLoginFailure();
    await expect(page).toHaveURL(/\/login/);
  });

  test('a new user can register', async ({ page }) => {
    const signup = new SignupPage(page);
    await signup.goto();
    await signup.register(uniqueSignup());
    await signup.expectRegistrationSuccess();
  });

  test('logging out returns to a logged-out navbar', async ({ authedPage }) => {
    await authedPage.goto('/');
    const nav = new NavBar(authedPage);
    await nav.expectLoggedIn();
    await nav.logoutButton.click();
    await nav.expectLoggedOut();
  });
});
