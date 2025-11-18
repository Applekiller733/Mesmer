import { Box, Button, Paper, TextField, ThemeProvider, Typography } from "@mui/material";
import { useFormik } from "formik";
import { Navigate, useParams } from "react-router";
import * as Yup from "yup";
import type { UpdateUserRequest } from "../../models/user";
import { darkTheme } from "../../themes/themes";
import "./profile.css";
import { useAppDispatch } from "../../hooks/hooks";
import { update } from "../../stores/thunks/userthunks";
import { useState } from "react";
import { updateCurrentUserHelper } from "../../utils/helpers/userhelpers";

export default function EditProfile() {
    const params = useParams();
    const paramid = (params.id as unknown) as number;
    const dispatch = useAppDispatch();
    const [status, setStatus] = useState('init');

    //maybe just use a single usestate with a FileModel type
    const [file, setFile] = useState<File | null>(null);
    const [fileName, setFileName] = useState('');
    const [extension, setExtension] = useState('');

    const saveFile = (e: any) => {
        setFile(e.target.files[0]);
        setFileName(e.target.files[0].name);
        setExtension(e.target.files[0].name.split('.')[1]);
    };

    async function handleSubmit(request: UpdateUserRequest) {
        console.log(request.profilepicture);
        let response = await dispatch(update(request));

        setStatus('loading');
        if (response.meta.requestStatus === 'fulfilled') {
            setStatus('successful');
            // const user: User = {
            //     id: response.payload.id,
            //     username: response.payload.userName,
            //     email: response.payload.email,
            //     role: response.payload.role,
            //     token: response.payload.jwtToken,
            // }
            // updateCurrentUserHook(user);
            updateCurrentUserHelper(response.payload);
        }
        else {
            setStatus('failed');
        }
    }

    const validationschema = Yup.object().shape(
        {
            username: Yup.string()
                .required('Username is Required'),
        }
    )

    const EditProfileFormik = useFormik({
        initialValues: {
            username: '',
        },
        validationSchema: validationschema,
        onSubmit: (values) => {
            handleSubmit({
                id: paramid,
                username: values.username,
                email: null,
                password: null,
                confirmpassword: null,
                role: null,
                profilepicture: {
                    file: file,
                    filename: fileName,
                    extension: extension,
                },
            })
        },
    });

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="profile-background">
                <Paper className="profile-paper">
                    <Typography variant="h4" align="center" gutterBottom>
                        Edit Profile
                    </Typography>

                    <form onSubmit={EditProfileFormik.handleSubmit}>
                        <TextField
                            id="username"
                            name="username"
                            label="Username"
                            variant="outlined"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={EditProfileFormik.values.username}
                            onChange={EditProfileFormik.handleChange}
                            error={EditProfileFormik.touched.username && Boolean(EditProfileFormik.errors.username)}
                            helperText={EditProfileFormik.touched.username && EditProfileFormik.errors.username}
                        ></TextField>

                        <input type="file" onChange={saveFile} />

                        <Button
                            type="submit"
                            variant="contained"
                            color="primary"
                            fullWidth
                            className="signup-button"
                        >
                            Save Changes
                        </Button>
                    </form>
                    <Button color='error' href={`/profile/${paramid}`}>Cancel</Button>
                    {status === 'successful' && <Navigate to={`../profile/${paramid}`}></Navigate>}
                </Paper>
            </Box>
        </ThemeProvider>
    )
}