import { IconButton, LinearProgress } from "@mui/material";
import type { Song } from "../../models/song";
import { useEffect, useRef, useState, type ReactEventHandler } from "react";
import ReactPlayer from 'react-player'; ``
import "./song.css";
// import "./audiowidget.css";
import PlayCircleFilled from "@mui/icons-material/PlayCircleFilled";
import PauseCircleFilled from "@mui/icons-material/PauseCircleFilled";
import KeyboardDoubleArrowLeftIcon from '@mui/icons-material/KeyboardDoubleArrowLeft';

interface AudioWidgetProps {
    song: Song,
    handleEnded?: ReactEventHandler,
    autoplay?: boolean,
}

const AudioWidget = (props: AudioWidgetProps) => {
    const song = props.song;
    const handleEnded = props.handleEnded;
    const autoplay = props.autoplay;

    // const [played, setPlayed] = useState(0);
    // const [duration, setDuration] = useState(0);
    // const progress = played / duration * 100;
    // console.log(progress);


    // MOVE THIS STATE UP TO A CONTAINER / THE PAGE ITSELF
    // or find some way to not use a state at all :D
    const [isPlaying, setIsPlaying] = useState(false);


    // useEffect(() => {
    //     if (props.isPlaying !== undefined && props.isPlaying) {
    //         setIsPlaying(true);
    //     }
    // }, [])

    const player = useRef(null);

    function handlePlay() {
        setIsPlaying(!isPlaying);
    }

    function handleEndedPlaying() {
        setIsPlaying(false);
    }

    function handleRevert() {
        //todo fix, currently doesnt seem to properly restart the song
        if (player.current !== null) {
            //@ts-ignore
            player.current.seekTo(0, 'seconds');
            setIsPlaying(false);
        }
    }




    return (
        <div className="audiowidget">
            <div className="audiowidget-overlapping-items">
                {song.imageUrl !== undefined && song.imageUrl !== '' ?
                    <img src={song.imageUrl} className="songimage"></img>
                    :
                    <img src='/songdefaulticon.jpg' className="songimage"></img>
                }

                <IconButton className="audiowidget-overlapping-button
                 audiowidget-overlapping-button-play" color="primary" size="medium" onClick={handlePlay}>
                    {
                        isPlaying || autoplay ?
                            <PauseCircleFilled style={{ fontSize: "50px" }}></PauseCircleFilled>
                            :
                            <PlayCircleFilled style={{ fontSize: "50px" }}></PlayCircleFilled>
                    }
                </IconButton>

                <IconButton className="audiowidget-overlapping-button
                 audiowidget-overlapping-button-revert" color="primary" size="medium" onClick={handleRevert}>
                    {
                        <KeyboardDoubleArrowLeftIcon style={{ fontSize: "50px" }}></KeyboardDoubleArrowLeftIcon>
                    }
                </IconButton>

                {/* <LinearProgress variant="determinate" value={progress}></LinearProgress> */}
            </div>

            {/*ref={player}*/}
            {handleEnded !== undefined ?
                <ReactPlayer ref={player} 
                src={song.soundUrl} autoPlay={autoplay} controls={false} height={0} width={0}
                    style={{ display: "none", margin: 0, padding: 0 }} onEnded={handleEnded}></ReactPlayer>
                :
                <ReactPlayer ref={player} 
                 src={song.soundUrl} playing={isPlaying} controls={false} height={0} width={0}
                    style={{ display: "none", margin: 0, padding: 0 }} onEnded={handleEndedPlaying}></ReactPlayer>
            }
        </div>
    );
}

export default AudioWidget;