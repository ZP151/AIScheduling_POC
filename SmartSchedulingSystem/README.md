# Smart Scheduling System Documentation

## 1. System Architecture Overview

### 1.1 System Architecture Diagram

The Smart Scheduling System follows a modern, layered architecture pattern:

```
+-----------------------+
|      Client Layer     |
|    (React Frontend)   |
+-----------+-----------+
            |
            | HTTP/REST
            |
+-----------v-----------+
|      API Layer        |
|  (ASP.NET Core APIs)  |
+-----------+-----------+
            |
            |
+-----------v-----------+
|   Application Layer   |
| (Scheduling Services) |
+-----------+-----------+
            |
            |
+-----------v-----------+
|     Domain Layer      |
| (Core Scheduling      |
|  Engine & Constraints)|
+-----------+-----------+
            |
            |
+-----------v-----------+
|    Persistence Layer  |
|   (Data Access)       |
+-----------+-----------+
```

### 1.2 Core Modules and Responsibilities

The system is composed of several key modules:

1. **SmartSchedulingSystem.API**: RESTful API endpoints for client communication
   - Schedule generation endpoints
   - Data input and result retrieval
   - Configuration management

2. **SmartSchedulingSystem.Core**: Core business logic
   - Domain models
   - Business rules
   - Service interfaces

3. **SmartSchedulingSystem.Scheduling**: The scheduling engine
   - Constraint definition and evaluation
   - Optimization algorithms (CP, LS, Hybrid)
   - Solution generation and validation

4. **SmartSchedulingSystem.Data**: Data persistence
   - Entity definitions
   - Repository implementations
   - Database context

### 1.3 Technology Stack

- **Backend**: .NET Core 9.0
- **Frontend**: React
- **Optimization Engine**: Custom implementation with:
  - Google OR-Tools for Constraint Programming
  - Custom implementation of Simulated Annealing
- **Dependency Injection**: Native ASP.NET Core DI
- **Logging**: Serilog with structured logging
- **Data Access**: Entity Framework Core
- **API Documentation**: Swagger/OpenAPI
- **Testing**: xUnit, Moq

### 1.4 Key Features

- Multi-level constraint system (hard and soft constraints)
- Hybrid optimization algorithm combining constraint programming and local search
- Conflict detection and resolution
- Multiple solution generation with quality assessment
- Extensible architecture for adding new constraints
- Real-time feedback and solution evaluation

## 2. Core Algorithms and Constraint Engine

### 2.1 Constraint Hierarchy System

The constraints are organized in a hierarchical structure:

1. **Level 1 - Core Hard Constraints**: 
   - Must be satisfied for a valid solution
   - Examples: TeacherConflictConstraint, ClassroomConflictConstraint

2. **Level 2 - Configurable Hard Constraints**:
   - Can be configured as hard or soft based on requirements
   - Examples: TeacherAvailabilityConstraint, ClassroomCapacityConstraint

3. **Level 3 - Physical Soft Constraints**:
   - Deal with physical resources and preferences
   - Examples: ResourceComplianceConstraint, EquipmentRequirementConstraint

4. **Level 4 - Quality Soft Constraints**:
   - Optimize for quality and convenience
   - Examples: TeacherPreferenceConstraint, TeacherWorkloadConstraint

### 2.2 Hybrid Optimization Algorithm

The system uses a two-phase approach:

1. **Constraint Programming (CP) Phase**:
   - Google OR-Tools CP-SAT solver
   - Efficiently finds feasible solutions satisfying hard constraints
   - Fast initial solution generation

2. **Local Search (LS) Phase**:
   - Simulated Annealing for optimization
   - Focuses on improving soft constraint satisfaction
   - Controlled by temperature cooling mechanism

3. **Integration Mechanism**:
   - CP solution feeds into LS as starting point
   - LS maintains hard constraint satisfaction
   - Multiple optimization runs with different parameters

### 2.3 Constraint Manager

The `ConstraintManager` class serves as the central coordinator:

- Registers and manages all constraint instances
- Determines constraint applicability based on level and context
- Handles constraint evaluation requests
- Calculates solution scores and identifies conflicts
- Provides extension points for custom constraints

Example constraint registration:

```csharp
// Registration in DependencyInjection.cs
services.AddSingleton<IConstraint, TeacherConflictConstraint>();
services.AddSingleton<IConstraint, ClassroomConflictConstraint>();
services.AddSingleton<IConstraint, ResourceComplianceConstraint>();
```

## 3. Data Models and State Management

### 3.1 Core Entities

The system operates with several primary entity types:

1. **SchedulingProblem**: The complete problem definition
   - Contains all input data needed for scheduling
   - Includes relationships between entities
   - Defines constraints and parameters

2. **CourseSectionInfo**: Course section to be scheduled
   - Course metadata (name, code, etc.)
   - Enrollment information
   - Resource requirements

3. **TeacherInfo**: Teacher resources
   - Qualifications and preferences
   - Availability constraints
   - Maximum workload settings

4. **ClassroomInfo**: Classroom resources
   - Capacity and location
   - Available equipment
   - Type and characteristics

5. **TimeSlotInfo**: Time slots for scheduling
   - Day of week and time range
   - Availability information
   - Time slot characteristics

### 3.2 Solution Representation

The scheduling solution is represented by:

1. **SchedulingSolution**: Complete solution representation
   - List of assignments
   - Solution metadata (algorithm, score, etc.)
   - Creation timestamp

2. **SchedulingAssignment**: Individual assignment
   - Links course section to teacher, classroom, and time slot
   - Contains assignment metadata
   - References to parent entities

3. **SchedulingEvaluation**: Solution quality assessment
   - Overall score
   - Detailed constraint satisfaction scores
   - Conflict information

4. **SchedulingConflict**: Constraint violation
   - Conflict type and severity
   - Description and root cause
   - Involved entities

### 3.3 Conflict Management

The conflict management system:

- Identifies constraint violations during evaluation
- Categorizes conflicts by type and severity
- Suggests potential resolutions
- Provides detailed information for user feedback

Example conflict detection:

```csharp
// From ResourceComplianceConstraint.cs
private SchedulingConflict CreateEquipmentMismatchConflict(
    SchedulingSolution solution, 
    CourseSectionInfo course, 
    ClassroomInfo classroom, 
    List<string> missingEquipment)
{
    return new SchedulingConflict
    {
        Id = solution.GetNextConflictId(),
        ConstraintId = this.Id,
        Type = SchedulingConflictType.EquipmentMismatch,
        Description = $"Course {course.CourseName} requires equipment: {string.Join(", ", missingEquipment)}, " +
                     $"which are not available in the assigned classroom",
        Severity = ConflictSeverity.Minor,
        Category = "Equipment Requirement Mismatch",
        InvolvedEntities = new Dictionary<string, List<int>>
        {
            { "Courses", new List<int> { course.Id } },
            { "Classrooms", new List<int> { classroom.Id } }
        }
    };
}
```

## 4. API Interface and Deployment Guide

### 4.1 API Endpoints

The system provides several REST API endpoints:

1. **Basic Schedule Generation**:
   - `POST /api/schedule/generate`
   - Generates schedules using basic constraints
   - Returns multiple solutions with scores

2. **Advanced Schedule Generation**:
   - `POST /api/schedule/generate-advanced`
   - Includes availability constraints and preferences
   - Higher quality solutions with more detailed evaluation

3. **Enhanced Schedule Generation**:
   - `POST /api/schedule/generate-enhanced`
   - Full constraint set including resource matching
   - Highest quality solutions with comprehensive evaluation

4. **Schedule Information**:
   - `GET /api/schedule/{id}`
   - Retrieves detailed information about a specific schedule
   - Includes assignments, scores, and conflicts

### 4.2 Request and Response Models

Request structure:

```json
{
  "semesterId": 1,
  "generateMultipleSolutions": true,
  "solutionCount": 3,
  "courseSectionObjects": [...],
  "teacherObjects": [...],
  "classroomObjects": [...],
  "timeSlotObjects": [...]
}
```

Response structure:

```json
{
  "solutions": [...],
  "schedules": [...],
  "generatedAt": "2023-12-01T12:00:00Z",
  "totalSolutions": 3,
  "bestScore": 0.92,
  "averageScore": 0.85,
  "primaryScheduleId": 1
}
```

### 4.3 Deployment Requirements

System requirements:

- **.NET Core 9.0 SDK** or later
- **Node.js 18+** for frontend development
- **SQL Server** (or compatible database)
- **50MB+ RAM** for medium-sized problems
- **2+ CPU cores** recommended for optimization

### 4.4 Deployment Steps

1. **Clone and Build**:
   ```
   git clone https://github.com/yourusername/SmartSchedulingSystem.git
   cd SmartSchedulingSystem
   dotnet build
   ```

2. **Database Setup**:
   ```
   dotnet ef database update --project SmartSchedulingSystem.Data
   ```

3. **API Deployment**:
   ```
   cd SmartSchedulingSystem.API
   dotnet publish -c Release -o ./publish
   ```

4. **Frontend Build**:
   ```
   cd SmartSchedulingSystem.API/scheduling-client
   npm install
   npm run build
   ```

5. **Running the Service**:
   ```
   cd SmartSchedulingSystem.API/publish
   dotnet SmartSchedulingSystem.API.dll
   ```

### 4.5 Configuration Options

The primary configuration options are available in `appsettings.json`:

```json
{
  "SchedulingEngine": {
    "EnableLocalSearch": true,
    "MaxLsIterations": 1000,
    "InitialTemperature": 100,
    "CoolingRate": 0.95,
    "UseStandardConstraints": true,
    "UseBasicConstraints": false,
    "UseEnhancedConstraints": true,
    "ResourceConstraintLevel": "Enhanced"
  }
}
```

## 5. Extending and Customizing the System

### 5.1 Adding New Constraints

1. **Create a new constraint class**:
   - Inherit from `BaseConstraint` and implement `IConstraint`
   - Define constraint properties (ID, name, etc.)
   - Implement `Evaluate` method

2. **Register the constraint**:
   - Add to the DI container in `DependencyInjection.cs`
   - Specify the appropriate constraint level

Example of a custom constraint:

```csharp
public class CustomDistributionConstraint : BaseConstraint, IConstraint
{
    public override int Id => 20;
    public override string Name => "Custom Distribution Constraint";
    public override string Description => "Ensures even distribution of courses";
    public override bool IsHard => false;
    public override ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level4_QualitySoft;
    
    public override (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
    {
        // Custom evaluation logic
        // Return a score and list of conflicts
    }
}

// Registration
services.AddSingleton<IConstraint, CustomDistributionConstraint>();
```

### 5.2 Performance Optimization

For large-scale problems:

1. **Constraint filtering**:
   - Enable only necessary constraints
   - Prioritize constraints based on importance

2. **Parameter tuning**:
   - Adjust simulated annealing parameters
   - Optimize cooling rate for best performance

3. **Parallel processing**:
   - Enable multiple solution generation in parallel
   - Distribute constraint evaluation across cores

### 5.3 Integration with Existing Systems

The system can be integrated with existing applications:

1. **API integration**:
   - Consume the REST APIs from any client
   - Pass existing data via the API endpoints

2. **Service integration**:
   - Reference the scheduling libraries directly
   - Use the scheduling engine as a service

3. **Data integration**:
   - Import/export data via provided models
   - Customize data connectors for specific sources

## 6. Troubleshooting

### 6.1 Common Issues

1. **Dependency Injection Errors**:
   - Check service registration in `DependencyInjection.cs`
   - Verify constructor parameters match registered services
   - Add explicit factory methods for complex dependencies

2. **No Feasible Solutions**:
   - Check for over-constrained problems
   - Review hard constraint definitions
   - Enable debug logging for constraint evaluation

3. **Performance Issues**:
   - Reduce problem size or constraint count
   - Optimize parameter settings
   - Use profiling to identify bottlenecks

### 6.2 Logging and Diagnostics

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartSchedulingSystem.Scheduling": "Debug"
    }
  }
}
```

Review logs for constraint evaluation:

```
info: SmartSchedulingSystem.Scheduling.Engine.ConstraintManager[0]
      Evaluating constraint 'Teacher Conflict Constraint' (Level1_CoreHard)
debug: SmartSchedulingSystem.Scheduling.Constraints.Level1_CoreHard.TeacherConflictConstraint[0]
      Found 2 conflicts, final score: 0.00
```

## 7. Conclusion

The Smart Scheduling System provides a powerful, extensible platform for educational timetabling. Its modular architecture, constraint-based approach, and hybrid optimization algorithms make it suitable for a wide range of scheduling problems.

By understanding the core architecture, constraint system, and extension points, developers can adapt and extend the system to meet specific institutional requirements while maintaining performance and solution quality.

For additional information or support, please contact Zhou.Ping@totaoebizsolutions.com 