import type { Friendship } from "../../stores/api/friendshipapi";
 
export function otherUserId(row: Friendship, currentUserId: string): string {
    return row.senderId === currentUserId ? row.receiverId : row.senderId;
}
 
export function otherUserName(row: Friendship, currentUserId: string): string {
    return row.senderId === currentUserId ? row.receiverUserName : row.senderUserName;
}
 
export function otherFriendCode(row: Friendship, currentUserId: string): string {
    return row.senderId === currentUserId ? row.receiverFriendCode : row.senderFriendCode;
}
 
/**
 * Convert a raw friend code ("ABCXYZ") to the display form ("ABC-XYZ").
 * Mirrors FriendCodeGenerator.ToDisplay on the backend.
 */
export function formatFriendCode(raw: string | undefined | null): string {
    if (!raw || raw.length !== 6) return raw ?? "";
    return `${raw.slice(0, 3)}-${raw.slice(3)}`;
}
