import authHeader from "./apihelper";

const API_URL = `${import.meta.env.VITE_API_URL}/friendships`;

export interface Friendship {
    id: string;
    senderId: string;
    receiverId: string;
    status: number;          // FriendshipStatus enum value
    createdAt: string;
    updatedAt: string | null;
}

export interface RelationshipStatus {
    status: number | null;        // null when no row exists
    isCurrentUserSender: boolean;
    isSelf: boolean;
}

export async function apigetrelationship(userId: string): Promise<RelationshipStatus> {
    const url = `${API_URL}/with/${userId}`;
    const response = await fetch(url, {
        method: "GET",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Fetching relationship failed");
    return data as RelationshipStatus;
}

export async function apisendfriendrequest(userId: string): Promise<Friendship> {
    const url = `${API_URL}/request/${userId}`;
    const response = await fetch(url, {
        method: "POST",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Sending request failed");
    return data as Friendship;
}

export async function apiacceptrequest(friendshipId: string): Promise<Friendship> {
    const url = `${API_URL}/${friendshipId}/accept`;
    const response = await fetch(url, {
        method: "POST",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Accepting request failed");
    return data as Friendship;
}

export async function apideclinerequest(friendshipId: string): Promise<void> {
    const url = `${API_URL}/${friendshipId}/decline`;
    const response = await fetch(url, {
        method: "POST",
        headers: { ...authHeader(url) },
    });
    if (!response.ok) {
        const data = await response.json().catch(() => ({}));
        throw new Error(data.message || "Declining request failed");
    }
}

export async function apiremovefriend(userId: string): Promise<void> {
    const url = `${API_URL}/with/${userId}`;
    const response = await fetch(url, {
        method: "DELETE",
        headers: { ...authHeader(url) },
    });
    if (!response.ok) {
        const data = await response.json().catch(() => ({}));
        throw new Error(data.message || "Removing friend failed");
    }
}

export async function apiblockuser(userId: string): Promise<Friendship> {
    const url = `${API_URL}/block/${userId}`;
    const response = await fetch(url, {
        method: "POST",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Blocking user failed");
    return data as Friendship;
}

export async function apiunblockuser(userId: string): Promise<void> {
    const url = `${API_URL}/block/${userId}`;
    const response = await fetch(url, {
        method: "DELETE",
        headers: { ...authHeader(url) },
    });
    if (!response.ok) {
        const data = await response.json().catch(() => ({}));
        throw new Error(data.message || "Unblocking user failed");
    }
}


export async function apigetfriends(): Promise<Friendship[]> {
    const url = `${API_URL}/friends`;
    const response = await fetch(url, {
        method: "GET",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Fetching friends failed");
    return data as Friendship[];
}

export async function apigetincoming(): Promise<Friendship[]> {
    const url = `${API_URL}/incoming`;
    const response = await fetch(url, {
        method: "GET",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Fetching incoming requests failed");
    return data as Friendship[];
}

export async function apigetoutgoing(): Promise<Friendship[]> {
    const url = `${API_URL}/outgoing`;
    const response = await fetch(url, {
        method: "GET",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Fetching outgoing requests failed");
    return data as Friendship[];
}

export async function apigetblocked(): Promise<Friendship[]> {
    const url = `${API_URL}/blocked`;
    const response = await fetch(url, {
        method: "GET",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Fetching blocked failed");
    return data as Friendship[];
}

export async function apigetincomingcount(): Promise<number> {
    const url = `${API_URL}/incoming/count`;
    const response = await fetch(url, {
        method: "GET",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Fetching incoming count failed");
    return data.count as number;
}