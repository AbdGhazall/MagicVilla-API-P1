using MagicVilla_VillaAPI.Models;

namespace MagicVilla_VillaAPI.Repository.IRepository
{
    public interface IVillaRepository : IRepository<Villa> // inherit from the generic repository
    {
        Task<Villa> UpdateAsync(Villa entity); // update may be dfferente from repo to another so we keep it here not in the generic repo
    }
}