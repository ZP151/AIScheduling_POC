# Smart Scheduling System - Architecture Details

## 1. Detailed Architecture

### 1.1 Component Architecture

The Smart Scheduling System is built on a layered architecture with clear separation of concerns:

```
+----------------------------------------------+
|                User Interface                |
| +------------------+ +--------------------+  |
| |    React UI      | |   Swagger UI       |  |
| +------------------+ +--------------------+  |
+----------------------------------------------+
                       |
                       | HTTP/REST
                       |
+----------------------------------------------+
|                    API Layer                 |
| +------------------+ +--------------------+  |
| |  API Controllers | |   API Middleware   |  |
| +------------------+ +--------------------+  |
+----------------------------------------------+
                       |
                       | Service Interfaces
                       |
+----------------------------------------------+
|               Application Services           |
| +------------------+ +--------------------+  |
| |Scheduling Service| |Validation Services |  |
| +------------------+ +--------------------+  |
+----------------------------------------------+
                       |
                       | Domain Models
                       |
+----------------------------------------------+
|                 Domain Layer                 |
| +------------------+ +--------------------+  |
| |  Scheduling      | |    Constraints     |  |
| |  Engine          | |    Framework       |  |
| +------------------+ +--------------------+  |
| +------------------+ +--------------------+  |
| |  Optimization    | |    Evaluation      |  |
| |  Algorithms      | |    System          |  |
| +------------------+ +--------------------+  |
+----------------------------------------------+
                       |
                       | Repositories
                       |
+----------------------------------------------+
|              Infrastructure Layer            |
| +------------------+ +--------------------+  |
| |  Data Access     | |  External Services |  |
| +------------------+ +--------------------+  |
+----------------------------------------------+
```

### 1.2 Solution Structure

The solution is organized into the following projects:

- **SmartSchedulingSystem.API**: ASP.NET Core Web API project
- **SmartSchedulingSystem.Core**: Core domain models and interfaces
- **SmartSchedulingSystem.Scheduling**: Scheduling and optimization engine
- **SmartSchedulingSystem.Data**: Data access layer
- **SmartSchedulingSystem.Test**: Testing project

### 1.3 Dependency Flow

The dependencies flow in one direction, from outer to inner layers:

```
API → Application Services → Domain → Infrastructure
```

This ensures that the core domain logic remains isolated from external concerns.

## 2. Constraint System Architecture

### 2.1 Constraint Interface and Base Class

The constraint system is built on a common interface and base class:

```csharp
public interface IConstraint
{
    int Id { get; }
    string Name { get; }
    string Description { get; }
    bool IsHard { get; }
    bool IsActive { get; set; }
    double Weight { get; set; }
    ConstraintHierarchy Hierarchy { get; }
    string Category { get; }
    (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution);
}

public abstract class BaseConstraint
{
    public abstract int Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract bool IsHard { get; }
    public virtual bool IsActive { get; set; } = true;
    public virtual double Weight { get; set; } = 1.0;
    public abstract ConstraintHierarchy Hierarchy { get; }
    public abstract string Category { get; }
    
    public abstract (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution);
}
```

### 2.2 Constraint Hierarchy

The constraint hierarchy is defined as an enumeration:

```csharp
public enum ConstraintHierarchy
{
    Level1_CoreHard,
    Level2_ConfigurableHard,
    Level3_PhysicalSoft,
    Level4_QualitySoft
}
```

### 2.3 Constraint Manager Architecture

The `ConstraintManager` provides centralized constraint management:

```
+-------------------+
| ConstraintManager |
+-------------------+
        |
        | Registers and manages
        v
+-------------------+
|    Constraints    |
+-------------------+
    |           |
    |           |
    v           v
+-------+   +-------+
| Hard  |   | Soft  |
+-------+   +-------+
```

## 3. Algorithm Architecture

### 3.1 Constraint Programming (CP) Component

The CP component uses Google OR-Tools to find initial feasible solutions:

```
+------------------+
|   CP Scheduler   |
+------------------+
        |
        v
+------------------+
|  CP Model Builder|
+------------------+
        |
        v
+------------------+
|CP-SAT Solver     |
+------------------+
        |
        v
+------------------+
|Solution Converter|
+------------------+
```

### 3.2 Local Search (LS) Component

The LS component implements simulated annealing for optimization:

```
+------------------+
|Local Search      |
|Optimizer         |
+------------------+
        |
        +--------------------+
        |                    |
        v                    v
+------------------+ +-------------------+
|Move Generator    | |Simulated Annealing|
+------------------+ |Controller         |
                     +-------------------+
```

### 3.3 Hybrid CP-LS Architecture

The system combines both approaches:

```
+------------------+
|CP-LS Scheduler   |
+------------------+
        |
        +--------------------+
        |                    |
        v                    v
+------------------+ +-------------------+
|CP Scheduler      | |LS Optimizer       |
+------------------+ +-------------------+
```

## 4. Evaluation System Architecture

### 4.1 Evaluation Components

The evaluation system follows this architecture:

```
+------------------+
|Solution Evaluator|
+------------------+
        |
        +--------------------+
        |                    |
        v                    v
+------------------+ +-------------------+
|Hard Constraint   | |Soft Constraint    |
|Evaluator         | |Evaluator          |
+------------------+ +-------------------+
        |                    |
        +--------------------+
        |
        v
+------------------+
|Conflict Detector |
+------------------+
```

### 4.2 Conflict Management

The conflict management system architecture:

```
+------------------+
|Conflict Resolver |
+------------------+
        |
        +--------------------+--------+
        |                    |        |
        v                    v        v
+------------------+ +--------------+ +-------------+
|Teacher Conflict  | |Room Conflict | |Other Conflict|
|Handler           | |Handler       | |Handlers     |
+------------------+ +--------------+ +-------------+
```

## 5. Data Flow Architecture

### 5.1 Scheduling Process Flow

```
+---------------+    +-----------------+    +------------------+
| Input Data    | -> | Scheduling      | -> | Solution         |
| Collection    |    | Engine          |    | Generation       |
+---------------+    +-----------------+    +------------------+
                           |  ^
                           |  |
                           v  |
                     +-----------------+
                     | Constraint      |
                     | Evaluation      |
                     +-----------------+
```

### 5.2 Optimization Flow

```
+---------------+    +-----------------+    +------------------+    +---------------+
| Initial       | -> | Move            | -> | Solution         | -> | Solution      |
| Solution      |    | Generation      |    | Evaluation       |    | Selection     |
+---------------+    +-----------------+    +------------------+    +---------------+
                           ^                        |
                           |                        v
                           |                +-----------------+
                           +----------------| Temperature     |
                                            | Control         |
                                            +-----------------+
```

## 6. Extensibility Points in the future

The system provides several key extension points:

### 6.1 Custom Constraints

Developers can add new constraints by inheriting from `BaseConstraint`:

```csharp
public class CustomConstraint : BaseConstraint, IConstraint
{
    // Implementation details
}
```

### 6.2 Custom Optimization Algorithms

The system can be extended with new optimization algorithms:

```csharp
public class CustomOptimizer : IOptimizer
{
    // Implementation details
}
```

### 6.3 Custom Data Providers

Data providers can be customized by implementing the appropriate interfaces:

```csharp
public class CustomDataProvider : ISchedulingDataProvider
{
    // Implementation details
}
```