import { type Page, type Locator, expect } from '@playwright/test';

export class SignupPage {
  readonly page: Page;
  readonly emailInput: Locator;
  readonly usernameInput: Locator;
  readonly passwordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly submitButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.emailInput = page.locator('#email');
    this.usernameInput = page.locator('#username');
    this.passwordInput = page.locator('#password');
    this.confirmPasswordInput = page.locator('#confirmpassword');
    this.submitButton = page.getByRole('button', { name: 'Sign up', exact: false });
  }

  async goto() {
    await this.page.goto('/signup');
    await expect(this.emailInput).toBeVisible();
  }

  async register(data: {
    email: string;
    username: string;
    password: string;
  }) {
    await this.emailInput.fill(data.email);
    await this.usernameInput.fill(data.username);
    await this.passwordInput.fill(data.password);
    await this.confirmPasswordInput.fill(data.password);
    await this.submitButton.click();
  }

  async expectRegistrationSuccess() {
    await expect(this.page).toHaveURL(/\/signup-success/);
  }
}
