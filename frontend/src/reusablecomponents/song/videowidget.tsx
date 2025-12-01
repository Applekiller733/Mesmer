import type { Song } from "../../models/song";
import ReactPlayer from 'react-player';

import "./song.css";
import { useEffect, useState, type ReactEventHandler } from "react";
import React from "react";

interface VideoWidgetProps {
    song: Song,
    handleEnded?: ReactEventHandler,
    autoplay?: boolean,
}

const VideoWidget = (props: VideoWidgetProps) => {
    const song = props.song;
    const handleEnded = props.handleEnded;
    // ENSURE VIDEO ONLY PLAYS DEPENDING ON A STATE / refer to audio widget
    const autoplay = props.autoplay;

    return (
        <div className="videowidget">
            {
                handleEnded !== undefined ?
                    <ReactPlayer src={song.videoUrl} controls={true} onEnded={handleEnded} autoPlay={autoplay}></ReactPlayer>
                    :
                    <ReactPlayer src={song.videoUrl} controls={true}></ReactPlayer>
            }
        </div>
    );
}

export default VideoWidget;