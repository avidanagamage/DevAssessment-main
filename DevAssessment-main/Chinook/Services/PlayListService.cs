using Chinook.ClientModels;
using Chinook.Interfaces;
using Chinook.Models;
using Chinook.Utility;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace Chinook.Services
{
    public class PlayListService: IPlaylistService
    {
        private readonly ChinookContext _dbContext;
        public event EventHandler<UserPlaylist> PlaylistAdded;
        public event EventHandler<UserPlaylist> PlaylistUpdated;
        private List<UserPlaylist> _userPlaylists = new List<UserPlaylist>();
        public PlayListService(ChinookContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Chinook.ClientModels.Playlist?> GetPlaylist(long playlistId, string currentUserId)
        {
            return await _dbContext.Playlists
                .Include(a => a.Tracks).ThenInclude(a => a.Album).ThenInclude(a => a.Artist)
                .Where(p => p.PlaylistId == playlistId)
                .Select(p => new ClientModels.Playlist()
                {
                    Name = p.Name,
                    Tracks = p.Tracks.Select(t => new ClientModels.PlaylistTrack()
                    {
                        AlbumTitle = t.Album.Title,
                        ArtistName = t.Album.Artist.Name,
                        TrackId = t.TrackId,
                        TrackName = t.Name,
                        IsFavorite = t.Playlists.Where(p => p.UserPlaylists.Any(up => up.UserId == currentUserId && up.Playlist.Name == "Favorites")).Any()
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task RemoveTrackFromPlaylist(long playlistId, long trackId, string userId)
        {
            var playlistTrack = await _dbContext.UserPlaylists.Include(x => x.Playlist).ThenInclude(x => x.Tracks)
                .Where(pt => pt.PlaylistId == playlistId && pt.Playlist.Tracks.Any(x => x.TrackId == trackId))
                .FirstOrDefaultAsync();

            if (playlistTrack != null)
            {
                _dbContext.UserPlaylists.Remove(playlistTrack);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task FavoriteTrack(long trackId, string userId)
        {
            var uplaylist = await _dbContext.UserPlaylists.ToListAsync();
            var playlist = await _dbContext.Playlists.Include(x => x.UserPlaylists).Include(x => x.Tracks).ToListAsync();

            var track = await _dbContext.Tracks.FirstOrDefaultAsync(x => x.TrackId == trackId);
            var favoritePlaylist = await _dbContext.Playlists
                .Include(p => p.UserPlaylists)
                .FirstOrDefaultAsync(p => p.Name == Constants.FavoritePlaylistName && p.UserPlaylists.Any(up => up.UserId == userId));

            if (favoritePlaylist == null)
            {
                favoritePlaylist = new Models.Playlist
                {
                    Name = Constants.FavoritePlaylistName,
                    Tracks = new List<Track> { track }

                };
                var userPlaylist = new UserPlaylist
                {
                    UserId = userId,
                    Playlist = favoritePlaylist
                };

                await _dbContext.UserPlaylists.AddAsync(userPlaylist);
            }

            else
            {
                favoritePlaylist.Tracks.Add(track);
                _dbContext.Playlists.Update(favoritePlaylist);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task UnfavoriteTrack(long trackId, string userId)
        {
            var userPlaylists = await GetUserPlaylistsAsync(userId);
            var favoritePlaylist = userPlaylists.FirstOrDefault(up => up.Playlist.Name == Constants.FavoritePlaylistName);

            if (favoritePlaylist != null)
            {
                var userPlaylist = await GetUserPlaylistAsync(favoritePlaylist.PlaylistId, trackId, userId);

                if (userPlaylist != null)
                {
                    await DeleteUserPlaylistAsync(userPlaylist, trackId);
                }
            }
        }

        public void AddPlaylist(UserPlaylist playlist)
        {
            _userPlaylists.Add(playlist);
            PlaylistAdded?.Invoke(this, playlist);
        }

        public void UpdatePlaylist(UserPlaylist playlist)
        {
            var existingUserPlaylist = _userPlaylists.FirstOrDefault(p => p.PlaylistId == playlist.PlaylistId);
            if (existingUserPlaylist != null)
            {
                existingUserPlaylist.Playlist.Name = playlist.Playlist.Name;
                PlaylistUpdated?.Invoke(this, playlist);
            }
        }

        private async Task DeleteUserPlaylistAsync(UserPlaylist userPlaylist, long trackId)
        {
            if (userPlaylist?.Playlist?.Tracks == null)
            {
                throw new ArgumentNullException(nameof(userPlaylist), "UserPlaylist or Playlist or Tracks is null");
            }

            var track = userPlaylist.Playlist.Tracks.FirstOrDefault(x => x.TrackId == trackId);
            if (track != null)
            {
                userPlaylist.Playlist.Tracks.Remove(track);

                if (userPlaylist.Playlist.Tracks.Count == 0)
                {
                    _dbContext.Playlists.Remove(userPlaylist.Playlist);
                    _dbContext.UserPlaylists.Remove(userPlaylist);
                }
                else
                {
                    _dbContext.UserPlaylists.Update(userPlaylist);
                }

                await _dbContext.SaveChangesAsync();
            }
        }
        private async Task<List<UserPlaylist>> GetUserPlaylistsAsync(string userId)
        {
            var userPlaylists = await _dbContext.UserPlaylists
                .Where(up => up.UserId == userId)
                .Include(up => up.Playlist)
                .ToListAsync();

            return userPlaylists;
        }
        private async Task<UserPlaylist?> GetUserPlaylistAsync(long playlistId, long trackId, string userId)
        {
            return await _dbContext.UserPlaylists
                .Include(up => up.Playlist).ThenInclude(x => x.Tracks)
                .Where(up => up.PlaylistId == playlistId && up.Playlist.Tracks.Any(x => x.TrackId == trackId) && up.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<UserPlaylist>> GetUserPlaylist(string userId)
        {
            _userPlaylists = await _dbContext.UserPlaylists.Include(a => a.Playlist).Where(x=> x.UserId == userId).ToListAsync();
            return _userPlaylists;
        }

        public async Task<List<Models.Playlist>> GetPlaylistsAsync(string userId)
        {
            return await _dbContext.Playlists.Include(a => a.Tracks).Include(x=> x.UserPlaylists).ToListAsync();
        }

        public async Task<Models.Playlist> GetPlaylistByNameAsync(string name)
        {
            return await _dbContext.Playlists.FirstOrDefaultAsync(p => p.Name == name);
        }

        public async Task<Models.Playlist> CreatePlaylistAsync(Models.Playlist playlist)
        {
            var totalPlayListCount = _dbContext.Playlists.ToList().Count;
            playlist.PlaylistId = totalPlayListCount + 1;
            _dbContext.Playlists.Add(playlist);
            await _dbContext.SaveChangesAsync();
            return playlist;
        }

        public async Task AddTrackToPlaylistAsync(Models.Playlist playList, long trackId, string userId)
        {
            if (playList.UserPlaylists == null || playList.UserPlaylists.Count == 0 || !playList.UserPlaylists.Any(x=> x.UserId == userId))
            {
                var userPlaylist = new UserPlaylist
                {
                    UserId = userId,
                    PlaylistId = playList.PlaylistId,
                    Playlist = playList,
                };
                _dbContext.UserPlaylists.Add(userPlaylist);
            }
            var track = await _dbContext.Tracks.Where(x => x.TrackId == trackId).FirstOrDefaultAsync();
            playList.Tracks.Add(track);
            _dbContext.Entry(playList).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }
    }
}
