import authHeader from "./apihelper";

const API_URL = `${import.meta.env.VITE_API_URL}/recommendations`;

/**
 * Returns an ordered list of recommended song IDs for the given playlist.
 * The list contains at most topK entries (server-side cap is 50).
 */
export async function apifetchrecommendationsforplaylist(
    playlistId: string,
    topK: number = 5
): Promise<string[]> {
    const url = `${API_URL}/for-playlist/${playlistId}?topK=${topK}`;
    const response = await fetch(url, {
        method: "GET",
        headers: { ...authHeader(url) },
    });

    const data = await response.json();
    if (!response.ok) {
        throw new Error(data.message || "Fetching recommendations failed");
    }

    return data as string[];
}