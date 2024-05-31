using Chinook.Models;
using Microsoft.EntityFrameworkCore;

namespace Chinook.Services
{
    public class HomeService
    {
        private readonly ChinookContext _dbContext;

        public HomeService(ChinookContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        public async Task<List<Artist>> GetArtists(string searchTerm = "")
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetArtists();
            }
            else
            {
                return await GetArtistsByName(searchTerm);
            }
        }

        public async Task<List<Artist>> GetArtistsByName(string name)
        {
            return await _dbContext.Artists.Include(x => x.Albums)
               .Where(a => a.Name.ToLower().Contains(name.ToLower()))
               .ToListAsync();
        }

        public async Task<List<Album>> GetAlbumsForArtist(int artistId)
        {
            return await _dbContext.Albums.Where(a => a.ArtistId == artistId).ToListAsync();
        }

        private async Task<List<Artist>> GetArtists()
        {
            var users = _dbContext.Users.Include(a => a.UserPlaylists).ToList();
            return await _dbContext.Artists.Include(x => x.Albums).ToListAsync();
        }
    }
}
