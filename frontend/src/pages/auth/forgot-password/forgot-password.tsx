import { ThemeProvider } from "@emotion/react";
import "../signup/signup.css";
import { darkTheme } from "../../../themes/themes";
import { Box, Button, Paper, TextField, Typography, Backdrop, CircularProgress } from "@mui/material";
import { Form, useFormik } from "formik";
import { useAppDispatch } from "../../../hooks/hooks";
import type { ForgotPasswordRequest } from "../../../models/user";
import { useState } from "react";
import * as Yup from 'yup';
import { forgotpassword } from "../../../stores/thunks/userthunks";

export function ForgotPassword() {
    const [status, setStatus] = useState('init');
    const dispatch = useAppDispatch();

    async function handleSubmit(request: ForgotPasswordRequest) {
        setStatus('loading');
        let response = await dispatch(forgotpassword(request));
        if (response.meta.requestStatus == "fulfilled") { // REQUEST WILL ALWAYS BE FULFILLED
            setStatus('successful');
        }
    }

    const validationschema = Yup.object().shape(
        {
            email: Yup.string()
                .email('Must be a valid email address')
                .required('Email is Required'),
        }
    )

    const ForgotPasswordFormik = useFormik({
        initialValues: {
            email: ''
        },
        validationSchema: validationschema,
        onSubmit: (values) => {
            handleSubmit({
                email: values.email
            })
        }
    });

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="signup-page">
                <Paper className="signup-card">
                    <Typography variant="h4" align="center" gutterBottom>
                        Forgot Password
                    </Typography>
                    {status != 'successful' ?
                        <form onSubmit={ForgotPasswordFormik.handleSubmit}>
                            <TextField
                                id="email"
                                name="email"
                                label="Email"
                                variant="outlined"
                                fullWidth
                                margin="normal"
                                className="field"
                                value={ForgotPasswordFormik.values.email}
                                onChange={ForgotPasswordFormik.handleChange}
                                error={ForgotPasswordFormik.touched.email && Boolean(ForgotPasswordFormik.errors.email)}
                                helperText={ForgotPasswordFormik.touched.email && ForgotPasswordFormik.errors.email}
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
                        <Typography>Your forgotten password request has been successful.
                            Check your email to reset your password.</Typography>
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