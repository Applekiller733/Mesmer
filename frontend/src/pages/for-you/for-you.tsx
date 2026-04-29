import { ThemeProvider } from "@emotion/react";
import "./for-you.css";
import { darkTheme } from "../../themes/themes";
import { Box } from "@mui/material";
import Navbar from "../../reusablecomponents/navbar";
import VerticalCarousel from "../../reusablecomponents/carousel/verticalcarousel";
import { useSelector } from "react-redux";
import {
    loadSong,
    selectAllSongIds,
    selectCurrentSongIndex,
    selectLoadedSongs,
    setIndex,
    unloadSong,
} from "../../stores/slices/songdataslice";
import { useAppDispatch } from "../../hooks/hooks";
import { useEffect, useMemo } from "react";
import SongComponent from "../../reusablecomponents/song/songcomponent";
import type { EmblaOptionsType } from "embla-carousel";
import { fetchSongById, fetchSongIds } from "../../stores/thunks/songthunks";
import RecommendationModeSelector from "./recommendationmodeselector";
import {
    selectRecommendationMode,
    selectRecommendedIds,
} from "../../stores/slices/recommendationmodeslice";

export default function ForYou() {
    const dispatch = useAppDispatch();
    const loadedSongs = useSelector(selectLoadedSongs);
    const allSongIds = useSelector(selectAllSongIds);
    const currentIndex = useSelector(selectCurrentSongIndex);
    const mode = useSelector(selectRecommendationMode);
    const recommendedIds = useSelector(selectRecommendedIds);

    useEffect(() => {
        dispatch(fetchSongIds());
    }, [dispatch]);

    const songIds = useMemo(() => {
        if (mode.kind === "playlist") return recommendedIds;
        return allSongIds;
    }, [mode, recommendedIds, allSongIds]);

    useEffect(() => {
        dispatch(setIndex(0));
    }, [mode, dispatch]);

    useEffect(() => {
        (async () => {
            const idsToKeep = [
                songIds[currentIndex - 1],
                songIds[currentIndex],
                songIds[currentIndex + 1],
            ].filter(Boolean);

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
        })();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [songIds, currentIndex]);

    function handleIndexChange(newIndex: number) {
        dispatch(setIndex(newIndex));
    }

    const SLIDES = songIds.map((id, i) => {
        const inWindow =
            i === currentIndex - 1 || i === currentIndex || i === currentIndex + 1;
        const song = loadedSongs[id];

        if (inWindow && song) {
            const autoplay = i === currentIndex ? true : undefined;
            return (
                <SongComponent
                    key={id}
                    song={song}
                    autoplay={autoplay}
                    onEnded={() => handleIndexChange(currentIndex + 1)}
                />
            );
        }

        return <div key={id ?? `placeholder-${i}`} className="carousel-placeholder" />;
    });

    const OPTIONS: EmblaOptionsType = {
        axis: "y",
        watchDrag: true,
        dragFree: false,
    };

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="for-you">
                <Navbar />
                <Box sx={{ p: 2 }}>
                    <RecommendationModeSelector />
                </Box>
                <div className="song-list">
                    <VerticalCarousel
                        slides={SLIDES}
                        options={OPTIONS}
                        currentSlideIndex={currentIndex}
                        onIndexChange={handleIndexChange}
                    />
                </div>
            </Box>
        </ThemeProvider>
    );
}