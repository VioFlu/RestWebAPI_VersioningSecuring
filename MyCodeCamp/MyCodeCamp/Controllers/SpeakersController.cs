using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using MyCodeCamp.Filters;
using MyCodeCamp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCodeCamp.Controllers
{

    [Route("/api/camps/{moniker}/speakers")]
    [ValidateModel]
    [ApiVersion("1.2")]
    [ApiVersion("1.1")]
    public class SpeakersController : BaseController
    {
        protected readonly ICampRepository _repository;
        protected readonly ILogger<SpeakersController> _logger;
        protected readonly IMapper _mapper;
        protected readonly UserManager<CampUser> _userManager;

        public SpeakersController(ICampRepository repository,
                            ILogger<SpeakersController> logger,
                            IMapper mapper,
                            UserManager<CampUser> userManager)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;

        }
        [HttpGet]
        [MapToApiVersion("1.2")]
        public IActionResult Getv12(string moniker, bool includeTalks = false)
        {
            var speakers = includeTalks ? _repository.GetSpeakersByMonikerWithTalks(moniker) : _repository.GetSpeakersByMoniker(moniker);
            return Ok(_mapper.Map<IEnumerable<SpeakerModel>>(speakers));
        }
        [HttpGet]
        [MapToApiVersion("1.1")]
        public virtual IActionResult GetWithCount(string moniker, bool includeTalks = false)
        {
            var speakers = includeTalks ? _repository.GetSpeakersByMonikerWithTalks(moniker) : _repository.GetSpeakersByMoniker(moniker);
            return Ok(new { count = speakers.Count(), results = _mapper.Map<IEnumerable<SpeakerModel>>(speakers) });
        }
        [HttpGet("{id}", Name = "SpeakerGet")]
        public IActionResult Get(string moniker, int id, bool includeTalks = false)
        {
            var speaker = includeTalks ? _repository.GetSpeakerWithTalks(id) : _repository.GetSpeaker(id);
            if (speaker == null) return NotFound();
            if (speaker.Camp.Moniker != moniker) return BadRequest("Speaker not in spoecified camp");
            return Ok(_mapper.Map<SpeakerModel>(speaker));
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post(string moniker, [FromBody]SpeakerModel model)
        {
            try
            {
                var camp = _repository.GetCampByMoniker(moniker);
                if (camp == null) return BadRequest("Could not find camp");
                var speaker = _mapper.Map<Speaker>(model);
                speaker.Camp = camp;
                var campUser = await _userManager.FindByNameAsync(this.User.Identity.Name);
                if (campUser != null)
                {
                    speaker.User = campUser;
                    _repository.Add(speaker);

                    if (await _repository.SaveAllAsync())
                    {
                        _logger.LogDebug("Creating URI");
                        var newUri = Url.Link("SpeakerGet", new { moniker = speaker.Camp.Moniker, id = speaker.Id });
                        return Created(newUri, _mapper.Map<SpeakerModel>(speaker));
                    }
                    else { _logger.LogWarning("Could not save Speaker to the database"); }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Threw exception while saving Speaker from Post:{ex}");
            }
            return BadRequest();
        }
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Put(string moniker,
                        int id,
                        [FromBody]SpeakerModel model)
        {
            try
            {
                var speaker = _repository.GetSpeaker(id);
                if (speaker == null) return NotFound();
                if (speaker.Camp.Moniker != moniker) return BadRequest("Speaker and Camp do not match");
                if (speaker.User.UserName != this.User.Identity.Name) return Forbid();
                _mapper.Map(model, speaker);

                if (await _repository.SaveAllAsync())
                {
                    return Ok(_mapper.Map<SpeakerModel>(speaker));
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Threw exception while saving Speaker from Put:{ex}");
            }
            return BadRequest();
        }
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string moniker, int id)
        {
            try
            {
                var speaker = _repository.GetSpeaker(id);
                if (speaker == null) return NotFound();
                if (speaker.Camp.Moniker != moniker) return BadRequest("Speaker and Camp do not match");

                _repository.Delete(speaker);
                if (await _repository.SaveAllAsync())
                {
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Threw exception while deleting Speaker:{ex}");
            }
            return BadRequest();
        }
    }
}