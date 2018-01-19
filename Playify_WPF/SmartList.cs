using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI;
using SpotifyAPI.Web.Enums; //Enums
using SpotifyAPI.Web.Models; //Models for the JSON-responses

namespace Playify_WPF
{
    class SmartList
    {
        public List<PlaylistTrack> tracks { get; set; }
        public string spotify_id { get; set; }
        public string playify_id { get; set; }
    }
}
