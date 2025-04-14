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
import { mockTimeSlots } from '../services/mockData';
import Alert from '@mui/material/Alert';

// 导入真实API服务
import { getScheduleById, getScheduleHistory } from '../services/api';

import ScheduleExplanation from './LLM/ScheduleExplanation';
import ConflictResolution from './LLM/ConflictResolution';


const ScheduleResults = ({ scheduleId, scheduleResults, onBack }) => {
  const [schedule, setSchedule] = useState(null);
  const [selectedScheduleId, setSelectedScheduleId] = useState(scheduleId);
  const [resultsTabValue, setResultsTabValue] = useState(0);
  // In ScheduleResults.jsx, add new state for week selection
  const [currentWeek, setCurrentWeek] = useState(1);
  const [totalWeeks, setTotalWeeks] = useState(16); // Default to 16 weeks in a semester
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [availableSchedules, setAvailableSchedules] = useState([]);

  const handleConflictResolved = (solution, conflictId) => {
    console.log(`Applying solution to conflict ${conflictId}:`, solution);
    // In a real app, we would call an API to apply the solution
    // For now, just update the local state
    setConflicts(prevConflicts => 
      prevConflicts.map(conflict => 
        conflict.id === conflictId 
          ? { ...conflict, status: 'Resolved' } 
          : conflict
      )
    );
    alert(`Solution applied to conflict ${conflictId}`);
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
      
      // 如果是第一次加载并且没有可用排课列表，获取历史数据
      if (availableSchedules.length === 0) {
        // 在实际项目中，我们可能需要根据当前学期ID获取历史记录
        // 这里暂时使用一个固定值
        const semesterId = 1;
        const historyData = await getScheduleHistory(semesterId);
        setAvailableSchedules(historyData);
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
      const selectedResult = scheduleResults.find(r => r.id === selectedScheduleId) || scheduleResults[0];
      setSchedule(selectedResult);
      setAvailableSchedules(scheduleResults);
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
  // Add this at the end of the component body
  const [multiCourseDialogOpen, setMultiCourseDialogOpen] = useState(false);
  const [selectedMultiCourses, setSelectedMultiCourses] = useState([]);

  // Add fallback content when no schedule data is available
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
          <Typography>加载排课数据中...</Typography>
        </Box>
      )}
      
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}
      
      {/* 排课选择下拉框 */}
      <Box sx={{ mb: 3 }}>
        <FormControl fullWidth>
          <InputLabel>选择排课方案</InputLabel>
          <Select
            value={selectedScheduleId || ''}
            onChange={(e) => setSelectedScheduleId(e.target.value)}
            label="选择排课方案"
          >
            {availableSchedules.map(result => (
              <MenuItem key={result.id} value={result.id}>
                {result.name} {result.status === 'Draft' ? '(草稿)' : ''}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>

      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', mb: 2 }}>
        <Typography variant="h6" gutterBottom>
          {schedule.name}
        </Typography>
        <Chip 
          label={schedule.status} 
          color={schedule.status === 'Published' ? 'success' : 'warning'} 
          variant="outlined" 
        />
      </Box>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
        <Tabs 
          value={resultsTabValue} 
          onChange={handleResultsTabChange}
          aria-label="schedule results tabs"
          variant="scrollable"
          scrollButtons="auto"
        >

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

          <Tab label="Calendar View" />
          <Tab label="List View" />
          <Tab label="By Teacher" />
          <Tab label="By Classroom" />
        </Tabs>
      </Box>
      
      {/* Conflicts Section */}
      {conflicts.length > 0 && (
        <Box sx={{ mb: 3 }}>
          <Typography variant="subtitle1" gutterBottom>
            Detected Conflicts ({conflicts.filter(c => c.status !== 'Resolved').length})
          </Typography>
          
          {conflicts.map(conflict => (
            <ConflictResolution 
              key={conflict.id} 
              conflict={conflict} 
              onResolve={handleConflictResolved}
            />
          ))}
          
          {conflicts.every(c => c.status === 'Resolved') && (
            <Alert severity="success" sx={{ mt: 2 }}>
              All conflicts have been resolved!
            </Alert>
          )}
        </Box>
      )}

      {/* Calendar View */}
      {resultsTabValue === 0 && (
        <TableContainer component={Paper} variant="outlined">
          <Table>
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 'bold' }}>Time/Room</TableCell>
                {uniqueClassrooms.map(classroom => (
                  <TableCell key={classroom} align="center" sx={{ fontWeight: 'bold' }}>
                    {classroom}
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {Object.values(timeTableData)
                .sort((a, b) => a.day - b.day || a.startTime.localeCompare(b.startTime))
                .map((timeRow) => (
                  <TableRow key={`${timeRow.day}-${timeRow.startTime}`}>
                    <TableCell sx={{ whiteSpace: 'nowrap', fontWeight: 'bold' }}>
                      {timeRow.dayName}<br />
                      {timeRow.startTime}-{timeRow.endTime}
                    </TableCell>
                    {uniqueClassrooms.map(classroom => {
                      // 使用当前行的时间和日期，而不是未定义的变量
                      const scheduleItems = timeRow.classrooms[classroom] ? [timeRow.classrooms[classroom]] : [];
                      
                      // 如果需要根据周过滤，可以这样做
                      const filteredItems = scheduleItems.filter(item => 
                        !item.weekSpecific || item.week === currentWeek
                      );
                      
                      return (
                        <TableCell key={classroom} align="center" sx={{ height: 80, minWidth: 140 }}>
                          {filteredItems.length > 0 ? (
                            <Box>
                              {filteredItems.length === 1 ? (
                                // Single course display
                                <>
                                  <Typography variant="body2" fontWeight="bold">
                                    {filteredItems[0].courseName}
                                  </Typography>
                                  <Typography variant="caption" display="block">
                                    {filteredItems[0].teacherName}
                                  </Typography>
                                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mt: 0.5 }}>
                                    <Chip 
                                      size="small" 
                                      label={filteredItems[0].courseCode} 
                                      color="primary" 
                                      variant="outlined" 
                                    />
                                    <ScheduleExplanation scheduleItem={filteredItems[0]} />
                                  </Box>
                                </>
                              ) : (
                                // Multiple courses - create dropdown effect
                                <Tooltip 
                                  title={
                                    <Box>
                                      {filteredItems.map((item, idx) => (
                                        <Box key={idx} sx={{ mb: idx < filteredItems.length - 1 ? 1 : 0, p: 1, borderBottom: idx < filteredItems.length - 1 ? '1px solid #eee' : 'none' }}>
                                          <Typography variant="body2" fontWeight="bold">
                                            {item.courseName} ({item.courseCode})
                                          </Typography>
                                          <Typography variant="caption" display="block">
                                            Teacher: {item.teacherName}
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
                                      // Open a dialog with all courses
                                      setMultiCourseDialogOpen(true);
                                      setSelectedMultiCourses(filteredItems);
                                    }}
                                  >
                                    {filteredItems.length} Courses
                                  </Button>
                                </Tooltip>
                              )}
                            </Box>
                          ) : null}
                        </TableCell>
                      );
                    })}
                  </TableRow>
                ))}
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
            Multiple Courses
            <IconButton
              aria-label="close"
              onClick={() => setMultiCourseDialogOpen(false)}
              sx={{ position: 'absolute', right: 8, top: 8 }}
            >
              <CloseIcon />
            </IconButton>
          </DialogTitle>
          <DialogContent>
            <List>
              {selectedMultiCourses.map((item, idx) => (
                <ListItem key={idx} divider={idx < selectedMultiCourses.length - 1}>
                  <ListItemText
                    primary={
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Typography variant="subtitle1">
                          {item.courseName} ({item.courseCode})
                        </Typography>
                        <ScheduleExplanation scheduleItem={item} />
                      </Box>
                    }
                    secondary={
                      <>
                        <Typography variant="body2">Teacher: {item.teacherName}</Typography>
                        <Typography variant="body2">Time: {item.dayName} {item.startTime}-{item.endTime}</Typography>
                        <Typography variant="body2">Classroom: {item.classroom}</Typography>
                      </>
                    }
                  />
                </ListItem>
              ))}
            </List>
          </DialogContent>
        </Dialog>
    </Box>

    
  );
};

export default ScheduleResults;