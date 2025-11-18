import { darkTheme } from "../../../themes/themes";
import { ThemeProvider } from "@emotion/react";
import { Box, Paper, Typography, TextField, Button, Alert, Backdrop, CircularProgress } from "@mui/material";
import { Link, Navigate } from "react-router";
import { useFormik } from "formik";
import './signup.css';
import type { RegisterRequest } from "../../../models/user";
import { useAppDispatch } from "../../../hooks/hooks";
import { useState } from "react";
import * as Yup from "yup";
import { register } from "../../../stores/thunks/userthunks";

export default function Signup() {
    const dispatch = useAppDispatch();
    const [status, setStatus] = useState('init');

    async function handleSubmit(request: RegisterRequest) {
        setStatus('loading');
        let result = await dispatch(register(request)); 
        if (result.meta.requestStatus == 'fulfilled'){ //not sure this actually works?
            setStatus('successful');
        }
        else {
            setStatus('failed');
        }
    }

    const validationschema = Yup.object().shape(
            {
                email: Yup.string()
                    .email('Must be a valid email address')
                    .required('Email is Required'),
                username: Yup.string()
                    .required('Username is Required'),
                password: Yup.string()
                    .required('Password is Required')
                    .min(6, 'Password must be at least 6 characters long'),
                confirmpassword: Yup.string()
                    .required('Confirm Password is Required')
                    .oneOf([Yup.ref('password')], 'Your passwords do not match.')
            }
        )

    const SignupFormik = useFormik({ 
        initialValues: {
            email: '',
            username: '',
            password: '',
            confirmpassword: '',
        },
        validationSchema: validationschema,
        onSubmit: (values) => {
            handleSubmit({
                username: values.username,
                email: values.email,
                password: values.password,
                confirmpassword: values.confirmpassword,
                acceptterms: true
            })
        },
    });

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="signup-page">
                <Paper elevation={6} className="signup-card">
                    <Typography variant="h4" align="center" gutterBottom>
                        Sign up
                    </Typography>

                    <form onSubmit={SignupFormik.handleSubmit}>
                        <TextField
                            id="email"
                            name="email"
                            label="Email"
                            variant="outlined"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={SignupFormik.values.email}
                            onChange={SignupFormik.handleChange}
                            error={SignupFormik.touched.email && Boolean(SignupFormik.errors.email)}
                            helperText={SignupFormik.touched.email && SignupFormik.errors.email}
                        ></TextField>

                        <TextField
                            id="username"
                            name="username"
                            label="Username"
                            variant="outlined"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={SignupFormik.values.username}
                            onChange={SignupFormik.handleChange}
                            error={SignupFormik.touched.username && Boolean(SignupFormik.errors.username)}
                            helperText={SignupFormik.touched.username && SignupFormik.errors.username}
                        ></TextField>

                        <TextField
                            id="password"
                            name="password"
                            label="Password"
                            variant="outlined"
                            type="password"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={SignupFormik.values.password}
                            onChange={SignupFormik.handleChange}
                            error={SignupFormik.touched.password && Boolean(SignupFormik.errors.password)}
                            helperText={SignupFormik.touched.password && SignupFormik.errors.password}
                        ></TextField>

                        <TextField
                            id="confirmpassword"
                            name="confirmpassword"
                            label="Confirm Password"
                            variant="outlined"
                            type="password"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={SignupFormik.values.confirmpassword}
                            onChange={SignupFormik.handleChange}
                            error={SignupFormik.touched.confirmpassword && Boolean(SignupFormik.errors.confirmpassword)}
                            helperText={SignupFormik.touched.confirmpassword && SignupFormik.errors.confirmpassword}
                        ></TextField>

                        <Button
                            type="submit"
                            variant="contained"
                            color="primary"
                            fullWidth
                            className="signup-button"
                        >
                            Sign up
                        </Button>
                        <Link to='../login'>Already have an account? Log in here</Link>
                    </form>
                </Paper>
                {status === 'successful' && <Navigate to="/signup-success"></Navigate>}
                {status === 'failed' && <Alert severity="error">
                    Signup failed. Invalid credentials.
                </Alert>}
                <Backdrop
                    sx={(theme) => ({ color: '#fff', zIndex: theme.zIndex.drawer + 1 })}
                    open={status === 'loading'}
                >
                    <CircularProgress color="inherit" />
                </Backdrop>
            </Box>
        </ThemeProvider>
    );
}