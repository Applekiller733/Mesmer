import { type Page, type Locator, expect } from '@playwright/test';

export class ForYouPage {
  readonly page: Page;
  readonly songCards: Locator;
  readonly modeSelect: Locator;

  constructor(page: Page) {
    this.page = page;
    this.songCards = page.locator('.songcomponent');
    // MUI Select rendered with the "Recommend from" label.
    this.modeSelect = page.getByLabel('Recommend from');
  }

  async goto() {
    await this.page.goto('/for-you');
  }

  // At least one song should render once the feed loads
  async expectSongsLoaded() {
    await expect(this.songCards.first()).toBeVisible();
  }

  async selectRecommendationPlaylist(playlistName: string) {
    await this.modeSelect.click();
    await this.page
      .getByRole('option', { name: playlistName, exact: false })
      .click();
  }

  /**
   * Switches the seed back to the full catalogue.
   */
  async selectAllSongs() {
    await this.modeSelect.click();
    await this.page
      .getByRole('option', { name: 'None (all songs)', exact: false })
      .click();
  }
}
