import type { PlaylistInvitation } from "../../models/playlistinvitation";

/**
 * Helpers for working with PlaylistInvitation rows from the current
 * user's perspective. Parallel to friendshiphelpers.ts — the
 * invitation entity has the same sender/receiver shape and the same
 * "show me the OTHER party" UI need.
 *
 * Kept in a separate file (rather than overloading friendshiphelpers)
 * so the types stay tight and grepping for `otherUserName` from either
 * feature points at the right helper.
 */

export function otherUserId(row: PlaylistInvitation, currentUserId: string): string {
    return row.senderId === currentUserId ? row.receiverId : row.senderId;
}

export function otherUserName(row: PlaylistInvitation, currentUserId: string): string {
    return row.senderId === currentUserId ? row.receiverUserName : row.senderUserName;
}

export function otherFriendCode(row: PlaylistInvitation, currentUserId: string): string {
    return row.senderId === currentUserId ? row.receiverFriendCode : row.senderFriendCode;
}

/**
 * Direction of the row relative to the current user. Useful for the
 * UI to decide whether to show "Accept/Decline" buttons (incoming) or
 * "Cancel" (outgoing).
 */
export function invitationDirection(
    row: PlaylistInvitation,
    currentUserId: string,
): "incoming" | "outgoing" {
    return row.receiverId === currentUserId ? "incoming" : "outgoing";
}