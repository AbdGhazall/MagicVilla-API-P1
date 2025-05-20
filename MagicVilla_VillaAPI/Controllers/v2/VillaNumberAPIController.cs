using AutoMapper;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace MagicVilla_VillaAPI.Controllers.v2
{
    // api-deprecated-versions: 2.0
    //[ApiVersion("2.0", Deprecated =true)] // this is the deprecated version (will be shown in the response that this method have another version)
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")] // reoute to this controller and specify the version that this controller will use
    [ApiVersion("2.0")] // new version
    public class VillaNumberAPIController : ControllerBase
    {
        protected APIResponse _response;
        private readonly IVillaNumberRepository _dbVillaNumber;
        private readonly IVillaRepository _dbVilla;
        private readonly IMapper _mapper;

        public VillaNumberAPIController(IVillaNumberRepository dbVillaNumber, IMapper mapper,
            IVillaRepository dbVilla)
        {
            _dbVillaNumber = dbVillaNumber;
            _mapper = mapper;
            _response = new();
            _dbVilla = dbVilla;
        }

        //[MapToApiVersion("2.0")]
        // this is the new version (no need to mention it here just above in head of the controller)
        [HttpGet("GetString")]
        public IEnumerable<string> Get()
        {
            return new string[] { "Abdullrahman", "Ghazal" };
        }
    }
}