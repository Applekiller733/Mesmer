import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../../themes/themes";
import { Box, Button, Paper, TextField, Typography } from "@mui/material";
import SideList from "../../../reusablecomponents/library/sidelist/sidelist";
import { useFormik } from "formik";
import * as Yup from "yup";
import type { CreatePlaylistRequest } from "../../../models/playlist";
import { PlaylistVisibility } from "../../../models/playlist";
import { useAppDispatch } from "../../../hooks/hooks";
import { createPlaylist, fetchPlaylistsSavedByAccountId } from "../../../stores/thunks/playlistthunks";
import { useState } from "react";
import AddSongsGrid from "../../../reusablecomponents/library/create-playlist-datagrids/addsongsdatagrid";
import { useSelector } from "react-redux";
import { selectCurrentUser } from "../../../stores/slices/userdataslice";
import VisibilitySelector from "../../../reusablecomponents/library/visibility/visibilityselector";

export default function CreatePlaylist({handleMainPage} : {handleMainPage:any}) {
    const [status, setStatus] = useState('init');
    const dispatch = useAppDispatch();
    const user = useSelector(selectCurrentUser);


    const validationschema = Yup.object().shape({
        name: Yup.string().required("Playlist Name is required!")
    });

    const CreatePlaylistFormik = useFormik({
        initialValues: {
            name: '',
            songIds: [] as string[],
            // Default to Private so a freshly-created playlist isn't
            // accidentally broadcast on the user's profile. The user
            // explicitly opts in to Unlisted/Public via the selector.
            visibility: PlaylistVisibility.Private as PlaylistVisibility,
        },
        validationSchema: validationschema,
        onSubmit: (values) => {
            handleSubmit({
                name: values.name,
                songIds: values.songIds,
                visibility: values.visibility,
            });
        }
    })

    async function handleSubmit(request:CreatePlaylistRequest){
        setStatus('loading');
        const response = await dispatch(createPlaylist(request));
        if (response.meta.requestStatus === 'fulfilled' && user.id){
            setStatus('successful');
            dispatch(fetchPlaylistsSavedByAccountId(user.id));
            handleMainPage();
        }
        else {
            setStatus('failed');
        }
    }

    return (
        <Box>
            <form onSubmit={CreatePlaylistFormik.handleSubmit}>
                <TextField
                    id="name"
                    name="name"
                    label="Playlist Name"
                    variant="outlined"
                    fullWidth
                    margin="normal"
                    className="field"
                    value={CreatePlaylistFormik.values.name}
                    onChange={CreatePlaylistFormik.handleChange}
                    error={CreatePlaylistFormik.touched.name && Boolean(CreatePlaylistFormik.errors.name)}
                    helperText={CreatePlaylistFormik.touched.name && CreatePlaylistFormik.errors.name}
                >
                </TextField>

                {/*
                  Visibility selector. Set up-front rather than as a
                  post-create PATCH so the user lands in their chosen
                  state in one step. The selector handler integrates
                  with Formik by calling setFieldValue directly — the
                  custom component doesn't fire synthetic events the
                  Formik handleChange could consume.
                */}
                <Box sx={{ mt: 2, mb: 1 }}>
                    <Typography variant="body2" sx={{ mb: 0.5 }}>
                        Visibility
                    </Typography>
                    <VisibilitySelector
                        value={CreatePlaylistFormik.values.visibility}
                        onChange={(next) =>
                            CreatePlaylistFormik.setFieldValue("visibility", next)
                        }
                        disabled={status === "loading"}
                    />
                </Box>

                <Button type="submit" color="success" disabled={status === "loading"}>
                    Save Playlist
                </Button>
            </form>
        </Box>
    );
}