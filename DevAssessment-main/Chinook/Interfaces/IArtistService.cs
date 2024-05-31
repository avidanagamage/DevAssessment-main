using Chinook.ClientModels;
using Chinook.Models;

namespace Chinook.Interfaces
{
    public interface IArtistService
    {
        Task<Artist?> GetArtistAsync(long artistId);
        Task<List<PlaylistTrack>> GetTracksAsync(long artistId, string currentUserId);
        Task AddToFavoriteTrackAsync(long trackId, string userId);
        Task RemoveFromFavoriteTrackAsync(long trackId, string userId);
    }
}
