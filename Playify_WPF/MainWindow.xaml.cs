using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Playify_WPF.ViewModels;
using Playify_WPF.Data;
using SpotifyAPI.Web; //Base Namespace
using SpotifyAPI.Web.Auth; //All Authentication-related classes
using SpotifyAPI.Web.Enums; //Enums
using SpotifyAPI.Web.Models; //Models for the JSON-responses

namespace Playify_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SpotifyWebAPI _spotify = null;
        string user_id = null;
        List<SmartList> smartLists = new List<SmartList>();
        const string prefix = "[S*]";
        public MainWindow()
        {
            InitializeComponent();
            
            authenticate();
            var profile = _spotify.GetPrivateProfile();
            user_id = profile.Id;
            Paging<SimplePlaylist> playlists =_spotify.GetUserPlaylists(user_id, 200);
            foreach (SimplePlaylist p in playlists.Items)
            {
                int num_tracks = p.Tracks.Total;
                Console.WriteLine("Found user playlist " + p.Name + " with " + num_tracks);
                if (p.Name.Substring(0, 4) == "[S*]")
                {
                    Console.WriteLine("found a smart playlist!");
                    SmartList list = new SmartList();
                    list.spotify_id = p.Id;
                    list.playify_id = p.Name.Split(' ')[1];
                    list.tracks = _spotify.GetPlaylistTracks(user_id, list.spotify_id).Items;
                    int total_grabbed = list.tracks.Count;
                    while (total_grabbed < num_tracks)
                    {
                        List<PlaylistTrack> more_tracks = _spotify.GetPlaylistTracks(user_id, list.spotify_id, "", 100, total_grabbed).Items;
                        total_grabbed += more_tracks.Count;
                        list.tracks.AddRange(more_tracks);
                    }
                    if (list.playify_id == "Master_List")
                    {
                        Console.WriteLine("found the master list!");
                        List<SongViewModel> song_result_list = new List<SongViewModel>();
                        foreach (PlaylistTrack track in list.tracks)
                        {
                            string artistList = "";
                            foreach (SimpleArtist a in track.Track.Artists)
                            {
                                artistList += a.Name + ", ";
                                artistList = artistList.Substring(0, artistList.Length - 2);
                            }
                            Song s = new Song()
                            {
                                Artist = artistList,
                                Album = track.Track.Album.Name,
                                Title = track.Track.Name,
                                uid = track.Track.Uri
                           
                            };

                            SongViewModel svm = SongViewModel.FromSong(s);
                            song_result_list.Add(svm);

                        }
                        searchResults.ItemsSource = song_result_list;
                    }
                    smartLists.Add(list);
                }
                foreach (SongViewModel svm in searchResults.ItemsSource)
                {
                    svm.Config = get_config(svm.spotify_id);
                }
            }

         
        }

        private async void authenticate()
        {
            string clientID = "820c135b23ff40409370f501e78bf251";
            WebAPIFactory webApiFactory = new WebAPIFactory(
                "http://localhost",
                8000,
                clientID,
                Scope.PlaylistModifyPublic | Scope.PlaylistModifyPrivate,
                TimeSpan.FromSeconds(20)
                );
            try
            {
                _spotify = await webApiFactory.GetWebApi();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            
        }

        private void search_keyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Console.WriteLine("Searching for " + txtSearch.Text);
                string[] search_terms = txtSearch.Text.Split(' ');
                string q = "";
                foreach (string term in search_terms)
                    q += term + "+";
                q = q.Substring(0, q.Length - 1);
                Console.WriteLine("Search_string = " + q);
                SearchItem item = _spotify.SearchItems(q, SearchType.Artist | SearchType.Album | SearchType.Track, 50);
                Paging < FullTrack > tracks = item.Tracks;
                Console.WriteLine("Found " + tracks.Total + " tracks");

                List<SongViewModel> song_result_list = new List<SongViewModel>();
                foreach (FullTrack track in tracks.Items)
                {
                    string artistList = "";
                    foreach (SimpleArtist a in track.Artists)
                    {
                        artistList += a.Name + ", ";
                        artistList = artistList.Substring(0, artistList.Length - 2);
                    }
                    Song s = new Song()
                    {
                        Artist = artistList,
                        Album = track.Album.Name,
                        Title = track.Name,
                        uid = track.Uri,
                        config = get_config(track.Uri)
                    };

                    SongViewModel svm = SongViewModel.FromSong(s);
                    song_result_list.Add(svm);
                    
                }
                searchResults.ItemsSource = song_result_list;
            }
        }
        private string get_config(string track_id)
        {
            string config = "";


            foreach (SmartList list in smartLists)
            {
                if (!list.playify_id.Contains('_'))
                {
                 //   Console.WriteLine("Checking for song in " + list.playify_id);
                    List<PlaylistTrack> tracks = list.tracks;
                    foreach (PlaylistTrack track in tracks)
                    {
                        if (track.Track.Uri == track_id)
                            config += list.playify_id + " ";
                    }
                }
            }
            if (config.Length > 0)
            {
                config = config.Substring(0, config.Length - 1);
            }
            return config;
        }
        private void search_result_clicked(object sender, SelectionChangedEventArgs e)
        {
            SongViewModel song = searchResults.SelectedItem as SongViewModel;
            
            if (song != null)
            {
                lblAlbum.Content = song.Album;
                lblArtist.Content = song.Artist;
                lblTitle.Content = song.Title;
                txtConfig.Text = song.Config;
            }
   
        }

        private void config_to_playlist(SongViewModel song)
        {
            Console.WriteLine("Configuring " + song.Title + " with '" + song.Config + "'");
            List<string> playlist_titles = song.Config.Split(' ').ToList<string>();
            if (playlist_titles.Count > 1)
                playlist_titles.Add(song.Config.Replace(' ', '_'));

            Console.WriteLine("Configuring " + playlist_titles.Count + " playlist(s)");
            foreach (string title in playlist_titles)
            {
                Console.WriteLine("Adding track to playlist " + title);
                //check if playlist exists
                bool found = false;
                bool on_list = false;
                string targetList = null;

                Console.WriteLine("Does " + title + " exist?");
                foreach (SmartList list in smartLists)
                {
                    if (list.playify_id == title)
                    {
                        found = true;
                        targetList = list.spotify_id;
                        Console.WriteLine("Found playlist with title " + title + " and id " + targetList);
                    }

                }

                //create if not exist
                if (!found)
                {
                    Console.WriteLine("..nope, let's create it!");
                    string my_title = prefix + " " + title;
                    FullPlaylist full_list = _spotify.CreatePlaylist(user_id, my_title);
                    targetList = full_list.Id;
                    Console.WriteLine("Done! New playlist with title " + title + " and id " + targetList);
                }
                //check if track already in list
                else
                    foreach (SmartList list in smartLists)
                        if (list.playify_id == title)
                            foreach (PlaylistTrack track in list.tracks)
                                if (track.Track.Uri == song.spotify_id)
                                    on_list = true;

                //add song to list
                if (!on_list)
                {
                    Console.Write("adding track..");



                    ErrorResponse response = _spotify.AddPlaylistTrack(user_id, targetList, song.spotify_id);
                    if (response.HasError())
                        Console.WriteLine(response.Error.Message);
                    Console.WriteLine("done!");
                }
                else
                    Console.WriteLine("it's already there bruh!");
   

            }
        }
        private void config_entered(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SongViewModel song = searchResults.SelectedItem as SongViewModel;
                string config = txtConfig.Text;
                song.Config = config;
                config_to_playlist(song);
                
            }
        }

        private void btnManage_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("fo real tho?", "bluh..", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                foreach (SongViewModel svm in searchResults.ItemsSource)
                {
                    
                }
            }

        }
    }
}
