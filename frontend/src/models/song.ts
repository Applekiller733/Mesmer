export interface Song {
    id: string,
    name: string,
    artist: string,
    upvotes: number,
    likedByAccountIds?: string[],
    imageUrl?: string,
    videoUrl?: string,
    soundUrl?: string,
}


//todo modify when adding the file functionality
export interface CreateSongRequest {
    name: string,
    artist: string,
    imageUrl?: string,
    videoUrl?: string,
    soundUrl?: string,
}

export interface DeleteSongRequest {
    id: string,
}

export interface FlipLikeRequest {
    id:string,
}