# TallyJ API Documentation

## Overview

The TallyJ Election Management System provides a RESTful API for managing elections, voters, ballots, and election results. This API enables decoupled frontend applications to interact with the TallyJ backend services.

## Domain Model

### Core Entities

#### Election
The central entity representing an election event.

**Properties:**
- `ElectionGuid` (Guid): Unique identifier
- `Name` (string): Election name
- `Convenor` (string): Election convenor name
- `DateOfElection` (DateTime?): Scheduled election date
- `ElectionType` (string): Type of election (Normal, ByElection, etc.)
- `ElectionMode` (string): Mode (InPerson, Online, Hybrid)
- `NumberToElect` (int?): Number of positions to fill
- `NumberExtra` (int?): Extra positions available
- `TallyStatus` (string): Current election status
- `ShowFullReport` (bool?): Whether to display full results

#### Person
Represents eligible voters and candidates in an election.

**Properties:**
- `PersonGuid` (Guid): Unique identifier
- `ElectionGuid` (Guid): Associated election
- `FirstName` (string): First name
- `LastName` (string): Last name
- `OtherNames` (string): Additional names
- `OtherLastNames` (string): Additional last names
- `OtherInfo` (string): Additional information
- `Area` (string): Geographic area
- `BahaiId` (string): Bahai community ID
- `AgeGroup` (string): Age classification
- `CanVote` (bool?): Voting eligibility
- `CanReceiveVotes` (bool?): Candidacy eligibility

#### Ballot
Represents individual ballots cast in an election.

**Properties:**
- `BallotGuid` (Guid): Unique identifier
- `ElectionGuid` (Guid): Associated election
- `PersonGuid` (Guid): Voter identifier
- `StatusCode` (string): Ballot status
- `ComputerCode` (string): Computer/location code

#### Vote
Individual vote selections within a ballot.

**Properties:**
- `VoteGuid` (Guid): Unique identifier
- `BallotGuid` (Guid): Associated ballot
- `PersonGuid` (Guid): Voted for person
- `VoteStatusCode` (string): Vote status

## Entity Relationships

```
Election (1) ──── (many) Person
Election (1) ──── (many) Ballot  
Election (1) ──── (many) Location
Election (1) ──── (many) Teller

Person (1) ──── (many) Vote (as candidate)
Person (1) ──── (many) Ballot (as voter)

Ballot (1) ──── (many) Vote
Location (1) ──── (many) Ballot
```

## API Endpoints

### Base URL
All API endpoints are prefixed with `/api/v1`

### Elections API

#### GET /elections
Returns a list of all elections.

**Response:** `200 OK`
```json
[
  {
    "electionGuid": "uuid",
    "name": "string",
    "convenor": "string", 
    "dateOfElection": "2024-01-01T00:00:00Z",
    "electionType": "Normal",
    "electionMode": "InPerson",
    "numberToElect": 9,
    "numberExtra": 2,
    "tallyStatus": "Setup",
    "showFullReport": false
  }
]
```

#### POST /elections
Creates a new election.

**Request Body:**
```json
{
  "name": "string (required)",
  "convenor": "string",
  "dateOfElection": "2024-01-01T00:00:00Z",
  "electionType": "Normal", 
  "numberToElect": 9
}
```

**Response:** `201 Created`

#### GET /elections/{id}
Returns a specific election by GUID.

**Parameters:**
- `id` (path): Election GUID

**Response:** `200 OK` or `404 Not Found`

#### PUT /elections/{id}
Updates an existing election.

**Parameters:**
- `id` (path): Election GUID

**Request Body:** Same as POST /elections

**Response:** `200 OK` or `404 Not Found`

#### DELETE /elections/{id}
Deletes an election.

**Parameters:**
- `id` (path): Election GUID

**Response:** `204 No Content` or `404 Not Found`

### Documentation Endpoints

#### GET /docs/openapi.json
Returns the OpenAPI 3.0 specification for the API.

#### GET /docs/endpoints
Returns a summary of available API endpoints.

## Authentication

The API currently inherits authentication from the main TallyJ application. Future versions will include:
- JWT token-based authentication
- Role-based access control
- API key authentication for external integrations

## Error Responses

All endpoints return standard HTTP status codes:

- `200 OK`: Successful GET/PUT request
- `201 Created`: Successful POST request
- `204 No Content`: Successful DELETE request
- `400 Bad Request`: Invalid input data
- `401 Unauthorized`: Authentication required
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

Error responses include a JSON body with details:
```json
{
  "error": "string",
  "message": "string",
  "details": {}
}
```

## Data Formats

- All dates are in ISO 8601 format (UTC)
- All GUIDs are in standard UUID format
- All string fields support UTF-8 encoding
- Request/response bodies use JSON format
- Content-Type: application/json

## Rate Limiting

Currently no rate limiting is implemented. Future versions will include:
- Per-user rate limits
- Per-IP rate limits  
- Configurable throttling policies

## API Versioning

The API uses path-based versioning (`/api/v1/`). Future versions will maintain backward compatibility where possible.

## SDK and Integration

Future development will include:
- JavaScript/TypeScript SDK
- .NET SDK
- Python SDK
- React/Vue component libraries

## Interactive Documentation

Access the interactive API explorer at `/ApiDocs` to test endpoints and view detailed schemas.