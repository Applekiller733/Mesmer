import type { User } from "../../models/user";
import { apirefreshToken } from "../../stores/api/userapi";
import { updateCurrentUser } from "../../stores/slices/userdataslice";
import { getAppDispatch } from "../../stores/storedispatch";

export function updateCurrentUserHelper(user: User) {
    const dispatch = getAppDispatch();
    // console.log(user);
    if (localStorage.getItem('currentuser')) {
        var storageuser = JSON.parse(localStorage.getItem('currentuser') || "");
        //todo maybe reimplement a bit more nicely
        if (storageuser.id !== user.id) {
            storageuser.id = user.id;
        }
        if (storageuser.username !== user.username) {
            storageuser.username = user.username;
        }
        if (storageuser.role !== user.role) {
            storageuser.role = user.role;
        }
        if (storageuser.email !== user.email) {
            storageuser.email = user.email;
        }
        localStorage.setItem('currentuser', JSON.stringify(storageuser));
    }
    else {
        localStorage.setItem('currentuser', JSON.stringify(user));
    }
    dispatch(updateCurrentUser(user));
}

export function deleteCurrentUserHelper() {
    const dispatch = getAppDispatch();
    localStorage.removeItem('currentuser');
    const emptyuser: User = {
        id: undefined,
        username: '',
        email: '',
        role: '',
        token: '',
    }
    dispatch(updateCurrentUser(emptyuser));
}

let refreshTokenTimeout: any;

//todo verify if token refresh timer works and fix if not
export function startRefreshTokenTimer() {
    console.log("START OF REFRESH TOKEN TIMER");
    console.log(localStorage.getItem('currentuser'));
    if (localStorage.getItem('currentuser')) {
        //@ts-ignore
        const user = JSON.parse(localStorage.getItem('currentuser'));

        console.log("INSIDE REFRESH TOKEN TIMER");

        const jwtToken = JSON.parse(atob(user.jwtToken.split('.')[1]));

        // console.log(jwtToken);
        // set a timeout to refresh the token a minute before it expires
        // console.log(jwtToken.exp);
        const expires = new Date(jwtToken.exp * 1000);
        const timeout = expires.getTime() - Date.now() - (60 * 1000);
        // const timeout = 5;
        // console.log(timeout);
        const trials = 3;
        // refreshTokenTimeout = setTimeout(apirefreshToken, timeout);
        refreshTokenTimeout = setTimeout(() => { refreshTokenTrials(trials) }, timeout);
    }
}

async function refreshTokenTrials(trials: number) {
    const timeout = 5 * 1000;
    const response = await apirefreshToken();
    if (typeof response === typeof Error || response === undefined || response === null && (trials > 0)) {
        setTimeout(() => { refreshTokenTrials(trials - 1) }, timeout)
    }
    else {
        startRefreshTokenTimer();
    }
}

export function stopRefreshTokenTimer() {
    clearTimeout(refreshTokenTimeout);
}