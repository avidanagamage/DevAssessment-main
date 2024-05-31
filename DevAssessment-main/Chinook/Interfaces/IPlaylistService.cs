namespace Chinook.Interfaces
{
    public interface IPlaylistService
    {
        Task<Chinook.ClientModels.Playlist?> GetPlaylist(long playlistId, string currentUserId);
        Task RemoveTrackFromPlaylist(long playlistId, long trackId, string userId);
        Task FavoriteTrack(long trackId, string userId);
        Task UnfavoriteTrack(long trackId, string userId);
    }
}
