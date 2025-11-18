import { BrowserRouter, Route, Routes } from 'react-router'
import Home from './pages/home/home'
import Login from './pages/auth/login/login'
import Signup from './pages/auth/signup/signup'
import './App.css'
import { Provider } from 'react-redux'
import userstore from './stores/store'
import { ThemeProvider } from '@emotion/react'
import { darkTheme } from './themes/themes'
import { SignupSuccess } from './pages/auth/signup/signupsuccess'
import { ResetPassword } from './pages/auth/reset-password/reset-password'
import { VerifyEmail } from './pages/auth/verify-email/verify-email'
import { ForgotPassword } from './pages/auth/forgot-password/forgot-password'
import Profile from './pages/profile/profile'
import EditProfile from './pages/profile/editprofile'
import { useAppDispatch } from './hooks/hooks'
import { setAppDispatch } from './stores/storedispatch'
import { useEffect } from 'react'
import ForYou from './pages/for-you/for-you'
import AdminDashboard from './pages/admin/admin'
import SongUpload from './pages/songupload/songupload'
import Library from './pages/library/library'
import ListenPlaylist from './pages/library/listen-playlist/listen-playlist'

function App() {
  const dispatch = useAppDispatch();
  useEffect(() => {
    setAppDispatch(dispatch);
  }, [dispatch]);

  return (
    <ThemeProvider theme={darkTheme}>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Home></Home>}></Route>
          <Route path="/login" element={<Login />}></Route>
          <Route path="/signup" element={<Signup />}></Route>
          <Route path="/signup-success" element={<SignupSuccess />}></Route>
          <Route path="/verify-email/:token" element={<VerifyEmail />}></Route>
          <Route path="/forgot-password" element={<ForgotPassword />}></Route>
          <Route path="/reset-password/:token" element={<ResetPassword />}></Route>
          <Route path="/profile/:id" element={<Profile />}></Route>
          <Route path="/profile/:id/edit" element={<EditProfile />}></Route>
          <Route path="/for-you" element={<ForYou/>}></Route>
          <Route path="/admin" element={<AdminDashboard/>}></Route>
          <Route path="/song-upload" element={<SongUpload/>}></Route>
          <Route path="/library" element={<Library/>}>
            
          </Route>
          {/* <Route path="" element={<Home />}></Route> */}
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  )
}

export default App
