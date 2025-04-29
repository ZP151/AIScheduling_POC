# Smart Scheduling System - API Documentation

## Overview

Smart Scheduling System API provides interfaces for generating and managing course schedules. This document includes API descriptions, debugging/deployment information, and implementation status.

## Service URLs

### Development Environment
```
http://localhost:5000/api
```

### Testing Environment
```
http://test-server:5000/api
```

### Production Environment
```
https://schedule.yourdomain.com/api
```

> **Note**: In development mode, you can access the Swagger UI for API documentation and testing: `http://localhost:5000/swagger`

## Authentication (Not Implemented)

API requests will require JWT token authentication in the future.

```
POST /auth/login
```

Basic request/response format:
```json
// Request
{
  "username": "admin",
  "password": "your_password"
}

// Response
{
  "token": "eyJhbGci...",
  "expiresIn": 3600
}
```

Add to request header:
```
Authorization: Bearer eyJhbGci...
```

> **Note**: JWT authentication is planned for v1.5 implementation.

## API Endpoints

### 1. Schedule Generation (Implemented)

#### 1.1 Basic Schedule Generation
```
POST /schedule/generate
```

#### 1.2 Advanced Schedule Generation
```
POST /schedule/generate-advanced
```

#### 1.3 Enhanced Schedule Generation
```
POST /schedule/generate-enhanced
```

> **All schedule generation APIs use the same request format**, including semester ID, course information, teacher information, classroom information, and time slot information.
> Responses contain the generated scheduling solutions with different fields depending on the constraint level used.

### 2. Schedule Management

#### 2.1 Get Schedule by ID (Implemented)
```
GET /schedule/{id}
```

#### 2.2 List Schedules (Implemented)
```
GET /schedule?semesterId={semesterId}
```

#### 2.3 Publish Schedule (Implemented)
```
POST /schedule/{id}/publish
```

#### 2.4 Delete Schedule (Implemented)
```
DELETE /schedule/{id}
```

#### 2.5 Export Schedule (Not Implemented)
```
GET /schedule/{id}/export?format={format}
```
> **Note**: This feature is planned for v2.0 implementation, supporting Excel and PDF formats.

### 3. Configuration Management

#### 3.1 Get Algorithm Configuration (Implemented)
```
GET /config/algorithm
```

#### 3.2 Update Algorithm Configuration (Implemented)
```
PUT /config/algorithm
```

#### 3.3 Reset Algorithm Configuration (Not Implemented)
```
POST /config/algorithm/reset
```
> **Note**: Planned for v1.5 implementation.

### 4. Constraint Management

#### 4.1 Get Available Constraints (Implemented)
```
GET /constraints
```

#### 4.2 Update Constraint Configuration (Implemented)
```
PUT /constraints/{id}/config
```

#### 4.3 Classroom Type Matching Configuration (Implemented)
```
PUT /constraints/classroom-type-matching/config
```

#### 4.4 Disable/Enable Constraint (Partially Implemented)
```
PATCH /constraints/{id}/status
```
```json
// Request
{
  "isActive": true
}
```
> **Note**: Currently only supports quick enabling/disabling of some constraints, full support planned for v1.5 implementation.

#### 4.5 Custom Constraint Creation (Not Implemented)
```
POST /constraints/custom
```
> **Note**: Planned for v3.0 implementation.

### 5. Data Management

#### 5.1 Import Teacher Data (Not Implemented)
```
POST /data/teachers/import
```
> **Note**: Planned for v2.0 implementation.

#### 5.2 Import Course Data (Not Implemented)
```
POST /data/courses/import
```
> **Note**: Planned for v2.0 implementation.

#### 5.3 Import Classroom Data (Not Implemented)
```
POST /data/classrooms/import
```
> **Note**: Planned for v2.0 implementation.

## Error Handling

All API endpoints use standard HTTP status codes:

- `200`: Success
- `400`: Bad Request
- `401`: Unauthorized
- `403`: Forbidden
- `404`: Resource Not Found
- `500`: Server Error

Error response format:
```json
{
  "status": 400,
  "message": "Error message",
  "details": "Detailed explanation",
  "errorCode": "ERROR_CODE"
}
```

## Pagination

Endpoints supporting pagination accept these parameters:
```
page: Page number (default 1)
pageSize: Items per page (default 20)
sort: Field to sort by
order: Sort order (asc/desc)
```

## Setup and Port Configuration

### Development Environment Setup

When setting up the project for development after cloning the repository, the system is configured to use the following ports:

1. **Frontend (React)**: http://localhost:3001
   - Located in the `/SmartSchedulingSystem.API/scheuduling-client` directory
   - Start with `npm start` from the scheuduling-client directory

2. **.NET Backend API**: http://localhost:5192
   - Located in the root directory `/SmartSchedulingSystem.API`
   - Start with `dotnet run` from the root directory
   - Swagger available at http://localhost:5192/swagger

3. **Python Service (FastAPI)**: http://localhost:8080
   - Located in the `/PythonService` directory
   - Start with `uvicorn main:app --reload --port 8080` from the PythonService directory
   - OpenAPI documentation available at http://localhost:8080/docs

### Production Deployment

When deploying to production using the `run-and-build` scripts, the ports are configured differently:

1. **Frontend**: http://localhost:3000
   - Built and served by the .NET backend in production mode

2. **.NET Backend API**: http://localhost:5001
   - Acts as the main entry point for all services
   - Handles authentication, API requests, and serves the frontend
   - Communicates with the Python service internally

3. **Python Service**: Not directly exposed
   - Deployed as an internal service
   - Communication managed by the .NET backend

### Deployment Scripts

```bash
# Deploy everything (from project root)
./run-and-build.sh

# Deploy only the backend (from project root)
./run-and-build.sh --backend-only

# Deploy only the frontend (from project root)
./run-and-build.sh --frontend-only
```

For Windows environments, use the equivalent `.bat` scripts:

```
run-and-build.bat [--backend-only|--frontend-only]
```

### .NET Backend Configuration

The .NET backend is configured to:
1. Serve the API endpoints at the specified port
2. Serve the built React app (in production)
3. Proxy API requests to the Python service when needed
4. Handle authentication and authorization

### Cross-Origin Resource Sharing (CORS)

In development mode, CORS is configured to allow:
- Frontend (React): http://localhost:3001
- Python Service: http://localhost:8080

In production, CORS settings are determined by the `SCHEDULING_CORS_ORIGINS` environment variable.

## Implementation Status Summary

| Feature | Status | Planned Version |
|---------|--------|-----------------|
| Basic Schedule Generation | ✅ Implemented | v1.0 |
| Advanced Schedule Generation | ✅ Implemented | v1.0 |
| Enhanced Schedule Generation | ✅ Implemented | v1.0 |
| Complete Schedule Generation | ❌ Not Implemented  | v1.0 |
| Schedule Management | ✅ Implemented | v1.0 |
| Schedule Export | ❌ Not Implemented | v2.0 |
| Algorithm Configuration | ✅ Implemented | v1.0 |
| Configuration Reset | ❌ Not Implemented | v1.5 |
| JWT Authentication | ❌ Not Implemented | v1.5 |
| Constraint Management | ✅ Implemented | v1.0 |
| Classroom Type Matching | ✅ Implemented | v1.0 |
| Quick Constraint Enable/Disable | ⚠️ Partially Implemented | v1.5 |
| Custom Constraints | ❌ Not Implemented | v3.0 |
| Data Import | ❌ Not Implemented | v2.0 |

## Deployment Instructions

### IIS Deployment
1. Publish application to appropriate IIS directory
2. Configure application pool (.NET 6.0)
3. Set binding information (domain, port, SSL certificate)
4. Ensure ConnectionStrings are correctly configured in appsettings.json

### Docker Deployment (Not Implemented)
```bash
# Planned for v2.0 support
docker build -t smartscheduling:latest .
docker run -p 5000:80 smartscheduling:latest
```

### Environment Variables

The following environment variables can be used to override default configurations:

- `SCHEDULING_CONNECTION_STRING`: Database connection string
- `SCHEDULING_JWT_SECRET`: JWT secret key (for future use)
- `SCHEDULING_CORS_ORIGINS`: Allowed CORS origins (comma-separated)
- `SCHEDULING_LOG_LEVEL`: Log level (Debug, Info, Warning, Error)

## Common Issues

1. **API returns 401 error**
   - This may be due to authentication not being fully implemented yet
   - Check if the endpoint requires authentication

2. **How long does schedule generation take?**
   - Basic generation: ~5-10 seconds
   - Advanced generation: ~15-30 seconds
   - Enhanced generation: ~30-90 seconds (depends on constraint number and data scale)

3. **How to handle performance issues?**
   - Reduce number of enabled constraints
   - Adjust algorithm parameters (reduce iteration count)
   - Process data in batches
