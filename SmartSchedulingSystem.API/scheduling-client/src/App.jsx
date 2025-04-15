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
    console.log(`Tab changed from ${tabValue} to ${newValue}`);
    
    // 对于历史记录标签，使用专门的处理函数
    if (newValue === 3) {
      console.log("Navigating to History tab via handler");
      // 强制通过navigateToScheduleHistory函数跳转
      navigateToScheduleHistory(scheduleResults);
      return;
    }
    
    // 对于其他标签页，直接切换
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
    
    // 确保每个方案都有正确的状态和状态历史
    const schedulesWithState = validSchedules.map(schedule => {
      // 如果方案没有状态，设置为"Generated"
      const newSchedule = {
        ...schedule,
        status: schedule.status || "Generated"
      };
      
      // 确保每个排课方案都有状态历史记录
      if (!newSchedule.statusHistory || newSchedule.statusHistory.length === 0) {
        newSchedule.statusHistory = [{
          status: newSchedule.status,
          timestamp: new Date().toISOString(),
          userId: "System"
        }];
      }
      
      return newSchedule;
    });
    
    // 设置排课结果
    setScheduleResults(schedulesWithState);
    
    // 设置活动排课方案ID
    // 使用评分最高的方案作为默认显示方案
    const activeId = schedulesWithState.length > 0 ? schedulesWithState[0].id : null;
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

  // 添加一个函数用于导航到排课历史页面
  const navigateToScheduleHistory = (schedulesData = null) => {
    // 如果传入了排课数据，传递给历史页面
    if (schedulesData) {
      // 更新排课数据，确保每个排课方案都有正确的状态和历史记录
      const processedSchedules = schedulesData.map(schedule => {
        // 确保每个方案都有正确的状态
        const updatedSchedule = {
          ...schedule,
          status: schedule.status || 'Generated'
        };

        // 确保每个方案都有状态历史记录
        if (!updatedSchedule.statusHistory || updatedSchedule.statusHistory.length === 0) {
          updatedSchedule.statusHistory = [{
            status: updatedSchedule.status,
            timestamp: updatedSchedule.createdAt || new Date().toISOString(),
            userId: 'System'
          }];
        }

        return updatedSchedule;
      });

      setScheduleResults(prev => {
        // 合并新旧数据，确保不重复并优先使用最新数据
        const mergedSchedules = [...prev];
        processedSchedules.forEach(newSchedule => {
          // 查找是否已存在相同ID的排课方案
          const existingIndex = mergedSchedules.findIndex(s => s.id === newSchedule.id);
          if (existingIndex >= 0) {
            // 如果存在，替换为最新数据
            mergedSchedules[existingIndex] = newSchedule;
          } else {
            // 如果不存在，添加到数组
            mergedSchedules.push(newSchedule);
          }
        });
        
        // 返回合并后的数据
        return mergedSchedules;
      });
    }
    
    // 切换到历史记录标签页
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
              // 强制通过navigateToScheduleHistory函数跳转
              navigateToScheduleHistory(scheduleResults);
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
            onViewHistory={(updatedSchedules) => navigateToScheduleHistory(updatedSchedules || scheduleResults)} // 添加导航到历史页面的功能，接收最新状态
          />
        )}
        
        {/* Schedule History Tab */}
        {tabValue === 3 && (
          <ScheduleHistory 
            key={`history-${scheduleResults.map(s => `${s.id}-${s.status}`).join('|')}-${Date.now()}`} // 更精确的key确保组件重新渲染
            onHistoryItemClick={handleHistoryItemClick} 
            schedulesFromResults={[...scheduleResults]} // 强制新的数组引用
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