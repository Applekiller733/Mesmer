import { test } from '../fixtures/auth';
import { LibraryPage } from '../pages/LibraryPage';

test.describe('Playlist management', () => {
  test('user can create a playlist and see it in the library', async ({ authedPage }) => {
    const library = new LibraryPage(authedPage);
    await library.goto();

    // Unique name so reruns don't collide on an existing playlist
    const name = `E2E Playlist ${Date.now()}`;

    await library.openCreatePlaylist();
    await library.createPlaylist(name);

    // Back on the main library view, the new playlist should be listed
    await library.expectPlaylistVisible(name);
  });

  test('the create form is reachable via the deep link', async ({ authedPage }) => {
    // library.tsx supports ?action=create to open the create subpage
    await authedPage.goto('/library?action=create');
    const library = new LibraryPage(authedPage);
    await library.playlistNameInput.waitFor({ state: 'visible' });
  });
});
