import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../../themes/themes";
import { Box } from "@mui/material";
import "./listen-playlist.css";
import VerticalCarousel from "../../../reusablecomponents/carousel/verticalcarousel";
import { useAppDispatch } from "../../../hooks/hooks";
import { selectLoadedPlaylist } from "../../../stores/slices/playlistdataslice";
import { useSelector } from "react-redux";
import SongComponent from "../../../reusablecomponents/song/songcomponent";
import type { EmblaOptionsType } from "embla-carousel";
import { selectCurrentSongIndex, setIndex } from "../../../stores/slices/songdataslice";
import { useEffect } from "react";

export default function ListenPlaylist({ id }: { id: any }) {
    const dispatch = useAppDispatch();
    const playlist = useSelector(selectLoadedPlaylist);
    const currentIndex = useSelector(selectCurrentSongIndex);

    useEffect(() => {
        dispatch(setIndex(0));
    }, [dispatch]);

    function handleIndexChange(newIndex: number) {
        dispatch(setIndex(newIndex));
    }

    function handleSongEnded() {
        if (currentIndex < playlist.songs.length - 1) {
            dispatch(setIndex(currentIndex + 1));
        }
    }

    const SLIDES = playlist.songs.map((s, i) => {
        const autoplay = i === currentIndex ? true : undefined;
        return (
            <SongComponent
                key={s.id}
                song={s}
                hideActions={true}
                onEnded={handleSongEnded}
                autoplay={autoplay}
            />
        );
    });

    const OPTIONS: EmblaOptionsType = {
        axis: "y",
        watchDrag: true,
        dragFree: false,
    };

    return (
        <ThemeProvider theme={darkTheme}>
            <Box>
                <VerticalCarousel
                    slides={SLIDES}
                    options={OPTIONS}
                    currentSlideIndex={currentIndex}
                    onIndexChange={handleIndexChange}
                />
            </Box>
        </ThemeProvider>
    );
}