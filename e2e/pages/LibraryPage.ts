import { type Page, type Locator, expect } from '@playwright/test';

export class LibraryPage {
  readonly page: Page;
  readonly createPlaylistButton: Locator;
  readonly playlistNameInput: Locator;
  readonly submitButton: Locator;

  constructor(page: Page) {
    this.page = page;
    // "Create Playlist" button lives at the bottom of the SideList
    this.createPlaylistButton = page.getByRole('button', {
      name: 'Create Playlist',
      exact: true,
    });
    this.playlistNameInput = page.locator('#name');

    this.submitButton = page.getByRole('button', {
      name: 'Save Playlist',
      exact: true,
    });
  }

  async goto() {
    await this.page.goto('/library');
    await expect(this.createPlaylistButton).toBeVisible();
  }

  async openCreatePlaylist() {
    await this.createPlaylistButton.click();
    await expect(this.playlistNameInput).toBeVisible();
  }

  async createPlaylist(name: string) {
    await this.playlistNameInput.fill(name);
    await this.submitButton.click();
  }

  async expectPlaylistVisible(name: string) {
    await expect(
      this.page.getByText(name, { exact: false }).first(),
    ).toBeVisible();
  }
}
