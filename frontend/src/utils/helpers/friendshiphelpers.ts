import type { Friendship } from "../../stores/api/friendshipapi";

export function otherUserId(row: Friendship, currentUserId: string): string {
    return row.senderId === currentUserId ? row.receiverId : row.senderId;
}