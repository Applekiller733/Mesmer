import authHeader from "./apihelper";
import type { Playlist } from "../../models/playlist";
import type { PlaylistInvitation } from "../../models/playlistinvitation";

const API_URL = `${import.meta.env.VITE_API_URL}/playlist-invitations`;

/**
 * Send an invitation. Authority depends on the playlist's visibility
 * (see backend PlaylistInvitationService.Invite):
 *   - Private: rejected.
 *   - Unlisted: owner only.
 *   - Public: anyone who can see it.
 *
 * Idempotent: re-inviting the same user to the same playlist returns
 * the existing pending row rather than erroring. Shadow-blocks (receiver
 * has blocked sender) return a synthetic-looking success with no real
 * row inserted — same pattern as friend requests.
 */
export async function apiinviteplaylist(
    playlistId: string,
    userId: string,
): Promise<PlaylistInvitation> {
    const url = `${API_URL}/invite/${playlistId}/${userId}`;
    const response = await fetch(url, {
        method: "POST",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Sending invitation failed");
    return data as PlaylistInvitation;
}

/**
 * Accept an incoming invitation. Backend side effects: receiver is
 * added to SavedByAccounts AND the invitation row is deleted.
 *
 * Returns the freshly-saved Playlist so callers can navigate or render
 * without a second fetch. The invitation row no longer exists after
 * this call — the caller should drop it from any local cache.
 */
export async function apiacceptinvitation(invitationId: string): Promise<Playlist> {
    const url = `${API_URL}/${invitationId}/accept`;
    const response = await fetch(url, {
        method: "POST",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Accepting invitation failed");
    return data as Playlist;
}

/**
 * Decline an incoming invitation. Removes the row, no other side
 * effects. Only the receiver may decline.
 */
export async function apideclineinvitation(invitationId: string): Promise<void> {
    const url = `${API_URL}/${invitationId}/decline`;
    const response = await fetch(url, {
        method: "POST",
        headers: { ...authHeader(url) },
    });
    if (!response.ok) {
        const data = await response.json().catch(() => ({}));
        throw new Error(data.message || "Declining invitation failed");
    }
}

/**
 * Sender retracts their own outgoing invitation before it's accepted
 * or declined. Distinct from Decline only in who's authorised (sender
 * vs receiver). The row deletion is the same.
 */
export async function apicancelinvitation(invitationId: string): Promise<void> {
    const url = `${API_URL}/${invitationId}`;
    const response = await fetch(url, {
        method: "DELETE",
        headers: { ...authHeader(url) },
    });
    if (!response.ok) {
        const data = await response.json().catch(() => ({}));
        throw new Error(data.message || "Cancelling invitation failed");
    }
}

/**
 * Pending invitations sent to the current user. Backs the inbox in
 * both the notification popper and the Socials → Playlists tab.
 */
export async function apigetincominginvitations(): Promise<PlaylistInvitation[]> {
    const url = `${API_URL}/incoming`;
    const response = await fetch(url, {
        method: "GET",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Fetching incoming invitations failed");
    return data as PlaylistInvitation[];
}

/**
 * Pending invitations the current user has sent. For the "I shared
 * this with X" outbox view on the Socials → Playlists tab.
 */
export async function apigetoutgoinginvitations(): Promise<PlaylistInvitation[]> {
    const url = `${API_URL}/outgoing`;
    const response = await fetch(url, {
        method: "GET",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Fetching outgoing invitations failed");
    return data as PlaylistInvitation[];
}

/**
 * Count of pending incoming invitations. Cheap query — used by the
 * navbar badge so we don't have to pull the full inbox on each poll.
 */
export async function apigetincominginvitationscount(): Promise<number> {
    const url = `${API_URL}/incoming/count`;
    const response = await fetch(url, {
        method: "GET",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Fetching invitations count failed");
    return data.count as number;
}