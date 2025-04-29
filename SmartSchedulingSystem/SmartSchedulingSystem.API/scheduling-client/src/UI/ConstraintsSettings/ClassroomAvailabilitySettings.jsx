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

const ClassroomAvailabilitySettings = ({ classrooms, availabilitySettings: externalAvailabilitySettings, semesterId, onUpdate }) => {
  // Initialize state with default values
  const [localAvailability, setLocalAvailability] = useState(externalAvailabilitySettings || {});
  const [selectedClassroom, setSelectedClassroom] = useState('');
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
  const [tabValue, setTabValue] = useState(0); // 0: Daily, 1: Weekly, 2: Date Range
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [unavailabilityReason, setUnavailabilityReason] = useState('Maintenance');
  
  // Synchronize external and local state
  useEffect(() => {
    setLocalAvailability(externalAvailabilitySettings);
  }, [externalAvailabilitySettings]);

  // Add debug useEffect
  useEffect(() => {
    console.log('Component initialized, current availability settings:', localAvailability);
    console.log('Classroom list:', classrooms);
  }, []);

  useEffect(() => {
    if (selectedClassroom) {
      console.log(`Selected classroom ${selectedClassroom}, their availability settings:`,
        localAvailability[selectedClassroom] || 'No availability settings');
    }
  }, [selectedClassroom, localAvailability]);

  // Ensure that when component loads and classroom is selected, if there are no availability settings, automatically initialize
  useEffect(() => {
    if (selectedClassroom && (!localAvailability[selectedClassroom] || Object.keys(localAvailability[selectedClassroom] || {}).length === 0)) {
      setLocalAvailability(prev => ({
        ...prev,
        [selectedClassroom]: {
          regular: {
            monday: [],
            tuesday: [],
            wednesday: [],
            thursday: [],
            friday: []
          },
          special: []
        }
      }));
    }
  }, [selectedClassroom]);

  // Handle classroom selection
  const handleClassroomSelect = (event) => {
    setSelectedClassroom(event.target.value);
  };

  // Handle day selection
  const handleDaySelect = (event) => {
    setSelectedDay(event.target.value);
  };

  // Handle time slot selection
  const handleTimeSlotSelect = (event) => {
    setSelectedTimeSlot(event.target.value);
  };

  // Handle edit mode change
  const handleEditModeChange = (event) => {
    setEditMode(event.target.value);
  };

  // Handle edit button click
  const handleEditClick = () => {
    setIsEditing(true);
  };

  // Handle save button click
  const handleSaveClick = () => {
    setIsEditing(false);
    if (onUpdate) {
      onUpdate(localAvailability);
    }
  };

  // Handle cancel button click
  const handleCancelClick = () => {
    setIsEditing(false);
    setLocalAvailability(externalAvailabilitySettings);
  };

  // Handle add time slot
  const handleAddTimeSlot = () => {
    if (!selectedClassroom || !selectedDay || !selectedTimeSlot) {
      showFeedback('Please select a classroom, day and time slot', 'warning');
      return;
    }

    const newSettings = { ...localAvailability };
    if (!newSettings[selectedClassroom]) {
      newSettings[selectedClassroom] = {
        regular: {
          monday: [],
          tuesday: [],
          wednesday: [],
          thursday: [],
          friday: []
        },
        special: []
      };
    }

    if (!newSettings[selectedClassroom].regular[selectedDay]) {
      newSettings[selectedClassroom].regular[selectedDay] = [];
    }

    if (newSettings[selectedClassroom].regular[selectedDay].includes(selectedTimeSlot)) {
      showFeedback('This time slot is already added', 'warning');
      return;
    }

    newSettings[selectedClassroom].regular[selectedDay].push(selectedTimeSlot);
    setLocalAvailability(newSettings);
    showFeedback('Time slot added successfully', 'success');
  };

  // Handle remove time slot
  const handleRemoveTimeSlot = () => {
    if (!selectedClassroom || !selectedDay || !selectedTimeSlot) {
      showFeedback('Please select a classroom, day and time slot', 'warning');
      return;
    }

    const newSettings = { ...localAvailability };
    if (!newSettings[selectedClassroom] || !newSettings[selectedClassroom].regular[selectedDay]) {
      showFeedback('No time slots to remove', 'warning');
      return;
    }

    const index = newSettings[selectedClassroom].regular[selectedDay].indexOf(selectedTimeSlot);
    if (index === -1) {
      showFeedback('Time slot not found', 'warning');
      return;
    }

    newSettings[selectedClassroom].regular[selectedDay].splice(index, 1);
    setLocalAvailability(newSettings);
    showFeedback('Time slot removed successfully', 'success');
  };

  // Show feedback message
  const showFeedback = (message, type = 'info') => {
    setFeedback({
      open: true,
      message,
      type
    });
  };

  // Close feedback message
  const handleCloseFeedback = () => {
    setFeedback(prev => ({
      ...prev,
      open: false
    }));
  };

  const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  const timeSlots = ['08:00-10:00', '10:00-12:00', '14:00-16:00', '16:00-18:00', '19:00-21:00'];
  
  const handleClassroomChange = (event) => {
    const newClassroomId = event.target.value;
    setSelectedClassroom(newClassroomId);
    
    // After selecting a classroom, if the classroom has no availability settings, automatically initialize all time slots as available
    if (newClassroomId && (!localAvailability[newClassroomId] || Object.keys(localAvailability[newClassroomId]).length === 0)) {
      initializeAvailability(newClassroomId);
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
    const classroomId = selectedClassroom;
    // Use functional update to ensure updates based on latest state
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    if (!newSettings[classroomId]) {
      newSettings[classroomId] = {};
    }
    
    if (!newSettings[classroomId][day]) {
      newSettings[classroomId][day] = {};
    }
    
    // Ensure setting is a clear boolean value
    newSettings[classroomId][day][timeSlot] = Boolean(isAvailable);
    
    // Add debug log
    console.log(`Setting ${day} ${timeSlot} to ${isAvailable ? 'available' : 'unavailable'}`);
    
    // Update local state
    setLocalAvailability(newSettings);
    
    // Call parent component's update function
    if (onUpdate) {
      onUpdate(newSettings);
    }
  };

  // Set availability for all time slots of a day
  const handleDayAvailabilityChange = (day, isAvailable) => {
    const classroomId = selectedClassroom;
    // Create deep copy instead of shallow copy
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    if (!newSettings[classroomId]) {
      newSettings[classroomId] = {};
    }
    
    if (!newSettings[classroomId][day]) {
      newSettings[classroomId][day] = {};
    }
    
    // Set all time slots for this day
    timeSlots.forEach(slot => {
      newSettings[classroomId][day][slot] = isAvailable;
    });
    
    // Add debug log
    console.log(`Setting all time slots for ${day} to ${isAvailable ? 'available' : 'unavailable'}`);
    
    // Update local state
    setLocalAvailability(newSettings);
    
    // Call parent component's update function
    if (onUpdate) {
      onUpdate(newSettings);
    }
  };

  // Set availability for a time slot across all days
  const handleTimeSlotAvailabilityChange = (timeSlot, isAvailable) => {
    const classroomId = selectedClassroom;
    // Create deep copy instead of shallow copy
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    if (!newSettings[classroomId]) {
      newSettings[classroomId] = {};
    }
    
    // Set this time slot for all days
    days.forEach(day => {
      if (!newSettings[classroomId][day]) {
        newSettings[classroomId][day] = {};
      }
      newSettings[classroomId][day][timeSlot] = isAvailable;
    });
    
    // Add debug log
    console.log(`Setting ${timeSlot} time slot for all days to ${isAvailable ? 'available' : 'unavailable'}`);
    
    // Update local state
    setLocalAvailability(newSettings);
    
    // Call parent component's update function
    if (onUpdate) {
      onUpdate(newSettings);
    }
  };
  
  // Initialize classroom's availability settings (default all available)
  const initializeAvailability = (classroomId) => {
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    newSettings[classroomId] = {};
    
    // Set all dates and time slots as available
    days.forEach(day => {
      newSettings[classroomId][day] = {};
      timeSlots.forEach(slot => {
        newSettings[classroomId][day][slot] = true; // Default to available
      });
    });
    
    console.log(`Initializing classroom ${classroomId} availability to all available`);
    
    // Update local state
    setLocalAvailability(newSettings);
    
    // Call parent component's update function
    if (onUpdate) {
      onUpdate(newSettings);
    }
  };

  // This ensures the save button works correctly based on local state
  const handleSaveAvailability = () => {
    if (selectedClassroom) {
      // In a real application, this would save to the backend
      alert(`Saved availability settings for classroom ${classrooms.find(c => c.id === selectedClassroom)?.name} with ${weekSets.length} different week patterns`);
      
      // If needed, call special save function here
      if (onUpdate) {
        onUpdate(localAvailability);
      }
    }
  };
  
  const handleAddDateRangeException = () => {
    if (!selectedClassroom || !startDate || !endDate) return;
    
    // In practice, date range exceptions are added here to the backend
    console.log(`Adding date range exception for classroom ${selectedClassroom} from ${startDate} to ${endDate}`);
    
    // Clear form fields
    setStartDate('');
    setEndDate('');
  };
  
  return (
    <Box>
      <Grid container spacing={2} sx={{ mb: 2 }}>
        <Grid item xs={12} md={6}>
          <FormControl fullWidth>
            <InputLabel>Select Classroom</InputLabel>
            <Select
              value={selectedClassroom || ''}
              onChange={handleClassroomChange}
              label="Select Classroom"
            >
              {classrooms.map(classroom => (
                <MenuItem key={classroom.id} value={classroom.id}>
                  {classroom.building}-{classroom.name} (Capacity: {classroom.capacity})
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Grid>
      </Grid>
      
      {selectedClassroom && (
        <>
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

          {selectedWeekSet && (
            <Box sx={{ mt: 2, p: 2, border: '1px solid #e0e0e0', borderRadius: 1 }}>
              <Grid container spacing={2} alignItems="center">
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Week Set Name"
                    value={weekSets.find(s => s.id === selectedWeekSet)?.name || ''}
                    onChange={handleWeekSetNameChange}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                    <Button
                      variant="outlined"
                      color="error"
                      size="small"
                      onClick={() => {
                        if (weekSets.length > 1) {
                          setWeekSets(prev => prev.filter(s => s.id !== selectedWeekSet));
                          setSelectedWeekSet(weekSets[0].id);
                        }
                      }}
                      disabled={weekSets.length <= 1}
                    >
                      Delete Week Set
                    </Button>
                  </Box>
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
              <Tab label="Date Range Exceptions" />
            </Tabs>
          </Paper>
          
          {/* Daily Schedule Tab - Calendar View */}
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
                                const classroomSettings = localAvailability[selectedClassroom] || {};
                                const daySettings = classroomSettings[day] || {};
                                // Check current status - if most are available then switch to unavailable and vice versa
                                const availableCount = timeSlots.filter(slot => 
                                  daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined)
                                ).length;
                                const isMainlyAvailable = availableCount > timeSlots.length / 2;
                                handleDayAvailabilityChange(day, !isMainlyAvailable);
                              }}
                            >
                              {/* Display different labels based on the overall availability status of this day */}
                              {(() => {
                                const classroomSettings = localAvailability[selectedClassroom] || {};
                                const daySettings = classroomSettings[day] || {};
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
                                  const classroomSettings = localAvailability[selectedClassroom] || {};
                                  // Check current status - if most are available then switch to unavailable,反之亦然
                                  const availableCount = days.filter(day => {
                                    const daySettings = classroomSettings[day] || {};
                                    return daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined);
                                  }).length;
                                  const isMainlyAvailable = availableCount > days.length / 2;
                                  handleTimeSlotAvailabilityChange(slot, !isMainlyAvailable);
                                }}
                              >
                                {/* Display different labels based on the overall availability status of this time slot */}
                                {(() => {
                                  const classroomSettings = localAvailability[selectedClassroom] || {};
                                  const availableCount = days.filter(day => {
                                    const daySettings = classroomSettings[day] || {};
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
                          const classroomSettings = localAvailability[selectedClassroom] || {};
                          const daySettings = classroomSettings[day] || {};
                          const isAvailable = daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined); // 修改可用性判断逻辑
                          
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
                          localAvailability[selectedClassroom] &&
                          localAvailability[selectedClassroom][day] &&
                          Object.values(localAvailability[selectedClassroom][day]).some(v => v)
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
                Hours of Operation
              </Typography>
              
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth size="small">
                    <InputLabel>Open Time</InputLabel>
                    <Select
                      value="08:00"
                      label="Open Time"
                    >
                      <MenuItem value="08:00">8:00 AM</MenuItem>
                      <MenuItem value="10:00">10:00 AM</MenuItem>
                      <MenuItem value="14:00">2:00 PM</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth size="small">
                    <InputLabel>Close Time</InputLabel>
                    <Select
                      value="18:00"
                      label="Close Time"
                    >
                      <MenuItem value="16:00">4:00 PM</MenuItem>
                      <MenuItem value="18:00">6:00 PM</MenuItem>
                      <MenuItem value="21:00">9:00 PM</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
              </Grid>
              
              <Typography variant="subtitle2" sx={{ mt: 2 }} gutterBottom>
                Recurring Usage Times
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {['Faculty Meetings (Mon 12:00-14:00)', 'Maintenance (Fri 16:00-18:00)'].map(timeBlock => (
                  <Chip 
                    key={timeBlock} 
                    label={timeBlock} 
                    onDelete={() => {}} 
                    color="primary" 
                    variant="outlined"
                  />
                ))}
                <Chip 
                  label="+ Add Time Block" 
                  onClick={() => {}} 
                  color="primary" 
                  variant="outlined"
                />
              </Box>
            </Paper>
          )}
          
          {/* Date Range Exceptions Tab */}
          {tabValue === 2 && (
            <Paper variant="outlined" sx={{ p: 2 }}>
              <Typography variant="body2" sx={{ mb: 2 }}>
                Set specific date ranges when this classroom is unavailable (e.g., for maintenance, events, exams).
              </Typography>
              
              <Grid container spacing={2}>
                <Grid item xs={12} md={4}>
                  <TextField
                    fullWidth
                    label="Start Date"
                    type="date"
                    value={startDate}
                    onChange={(e) => setStartDate(e.target.value)}
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>
                <Grid item xs={12} md={4}>
                  <TextField
                    fullWidth
                    label="End Date"
                    type="date"
                    value={endDate}
                    onChange={(e) => setEndDate(e.target.value)}
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>
                <Grid item xs={12} md={4}>
                  <FormControl fullWidth>
                    <InputLabel>Reason</InputLabel>
                    <Select
                      value={unavailabilityReason}
                      onChange={(e) => setUnavailabilityReason(e.target.value)}
                      label="Reason"
                    >
                      <MenuItem value="Maintenance">Maintenance</MenuItem>
                      <MenuItem value="Event">Special Event</MenuItem>
                      <MenuItem value="Exam">Exam</MenuItem>
                      <MenuItem value="Renovation">Renovation</MenuItem>
                      <MenuItem value="Other">Other</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12}>
                  <Button 
                    variant="contained" 
                    color="primary"
                    onClick={handleAddDateRangeException}
                    disabled={!startDate || !endDate}
                  >
                    Add Exception
                  </Button>
                </Grid>
              </Grid>
              
              <Typography variant="subtitle2" sx={{ mt: 3, mb: 1 }}>
                Unavailable Date Periods
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
                      <TableCell>Maintenance</TableCell>
                      <TableCell>
                        <Button size="small" color="error">
                          Delete
                        </Button>
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>2024-12-24</TableCell>
                      <TableCell>2025-01-05</TableCell>
                      <TableCell>Holiday Closure</TableCell>
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
      <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end' }}>
        <Button
          variant="contained"
          color="primary"
          onClick={handleSaveAvailability}
          disabled={!selectedClassroom}
        >
          Save Classroom Availability
        </Button>
      </Box>
    </Box>
  );
};

export default ClassroomAvailabilitySettings; 