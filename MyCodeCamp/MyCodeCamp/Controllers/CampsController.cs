using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using MyCodeCamp.Filters;
using MyCodeCamp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyCodeCamp.Controllers
{
    //[Authorize]
    [EnableCors("AnyGET")]
    [Route("api/[controller]")]
    [ValidateModel]
    public class CampsController : BaseController
    {
        private readonly ICampRepository _repo;
        private readonly ILogger<CampsController> _logger;
        private readonly IMapper _mapper;

        public CampsController(ICampRepository repo,
                            ILogger<CampsController> logger,
                            IMapper mapper)
        {
            _repo = repo;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet("")]
        public IActionResult Get()
        {
            var camps = _repo.GetAllCamps();
            return Ok(_mapper.Map<IEnumerable<CampModel>>(camps));
        }

        [HttpGet("{moniker}", Name = "CampGet")]
        public IActionResult Get(string moniker, bool includeSpeakers = false)
        {
            try
            {
                Camp camp = null;
                if (includeSpeakers) camp = _repo.GetCampByMonikerWithSpeakers(moniker);
                else camp = _repo.GetCampByMoniker(moniker);
                if (camp == null) return NotFound($"Camp {moniker} was not found");
                return Ok(_mapper.Map<CampModel>(camp));
            }
            catch
            {
                return BadRequest();
            }
        }
        [EnableCors("Fluer")]
        [Authorize(Policy = "SuperUser")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]CampModel model)
        {
            try
            {
                //replaced by [ValidateModel]
                //if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("Creating a new Code Camp");
                var camp = _mapper.Map<Camp>(model);
                _repo.Add(camp);
                if (await _repo.SaveAllAsync())
                {
                    _logger.LogDebug("Creating URI");
                    var newUri = Url.Link("CampGet", new { moniker = camp.Moniker });
                    return Created(newUri, _mapper.Map<CampModel>(camp));
                }
                else { _logger.LogWarning("Could not save Camp to the database"); }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Threw exception while saving Camp from Post:{ex}");
            }
            return BadRequest();
        }
        [HttpPut("{moniker}")]
        [HttpPatch("{moniker}")]
        public async Task<IActionResult> Put(string moniker, [FromBody] CampModel model)
        {
            try
            {
                //replaced [ValidateModel]
                //if (!ModelState.IsValid) return BadRequest(ModelState);
                var oldCamp = _repo.GetCampByMoniker(moniker);
                if (oldCamp == null) return NotFound($"The Camp with the id: {moniker} has not been found in database");
                //Map model to the oldCamp
                _mapper.Map(model, oldCamp);
                if (await _repo.SaveAllAsync())
                {
                    return Ok(_mapper.Map<CampModel>(oldCamp));
                }

            }
            catch (Exception ex)
            {

            }
            return BadRequest("Couldn't update Camp");

        }
        [HttpDelete("{moniker}")]
        public async Task<IActionResult> Delete(string moniker)
        {
            try
            {
                var oldCamp = _repo.GetCampByMoniker(moniker);
                if (oldCamp == null) return NotFound($"Could not find Camp with ID od {moniker}");
                _repo.Delete(oldCamp);
                if (await _repo.SaveAllAsync())
                {
                    return Ok();
                }
            }
            catch (Exception ex) { }
            return BadRequest("Couldn not delete Camp");
        }
    }
}