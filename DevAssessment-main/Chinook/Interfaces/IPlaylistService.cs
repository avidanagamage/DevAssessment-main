using Chinook.Models;
using System.Threading.Tasks;

namespace Chinook.Interfaces
{
    public interface IPlaylistService
    {
        Task<Chinook.ClientModels.Playlist?> GetPlaylist(long playlistId, string currentUserId);
        Task RemoveTrackFromPlaylist(long playlistId, long trackId, string userId);
        Task FavoriteTrack(long trackId, string userId);
        Task UnfavoriteTrack(long trackId, string userId);
        Task<List<UserPlaylist>> GetUserPlaylist(string userId);
        Task<List<Playlist>> GetPlaylistsAsync(string userId);
        Task<Models.Playlist> GetPlaylistByNameAsync(string name);
        Task<Models.Playlist> CreatePlaylistAsync(Playlist playlist);
        Task AddTrackToPlaylistAsync(Playlist playList, long trackId, string userId);
    }
}
