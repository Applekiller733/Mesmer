import { ThemeProvider } from "@emotion/react";
import "../signup/signup.css";
import { darkTheme } from "../../../themes/themes";
import { Box, Paper, TextField, Button, Backdrop, CircularProgress, Typography } from "@mui/material";
import { Link, useParams } from "react-router";
import { useAppDispatch } from "../../../hooks/hooks";
import type { ResetPasswordRequest } from "../../../models/user";
import { useFormik } from "formik";
import { useState } from "react";
import * as Yup from 'yup';
import { resetpassword } from "../../../stores/thunks/userthunks";

export function ResetPassword() {
    const { token } = useParams();
    const dispatch = useAppDispatch();
    const [status, setStatus] = useState('init');

    async function handleSubmit(request: ResetPasswordRequest) {
        setStatus('loading')
        const response = await dispatch(resetpassword(request));
        if (response.meta.requestStatus === "fulfilled") {
            setStatus('successful');
        }
        else {
            setStatus('failed');
        }
    }

    const validationschema = Yup.object().shape(
        {
            password: Yup.string()
                .required('Password is Required')
                .min(6, 'Password must be at least 6 characters long'),
            confirmpassword: Yup.string()
                .required('Confirm Password is Required')
                .oneOf([Yup.ref('password')], 'Your passwords do not match.')
        }
    )

    const ResetPasswordFormik = useFormik({
        initialValues: {
            password: '',
            confirmpassword: '',
        },
        validationSchema: validationschema,
        onSubmit: (values) => {
            if (token) {
                handleSubmit({
                    token: token,
                    password: values.password,
                    confirmpassword: values.confirmpassword,
                })
            }
        }
    });

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="signup-page">
                <Paper className="signup-card">
                    {status !== 'successful' ?
                        <form onSubmit={ResetPasswordFormik.handleSubmit}>
                            <TextField
                                id="password"
                                name="password"
                                label="Password"
                                variant="outlined"
                                fullWidth
                                margin="normal"
                                className="field"
                                value={ResetPasswordFormik.values.password}
                                onChange={ResetPasswordFormik.handleChange}
                                error={ResetPasswordFormik.touched.password && Boolean(ResetPasswordFormik.errors.password)}
                                helperText={ResetPasswordFormik.touched.password && ResetPasswordFormik.errors.password}
                            ></TextField>
                            <TextField
                                id="confirmpassword"
                                name="confirmpassword"
                                label="Confirm Password"
                                variant="outlined"
                                fullWidth
                                margin="normal"
                                className="field"
                                value={ResetPasswordFormik.values.confirmpassword}
                                onChange={ResetPasswordFormik.handleChange}
                                error={ResetPasswordFormik.touched.confirmpassword && Boolean(ResetPasswordFormik.errors.confirmpassword)}
                                helperText={ResetPasswordFormik.touched.confirmpassword && ResetPasswordFormik.errors.confirmpassword}
                            ></TextField>
                            <Button
                                type="submit"
                                variant="contained"
                                color="primary"
                                fullWidth
                                className="signup-button"
                            >
                                Submit
                            </Button>
                        </form>
                        :
                        <Typography>You have successfully reset your password. Go back to <Link to="../">Home</Link></Typography>
                    }
                    <Backdrop
                        sx={(theme) => ({ color: '#fff', zIndex: theme.zIndex.drawer + 1 })}
                        open={status === 'loading'}
                    >
                        <CircularProgress color="inherit" />
                    </Backdrop>
                </Paper>
            </Box>
        </ThemeProvider>
    )
}