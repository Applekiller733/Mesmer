import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../../themes/themes";
import { Box, Button, Paper, TextField } from "@mui/material";
import SideList from "../../../reusablecomponents/library/sidelist/sidelist";
import { useFormik } from "formik";
import * as Yup from "yup";
import type { CreatePlaylistRequest } from "../../../models/playlist";
import { useAppDispatch } from "../../../hooks/hooks";
import { createPlaylist, fetchPlaylistsSavedByAccountId } from "../../../stores/thunks/playlistthunks";
import { useState } from "react";
import AddSongsGrid from "../../../reusablecomponents/library/create-playlist-datagrids/addsongsdatagrid";
import { useSelector } from "react-redux";
import { selectCurrentUser } from "../../../stores/slices/userdataslice";

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
            songIds: [],
        },
        validationSchema: validationschema,
        onSubmit: (values) => {
            handleSubmit({
                name: values.name,
                songIds: values.songIds,
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

                <Button type="submit" color="success">
                    Save Playlist
                </Button>   
            </form>
        </Box>
    );
}
