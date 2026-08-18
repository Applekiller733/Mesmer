import { type Page, type Locator, expect } from '@playwright/test';


export class NavBar {
  readonly page: Page;
  readonly homeLink: Locator;
  readonly forYouLink: Locator;
  readonly libraryLink: Locator;
  readonly socialsLink: Locator;
  readonly adminLink: Locator;
  readonly profileLink: Locator;
  readonly loginLink: Locator;
  readonly signupLink: Locator;
  readonly logoutButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.homeLink = page.getByRole('link', { name: 'Home', exact: true });
    this.forYouLink = page.getByRole('link', { name: 'For You', exact: true });
    this.libraryLink = page.getByRole('link', { name: 'Library', exact: true });
    this.socialsLink = page.getByRole('link', { name: 'Socials', exact: true });
    this.adminLink = page.getByRole('link', { name: 'Admin', exact: true });
    this.profileLink = page.getByRole('link', { name: 'Profile', exact: true });
    this.loginLink = page.getByRole('link', { name: 'Login', exact: true });
    this.signupLink = page.getByRole('link', { name: 'Sign Up', exact: true });
    this.logoutButton = page.getByRole('button', { name: 'Logout', exact: true });
  }

  async expectLoggedIn() {
    await expect(this.logoutButton).toBeVisible();
    await expect(this.libraryLink).toBeVisible();
  }

  async expectLoggedOut() {
    await expect(this.loginLink).toBeVisible();
    await expect(this.signupLink).toBeVisible();
  }

  async goToLibrary() {
    await this.libraryLink.click();
    await expect(this.page).toHaveURL(/\/library/);
  }

  async goToForYou() {
    await this.forYouLink.click();
    await expect(this.page).toHaveURL(/\/for-you/);
  }
}
