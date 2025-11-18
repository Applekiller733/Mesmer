
import type { Song, CreateSongRequest, DeleteSongRequest, FlipLikeRequest } from "../../models/song";
// import type Song from "../../models/song";
import authHeader from "./apihelper";


const API_URL = `${import.meta.env.VITE_API_URL}/songs`;

const songdata = [
    {
        id: '1',
        name: 'Island in the Sun',
        artist: 'Weezer',
        upvotes: 6,
        video: 'https://www.youtube.com/embed/erG5rgNYSdk?si=iiBLEAVvbKEmtGtQ'
    },
    {
        id: '2',
        name: 'Passenger',
        artist: 'Deftones',
        upvotes: 10,
        image: 'https://i1.sndcdn.com/artworks-cA8SCHiCogzH-0-t500x500.jpg',
        sound: 'https://www.youtube.com/watch?v=IjainiB8mk4'
    },
    {
        id: '3',
        name: 'Placeholder',
        artist: '???',
        upvotes: 420,
        sound: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ'
    },
    {
        id: '4',
        name: 'Warrior',
        artist: "Disturbed",
        upvotes: 3451,
        image: 'https://i1.sndcdn.com/artworks-000140282568-baa0pd-t500x500.jpg',
        sound: 'https://www.youtube.com/watch?v=O-1Q25kb-2w'
    },
    {
        id: '5',
        name: 'Runaway',
        artist: 'Linking Park',
        upvotes: 65472,
        image: 'https://linkinpedia.com/www/w/images/thumb/c/c5/Studio-Hybrid_Theory_Cover.jpg/1200px-Studio-Hybrid_Theory_Cover.jpg',
        sound: 'https://www.youtube.com/watch?v=ig-fyQqf510',
    },
    {
        id: '6',
        name: 'Time Window',
        artist: 'Simon Viklund',
        upvotes: 486,
        image: 'https://i.scdn.co/image/ab67616d0000b2739aa7a825a468735e9e841780',
        sound: 'https://www.youtube.com/watch?v=9vmM7tuHPcI',
    }
]

export function fetchSongsMocked() {
    return new Promise(resolve => setTimeout(() => { resolve(songdata) }, 500));
}

//todo test if it properly returns the song list
export async function apifetchsongs() {
    const url = `${API_URL}`;
    const response = await fetch(url, {
        method: "GET",
        headers: { "Content-Type": "application/json" }
    })

    const data = await response.json();

    if (!response.ok) {
        throw new Error(data.message || "Fetching Songs failed");
    }

    return data;
    // .then(response => response.json())
    // .then(response => {
    //     return response.map((s: any) => {
    //         const song: Song = {
    //             id: s.id,
    //             name: s.name,
    //             artist: s.artist,
    //             upvotes: s.upvotes,
    //             image: s.image,
    //             video: s.video,
    //             sound: s.sound,
    //         }
    //         return song;
    //     })
    // }
    // )
}

export async function apifetchsongids() {
    const url = `${API_URL}/song-ids`;
    const response = await fetch(url, {
        method: "GET",
        headers: { "Content-Type": "application/json" }
    })

    const data = await response.json();

    if (!response.ok) {
        throw new Error(data.message || "Fetching Song Ids failed");
    }

    return data;
}

//todo test if it properly returns song
export async function apifetchsongbyid(id: number) {
    const url = `${API_URL}/${id}`;
    const response = await fetch(url, {
        method: "GET",
        headers: { "Content-Type": "application/json" }
    })
    const data = await response.json();

    if (!response.ok) {
        throw new Error(data.message || "Fetching Song by Id failed");
    }

    return data;
    // .then(response => response.json())
    // .then(s => {
    //     const song: Song = {
    //         id: s.id,
    //         name: s.name,
    //         artist: s.artist,
    //         upvotes: s.upvotes,
    //         imageUrl: s.imageUrl,
    //         videoUrl: s.videoUrl,
    //         soundUrl: s.soundUrl,
    //     }
    //     return song;
    // })
}

export async function apicreatesong(request: CreateSongRequest) {
    const url = `${API_URL}/create-song`;
    console.log(request);
    const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", ...authHeader(url) },
        body: JSON.stringify(request),
    })
    const data = await response.json();

    if (!response.ok) {
        throw new Error(data.message || "Creating Song failed");
    }

    return data;
}

export async function apideletesong(request: DeleteSongRequest) {
    const url = `${API_URL}`;
    const response = await fetch(url, {
        method: "DELETE",
        headers: { "Content-Type": "application/json", ...authHeader(url) },
        body: JSON.stringify(request)
    })
    const data = await response.json();

    if (!response.ok) {
        throw new Error(data.message || "Deleting Song failed");
    }

    return data;
}

export async function apifliplike(request: FlipLikeRequest) {
    const url = `${API_URL}/flip-like`;
    const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", ...authHeader(url) },
        body: JSON.stringify(request),
    })
    const data = await response.json();

    if (!response.ok) {
        throw new Error(data.message || "Flipping Like failed");
    }

    return data;
}