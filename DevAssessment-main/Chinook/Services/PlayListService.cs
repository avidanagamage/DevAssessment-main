using Chinook.Models;
using Microsoft.EntityFrameworkCore;

namespace Chinook.Services
{
    public class PlayListService
    {
        private readonly ChinookContext _dbContext;

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
                .FirstOrDefaultAsync(p => p.Name == "My favorite tracks" && p.UserPlaylists.Any(up => up.UserId == userId));

            if (favoritePlaylist == null)
            {
                favoritePlaylist = new Models.Playlist
                {
                    Name = "My favorite tracks",
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
            var favoritePlaylist = userPlaylists.FirstOrDefault(up => up.Playlist.Name == "My favorite tracks");

            if (favoritePlaylist != null)
            {
                var userPlaylist = await GetUserPlaylistAsync(favoritePlaylist.PlaylistId, trackId, userId);

                if (userPlaylist != null)
                {
                    await DeleteUserPlaylistAsync(userPlaylist, trackId);
                }
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
    }
}
