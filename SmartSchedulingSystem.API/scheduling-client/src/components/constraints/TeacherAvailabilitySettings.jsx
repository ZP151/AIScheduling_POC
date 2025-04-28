// TeacherAvailabilitySettings.jsx
import React, { useState, useEffect } from 'react';
import { 
  Box, 
  Typography, 
  FormControl, 
  InputLabel, 
  Select, 
  MenuItem, 
  Paper, 
  Table, 
  TableBody, 
  TableCell, 
  TableContainer, 
  TableHead, 
  TableRow, 
  Switch,
  Tab,
  Tabs,
  TextField,
  Button,
  Grid,
  FormControlLabel,
  Chip,
  IconButton,
  Tooltip
} from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';

// Remove the date pickers for now to avoid the date-fns error
// We'll use simple text fields instead

const TeacherAvailabilitySettings = ({ 
  teachers = [], // Provide default empty array
  availabilitySettings: externalAvailabilitySettings = {}, // Provide default empty object
  semesterId = 1, // Provide default value
  onSettingsChange 
}) => {
  // First define all state variables
  // Use local state to manage availability settings
  const [localAvailability, setLocalAvailability] = useState(externalAvailabilitySettings);
  const [selectedTeacher, setSelectedTeacher] = useState('');
  const [selectedDay, setSelectedDay] = useState('');
  const [selectedTimeSlot, setSelectedTimeSlot] = useState('');
  const [isEditing, setIsEditing] = useState(false);
  const [editMode, setEditMode] = useState('add'); // 'add' or 'remove'
  const [feedback, setFeedback] = useState({ open: false, message: '', type: 'info' });
  
  const [weekMode, setWeekMode] = useState('regular');
  const [weekSets, setWeekSets] = useState([
    { id: 1, name: 'Regular Weeks', weeks: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14] },
    { id: 2, name: 'Exam Weeks', weeks: [15, 16] },
    { id: 3, name: 'Special Weeks', weeks: [5, 6] },
  ]);
  const [selectedWeekSet, setSelectedWeekSet] = useState(1);
  const [tabValue, setTabValue] = useState(0); // 0: Daily, 1: Weekly, 2: Monthly
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  
  // Synchronize external and local state
  useEffect(() => {
    setLocalAvailability(externalAvailabilitySettings);
  }, [externalAvailabilitySettings]);
  
  // Add debug useEffect
  useEffect(() => {
    console.log('Component initialized, current availability settings:', localAvailability);
    console.log('Teacher list:', teachers);
  }, []);

  useEffect(() => {
    if (selectedTeacher) {
      console.log(`Selected teacher ${selectedTeacher}, their availability settings:`,
        localAvailability[selectedTeacher] || 'No availability settings');
    }
  }, [selectedTeacher, localAvailability]);
  
  // Ensure that when component loads and teacher is selected, if there are no availability settings, automatically initialize
  useEffect(() => {
    if (selectedTeacher && (!localAvailability[selectedTeacher] || Object.keys(localAvailability[selectedTeacher] || {}).length === 0)) {
      console.log(`Teacher ${selectedTeacher} has no availability settings, auto initializing...`);
      initializeAvailability(selectedTeacher);
    }
  }, [selectedTeacher, localAvailability]);
  
  const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  const timeSlots = ['08:00-10:00', '10:00-12:00', '14:00-16:00', '16:00-18:00', '19:00-21:00'];
  
  const handleTeacherChange = (event) => {
    const newTeacherId = event.target.value;
    setSelectedTeacher(newTeacherId);
    
    // After selecting a teacher, if the teacher has no availability settings, automatically initialize all time slots as available
    if (newTeacherId && (!localAvailability[newTeacherId] || Object.keys(localAvailability[newTeacherId]).length === 0)) {
      initializeAvailability(newTeacherId);
    }
  };
  
  const handleTabChange = (event, newValue) => {
    setTabValue(newValue);
  };

  // Function to handle week selection in a week set
  const handleWeekSelection = (weekNumber, selected) => {
    setWeekSets(prev => prev.map(set => 
      set.id === selectedWeekSet
        ? {
            ...set,
            weeks: selected 
              ? [...set.weeks, weekNumber].sort((a, b) => a - b)
              : set.weeks.filter(w => w !== weekNumber)
          } 
        : set
    ));
  };

  // Function to handle week set name change
  const handleWeekSetNameChange = (e) => {
    setWeekSets(prev => prev.map(set => 
      set.id === selectedWeekSet
        ? { ...set, name: e.target.value }
        : set
    ));
  };

  const handleAvailabilityChange = (day, timeSlot, isAvailable) => {
    const teacherId = selectedTeacher;
    // Use functional update to ensure updates based on latest state
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    if (!newSettings[teacherId]) {
      newSettings[teacherId] = {};
    }
    
    if (!newSettings[teacherId][day]) {
      newSettings[teacherId][day] = {};
    }
    
    // Ensure setting is a clear boolean value
    newSettings[teacherId][day][timeSlot] = Boolean(isAvailable);
    
    // Add debug log
    console.log(`Setting ${day} ${timeSlot} to ${isAvailable ? 'available' : 'unavailable'}`);
    
    // Update local state
    setLocalAvailability(newSettings);
    
    // Call parent component's update function
    if (onSettingsChange) {
      onSettingsChange(newSettings);
    }
  };

  // Set availability for all time slots of a day
  const handleDayAvailabilityChange = (day, isAvailable) => {
    const teacherId = selectedTeacher;
    // Create deep copy instead of shallow copy
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    if (!newSettings[teacherId]) {
      newSettings[teacherId] = {};
    }
    
    if (!newSettings[teacherId][day]) {
      newSettings[teacherId][day] = {};
    }
    
    // Set all time slots for this day
    timeSlots.forEach(slot => {
      newSettings[teacherId][day][slot] = isAvailable;
    });
    
    // Add debug log
    console.log(`Setting all time slots for ${day} to ${isAvailable ? 'available' : 'unavailable'}`);
    
    // Update local state
    setLocalAvailability(newSettings);
    
    // Call parent component's update function
    if (onSettingsChange) {
      onSettingsChange(newSettings);
    }
  };

  // Set availability for a time slot across all days
  const handleTimeSlotAvailabilityChange = (timeSlot, isAvailable) => {
    const teacherId = selectedTeacher;
    // Create deep copy instead of shallow copy
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    if (!newSettings[teacherId]) {
      newSettings[teacherId] = {};
    }
    
    // Set this time slot for all days
    days.forEach(day => {
      if (!newSettings[teacherId][day]) {
        newSettings[teacherId][day] = {};
      }
      newSettings[teacherId][day][timeSlot] = isAvailable;
    });
    
    // Add debug log
    console.log(`Setting ${timeSlot} time slot for all days to ${isAvailable ? 'available' : 'unavailable'}`);
    
    // Update local state
    setLocalAvailability(newSettings);
    
    // Call parent component's update function
    if (onSettingsChange) {
      onSettingsChange(newSettings);
    }
  };
  
  const handleSaveTimeRange = () => {
    if (startDate && endDate && selectedTeacher) {
      // In a real app, this would save the date range to the backend
      alert(`Saved availability for ${teachers.find(t => t.id === selectedTeacher)?.name} from ${startDate} to ${endDate}`);
    }
  };
  
  // Initialize teacher's availability settings (default all available)
  const initializeAvailability = (teacherId) => {
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    newSettings[teacherId] = {};
    
    // Set all dates and time slots as available
    days.forEach(day => {
      newSettings[teacherId][day] = {};
      timeSlots.forEach(slot => {
        newSettings[teacherId][day][slot] = true; // Default to available
      });
    });
    
    console.log(`Initializing teacher ${teacherId} availability to all available`);
    
    // Update local state
    setLocalAvailability(newSettings);
    
    // Call parent component's update function
    if (onSettingsChange) {
      onSettingsChange(newSettings);
    }
  };

  // This ensures the save button works correctly based on local state
  const handleSaveAvailability = () => {
    if (selectedTeacher) {
      // In a real application, this would save to the backend
      alert(`Saved availability settings for ${teachers.find(t => t.id === selectedTeacher)?.name} with ${weekSets.length} different week patterns`);
      
      // If needed, call special save function here
      if (onSettingsChange) {
        onSettingsChange(localAvailability);
      }
    }
  };

  return (
    <Box>
      <Grid container spacing={2} sx={{ mb: 2 }}>
        <Grid item xs={12} md={6}>
          <FormControl fullWidth>
            <InputLabel>Select Teacher</InputLabel>
            <Select
              value={selectedTeacher || ''}
              onChange={handleTeacherChange}
              label="Select Teacher"
            >
              {teachers && teachers.length > 0 ? (
                teachers.map(teacher => (
                  <MenuItem key={teacher.id} value={teacher.id}>
                    {teacher.name} ({teacher.department})
                  </MenuItem>
                ))
              ) : (
                <MenuItem disabled>No teachers available</MenuItem>
              )}
            </Select>
          </FormControl>
        </Grid>
      </Grid>
      
      {selectedTeacher && (
        <>
          {/* // Add week selector above the tabs */}
          <Box sx={{ mb: 3 }}>
            <FormControl fullWidth>
              <InputLabel>Week Pattern</InputLabel>
              <Select
                value={selectedWeekSet}
                onChange={(e) => setSelectedWeekSet(e.target.value)}
                label="Week Pattern"
              >
                {weekSets.map(set => (
                  <MenuItem key={set.id} value={set.id}>
                    {set.name} (Weeks: {set.weeks.join(', ')})
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <Button
              variant="outlined"
              size="small"
              sx={{ mt: 1 }}
              onClick={() => {
                // Logic to create a new week set
                const newSetId = Math.max(...weekSets.map(s => s.id)) + 1;
                setWeekSets([...weekSets, { id: newSetId, name: `New Week Set ${newSetId}`, weeks: [] }]);
                setSelectedWeekSet(newSetId);
              }}
            >
              Create New Week Pattern
            </Button>
          </Box>

          {/* // Add a week set configuration section
          // Add this after the week selector */}
          {selectedWeekSet && (
            <Box sx={{ mb: 3 }}>
              <Grid container spacing={2}>
                <Grid item xs={12}>
                  <TextField
                    fullWidth
                    label="Week Set Name"
                    value={weekSets.find(s => s.id === selectedWeekSet)?.name || ''}
                    onChange={handleWeekSetNameChange}
                  />
                </Grid>
                <Grid item xs={12}>
                  <Typography variant="subtitle2" gutterBottom>
                    Select Weeks in Set
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {Array.from({ length: 16 }, (_, i) => i + 1).map(week => {
                      const isSelected = weekSets.find(s => s.id === selectedWeekSet)?.weeks.includes(week) || false;
                      return (
                        <Chip
                          key={week}
                          label={`Week ${week}`}
                          color={isSelected ? 'primary' : 'default'}
                          variant={isSelected ? 'filled' : 'outlined'}
                          onClick={() => handleWeekSelection(week, !isSelected)}
                          sx={{ cursor: 'pointer' }}
                        />
                      );
                    })}
                  </Box>
                </Grid>
              </Grid>
            </Box>
          )}

          <Paper sx={{ mb: 2 }}>
            <Tabs value={tabValue} onChange={handleTabChange} centered>
              <Tab label="Daily Schedule" />
              <Tab label="Weekly Pattern" />
              <Tab label="Date Range" />
            </Tabs>
          </Paper>
          
          {/* Daily Schedule Tab - Updated to calendar view style */}
          {tabValue === 0 && (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell></TableCell>
                    {days.map((day, index) => (
                      <TableCell key={day} align="center">
                        <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                          <Typography variant="subtitle2">{day}</Typography>
                          <Tooltip title={`Toggle all time slots for ${day}`}>
                            <Button 
                              variant="outlined" 
                              size="small"
                              sx={{ mt: 1, minWidth: '90px' }}
                              onClick={() => {
                                const teacherSettings = localAvailability[selectedTeacher] || {};
                                const daySettings = teacherSettings[day] || {};
                                // Check current status - if most are available then switch to unavailable, and vice versa
                                const availableCount = timeSlots.filter(slot => 
                                  daySettings[slot] !== false
                                ).length;
                                const isMainlyAvailable = availableCount > timeSlots.length / 2;
                                handleDayAvailabilityChange(day, !isMainlyAvailable);
                              }}
                            >
                              {/* Display different labels based on the overall availability status of this day */}
                              {(() => {
                                const teacherSettings = localAvailability[selectedTeacher] || {};
                                const daySettings = teacherSettings[day] || {};
                                const availableCount = timeSlots.filter(slot => 
                                  daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined)
                                ).length;
                                if (availableCount === 0) return 'All Off';
                                if (availableCount === timeSlots.length) return 'All On';
                                return `${availableCount}/${timeSlots.length}`;
                              })()}
                            </Button>
                          </Tooltip>
                        </Box>
                      </TableCell>
                    ))}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {timeSlots.map(slot => {
                    const [startTime, endTime] = slot.split('-');
                    return (
                      <TableRow key={slot}>
                        <TableCell component="th" scope="row" sx={{ whiteSpace: 'nowrap' }}>
                          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start' }}>
                            <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                              {startTime}-{endTime}
                            </Typography>
                            <Tooltip title="Toggle this time slot for all days">
                              <Button 
                                variant="outlined" 
                                size="small"
                                sx={{ mt: 1, minWidth: '90px' }}
                                onClick={() => {
                                  const teacherSettings = localAvailability[selectedTeacher] || {};
                                  // Check current status - if most are available then switch to unavailable and vice versa
                                  const availableCount = days.filter(day => {
                                    const daySettings = teacherSettings[day] || {};
                                    return daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined);
                                  }).length;
                                  const isMainlyAvailable = availableCount > days.length / 2;
                                  handleTimeSlotAvailabilityChange(slot, !isMainlyAvailable);
                                }}
                              >
                                {/* Display different labels based on the overall availability status of this time slot */}
                                {(() => {
                                  const teacherSettings = localAvailability[selectedTeacher] || {};
                                  const availableCount = days.filter(day => {
                                    const daySettings = teacherSettings[day] || {};
                                    return daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined);
                                  }).length;
                                  if (availableCount === 0) return 'All Off';
                                  if (availableCount === days.length) return 'All On';
                                  return `${availableCount}/${days.length}`;
                                })()}
                              </Button>
                            </Tooltip>
                          </Box>
                        </TableCell>
                        {days.map(day => {
                          const teacherSettings = localAvailability[selectedTeacher] || {};
                          const daySettings = teacherSettings[day] || {};
                          const isAvailable = daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined); // Modified availability judgment logic
                          
                          return (
                            <TableCell 
                              key={`${day}-${slot}`} 
                              align="center" 
                              sx={{ 
                                height: 70, 
                                minWidth: 100,
                                backgroundColor: isAvailable ? 'rgba(76, 175, 80, 0.1)' : 'rgba(244, 67, 54, 0.1)',
                                '&:hover': {
                                  backgroundColor: isAvailable ? 'rgba(76, 175, 80, 0.2)' : 'rgba(244, 67, 54, 0.2)',
                                },
                                cursor: 'pointer',
                                transition: 'background-color 0.2s'
                              }}
                              onClick={() => handleAvailabilityChange(day, slot, !isAvailable)}
                            >
                              <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
                                {isAvailable ? (
                                  <CheckCircleIcon color="success" sx={{ fontSize: 28 }} />
                                ) : (
                                  <CancelIcon color="error" sx={{ fontSize: 28 }} />
                                )}
                              </Box>
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
          
          {/* Weekly Pattern Tab */}
          {tabValue === 1 && (
            <Paper variant="outlined" sx={{ p: 2 }}>
              <Typography variant="body2" sx={{ mb: 2 }}>
                Set recurring availability patterns for the entire semester.
              </Typography>
              
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
                {days.map(day => (
                  <FormControlLabel
                    key={day}
                    control={
                      <Switch
                        checked={
                          localAvailability[selectedTeacher] &&
                          localAvailability[selectedTeacher][day] &&
                          Object.values(localAvailability[selectedTeacher][day]).some(v => v)
                        }
                        onChange={(e) => {
                          // In a real app, this would set all time slots for this day
                          timeSlots.forEach(slot => {
                            handleAvailabilityChange(day, slot, e.target.checked);
                          });
                        }}
                      />
                    }
                    label={day}
                  />
                ))}
              </Box>
              
              <Typography variant="subtitle2" gutterBottom>
                Working Hours
              </Typography>
              
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth size="small">
                    <InputLabel>Work Day Start</InputLabel>
                    <Select
                      value="08:00"
                      label="Work Day Start"
                    >
                      <MenuItem value="08:00">8:00 AM</MenuItem>
                      <MenuItem value="10:00">10:00 AM</MenuItem>
                      <MenuItem value="14:00">2:00 PM</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth size="small">
                    <InputLabel>Work Day End</InputLabel>
                    <Select
                      value="18:00"
                      label="Work Day End"
                    >
                      <MenuItem value="16:00">4:00 PM</MenuItem>
                      <MenuItem value="18:00">6:00 PM</MenuItem>
                      <MenuItem value="21:00">9:00 PM</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
              </Grid>
              
              <Typography variant="subtitle2" sx={{ mt: 2 }} gutterBottom>
                Break Times
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {['Lunch (12:00-14:00)', 'Evening (18:00-19:00)'].map(breakTime => (
                  <Chip 
                    key={breakTime} 
                    label={breakTime} 
                    onDelete={() => {}} 
                    color="primary" 
                    variant="outlined"
                  />
                ))}
                <Chip 
                  label="+ Add Break" 
                  onClick={() => {}} 
                  color="primary" 
                  variant="outlined"
                />
              </Box>
            </Paper>
          )}
          
          {/* Date Range Tab */}
          {tabValue === 2 && (
            <Paper variant="outlined" sx={{ p: 2 }}>
              <Typography variant="body2" sx={{ mb: 2 }}>
                Set specific date ranges when this teacher is unavailable (e.g., for leave, conferences).
              </Typography>
              
              <Grid container spacing={2}>
                <Grid item xs={12} md={5}>
                  <TextField
                    fullWidth
                    label="Start Date"
                    type="date"
                    value={startDate}
                    onChange={(e) => setStartDate(e.target.value)}
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>
                <Grid item xs={12} md={5}>
                  <TextField
                    fullWidth
                    label="End Date"
                    type="date"
                    value={endDate}
                    onChange={(e) => setEndDate(e.target.value)}
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>
                <Grid item xs={12} md={2}>
                  <Button 
                    variant="contained" 
                    color="primary" 
                    fullWidth
                    sx={{ height: '100%' }}
                    onClick={handleSaveTimeRange}
                    disabled={!startDate || !endDate}
                  >
                    Add Range
                  </Button>
                </Grid>
              </Grid>
              
              <Typography variant="subtitle2" sx={{ mt: 3, mb: 1 }}>
                Unavailable Date Ranges
              </Typography>
              
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Start Date</TableCell>
                      <TableCell>End Date</TableCell>
                      <TableCell>Reason</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    <TableRow>
                      <TableCell>2024-10-15</TableCell>
                      <TableCell>2024-10-20</TableCell>
                      <TableCell>Conference</TableCell>
                      <TableCell>
                        <Button size="small" color="error">
                          Delete
                        </Button>
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>2024-12-24</TableCell>
                      <TableCell>2025-01-05</TableCell>
                      <TableCell>Winter Holidays</TableCell>
                      <TableCell>
                        <Button size="small" color="error">
                          Delete
                        </Button>
                      </TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            </Paper>
          )}
        </>
      )}
      {/* // Add save button for teacher availability
      // Add this at the end of the component */}
      <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end' }}>
        <Button
          variant="contained"
          color="primary"
          onClick={handleSaveAvailability}
          disabled={!selectedTeacher}
        >
          Save Teacher Availability
        </Button>
      </Box>
    </Box>
  );
};

export default TeacherAvailabilitySettings;