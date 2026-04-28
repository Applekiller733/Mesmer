
//const API_URL = "PLACEHOLDER";

import { useAppDispatch, useUpdateCurrentUser } from "../../hooks/hooks";
import type { AuthenticateRequest, ForgotPasswordRequest, RegisterRequest, ResetPasswordRequest, UpdateUserRequest, User, VerifyEmailRequest } from "../../models/user";
import { startRefreshTokenTimer, updateCurrentUserHelper } from "../../utils/helpers/userhelpers";
import { updateCurrentUser } from "../slices/userdataslice";
import type { AppDispatch } from "../store";
import { getAppDispatch } from "../storedispatch";
import authHeader from "./apihelper";

const API_URL = `${import.meta.env.VITE_API_URL}/accounts`

const userdata = [
    {
        id: '1',
        username: 'test',
        password: 'testpass'
    },
    {
        id: '2',
        username: 'andrei',
        password: '123'
    }
]

// function updateCurrentUserHelper(user: User){
//     const updateCurrentUserHook = useUpdateCurrentUser();
//     updateCurrentUserHook(user);
// }

export function apifetchUsersMocked() {
    return new Promise(resolve => setTimeout(() => { resolve(userdata) }, 500));
}

export function apifetchUsers() {
    const url = `${API_URL}`;
    return fetch(url, {
        method: "GET",
        headers: { "Content-Type": "application/json", ...authHeader(url) },
        credentials: 'include',
    })
    .then(response => response.json());
}

export function apifetchUser(id: string) {
    return "NOT IMPLEMENTED";
}

export function apifetchUserProfile(id: string) {
    const url = `${API_URL}/profile/${id}`;
    return fetch(url, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
    })
        .then(response => response.json())
}

export async function apigetprofilepicture(id: string): Promise<Blob> {
    const url = `${API_URL}/profile/${id}/picture`;
    const response = await fetch(url, {
        method: "GET",
        headers: { ...authHeader(url) },
    });
 
    if (!response.ok) {
        throw new Error(
            response.status === 404
                ? "No profile picture"
                : `Fetching profile picture failed (${response.status})`
        );
    }
 
    return await response.blob();
}


export function apiregister(request: RegisterRequest) {
    const url = `${API_URL}/register`
    return fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request)
    })
}

export function apiverify(request: VerifyEmailRequest) {
    const url = `${API_URL}/verify-email`;
    return fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request)
    })
}

export async function apilogin(request: AuthenticateRequest) {
    const url = `${API_URL}/authenticate`
    const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request),
        credentials: "include",
    })

    const data = await response.json();
    if (!response.ok) {
        throw new Error(data.message || "Authentication failed");
    }

    return data; 
}

export async function apilogout(): Promise<unknown | null> {
    const url = `${API_URL}/revoke-token`;
    try {
        const response = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json", ...authHeader(url) },
            credentials: "include",
        });
        // 401 / 400 / network error — none of these should block local logout.
        if (!response.ok) return null;
        return await response.json().catch(() => null);
    } catch {
        return null;
    }
}


export function apiforgotpassword(request: ForgotPasswordRequest) {
    const url = `${API_URL}/forgot-password`;
    return fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request),
    })
}

export function apiresetpassword(request: ResetPasswordRequest) {
    const url = `${API_URL}/reset-password`;
    return fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request),
    })
}

//todo maybe refactor
export async function apiupdateuser(request: UpdateUserRequest) {
    const url = `${API_URL}`;
    const formData = new FormData();
    var stringid = request.id.toString();

    formData.append("Id", stringid);
    if (request.username) formData.append("UserName", request.username);
    if (request.email) formData.append("Email", request.email);
    if (request.password) formData.append("Password", request.password);
    if (request.confirmpassword) formData.append("ConfirmPassword", request.confirmpassword);
    if (request.role) formData.append("Role", request.role);

    if (request.profilepicture && request.profilepicture.file) {
        formData.append("ProfilePicture.FileName", request.profilepicture.filename);
        formData.append("ProfilePicture.Extension", request.profilepicture.extension);
        formData.append("ProfilePicture.FormFile", request.profilepicture.file);
    }

    const response = await fetch(url, {
        method: "PUT",
        headers: {
            ...authHeader(url),
        },
        body: formData,
    })

    const data = await response.json();
    if (!response.ok) {
        throw new Error(data.message || "Updating user failed");
    }

    return data; 
       
}

export function apideleteuser(id: string) {
    const url = `${API_URL}/${id}`;
    return fetch(url, {
        method: "DELETE",
        headers: {"Content-Type": "application/json", ...authHeader(url)}
    })
}

export async function apirefreshToken(): Promise<boolean> {
    const url = `${API_URL}/refresh-token`;
    try {
        const response = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
        });
 
        if (!response.ok) return false;
 
        const apiUser = await response.json();
        if (!apiUser || typeof apiUser.jwtToken !== "string") return false;
 
        // updateCurrentUserHelper writes localStorage and the slice in one go.
        updateCurrentUserHelper(apiUser);
        return true;
    } catch {
        return false;
    }
}



