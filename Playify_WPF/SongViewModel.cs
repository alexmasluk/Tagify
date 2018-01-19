using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Playify_WPF.Data;


namespace Playify_WPF.ViewModels
{
    class SongViewModel
    {
        public string Artist;
        public string Album;
        public string Title;
        public string Config = "";
        public string spotify_id = "";

        public static SongViewModel FromSong(Song song)
        {
            var songmodel = new SongViewModel();
            songmodel.Artist = song.Artist;
            songmodel.Album = song.Album;
            songmodel.Title = song.Title;
            songmodel.spotify_id = song.uid;
            songmodel.Config = song.config;
            return songmodel;
        }

        public override string ToString()
        {
            return Artist + " | " + Album + " ==> " + Title;
        }
    }
}
