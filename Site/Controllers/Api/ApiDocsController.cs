using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Mvc;

namespace TallyJ.Controllers.Api
{
    /// <summary>
    /// API documentation and schema endpoints
    /// </summary>
    [RoutePrefix("api/v1/docs")]
    public class ApiDocsController : ApiController
    {
        /// <summary>
        /// Get OpenAPI specification
        /// </summary>
        /// <returns>OpenAPI JSON schema</returns>
        [System.Web.Http.HttpGet]
        [Route("openapi.json")]
        public IHttpActionResult GetOpenApiSpec()
        {
            var openApiSpec = new
            {
                openapi = "3.0.0",
                info = new
                {
                    title = "TallyJ API",
                    version = "1.0.0",
                    description = "TallyJ Election Management System REST API",
                    contact = new
                    {
                        name = "TallyJ Team",
                        url = "https://github.com/glittle/TallyJ-3.0",
                        email = "support@tallyj.com"
                    },
                    license = new
                    {
                        name = "Apache 2.0",
                        url = "http://www.apache.org/licenses/LICENSE-2.0.html"
                    }
                },
                servers = new[]
                {
                    new { url = "/api/v1", description = "TallyJ API v1" }
                },
                paths = new
                {
                    // Elections endpoints
                    "/elections" = new
                    {
                        get = new
                        {
                            tags = new[] { "Elections" },
                            summary = "Get all elections",
                            description = "Retrieve a list of all elections",
                            responses = new
                            {
                                _200 = new
                                {
                                    description = "Success",
                                    content = new
                                    {
                                        applicationjson = new
                                        {
                                            schema = new
                                            {
                                                type = "array",
                                                items = new { _ref = "#/components/schemas/ElectionDto" }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        post = new
                        {
                            tags = new[] { "Elections" },
                            summary = "Create a new election",
                            description = "Create a new election with the provided details",
                            requestBody = new
                            {
                                required = true,
                                content = new
                                {
                                    applicationjson = new
                                    {
                                        schema = new { _ref = "#/components/schemas/CreateElectionRequest" }
                                    }
                                }
                            },
                            responses = new
                            {
                                _201 = new
                                {
                                    description = "Election created",
                                    content = new
                                    {
                                        applicationjson = new
                                        {
                                            schema = new { _ref = "#/components/schemas/ElectionDto" }
                                        }
                                    }
                                },
                                _400 = new { description = "Invalid input" }
                            }
                        }
                    },
                    "/elections/{id}" = new
                    {
                        get = new
                        {
                            tags = new[] { "Elections" },
                            summary = "Get election by ID",
                            description = "Retrieve a specific election by its GUID",
                            parameters = new[]
                            {
                                new
                                {
                                    name = "id",
                                    _in = "path",
                                    required = true,
                                    schema = new { type = "string", format = "uuid" },
                                    description = "Election GUID"
                                }
                            },
                            responses = new
                            {
                                _200 = new
                                {
                                    description = "Success",
                                    content = new
                                    {
                                        applicationjson = new
                                        {
                                            schema = new { _ref = "#/components/schemas/ElectionDto" }
                                        }
                                    }
                                },
                                _404 = new { description = "Election not found" }
                            }
                        },
                        put = new
                        {
                            tags = new[] { "Elections" },
                            summary = "Update election",
                            description = "Update an existing election",
                            parameters = new[]
                            {
                                new
                                {
                                    name = "id",
                                    _in = "path",
                                    required = true,
                                    schema = new { type = "string", format = "uuid" }
                                }
                            },
                            requestBody = new
                            {
                                required = true,
                                content = new
                                {
                                    applicationjson = new
                                    {
                                        schema = new { _ref = "#/components/schemas/CreateElectionRequest" }
                                    }
                                }
                            },
                            responses = new
                            {
                                _200 = new
                                {
                                    description = "Election updated",
                                    content = new
                                    {
                                        applicationjson = new
                                        {
                                            schema = new { _ref = "#/components/schemas/ElectionDto" }
                                        }
                                    }
                                },
                                _404 = new { description = "Election not found" }
                            }
                        },
                        delete = new
                        {
                            tags = new[] { "Elections" },
                            summary = "Delete election",
                            description = "Delete an existing election",
                            parameters = new[]
                            {
                                new
                                {
                                    name = "id",
                                    _in = "path",
                                    required = true,
                                    schema = new { type = "string", format = "uuid" }
                                }
                            },
                            responses = new
                            {
                                _204 = new { description = "Election deleted" },
                                _404 = new { description = "Election not found" }
                            }
                        }
                    }
                },
                components = new
                {
                    schemas = new
                    {
                        ElectionDto = new
                        {
                            type = "object",
                            properties = new
                            {
                                electionGuid = new { type = "string", format = "uuid", description = "Unique identifier for the election" },
                                name = new { type = "string", description = "Name of the election" },
                                convenor = new { type = "string", description = "Name of the election convenor" },
                                dateOfElection = new { type = "string", format = "date-time", description = "Date when the election is scheduled" },
                                electionType = new { type = "string", description = "Type of election" },
                                electionMode = new { type = "string", description = "Election mode" },
                                numberToElect = new { type = "integer", description = "Number of positions to elect" },
                                numberExtra = new { type = "integer", description = "Number of extra positions" },
                                tallyStatus = new { type = "string", description = "Current tally status" },
                                showFullReport = new { type = "boolean", description = "Whether to show full report" }
                            }
                        },
                        CreateElectionRequest = new
                        {
                            type = "object",
                            required = new[] { "name" },
                            properties = new
                            {
                                name = new { type = "string", maxLength = 200, description = "Name of the election" },
                                convenor = new { type = "string", maxLength = 100, description = "Name of the election convenor" },
                                dateOfElection = new { type = "string", format = "date-time", description = "Date when the election is scheduled" },
                                electionType = new { type = "string", maxLength = 50, description = "Type of election" },
                                numberToElect = new { type = "integer", description = "Number of positions to elect" }
                            }
                        }
                    }
                }
            };

            return Ok(openApiSpec);
        }

        /// <summary>
        /// Get available API endpoints
        /// </summary>
        /// <returns>List of available endpoints</returns>
        [System.Web.Http.HttpGet]
        [Route("endpoints")]
        public IHttpActionResult GetEndpoints()
        {
            var endpoints = new
            {
                version = "1.0.0",
                baseUrl = "/api/v1",
                documentation = "/api/v1/docs/openapi.json",
                endpoints = new[]
                {
                    new { method = "GET", path = "/elections", description = "Get all elections" },
                    new { method = "POST", path = "/elections", description = "Create a new election" },
                    new { method = "GET", path = "/elections/{id}", description = "Get election by ID" },
                    new { method = "PUT", path = "/elections/{id}", description = "Update election" },
                    new { method = "DELETE", path = "/elections/{id}", description = "Delete election" },
                    new { method = "GET", path = "/docs/openapi.json", description = "Get OpenAPI specification" },
                    new { method = "GET", path = "/docs/endpoints", description = "Get available endpoints" }
                }
            };

            return Ok(endpoints);
        }
    }
}