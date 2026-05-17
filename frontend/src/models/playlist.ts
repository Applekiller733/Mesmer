import type { Song } from "./song";

export const PlaylistVisibility = {
    Private: "Private",
    Unlisted: "Unlisted",
    Public: "Public",
} as const;
export type PlaylistVisibility =
    typeof PlaylistVisibility[keyof typeof PlaylistVisibility];

export function visibilityLabel(v: PlaylistVisibility): string {
    switch (v) {
        case PlaylistVisibility.Private: return "Private";
        case PlaylistVisibility.Unlisted: return "Unlisted";
        case PlaylistVisibility.Public: return "Public";
        default: return "Unknown";
    }
}

export interface Playlist {
    id: string,
    name: string,
    createdAt: string,
    updatedAt: string,
    visibility: PlaylistVisibility,
    songs: Song[],
    createdBy?: {
        id: string,
        userName?: string,
    },
}

export interface CreatePlaylistRequest {
    name: string,
    songIds: string[],
    visibility?: PlaylistVisibility,
}

export interface UpdatePlaylistRequest {
    id: string,
    name: string,
    songIds: string[],
}

export interface DeletePlaylistRequest {
    id: string,
}