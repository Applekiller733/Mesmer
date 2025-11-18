import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../../themes/themes";
import { Box, LinearProgress, Paper } from "@mui/material";
import Navbar from "../../../reusablecomponents/navbar";
import "./listen-playlist.css";
import VerticalCarousel from "../../../reusablecomponents/carousel/verticalcarousel";
import { useParams } from "react-router";
import { useAppDispatch } from "../../../hooks/hooks";
import { selectLoadedPlaylist } from "../../../stores/slices/playlistdataslice";
import { useSelector } from "react-redux";
import SongComponent from "../../../reusablecomponents/song/songcomponent";
import type { EmblaOptionsType } from "embla-carousel";
import { selectCurrentSongIndex, setIndex } from "../../../stores/slices/songdataslice";
import { useEffect } from "react";

export default function ListenPlaylist({ id }
    :
    { id: any }) {
    const dispatch = useAppDispatch();
    const playlist = useSelector(selectLoadedPlaylist);
    const songIds = playlist.songs.map((s) => s.id);
    const currentIndex = useSelector(selectCurrentSongIndex);

    useEffect(() => {
        dispatch(setIndex(0));
    }, [])

    function handleSongEnded() {
        onNext();
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


    const SLIDES = playlist.songs.map((s) => {
        let autoplay = undefined;
        if (s.id === playlist.songs[currentIndex].id) {
            autoplay = true;
        }
        return (
        <SongComponent song={s} hideActions={true} onEnded={handleSongEnded} autoplay={autoplay}></SongComponent>
        );
    })
    const OPTIONS: EmblaOptionsType = { axis: 'y', dragFree: false, watchDrag: false };

    return (
        <ThemeProvider theme={darkTheme}>
            <Box>
                <VerticalCarousel
                    slides={SLIDES}
                    options={OPTIONS}
                    currentSlideIndex={currentIndex}
                    onNext={onNext}
                    onPrevious={onPrevious}
                ></VerticalCarousel>
            </Box>
        </ThemeProvider>
    )
}