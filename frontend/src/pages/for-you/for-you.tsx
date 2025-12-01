
import { ThemeProvider } from '@emotion/react';
import './for-you.css'
import { darkTheme } from '../../themes/themes';
import { Box, CircularProgress } from '@mui/material';
import Navbar from '../../reusablecomponents/navbar';
import VerticalCarousel from '../../reusablecomponents/carousel/verticalcarousel';
import { useSelector } from 'react-redux';
import { loadSong, selectAllSongIds, selectAllSongs, selectCurrentSongIndex, selectLoadedSongs, setIndex, unloadSong } from '../../stores/slices/songdataslice';
import { useAppDispatch } from '../../hooks/hooks';
import React, { useCallback, useEffect, useState } from 'react';
import SongComponent from '../../reusablecomponents/song/songcomponent';
import type { EmblaOptionsType } from 'embla-carousel';
import { fetchSongById, fetchSongIds, fetchSongs } from '../../stores/thunks/songthunks';
import useEmblaCarousel from 'embla-carousel-react';

export default function ForYou() {
    const [status, setStatus] = useState('init');
    // const songs = useSelector(selectAllSongs);
    const loadedSongs = useSelector(selectLoadedSongs);
    const songIds = useSelector(selectAllSongIds);
    const currentIndex = useSelector(selectCurrentSongIndex);
    const dispatch = useAppDispatch();
    console.log(currentIndex);

    useEffect(() => {
        dispatch(fetchSongIds());
    }, [])

    useEffect(() => {
        (async () => {
            await manageLoadedSongs();
        })();
    }, [songIds, currentIndex])

    async function manageLoadedSongs() {
        // console.log("INSIDE MANAGE LOADED CONTENT");
        // console.log(songIds);
        const idsToKeep = [
            songIds[currentIndex - 1],
            songIds[currentIndex],
            songIds[currentIndex + 1],
        ].filter(Boolean);
        console.log(idsToKeep);

        for (const id of idsToKeep) {
            if (!loadedSongs[id]) {
                const song = await dispatch(fetchSongById(id)).unwrap();
                dispatch(loadSong(song));
            }
        }

        for (const loadedId of Object.keys(loadedSongs)) {
            if (!idsToKeep.includes(loadedId)) {
                dispatch(unloadSong(loadedId));
            }
        }
    }

    function onNext() {
        if (currentIndex < songIds.length - 1) {
            dispatch(setIndex(currentIndex + 1));
        }
    }

    function onPrevious() {
        if (currentIndex > 0) {
            dispatch(setIndex(currentIndex - 1));
        }
    }

    const OPTIONS: EmblaOptionsType = { axis: 'y', dragFree: false, watchDrag: false };
    const slideData = [currentIndex - 1, currentIndex, currentIndex + 1]
        .filter(i => i >= 0 && i < songIds.length)
        .map(i => {
            const id = songIds[i];
            const song = loadedSongs[id];
            return { id, element: song ? <SongComponent key={id} song={song}  /> : <div key={id}>Loading...</div> };
        }).filter(Boolean);
    const SLIDES = slideData.map(s => s.element);
    const currentSlideIndex = slideData.findIndex(s => s.id === songIds[currentIndex]);

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="for-you">
                <Navbar></Navbar>
                <div className="song-list">
                    <VerticalCarousel
                        slides={SLIDES}
                        options={OPTIONS}
                        onNext={onNext}
                        onPrevious={onPrevious}
                        currentSlideIndex={currentSlideIndex}
                    ></VerticalCarousel>
                </div>
            </Box>
        </ThemeProvider>
    );
}