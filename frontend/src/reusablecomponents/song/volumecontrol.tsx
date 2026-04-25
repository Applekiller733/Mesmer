// frontend/src/reusablecomponents/song/volumecontrol.tsx
//
// VolumeControl no longer positions itself — it's a regular inline-flex
// component, dropped into a flex row by its parent. The parent is now
// `.audiowidget-controls-right` in the controls bar.
//
// Behavior unchanged from before: speaker icon click opens the popout
// slider; click outside closes; click icon while open mutes.

import { useRef, useState } from "react";
import { useSelector } from "react-redux";
import {
    IconButton,
    Popper,
    Paper,
    Slider,
    ClickAwayListener,
} from "@mui/material";
import VolumeUp from "@mui/icons-material/VolumeUp";
import VolumeDown from "@mui/icons-material/VolumeDown";
import VolumeOff from "@mui/icons-material/VolumeOff";
import VolumeMute from "@mui/icons-material/VolumeMute";

import { useAppDispatch } from "../../hooks/hooks";
import {
    selectVolume,
    selectMuted,
    setVolume,
    toggleMuted,
} from "../../stores/slices/playbackslice";
import "./volumecontrol.css";

interface VolumeControlProps {
    /** Optional extra class for callers that want to tweak per-instance styling. */
    className?: string;
    /** Icon font-size in px; defaults to 50 to match play/revert icons. */
    iconSize?: number;
}

const VolumeControl = ({ className, iconSize = 50 }: VolumeControlProps) => {
    const dispatch = useAppDispatch();
    const volume = useSelector(selectVolume);
    const muted = useSelector(selectMuted);

    const anchorRef = useRef<HTMLButtonElement | null>(null);
    const [open, setOpen] = useState(false);

    function handleIconClick() {
        if (!open) {
            setOpen(true);
        } else {
            dispatch(toggleMuted());
        }
    }

    function handleClickAway() {
        if (open) setOpen(false);
    }

    function handleSliderChange(_: Event, value: number | number[]) {
        const next = Array.isArray(value) ? value[0] : value;
        dispatch(setVolume(next / 100));
    }

    function renderIcon() {
        const style = { fontSize: `${iconSize}px` };
        if (muted || volume === 0) return <VolumeOff style={style} />;
        if (volume < 0.33) return <VolumeMute style={style} />;
        if (volume < 0.66) return <VolumeDown style={style} />;
        return <VolumeUp style={style} />;
    }

    const sliderValue = muted ? 0 : Math.round(volume * 100);

    return (
        <ClickAwayListener onClickAway={handleClickAway}>
            <div className={`volumecontrol ${className ?? ""}`}>
                <IconButton
                    ref={anchorRef}
                    onClick={handleIconClick}
                    color="primary"
                    size="medium"
                    title={muted ? "Unmute" : "Volume"}
                    className="volumecontrol-iconbutton"
                >
                    {renderIcon()}
                </IconButton>

                <Popper
                    open={open}
                    anchorEl={anchorRef.current}
                    placement="top"
                    modifiers={[{ name: "offset", options: { offset: [0, 8] } }]}
                    className="volumecontrol-popper"
                >
                    <Paper elevation={6} className="volumecontrol-popper-paper">
                        <Slider
                            orientation="vertical"
                            min={0}
                            max={100}
                            step={1}
                            value={sliderValue}
                            onChange={handleSliderChange}
                            aria-label="Volume"
                            className="volumecontrol-slider"
                        />
                    </Paper>
                </Popper>
            </div>
        </ClickAwayListener>
    );
};

export default VolumeControl;