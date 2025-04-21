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
  
  // 添加冲突面板展开/折叠状态
  const [conflictsPanelExpanded, setConflictsPanelExpanded] = useState(false);
  const [aiAnalysisExpanded, setAiAnalysisExpanded] = useState(false);
  
  // 添加加载状态和冲突分析状态
  const [analyzingConflict, setAnalyzingConflict] = useState(false);
  const [analyzedConflictId, setAnalyzedConflictId] = useState(null);
  const [confirmDialogOpen, setConfirmDialogOpen] = useState(false);
  const [conflictToResolve, setConflictToResolve] = useState(null);
  const [solutionToApply, setSolutionToApply] = useState(null);

  // 教师冲突和教室冲突状态
  const [teacherConflicts, setTeacherConflicts] = useState([]);
  const [classroomConflicts, setClassroomConflicts] = useState([]);

  // 添加排课历史记录状态
  const [scheduleHistory, setScheduleHistory] = useState([]);
  const [historyDialogOpen, setHistoryDialogOpen] = useState(false);
  const [statusHistoryOpen, setStatusHistoryOpen] = useState(false);
  const [selectedScheduleHistory, setSelectedScheduleHistory] = useState(null);

  // 添加调试辅助函数
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
    
    // 检测冲突类型，可以同时处理多种冲突
    const isTeacherConflict = conflictId.toString().includes('teacher');
    const isClassroomConflict = conflictId.toString().includes('classroom');
    
    // 更新教师冲突状态
    if (isTeacherConflict) {
      setTeacherConflicts(prevConflicts => 
        prevConflicts.map(conflict => 
          conflict.id === conflictId || conflict.id.toString() === conflictId.toString()
            ? { ...conflict, status: 'Resolved' } 
            : conflict
        )
      );
    }
    
    // 更新教室冲突状态
    if (isClassroomConflict) {
      setClassroomConflicts(prevConflicts => 
        prevConflicts.map(conflict => 
          conflict.id === conflictId || conflict.id.toString() === conflictId.toString()
            ? { ...conflict, status: 'Resolved' } 
            : conflict
        )
      );
    }
    
    // 如果是组合冲突（即既包含教师冲突又包含教室冲突），则同时更新两种状态
    if (conflictId.toString().includes('combined')) {
      // 更新教师冲突
      setTeacherConflicts(prevConflicts => 
        prevConflicts.map(conflict => 
          conflict.relatedId === conflictId || conflict.relatedId?.toString() === conflictId.toString()
            ? { ...conflict, status: 'Resolved' } 
            : conflict
        )
      );
      
      // 更新教室冲突
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
  
  // 处理确认应用解决方案
  const handleConfirmApplySolution = () => {
    if (conflictToResolve && solutionToApply) {
      // 在实际应用中，会调用API
      console.log("应用解决方案...", solutionToApply, "到冲突：", conflictToResolve.id);
      
      // 更新本地状态
      handleConflictResolved(solutionToApply, conflictToResolve.id);
    }
  };
  
  // 处理取消应用解决方案
  const handleCancelApplySolution = () => {
    setConfirmDialogOpen(false);
    setConflictToResolve(null);
    setSolutionToApply(null);
  };
  
  // 处理分析冲突
  const handleAnalyzeConflict = (conflictId) => {
    setAnalyzingConflict(true);
    setAnalyzedConflictId(null);
    
    // 模拟分析延迟
    setTimeout(() => {
      setAnalyzingConflict(false);
      setAnalyzedConflictId(conflictId);
    }, 1000);
  };
  
  // 处理应用解决方案前的确认
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

  // 获取排课数据
  const fetchScheduleData = async (id) => {
    if (!id) return;
    
    setLoading(true);
    setError(null);
    
    try {
      // 使用API服务获取排课数据
      const scheduleData = await getScheduleById(id);
      setSchedule(scheduleData);
      
      // 如果是第一次加载并且没有可用排课列表，使用传入的scheduleResults
      if (availableSchedules.length === 0 && scheduleResults && scheduleResults.length > 0) {
        // 确保每个排课方案都有状态和状态历史字段
        const schedulesWithHistory = scheduleResults.map(result => {
          // 确保状态值存在
          const updatedResult = {
            ...result,
            status: result.status || 'Generated'
          };
          
          if (!updatedResult.statusHistory) {
            // 创建默认的状态历史
            updatedResult.statusHistory = [{
              status: updatedResult.status,
              timestamp: updatedResult.createdAt || new Date().toISOString(),
              userId: 'System'
            }];
          }
          return updatedResult;
        });
        
        // 按创建时间降序排序
        schedulesWithHistory.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
        
        setAvailableSchedules(schedulesWithHistory);
      }
    } catch (err) {
      console.error('获取排课数据失败:', err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  // 当ID变化时获取数据
  useEffect(() => {
    // 如果传入的scheduleResults有数据，使用它
    if (scheduleResults && scheduleResults.length > 0) {
      // 如果没有选中特定的排课方案ID，默认选中第一个
      if (!selectedScheduleId && scheduleResults.length > 0) {
        setSelectedScheduleId(scheduleResults[0].id);
      }
      
      // 获取选中的排课方案或默认第一个
      const selectedResult = scheduleResults.find(r => r.id === selectedScheduleId) || scheduleResults[0];
      
      // 确保选中的排课方案有正确的状态
      const updatedResult = {
        ...selectedResult,
        status: selectedResult.status || 'Generated'
      };
      
      // 设置当前查看的排课方案
      setSchedule(updatedResult);
      
      // 确保每个排课方案都有状态和状态历史字段
      const schedulesWithHistory = scheduleResults.map(result => {
        // 确保状态值存在
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
      // 否则从API获取
      fetchScheduleData(selectedScheduleId || scheduleId);
    }
  }, [scheduleId, selectedScheduleId, scheduleResults]);

  // 当所选ID变化时更新
  useEffect(() => {
    if (selectedScheduleId && (!scheduleResults || scheduleResults.length === 0)) {
      fetchScheduleData(selectedScheduleId);
    }
  }, [selectedScheduleId]);

  // 添加调试日志，检查各时间段的课程分布
  useEffect(() => {
    if (schedule && schedule.details) {
      // 按时间段统计课程数量
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
      
      // 输出统计信息
      console.log("课程时间段分布:");
      console.log(`  早上: ${stats.morning} 门课程 (${(stats.morning / stats.total * 100).toFixed(1)}%)`);
      console.log(`  下午: ${stats.afternoon} 门课程 (${(stats.afternoon / stats.total * 100).toFixed(1)}%)`);
      console.log(`  晚上: ${stats.evening} 门课程 (${(stats.evening / stats.total * 100).toFixed(1)}%)`);
    }
  }, [schedule]);

  // 添加调试日志，在组件加载时查看数据结构
  // const [hasInjectedEveningCourses, setHasInjectedEveningCourses] = useState(false);

  /*
  useEffect(() => {
    if (schedule && schedule.details && !hasInjectedEveningCourses) {
      console.log("检查课表数据中是否有晚上课程...");
      
      // 特别检查晚上时间段的课程
      const eveningCourses = schedule.details.filter(item => 
        item.startTime && (item.startTime.includes("19") || item.startTime.includes("21"))
      );
      
      if (eveningCourses.length > 0) {
        console.log(`找到 ${eveningCourses.length} 门晚上时间段的课程`);
        setHasInjectedEveningCourses(true); // 标记已经有了晚上课程，不需要再注入
      } else {
        console.log("没有找到晚上时间段的课程，注入模拟数据");
        
        // 没有找到晚上课程，注入模拟数据
        injectMockEveningCourses();
        setHasInjectedEveningCourses(true); // 标记已经注入了晚上课程
      }
    }
  }, [schedule, hasInjectedEveningCourses]);
  */

  // 注入模拟的晚上课程数据
  const injectMockEveningCourses = () => {
    if (!schedule || !schedule.details || schedule.details.length === 0) return;
    
    // 复制一个现有课程作为模板
    const templateCourse = {...schedule.details[0]};
    
    // 周一晚上19:00-20:30的课程
    const mondayEveningCourse = {
      ...templateCourse,
      courseCode: "CS601",
      courseName: "晚间编程实验室",
      teacherName: "Prof. Night",
      classroom: "Building A-101",
      day: 1, // 周一
      dayName: "Monday",
      startTime: "19:00",
      endTime: "20:30"
    };
    
    // 周二晚上21:00-22:30的课程
    const tuesdayEveningCourse = {
      ...templateCourse,
      courseCode: "CS602",
      courseName: "高级夜间编程",
      teacherName: "Prof. Moon",
      classroom: "Building B-202",
      day: 2, // 周二
      dayName: "Tuesday",
      startTime: "21:00",
      endTime: "22:30"
    };
    
    // 周四晚上19:00-20:30的课程
    const thursdayEveningCourse = {
      ...templateCourse,
      courseCode: "CS603",
      courseName: "夜间算法设计",
      teacherName: "Prof. Star",
      classroom: "Building C-303",
      day: 4, // 周四
      dayName: "Thursday",
      startTime: "19:00",
      endTime: "20:30"
    };
    
    // 添加到课程列表
    const updatedDetails = [...schedule.details, mondayEveningCourse, tuesdayEveningCourse, thursdayEveningCourse];
    
    // 创建更新后的课表对象
    const updatedSchedule = {
      ...schedule,
      details: updatedDetails
    };
    
    // 更新状态
    setSchedule(updatedSchedule);
    
    console.log("已注入模拟晚上课程数据:", [mondayEveningCourse, tuesdayEveningCourse, thursdayEveningCourse]);
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
    
    // 检查教师冲突
    const newTeacherConflicts = [];
    
    // 按教师和时间分组
    const teacherGroups = {};
    
    // 收集所有课程按教师分组
    schedule.details.forEach(item => {
      const key = `${item.teacherName}-${item.dayName}-${item.startTime}-${item.endTime}`;
      if (!teacherGroups[key]) {
        teacherGroups[key] = [];
      }
      teacherGroups[key].push(item);
    });
    
    // 只有当同一教师在同一时间段有多个课程时才认为是冲突 
    Object.entries(teacherGroups).forEach(([key, courses]) => {
      if (courses.length > 1) {
        const conflictId = `teacher-${key}-${schedule.id}`;
        // 检查这个冲突是否已经在状态中被标记为已解决
        const existingConflict = teacherConflicts.find(c => c.id === conflictId);
        const resolvedStatusInState = existingConflict ? existingConflict.status === 'Resolved' : false;
        
        newTeacherConflicts.push({
          id: conflictId, 
          type: 'Teacher Schedule',
          description: `${courses[0].teacherName} has ${courses.length} courses scheduled at the same time (${courses[0].dayName}, ${courses[0].startTime}-${courses[0].endTime})`,
          status: resolvedStatusInState ? 'Resolved' : 'Unresolved',
          involvedCourses: courses,
          scheduleId: schedule.id // 关联到特定排课方案
        });
      }
    });

    // 检查教室冲突
    const newClassroomConflicts = [];
    
    // 按教室和时间分组
    const classroomGroups = {};
    
    // 收集所有课程按教室分组
    schedule.details.forEach(item => {
      const key = `${item.classroom}-${item.dayName}-${item.startTime}-${item.endTime}`;
      if (!classroomGroups[key]) {
        classroomGroups[key] = [];
      }
      classroomGroups[key].push(item);
    });
    
    // 只有当同一教室在同一时间段有多个课程时才认为是冲突
    Object.entries(classroomGroups).forEach(([key, courses]) => {
      if (courses.length > 1) {
        const conflictId = `classroom-${key}-${schedule.id}`;
        // 检查这个冲突是否已经在状态中被标记为已解决
        const existingConflict = classroomConflicts.find(c => c.id === conflictId);
        const resolvedStatusInState = existingConflict ? existingConflict.status === 'Resolved' : false;
        
        newClassroomConflicts.push({
          id: conflictId,
          type: 'Classroom Assignment',
          description: `${courses[0].classroom} has ${courses.length} courses scheduled at the same time (${courses[0].dayName}, ${courses[0].startTime}-${courses[0].endTime})`,
          status: resolvedStatusInState ? 'Resolved' : 'Unresolved',
          involvedCourses: courses,
          scheduleId: schedule.id // 关联到特定排课方案
        });
      }
    });
    
    // 更新教师冲突状态
    setTeacherConflicts(prevConflicts => {
      // 保持已解决状态
      const updatedConflicts = newTeacherConflicts.map(conflict => {
        const existingConflict = prevConflicts.find(c => c.id === conflict.id);
        if (existingConflict && existingConflict.status === 'Resolved') {
          return { ...conflict, status: 'Resolved' };
        }
        return conflict;
      });
      return updatedConflicts;
    });
    
    // 更新教室冲突状态
    setClassroomConflicts(prevConflicts => {
      // 保持已解决状态
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
    // 创建状态更新记录
    const statusUpdate = {
      status: newStatus,
      timestamp: new Date().toISOString(),
      userId: 'Current User'
    };
    
    // 更新当前选中排课方案的状态历史
    let updatedStatusHistory = schedule.statusHistory || [];
    updatedStatusHistory = [...updatedStatusHistory, statusUpdate];
    
    // 创建新的排课对象（不可变更新）
    const updatedSchedule = {
      ...schedule, 
      status: newStatus,
      statusHistory: updatedStatusHistory
    };
    
    // 更新当前显示的排课方案
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
                      // 筛选当前时间段和星期的课程，使用更宽松的匹配条件
                      const coursesInCell = schedule.details.filter(item => {
                        // 特殊处理晚上时间段的匹配逻辑
                        if (startTime === "19:00" || startTime === "21:00") {
                          // 晚上时段的匹配采用宽松匹配，只要时间段的开头匹配即可
                          const itemStartsAt19 = item.startTime && item.startTime.startsWith("19");
                          const itemStartsAt21 = item.startTime && item.startTime.startsWith("21");
                          
                          // 因为startTime可能是"19:00"或"21:00"
                          const matchesStartTime = 
                            (startTime === "19:00" && itemStartsAt19) || 
                            (startTime === "21:00" && itemStartsAt21);
                            
                          return matchesStartTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        } else {
                          // 其他时间段仍然使用精确匹配
                          return item.startTime === startTime && 
                            item.endTime === endTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        }
                      });
                      
                      // 调试信息 - 检查晚上时间段是否有课程
                      if (startTime === "19:00" || startTime === "21:00") {
                        // 只在第一次渲染时显示日志
                        if (dayNum === 0 && !window.hasLoggedEveningSlots) {
                          console.log(`检查时间段 ${startTime}-${endTime} 的课程数量: ${coursesInCell.length}`);
                          window.hasLoggedEveningSlots = true;
                        }
                        
                        // 打印所有课程的时间，看看是否有匹配问题，但只有当找不到课程时才执行
                        if (coursesInCell.length === 0 && dayNum === 0 && !window.hasLoggedEveningMatches) {
                          const possibleMatches = schedule.details.filter(item => 
                            item.day === dayNum &&
                            (item.startTime.includes("19") || item.startTime.includes("21"))
                          );
                          
                          if (possibleMatches.length > 0) {
                            console.log(`找到可能匹配的晚上课程，但未显示在时间段 ${startTime}-${endTime} 中`);
                            window.hasLoggedEveningMatches = true;
                          }
                        }
                      }
                      
                      // 检测同一教师在同一时间的冲突
                      const hasTeacherConflict = (() => {
                        // 按教师名称分组
                        const teacherGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!teacherGroups[course.teacherName]) {
                            teacherGroups[course.teacherName] = [];
                          }
                          teacherGroups[course.teacherName].push(course);
                        });
                        
                        // 只有当同一教师在同一时间段有多个课程时才认为是冲突
                        return Object.values(teacherGroups).some(courses => courses.length > 1);
                      })();
                      
                      // 检测同一教室在同一时间的冲突
                      const hasClassroomConflict = (() => {
                        // 按教室分组
                        const classroomGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!classroomGroups[course.classroom]) {
                            classroomGroups[course.classroom] = [];
                          }
                          classroomGroups[course.classroom].push(course);
                        });
                        
                        // 只有当同一教室在同一时间段有多个课程时才认为是冲突
                        return Object.values(classroomGroups).some(courses => courses.length > 1);
                      })();
                      
                      // 是否存在任何冲突
                      const hasConflict = hasTeacherConflict || hasClassroomConflict;
                      
                      return (
                        <TableCell key={`morning-${timeSlot}-${dayNum}`} align="center" sx={{ height: 80, minWidth: 140 }}>
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
                                // 多课程显示 - 创建下拉效果
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
                                      // 打开包含所有课程的对话框
                                      setMultiCourseDialogOpen(true);
                                      setSelectedMultiCourses(coursesInCell);
                                    }}
                                    // 如果有冲突，使用红色样式
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
                      // 筛选当前时间段和星期的课程，使用更宽松的匹配条件
                      const coursesInCell = schedule.details.filter(item => {
                        // 特殊处理晚上时间段的匹配逻辑
                        if (startTime === "19:00" || startTime === "21:00") {
                          // 晚上时段的匹配采用宽松匹配，只要时间段的开头匹配即可
                          const itemStartsAt19 = item.startTime && item.startTime.startsWith("19");
                          const itemStartsAt21 = item.startTime && item.startTime.startsWith("21");
                          
                          // 因为startTime可能是"19:00"或"21:00"
                          const matchesStartTime = 
                            (startTime === "19:00" && itemStartsAt19) || 
                            (startTime === "21:00" && itemStartsAt21);
                            
                          return matchesStartTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        } else {
                          // 其他时间段仍然使用精确匹配
                          return item.startTime === startTime && 
                            item.endTime === endTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        }
                      });
                      
                      // 调试信息 - 检查晚上时间段是否有课程
                      if (startTime === "19:00" || startTime === "21:00") {
                        // 只在第一次渲染时显示日志
                        if (dayNum === 0 && !window.hasLoggedEveningSlots) {
                          console.log(`检查时间段 ${startTime}-${endTime} 的课程数量: ${coursesInCell.length}`);
                          window.hasLoggedEveningSlots = true;
                        }
                        
                        // 打印所有课程的时间，看看是否有匹配问题，但只有当找不到课程时才执行
                        if (coursesInCell.length === 0 && dayNum === 0 && !window.hasLoggedEveningMatches) {
                          const possibleMatches = schedule.details.filter(item => 
                            item.day === dayNum &&
                            (item.startTime.includes("19") || item.startTime.includes("21"))
                          );
                          
                          if (possibleMatches.length > 0) {
                            console.log(`找到可能匹配的晚上课程，但未显示在时间段 ${startTime}-${endTime} 中`);
                            window.hasLoggedEveningMatches = true;
                          }
                        }
                      }
                      
                      // 检测同一教师在同一时间的冲突
                      const hasTeacherConflict = (() => {
                        // 按教师名称分组
                        const teacherGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!teacherGroups[course.teacherName]) {
                            teacherGroups[course.teacherName] = [];
                          }
                          teacherGroups[course.teacherName].push(course);
                        });
                        
                        // 只有当同一教师在同一时间段有多个课程时才认为是冲突
                        return Object.values(teacherGroups).some(courses => courses.length > 1);
                      })();
                      
                      // 检测同一教室在同一时间的冲突
                      const hasClassroomConflict = (() => {
                        // 按教室分组
                        const classroomGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!classroomGroups[course.classroom]) {
                            classroomGroups[course.classroom] = [];
                          }
                          classroomGroups[course.classroom].push(course);
                        });
                        
                        // 只有当同一教室在同一时间段有多个课程时才认为是冲突
                        return Object.values(classroomGroups).some(courses => courses.length > 1);
                      })();
                      
                      // 是否存在任何冲突
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
                                // 多课程显示 - 创建下拉效果
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
                                      // 打开包含所有课程的对话框
                                      setMultiCourseDialogOpen(true);
                                      setSelectedMultiCourses(coursesInCell);
                                    }}
                                    // 如果有冲突，使用红色样式
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
                      // 筛选当前时间段和星期的课程，使用更宽松的匹配条件
                      const coursesInCell = schedule.details.filter(item => {
                        // 特殊处理晚上时间段的匹配逻辑
                        if (startTime === "19:00" || startTime === "21:00") {
                          // 晚上时段的匹配采用宽松匹配，只要时间段的开头匹配即可
                          const itemStartsAt19 = item.startTime && item.startTime.startsWith("19");
                          const itemStartsAt21 = item.startTime && item.startTime.startsWith("21");
                          
                          // 因为startTime可能是"19:00"或"21:00"
                          const matchesStartTime = 
                            (startTime === "19:00" && itemStartsAt19) || 
                            (startTime === "21:00" && itemStartsAt21);
                            
                          return matchesStartTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        } else {
                          // 其他时间段仍然使用精确匹配
                          return item.startTime === startTime && 
                            item.endTime === endTime && 
                            item.day === dayNum &&
                            (!item.weekSpecific || item.week === currentWeek);
                        }
                      });
                      
                      // 调试信息 - 检查晚上时间段是否有课程
                      if (startTime === "19:00" || startTime === "21:00") {
                        // 只在第一次渲染时显示日志
                        if (dayNum === 0 && !window.hasLoggedEveningSlots) {
                          console.log(`检查时间段 ${startTime}-${endTime} 的课程数量: ${coursesInCell.length}`);
                          window.hasLoggedEveningSlots = true;
                        }
                        
                        // 打印所有课程的时间，看看是否有匹配问题，但只有当找不到课程时才执行
                        if (coursesInCell.length === 0 && dayNum === 0 && !window.hasLoggedEveningMatches) {
                          const possibleMatches = schedule.details.filter(item => 
                            item.day === dayNum &&
                            (item.startTime.includes("19") || item.startTime.includes("21"))
                          );
                          
                          if (possibleMatches.length > 0) {
                            console.log(`找到可能匹配的晚上课程，但未显示在时间段 ${startTime}-${endTime} 中`);
                            window.hasLoggedEveningMatches = true;
                          }
                        }
                      }
                      
                      // 检测同一教师在同一时间的冲突
                      const hasTeacherConflict = (() => {
                        // 按教师名称分组
                        const teacherGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!teacherGroups[course.teacherName]) {
                            teacherGroups[course.teacherName] = [];
                          }
                          teacherGroups[course.teacherName].push(course);
                        });
                        
                        // 只有当同一教师在同一时间段有多个课程时才认为是冲突
                        return Object.values(teacherGroups).some(courses => courses.length > 1);
                      })();
                      
                      // 检测同一教室在同一时间的冲突
                      const hasClassroomConflict = (() => {
                        // 按教室分组
                        const classroomGroups = {};
                        
                        coursesInCell.forEach(course => {
                          if (!classroomGroups[course.classroom]) {
                            classroomGroups[course.classroom] = [];
                          }
                          classroomGroups[course.classroom].push(course);
                        });
                        
                        // 只有当同一教室在同一时间段有多个课程时才认为是冲突
                        return Object.values(classroomGroups).some(courses => courses.length > 1);
                      })();
                      
                      // 是否存在任何冲突
                      const hasConflict = hasTeacherConflict || hasClassroomConflict;
                      
                      return (
                        <TableCell key={`evening-${timeSlot}-${dayNum}`} align="center" sx={{ height: 80, minWidth: 140 }}>
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
                                // 多课程显示 - 创建下拉效果
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
                                      // 打开包含所有课程的对话框
                                      setMultiCourseDialogOpen(true);
                                      setSelectedMultiCourses(coursesInCell);
                                    }}
                                    // 如果有冲突，使用红色样式
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
            {/* 检测是否有教师冲突和教室冲突 */}
            {(() => {
              // 按教师分组
              const teacherGroups = {};
              selectedMultiCourses.forEach(course => {
                const key = `${course.teacherName}-${course.dayName}-${course.startTime}-${course.endTime}`;
                if (!teacherGroups[key]) {
                  teacherGroups[key] = [];
                }
                teacherGroups[key].push(course);
              });
              
              // 按教室分组
              const classroomGroups = {};
              selectedMultiCourses.forEach(course => {
                const key = `${course.classroom}-${course.dayName}-${course.startTime}-${course.endTime}`;
                if (!classroomGroups[key]) {
                  classroomGroups[key] = [];
                }
                classroomGroups[key].push(course);
              });
              
              // 找出真正的冲突 - 同一教师或同一教室在同一时间有多门课程
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
            {/* 如果有冲突，显示警告消息 */}
            {(() => {
              // 按教师分组
              const teacherGroups = {};
              selectedMultiCourses.forEach(course => {
                const key = `${course.teacherName}-${course.dayName}-${course.startTime}-${course.endTime}`;
                if (!teacherGroups[key]) {
                  teacherGroups[key] = [];
                }
                teacherGroups[key].push(course);
              });
              
              // 按教室分组
              const classroomGroups = {};
              selectedMultiCourses.forEach(course => {
                const key = `${course.classroom}-${course.dayName}-${course.startTime}-${course.endTime}`;
                if (!classroomGroups[key]) {
                  classroomGroups[key] = [];
                }
                classroomGroups[key].push(course);
              });
              
              // 找出真正的冲突 - 同一教师或同一教室在同一时间有多门课程
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
                // 检查当前课程是否有教师冲突
                const hasTeacherConflict = selectedMultiCourses.some(
                  otherItem => 
                    otherItem !== item && 
                    otherItem.teacherName === item.teacherName &&
                    otherItem.dayName === item.dayName &&
                    otherItem.startTime === item.startTime &&
                    otherItem.endTime === item.endTime
                );
                
                // 检查当前课程是否有教室冲突
                const hasClassroomConflict = selectedMultiCourses.some(
                  otherItem => 
                    otherItem !== item && 
                    otherItem.classroom === item.classroom &&
                    otherItem.dayName === item.dayName &&
                    otherItem.startTime === item.startTime &&
                    otherItem.endTime === item.endTime
                );
                
                // 是否存在任何冲突
                const hasConflict = hasTeacherConflict || hasClassroomConflict;
                
                return (
                  <ListItem 
                    key={idx} 
                    divider={idx < selectedMultiCourses.length - 1}
                    sx={{ 
                      borderLeft: hasConflict ? '4px solid' : 'none', 
                      borderLeftColor: hasConflict ? 'error.main' : 'transparent',
                      pl: hasConflict ? 2 : 1, // 冲突项稍微增加左边距
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
        
        {/* 解决方案确认对话框 */}
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
        
        {/* 统一的冲突分析与解决部分 */}
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

        {/* 状态历史记录对话框 */}
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