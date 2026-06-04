import { Genre } from "./genre";

export interface Song {
    id: string,
    name: string,
    artist: string,
    upvotes: number,
    likedByAccountIds?: string[],
    imageUrl?: string,
    videoUrl?: string,
    soundUrl?: string,
    genre?: Genre,
}


export interface CreateSongRequest {
    name: string;
    artist: string;
    imageUrl?: string;
    videoUrl?: string;
    soundUrl?: string;
    soundFile?: File | null;
}

export interface UpdateSongRequest {
    id: string;
    name?: string;
    artist?: string;
    genre?: Genre;
}

export interface DeleteSongRequest {
    id: string,
}

export interface FlipLikeRequest {
    id: string,
}