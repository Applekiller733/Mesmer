import type { AppDispatch } from './store';
let appDispatch: AppDispatch | null = null;

export const setAppDispatch = (dispatch: AppDispatch) => {
    appDispatch = dispatch;
};

export const getAppDispatch = () => {
    if (!appDispatch) throw new Error("Dispatch not set");
    return appDispatch;
};