import React, { useState, useEffect } from 'react';
import { 
  Box, 
  Typography, 
  Tabs, 
  Tab, 
  Grid, 
  Card, 
  CardContent, 
  Divider, 
  TableContainer, 
  Table, 
  TableHead, 
  TableBody, 
  TableRow, 
  TableCell, 
  Paper, 
  Chip ,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  Slider,
  List,
  ListItem,
  ListItemText,
  Dialog,
  DialogTitle,
  DialogContent,
  Tooltip,
  IconButton
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import CloseIcon from '@mui/icons-material/Close';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import { mockTimeSlots } from '../services/mockData';
import Alert from '@mui/material/Alert';
import WarningIcon from '@mui/icons-material/Warning';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import DialogActions from '@mui/material/DialogActions';
import DialogContentText from '@mui/material/DialogContentText';
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import HistoryIcon from '@mui/icons-material/History';
import { format } from 'date-fns';

// 导入真实API服务
import { getScheduleById } from '../services/api';

import ScheduleExplanation from './LLM/ScheduleExplanation';
import ConflictResolution from './LLM/ConflictResolution';
import ScheduleHistory from './ScheduleHistory';


const ScheduleResults = ({ scheduleId, scheduleResults, onBack, onViewHistory }) => {
  const [schedule, setSchedule] = useState(null);
  const [selectedScheduleId, setSelectedScheduleId] = useState(scheduleId);
  const [resultsTabValue, setResultsTabValue] = useState(0);
  const [currentWeek, setCurrentWeek] = useState(1);
  const [totalWeeks, setTotalWeeks] = useState(16); // Default to 16 weeks in a semester
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [availableSchedules, setAvailableSchedules] = useState([]);
  
  // Add conflict panel expansion/collapse status
  const [conflictsPanelExpanded, setConflictsPanelExpanded] = useState(false);
  const [aiAnalysisExpanded, setAiAnalysisExpanded] = useState(false);
  
  // Add loading status and conflict analysis status
  const [analyzingConflict, setAnalyzingConflict] = useState(false);
  const [analyzedConflictId, setAnalyzedConflictId] = useState(null);
  const [confirmDialogOpen, setConfirmDialogOpen] = useState(false);
  const [conflictToResolve, setConflictToResolve] = useState(null);
  const [solutionToApply, setSolutionToApply] = useState(null);

  // Teacher conflict and classroom conflict status
  const [teacherConflicts, setTeacherConflicts] = useState([]);
  const [classroomConflicts, setClassroomConflicts] = useState([]);

  // Add schedule history record status
  const [scheduleHistory, setScheduleHistory] = useState([]);
  const [historyDialogOpen, setHistoryDialogOpen] = useState(false);
  const [statusHistoryOpen, setStatusHistoryOpen] = useState(false);
  const [selectedScheduleHistory, setSelectedScheduleHistory] = useState(null);

  // Add debugging helper function
  const debugObject = (obj) => {
    console.log(JSON.stringify(obj, null, 2));
  };

  const handleConflictResolved = (solution, conflictId) => {
    console.log(`Applying solution to conflict ${conflictId}:`, solution);
    
    // Force resolve all conflicts, ignoring constraint limitations
    // Update conflict status
    setConflicts(prevConflicts => 
      prevConflicts.map(conflict => 
        conflict.id === conflictId || conflict.id === Number(conflictId) // Handle possible string/number ID mismatch
          ? { ...conflict, status: 'Resolved' } 
          : conflict
      )
    );
    
    // Detect conflict type, can handle multiple conflicts at once
    const isTeacherConflict = conflictId.toString().includes('teacher');
    const isClassroomConflict = conflictId.toString().includes('classroom');
    
    // Update teacher conflict status
    if (isTeacherConflict) {
      setTeacherConflicts(prevConflicts => 
        prevConflicts.map(conflict => 
          conflict.id === conflictId || conflict.id.toString() === conflictId.toString()
            ? { ...conflict, status: 'Resolved' } 
            : conflict
        )
      );
    }
    
    // Update classroom conflict status
    if (isClassroomConflict) {
      setClassroomConflicts(prevConflicts => 
        prevConflicts.map(conflict => 
          conflict.id === conflictId || conflict.id.toString() === conflictId.toString()
            ? { ...conflict, status: 'Resolved' } 
            : conflict
        )
      );
    }
    
    // If it is a combined conflict (i.e., both teacher and classroom conflicts), update both statuses simultaneously
    if (conflictId.toString().includes('combined')) {
      // Update teacher conflict
      setTeacherConflicts(prevConflicts => 
        prevConflicts.map(conflict => 
          conflict.relatedId === conflictId || conflict.relatedId?.toString() === conflictId.toString()
            ? { ...conflict, status: 'Resolved' } 
            : conflict
        )
      );
      
      // Update classroom conflict
      setClassroomConflicts(prevConflicts => 
        prevConflicts.map(conflict => 
          conflict.relatedId === conflictId || conflict.relatedId?.toString() === conflictId.toString()
            ? { ...conflict, status: 'Resolved' } 
            : conflict
        )
      );
    }
    
    // Close confirmation dialog
    setConfirmDialogOpen(false);
    // Reset current conflict and solution
    setConflictToResolve(null);
    setSolutionToApply(null);
    
    // Display test message
    alert(`Test environment: Solution applied successfully! In production, the system will send a request to the server and process actual constraints.`);
  };
  
  // Handle confirm application solution
  const handleConfirmApplySolution = () => {
    if (conflictToResolve && solutionToApply) {
      // In actual application, the API will be called
      console.log("Applying solution...", solutionToApply, "to conflict:", conflictToResolve.id);
      
      // Update local status
      handleConflictResolved(solutionToApply, conflictToResolve.id);
    }
  };
  
  // Handle cancel application solution
  const handleCancelApplySolution = () => {
    setConfirmDialogOpen(false);
    setConflictToResolve(null);
    setSolutionToApply(null);
  };
  
  // Handle analyze conflict
  const handleAnalyzeConflict = (conflictId) => {
    setAnalyzingConflict(true);
    setAnalyzedConflictId(null);
    
    // Simulate analysis delay
    setTimeout(() => {
      setAnalyzingConflict(false);
      setAnalyzedConflictId(conflictId);
    }, 1000);
  };
  
  // Handle apply solution confirmation
  const handleApplySolution = (solution, conflict) => {
    setConflictToResolve(conflict);
    setSolutionToApply(solution);
    setConfirmDialogOpen(true);
  };

  const [conflicts, setConflicts] = useState([
    {
      id: 1,
      type: 'Teachers ',
      description: 'Professor Smith has two courses scheduled at the same time (Monday, 08:00-09:30)',
      status: 'Unresolved',
      involvedCourses: [
        {
          code: 'CS101',
          name: 'Introduction to Computer Science',
          teacher: 'Prof. Smith',
          classroom: 'Building A-101',
          timeSlot: 'Monday 08:00-09:30'
        },
        {
          code: 'CS301',
          name: 'Algorithm Design',
          teacher: 'Prof. Smith',
          classroom: 'Building A-101',
          timeSlot: 'Monday 08:00-09:30'
        }
      ]
    },
    {
      id: 2,
      type: 'Classrooms ',
      description: 'Two courses assigned to the same classroom at the same time (Tuesday, 10:00-11:30, Building B-301)',
      status: 'Unresolved',
      involvedCourses: [
        {
          code: 'MATH101',
          name: 'Advanced Mathematics',
          teacher: 'Prof. Williams',
          classroom: 'Building B-301',
          timeSlot: 'Tuesday 10:00-11:30'
        },
        {
          code: 'PHYS101',
          name: 'Physics I',
          teacher: 'Prof. Brown',
          classroom: 'Building B-301',
          timeSlot: 'Tuesday 10:00-11:30'
        }
      ]
    }
  ]);

  // Get schedule data
  const fetchScheduleData = async (id) => {
    if (!id) return;
    
    setLoading(true);
    setError(null);
    
    try {
      // Use API service to get schedule data
      const scheduleData = await getScheduleById(id);
      setSchedule(scheduleData);
      
      // If it is the first load and there is no available schedule list, use the incoming scheduleResults
      if (availableSchedules.length === 0 && scheduleResults && scheduleResults.length > 0) {
        // Ensure each schedule has status and status history fields
        const schedulesWithHistory = scheduleResults.map(result => {
          // Ensure status value exists
          const updatedResult = {
            ...result,
            status: result.status || 'Generated'
          };
          
          if (!updatedResult.statusHistory) {
            // Create default status history
            updatedResult.statusHistory = [{
              status: updatedResult.status,
              timestamp: updatedResult.createdAt || new Date().toISOString(),
              userId: 'System'
            }];
          }
          return updatedResult;
        });
        
        // Sort by creation time in descending order
        schedulesWithHistory.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
        
        setAvailableSchedules(schedulesWithHistory);
      }
    } catch (err) {
      console.error('Failed to get schedule data:', err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  // When ID changes, get data
  useEffect(() => {
    // If the incoming scheduleResults has data, use it
    if (scheduleResults && scheduleResults.length > 0) {
      // If there is no selected specific schedule ID, default to the first one
      if (!selectedScheduleId && scheduleResults.length > 0) {
        setSelectedScheduleId(scheduleResults[0].id);
      }
      
      // Get the selected schedule or default to the first one
      const selectedResult = scheduleResults.find(r => r.id === selectedScheduleId) || scheduleResults[0];
      
      // Ensure the selected schedule has the correct status
      const updatedResult = {
        ...selectedResult,
        status: selectedResult.status || 'Generated'
      };
      
      // Set the current viewed schedule
      setSchedule(updatedResult);
      
      // Ensure each schedule has status and status history fields
      const schedulesWithHistory = scheduleResults.map(result => {
        // Ensure status value exists
        const updatedSchedule = {
          ...result,
          status: result.status || 'Generated'
        };
        
        if (!updatedSchedule.statusHistory || updatedSchedule.statusHistory.length === 0) {
          updatedSchedule.statusHistory = [{
            status: updatedSchedule.status,
            timestamp: updatedSchedule.createdAt || new Date().toISOString(),
            userId: 'System'
          }];
        }
        
        return updatedSchedule;
      });
      
      setAvailableSchedules(schedulesWithHistory);
    } else {
      // Otherwise, get from API
      fetchScheduleData(selectedScheduleId || scheduleId);
    }
  }, [scheduleId, selectedScheduleId, scheduleResults]);

  // When the selected ID changes, update
  useEffect(() => {
    if (selectedScheduleId && (!scheduleResults || scheduleResults.length === 0)) {
      fetchScheduleData(selectedScheduleId);
    }
  }, [selectedScheduleId]);

  // Add debugging log, check the course distribution in each time period
  useEffect(() => {
    if (schedule && schedule.details) {
      // Count the number of courses in each time period
      const stats = {
        morning: 0,
        afternoon: 0,
        evening: 0,
        total: schedule.details.length
      };
      
      schedule.details.forEach(item => {
        if (item.startTime) {
          if (item.startTime.startsWith('08') || item.startTime.startsWith('09') || 
              item.startTime.startsWith('10') || item.startTime.startsWith('11')) {
            stats.morning++;
          } else if (item.startTime.startsWith('14') || item.startTime.startsWith('15') || 
                     item.startTime.startsWith('16') || item.startTime.startsWith('17')) {
            stats.afternoon++;
          } else if (item.startTime.startsWith('19') || item.startTime.startsWith('21')) {
            stats.evening++;
          }
        }
      });
      
      // Output statistics
      console.log("Course time period distribution:");
      console.log(`  Morning: ${stats.morning} courses (${(stats.morning / stats.total * 100).toFixed(1)}%)`);
      console.log(`  Afternoon: ${stats.afternoon} courses (${(stats.afternoon / stats.total * 100).toFixed(1)}%)`);
      console.log(`  Evening: ${stats.evening} courses (${(stats.evening / stats.total * 100).toFixed(1)}%)`);
    }
  }, [schedule]);

  

  // Inject mock evening course data
  const injectMockEveningCourses = () => {
    if (!schedule || !schedule.details || schedule.details.length === 0) return;
    
    // Copy an existing course as a template
    const templateCourse = {...schedule.details[0]};
    
    // Monday evening 19:00-20:30 course
    const mondayEveningCourse = {
      ...templateCourse,
      courseCode: "CS601",
      courseName: "Evening Programming Lab",
      teacherName: "Prof. Night",
      classroom: "Building A-101",
      day: 1, // Monday
      dayName: "Monday",
      startTime: "19:00",
      endTime: "20:30"
    };
    
    // Tuesday evening 21:00-22:30 course
    const tuesdayEveningCourse = {
      ...templateCourse,
      courseCode: "CS602",
      courseName: "Advanced Evening Programming",
      teacherName: "Prof. Moon",
      classroom: "Building B-202",
      day: 2, // Tuesday
      dayName: "Tuesday",
      startTime: "21:00",
      endTime: "22:30"
    };
    
    // Thursday evening 19:00-20:30 course
    const thursdayEveningCourse = {
      ...templateCourse,
      courseCode: "CS603",
      courseName: "Evening Algorithm Design",
      teacherName: "Prof. Star",
      classroom: "Building C-303",
      day: 4, // Thursday
      dayName: "Thursday",
      startTime: "19:00",
      endTime: "20:30"
    };
    
    // Add to course list
    const updatedDetails = [...schedule.details, mondayEveningCourse, tuesdayEveningCourse, thursdayEveningCourse];
    
    // Create updated schedule object
    const updatedSchedule = {
      ...schedule,
      details: updatedDetails
    };
    
    // Update status
    setSchedule(updatedSchedule);
    
    console.log("Mock evening course data injected:", [mondayEveningCourse, tuesdayEveningCourse, thursdayEveningCourse]);
  };

  const handleResultsTabChange = (event, newValue) => {
    setResultsTabValue(newValue);
  };

  // Create time table view data
  const createTimeTableData = () => {
    if (!schedule || !schedule.details) return {};
    
    // Group by classroom and time
    const tableData = {};
    
    // Initialize table structure
    mockTimeSlots.forEach(timeSlot => {
      const key = `${timeSlot.day}-${timeSlot.startTime}`;
      if (!tableData[key]) {
        tableData[key] = {
          day: timeSlot.day,
          dayName: timeSlot.dayName,
          startTime: timeSlot.startTime,
          endTime: timeSlot.endTime,
          classrooms: {}
        };
      }
    });
    
    // Fill with course data
    schedule.details.forEach(scheduleItem => {
      const key = `${scheduleItem.day}-${scheduleItem.startTime}`;
      const classroom = scheduleItem.classroom.split('-')[1];
      
      if (tableData[key]) {
        tableData[key].classrooms[classroom] = {
          courseCode: scheduleItem.courseCode,
          courseName: scheduleItem.courseName,
          teacherName: scheduleItem.teacherName
        };
      }
    });
    
    return tableData;
  };

  // Get unique classrooms from schedule details
  const getUniqueClassrooms = () => {
    if (!schedule || !schedule.details) return [];
    return [...new Set(schedule.details.map(s => s.classroom.split('-')[1]))];
  };

  const timeTableData = createTimeTableData();
  const uniqueClassrooms = getUniqueClassrooms();

  // Group schedule by teachers
  const getTeacherSchedules = () => {
    if (!schedule || !schedule.details) return [];
    
    const teacherMap = {};
    schedule.details.forEach(item => {
      if (!teacherMap[item.teacherName]) {
        teacherMap[item.teacherName] = {
          name: item.teacherName,
          schedules: []
        };
      }
      teacherMap[item.teacherName].schedules.push(item);
    });
    
    return Object.values(teacherMap);
  };

  // Group schedule by classrooms
  const getClassroomSchedules = () => {
    if (!schedule || !schedule.details) return [];
    
    const classroomMap = {};
    schedule.details.forEach(item => {
      if (!classroomMap[item.classroom]) {
        classroomMap[item.classroom] = {
          name: item.classroom,
          schedules: []
        };
      }
      classroomMap[item.classroom].schedules.push(item);
    });
    
    return Object.values(classroomMap);
  };

  // Add a dialog for displaying multiple courses
  const [multiCourseDialogOpen, setMultiCourseDialogOpen] = useState(false);
  const [selectedMultiCourses, setSelectedMultiCourses] = useState([]);

  // Add useEffect to update conflicts - must be placed before all conditional judgments
  useEffect(() => {
    if (!schedule || !schedule.details) return;
    
    // Check teacher conflicts
    const newTeacherConflicts = [];
    
    // Group by teacher and time
    const teacherGroups = {};
    
    // Collect all courses grouped by teacher
    schedule.details.forEach(item => {
      const key = `${item.teacherName}-${item.dayName}-${item.startTime}-${item.endTime}`;
      if (!teacherGroups[key]) {
        teacherGroups[key] = [];
      }
      teacherGroups[key].push(item);
    });
    
    // Only consider conflicts when there are multiple courses from the same teacher at the same time
    Object.entries(teacherGroups).forEach(([key, courses]) => {
      if (courses.length > 1) {
        const conflictId = `teacher-${key}-${schedule.id}`;
        // Check if this conflict has already been marked as resolved in the state
        const existingConflict = teacherConflicts.find(c => c.id === conflictId);
        const resolvedStatusInState = existingConflict ? existingConflict.status === 'Resolved' : false;
        
        newTeacherConflicts.push({
          id: conflictId, 
          type: 'Teacher Schedule',
          description: `${courses[0].teacherName} has ${courses.length} courses scheduled at the same time (${courses[0].dayName}, ${courses[0].startTime}-${courses[0].endTime})`,
          status: resolvedStatusInState ? 'Resolved' : 'Unresolved',
          involvedCourses: courses,
          scheduleId: schedule.id // Associated with a specific schedule plan
        });
      }
    });

    // Check classroom conflicts
    const newClassroomConflicts = [];
    
    // Group by classroom and time
    const classroomGroups = {};
    
    // Collect all courses grouped by classroom
    schedule.details.forEach(item => {
      const key = `${item.classroom}-${item.dayName}-${item.startTime}-${item.endTime}`;
      if (!classroomGroups[key]) {
        classroomGroups[key] = [];
      }
      classroomGroups[key].push(item);
    });
    
    // Only consider conflicts when there are multiple courses from the same classroom at the same time
    Object.entries(classroomGroups).forEach(([key, courses]) => {
      if (courses.length > 1) {
        const conflictId = `classroom-${key}-${schedule.id}`;
        // Check if this conflict has already been marked as resolved in the state
        const existingConflict = classroomConflicts.find(c => c.id === conflictId);
        const resolvedStatusInState = existingConflict ? existingConflict.status === 'Resolved' : false;
        
        newClassroomConflicts.push({
          id: conflictId,
          type: 'Classroom Assignment',
          description: `${courses[0].classroom} has ${courses.length} courses scheduled at the same time (${courses[0].dayName}, ${courses[0].startTime}-${courses[0].endTime})`,
          status: resolvedStatusInState ? 'Resolved' : 'Unresolved',
          involvedCourses: courses,
          scheduleId: schedule.id // Associated with a specific schedule plan
        });
      }
    });
    
    // Update teacher conflict status
    setTeacherConflicts(prevConflicts => {
      // Keep resolved status
      const updatedConflicts = newTeacherConflicts.map(conflict => {
        const existingConflict = prevConflicts.find(c => c.id === conflict.id);
        if (existingConflict && existingConflict.status === 'Resolved') {
          return { ...conflict, status: 'Resolved' };
        }
        return conflict;
      });
      return updatedConflicts;
    });
    
    // Update classroom conflict status
    setClassroomConflicts(prevConflicts => {
      // Keep resolved status
      const updatedConflicts = newClassroomConflicts.map(conflict => {
        const existingConflict = prevConflicts.find(c => c.id === conflict.id);
        if (existingConflict && existingConflict.status === 'Resolved') {
          return { ...conflict, status: 'Resolved' };
        }
        return conflict;
      });
      return updatedConflicts;
    });
  }, [schedule, teacherConflicts, classroomConflicts]);

  // Update availableSchedules status management
  const handleScheduleStatusChange = (newStatus) => {
    // Create status update record
    const statusUpdate = {
      status: newStatus,
      timestamp: new Date().toISOString(),
      userId: 'Current User'
    };
    
    // Update the status history of the currently selected schedule plan
    let updatedStatusHistory = schedule.statusHistory || [];
    updatedStatusHistory = [...updatedStatusHistory, statusUpdate];
    
    // Create a new schedule object (immutable update)
    const updatedSchedule = {
      ...schedule, 
      status: newStatus,
      statusHistory: updatedStatusHistory
    };
    
    // Update the currently displayed schedule plan
    setSchedule(updatedSchedule);
    
    // 更新可用排课方案列表中的相应方案
    setAvailableSchedules(prevSchedules => 
      prevSchedules.map(s => 
        s.id === schedule.id ? updatedSchedule : s
      )
    );
    
    // 显示测试消息
    alert(`Test environment: Schedule status has been changed to ${newStatus}! In production, the system will send an update request to the server.`);
  };

  // 打开状态历史对话框
  const handleOpenStatusHistory = (scheduleToView) => {
    // 找到availableSchedules中的该方案的最新版本
    // 这样可以确保我们总是显示最新的状态历史
    const updatedSchedule = availableSchedules.find(s => s.id === scheduleToView.id) || scheduleToView;

    // 确保statusHistory存在且是按时间排序的数组
    if (!updatedSchedule.statusHistory) {
      updatedSchedule.statusHistory = [{
        status: updatedSchedule.status || 'Draft',
        timestamp: updatedSchedule.createdAt || new Date().toISOString(),
        userId: 'System'
      }];
    }

    // 设置状态并打开对话框
    setSelectedScheduleHistory(updatedSchedule);
    setStatusHistoryOpen(true);
  };

  // 关闭状态历史对话框
  const handleCloseStatusHistory = () => {
    setStatusHistoryOpen(false);
    setSelectedScheduleHistory(null);
  };

  // 格式化日期
  const formatDate = (dateString) => {
    try {
      const date = new Date(dateString);
      return format(date, 'yyyy-MM-dd HH:mm');
    } catch (e) {
      return dateString;
    }
  };

  // 跳转到排课历史页面
  const handleViewHistory = () => {
    // 使用props传入的onViewHistory函数
    if (typeof onBack === 'function' && typeof onViewHistory === 'function') {
      // 确保availableSchedules包含最新状态
      const updatedSchedules = availableSchedules.map(s => {
        // 如果当前正在查看这个方案，使用最新状态
        if (s.id === schedule.id) {
          return schedule;
        }
        return s;
      });
      
      // 传递最新的状态数据给历史页面
      onViewHistory(updatedSchedules);
    }
  };

  // Render different UI states
  if (!schedule && availableSchedules.length === 0) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="info" sx={{ mb: 2 }}>
          No schedule data is available. Please generate a schedule first.
        </Alert>
        <Button 
          variant="contained" 
          onClick={() => {
            // Navigate back to schedule creation page
            if (typeof onBack === 'function') {
              onBack();
            }
          }}
        >
          Create New Schedule
        </Button>
      </Box>
    );
  }
  
  if (!schedule) {
    return (
      <Box sx={{ p: 3, textAlign: 'center' }}>
        <Typography variant="h6">Loading schedule data...</Typography>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', my: 3 }}>
          <Typography>Loading schedule data...</Typography>
        </Box>
      )}
      
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}
      
      {/* 顶部操作栏 - 添加历史记录按钮 */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
        <Button 
          variant="outlined" 
          startIcon={<HistoryIcon />}
          onClick={handleViewHistory}
        >
          View Schedule History
        </Button>
        
        <Button 
          variant="outlined" 
          onClick={onBack}
        >
          Back to Schedule Creation
        </Button>
      </Box>
      
      {/* Schedule selection dropdown */}
      <Box sx={{ mb: 3 }}>
        <FormControl fullWidth>
          <InputLabel>Select Schedule Plan</InputLabel>
          <Select
            value={selectedScheduleId || ''}
            onChange={(e) => setSelectedScheduleId(e.target.value)}
            label="Select Schedule Plan"
          >
            {availableSchedules.map(result => (
              <MenuItem key={result.id} value={result.id}>
                {result.name} {result.status === 'Draft' ? '(Draft)' : ''}
                {result.isPrimary && ' (Primary)'}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>

      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', mb: 2 }}>
        <Typography variant="h6" gutterBottom>
          {schedule.name}
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center' }}>
          <FormControl size="small" sx={{ minWidth: 150, mr: 2 }}>
            <InputLabel>Schedule Status</InputLabel>
            <Select
              value={schedule.status || 'Draft'}
              onChange={(e) => handleScheduleStatusChange(e.target.value)}
              label="Schedule Status"
              size="small"
            >
              <MenuItem value="Draft">Draft</MenuItem>
              <MenuItem value="Generated">Generated</MenuItem>
              <MenuItem value="Published">Published</MenuItem>
              <MenuItem value="Canceled">Canceled</MenuItem>
              <MenuItem value="Archived">Archived</MenuItem>
            </Select>
          </FormControl>
          <Box sx={{ display: 'flex', alignItems: 'center' }}>
            <Chip 
              label={schedule.status || 'Draft'} // 确保状态显示不为空
              color={
                schedule.status === 'Published' ? 'success' : 
                schedule.status === 'Draft' ? 'warning' : 
                schedule.status === 'Generated' ? 'info' : 
                schedule.status === 'Canceled' ? 'error' : 
                schedule.status === 'Archived' ? 'default' : 'warning' // 默认显示为warning (Draft)
              } 
              variant="outlined" 
              sx={{ mr: 1 }}
            />
            <Tooltip title="View Status History">
              <IconButton 
                size="small" 
                onClick={() => handleOpenStatusHistory(schedule)}
                disabled={!schedule?.statusHistory || schedule.statusHistory.length === 0}
              >
                <AccessTimeIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Box>
        </Box>
      </Box>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
        <Tabs 
          value={resultsTabValue} 
          onChange={handleResultsTabChange}
          aria-label="schedule results tabs"
          variant="scrollable"
          scrollButtons="auto"
        >
          <Tab label="Calendar View" />
          <Tab label="List View" />
          <Tab label="By Teacher" />
          <Tab label="By Classroom" />
        </Tabs>
      </Box>
      
      {/* 周选择组件 - 位置已修正，移到Tabs外部 */}
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="subtitle2">
              Week {currentWeek} of {totalWeeks}
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'center' }}>
              <Button 
                size="small" 
                disabled={currentWeek === 1}
                onClick={() => setCurrentWeek(prev => Math.max(1, prev - 1))}
              >
                Previous
              </Button>
              <Slider
                value={currentWeek}
                onChange={(e, newValue) => setCurrentWeek(newValue)}
                step={1}
                marks
                min={1}
                max={totalWeeks}
                valueLabelDisplay="auto"
                sx={{ mx: 2, width: 200 }}
              />
              <Button 
                size="small" 
                disabled={currentWeek === totalWeeks}
                onClick={() => setCurrentWeek(prev => Math.min(totalWeeks, prev + 1))}
              >
                Next
              </Button>
            </Box>
          </Box>

      {/* Calendar View */}
      {resultsTabValue === 0 && (
        <TableContainer component={Paper} variant="outlined">
          <Table>
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 'bold' }}>Time/Day</TableCell>
                <TableCell align="center" sx={{ fontWeight: 'bold' }}>Sunday</TableCell>
                <TableCell align="center" sx={{ fontWeight: 'bold' }}>Monday</TableCell>
                <TableCell align="center" sx={{ fontWeight: 'bold' }}>Tuesday</TableCell>
                <TableCell align="center" sx={{ fontWeight: 'bold' }}>Wednesday</TableCell>
                <TableCell align="center" sx={{ fontWeight: 'bold' }}>Thursday</TableCell>
                <TableCell align="center" sx={{ fontWeight: 'bold' }}>Friday</TableCell>
                <TableCell align="center" sx={{ fontWeight: 'bold' }}>Saturday</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {/* Morning section header */}
              <TableRow>
                <TableCell colSpan={8} sx={{ backgroundColor: '#f5f5f5', fontWeight: 'bold' }}>
                  Morning (8:00 - 12:00)
                </TableCell>
              </TableRow>
              
              {/* Morning time slots */}
              {['08:00-09:30', '10:00-11:30'].map(timeSlot => {
                const [startTime, endTime] = timeSlot.split('-');
                return (
                  <TableRow key={`morning-${timeSlot}`}>
                    <TableCell sx={{ whiteSpace: 'nowrap', fontWeight: 'bold' }}>
                      {startTime}-{endTime}
                    </TableCell>
                    {/* Sunday to Saturday (0-6) */}
                    {[0, 1, 2, 3, 4, 5, 6].map(dayNum => {
                      // Filter courses for the current time slot and day, using more relaxed matching conditions
                      const coursesInCell = schedule.details.filter(item => {
                        // Special handling for matching logic in evening time slots
                        if (startTime === "19:00" || startTime === "21:00") {
                          // Evening time slots use relaxed matching, only the start of the time slot needs to match
                          const itemStartsAt19 = item.startTime && item.startTime.startsWith("19");
                          const itemStartsAt21 = item.startTime && item.startTime.startsWith("21");
                          
                          // Because startTime might be "19:00" or "21:00"
                          const matchesStartTime = 
                            (startTime === "19:00" && itemStartsAt19) || 
                            (startTime === "21:00" && itemStartsAt21);
                            
                          return matchesStartTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        } else {
                          // Other time slots still use exact matching
                          return item.startTime === startTime && 
                            item.endTime === endTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        }
                      });
                      
                      // Debug information - Check if there are courses in the evening time slots
                      if (startTime === "19:00" || startTime === "21:00") {
                        // Only log on the first render
                        if (dayNum === 0 && !window.hasLoggedEveningSlots) {
                          console.log(`Check the number of courses in the time slot ${startTime}-${endTime}: ${coursesInCell.length}`);
                          window.hasLoggedEveningSlots = true;
                        }
                        
                        // Print all course times to see if there are matching issues, but only execute if no courses are found
                        if (coursesInCell.length === 0 && dayNum === 0 && !window.hasLoggedEveningMatches) {
                          const possibleMatches = schedule.details.filter(item => 
                            item.day === dayNum &&
                            (item.startTime.includes("19") || item.startTime.includes("21"))
                          );
                          
                          if (possibleMatches.length > 0) {
                            console.log(`Found possible matching evening courses, but not displayed in the time slot ${startTime}-${endTime}`);
                            window.hasLoggedEveningMatches = true;
                          }
                        }
                      }
                      
                      // Detect conflicts for the same teacher at the same time
                      const hasTeacherConflict = (() => {
                        // Group by teacher name
                        const teacherGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!teacherGroups[course.teacherName]) {
                            teacherGroups[course.teacherName] = [];
                          }
                          teacherGroups[course.teacherName].push(course);
                        });
                        
                        // Only consider it a conflict if the same teacher has multiple courses in the same time slot
                        return Object.values(teacherGroups).some(courses => courses.length > 1);
                      })();
                      
                      // Detect conflicts for the same classroom at the same time
                      const hasClassroomConflict = (() => {
                        // Group by classroom
                        const classroomGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!classroomGroups[course.classroom]) {
                            classroomGroups[course.classroom] = [];
                          }
                          classroomGroups[course.classroom].push(course);
                        });
                        
                        // Only consider it a conflict if the same classroom has multiple courses in the same time slot
                        return Object.values(classroomGroups).some(courses => courses.length > 1);
                      })();
                      
                      // Check if there are any conflicts
                      const hasConflict = hasTeacherConflict || hasClassroomConflict;
                      
                      return (
                        <TableCell key={`morning-${timeSlot}-${dayNum}`} align="center" sx={{ height: 80, minWidth: 140 }}>
                          {coursesInCell.length > 0 ? (
                            <Box>
                              {coursesInCell.length === 1 ? (
                                // Display single course
                                <>
                                  <Typography variant="body2" fontWeight="bold">
                                    {coursesInCell[0].courseName}
                                  </Typography>
                                  <Typography variant="caption" display="block">
                                    {coursesInCell[0].teacherName}
                                  </Typography>
                                  <Typography variant="caption" display="block">
                                    {coursesInCell[0].classroom}
                                  </Typography>
                                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mt: 0.5 }}>
                                    <Chip 
                                      size="small" 
                                      label={coursesInCell[0].courseCode} 
                                      color="primary" 
                                      variant="outlined" 
                                    />
                                    <ScheduleExplanation scheduleItem={coursesInCell[0]} />
                                  </Box>
                                </>
                              ) : (
                                // Display multiple courses - Create dropdown effect
                                <Tooltip 
                                  title={
                                    <Box>
                                      {coursesInCell.map((item, idx) => (
                                        <Box key={idx} sx={{ mb: idx < coursesInCell.length - 1 ? 1 : 0, p: 1, borderBottom: idx < coursesInCell.length - 1 ? '1px solid #eee' : 'none' }}>
                                          <Typography variant="body2" fontWeight="bold">
                                            {item.courseName} ({item.courseCode})
                                          </Typography>
                                          <Typography variant="caption" display="block">
                                            Teacher: {item.teacherName}
                                          </Typography>
                                          <Typography variant="caption" display="block">
                                            Room: {item.classroom}
                                          </Typography>
                                        </Box>
                                      ))}
                                    </Box>
                                  } 
                                  arrow
                                  placement="top"
                                >
                                  <Button 
                                    variant="outlined" 
                                    size="small" 
                                    fullWidth 
                                    endIcon={<ExpandMoreIcon />}
                                    onClick={(e) => {
                                      // Open dialog with all courses
                                      setMultiCourseDialogOpen(true);
                                      setSelectedMultiCourses(coursesInCell);
                                    }}
                                    // If there is a conflict, use red style
                                    sx={{ color: hasConflict ? 'error.main' : undefined, 
                                         borderColor: hasConflict ? 'error.main' : undefined }}
                                  >
                                    {hasConflict && "⚠️ "}
                                    {hasTeacherConflict && hasClassroomConflict ? "Teacher+Room" : 
                                     hasTeacherConflict ? "Teacher Conflict" : 
                                     hasClassroomConflict ? "Room Conflict" : ""} 
                                    {coursesInCell.length} Courses
                                  </Button>
                                </Tooltip>
                              )}
                            </Box>
                          ) : null}
                        </TableCell>
                      );
                    })}
                  </TableRow>
                );
              })}
              
              {/* Afternoon section header */}
              <TableRow>
                <TableCell colSpan={8} sx={{ backgroundColor: '#f5f5f5', fontWeight: 'bold' }}>
                  Afternoon (14:00 - 18:00)
                </TableCell>
              </TableRow>
              
              {/* Afternoon time slots */}
              {['14:00-15:30', '16:00-17:30'].map(timeSlot => {
                const [startTime, endTime] = timeSlot.split('-');
                      return (
                  <TableRow key={`afternoon-${timeSlot}`}>
                    <TableCell sx={{ whiteSpace: 'nowrap', fontWeight: 'bold' }}>
                      {startTime}-{endTime}
                    </TableCell>
                    {/* Sunday to Saturday (0-6) */}
                    {[0, 1, 2, 3, 4, 5, 6].map(dayNum => {
                      // Filter courses for the current time slot and day, using more relaxed matching conditions
                      const coursesInCell = schedule.details.filter(item => {
                        // Special handling for matching logic in evening time slots
                        if (startTime === "19:00" || startTime === "21:00") {
                          // Evening time slots use relaxed matching, only the start of the time slot needs to match
                          const itemStartsAt19 = item.startTime && item.startTime.startsWith("19");
                          const itemStartsAt21 = item.startTime && item.startTime.startsWith("21");
                          
                          // Because startTime might be "19:00" or "21:00"
                          const matchesStartTime = 
                            (startTime === "19:00" && itemStartsAt19) || 
                            (startTime === "21:00" && itemStartsAt21);
                            
                          return matchesStartTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        } else {
                          // Other time slots still use exact matching
                          return item.startTime === startTime && 
                            item.endTime === endTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        }
                      });
                      
                      // Debug information - Check if there are courses in the evening time slots
                      if (startTime === "19:00" || startTime === "21:00") {
                        // Only log on the first render
                        if (dayNum === 0 && !window.hasLoggedEveningSlots) {
                          console.log(`Check the number of courses in the time slot ${startTime}-${endTime}: ${coursesInCell.length}`);
                          window.hasLoggedEveningSlots = true;
                        }
                        
                        // Print all course times to see if there are matching issues, but only execute if no courses are found
                        if (coursesInCell.length === 0 && dayNum === 0 && !window.hasLoggedEveningMatches) {
                          const possibleMatches = schedule.details.filter(item => 
                            item.day === dayNum &&
                            (item.startTime.includes("19") || item.startTime.includes("21"))
                          );
                          
                          if (possibleMatches.length > 0) {
                            console.log(`Found possible matching evening courses, but not displayed in the time slot ${startTime}-${endTime}`);
                            window.hasLoggedEveningMatches = true;
                          }
                        }
                      }
                      
                      // Detect conflicts for the same teacher at the same time
                      const hasTeacherConflict = (() => {
                        // Group by teacher name
                        const teacherGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!teacherGroups[course.teacherName]) {
                            teacherGroups[course.teacherName] = [];
                          }
                          teacherGroups[course.teacherName].push(course);
                        });
                        
                        // Only consider it a conflict if the same teacher has multiple courses in the same time slot
                        return Object.values(teacherGroups).some(courses => courses.length > 1);
                      })();
                      
                      // Detect conflicts for the same classroom at the same time
                      const hasClassroomConflict = (() => {
                        // Group by classroom
                        const classroomGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!classroomGroups[course.classroom]) {
                            classroomGroups[course.classroom] = [];
                          }
                          classroomGroups[course.classroom].push(course);
                        });
                        
                        // Only consider it a conflict if the same classroom has multiple courses in the same time slot
                        return Object.values(classroomGroups).some(courses => courses.length > 1);
                      })();
                      
                      // Check if there are any conflicts
                      const hasConflict = hasTeacherConflict || hasClassroomConflict;
                      
                      return (
                        <TableCell key={`afternoon-${timeSlot}-${dayNum}`} align="center" sx={{ height: 80, minWidth: 140 }}>
                          {coursesInCell.length > 0 ? (
                            <Box>
                              {coursesInCell.length === 1 ? (
                                // 单课程显示
                                <>
                                  <Typography variant="body2" fontWeight="bold">
                                    {coursesInCell[0].courseName}
                                  </Typography>
                                  <Typography variant="caption" display="block">
                                    {coursesInCell[0].teacherName}
                                  </Typography>
                                  <Typography variant="caption" display="block">
                                    {coursesInCell[0].classroom}
                                  </Typography>
                                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mt: 0.5 }}>
                                    <Chip 
                                      size="small" 
                                      label={coursesInCell[0].courseCode} 
                                      color="primary" 
                                      variant="outlined" 
                                    />
                                    <ScheduleExplanation scheduleItem={coursesInCell[0]} />
                                  </Box>
                                </>
                              ) : (
                                // Display multiple courses - Create dropdown effect
                                <Tooltip 
                                  title={
                                    <Box>
                                      {coursesInCell.map((item, idx) => (
                                        <Box key={idx} sx={{ mb: idx < coursesInCell.length - 1 ? 1 : 0, p: 1, borderBottom: idx < coursesInCell.length - 1 ? '1px solid #eee' : 'none' }}>
                                          <Typography variant="body2" fontWeight="bold">
                                            {item.courseName} ({item.courseCode})
                                          </Typography>
                                          <Typography variant="caption" display="block">
                                            Teacher: {item.teacherName}
                                          </Typography>
                                          <Typography variant="caption" display="block">
                                            Room: {item.classroom}
                                          </Typography>
                                        </Box>
                                      ))}
                                    </Box>
                                  } 
                                  arrow
                                  placement="top"
                                >
                                  <Button 
                                    variant="outlined" 
                                    size="small" 
                                    fullWidth 
                                    endIcon={<ExpandMoreIcon />}
                                    onClick={(e) => {
                                      // Open dialog with all courses
                                      setMultiCourseDialogOpen(true);
                                      setSelectedMultiCourses(coursesInCell);
                                    }}
                                    // If there is a conflict, use red style
                                    sx={{ color: hasConflict ? 'error.main' : undefined, 
                                         borderColor: hasConflict ? 'error.main' : undefined }}
                                  >
                                    {hasConflict && "⚠️ "}
                                    {hasTeacherConflict && hasClassroomConflict ? "Teacher+Room" : 
                                     hasTeacherConflict ? "Teacher Conflict" : 
                                     hasClassroomConflict ? "Room Conflict" : ""} 
                                    {coursesInCell.length} Courses
                                  </Button>
                                </Tooltip>
                              )}
                            </Box>
                          ) : null}
                        </TableCell>
                      );
                    })}
                  </TableRow>
                );
              })}
              
              {/* Evening section header */}
              <TableRow>
                <TableCell colSpan={8} sx={{ backgroundColor: '#f5f5f5', fontWeight: 'bold' }}>
                  Evening (19:00 - 23:00)
                </TableCell>
              </TableRow>
              
              {/* Evening time slots */}
              {['19:00-20:30', '21:00-22:30'].map(timeSlot => {
                const [startTime, endTime] = timeSlot.split('-');
                return (
                  <TableRow key={`evening-${timeSlot}`}>
                    <TableCell sx={{ whiteSpace: 'nowrap', fontWeight: 'bold' }}>
                      {startTime}-{endTime}
                    </TableCell>
                    {/* Sunday to Saturday (0-6) */}
                    {[0, 1, 2, 3, 4, 5, 6].map(dayNum => {
                      // Filter courses for the current time slot and day, using more relaxed matching conditions
                      const coursesInCell = schedule.details.filter(item => {
                        // Special handling for matching logic in evening time slots
                        if (startTime === "19:00" || startTime === "21:00") {
                          // Evening time slots use relaxed matching, only the start of the time slot needs to match
                          const itemStartsAt19 = item.startTime && item.startTime.startsWith("19");
                          const itemStartsAt21 = item.startTime && item.startTime.startsWith("21");
                          
                          // Because startTime might be "19:00" or "21:00"
                          const matchesStartTime = 
                            (startTime === "19:00" && itemStartsAt19) || 
                            (startTime === "21:00" && itemStartsAt21);
                            
                          return matchesStartTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        } else {
                          // Other time slots still use exact matching
                          return item.startTime === startTime && 
                            item.endTime === endTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        }
                      });
                      
                      // Debug information - Check if there are courses in the evening time slots
                      if (startTime === "19:00" || startTime === "21:00") {
                        // Only log on the first render
                        if (dayNum === 0 && !window.hasLoggedEveningSlots) {
                          console.log(`Check the number of courses in the time slot ${startTime}-${endTime}: ${coursesInCell.length}`);
                          window.hasLoggedEveningSlots = true;
                        }
                        
                        // Print all course times to see if there are matching issues, but only execute if no courses are found
                        if (coursesInCell.length === 0 && dayNum === 0 && !window.hasLoggedEveningMatches) {
                          const possibleMatches = schedule.details.filter(item => 
                            item.day === dayNum &&
                            (item.startTime.includes("19") || item.startTime.includes("21"))
                          );
                          
                          if (possibleMatches.length > 0) {
                            console.log(`Found possible matching evening courses, but not displayed in the time slot ${startTime}-${endTime}`);
                            window.hasLoggedEveningMatches = true;
                          }
                        }
                      }
                      
                      // Detect conflicts for the same teacher at the same time
                      const hasTeacherConflict = (() => {
                        // Group by teacher name
                        const teacherGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!teacherGroups[course.teacherName]) {
                            teacherGroups[course.teacherName] = [];
                          }
                          teacherGroups[course.teacherName].push(course);
                        });
                        
                        // Only consider it a conflict if the same teacher has multiple courses in the same time slot
                        return Object.values(teacherGroups).some(courses => courses.length > 1);
                      })();
                      
                      // Detect conflicts for the same classroom at the same time
                      const hasClassroomConflict = (() => {
                        // Group by classroom
                        const classroomGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!classroomGroups[course.classroom]) {
                            classroomGroups[course.classroom] = [];
                          }
                          classroomGroups[course.classroom].push(course);
                        });
                        
                        // Only consider it a conflict if the same classroom has multiple courses in the same time slot
                        return Object.values(classroomGroups).some(courses => courses.length > 1);
                      })();
                      
                      // Check if there are any conflicts
                      const hasConflict = hasTeacherConflict || hasClassroomConflict;
                      
                      return (
                        <TableCell key={`evening-${timeSlot}-${dayNum}`} align="center" sx={{ height: 80, minWidth: 140 }}>
                          {coursesInCell.length > 0 ? (
                            <Box>
                              {coursesInCell.length === 1 ? (
                                // Display single course
                                <>
                                  <Typography variant="body2" fontWeight="bold">
                                    {coursesInCell[0].courseName}
                                  </Typography>
                                  <Typography variant="caption" display="block">
                                    {coursesInCell[0].teacherName}
                                  </Typography>
                                  <Typography variant="caption" display="block">
                                    {coursesInCell[0].classroom}
                                  </Typography>
                                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mt: 0.5 }}>
                                    <Chip 
                                      size="small" 
                                      label={coursesInCell[0].courseCode} 
                                      color="primary" 
                                      variant="outlined" 
                                    />
                                    <ScheduleExplanation scheduleItem={coursesInCell[0]} />
                                  </Box>
                                </>
                              ) : (
                                // Display multiple courses - Create dropdown effect
                                <Tooltip 
                                  title={
                                    <Box>
                                      {coursesInCell.map((item, idx) => (
                                        <Box key={idx} sx={{ mb: idx < coursesInCell.length - 1 ? 1 : 0, p: 1, borderBottom: idx < coursesInCell.length - 1 ? '1px solid #eee' : 'none' }}>
                                          <Typography variant="body2" fontWeight="bold">
                                            {item.courseName} ({item.courseCode})
                                          </Typography>
                                          <Typography variant="caption" display="block">
                                            Teacher: {item.teacherName}
                                          </Typography>
                                          <Typography variant="caption" display="block">
                                            Room: {item.classroom}
                                          </Typography>
                                        </Box>
                                      ))}
                                    </Box>
                                  } 
                                  arrow
                                  placement="top"
                                >
                                  <Button 
                                    variant="outlined" 
                                    size="small" 
                                    fullWidth 
                                    endIcon={<ExpandMoreIcon />}
                                    onClick={(e) => {
                                      // Open dialog with all courses
                                      setMultiCourseDialogOpen(true);
                                      setSelectedMultiCourses(coursesInCell);
                                    }}
                                    // If there is a conflict, use red style
                                    sx={{ color: hasConflict ? 'error.main' : undefined, 
                                         borderColor: hasConflict ? 'error.main' : undefined }}
                                  >
                                    {hasConflict && "⚠️ "}
                                    {hasTeacherConflict && hasClassroomConflict ? "Teacher+Room" : 
                                     hasTeacherConflict ? "Teacher Conflict" : 
                                     hasClassroomConflict ? "Room Conflict" : ""} 
                                    {coursesInCell.length} Courses
                                  </Button>
                                </Tooltip>
                              )}
                            </Box>
                          ) : null}
                        </TableCell>
                      );
                    })}
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      )}
      
      {/* List View */}
      {resultsTabValue === 1 && (
        <TableContainer component={Paper} variant="outlined">
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Course Code</TableCell>
                <TableCell>Course Name</TableCell>
                <TableCell>Teacher</TableCell>
                <TableCell>Classroom</TableCell>
                <TableCell>Time</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {schedule.details
                .sort((a, b) => a.courseCode.localeCompare(b.courseCode))
                .map((scheduleItem, index) => (
                  <TableRow key={index}>
                    <TableCell>{scheduleItem.courseCode}</TableCell>
                    <TableCell>{scheduleItem.courseName}</TableCell>
                    <TableCell>{scheduleItem.teacherName}</TableCell>
                    <TableCell>{scheduleItem.classroom}</TableCell>
                    <TableCell>
                      {scheduleItem.dayName} {scheduleItem.startTime}-{scheduleItem.endTime}
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
      
      {/* By Teacher View */}
      {resultsTabValue === 2 && (
        <Grid container spacing={3}>
          {getTeacherSchedules().map((teacher, idx) => (
            <Grid item xs={12} md={6} key={idx}>
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    {teacher.name}
                  </Typography>
                  <Divider sx={{ my: 1 }} />
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Course</TableCell>
                          <TableCell>Time</TableCell>
                          <TableCell>Classroom</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {teacher.schedules
                          .sort((a, b) => a.day - b.day || a.startTime.localeCompare(b.startTime))
                          .map((scheduleItem, idx) => (
                            <TableRow key={idx}>
                              <TableCell>{scheduleItem.courseCode} {scheduleItem.courseName}</TableCell>
                              <TableCell>
                                {scheduleItem.dayName}<br />
                                {scheduleItem.startTime}-{scheduleItem.endTime}
                              </TableCell>
                              <TableCell>{scheduleItem.classroom}</TableCell>
                            </TableRow>
                          ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
      
      {/* By Classroom View */}
      {resultsTabValue === 3 && (
        <Grid container spacing={3}>
          {getClassroomSchedules().map((classroom, idx) => (
            <Grid item xs={12} md={6} key={idx}>
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Classroom: {classroom.name}
                  </Typography>
                  <Divider sx={{ my: 1 }} />
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Course</TableCell>
                          <TableCell>Teacher</TableCell>
                          <TableCell>Time</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {classroom.schedules
                          .sort((a, b) => a.day - b.day || a.startTime.localeCompare(b.startTime))
                          .map((scheduleItem, idx) => (
                            <TableRow key={idx}>
                              <TableCell>{scheduleItem.courseCode} {scheduleItem.courseName}</TableCell>
                              <TableCell>{scheduleItem.teacherName}</TableCell>
                              <TableCell>
                                {scheduleItem.dayName}<br />
                                {scheduleItem.startTime}-{scheduleItem.endTime}
                              </TableCell>
                            </TableRow>
                          ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

        {/* // Then add the Dialog component */}
        <Dialog
          open={multiCourseDialogOpen}
          onClose={() => setMultiCourseDialogOpen(false)}
          maxWidth="md"
        >
          <DialogTitle>
            {/* Check if there are teacher conflicts and classroom conflicts */}
            {(() => {
              // Group by teacher
              const teacherGroups = {};
              selectedMultiCourses.forEach(course => {
                const key = `${course.teacherName}-${course.dayName}-${course.startTime}-${course.endTime}`;
                if (!teacherGroups[key]) {
                  teacherGroups[key] = [];
                }
                teacherGroups[key].push(course);
              });
              
              // Group by classroom
              const classroomGroups = {};
              selectedMultiCourses.forEach(course => {
                const key = `${course.classroom}-${course.dayName}-${course.startTime}-${course.endTime}`;
                if (!classroomGroups[key]) {
                  classroomGroups[key] = [];
                }
                classroomGroups[key].push(course);
              });
              
              // Find actual conflicts - same teacher or same classroom at the same time
              const realTeacherConflicts = [];
              Object.entries(teacherGroups).forEach(([key, courses]) => {
                if (courses.length > 1) {
                  realTeacherConflicts.push({
                    teacherName: courses[0].teacherName,
                    dayName: courses[0].dayName,
                    startTime: courses[0].startTime,
                    endTime: courses[0].endTime,
                    coursesCount: courses.length,
                    courses: courses
                  });
                }
              });
              
              const realClassroomConflicts = [];
              Object.entries(classroomGroups).forEach(([key, courses]) => {
                if (courses.length > 1) {
                  realClassroomConflicts.push({
                    classroom: courses[0].classroom,
                    dayName: courses[0].dayName,
                    startTime: courses[0].startTime,
                    endTime: courses[0].endTime,
                    coursesCount: courses.length,
                    courses: courses
                  });
                }
              });
              
              const hasConflicts = realTeacherConflicts.length > 0 || realClassroomConflicts.length > 0;
              
              return (
                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                  {realTeacherConflicts.length > 0 && (
                    <Chip 
                      label="Teacher Conflict" 
                      color="error" 
                      size="small" 
                      icon={<WarningIcon />} 
                      sx={{ mr: 1 }}
                    />
                  )}
                  {realClassroomConflicts.length > 0 && (
                    <Chip 
                      label="Classroom Conflict" 
                      color="error" 
                      size="small" 
                      icon={<WarningIcon />} 
                      sx={{ mr: 1 }}
                    />
                  )}
            Multiple Courses
                </Box>
              );
            })()}
            <IconButton
              aria-label="close"
              onClick={() => setMultiCourseDialogOpen(false)}
              sx={{ position: 'absolute', right: 8, top: 8 }}
            >
              <CloseIcon />
            </IconButton>
          </DialogTitle>
          <DialogContent>
            {/* If there are conflicts, display warning message */}
            {(() => {
              // Group by teacher
              const teacherGroups = {};
              selectedMultiCourses.forEach(course => {
                const key = `${course.teacherName}-${course.dayName}-${course.startTime}-${course.endTime}`;
                if (!teacherGroups[key]) {
                  teacherGroups[key] = [];
                }
                teacherGroups[key].push(course);
              });
              
              // Group by classroom
              const classroomGroups = {};
              selectedMultiCourses.forEach(course => {
                const key = `${course.classroom}-${course.dayName}-${course.startTime}-${course.endTime}`;
                if (!classroomGroups[key]) {
                  classroomGroups[key] = [];
                }
                classroomGroups[key].push(course);
              });
              
              // Find actual conflicts - same teacher or same classroom at the same time
              const realTeacherConflicts = [];
              Object.entries(teacherGroups).forEach(([key, courses]) => {
                if (courses.length > 1) {
                  realTeacherConflicts.push({
                    teacherName: courses[0].teacherName,
                    dayName: courses[0].dayName,
                    startTime: courses[0].startTime,
                    endTime: courses[0].endTime,
                    coursesCount: courses.length,
                    courses: courses
                  });
                }
              });
              
              const realClassroomConflicts = [];
              Object.entries(classroomGroups).forEach(([key, courses]) => {
                if (courses.length > 1) {
                  realClassroomConflicts.push({
                    classroom: courses[0].classroom,
                    dayName: courses[0].dayName,
                    startTime: courses[0].startTime,
                    endTime: courses[0].endTime,
                    coursesCount: courses.length,
                    courses: courses
                  });
                }
              });
              
              const hasConflicts = realTeacherConflicts.length > 0 || realClassroomConflicts.length > 0;

              if (hasConflicts) {
                return (
                  <Alert severity="error" sx={{ mb: 2 }}>
                    <Typography variant="body2" fontWeight="bold">
                      Scheduling Conflict Results
                    </Typography>
                    {realTeacherConflicts.map((conflict, idx) => (
                      <Typography key={`teacher-${idx}`} variant="body2">
                        Teacher Conflict: {conflict.teacherName} is scheduled for {conflict.coursesCount} courses at {conflict.dayName} {conflict.startTime}-{conflict.endTime}
                      </Typography>
                    ))}
                    {realClassroomConflicts.map((conflict, idx) => (
                      <Typography key={`classroom-${idx}`} variant="body2">
                        Classroom Conflict: {conflict.classroom} has {conflict.coursesCount} courses scheduled at {conflict.dayName} {conflict.startTime}-{conflict.endTime}
                      </Typography>
                    ))}
                    <Typography variant="body2" fontWeight="bold" sx={{ mt: 1 }}>
                      AI Conflict Analysis: 
                      {realTeacherConflicts.length > 0 && realClassroomConflicts.length > 0 ? 
                        "Teacher and classroom time overlapping conflicts exist. Consider adjusting course times or changing teachers/classrooms." : 
                        realTeacherConflicts.length > 0 ? 
                          "The same teacher is scheduled for multiple courses at the same time. Consider assigning different teachers or adjusting course times." : 
                          "The same classroom is scheduled for multiple courses at the same time. Consider using different classrooms or adjusting course times."}
                    </Typography>
                  </Alert>
                );
              }
              
              return null;
            })()}
            
            <List>
              {selectedMultiCourses.map((item, idx) => {
                // Check if the current course has a teacher conflict
                const hasTeacherConflict = selectedMultiCourses.some(
                  otherItem => 
                    otherItem !== item && 
                    otherItem.teacherName === item.teacherName &&
                    otherItem.dayName === item.dayName &&
                    otherItem.startTime === item.startTime &&
                    otherItem.endTime === item.endTime
                );
                
                // Check if the current course has a classroom conflict
                const hasClassroomConflict = selectedMultiCourses.some(
                  otherItem => 
                    otherItem !== item && 
                    otherItem.classroom === item.classroom &&
                    otherItem.dayName === item.dayName &&
                    otherItem.startTime === item.startTime &&
                    otherItem.endTime === item.endTime
                );
                
                // Check if there are any conflicts
                const hasConflict = hasTeacherConflict || hasClassroomConflict;
                
                return (
                  <ListItem 
                    key={idx} 
                    divider={idx < selectedMultiCourses.length - 1}
                    sx={{ 
                      borderLeft: hasConflict ? '4px solid' : 'none', 
                      borderLeftColor: hasConflict ? 'error.main' : 'transparent',
                      pl: hasConflict ? 2 : 1, // Conflict items slightly increase left margin
                    }}
                  >
                  <ListItemText
                    primary={
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                          <Typography 
                            variant="subtitle1"
                            color={hasConflict ? 'error' : 'inherit'}
                          >
                            {item.courseName} ({item.courseCode}) {hasConflict && '⚠️'}
                        </Typography>
                        <ScheduleExplanation scheduleItem={item} />
                      </Box>
                    }
                    secondary={
                      <>
                          <Typography 
                            variant="body2"
                            color={hasTeacherConflict ? 'error' : 'inherit'}
                            fontWeight={hasTeacherConflict ? 'bold' : 'normal'}
                          >
                            Teacher: {item.teacherName} {hasTeacherConflict && <HelpOutlineIcon fontSize="small" color="error" />}
                          </Typography>
                        <Typography variant="body2">Time: {item.dayName} {item.startTime}-{item.endTime}</Typography>
                          <Typography 
                            variant="body2"
                            color={hasClassroomConflict ? 'error' : 'inherit'}
                            fontWeight={hasClassroomConflict ? 'bold' : 'normal'}
                          >
                            Classroom: {item.classroom} {hasClassroomConflict && <HelpOutlineIcon fontSize="small" color="error" />}
                          </Typography>
                      </>
                    }
                  />
                </ListItem>
                );
              })}
            </List>
          </DialogContent>
        </Dialog>
        
        {/* Solution confirmation dialog */}
        <Dialog
          open={confirmDialogOpen}
          onClose={handleCancelApplySolution}
          aria-labelledby="alert-dialog-title"
          aria-describedby="alert-dialog-description"
        >
          <DialogTitle id="alert-dialog-title">
            Confirm Solution
          </DialogTitle>
          <DialogContent>
            <DialogContentText id="alert-dialog-description">
              Are you sure you want to apply this solution?
            </DialogContentText>
            {solutionToApply && (
              <>
                <Typography variant="subtitle1" sx={{ mt: 2, mb: 1 }}>
                  Solution {solutionToApply.id} ({solutionToApply.compatibility}% compatibility)
                </Typography>
                <Typography variant="body2">
                  {solutionToApply.description}
                </Typography>
              </>
            )}
          </DialogContent>
          <DialogActions>
            <Button 
              onClick={handleCancelApplySolution} 
              color="primary"
            >
              CANCEL
            </Button>
            <Button 
              onClick={handleConfirmApplySolution} 
              color="primary" 
              variant="contained"
              startIcon={<CheckCircleIcon />}
              autoFocus
            >
              CONFIRM
            </Button>
          </DialogActions>
        </Dialog>
        
        {/* Unified conflict analysis and resolution section */}
        <Box sx={{ mt: 4, mb: 3, border: '1px solid #e0e0e0', borderRadius: 1 }}>
          {/* Conflict panel title */}
          <Box 
            sx={{ 
              p: 2, 
              bgcolor: teacherConflicts.filter(c => c.status !== 'Resolved').length > 0 || 
                      classroomConflicts.filter(c => c.status !== 'Resolved').length > 0 
                ? '#f5f5f5' : '#e8f5e9', 
              borderBottom: '1px solid #e0e0e0', 
              display: 'flex', 
              justifyContent: 'space-between', 
              alignItems: 'center',
              cursor: 'pointer'
            }}
            onClick={() => setConflictsPanelExpanded(!conflictsPanelExpanded)}
          >
            <Box sx={{ display: 'flex', alignItems: 'center' }}>
              <Typography variant="subtitle1" sx={{ 
                fontWeight: 'bold', 
                mr: 2, 
                color: teacherConflicts.filter(c => c.status !== 'Resolved').length > 0 || 
                        classroomConflicts.filter(c => c.status !== 'Resolved').length > 0 
                  ? 'text.primary' : 'success.main' 
              }}>
                Conflict Analysis & Resolution {
                  teacherConflicts.filter(c => c.status !== 'Resolved').length === 0 && 
                  classroomConflicts.filter(c => c.status !== 'Resolved').length === 0 && 
                  <CheckCircleIcon fontSize="small" sx={{ ml: 1 }} />
                }
              </Typography>

              {/* Add missing status indicators */}
              {teacherConflicts.filter(c => c.status !== 'Resolved').length > 0 || 
              classroomConflicts.filter(c => c.status !== 'Resolved').length > 0 ? (
                <>
                  <Chip 
                    label={`${
                      teacherConflicts.filter(c => c.status !== 'Resolved').length + 
                      classroomConflicts.filter(c => c.status !== 'Resolved').length
                    } Unresolved`} 
                    size="small" 
                    color="error" 
                    variant="outlined" 
                    sx={{ mr: 1 }}
                  />
                  {(teacherConflicts.filter(c => c.status === 'Resolved').length > 0 || 
                  classroomConflicts.filter(c => c.status === 'Resolved').length > 0) && (
                    <Chip 
                      label={`${
                        teacherConflicts.filter(c => c.status === 'Resolved').length + 
                        classroomConflicts.filter(c => c.status === 'Resolved').length
                      } Resolved`} 
                      size="small" 
                      color="success" 
                      variant="outlined" 
                      sx={{ mr: 1 }}
                    />
                  )}
                </>
              ) : (
                <Chip 
                  label="All Conflicts Resolved" 
                  size="small" 
                  color="success" 
                  variant="outlined" 
                  sx={{ mr: 1 }}
                />
              )}
            </Box>
            <Box>
              <IconButton
                onClick={(e) => {
                  e.stopPropagation();
                  setConflictsPanelExpanded(!conflictsPanelExpanded);
                }}
                size="small"
                sx={{ transform: conflictsPanelExpanded ? 'rotate(180deg)' : 'none' }}
              >
                <ExpandMoreIcon />
              </IconButton>
            </Box>
    </Box>

          {/* Conflict area content - only displayed when expanded */}
          {conflictsPanelExpanded && (
            <Box sx={{ p: 2 }}>
              {/* Message displayed when all conflicts are resolved */}
              {teacherConflicts.filter(c => c.status !== 'Resolved').length === 0 && 
              classroomConflicts.filter(c => c.status !== 'Resolved').length === 0 && (
                <Alert severity="success" sx={{ mb: 2 }}>
                  <Typography variant="body1" fontWeight="bold">
                    Congratulations!
                  </Typography>
                  <Typography variant="body2">
                    All conflicts have been successfully resolved. Your schedule is now optimized.
                  </Typography>
                </Alert>
              )}
              
              {/* Teacher conflicts */}
              {teacherConflicts.length > 0 && (
                <Box sx={{ mb: 3 }}>
                  <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1 }}>
                    Teacher Conflicts ({teacherConflicts.length})
                  </Typography>
                  {teacherConflicts.map((conflict, index) => (
                    <Box key={index} sx={{ mb: 2 }}>
                      <ConflictResolution 
                        key={conflict.id} 
                        conflict={conflict} 
                        onResolve={(solution, conflictId) => {
                          console.log("Solution selected:", solution, "for conflict:", conflictId);
                          setConflictToResolve(conflict);
                          setSolutionToApply(solution);
                          setConfirmDialogOpen(true);
                        }}
                        onAnalyze={handleAnalyzeConflict}
                        isAnalyzing={analyzingConflict && analyzedConflictId === conflict.id}
                        isAnalyzed={analyzedConflictId === conflict.id}
                        showSolutions={analyzedConflictId === conflict.id}
                      />
                    </Box>
                  ))}
                </Box>
              )}
              
              {/* Classroom conflicts */}
              {classroomConflicts.length > 0 && (
                <Box sx={{ mb: 3 }}>
                  <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1 }}>
                    Classroom Conflicts ({classroomConflicts.length})
                  </Typography>
                  {classroomConflicts.map((conflict, index) => (
                    <Box key={index} sx={{ mb: 2 }}>
                      <ConflictResolution 
                        key={conflict.id} 
                        conflict={conflict} 
                        onResolve={(solution, conflictId) => {
                          console.log("Solution selected:", solution, "for conflict:", conflictId);
                          setConflictToResolve(conflict);
                          setSolutionToApply(solution);
                          setConfirmDialogOpen(true);
                        }}
                        onAnalyze={handleAnalyzeConflict}
                        isAnalyzing={analyzingConflict && analyzedConflictId === conflict.id}
                        isAnalyzed={analyzedConflictId === conflict.id}
                        showSolutions={analyzedConflictId === conflict.id}
                      />
                    </Box>
                  ))}
                </Box>
              )}
            </Box>
          )}
        </Box>

        {/* Status history dialog */}
        <Dialog
          open={statusHistoryOpen}
          onClose={handleCloseStatusHistory}
          maxWidth="sm"
          fullWidth
        >
          <DialogTitle>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="h6">Status Change History</Typography>
              <IconButton onClick={handleCloseStatusHistory}>
                <CloseIcon />
              </IconButton>
            </Box>
          </DialogTitle>
          <DialogContent dividers>
            {!selectedScheduleHistory?.statusHistory || selectedScheduleHistory.statusHistory.length === 0 ? (
              <Alert severity="info">No status history found for this schedule</Alert>
            ) : (
              <List>
                {/* Sort status history by time in descending order, with the latest status shown at the top */}
                {[...selectedScheduleHistory.statusHistory]
                  .sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp))
                  .map((statusChange, index) => (
                    <ListItem 
                      key={index}
                      divider={index < selectedScheduleHistory.statusHistory.length - 1}
                    >
                      <ListItemText
                        primary={
                          <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            <Chip 
                              label={statusChange.status || 'Draft'} 
                              size="small"
                              color={
                                statusChange.status === 'Published' ? 'success' : 
                                statusChange.status === 'Draft' ? 'warning' : 
                                statusChange.status === 'Generated' ? 'info' :
                                statusChange.status === 'Canceled' ? 'error' : 'default'
                              } 
                              variant="outlined" 
                              sx={{ mr: 1 }}
                            />
                            <Typography variant="subtitle2">
                              {statusChange.status || 'Draft'}
                            </Typography>
                            {index === 0 && (
                              <Chip 
                                label="Current Status" 
                                size="small"
                                color="primary"
                                variant="outlined"
                                sx={{ ml: 1 }}
                              />
                            )}
                          </Box>
                        }
                        secondary={
                          <Box sx={{ mt: 1 }}>
                            <Typography variant="body2" color="text.secondary">
                              Changed by: {statusChange.userId || 'System'}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Time: {formatDate(statusChange.timestamp)}
                            </Typography>
                          </Box>
                        }
                      />
                    </ListItem>
                  ))}
              </List>
            )}
          </DialogContent>
        </Dialog>
    </Box>
  );
};

export default ScheduleResults;