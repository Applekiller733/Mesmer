import type { FileModel } from "./file"

export interface User{
    id?: number,
    username: string,
    email:string,
    role:string,
    token:string,
}

export interface RegisterRequest{
    username: string,
    email:string,
    password:string,
    confirmpassword:string,
    acceptterms: boolean
}

export interface VerifyEmailRequest {
    token: string,
}

export interface AuthenticateRequest {
    email: string,
    password: string,
}

export interface ForgotPasswordRequest {
    email: string,
}

export interface ResetPasswordRequest {
    token: string,
    password: string,
    confirmpassword: string,
}

export interface UserProfile {
    username: string,
    role: string,
    createdAt: string,
    updatedAt: string,
}

export interface UpdateUserRequest{
    id: number,
    username: string | null,
    email: string | null,
    password: string | null,
    confirmpassword: string | null,
    role: string | null,
    profilepicture: FileModel | null,
}

export interface UserAdminView {
    id: number | null,
    username: string | null,
    email: string | null,
    role: string | null,
    profilepicture: FileModel | null,
}