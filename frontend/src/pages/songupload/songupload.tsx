import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../themes/themes";
import { Alert, Backdrop, Box, Button, CircularProgress, Paper, TextField, Typography } from "@mui/material";
import "./songupload.css";
import { useState } from "react";
import { useAppDispatch } from "../../hooks/hooks";
import { useFormik } from "formik";
import * as Yup from "yup";
import type { CreateSongRequest } from "../../models/song";
import { createSong } from "../../stores/thunks/songthunks";

export default function SongUpload() {
    const dispatch = useAppDispatch();
    const [status, setStatus] = useState('init');

    //todo add file integration for the songs
    // const [file, setFile] = useState<File | null>(null);
    // const [fileName, setFileName] = useState('');
    // const [extension, setExtension] = useState('');

    // const saveFile = (e: any) => {
    //     setFile(e.target.files[0]);
    //     setFileName(e.target.files[0].name);
    //     setExtension(e.target.files[0].name.split('.')[1]);
    // };

    const validationschema = Yup.object().shape(
        {
            name: Yup.string().required("Name is required!"),
            artist: Yup.string().required("Artist is required!"),
            imageUrl: Yup.string().url("Image is not a url!").nullable(),
            videoUrl: Yup.string().url("Video is not a url!").nullable(),
            soundUrl: Yup.string().url("Sound is not a url!").nullable(),
        }
    ).test(
        "video-or-sound-required",
        "Either video or sound is required", 
        (values, context) => {
            if (!values) return false;

            const hasVideo = !!values.videoUrl;
            const hasSound = !!values.soundUrl;

            if (!hasVideo && !hasSound) {
                return context.createError({
                    path: "videoUrl",
                    message: "Either video or sound is required",
                });
            }

            return true;
        }
    );

    const UploadSongFormik = useFormik({
        initialValues: {
            name: '',
            artist: '',
            videoUrl: '',
            imageUrl: '',
            soundUrl: '',
        },
        validationSchema: validationschema,
        onSubmit: (values) => {
            handleSubmit({
                name: values.name,
                artist: values.artist,
                imageUrl: values.imageUrl,
                soundUrl: values.soundUrl,
                videoUrl: values.videoUrl,
            })
        }
    }
    );

    async function handleSubmit(request: CreateSongRequest) {
        setStatus('loading');
        const response = await dispatch(createSong(request));
        if (response.payload) {
            setStatus('successful');
        }
        else {
            setStatus('failed');
        }
    }

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="song-upload">
                <Paper className="song-upload-paper">
                    <Typography>Upload Song</Typography>
                    <form onSubmit={UploadSongFormik.handleSubmit}>
                        <TextField
                            id="name"
                            name="name"
                            label="Song Name"
                            variant="outlined"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={UploadSongFormik.values.name}
                            onChange={UploadSongFormik.handleChange}
                            error={UploadSongFormik.touched.name && Boolean(UploadSongFormik.errors.name)}
                            helperText={UploadSongFormik.touched.name && UploadSongFormik.errors.name}
                        ></TextField>
                        <TextField
                            id="artist"
                            name="artist"
                            label="Artist"
                            variant="outlined"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={UploadSongFormik.values.artist}
                            onChange={UploadSongFormik.handleChange}
                            error={UploadSongFormik.touched.artist && Boolean(UploadSongFormik.errors.artist)}
                            helperText={UploadSongFormik.touched.artist && UploadSongFormik.errors.artist}
                        ></TextField>
                        <TextField
                            id="imageUrl"
                            name="imageUrl"
                            label="* Image URL"
                            variant="outlined"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={UploadSongFormik.values.imageUrl}
                            onChange={UploadSongFormik.handleChange}
                            error={UploadSongFormik.touched.imageUrl && Boolean(UploadSongFormik.errors.imageUrl)}
                            helperText={UploadSongFormik.touched.imageUrl && UploadSongFormik.errors.imageUrl}
                        ></TextField>
                        <TextField
                            id="soundUrl"
                            name="soundUrl"
                            label="Sound URL"
                            variant="outlined"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={UploadSongFormik.values.soundUrl}
                            onChange={UploadSongFormik.handleChange}
                            error={UploadSongFormik.touched.soundUrl && Boolean(UploadSongFormik.errors.soundUrl)}
                            helperText={UploadSongFormik.touched.soundUrl && UploadSongFormik.errors.soundUrl}
                        ></TextField>
                        <TextField
                            id="videoUrl"
                            name="videoUrl"
                            label="Video URL"
                            variant="outlined"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={UploadSongFormik.values.videoUrl}
                            onChange={UploadSongFormik.handleChange}
                            error={UploadSongFormik.touched.videoUrl && Boolean(UploadSongFormik.errors.videoUrl)}
                            helperText={UploadSongFormik.touched.videoUrl && UploadSongFormik.errors.videoUrl}
                        ></TextField>

                        <Button type="submit" color="success">
                            Upload Song
                        </Button>
                    </form>
                    {status === 'successful' && <Alert severity="success">Song upload successful</Alert>}
                    {status === 'failed' && <Alert severity="error">Song upload failed</Alert>}
                    <Backdrop
                        sx={(theme) => ({ color: '#fff', zIndex: theme.zIndex.drawer + 1 })}
                        open={status === 'loading'}
                    >
                        <CircularProgress color="inherit" />
                    </Backdrop>
                </Paper>
            </Box>
        </ThemeProvider>
    );
}