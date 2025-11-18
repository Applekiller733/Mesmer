import { createSlice } from '@reduxjs/toolkit';
import type { User, UserProfile } from '../../models/user';
import type { RootState } from '../store';
import { fetchUserProfile, fetchUsers, register, verify } from '../thunks/userthunks';

const getInitialState = (): { users: User[], currentuser: User, loadedProfile: UserProfile, status: string, errormsg: string } => {
    const users: User[] = [];
    const currentuser: User = { id: undefined, username: '', email: '', role: '', token: '' };
    const loadedProfile: UserProfile = {username: '', role: '', createdAt: '', updatedAt: ''};
    const status: string = 'idle';
    const errormsg: string = '';

    if (localStorage.getItem('currentuser') && typeof localStorage.getItem('currentuser') !== null) {
        //@ts-ignore
        var user = JSON.parse(localStorage.getItem('currentuser'));

        currentuser.id = user.id;
        currentuser.email = user.email;
        currentuser.username = user.userName;
        currentuser.role = user.role;
        currentuser.token = user.jwtToken;
    }

    return {
        users: users,
        currentuser: currentuser,
        loadedProfile: loadedProfile,
        status: status,
        errormsg: errormsg,
    };
};

export const userdataSlice = createSlice(
    {
        name: 'userdata',
        initialState: getInitialState(),
        reducers: {
            updateCurrentUser: (state, action) => {
                state.currentuser = action.payload;                
                console.log(state.currentuser);
            },
        },
        extraReducers(builder) {
            builder.addCase(fetchUsers.pending, (state) => {
                state.status = 'loading';
            })
                .addCase(fetchUsers.fulfilled, (state, action) => {
                    state.status = 'succeeded';
                    const loadedUsers = action.payload?.map((u:any) => {
                        //in case i need to add any other ops here/remove fields etc idk
                        const user:User = {
                            id:u.id,
                            username: u.userName,
                            email: u.email,
                            role: u.role,
                            token: u.jwtToken,
                        }
                        return user;
                    });
                    console.log(loadedUsers);
                    if (loadedUsers !== undefined)
                        state.users = state.users.concat(loadedUsers);
                })
                .addCase(fetchUsers.rejected, (state, action) => {
                    state.status = 'failed';
                    state.errormsg = action.error.message || 'failed to load error';
                })
                .addCase(fetchUserProfile.fulfilled, (state, action) => {
                    state.status = 'succeeded';
                    const profile = action.payload;
                    state.loadedProfile = {
                        username: profile.userName,
                        role: profile.role,
                        createdAt: profile.created,
                        updatedAt: profile.updated,
                    };
                })
                
        },
    }
);

export const selectAllUsers = (store: RootState) => store.userdata.users; // Can add further validation
export const selectCurrentUser = (store: RootState) => store.userdata.currentuser;
export const selectLoadedProfile = (store: RootState) => store.userdata.loadedProfile;
export const selectUsersStatus = (store: RootState) => store.userdata.status;
export const selectUsersError = (store: RootState) => store.userdata.errormsg;

export const { updateCurrentUser } = userdataSlice.actions;

export default userdataSlice.reducer;