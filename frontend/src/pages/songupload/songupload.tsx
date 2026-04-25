import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../themes/themes";
import {
    Alert, Backdrop, Box, Button, CircularProgress, Paper,
    TextField, Typography
} from "@mui/material";
import "./songupload.css";
import { useState } from "react";
import { useAppDispatch } from "../../hooks/hooks";
import { useFormik } from "formik";
import * as Yup from "yup";
import type { CreateSongRequest } from "../../models/song";
import { createSong } from "../../stores/thunks/songthunks";

export default function SongUpload() {
    const dispatch = useAppDispatch();
    const [status, setStatus] = useState("init");
    const [audioFile, setAudioFile] = useState<File | null>(null);
    const [audioFileName, setAudioFileName] = useState<string>("");

    const handleAudioChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const f = e.target.files?.[0] ?? null;
        setAudioFile(f);
        setAudioFileName(f?.name ?? "");
    };

    // Yup can't validate the file (it's in component state, not Formik state).
    // We validate it manually in onSubmit alongside Formik's URL validation.
    const validationschema = Yup.object().shape({
        name: Yup.string().required("Name is required!"),
        artist: Yup.string().required("Artist is required!"),
        imageUrl: Yup.string().url("Image is not a url!").nullable(),
        videoUrl: Yup.string().url("Video is not a url!").nullable(),
        soundUrl: Yup.string().url("Sound is not a url!").nullable(),
    });

    const UploadSongFormik = useFormik({
        initialValues: {
            name: "",
            artist: "",
            videoUrl: "",
            imageUrl: "",
            soundUrl: "",
        },
        validationSchema: validationschema,
        onSubmit: (values) => {
            // Cross-field rule: must have *some* audio source.
            const hasSoundUrl = !!values.soundUrl;
            const hasVideo = !!values.videoUrl;
            const hasFile = !!audioFile;

            if (!hasSoundUrl && !hasVideo && !hasFile) {
                setStatus("failed");
                return;
            }

            handleSubmit({
                name: values.name,
                artist: values.artist,
                imageUrl: values.imageUrl || undefined,
                videoUrl: values.videoUrl || undefined,
                // If a file is attached, ignore any soundUrl the user typed —
                // the server enforces this too, but we keep the request clean.
                soundUrl: hasFile ? undefined : values.soundUrl || undefined,
                soundFile: audioFile,
            });
        },
    });

    async function handleSubmit(request: CreateSongRequest) {
        setStatus("loading");
        const response = await dispatch(createSong(request));
        if (response.payload) {
            setStatus("successful");
        } else {
            setStatus("failed");
        }
    }

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="song-upload">
                <Paper className="song-upload-paper">
                    <Typography>Upload Song</Typography>
                    <form onSubmit={UploadSongFormik.handleSubmit}>
                        <TextField
                            id="name" name="name" label="Song Name"
                            variant="outlined" fullWidth margin="normal" className="field"
                            value={UploadSongFormik.values.name}
                            onChange={UploadSongFormik.handleChange}
                            error={UploadSongFormik.touched.name && Boolean(UploadSongFormik.errors.name)}
                            helperText={UploadSongFormik.touched.name && UploadSongFormik.errors.name}
                        />
                        <TextField
                            id="artist" name="artist" label="Artist"
                            variant="outlined" fullWidth margin="normal" className="field"
                            value={UploadSongFormik.values.artist}
                            onChange={UploadSongFormik.handleChange}
                            error={UploadSongFormik.touched.artist && Boolean(UploadSongFormik.errors.artist)}
                            helperText={UploadSongFormik.touched.artist && UploadSongFormik.errors.artist}
                        />
                        <TextField
                            id="imageUrl" name="imageUrl" label="Image URL"
                            variant="outlined" fullWidth margin="normal" className="field"
                            value={UploadSongFormik.values.imageUrl}
                            onChange={UploadSongFormik.handleChange}
                            error={UploadSongFormik.touched.imageUrl && Boolean(UploadSongFormik.errors.imageUrl)}
                            helperText={UploadSongFormik.touched.imageUrl && UploadSongFormik.errors.imageUrl}
                        />

                        {/* Audio file upload — preferred over the URL */}
                        <Box sx={{ my: 2 }}>
                            <Button variant="outlined" component="label">
                                {audioFile ? "Replace Audio File" : "Upload Audio File"}
                                <input
                                    type="file"
                                    hidden
                                    accept="audio/*"
                                    onChange={handleAudioChange}
                                />
                            </Button>
                            {audioFileName && (
                                <Typography variant="body2" sx={{ mt: 1 }}>
                                    Selected: {audioFileName}
                                </Typography>
                            )}
                        </Box>

                        <TextField
                            id="soundUrl" name="soundUrl"
                            label="Sound URL (used only if no audio file uploaded)"
                            variant="outlined" fullWidth margin="normal" className="field"
                            disabled={!!audioFile}
                            value={UploadSongFormik.values.soundUrl}
                            onChange={UploadSongFormik.handleChange}
                            error={UploadSongFormik.touched.soundUrl && Boolean(UploadSongFormik.errors.soundUrl)}
                            helperText={UploadSongFormik.touched.soundUrl && UploadSongFormik.errors.soundUrl}
                        />
                        <TextField
                            id="videoUrl" name="videoUrl" label="Video URL"
                            variant="outlined" fullWidth margin="normal" className="field"
                            value={UploadSongFormik.values.videoUrl}
                            onChange={UploadSongFormik.handleChange}
                            error={UploadSongFormik.touched.videoUrl && Boolean(UploadSongFormik.errors.videoUrl)}
                            helperText={UploadSongFormik.touched.videoUrl && UploadSongFormik.errors.videoUrl}
                        />

                        <Button type="submit" color="success">Upload Song</Button>
                    </form>

                    {status === "successful" && <Alert severity="success">Song upload successful</Alert>}
                    {status === "failed" && (
                        <Alert severity="error">
                            Song upload failed — make sure you provided either an audio file, sound URL, or video URL.
                        </Alert>
                    )}
                    <Backdrop
                        sx={(theme) => ({ color: "#fff", zIndex: theme.zIndex.drawer + 1 })}
                        open={status === "loading"}
                    >
                        <CircularProgress color="inherit" />
                    </Backdrop>
                </Paper>
            </Box>
        </ThemeProvider>
    );
}