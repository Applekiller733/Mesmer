import { test, expect } from '../fixtures/auth';
import { ForYouPage } from '../pages/ForYouPage';
import { LibraryPage } from '../pages/LibraryPage';

test.describe('Recommendations (For You)', () => {
  test('the feed loads songs in the default (all songs) mode', async ({ authedPage }) => {
    const forYou = new ForYouPage(authedPage);
    await forYou.goto();
    await forYou.expectSongsLoaded();
  });

  test('selecting a playlist seed reloads the feed from the microservice', async ({
    authedPage,
  }) => {
    // Ensure the user has at least one playlist to seed from. Create one
    // first so the test is self-contained rather than assuming seed data
    const seedName = `Rec Seed ${Date.now()}`;
    const library = new LibraryPage(authedPage);
    await library.goto();
    await library.openCreatePlaylist();
    await library.createPlaylist(seedName);
    await library.expectPlaylistVisible(seedName);

    const forYou = new ForYouPage(authedPage);
    await forYou.goto();
    await forYou.expectSongsLoaded();

    // Switch the recommendation seed to the playlist we just made. This
    // fires setModePlaylist, which calls the recommendation microservice
    await forYou.selectRecommendationPlaylist(seedName);

    // The selector reflects the chosen playlist and the feed still renders
    await expect(forYou.modeSelect).toContainText(seedName);
    await forYou.expectSongsLoaded();
  });
});
