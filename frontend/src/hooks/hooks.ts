import type { AppDispatch } from "../stores/store";
import { useDispatch } from "react-redux";
import type { User } from "../models/user";
import { updateCurrentUser } from "../stores/slices/userdataslice";

export const useAppDispatch = () => useDispatch<AppDispatch>();
 
export function useUpdateCurrentUser() {
    const dispatch = useAppDispatch();

    return (user: User) => {
        localStorage.setItem('currentuser', JSON.stringify(user));
        dispatch(updateCurrentUser(user));
    };
}

export function useDeleteCurrentUser(){
    const dispatch = useAppDispatch();

    return (user:User) => {
        localStorage.removeItem('currentuser');
        dispatch(updateCurrentUser(user));
    }
}