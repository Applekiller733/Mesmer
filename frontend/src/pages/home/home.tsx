// import type { Route } from "../+types/home";
import { List, ListItem, ThemeProvider } from "@mui/material";
import { Box } from "@mui/material";
import { darkTheme } from "../../themes/themes";
import Navbar from "../../reusablecomponents/navbar";
import './home.css'
// import { useSelector } from "react-redux";
// import { fetchSongs, selectAllSongs } from "../../stores/slices/songdataslice";
// import { useAppDispatch } from "../../hooks/hooks";
// import { useEffect } from "react";
// import SongComponent from "../../reusablecomponents/song/songcomponent";
// import FileUploader from "../../reusablecomponents/fileuploader/fileuploader";
// import type { EmblaOptionsType } from 'embla-carousel'
// import VerticalCarousel from "../../reusablecomponents/carousel/verticalcarousel";

export default function Home() {

  return (
    <div>
      <ThemeProvider theme={darkTheme}>
        <Box className='home-page'>
          <Navbar></Navbar>
          {/* <div className="song-list"> */}
            {/* <List className="song-list">
            {songs.map((s) => (
              <ListItem key={s.id}>
                <SongComponent id={s.id} name={s.name} artist={s.artist} upvotes={s.upvotes}
                  image={s.image} video={s.video} sound={s.sound}></SongComponent>
              </ListItem>
            ))}
          </List> */}
          {/* </div> */}
        </Box>
      </ThemeProvider>
    </div>
  );
}
