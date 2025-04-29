import React, { useState,useEffect  } from 'react';
import { Container, Typography, Paper, Box, Tabs, Tab } from '@mui/material';
import ScheduleCoursesForm from './UI/MainModules/ScheduleCoursesForm';
import ScheduleExamsForm from './UI/MainModules/ScheduleExamsForm';
import ScheduleResults from './UI/MainModules/ScheduleResults';
import DataManagement from './UI/MainModules/DataManagement';
import ScheduleHistory from './UI/MainModules/ScheduleHistory';
import './styles.css';
import IntelligentAssistant from './UI/AIFeatures/IntelligentAssistant';
// Import example scheduling data
import { mockScheduleResults } from './Services/mockData';

function App() {
  const [tabValue, setTabValue] = useState(0);
  const [showResults, setShowResults] = useState(false);
  const [currentScheduleId, setCurrentScheduleId] = useState(null);
  
  // Convert mockScheduleResults to array with isExample flag
  const getExampleSchedules = () => {
    return mockScheduleResults.map(schedule => {
      // Add default status
      const status = schedule.status || 'Generated';
      
      // Ensure status history exists
      let statusHistory = schedule.statusHistory || [];
      if (statusHistory.length === 0) {
        statusHistory = [{
          status: status,
          timestamp: schedule.createdAt || new Date().toISOString(),
          userId: 'System'
        }];
      }
      
      // Add flag indicating this is example data
      return {
        ...schedule,
        status,
        statusHistory,
        isExample: true
      };
    });
  };
  
  // Save reference to last generated scheduling data
  const [lastGeneratedSchedules, setLastGeneratedSchedules] = useState([]);
  
  // Initialize state with example data only
  const [scheduleResults, setScheduleResults] = useState(() => getExampleSchedules());
  
  const [activeScheduleResultId, setActiveScheduleResultId] = useState(null);

  // Add shared parameter state management in App.jsx:

  // Add system parameters state
  const [systemParameters, setSystemParameters] = useState({
    // Default parameters
    facultyWorkloadBalance: 0.8,
    studentScheduleCompactness: 0.7,
    classroomTypeMatchingWeight: 0.7,
    minimumTravelTime: 30,
    maximumConsecutiveClasses: 3,
    campusTravelTimeWeight: 0.6,
    preferredClassroomProximity: 0.5,
    genderSegregation: true,
    enableRamadanSchedule: false,
    allowCrossListedCourses: true,
    enableMultiCampusConstraints: true,
    generateMultipleSolutions: true,
    solutionCount: 3,
    holidayExclusions: true,
    allowCrossSchoolEnrollment: true,
    allowCrossDepartmentTeaching: true,
    prioritizeHomeBuildings: true,
  });

  // Add active preset tracking
  const [activePresetId, setActivePresetId] = useState(1); // Default preset ID
  const [presets, setPresets] = useState([
    { id: 1, name: "Default Parameters", description: "System default configuration" },
    { id: 2, name: "Optimized for Engineering", description: "Prioritizes lab availability" },
    { id: 3, name: "Space Optimization", description: "Maximizes classroom utilization" },
    { id: 4, name: "Faculty Preference", description: "Prioritizes faculty scheduling preferences" }
  ]);
  const handleTabChange = (event, newValue) => {
    console.log(`Tab changed from ${tabValue} to ${newValue}`);
    
    // For history tab, use dedicated handler
    if (newValue === 3) {
      console.log("Navigating to History tab via handler");
      // Force navigation through navigateToScheduleHistory function
      navigateToScheduleHistory();
      return;
    }
    
    // For other tabs, switch directly
    setTabValue(newValue);
  };
  
  // Handle parameter updates
  const handleParametersUpdate = (updatedParameters) => {
    setSystemParameters(updatedParameters);
  };
  
  // Handle preset selection
  const handlePresetSelect = (presetId) => {
    setActivePresetId(presetId);
    // In a real app, this would load parameters from backend
    // For now, we just simulate with some example parameter changes
    if (presetId === 1) {
      setSystemParameters({
        ...systemParameters,
        facultyWorkloadBalance: 0.5,
        studentScheduleCompactness: 0.5,
        classroomTypeMatchingWeight: 0.5,
        // Other default parameters...
      });
    } else if (presetId === 2) {
      setSystemParameters({
        ...systemParameters,
        facultyWorkloadBalance: 0.6,
        studentScheduleCompactness: 0.7,
        classroomTypeMatchingWeight: 0.9,
        // Other engineering-specific parameters...
      });
    }
    // Handle other presets...
  };
 
  // Completely rewrite handleScheduleGenerated function
  const handleScheduleGenerated = (result) => {
    // Ensure schedules is an array
    const validSchedules = Array.isArray(result.schedules) ? result.schedules : [];
    
    // Ensure each newly generated solution has correct status and status history
    const schedulesWithState = validSchedules.map(schedule => {
      // If solution has no status, set to "Generated"
      const newSchedule = {
        ...schedule,
        status: schedule.status || "Generated",
        // Add timestamp as createdAt
        createdAt: schedule.createdAt || new Date().toISOString(),
        // Add batch requestId for grouping
        requestId: `${new Date().toISOString().split('T')[0]}`
      };
      
      // Ensure each scheduling solution has status history
      if (!newSchedule.statusHistory || newSchedule.statusHistory.length === 0) {
        newSchedule.statusHistory = [{
          status: newSchedule.status,
          timestamp: newSchedule.createdAt,
          userId: "System"
        }];
      }
      
      // Mark as non-example data
      newSchedule.isExample = false;
      
      return newSchedule;
    });
    
    // Save last generated scheduling data
    setLastGeneratedSchedules(schedulesWithState);
    
    // Set scheduling results: only include latest generated data and example data
    setScheduleResults(() => {
      // Get example data
      const exampleSchedules = getExampleSchedules();
      
      // Merge latest generated data and example data
      return [...schedulesWithState, ...exampleSchedules];
    });
    
    // Set active scheduling solution ID
    // Use highest scoring solution as default display solution
    const activeId = schedulesWithState.length > 0 ? schedulesWithState[0].id : null;
    setActiveScheduleResultId(activeId);
    
    // Show results and switch to results tab
    setShowResults(true);
    setTabValue(2);
    
    console.log(`Schedule generated. Total schedules: ${result.totalSolutions}, Best score: ${result.bestScore}, Average score: ${result.averageScore}`);
  };

  const handleHistoryItemClick = (scheduleId) => {
    setCurrentScheduleId(scheduleId);
    setShowResults(true);
    setTabValue(2); // Switch to results tab
  };
  
  // Add a function to navigate to the DataManagement tab
  const navigateToDataManagement = () => {
    setTabValue(4); // Index 4 corresponds to the DataManagement tab
  };
  
  // Add to App.jsx
  const [activeDataManagementTab, setActiveDataManagementTab] = useState(0);
  const [activeConstraintSubTab, setActiveConstraintSubTab] = useState(0);

  const navigateToSystemConfig = (tab = 0, subTab = 0) => {
    setTabValue(4); // Switch to Data Management
    setActiveDataManagementTab(tab);
    
    if (tab === 1) { // If Constraint Management
      setActiveConstraintSubTab(subTab);
    }
  };

  // Rewrite navigateToScheduleHistory function
  const navigateToScheduleHistory = () => {
    // Prepare history data: latest generated scheduling data + example data
    const exampleSchedules = getExampleSchedules();
    
    // Merge data: prioritize recently generated schedules, then example data
    setScheduleResults(() => {
      return [...lastGeneratedSchedules, ...exampleSchedules];
    });
    
    // Switch to history tab
    setTabValue(3);
  };

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" gutterBottom>
        Intelligent Scheduling System - PoC
      </Typography>
      
      <Paper sx={{ width: '100%', mb: 2 }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          indicatorColor="primary"
          textColor="primary"
        >
          <Tab label="Schedule Courses" />
          <Tab label="Schedule Exams" />
          <Tab label="Schedule Results" disabled={!showResults} />
          <Tab 
            label="Schedule History" 
            onClick={() => {
              console.log("Direct Schedule History tab click detected");
              // Force navigation through navigateToScheduleHistory function
              navigateToScheduleHistory();
            }}
          />
          <Tab label="Data Management" />
        </Tabs>
        

        {/* // In the ScheduleCoursesForm tab */}
        {tabValue === 0 && (
          <ScheduleCoursesForm 
            onScheduleGenerated={handleScheduleGenerated} 
            systemParameters={systemParameters} 
            activePresetId={activePresetId}
            presets={presets}
            onPresetSelect={handlePresetSelect}
            onNavigateToDataManagement={navigateToDataManagement} // Add this prop
            navigateToSystemConfig={navigateToSystemConfig}

          />
        )}

        {/* Schedule Exams Tab */}
        {tabValue === 1 && (
          <ScheduleExamsForm onScheduleGenerated={handleScheduleGenerated} />
        )}
        {/* Schedule Results Tab */}
        {tabValue === 2 && (
          <ScheduleResults 
            scheduleId={currentScheduleId} 
            scheduleResults={scheduleResults}
            onBack={() => setTabValue(0)} // Allow navigation back to creation screen
            onViewHistory={() => navigateToScheduleHistory()} // Simplify history page navigation logic
          />
        )}
        
        {/* Schedule History Tab */}
        {tabValue === 3 && (
          <ScheduleHistory 
            key={`history-${Date.now()}`} // Use simple timestamp to ensure component re-renders
            onHistoryItemClick={handleHistoryItemClick} 
            schedulesFromResults={[...scheduleResults]} // Pass current complete dataset
          />
        )}
        
        {/* Data Management Tab */}
        {tabValue === 4 && (
          <DataManagement 
            parameters={systemParameters}
            onParametersUpdate={handleParametersUpdate}
            activePresetId={activePresetId}
            presets={presets}
            onPresetSelect={handlePresetSelect}
            initialTab={activeDataManagementTab}
            initialConstraintSubTab={activeConstraintSubTab}
          />
        )}
      </Paper>

      {/* Global Intelligent Assistant */}
      <IntelligentAssistant />
      
    </Container>
    
  );
}

export default App;