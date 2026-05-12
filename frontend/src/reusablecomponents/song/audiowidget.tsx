import { IconButton, Slider, Typography, Box } from "@mui/material";
import type { Song } from "../../models/song";
import { useEffect, useRef, useState, type ReactEventHandler } from "react";
import ReactPlayer from "react-player";
import { useSelector } from "react-redux";

import "./song.css";
import PlayCircleFilled from "@mui/icons-material/PlayCircleFilled";
import PauseCircleFilled from "@mui/icons-material/PauseCircleFilled";

import VolumeControl from "./volumecontrol";
import { selectEffectiveVolume } from "../../stores/slices/playbackslice";

interface AudioWidgetProps {
    song: Song;
    handleEnded?: ReactEventHandler;
    autoplay?: boolean;
}

function formatTime(seconds: number | null): string {
    if (seconds === null || !Number.isFinite(seconds) || seconds < 0) {
        return "--:--";
    }
    const total = Math.floor(seconds);
    const hours = Math.floor(total / 3600);
    const mins = Math.floor((total % 3600) / 60);
    const secs = total % 60;
    const padSecs = secs.toString().padStart(2, "0");
    if (hours > 0) {
        const padMins = mins.toString().padStart(2, "0");
        return `${hours}:${padMins}:${padSecs}`;
    }
    return `${mins}:${padSecs}`;
}

function resolveSoundUrl(raw: string | undefined | null): string | undefined {
    if (!raw) return undefined;
    if (/^https?:\/\//i.test(raw)) return raw;
    const base = import.meta.env.VITE_API_URL ?? "";
    return `${base.replace(/\/$/, "")}${raw.startsWith("/") ? raw : `/${raw}`}`;
}

const PLAY_TIMEOUT_MS = 500;

const AudioWidget = (props: AudioWidgetProps) => {
    const { song, handleEnded, autoplay } = props;
    const [isPlaying, setIsPlaying] = useState<boolean>(autoplay === true);

    const player = useRef<HTMLVideoElement | null>(null);
    const playTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    const [currentTime, setCurrentTime] = useState<number>(0);
    const [duration, setDuration] = useState<number | null>(null);
    const [pendingSeek, setPendingSeek] = useState<number | null>(null);

    const effectiveVolume = useSelector(selectEffectiveVolume);

    const resolvedSrc = resolveSoundUrl(song.soundUrl);
    const hasAudio = !!resolvedSrc;

    useEffect(() => {
        setIsPlaying(autoplay === true);
    }, [autoplay]);

    useEffect(() => {
        if (playTimeoutRef.current !== null) {
            clearTimeout(playTimeoutRef.current);
            playTimeoutRef.current = null;
        }

        if (isPlaying && hasAudio) {
            playTimeoutRef.current = setTimeout(() => {
                playTimeoutRef.current = null;
                const p = player.current;
                if (p && p.paused) {
                    setIsPlaying(false);
                }
            }, PLAY_TIMEOUT_MS);
        }

        return () => {
            if (playTimeoutRef.current !== null) {
                clearTimeout(playTimeoutRef.current);
                playTimeoutRef.current = null;
            }
        };
    }, [isPlaying, hasAudio]);

    function handlePlay() {
        if (!hasAudio) return;
        setIsPlaying((prev) => !prev);
    }

    function handlePlaying() {
        if (playTimeoutRef.current !== null) {
            clearTimeout(playTimeoutRef.current);
            playTimeoutRef.current = null;
        }
    }

    function handleEndedPlaying() {
        setIsPlaying(false);
        setCurrentTime(0);
    }

    function handleTimeUpdate() {
        if (pendingSeek !== null) return;
        const p = player.current;
        if (p && Number.isFinite(p.currentTime)) {
            setCurrentTime(p.currentTime);
        }
    }

    function handleDurationChange() {
        const p = player.current;
        if (p && Number.isFinite(p.duration)) {
            setDuration(p.duration);
        }
    }

    function handleSeekDrag(_: Event, value: number | number[]) {
        const v = Array.isArray(value) ? value[0] : value;
        setPendingSeek(v);
    }

    function handleSeekCommit(_: Event | React.SyntheticEvent, value: number | number[]) {
        const v = Array.isArray(value) ? value[0] : value;
        const p = player.current;
        if (p) {
            p.currentTime = v;
            setCurrentTime(v);
        }
        setPendingSeek(null);
    }

    const sliderValue = pendingSeek !== null ? pendingSeek : currentTime;
    const sliderMax = duration ?? 1;
    const seekDisabled = !hasAudio || duration === null;

    return (
        <div className="audiowidget">
            <div className="audiowidget-overlapping-items">
                {song.imageUrl ? (
                    <img src={song.imageUrl} className="songimage" alt={song.name} />
                ) : (
                    <img src="/songdefaulticon.jpg" className="songimage" alt="default" />
                )}

                <div className="audiowidget-controls">
                    <Box className="audiowidget-progress-row">
                        <Typography className="audiowidget-time">
                            {formatTime(sliderValue)}
                        </Typography>
                        <Slider
                            className="audiowidget-progress-slider"
                            min={0}
                            max={sliderMax}
                            step={0.1}
                            value={sliderValue}
                            disabled={seekDisabled}
                            onChange={handleSeekDrag}
                            onChangeCommitted={handleSeekCommit}
                            aria-label="Seek"
                            size="small"
                        />
                        <Typography className="audiowidget-time">
                            {formatTime(duration)}
                        </Typography>
                    </Box>

                    <div className="audiowidget-buttons-row">
                        <div className="audiowidget-controls-left">
                            <IconButton
                                className="audiowidget-control-button audiowidget-control-button-circle"
                                color="primary"
                                size="medium"
                                onClick={handlePlay}
                                disabled={!hasAudio}
                                title={hasAudio ? (isPlaying ? "Pause" : "Play") : "No audio available"}
                            >
                                {isPlaying ? (
                                    <PauseCircleFilled style={{ fontSize: "50px" }} />
                                ) : (
                                    <PlayCircleFilled style={{ fontSize: "50px" }} />
                                )}
                            </IconButton>
                        </div>

                        <div className="audiowidget-controls-right">
                            <VolumeControl />
                        </div>
                    </div>
                </div>
            </div>

            {hasAudio && (
                <ReactPlayer
                    ref={player as any}
                    src={resolvedSrc}
                    playing={isPlaying}
                    volume={effectiveVolume}
                    controls={false}
                    style={{
                        position: "absolute",
                        left: "-9999px",
                        top: "-9999px",
                        width: "1px",
                        height: "1px",
                        opacity: 0,
                        pointerEvents: "none",
                    }}
                    onEnded={handleEnded ?? handleEndedPlaying}
                    onTimeUpdate={handleTimeUpdate}
                    onDurationChange={handleDurationChange}
                    onPlaying={handlePlaying}
                />
            )}
        </div>
    );
};

export default AudioWidget;