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
import { mockSemesters } from '../../Services/mockData';
import { getScheduleHistory, publishSchedule, cancelSchedule } from '../../Services/api';
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

  // New state for report dialog
  const [reportDialogOpen, setReportDialogOpen] = useState(false);
  const [reportType, setReportType] = useState('');

  // Load schedule history data - Simplified to single useEffect
  useEffect(() => {
    console.log("ScheduleHistory component mounted/updated");
    
    // Process incoming schedule results data
    if (schedulesFromResults && schedulesFromResults.length > 0) {
      console.log("Processing schedules from results:", schedulesFromResults.length);
      processSchedulesFromResults([...schedulesFromResults]);
    } else {
      // If no data is passed, fetch from API
      console.log("Fetching schedule history from API");
      fetchScheduleHistory();
    }
  }, [schedulesFromResults]); // Only execute when component mounts and schedulesFromResults changes

  // Simplified processing function, directly process data without additional merging
  const processSchedulesFromResults = (resultsData) => {
    console.log("Processing schedule data:", resultsData.length);
    
    // Build schedule solution groups
    const requestGroups = {};
    
    resultsData.forEach(schedule => {
      // Use requestId or creation date as group identifier
      const requestId = schedule.recordId || schedule.requestId || (new Date(schedule.createdAt).toISOString().split('T')[0]);
      
      // Create group or use existing group
      if (!requestGroups[requestId]) {
        requestGroups[requestId] = {
          requestId: requestId,
          generatedAt: schedule.recordDate || schedule.createdAt || new Date().toISOString(),
          semesterName: schedule.semesterName || 'Current Semester',
          totalSolutions: 0,
          schedules: []
        };
      }
      
      // Process schedule solution data
      const updatedSchedule = {
        ...schedule,
        status: schedule.status || 'Generated'
      };
      
      // Ensure schedule has status history
      if (!updatedSchedule.statusHistory || updatedSchedule.statusHistory.length === 0) {
        updatedSchedule.statusHistory = [{
          status: updatedSchedule.status,
          timestamp: updatedSchedule.createdAt || new Date().toISOString(),
          userId: 'System'
        }];
      }
      
      // Add schedule to group
      requestGroups[requestId].schedules.push(updatedSchedule);
      requestGroups[requestId].totalSolutions += 1;
    });
    
    // Convert to array
    const groupedData = Object.values(requestGroups);
    
    // Sort by generation time in descending order
    groupedData.sort((a, b) => new Date(b.generatedAt) - new Date(a.generatedAt));
    
    console.log("Schedule history data grouping completed:", groupedData.length);
    
    // Directly set new state
    setSchedules(groupedData);
  };

  const fetchScheduleHistory = async () => {
    setLoading(true);
    setError(null);
    try {
      // Temporary use default semesterId=1
      const semesterId = 1;
      const resultsFromApi = await getScheduleHistory(semesterId);
      
      // Merge the simulation data obtained by the API with the scheduling data in the current state
      setSchedules(prevSchedules => {
        // If there is no data before, directly use the API results
        if (!prevSchedules || prevSchedules.length === 0) {
          return resultsFromApi;
        }
        
        // Merge data: keep all existing data and add data that is not repeated in the API results
        const mergedSchedules = [...prevSchedules];
        
        // Check each record in the API results, if it does not exist in the current data, add it
        resultsFromApi.forEach(apiRecord => {
          // Check if there is a record with the same requestId
          const existingIndex = mergedSchedules.findIndex(s => s.requestId === apiRecord.requestId);
          
          if (existingIndex === -1) {
            // If it does not exist, add this record
            mergedSchedules.push(apiRecord);
          } else {
            // If there is a record with the same requestId, merge the schedules
            const existingRecord = mergedSchedules[existingIndex];
            
            // Check each schedule returned by the API, if it does not exist in the existing record, add it
            apiRecord.schedules.forEach(apiSchedule => {
              // Check if the schedule ID already exists
              const existingScheduleIndex = existingRecord.schedules.findIndex(s => s.id === apiSchedule.id);
              
              if (existingScheduleIndex === -1) {
                // If it does not exist, add this schedule
                existingRecord.schedules.push(apiSchedule);
                existingRecord.totalSolutions += 1;
              }
            });
          }
        });
        
        // Sort by generation time in descending order
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

  // Handle report generation button click
  const handleGenerateReport = (type) => {
    setReportType(type);
    setReportDialogOpen(true);
  };
  
  // Close report dialog
  const handleCloseReportDialog = () => {
    setReportDialogOpen(false);
  };
  
  // Extract classroom utilization data from schedules data
  const generateClassroomUtilizationData = () => {
    // Merge all schedule details
    const allScheduleDetails = schedules.flatMap(schedule => schedule.details || []);
    
    // Count the usage of each classroom
    const classroomUsage = {};
    allScheduleDetails.forEach(detail => {
      const classroom = detail.classroom;
      if (classroom) {
        classroomUsage[classroom] = (classroomUsage[classroom] || 0) + 1;
      }
    });
    
    // Convert to chart data format
    return Object.keys(classroomUsage).map(classroom => ({
      name: classroom,
      sessions: classroomUsage[classroom]
    })).sort((a, b) => b.sessions - a.sessions).slice(0, 10); // Only show the top 10 classrooms
  };
  
  // Extract teacher workload data from schedules data
  const generateFacultyWorkloadData = () => {
    // Merge all schedule details
    const allScheduleDetails = schedules.flatMap(schedule => schedule.details || []);
    
    // Count the teaching hours of each teacher
    const teacherWorkload = {};
    allScheduleDetails.forEach(detail => {
      const teacher = detail.teacherName;
      if (teacher) {
        // Assume each lesson is 1.5 hours
        teacherWorkload[teacher] = (teacherWorkload[teacher] || 0) + 1.5;
      }
    });
    
    // Convert to chart data format
    return Object.keys(teacherWorkload).map(teacher => ({
      name: teacher,
      hours: teacherWorkload[teacher]
    })).sort((a, b) => b.hours - a.hours).slice(0, 8); // Only show the top 8 teachers
  };
  
  // Extract course demand trend data from schedules data
  const generateCourseDemandData = () => {
    // Merge all schedule details
    const allScheduleDetails = schedules.flatMap(schedule => schedule.details || []);
    
    // Group by course code
    const courseCounts = {};
    allScheduleDetails.forEach(detail => {
      const courseCode = detail.courseCode;
      if (courseCode) {
        courseCounts[courseCode] = (courseCounts[courseCode] || 0) + 1;
      }
    });
    
    // Group by subject (assuming the first two characters of the course code represent the subject)
    const subjectCounts = {};
    Object.keys(courseCounts).forEach(code => {
      const subject = code.substring(0, 2);
      subjectCounts[subject] = (subjectCounts[subject] || 0) + courseCounts[code];
    });
    
    // Convert to pie chart data format
    return Object.keys(subjectCounts).map(subject => ({
      name: subject,
      value: subjectCounts[subject]
    }));
  };
  
  // Render content for different types of report dialogs
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