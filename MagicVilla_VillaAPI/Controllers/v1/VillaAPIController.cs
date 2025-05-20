using System.Net;
using System.Text.Json;
using AutoMapper;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.DTO;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace MagicVilla_VillaAPI.Controllers.v1
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")] // default version
    public class VillaAPIController : ControllerBase
    {
        protected APIResponse _response; // work with APIResponse class to return response in a standard way
        private readonly ILogger<VillaAPIController> _logger; // work with serilog and built-in logging
        private readonly IMapper _mapper; // work with automapper
        private readonly IVillaRepository _dbVilla; // work with repository

        public VillaAPIController(ILogger<VillaAPIController> logger, IMapper mapper, IVillaRepository villaRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _dbVilla = villaRepository;
            _response = new();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ResponseCache(CacheProfileName = "Default30")] // this will cache the response for 30 seconds, will not hit the database again for 30 seconds and just return the cached response , get from program.cs
        public async Task<ActionResult<APIResponse>> GetVillas([FromQuery(Name = "filterOccupancy")] int? occupancy,
            [FromQuery] string? search, int pageSize = 0, int pageNumber = 1)
        {
            try
            {
                IEnumerable<Villa> villaList;
                if (occupancy > 0)
                {
                    villaList = await _dbVilla.GetAllAsync(v => v.Occupancy == occupancy, pageSize: pageSize, pageNumber: pageNumber); //call GetAll method from repository(contains everything)
                }
                else
                {
                    _logger.LogInformation("Getting all villas");
                    villaList = await _dbVilla.GetAllAsync(pageSize: pageSize, pageNumber: pageNumber); //call GetAll method from repository(contains everything)
                }
                if (!string.IsNullOrEmpty(search))
                {
                    villaList = villaList.Where(v => v.Name.ToLower().Contains(search)); // filter the villa list by name
                }

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize }; // create a new instance of Pagination class and set the page number and page size
                Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(pagination)); // add the pagination to the response headers

                _response.Result = _mapper.Map<List<VillaDTO>>(villaList); // map villaList(from database) to VillaDTO and assign it to the result property of the response
                _response.StatusCode = HttpStatusCode.OK; // set the status code of the response
                return Ok(_response); // return the response
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [HttpGet("{id}", Name = "GetVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //[ResponseCache(Duration = 30)] // this will cache the response for 30 seconds, will not hit the database again for 30 seconds and just return the cached response
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)] // this will not cache the response, will hit the database every time
        public async Task<ActionResult<APIResponse>> GetVilla(int id)
        {
            try
            {
                if (id == 0)
                {
                    _logger.LogError("GetVilla Error with id:" + id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                var villa = await _dbVilla.GetAsync(v => v.Id == id); //call Get method from repository(contains everything)
                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }
                _response.Result = _mapper.Map<VillaDTO>(villa); // map villa to VillaDTO and assign it to the result property of the response
                _response.StatusCode = HttpStatusCode.OK; // set the status code of the response
                return Ok(_response); // return the response
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateVilla([FromBody] VillaCreateDTO CreateDTO)
        {
            try
            {
                if (await _dbVilla.GetAsync(v => v.Name.ToLower() == CreateDTO.Name.ToLower()) != null)
                {
                    ModelState.AddModelError("ErrorMessages", "Villa already exists!");

                    return BadRequest(ModelState);
                }

                if (CreateDTO == null)
                {
                    return BadRequest(CreateDTO);
                }

                Villa villa = _mapper.Map<Villa>(CreateDTO); // map CreateDTO to Villa model

                //no need to use this code as we are using automapper
                //Villa model = new()
                //{
                //    Name = CreateDTO.Name,
                //    Occupancy = CreateDTO.Occupancy,
                //    Sqft = CreateDTO.Sqft,
                //    Rate = CreateDTO.Rate,
                //    ImageUrl = CreateDTO.ImageUrl,
                //    Amenity = CreateDTO.Amenity,
                //    Details = CreateDTO.Details,
                //};
                //await _db.Villas.AddAsync(model);
                //await _db.SaveChangesAsync();

                await _dbVilla.CreateAsync(villa); //call Create method from repository(contains everything)

                _response.Result = _mapper.Map<VillaDTO>(villa);
                _response.StatusCode = HttpStatusCode.Created;
                return CreatedAtRoute("GetVilla", new { id = villa.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [HttpDelete("{id:int}", Name = "DeleteVilla")]
        [Authorize(Roles = "admin")] // any other role can't access
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<APIResponse>> DeleteVilla(int id)
        {
            try
            {
                if (id == 0)
                {
                    return BadRequest();
                }
                var villa = await _dbVilla.GetAsync(v => v.Id == id);
                if (villa == null)
                {
                    return NotFound();
                }
                await _dbVilla.RemoveAsync(villa); //call Remove method from repository(contains everything)
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [HttpPut("{id:int}", Name = "UpdateVilla")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateVilla(int id, [FromBody] VillaUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null || id != updateDTO.Id)
                {
                    return BadRequest();
                }

                Villa model = _mapper.Map<Villa>(updateDTO); // map updateDTO to Villa model

                await _dbVilla.UpdateAsync(model); //call Update method from repository(contains everything)
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [HttpPatch("{id:int}", Name = "UpdatePartialVilla")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePartialVilla(int id, JsonPatchDocument<VillaUpdateDTO> patchDTO)
        {
            if (patchDTO == null || id == 0)
            {
                return BadRequest();
            }
            var villa = await _dbVilla.GetAsync(v => v.Id == id, tracked: false);

            VillaUpdateDTO villaDTO = _mapper.Map<VillaUpdateDTO>(villa); // map villa to VillaUpdateDTO

            if (villa == null)
            {
                return NotFound();
            }
            patchDTO.ApplyTo(villaDTO, ModelState);

            Villa model = _mapper.Map<Villa>(villaDTO); // map villaDTO to Villa model

            await _dbVilla.UpdateAsync(model);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return NoContent();
        }
    }
}