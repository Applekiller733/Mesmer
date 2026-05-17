import authHeader from "./apihelper";
import type {Playlist, CreatePlaylistRequest, UpdatePlaylistRequest, DeletePlaylistRequest, PlaylistVisibility} from "../../models/playlist";

const API_URL = `${import.meta.env.VITE_API_URL}/playlists`;

export async function apifetchplaylists(){
    const url = `${API_URL}`;
    const response = await fetch(url, {
        method:"GET",
        headers: {"Content-Type":"application/json", ...authHeader(url)},
    })

    const data = await response.json();
    if (!response.ok){
        throw new Error(data.message || "Fetching Playlists failed");
    }

    return data;
    // .then(response => response.json())
    // .then(response => {
    //     return response.map((p:any) => {
    //         const playlist:Playlist = {
    //             id: p.id,
    //             name: p.name,
    //             createdAt: p.createdAt,
    //             updatedAt: p.updatedAt,
    //             songs: p.songs,
    //         }
    //         return playlist;
    //     })
    // })
}

export async function apifetchplaylistbyid(id:string){
    const url = `${API_URL}/${id}`;
    const response = await fetch(url, {
        method: "GET",
        headers: {"Content-Type": "application/json", ...authHeader(url)},
    })

    const data = await response.json();
    if (!response.ok){
        throw new Error(data.message || "Fetching Playlist by Id failed");
    }

    return data;
    // .then(response => response.json())
    // .then(p => {
    //     const playlist:Playlist = {
    //         id: p.id,
    //         name: p.name,
    //         createdAt: p.createdAt,
    //         updatedAt: p.updatedAt,
    //         songs: p.songs,
    //     }
    //     return playlist;
    // })
}

export async function apifetchplaylistscreatedbyaccount(accountid:string){
    const url = `${API_URL}/made-by/${accountid}`
    const response = await fetch(url, {
        method: "GET",
        headers: {"Content-Type": "application/json", ...authHeader(url)},
    })

    const data = await response.json();
    if (!response.ok){
        throw new Error(data.message || "Fetching Playlists created by Account failed");
    }

    return data;
    // .then (response => response.json())
    // .then (response => {
    //     return response.map((p:any) => {
    //         const playlist:Playlist = {
    //             id: p.id,
    //             name: p.name,
    //             createdAt: p.createdAt,
    //             updatedAt: p.updatedAt,
    //             songs: p.songs,
    //         }
    //         return playlist;
    //     })
    // })
}

export async function apifetchplaylistssavedbyaccount(accountid:string){
    const url = `${API_URL}/saved-by/${accountid}`
    const response = await fetch(url, {
        method: "GET",
        headers: {"Content-Type": "application/json", ...authHeader(url)},
    })

    const data = await response.json();
    if (!response.ok){
        throw new Error(data.message || "Fetching Playlist saved by Account failed");
    }

    return data;
}

export async function apicreateplaylist(request: CreatePlaylistRequest) {
    const url = `${API_URL}/create-playlist`;
    const response = await fetch(url, {
        method: "POST",
        headers: {"Content-Type": "application/json", ...authHeader(url)},
        body: JSON.stringify(request),
    })

    const data = await response.json();
    if (!response.ok){
        throw new Error(data.message || "Creating Playlist failed");
    }

    return data;
}

export async function apiupdateplaylist(request: UpdatePlaylistRequest){
    const url = `${API_URL}`;
    const response = await fetch(url, {
        method: "PUT",
        headers: {"Content-Type": "application/json", ...authHeader(url)},
        body: JSON.stringify(request),
    })

    const data = await response.json();
    if (!response.ok){
        throw new Error(data.message || "Updating Playlist failed");
    }

    return data;
}

export async function apideleteplaylist(request: DeletePlaylistRequest){
    const url = `${API_URL}`;
    const response = await fetch(url, {
        method: "DELETE",
        headers: {"Content-Type": "application/json", ...authHeader(url)},
        body: JSON.stringify(request),
    })

    const data = await response.json();
    if (!response.ok){
        throw new Error(data.message || "Deleting Playlist failed");
    }

    return data;
}

// ---------------------------------------------------------------------------
// Step 4 endpoints: save, unsave, change-visibility
// ---------------------------------------------------------------------------

/**
 * Add a playlist to the current user's library. The backend validates
 * visibility (Public → anyone; Unlisted → only users with a pending
 * invitation; Private → 404), so this can be called optimistically;
 * surface the error message if the response isn't OK.
 *
 * Returns the saved Playlist so the caller can immediately render or
 * navigate to it without a follow-up fetch.
 */
export async function apisaveplaylist(playlistId: string): Promise<Playlist> {
    const url = `${API_URL}/${playlistId}/save`;
    const response = await fetch(url, {
        method: "POST",
        headers: { ...authHeader(url) },
    });
    const data = await response.json();
    if (!response.ok) {
        throw new Error(data.message || "Saving playlist failed");
    }
    return data as Playlist;
}

/**
 * Remove a playlist from the current user's library. The backend
 * rejects owners trying to unsave their own playlists — surface that
 * error so the UI can prompt "delete instead" rather than silently
 * failing.
 */
export async function apiunsaveplaylist(playlistId: string): Promise<void> {
    const url = `${API_URL}/${playlistId}/save`;
    const response = await fetch(url, {
        method: "DELETE",
        headers: { ...authHeader(url) },
    });
    if (!response.ok) {
        const data = await response.json().catch(() => ({}));
        throw new Error(data.message || "Unsaving playlist failed");
    }
}

/**
 * Owner-only: change a playlist's visibility. Backend side effect:
 * transitioning to Private auto-cancels pending invitations for this
 * playlist. The returned Playlist reflects the new state.
 */
export async function apiupdateplaylistvisibility(
    playlistId: string,
    visibility: PlaylistVisibility,
): Promise<Playlist> {
    const url = `${API_URL}/${playlistId}/visibility`;
    const response = await fetch(url, {
        method: "PATCH",
        headers: { "Content-Type": "application/json", ...authHeader(url) },
        body: JSON.stringify({ visibility }),
    });
    const data = await response.json();
    if (!response.ok) {
        throw new Error(data.message || "Updating playlist visibility failed");
    }
    return data as Playlist;
}