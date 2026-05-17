import type { PlaylistVisibility } from "./playlist";

/**
 * A pending playlist-share invitation. Mirrors the backend
 * PlaylistInvitationResponse 1:1.
 *
 * Carries both sender and receiver fields so the same shape works for
 * incoming-inbox and outgoing-outbox views — the consumer picks
 * whichever side it cares about. Matches the Friendship-on-wire
 * convention used elsewhere.
 */
export interface PlaylistInvitation {
    id: string,

    playlistId: string,
    playlistName: string,
    playlistVisibility: PlaylistVisibility,

    senderId: string,
    senderUserName: string,
    senderFriendCode: string,

    receiverId: string,
    receiverUserName: string,
    receiverFriendCode: string,

    createdAt: string,
}