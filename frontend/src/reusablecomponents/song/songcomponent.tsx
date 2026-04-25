import type { Song } from "../../models/song";
import AudioWidget from "./audiowidget";
import VideoWidget from "./videowidget";
import { Box, Button, Icon, Paper, Typography } from "@mui/material";
import ThumbUpOffAlt from "@mui/icons-material/ThumbUpOffAlt";
import ThumbUpAlt from "@mui/icons-material/ThumbUpAlt";
import './song.css';
import { useEffect, useState, type ReactEventHandler } from "react";
// import { updateUpvotes } from "../../stores/slices/songdataslice";
import { useAppDispatch } from "../../hooks/hooks";
import AddToPlaylistDialog from "./addtoplaylistdialog";
import { flipLike } from "../../stores/thunks/songthunks";
import React from "react";

interface SongProps {
    song: Song,
    hideActions?: Boolean,
    onEnded?: ReactEventHandler,
    autoplay?: boolean,
}

const SongComponent = (props: SongProps) => {
    const [liked, setLiked] = useState
        (props.song.likedByAccountIds?.find(id => 
            id = props.song.id) !== undefined); // works, but maybe shorten it a bit
    const dispatch = useAppDispatch();
    const [isSaving, setIsSaving] = useState(false);
    const song = props.song;
    const hideActions = props.hideActions;
    const onEnded = props.onEnded;
    const autoplay = props.autoplay;

    function handleLike() {
        dispatch(flipLike({id:song.id}))
        setLiked(!liked);
    }

    function handleSave() {
        setIsSaving(true);
    }

    function handleDialogClose() {
        setIsSaving(false);
    }

    return (
        <Paper className="songcomponent">
            <div className="text">
                <Typography variant="h4">{song.name}</Typography>
                <Typography variant="h5">{song.artist}</Typography>
            </div>
            <div>
                {
                    song.videoUrl !== undefined && song.videoUrl !== '' && song.videoUrl !== null?
                        (<VideoWidget song={song} handleEnded={onEnded} autoplay={autoplay}></VideoWidget>)
                        :
                        (<AudioWidget song={song} handleEnded={onEnded} autoplay={autoplay}></AudioWidget>)
                }
            </div>

            {
                !hideActions &&
                <div className="text">
                    <Button onClick={handleLike}>
                        {
                            liked === false ?
                                <>
                                    <ThumbUpOffAlt></ThumbUpOffAlt>
                                    <Typography className="upvotes-text">{song.upvotes}</Typography>
                                </>
                                :
                                <>
                                    <ThumbUpAlt></ThumbUpAlt>
                                    <Typography className="upvotes-text">{song.upvotes}</Typography>
                                </>
                        }
                    </Button>
                    <Button color="success" onClick={handleSave}>Save</Button>
                    <AddToPlaylistDialog open={isSaving} song={song} handleDialogClose={handleDialogClose}></AddToPlaylistDialog>
                    {/* <Typography>Upvotes: {song.upvotes}</Typography> */}
                </div>
            }

        </Paper>
    );
}

export default SongComponent;