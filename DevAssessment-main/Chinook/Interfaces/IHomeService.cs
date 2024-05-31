using Chinook.Models;

namespace Chinook.Interfaces
{
    public interface IHomeService
    {

        Task<List<Artist>> GetArtists(string searchTerm = "");
        Task<List<Artist>> GetArtistsByName(string name);
        Task<List<Album>> GetAlbumsForArtist(int artistId);

    }
}
