# Smart Scheduling System Constraint Architecture Design

## Overview

The constraint architecture of the smart scheduling system is a layered design aimed at flexibly addressing the requirements of various scheduling scenarios. This architecture includes the following main components:

1. **Constraint Hierarchy** (`ConstraintHierarchy`): Defines the importance and priority levels of constraints
2. **Basic Scheduling Rules** (`BasicSchedulingRules`): Defines common rules applicable to various scheduling scenarios
3. **Constraint Definitions** (`ConstraintDefinitions`): Defines specific constraint IDs and categories
4. **Constraint Manager** (`ConstraintManager`): Manages constraint states and application

## Constraint Hierarchy Structure

Constraints are divided into four levels based on importance and nature:

1. **Level1_CoreHard**: Basic scheduling rule hard constraints, such as resource conflicts, classroom capacity, and other basic limitations
2. **Level2_ConfigurableHard**: Configurable hard constraints, such as teacher availability, classroom availability, and other configurable limitations
3. **Level3_PhysicalSoft**: Physical limitation soft constraints, such as classroom type, equipment requirements, and other physical resource-related optimization goals
4. **Level4_QualitySoft**: Quality soft constraints, such as teacher preferences, schedule compactness, and other quality improvement-related optimization goals

## Component Relationships

### Relationship between Basic Scheduling Rules and Constraint Definitions

`BasicSchedulingRules` defines common, high-level scheduling rules that apply to various scheduling scenarios. `ConstraintDefinitions` defines more specific constraint implementations.

For example, the general rule `BasicSchedulingRules.ResourceConflict` corresponds to multiple specific constraints in `ConstraintDefinitions`:
- `TeacherConflict`: Teacher time slot conflict constraint
- `ClassroomConflict`: Classroom time slot conflict constraint
- `StudentConflict`: Student time slot conflict constraint

This design allows the system to understand and handle constraints at different levels:
- At a general level, we focus on the abstract rule "resources cannot conflict"
- In specific implementations, we define specialized constraints for each resource type (teachers, classrooms, students)

### Relationship between Constraint Definitions and Constraint Manager

`ConstraintDefinitions` defines constraint IDs and classifications, while `ConstraintManager` is responsible for:
- Maintaining the collection of constraint instances
- Managing constraint states (enabled/disabled)
- Setting constraint weights
- Providing methods to query constraints by level or type

`ConstraintManager` does not directly depend on `ConstraintDefinitions`, but rather establishes associations through constraint IDs. This loose coupling design makes the constraint system more flexible, allowing constraint types to be extended without modifying the manager.

## Constraint Application in Random Algorithms

The random algorithm (`CPScheduler.GenerateConstraintAwareRandomSolution`) considers constraints by level:

1. First attempts to satisfy basic scheduling rules (Level1_CoreHard)
2. If unsuccessful, tries to consider configurable hard constraints (Level2_ConfigurableHard)
3. If still unsuccessful, uses the minimal constraint set for forced assignment

This progressive strategy ensures acceptable solutions can be generated even under complex constraints.

## Constraint Application Scenarios

The system provides constraint sets for different scheduling scenarios:

1. **University Scheduling**: Focuses on prerequisite course relationships, teacher qualifications, and classroom type matching constraints
2. **K12 Scheduling**: Focuses on time continuity, course distribution balance, and minimum interval constraints
3. **Exam Scheduling**: Focuses on student conflict avoidance, exam intervals, and building capacity balance constraints

Each scenario can obtain recommended constraint sets through corresponding methods.

## Constraint Extension Methods

The system supports the following constraint extension methods:

1. **Adding New Constraint Definitions**: Add new constraint constants and corresponding groups in `ConstraintDefinitions`
2. **Implementing New Constraint Classes**: Create new classes implementing the `IConstraint` interface
3. **Registering New Constraints**: Register new constraints through the `ConstraintManager.RegisterConstraint` method

## Summary

This layered constraint architecture design provides the following advantages:

1. **Modularity**: Separation of constraint definitions and management, facilitating independent evolution
2. **Universality**: Abstract basic rules applicable to various scheduling scenarios
3. **Flexibility**: Ability to combine different constraints to meet specific scheduling requirements
4. **Extensibility**: Easy to add new constraints without modifying core logic
5. **Progressive Solution**: Can provide acceptable solutions even under complex constraints 