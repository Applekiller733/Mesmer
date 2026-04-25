// frontend/src/reusablecomponents/song/audiowidget.tsx
//
// Buttons now live in an `.audiowidget-controls` flex row that overlays
// the bottom of the cover. Layout uses flexbox, not per-button absolute
// positioning, so adding/removing/reordering controls is just JSX edits.
//
// Layout: play (far left) | volume + revert (far right)

import { IconButton } from "@mui/material";
import type { Song } from "../../models/song";
import { useRef, useState, type ReactEventHandler } from "react";
import ReactPlayer from "react-player";
import { useSelector } from "react-redux";

import "./song.css";
import PlayCircleFilled from "@mui/icons-material/PlayCircleFilled";
import PauseCircleFilled from "@mui/icons-material/PauseCircleFilled";
import KeyboardDoubleArrowLeftIcon from "@mui/icons-material/KeyboardDoubleArrowLeft";

import VolumeControl from "./volumecontrol";
import { selectEffectiveVolume } from "../../stores/slices/playbackslice";

interface AudioWidgetProps {
    song: Song;
    handleEnded?: ReactEventHandler;
    autoplay?: boolean;
}

const AudioWidget = (props: AudioWidgetProps) => {
    const { song, handleEnded, autoplay } = props;
    const [isPlaying, setIsPlaying] = useState(false);
    const player = useRef(null);

    const effectiveVolume = useSelector(selectEffectiveVolume);
    const hasAudio = !!song.soundUrl && song.soundUrl.length > 0;

    function handlePlay() {
        if (!hasAudio) return;
        setIsPlaying((prev) => !prev);
    }

    function handleEndedPlaying() {
        setIsPlaying(false);
    }

    function handleRevert() {
        if (player.current !== null) {
            // @ts-ignore — ReactPlayer ref doesn't expose seekTo cleanly
            player.current.seekTo(0, "seconds");
            setIsPlaying(false);
        }
    }

    const shouldPlay = isPlaying || autoplay === true;

    return (
        <div className="audiowidget">
            <div className="audiowidget-overlapping-items">
                {song.imageUrl ? (
                    <img src={song.imageUrl} className="songimage" alt={song.name} />
                ) : (
                    <img src="/songdefaulticon.jpg" className="songimage" alt="default" />
                )}

                {/* Controls bar — single absolute element, flex row inside.
                    Two groups split by `justify-content: space-between`:
                      .audiowidget-controls-left   → play
                      .audiowidget-controls-right  → volume + revert
                */}
                <div className="audiowidget-controls">
                    <div className="audiowidget-controls-left">
                        <IconButton
                            className="audiowidget-control-button audiowidget-control-button-circle"
                            color="primary"
                            size="medium"
                            onClick={handlePlay}
                            disabled={!hasAudio}
                            title={hasAudio ? (shouldPlay ? "Pause" : "Play") : "No audio available"}
                        >
                            {shouldPlay ? (
                                <PauseCircleFilled style={{ fontSize: "50px" }} />
                            ) : (
                                <PlayCircleFilled style={{ fontSize: "50px" }} />
                            )}
                        </IconButton>
                    </div>

                    <div className="audiowidget-controls-right">
                        <VolumeControl />

                        <IconButton
                            className="audiowidget-control-button audiowidget-control-button-circle"
                            color="primary"
                            size="medium"
                            onClick={handleRevert}
                            disabled={!hasAudio}
                            title="Restart"
                        >
                            <KeyboardDoubleArrowLeftIcon style={{ fontSize: "50px" }} />
                        </IconButton>
                    </div>
                </div>
            </div>

            {hasAudio && (
                <ReactPlayer
                    ref={player}
                    src={song.soundUrl}
                    playing={shouldPlay}
                    volume={effectiveVolume}
                    controls={false}
                    height={0}
                    width={0}
                    style={{ display: "none", margin: 0, padding: 0 }}
                    onEnded={handleEnded ?? handleEndedPlaying}
                />
            )}
        </div>
    );
};

export default AudioWidget;