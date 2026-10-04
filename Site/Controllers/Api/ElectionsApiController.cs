using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Swashbuckle.Swagger.Annotations;
using TallyJ.Code.Data;
using TallyJ.EF;
using TallyJ.Models.DTOs;

namespace TallyJ.Controllers.Api
{
    /// <summary>
    /// Election management API endpoints
    /// </summary>
    [RoutePrefix("api/v1/elections")]
    public class ElectionsApiController : ApiController
    {
        private readonly IDbContextFactory _dbContextFactory;

        public ElectionsApiController(IDbContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Get all elections
        /// </summary>
        /// <returns>List of elections</returns>
        [HttpGet]
        [Route("")]
        [SwaggerOperation("GetAllElections")]
        [SwaggerResponse(200, "Success", typeof(IEnumerable<ElectionDto>))]
        [SwaggerResponse(500, "Internal server error")]
        public IHttpActionResult GetElections()
        {
            try
            {
                using (var db = _dbContextFactory.Create())
                {
                    var elections = db.Elections
                        .Select(e => new ElectionDto
                        {
                            ElectionGuid = e.ElectionGuid,
                            Name = e.Name,
                            Convenor = e.Convenor,
                            DateOfElection = e.DateOfElection,
                            ElectionType = e.ElectionType,
                            ElectionMode = e.ElectionMode,
                            NumberToElect = e.NumberToElect,
                            NumberExtra = e.NumberExtra,
                            TallyStatus = e.TallyStatus,
                            ShowFullReport = e.ShowFullReport
                        })
                        .ToList();

                    return Ok(elections);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Get a specific election by ID
        /// </summary>
        /// <param name="id">Election GUID</param>
        /// <returns>Election details</returns>
        [HttpGet]
        [Route("{id:guid}")]
        [SwaggerOperation("GetElectionById")]
        [SwaggerResponse(200, "Success", typeof(ElectionDto))]
        [SwaggerResponse(404, "Election not found")]
        [SwaggerResponse(500, "Internal server error")]
        public IHttpActionResult GetElection(Guid id)
        {
            try
            {
                using (var db = _dbContextFactory.Create())
                {
                    var election = db.Elections
                        .Where(e => e.ElectionGuid == id)
                        .Select(e => new ElectionDto
                        {
                            ElectionGuid = e.ElectionGuid,
                            Name = e.Name,
                            Convenor = e.Convenor,
                            DateOfElection = e.DateOfElection,
                            ElectionType = e.ElectionType,
                            ElectionMode = e.ElectionMode,
                            NumberToElect = e.NumberToElect,
                            NumberExtra = e.NumberExtra,
                            TallyStatus = e.TallyStatus,
                            ShowFullReport = e.ShowFullReport
                        })
                        .FirstOrDefault();

                    if (election == null)
                    {
                        return NotFound();
                    }

                    return Ok(election);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Create a new election
        /// </summary>
        /// <param name="request">Election creation details</param>
        /// <returns>Created election</returns>
        [HttpPost]
        [Route("")]
        [SwaggerOperation("CreateElection")]
        [SwaggerResponse(201, "Election created", typeof(ElectionDto))]
        [SwaggerResponse(400, "Invalid input")]
        [SwaggerResponse(500, "Internal server error")]
        public IHttpActionResult CreateElection([FromBody] CreateElectionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using (var db = _dbContextFactory.Create())
                {
                    var election = new Election
                    {
                        ElectionGuid = Guid.NewGuid(),
                        Name = request.Name,
                        Convenor = request.Convenor,
                        DateOfElection = request.DateOfElection,
                        ElectionType = request.ElectionType ?? "Normal",
                        ElectionMode = "InPerson",
                        NumberToElect = request.NumberToElect,
                        TallyStatus = "Setup",
                        ShowFullReport = false
                    };

                    db.Elections.Add(election);
                    db.SaveChanges();

                    var result = new ElectionDto
                    {
                        ElectionGuid = election.ElectionGuid,
                        Name = election.Name,
                        Convenor = election.Convenor,
                        DateOfElection = election.DateOfElection,
                        ElectionType = election.ElectionType,
                        ElectionMode = election.ElectionMode,
                        NumberToElect = election.NumberToElect,
                        NumberExtra = election.NumberExtra,
                        TallyStatus = election.TallyStatus,
                        ShowFullReport = election.ShowFullReport
                    };

                    return Created($"api/v1/elections/{election.ElectionGuid}", result);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Update an existing election
        /// </summary>
        /// <param name="id">Election GUID</param>
        /// <param name="request">Updated election details</param>
        /// <returns>Updated election</returns>
        [HttpPut]
        [Route("{id:guid}")]
        [SwaggerOperation("UpdateElection")]
        [SwaggerResponse(200, "Election updated", typeof(ElectionDto))]
        [SwaggerResponse(400, "Invalid input")]
        [SwaggerResponse(404, "Election not found")]
        [SwaggerResponse(500, "Internal server error")]
        public IHttpActionResult UpdateElection(Guid id, [FromBody] CreateElectionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using (var db = _dbContextFactory.Create())
                {
                    var election = db.Elections.FirstOrDefault(e => e.ElectionGuid == id);
                    if (election == null)
                    {
                        return NotFound();
                    }

                    election.Name = request.Name;
                    election.Convenor = request.Convenor;
                    election.DateOfElection = request.DateOfElection;
                    election.ElectionType = request.ElectionType ?? election.ElectionType;
                    election.NumberToElect = request.NumberToElect;

                    db.SaveChanges();

                    var result = new ElectionDto
                    {
                        ElectionGuid = election.ElectionGuid,
                        Name = election.Name,
                        Convenor = election.Convenor,
                        DateOfElection = election.DateOfElection,
                        ElectionType = election.ElectionType,
                        ElectionMode = election.ElectionMode,
                        NumberToElect = election.NumberToElect,
                        NumberExtra = election.NumberExtra,
                        TallyStatus = election.TallyStatus,
                        ShowFullReport = election.ShowFullReport
                    };

                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Delete an election
        /// </summary>
        /// <param name="id">Election GUID</param>
        /// <returns>Success status</returns>
        [HttpDelete]
        [Route("{id:guid}")]
        [SwaggerOperation("DeleteElection")]
        [SwaggerResponse(204, "Election deleted")]
        [SwaggerResponse(404, "Election not found")]
        [SwaggerResponse(500, "Internal server error")]
        public IHttpActionResult DeleteElection(Guid id)
        {
            try
            {
                using (var db = _dbContextFactory.Create())
                {
                    var election = db.Elections.FirstOrDefault(e => e.ElectionGuid == id);
                    if (election == null)
                    {
                        return NotFound();
                    }

                    db.Elections.Remove(election);
                    db.SaveChanges();

                    return StatusCode(System.Net.HttpStatusCode.NoContent);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}