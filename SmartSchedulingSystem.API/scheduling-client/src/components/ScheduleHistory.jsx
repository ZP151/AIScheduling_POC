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
  Alert
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import CloseIcon from '@mui/icons-material/Close';
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import { mockSemesters } from '../services/mockData';
import { getScheduleHistory, publishSchedule, cancelSchedule } from '../services/api';
import { format } from 'date-fns';

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

  // Load schedule history data
  useEffect(() => {
    // Reset schedules state each time schedulesFromResults changes
    if (schedulesFromResults && schedulesFromResults.length > 0) {
      console.log("ScheduleHistory: Resetting schedules state");
      // Reset schedules state to ensure data is reprocessed
      setSchedules([]);
      
      // Process data in next event loop to ensure state reset is applied
      setTimeout(() => {
        console.log("ScheduleHistory: Processing schedules after reset");
        processSchedulesFromResults(schedulesFromResults);
      }, 0);
    } else {
      fetchScheduleHistory();
    }
  }, [schedulesFromResults]);
  
  // Ensure status changes are reflected in real-time when the selected schedule is the one being viewed
  useEffect(() => {
    if (selectedScheduleHistory && schedulesFromResults && schedulesFromResults.length > 0) {
      // Find the latest status of the selected schedule
      const updatedSchedule = schedulesFromResults.find(s => s.id === selectedScheduleHistory.id);
      
      if (updatedSchedule) {
        // If found and status has changed, update status history
        if (updatedSchedule.status !== selectedScheduleHistory.status || 
            updatedSchedule.statusHistory?.length !== selectedScheduleHistory.statusHistory?.length) {
          setSelectedScheduleHistory(updatedSchedule);
        }
      }
    }
  }, [schedulesFromResults, selectedScheduleHistory]);

  // Component loads data immediately
  useEffect(() => {
    console.log("ScheduleHistory component mounted/updated");
    
    // Prioritize processing the provided schedule results
    if (schedulesFromResults && schedulesFromResults.length > 0) {
      console.log("Processing schedules from results on mount:", schedulesFromResults.length);
      // Reset schedules state before processing data
      setSchedules([]);
      // Force processing with new data
      processSchedulesFromResults([...schedulesFromResults]);
    } else {
      // Fetch from API if no data is provided
      console.log("Fetching schedule history from API");
      fetchScheduleHistory();
    }
  }, []); // Only execute once when component mounts
  
  // Dedicated useEffect to monitor schedulesFromResults changes
  useEffect(() => {
    if (schedulesFromResults && schedulesFromResults.length > 0) {
      console.log("schedulesFromResults changed:", schedulesFromResults.length);
      console.log("Schedule statuses:", schedulesFromResults.map(s => ({id: s.id, status: s.status})));
      
      // Reset schedules state
      setSchedules([]);
      
      // Process new data after state reset
      setTimeout(() => {
        processSchedulesFromResults([...schedulesFromResults]);
      }, 0);
    }
  }, [schedulesFromResults]);

  // Process data from ScheduleResults - simplified version, no longer attempting complex merging
  const processSchedulesFromResults = (resultsData) => {
    console.log("Processing schedules from results", {
      resultsCount: resultsData.length,
      statuses: resultsData.map(s => ({id: s.id, status: s.status}))
    });
    
    // Simplified version: always rebuild grouped data
    const requestGroups = {};
    
    resultsData.forEach(schedule => {
      // Use request date as grouping identifier
      const requestId = schedule.recordId || schedule.requestId || (new Date(schedule.createdAt).toISOString().split('T')[0]);
      
      if (!requestGroups[requestId]) {
        requestGroups[requestId] = {
          requestId: requestId,
          generatedAt: schedule.recordDate || schedule.createdAt || new Date().toISOString(),
          semesterName: schedule.semesterName || 'Current Semester',
          totalSolutions: 0,
          schedules: []
        };
      }
      
      // Ensure schedule plan has correct status
      const updatedSchedule = {
        ...schedule,
        status: schedule.status || 'Generated'
      };
      
      // Ensure schedule plan has status history record
      if (!updatedSchedule.statusHistory || updatedSchedule.statusHistory.length === 0) {
        updatedSchedule.statusHistory = [{
          status: updatedSchedule.status,
          timestamp: updatedSchedule.createdAt || new Date().toISOString(),
          userId: 'System'
        }];
      }
      
      // Add schedule plan to request group
      requestGroups[requestId].schedules.push(updatedSchedule);
      requestGroups[requestId].totalSolutions += 1;
    });
    
    // Convert to array
    const groupedData = Object.values(requestGroups);
    
    // Sort by generation time in descending order
    groupedData.sort((a, b) => new Date(b.generatedAt) - new Date(a.generatedAt));
    
    console.log("Setting new schedules:", groupedData);
    
    // Update state
    setSchedules(groupedData);
    
    // If a plan is selected, update its status history
    if (selectedScheduleHistory) {
      const updatedSchedule = resultsData.find(s => s.id === selectedScheduleHistory.id);
      if (updatedSchedule) {
        setSelectedScheduleHistory({
          ...selectedScheduleHistory,
          ...updatedSchedule,
          status: updatedSchedule.status || 'Generated',
          statusHistory: updatedSchedule.statusHistory || selectedScheduleHistory.statusHistory
        });
      }
    }
  };

  const fetchScheduleHistory = async () => {
    setLoading(true);
    setError(null);
    try {
      // Temporary use default semesterId=1
      const semesterId = 1;
      const results = await getScheduleHistory(semesterId);
      setSchedules(results);
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
      
      {/* Usage Reports paragraph */}
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
                <Button variant="contained" fullWidth>Generate Report</Button>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle1" gutterBottom>
                  Faculty Workload
                </Typography>
                <Button variant="contained" fullWidth>Generate Report</Button>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle1" gutterBottom>
                  Course Demand Trends
                </Typography>
                <Button variant="contained" fullWidth>Generate Report</Button>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </Box>
    </Box>
  );
};

export default ScheduleHistory;