import { configureStore } from "@reduxjs/toolkit";
import userdataReducer from './slices/userdataslice';
import songdataReducer from './slices/songdataslice';
import playlistdataReducer from "./slices/playlistdataslice";
// import currentuserReducer from './slices/currentuserslice';
import playbackReducer from "./slices/playbackslice";
import recommendationModeReducer from "./slices/recommendationmodeslice";

const store = configureStore({
    reducer: {
        userdata: userdataReducer,
        songdata: songdataReducer,
        playlistdata: playlistdataReducer,
        playback: playbackReducer,
        recommendationmode: recommendationModeReducer,
    },
}
);

// if (typeof window !== undefined) {
//     store.subscribe(() => {
//         localStorage.setItem('currentuser', JSON.stringify(store.getState().userdata.currentuser))
//     })
// }

export default store;
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;