import authHeader from "./apihelper";
import type {Playlist, CreatePlaylistRequest, UpdatePlaylistRequest, DeletePlaylistRequest} from "../../models/playlist";

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

export async function apifetchplaylistbyid(id:number){
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

export async function apifetchplaylistscreatedbyaccount(accountid:number){
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

export async function apifetchplaylistssavedbyaccount(accountid:number){
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
    // .then(response => response.json())
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