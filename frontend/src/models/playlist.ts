import type {Song} from "./song";

export interface Playlist{
    id: string,
    name: string,
    createdAt: string,
    updatedAt: string,
    songs: Song[],
}

export interface CreatePlaylistRequest {
    name:string, 
    songIds: string[],
}

export interface UpdatePlaylistRequest {
    id: number,
    name:string,
    songIds: string[],
}

export interface DeletePlaylistRequest {
    id: number,
}