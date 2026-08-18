import { type Page, type Locator, expect } from '@playwright/test';

export class LoginPage {
  readonly page: Page;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorAlert: Locator;

  constructor(page: Page) {
    this.page = page;
    this.emailInput = page.locator('#email');
    this.passwordInput = page.locator('#password');
    this.submitButton = page.getByRole('button', { name: 'Login', exact: true });
    // The failure Alert renders with role=alert and the copy below.
    this.errorAlert = page.getByText('Login failed. Invalid credentials.');
  }

  async goto() {
    await this.page.goto('/login');
    await expect(this.emailInput).toBeVisible();
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }

  /** On success the app redirects to '/' (home). */
  async expectLoginSuccess() {
    await expect(this.page).toHaveURL(/\/$/);
  }

  async expectLoginFailure() {
    await expect(this.errorAlert).toBeVisible();
  }
}
