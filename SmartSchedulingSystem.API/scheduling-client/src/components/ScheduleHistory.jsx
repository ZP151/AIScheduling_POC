import React, { useState, useEffect } from 'react';
import { 
  Box, 
  Typography, 
  TableContainer, 
  Table, 
  TableHead, 
  TableBody, 
  TableRow, 
  TableCell, 
  Paper, 
  Chip, 
  Button,
  Grid,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Card,
  CardContent,
  CircularProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  List,
  ListItem,
  ListItemText,
  Dialog,
  DialogTitle,
  DialogContent,
  IconButton,
  Tooltip,
  Alert,
  DialogActions
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import CloseIcon from '@mui/icons-material/Close';
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import { mockSemesters } from '../services/mockData';
import { getScheduleHistory, publishSchedule, cancelSchedule } from '../services/api';
import { format } from 'date-fns';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip as RechartsTooltip, Legend, PieChart, Pie, Cell, LineChart, Line } from 'recharts';

const ScheduleHistory = ({ onHistoryItemClick, schedulesFromResults = [] }) => {
  const [filters, setFilters] = useState({
    startDate: '',
    endDate: '',
    semester: '',
    status: '',
    searchTerm: '',
    searchBy: 'all'  // 'all', 'course', 'teacher', 'classroom'
  });

  const [schedules, setSchedules] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  
  // Status history dialog state
  const [statusHistoryOpen, setStatusHistoryOpen] = useState(false);
  const [selectedScheduleHistory, setSelectedScheduleHistory] = useState(null);

  // 新增状态用于报告对话框
  const [reportDialogOpen, setReportDialogOpen] = useState(false);
  const [reportType, setReportType] = useState('');

  // Load schedule history data - 简化为单一useEffect
  useEffect(() => {
    console.log("ScheduleHistory component mounted/updated");
    
    // 处理传入的排课结果数据
    if (schedulesFromResults && schedulesFromResults.length > 0) {
      console.log("Processing schedules from results:", schedulesFromResults.length);
      processSchedulesFromResults([...schedulesFromResults]);
    } else {
      // 没有传入数据则从API获取
      console.log("Fetching schedule history from API");
      fetchScheduleHistory();
    }
  }, [schedulesFromResults]); // 只在组件挂载和schedulesFromResults变化时执行

  // 简化处理函数，直接处理数据不做额外合并
  const processSchedulesFromResults = (resultsData) => {
    console.log("处理排课数据:", resultsData.length);
    
    // 构建排课方案的分组
    const requestGroups = {};
    
    resultsData.forEach(schedule => {
      // 使用requestId或创建日期作为分组标识符
      const requestId = schedule.recordId || schedule.requestId || (new Date(schedule.createdAt).toISOString().split('T')[0]);
      
      // 创建分组或使用现有分组
      if (!requestGroups[requestId]) {
        requestGroups[requestId] = {
          requestId: requestId,
          generatedAt: schedule.recordDate || schedule.createdAt || new Date().toISOString(),
          semesterName: schedule.semesterName || 'Current Semester',
          totalSolutions: 0,
          schedules: []
        };
      }
      
      // 处理排课方案数据
      const updatedSchedule = {
        ...schedule,
        status: schedule.status || 'Generated'
      };
      
      // 确保排课方案有状态历史记录
      if (!updatedSchedule.statusHistory || updatedSchedule.statusHistory.length === 0) {
        updatedSchedule.statusHistory = [{
          status: updatedSchedule.status,
          timestamp: updatedSchedule.createdAt || new Date().toISOString(),
          userId: 'System'
        }];
      }
      
      // 添加排课方案到分组
      requestGroups[requestId].schedules.push(updatedSchedule);
      requestGroups[requestId].totalSolutions += 1;
    });
    
    // 转换为数组
    const groupedData = Object.values(requestGroups);
    
    // 按生成时间降序排序
    groupedData.sort((a, b) => new Date(b.generatedAt) - new Date(a.generatedAt));
    
    console.log("排课历史数据分组完成:", groupedData.length);
    
    // 直接设置新状态
    setSchedules(groupedData);
  };

  const fetchScheduleHistory = async () => {
    setLoading(true);
    setError(null);
    try {
      // Temporary use default semesterId=1
      const semesterId = 1;
      const resultsFromApi = await getScheduleHistory(semesterId);
      
      // 合并API获取的模拟数据与当前状态中的排课数据
      setSchedules(prevSchedules => {
        // 如果之前没有数据，直接使用API结果
        if (!prevSchedules || prevSchedules.length === 0) {
          return resultsFromApi;
        }
        
        // 合并数据：保留所有现有数据，并添加API结果中不重复的数据
        const mergedSchedules = [...prevSchedules];
        
        // 检查API结果中每条记录，如果不存在于当前数据中则添加
        resultsFromApi.forEach(apiRecord => {
          // 检查是否存在相同requestId的记录
          const existingIndex = mergedSchedules.findIndex(s => s.requestId === apiRecord.requestId);
          
          if (existingIndex === -1) {
            // 如果不存在，添加这条记录
            mergedSchedules.push(apiRecord);
          } else {
            // 如果存在相同requestId的记录，合并其中的schedules
            const existingRecord = mergedSchedules[existingIndex];
            
            // 检查API返回的每个排课方案，如果不存在于现有记录中则添加
            apiRecord.schedules.forEach(apiSchedule => {
              // 检查排课方案ID是否已存在
              const existingScheduleIndex = existingRecord.schedules.findIndex(s => s.id === apiSchedule.id);
              
              if (existingScheduleIndex === -1) {
                // 如果不存在，添加这个排课方案
                existingRecord.schedules.push(apiSchedule);
                existingRecord.totalSolutions += 1;
              }
            });
          }
        });
        
        // 按生成时间降序排序
        mergedSchedules.sort((a, b) => new Date(b.generatedAt) - new Date(a.generatedAt));
        
        return mergedSchedules;
      });
    } catch (err) {
      console.error('Failed to retrieve schedule history:', err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString) => {
    try {
      const date = new Date(dateString);
      return format(date, 'MMM d, yyyy h:mm a');
    } catch (e) {
      return dateString;
    }
  };

  const handleViewSchedule = (scheduleId) => {
    if (onHistoryItemClick) {
      onHistoryItemClick(scheduleId);
    }
  };

  const handlePublishSchedule = async (scheduleId) => {
    try {
      await publishSchedule(scheduleId);
      // Refresh data
      fetchScheduleHistory();
    } catch (error) {
      alert(`Failed to publish schedule: ${error.message}`);
    }
  };

  const handleCancelSchedule = async (scheduleId) => {
    try {
      await cancelSchedule(scheduleId);
      // Refresh data
      fetchScheduleHistory();
    } catch (error) {
      alert(`Failed to cancel schedule: ${error.message}`);
    }
  };
  
  // Open status history dialog
  const handleOpenStatusHistory = (scheduleToView) => {
    // Find the latest schedule data (possibly updated in schedulesFromResults)
    let latestScheduleData = null;
    
    // First search in schedulesFromResults
    if (schedulesFromResults && schedulesFromResults.length > 0) {
      latestScheduleData = schedulesFromResults.find(s => s.id === scheduleToView.id);
    }
    
    // If not found, use original data
    const updatedSchedule = latestScheduleData || scheduleToView;
    
    // Ensure status history exists
    if (!updatedSchedule.statusHistory) {
      updatedSchedule.statusHistory = [{
        status: updatedSchedule.status || 'Draft',
        timestamp: updatedSchedule.createdAt || new Date().toISOString(),
        userId: 'System'
      }];
    }
    
    setSelectedScheduleHistory(updatedSchedule);
    setStatusHistoryOpen(true);
  };

  // Close status history dialog
  const handleCloseStatusHistory = () => {
    setStatusHistoryOpen(false);
    setSelectedScheduleHistory(null);
  };

  // Add function to handle filter changes
  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters({
      ...filters,
      [name]: value
    });
  };

  // Filter function - modified to handle grouped data
  const filterSchedules = () => {
    return schedules.filter(record => {
      // Check record's certain attributes, such as generation date or related semester
      // Date range filter
      if (filters.startDate) {
        const startDate = new Date(filters.startDate);
        const recordDate = new Date(record.generatedAt);
        if (recordDate < startDate) return false;
      }
      
      if (filters.endDate) {
        const endDate = new Date(filters.endDate);
        endDate.setHours(23, 59, 59);
        const recordDate = new Date(record.generatedAt);
        if (recordDate > endDate) return false;
      }
      
      // Semester name filter
      if (filters.semester && record.semesterName !== filters.semester) {
        return false;
      }
      
      // Internal schedule plan status filter
      if (filters.status) {
        // Check if any schedule plan matches selected status
        const hasMatchingStatus = record.schedules.some(
          schedule => schedule.status === filters.status
        );
        if (!hasMatchingStatus) return false;
      }
      
      // Search term filter
      if (filters.searchTerm) {
        const searchTerm = filters.searchTerm.toLowerCase();
        
        // Filter based on search method
        if (filters.searchBy === 'name' || filters.searchBy === 'all') {
          // Search request ID and semester name
          if (record.requestId.toString().toLowerCase().includes(searchTerm) || 
              record.semesterName.toLowerCase().includes(searchTerm)) {
            return true;
          }
        }
        
        // Search within each schedule plan
        return record.schedules.some(schedule => {
          if (filters.searchBy === 'name' || filters.searchBy === 'all') {
            // Schedule name search
            if (schedule.name && schedule.name.toLowerCase().includes(searchTerm)) {
              return true;
            }
          }
          
          // Search within course, teacher, or classroom
          if (schedule.details && schedule.details.length > 0) {
            return schedule.details.some(item => {
              if (filters.searchBy === 'course' || filters.searchBy === 'all') {
                if ((item.courseCode && item.courseCode.toLowerCase().includes(searchTerm)) ||
                    (item.courseName && item.courseName.toLowerCase().includes(searchTerm))) {
                  return true;
                }
              }
              
              if (filters.searchBy === 'teacher' || filters.searchBy === 'all') {
                if (item.teacherName && item.teacherName.toLowerCase().includes(searchTerm)) {
                  return true;
                }
              }
              
              if (filters.searchBy === 'classroom' || filters.searchBy === 'all') {
                if (item.classroom && item.classroom.toLowerCase().includes(searchTerm)) {
                  return true;
                }
              }
              
              return false;
            });
          }
          
          return false;
        });
      }
      
      // Pass all filter conditions
      return true;
    });
  };

  // 处理报告生成按钮点击
  const handleGenerateReport = (type) => {
    setReportType(type);
    setReportDialogOpen(true);
  };
  
  // 关闭报告对话框
  const handleCloseReportDialog = () => {
    setReportDialogOpen(false);
  };
  
  // 从schedules数据中提取教室利用率数据
  const generateClassroomUtilizationData = () => {
    // 合并所有方案的课程安排
    const allScheduleDetails = schedules.flatMap(schedule => schedule.details || []);
    
    // 统计每个教室的使用次数
    const classroomUsage = {};
    allScheduleDetails.forEach(detail => {
      const classroom = detail.classroom;
      if (classroom) {
        classroomUsage[classroom] = (classroomUsage[classroom] || 0) + 1;
      }
    });
    
    // 转换为图表数据格式
    return Object.keys(classroomUsage).map(classroom => ({
      name: classroom,
      sessions: classroomUsage[classroom]
    })).sort((a, b) => b.sessions - a.sessions).slice(0, 10); // 只显示使用最多的10个教室
  };
  
  // 从schedules数据中提取教师工作负载数据
  const generateFacultyWorkloadData = () => {
    // 合并所有方案的课程安排
    const allScheduleDetails = schedules.flatMap(schedule => schedule.details || []);
    
    // 统计每个教师的教学课时
    const teacherWorkload = {};
    allScheduleDetails.forEach(detail => {
      const teacher = detail.teacherName;
      if (teacher) {
        // 假设每节课1.5小时
        teacherWorkload[teacher] = (teacherWorkload[teacher] || 0) + 1.5;
      }
    });
    
    // 转换为图表数据格式
    return Object.keys(teacherWorkload).map(teacher => ({
      name: teacher,
      hours: teacherWorkload[teacher]
    })).sort((a, b) => b.hours - a.hours).slice(0, 8); // 只显示工作量最大的8位教师
  };
  
  // 从schedules数据中提取课程需求趋势数据
  const generateCourseDemandData = () => {
    // 合并所有方案的课程安排
    const allScheduleDetails = schedules.flatMap(schedule => schedule.details || []);
    
    // 按课程代码分组
    const courseCounts = {};
    allScheduleDetails.forEach(detail => {
      const courseCode = detail.courseCode;
      if (courseCode) {
        courseCounts[courseCode] = (courseCounts[courseCode] || 0) + 1;
      }
    });
    
    // 按学科分组（假设课程代码的前两个字符表示学科）
    const subjectCounts = {};
    Object.keys(courseCounts).forEach(code => {
      const subject = code.substring(0, 2);
      subjectCounts[subject] = (subjectCounts[subject] || 0) + courseCounts[code];
    });
    
    // 转换为饼图数据格式
    return Object.keys(subjectCounts).map(subject => ({
      name: subject,
      value: subjectCounts[subject]
    }));
  };
  
  // 渲染不同类型的报告对话框内容
  const renderReportContent = () => {
    switch (reportType) {
      case 'classroom':
        const classroomData = generateClassroomUtilizationData();
        return (
          <>
            <Typography variant="h6" gutterBottom>Classroom Utilization Report</Typography>
            <Typography variant="body2" paragraph>
              This report shows the most frequently used classrooms across all schedules.
            </Typography>
            <Box sx={{ width: '100%', height: 400 }}>
              <BarChart width={550} height={350} data={classroomData}
                margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis label={{ value: 'Sessions', angle: -90, position: 'insideLeft' }} />
                <RechartsTooltip />
                <Legend />
                <Bar dataKey="sessions" fill="#8884d8" name="Number of Sessions" />
              </BarChart>
            </Box>
          </>
        );
      
      case 'faculty':
        const facultyData = generateFacultyWorkloadData();
        return (
          <>
            <Typography variant="h6" gutterBottom>Faculty Workload Report</Typography>
            <Typography variant="body2" paragraph>
              This report shows the teaching hours distribution among faculty members.
            </Typography>
            <Box sx={{ width: '100%', height: 400 }}>
              <BarChart width={550} height={350} data={facultyData}
                margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis label={{ value: 'Hours', angle: -90, position: 'insideLeft' }} />
                <RechartsTooltip />
                <Legend />
                <Bar dataKey="hours" fill="#82ca9d" name="Teaching Hours" />
              </BarChart>
            </Box>
          </>
        );
      
      case 'course':
        const courseData = generateCourseDemandData();
        const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884d8', '#82ca9d', '#ffc658', '#8dd1e1'];
        return (
          <>
            <Typography variant="h6" gutterBottom>Course Demand Trends</Typography>
            <Typography variant="body2" paragraph>
              This report shows the distribution of courses by subject area.
            </Typography>
            <Box sx={{ width: '100%', height: 400, display: 'flex', justifyContent: 'center' }}>
              <PieChart width={400} height={350}>
                <Pie
                  data={courseData}
                  cx={200}
                  cy={150}
                  labelLine={true}
                  outerRadius={120}
                  fill="#8884d8"
                  dataKey="value"
                  label={({name, percent}) => `${name} ${(percent * 100).toFixed(0)}%`}
                >
                  {courseData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <RechartsTooltip formatter={(value, name, props) => [`${value} sessions`, `Subject: ${props.payload.name}`]} />
                <Legend />
              </PieChart>
            </Box>
          </>
        );
      
      default:
        return <Typography>No report data available</Typography>;
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        Schedule History
      </Typography>
      
      {/* Search and filter panel */}
      <Paper sx={{ p: 2, mb: 2 }}>
        <Grid container spacing={2}>
          <Grid item xs={12}>
            <Typography variant="subtitle1">Search and Filter</Typography>
          </Grid>
          
          {/* Search box */}
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Search"
              name="searchTerm"
              value={filters.searchTerm}
              onChange={handleFilterChange}
              placeholder="Search by name, course, teacher or classroom"
            />
          </Grid>
          
          {/* Search type selection */}
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>Search Type</InputLabel>
              <Select
                name="searchBy"
                value={filters.searchBy}
                onChange={handleFilterChange}
                label="Search Type"
              >
                <MenuItem value="all">All Fields</MenuItem>
                <MenuItem value="name">Schedule Name</MenuItem>
                <MenuItem value="course">Course</MenuItem>
                <MenuItem value="teacher">Teacher</MenuItem>
                <MenuItem value="classroom">Classroom</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          
          {/* Date range filter - using standard HTML date input */}
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Start Date"
              type="date"
              name="startDate"
              value={filters.startDate}
              onChange={handleFilterChange}
              InputLabelProps={{ shrink: true }}
              inputProps={{ lang: 'en-US' }}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="End Date"
              type="date"
              name="endDate"
              value={filters.endDate}
              onChange={handleFilterChange}
              InputLabelProps={{ shrink: true }}
              inputProps={{ lang: 'en-US' }}
            />
          </Grid>
          
          {/* Semester filter */}
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>Semester</InputLabel>
              <Select
                name="semester"
                value={filters.semester}
                onChange={handleFilterChange}
                label="Semester"
              >
                <MenuItem value="">All Semesters</MenuItem>
                {mockSemesters.map(semester => (
                  <MenuItem key={semester.id} value={semester.name}>
                    {semester.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          
          {/* Status filter */}
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>Status</InputLabel>
              <Select
                name="status"
                value={filters.status}
                onChange={handleFilterChange}
                label="Status"
              >
                <MenuItem value="">All Status</MenuItem>
                <MenuItem value="Draft">Draft</MenuItem>
                <MenuItem value="Generated">Generated</MenuItem>
                <MenuItem value="Published">Published</MenuItem>
                <MenuItem value="Canceled">Canceled</MenuItem>
                <MenuItem value="Archived">Archived</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          
          {/* Reset button */}
          <Grid item xs={12}>
            <Button 
              variant="outlined" 
              onClick={() => setFilters({
                startDate: '',
                endDate: '',
                semester: '',
                status: '',
                searchTerm: '',
                searchBy: 'all'
              })}
            >
              Reset Filters
            </Button>
          </Grid>
        </Grid>
      </Paper>
      
      {/* Loading state display */}
      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', my: 3 }}>
          <CircularProgress />
        </Box>
      )}
      
      {/* Error message display */}
      {error && (
        <Box sx={{ mb: 2, p: 2, bgcolor: 'error.light', borderRadius: 1 }}>
          <Typography color="error">
            Failed to load schedule history: {error}
          </Typography>
        </Box>
      )}
      
      {/* Filtered results list - grouped display mode */}
      {!loading && !error && (
        <Box>
          {filterSchedules().map((record, index) => (
            <Accordion key={record.requestId} defaultExpanded={index === 0}>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', width: '100%', alignItems: 'center' }}>
                  <Typography variant="subtitle1" fontWeight="bold">
                    {record.semesterName} - Request #{record.requestId}
                  </Typography>
                  <Box>
                    <Typography variant="body2" color="text.secondary">
                      {formatDate(record.generatedAt)} 
                      <Chip 
                        size="small" 
                        label={`${record.totalSolutions} Solutions`} 
                        sx={{ ml: 1 }} 
                        variant="outlined" 
                      />
                    </Typography>
                  </Box>
                </Box>
              </AccordionSummary>
              <AccordionDetails>
                <TableContainer component={Paper} variant="outlined">
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Schedule ID</TableCell>
                        <TableCell>Name</TableCell>
                        <TableCell>Status</TableCell>
                        <TableCell>Created On</TableCell>
                        <TableCell>Score</TableCell>
                        <TableCell>Actions</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {record.schedules.map((schedule) => {
                        // Calculate latest status (based on timestamp)
                        const currentStatus = schedule.statusHistory && schedule.statusHistory.length > 0 
                          ? [...schedule.statusHistory].sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp))[0].status
                          : schedule.status || 'Draft';
                          
                        return (
                          <TableRow key={schedule.id}>
                            <TableCell>{schedule.id}</TableCell>
                            <TableCell>
                              {schedule.name}
                              {schedule.isPrimary && (
                                <Chip 
                                  size="small" 
                                  label="Primary" 
                                  color="primary" 
                                  variant="outlined" 
                                  sx={{ ml: 1 }} 
                                />
                              )}
                            </TableCell>
                            <TableCell>
                              <Chip 
                                label={currentStatus === 'Draft' ? 'Draft' : 
                                      currentStatus === 'Generated' ? 'Generated' :
                                      currentStatus === 'Published' ? 'Published' : 
                                      currentStatus === 'Canceled' ? 'Canceled' :
                                      currentStatus === 'Archived' ? 'Archived' : 'Unknown'} 
                                size="small"
                                color={
                                  currentStatus === 'Published' ? 'success' : 
                                  currentStatus === 'Draft' ? 'warning' : 
                                  currentStatus === 'Generated' ? 'info' :
                                  currentStatus === 'Canceled' ? 'error' :
                                  currentStatus === 'Archived' ? 'default' : 'default'
                                } 
                                variant="outlined" 
                              />
                            </TableCell>
                            <TableCell>{formatDate(schedule.createdAt)}</TableCell>
                            <TableCell>{schedule.score ? `${(schedule.score * 100).toFixed(0)}%` : '-'}</TableCell>
                            <TableCell>
                              <Box sx={{ display: 'flex', gap: 1 }}>
                                <Button 
                                  size="small" 
                                  variant="outlined"
                                  onClick={() => handleViewSchedule(schedule.id)}
                                >
                                  View
                                </Button>
                                {schedule.statusHistory && schedule.statusHistory.length > 0 && (
                                  <Tooltip title="View Status History">
                                    <IconButton 
                                      size="small" 
                                      onClick={() => handleOpenStatusHistory(schedule)}
                                    >
                                      <AccessTimeIcon fontSize="small" />
                                    </IconButton>
                                  </Tooltip>
                                )}
                              </Box>
                            </TableCell>
                          </TableRow>
                        );
                      })}
                    </TableBody>
                  </Table>
                </TableContainer>
              </AccordionDetails>
            </Accordion>
          ))}
        </Box>
      )}
      
      {!loading && !error && filterSchedules().length === 0 && (
        <Box sx={{ textAlign: 'center', py: 4 }}>
          <Typography variant="body1" color="text.secondary">
            No matching schedules found. Please try adjusting your filters.
          </Typography>
        </Box>
      )}
      
      {/* Status history record dialog */}
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
            <Alert severity="info">No status history found for this schedule.</Alert>
          ) : (
            <List>
              {/* Sort status history by time in descending order, latest status displayed at the top */}
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
                          label={statusChange.status === 'Draft' ? 'Draft' : 
                                statusChange.status === 'Generated' ? 'Generated' :
                                statusChange.status === 'Published' ? 'Published' : 
                                statusChange.status === 'Canceled' ? 'Canceled' :
                                statusChange.status === 'Archived' ? 'Archived' : 'Unknown'} 
                          size="small"
                          color={
                            statusChange.status === 'Published' ? 'success' : 
                            statusChange.status === 'Draft' ? 'warning' : 
                            statusChange.status === 'Generated' ? 'info' :
                            statusChange.status === 'Canceled' ? 'error' :
                            statusChange.status === 'Archived' ? 'default' : 'default'
                          } 
                          variant="outlined" 
                          sx={{ mr: 1 }}
                        />
                        <Typography variant="subtitle2">
                          {statusChange.status === 'Draft' ? 'Draft' : 
                          statusChange.status === 'Generated' ? 'Generated' :
                          statusChange.status === 'Published' ? 'Published' : 
                          statusChange.status === 'Canceled' ? 'Canceled' :
                          statusChange.status === 'Archived' ? 'Archived' : 'Unknown'}
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
      
      {/* Usage Reports section */}
      <Box sx={{ mt: 4 }}>
        <Typography variant="h6" gutterBottom>
          Usage Reports
        </Typography>
        <Grid container spacing={2}>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle1" gutterBottom>
                  Classroom Utilization
                </Typography>
                <Button 
                  variant="contained" 
                  fullWidth
                  onClick={() => handleGenerateReport('classroom')}
                >
                  Generate Report
                </Button>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle1" gutterBottom>
                  Faculty Workload
                </Typography>
                <Button 
                  variant="contained" 
                  fullWidth
                  onClick={() => handleGenerateReport('faculty')}
                >
                  Generate Report
                </Button>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle1" gutterBottom>
                  Course Demand Trends
                </Typography>
                <Button 
                  variant="contained" 
                  fullWidth
                  onClick={() => handleGenerateReport('course')}
                >
                  Generate Report
                </Button>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </Box>
      
      {/* Report Dialog */}
      <Dialog
        open={reportDialogOpen}
        onClose={handleCloseReportDialog}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>
          {reportType === 'classroom' && 'Classroom Utilization Report'}
          {reportType === 'faculty' && 'Faculty Workload Report'}
          {reportType === 'course' && 'Course Demand Trends Report'}
        </DialogTitle>
        <DialogContent dividers>
          {renderReportContent()}
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseReportDialog}>Close</Button>
          <Button color="primary">Export as PDF</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default ScheduleHistory;