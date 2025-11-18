

export default function authHeader(url: any) {
    // return auth header with jwt if user is logged in and request is to the api url
    //@ts-ignore
    const user = JSON.parse(localStorage.getItem('currentuser'));
    // console.log(user);
    const isLoggedIn = user !== undefined && user.jwtToken !== null;
    const isApiUrl = url.startsWith(`${import.meta.env.VITE_API_URL}`);
    // console.log(isLoggedIn);
    // console.log(isApiUrl);
    if (isLoggedIn && isApiUrl) {
        return { Authorization: `Bearer ${user.jwtToken}` };
    } else {
        return ''
    };
}