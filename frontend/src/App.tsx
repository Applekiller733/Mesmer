import { BrowserRouter, Route, Routes } from 'react-router';
import Home from './pages/home/home';
import Login from './pages/auth/login/login';
import Signup from './pages/auth/signup/signup';
import './App.css';
import { ThemeProvider } from '@emotion/react';
import { darkTheme } from './themes/themes';
import { SignupSuccess } from './pages/auth/signup/signupsuccess';
import { ResetPassword } from './pages/auth/reset-password/reset-password';
import { VerifyEmail } from './pages/auth/verify-email/verify-email';
import { ForgotPassword } from './pages/auth/forgot-password/forgot-password';
import Profile from './pages/profile/profile';
import EditProfile from './pages/profile/editprofile';
import { useAppDispatch } from './hooks/hooks';
import { setAppDispatch } from './stores/storedispatch';
import { useEffect } from 'react';
import ForYou from './pages/for-you/for-you';
import AdminDashboard from './pages/admin/admin';
import SongUpload from './pages/songupload/songupload';
import Library from './pages/library/library';
import SocialsPage from "./pages/socials/socials";
import ViewPlaylistPage from "./pages/playlist/viewplaylistpage";
import ListenPlaylistPage from "./pages/playlist/listenplaylistpage";
import {
    startRefreshTokenTimer,
    stopRefreshTokenTimer,
    updateCurrentUserHelper,
} from './utils/helpers/userhelpers';

function App() {
    const dispatch = useAppDispatch();

    useEffect(() => {
        setAppDispatch(dispatch);

        try {
            const raw = localStorage.getItem('currentuser');
            if (raw) {
                const user = JSON.parse(raw);
                if (user && typeof user.jwtToken === 'string') {
                    updateCurrentUserHelper(user);
                    startRefreshTokenTimer();
                }
            }
        } catch {
            localStorage.removeItem('currentuser');
        }

        return () => stopRefreshTokenTimer();
    }, [dispatch]);

    return (
        <ThemeProvider theme={darkTheme}>
            <BrowserRouter>
                <Routes>
                    <Route path="/" element={<Home />} />
                    <Route path="/login" element={<Login />} />
                    <Route path="/signup" element={<Signup />} />
                    <Route path="/signup-success" element={<SignupSuccess />} />
                    <Route path="/verify-email/:token" element={<VerifyEmail />} />
                    <Route path="/forgot-password" element={<ForgotPassword />} />
                    <Route path="/reset-password/:token" element={<ResetPassword />} />
                    <Route path="/profile/:id" element={<Profile />} />
                    <Route path="/profile/:id/edit" element={<EditProfile />} />
                    <Route path="/for-you" element={<ForYou />} />
                    <Route path="/admin" element={<AdminDashboard />} />
                    <Route path="/song-upload" element={<SongUpload />} />
                    <Route path="/library" element={<Library />} />
                    <Route path="/socials" element={<SocialsPage />} />
                    <Route path="/playlist/:id" element={<ViewPlaylistPage />} />
                    <Route path="/playlist/:id/listen" element={<ListenPlaylistPage />} />
                </Routes>
            </BrowserRouter>
        </ThemeProvider>
    );
}

export default App;