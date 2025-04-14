import React, { useState,useEffect  } from 'react';
import { Container, Typography, Paper, Box, Tabs, Tab } from '@mui/material';
import ScheduleCoursesForm from './components/ScheduleCoursesForm';
import ScheduleExamsForm from './components/ScheduleExamsForm';
import ScheduleResults from './components/ScheduleResults';
import DataManagement from './components/DataManagement';
import ScheduleHistory from './components/ScheduleHistory';
import './styles.css';
import IntelligentAssistant from './components/LLM/IntelligentAssistant';

function App() {
  const [tabValue, setTabValue] = useState(0);
  const [showResults, setShowResults] = useState(false);
  const [currentScheduleId, setCurrentScheduleId] = useState(null);
  
  // In App.jsx, change the state to support multiple schedule results:
  const [scheduleResults, setScheduleResults] = useState([]);
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
 
  // In App.jsx, update handleScheduleGenerated
  const handleScheduleGenerated = (result) => {
    // 确保schedules是一个数组
    const validSchedules = Array.isArray(result.schedules) ? result.schedules : [];
    
    // 设置排课结果
    setScheduleResults(validSchedules);
    
    // 设置活动排课方案ID
    // 使用评分最高的方案作为默认显示方案
    const activeId = validSchedules.length > 0 ? validSchedules[0].id : null;
    setActiveScheduleResultId(activeId);
    
    // 显示结果并切换到结果标签页
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
          <Tab label="Schedule History" />
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
  />        )}
        
        {/* Schedule History Tab */}
        {tabValue === 3 && (
          <ScheduleHistory onHistoryItemClick={handleHistoryItemClick} />
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