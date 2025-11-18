// import { updateUser, updateToken } from "../../stores/slices/currentuserslice";
import './login.css';
import { useState } from "react";
import { useAppDispatch, useUpdateCurrentUser} from "../../../hooks/hooks";
import { Box, Paper, Typography, TextField, Button, Backdrop, CircularProgress, Alert } from '@mui/material';
import { ThemeProvider } from "@emotion/react";
import { darkTheme } from '../../../themes/themes';
import { Link, Navigate } from "react-router";
import { useFormik } from 'formik';
import type { AuthenticateRequest, User } from "../../../models/user";
import * as Yup from "yup";
import { login } from '../../../stores/thunks/userthunks';
import { startRefreshTokenTimer, updateCurrentUserHelper } from '../../../utils/helpers/userhelpers';

export default function Login() {
    // const users = useSelector(selectAllUsers);
    const dispatch = useAppDispatch();
    const [status, setStatus] = useState('init');

    // useEffect(() => {
    // }, [])


    async function handleSubmit(request: AuthenticateRequest) {
        setStatus('loading');
        let response = await dispatch(login(request));

        console.log(response.meta.requestStatus);
        console.log(response.payload);
        if (response.meta.requestStatus === 'fulfilled'){
            setStatus('successful');
            updateCurrentUserHelper(response.payload);
            startRefreshTokenTimer();
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
            password: Yup.string()
                .required('Password is Required')
                .min(6, 'Password must be at least 6 characters long'),
        }
    )

    const LoginFormik = useFormik({
        initialValues: {
            email: '',
            password: '',
        },
        validationSchema: validationschema,
        onSubmit: (values) => {
            handleSubmit({
                email: values.email,
                password: values.password,
            })
        }
    })

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="login-page">
                <Paper elevation={6} className="login-card">
                    <Typography variant="h4" align="center" gutterBottom>
                        Login
                    </Typography>
                    <form onSubmit={LoginFormik.handleSubmit}>
                        <TextField
                            id="email"
                            name="email"
                            label="Email"
                            variant="outlined"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={LoginFormik.values.email}
                            onChange={LoginFormik.handleChange}
                            error={LoginFormik.touched.email && Boolean(LoginFormik.errors.email)}
                            helperText={LoginFormik.touched.email && LoginFormik.errors.email}
                        ></TextField>

                        <TextField
                            id="password"
                            name="password"
                            type="password"
                            label="Password"
                            variant="outlined"
                            fullWidth
                            margin="normal"
                            className="field"
                            value={LoginFormik.values.password}
                            onChange={LoginFormik.handleChange}
                            error={LoginFormik.touched.password && Boolean(LoginFormik.errors.password)}
                            helperText={LoginFormik.touched.password && LoginFormik.errors.password}
                        ></TextField>

                        <Button
                            type="submit"
                            variant="contained"
                            color="primary"
                            fullWidth
                            className="login-button"
                        >
                            Login
                        </Button>
                    </form>
                    <Link to='../forgot-password'>Forgot your password? Reset it here</Link>
                    <br></br>
                    <Link to='../signup'>Not yet registered? Create an account here</Link>
                </Paper>
                {status === 'successful' && <Navigate to='../'></Navigate>}
                {status === 'failed' && <Alert severity="error">
                    Login failed. Invalid credentials.
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