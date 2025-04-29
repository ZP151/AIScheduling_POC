# Smart Scheduling System - Constraint System Documentation

## Introduction

The constraint system is the core of the Smart Scheduling System. It defines the rules that govern how courses, teachers, classrooms, and time slots can be assigned. This document provides a comprehensive reference of all constraints implemented in the system.

## Constraint Hierarchy

Constraints in the system are organized into a hierarchical structure:

1. **Level 1 - Core Hard Constraints**: Must be satisfied for a valid solution.
2. **Level 2 - Configurable Hard Constraints**: Can be configured as hard or soft.
3. **Level 3 - Physical Soft Constraints**: Deal with physical resources and preferences.
4. **Level 4 - Quality Soft Constraints**: Optimize for quality and convenience.

## Level 1: Core Hard Constraints

### 1. TeacherConflictConstraint

**Description**: Ensures that a teacher is not assigned to teach two different course sections at the same time.

**Implementation Details**:
- Checks all assignments for the same teacher
- Verifies that time slots don't overlap
- Returns score of 0.0 if any conflicts are found

**Configuration**: Not configurable (always hard)

**Example Conflict**:
```
Teacher "Dr. Smith" is assigned to teach "CS101-A" and "MATH200-B" on Monday 10:00-11:30.
```

### 2. ClassroomConflictConstraint

**Description**: Ensures that a classroom is not assigned to two different course sections at the same time.

**Implementation Details**:
- Checks all assignments for the same classroom
- Verifies that time slots don't overlap
- Returns score of 0.0 if any conflicts are found

**Configuration**: Not configurable (always hard)

**Example Conflict**:
```
Room "A101" is assigned to "CS101-A" and "MATH200-B" on Monday, 10:00-11:30.
```

### 3. TimeSlotConflictConstraint

**Description**: Ensures that a course section is not scheduled at multiple time slots.

**Implementation Details**:
- Validates that each course section appears exactly once in the solution
- Returns score of 0.0 if any course section is missing or appears multiple times

**Configuration**: Not configurable (always hard)

**Example Conflict**:
```
Course "CS101-A" is assigned to multiple time slots.
```

## Level 2: Configurable Hard Constraints

### 1. TeacherAvailabilityConstraint

**Description**: Ensures that teachers are only assigned to time slots when they are available.

**Implementation Details**:
- Checks each assignment against teacher availability data
- Calculates a score based on the percentage of assignments that respect availability
- Can be configured as hard or soft

**Configuration**:
```json
{
  "isHard": true,
  "weight": 1.0
}
```

**Example Conflict**:
```
Teacher "Dr. Smith" is assigned to teach "CS101-A" on Monday 10:00-11:30, but has marked this time as unavailable.
```

### 2. ClassroomCapacityConstraint

**Description**: Ensures that assigned classrooms have sufficient capacity for the course enrollment.

**Implementation Details**:
- Compares course enrollment with classroom capacity
- Can consider a small overflow (configurable)
- Returns conflicts for significant capacity mismatches

**Configuration**:
```json
{
  "isHard": true,
  "weight": 1.0,
  "allowedOverflowPercentage": 5
}
```

**Example Conflict**:
```
Course "CS101-A" with enrollment of 35 students is assigned to room "A101" with capacity 30.
```

### 3. TeacherQualificationConstraint

**Description**: Ensures that teachers are qualified to teach their assigned courses.

**Implementation Details**:
- Checks if the teacher has the required qualifications for the course
- Considers primary and secondary qualifications with different weights
- Returns conflicts for unqualified assignments

**Configuration**:
```json
{
  "isHard": true,
  "weight": 1.0,
  "allowUnqualifiedWithPenalty": false
}
```

**Example Conflict**:
```
Teacher "Dr. Smith" is assigned to teach "PHYS300" but is not qualified for this course.
```

## Level 3: Physical Soft Constraints

### 1. ResourceComplianceConstraint

**Description**: Ensures that classrooms have the necessary resources required by courses.

**Implementation Details**:
- Compares course resource requirements with classroom resources
- Calculates a score based on the percentage of requirements met
- Returns conflicts for missing critical resources

**Configuration**:
```json
{
  "isHard": false,
  "weight": 0.8,
  "criticalResourceTypes": ["Computers", "SpecializedEquipment"]
}
```

**Example Conflict**:
```
Course "CS101-A" requires computers, but room "B201" doesn't have computers.
```

### 2. EquipmentRequirementConstraint

**Description**: Ensures that classrooms have the specific equipment required by courses.

**Implementation Details**:
- Checks for specific equipment items needed by courses
- Supports partial matching with scoring
- Returns detailed conflicts for missing equipment

**Configuration**:
```json
{
  "isHard": false,
  "weight": 0.7,
  "requiredEquipmentWeight": 0.8,
  "preferredEquipmentWeight": 0.2
}
```

**Example Conflict**:
```
Course "PHYS300" requires "Oscilloscope" which is not available in room "A101".
```

### 3. ClassroomTypeMatchingConstraint

**Description**: Ensures that courses are assigned to appropriate classroom types based on the course's nature and requirements.

**Implementation Details**:
- Maps course types to preferred and alternative classroom types
- Calculates scores based on match quality (perfect, alternative, or mismatch)
- Supports strict matching for specific course types (e.g., laboratory courses)
- Considers special requirements like computer labs for programming courses

**Configuration**:
```json
{
  "isHard": false,
  "weight": 0.75,
  "courseTypeMapping": [
    {
      "courseType": "Programming",
      "preferredClassroomTypes": ["ComputerLab"],
      "alternativeClassroomTypes": ["MultimediaRoom"],
      "preferenceScore": 1.0
    },
    {
      "courseType": "Lecture",
      "preferredClassroomTypes": ["LectureHall", "ClassRoom"],
      "alternativeClassroomTypes": ["ConferenceRoom"],
      "preferenceScore": 0.8
    }
  ],
  "penaltyForMismatch": 0.7,
  "strictMatchingForTypes": ["Laboratory"]
}
```

**Example Conflict**:
```
Course "CS101-A" (Programming) is assigned to room "A101" (LectureHall) instead of a ComputerLab.
```

**Performance Considerations**:
- Uses cached lookup tables for course type and classroom type mappings
- Pre-computes compatibility matrices to optimize constraint evaluation
- Time complexity: O(n), where n is the number of course assignments

## Level 4: Quality Soft Constraints

### 1. TeacherPreferenceConstraint

**Description**: Attempts to honor teacher preferences for specific time slots and classrooms.

**Implementation Details**:
- Evaluates assignments against teacher preference data
- Calculates a weighted score based on preference satisfaction
- Returns conflicts for significant preference violations

**Configuration**:
```json
{
  "isHard": false,
  "weight": 0.6,
  "timeSlotPreferenceWeight": 0.7,
  "classroomPreferenceWeight": 0.3
}
```

**Example Conflict**:
```
Teacher "Dr. Smith" prefers not to teach on Friday afternoons, but is assigned to "CS101-A" on Friday 14:00-15:30.
```

### 2. TeacherWorkloadConstraint

**Description**: Ensures that teachers have balanced workloads and don't exceed maximum teaching hours.

**Implementation Details**:
- Calculates total teaching hours per teacher
- Compares to target and maximum workload values
- Returns conflicts for overloaded teachers

**Configuration**:
```json
{
  "isHard": false,
  "weight": 0.7,
  "targetWorkloadHoursPerWeek": 12,
  "maxWorkloadHoursPerWeek": 18,
  "minWorkloadHoursPerWeek": 6
}
```

**Example Conflict**:
```
Teacher "Dr. Smith" is assigned 20 teaching hours per week, exceeding the maximum of 18 hours.
```

### 3. ConsecutiveClassesConstraint

**Description**: Attempts to schedule classes in contiguous blocks without large gaps.

**Implementation Details**:
- Analyzes the schedule for each teacher to identify gaps
- Applies penalties for large gaps between classes on the same day
- Returns conflicts for problematic schedules

**Configuration**:
```json
{
  "isHard": false,
  "weight": 0.5,
  "maxPreferredGapMinutes": 60,
  "maxAllowedGapMinutes": 180
}
```

**Example Conflict**:
```
Teacher "Dr. Smith" has a 3-hour gap between morning and afternoon classes on Monday.
```

### 4. CourseDistributionConstraint

**Description**: Attempts to distribute courses evenly throughout the week.

**Implementation Details**:
- Analyzes the distribution of courses across days and time slots
- Applies penalties for imbalanced distributions
- Returns conflicts for heavily skewed schedules

**Configuration**:
```json
{
  "isHard": false,
  "weight": 0.4,
  "maxCoursesPerDay": 3,
  "preferredDistributionDeviation": 1.0
}
```

**Example Conflict**:
```
5 courses are scheduled on Monday, while only 1 course is scheduled on Wednesday.
```

### 5. BuildingProximityConstraint

**Description**: Attempts to schedule consecutive classes for the same teacher in nearby buildings.

**Implementation Details**:
- Calculates distances between buildings for consecutive time slots
- Applies penalties for excessive distances
- Returns conflicts for problematic transitions

**Configuration**:
```json
{
  "isHard": false,
  "weight": 0.5,
  "maxPreferredDistance": 100,
  "transitionTimeMinutes": 15
}
```

**Example Conflict**:
```
Teacher "Dr. Smith" has classes in "Building A" and "Building C" back-to-back with insufficient transition time.
```

## Adding Custom Constraints

### Implementation Steps

1. Create a new class that inherits from `BaseConstraint` and implements `IConstraint`:

```csharp
public class CustomConstraint : BaseConstraint, IConstraint
{
    public override int Id => 20; // Unique ID
    public override string Name => "Custom Constraint Name";
    public override string Description => "Description of constraint";
    public override bool IsHard => false; // Soft constraint by default
    public override ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level4_QualitySoft;
    
    public override (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
    {
        // Implementation logic
        var conflicts = new List<SchedulingConflict>();
        double score = 1.0;
        
        // Evaluate solution and generate conflicts if needed
        
        return (score, conflicts);
    }
}
```

2. Register the constraint in the dependency injection container:

```csharp
// In DependencyInjection.cs
services.AddSingleton<IConstraint, CustomConstraint>();
```

3. Configure the constraint through appsettings.json:

```json
"ConstraintSettings": {
  "CustomConstraint": {
    "IsActive": true,
    "Weight": 0.5,
    "CustomParameter1": "Value1",
    "CustomParameter2": 42
  }
}
```

## Constraint Configuration

### Global Configuration

The constraint system can be configured globally through appsettings.json:

```json
"ConstraintSystem": {
  "EnabledConstraintLevels": ["Level1_CoreHard", "Level2_ConfigurableHard", "Level3_PhysicalSoft"],
  "DefaultHardConstraintWeight": 1.0,
  "DefaultSoftConstraintWeight": 0.5,
  "ConflictThreshold": 0.8
}
```

### Individual Constraint Configuration

Each constraint can be configured individually:

```json
"ConstraintSettings": {
  "TeacherWorkloadConstraint": {
    "IsActive": true,
    "Weight": 0.8,
    "TargetWorkloadHoursPerWeek": 15,
    "MaxWorkloadHoursPerWeek": 20
  }
}
```

## Constraint Selection Guide

When enabling constraints for different scenarios, consider the following:

1. **Basic Scheduling**: Enable only Level 1 constraints for quick, valid schedules.

2. **Standard Scheduling**: Enable Level 1 and Level 2 constraints for realistic, valid schedules.

3. **Enhanced Scheduling**: Enable all constraint levels for high-quality, optimized schedules.

For performance-critical scenarios, consider disabling some of the more computationally expensive constraints like `BuildingProximityConstraint` or setting higher thresholds for soft constraints.

## Troubleshooting Constraints

If no valid solutions can be found:

1. Check for over-constrained problems by temporarily disabling Level 2 hard constraints.
2. Look for conflicting teacher availability vs. required courses.
3. Examine the conflict logs for specific constraint violations.
4. Consider reducing the weight of soft constraints or changing some hard constraints to soft.

Common issues:

- **Teacher conflicts**: Check for double-booked teachers
- **Classroom conflicts**: Check for double-booked rooms
- **Availability conflicts**: Ensure teachers have enough available time slots
- **Qualification conflicts**: Ensure enough qualified teachers for each course 