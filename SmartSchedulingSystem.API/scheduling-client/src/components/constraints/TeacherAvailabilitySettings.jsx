// TeacherAvailabilitySettings.jsx
import React, { useState } from 'react';
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
  Grid,  // Added missing import
  FormControlLabel,  // Added missing import
  Chip
} from '@mui/material';

// Remove the date pickers for now to avoid the date-fns error
// We'll use simple text fields instead

const TeacherAvailabilitySettings = ({ teachers, availabilitySettings, semesterId, onUpdate }) => {
  // Add this state at the beginning of the component
  const [weekMode, setWeekMode] = useState('regular');
  const [weekSets, setWeekSets] = useState([
    { id: 1, name: 'Regular Weeks', weeks: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14] },
    { id: 2, name: 'Exam Weeks', weeks: [15, 16] },
    { id: 3, name: 'Special Weeks', weeks: [5, 6] },
  ]);
  const [selectedWeekSet, setSelectedWeekSet] = useState(1);
  
  const [selectedTeacher, setSelectedTeacher] = useState(null);
  const [tabValue, setTabValue] = useState(0); // 0: Daily, 1: Weekly, 2: Monthly
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  
  const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  const timeSlots = ['08:00-09:30', '10:00-11:30', '12:00-13:30', '14:00-15:30', '16:00-17:30', '18:00-19:30'];
  
  const handleTeacherChange = (event) => {
    setSelectedTeacher(event.target.value);
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
    const newSettings = { ...availabilitySettings };
    
    if (!newSettings[teacherId]) {
      newSettings[teacherId] = {};
    }
    
    if (!newSettings[teacherId][day]) {
      newSettings[teacherId][day] = {};
    }
    
    newSettings[teacherId][day][timeSlot] = isAvailable;
    
    onUpdate(newSettings);
  };
  
  const handleSaveTimeRange = () => {
    if (startDate && endDate && selectedTeacher) {
      // In a real app, this would save the date range to the backend
      alert(`Saved availability for ${teachers.find(t => t.id === selectedTeacher)?.name} from ${startDate} to ${endDate}`);
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
              {teachers.map(teacher => (
                <MenuItem key={teacher.id} value={teacher.id}>
                  {teacher.name} ({teacher.department})
                </MenuItem>
              ))}
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
              <Tab label="Date Range" />
            </Tabs>
          </Paper>
          
          {/* Daily Schedule Tab */}
          {tabValue === 0 && (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell></TableCell>
                    {timeSlots.map(slot => (
                      <TableCell key={slot} align="center">{slot}</TableCell>
                    ))}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {days.map(day => (
                    <TableRow key={day}>
                      <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                        {day}
                      </TableCell>
                      {timeSlots.map(slot => {
                        const teacherSettings = availabilitySettings[selectedTeacher] || {};
                        const daySettings = teacherSettings[day] || {};
                        const isAvailable = daySettings[slot] !== false; // Default to available
                        
                        return (
                          <TableCell key={`${day}-${slot}`} align="center">
                            <Switch
                              size="small"
                              checked={isAvailable}
                              onChange={(e) => handleAvailabilityChange(day, slot, e.target.checked)}
                            />
                          </TableCell>
                        );
                      })}
                    </TableRow>
                  ))}
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
                          availabilitySettings[selectedTeacher] &&
                          availabilitySettings[selectedTeacher][day] &&
                          Object.values(availabilitySettings[selectedTeacher][day]).some(v => v)
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
              <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                <TextField
                  label="Start Time"
                  type="time"
                  defaultValue="08:00"
                  InputLabelProps={{ shrink: true }}
                  sx={{ width: 150 }}
                />
                <Typography>to</Typography>
                <TextField
                  label="End Time"
                  type="time"
                  defaultValue="18:00"
                  InputLabelProps={{ shrink: true }}
                  sx={{ width: 150 }}
                />
                <Button 
                  variant="contained" 
                  size="small"
                  onClick={() => {
                    // In a real app, this would set availability for the specified time range
                    alert("Applied working hours to all selected days");
                  }}
                >
                  Apply
                </Button>
              </Box>
            </Paper>
          )}
          
          {/* Date Range Tab */}
          {tabValue === 2 && (
            <Paper variant="outlined" sx={{ p: 2 }}>
              <Typography variant="body2" sx={{ mb: 2 }}>
                Set specific date ranges when this teacher is available or unavailable.
              </Typography>
              
              <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                <TextField
                  label="Start Date"
                  type="date"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  InputLabelProps={{ shrink: true }}
                  fullWidth
                />
                <TextField
                  label="End Date"
                  type="date"
                  value={endDate}
                  onChange={(e) => setEndDate(e.target.value)}
                  InputLabelProps={{ shrink: true }}
                  fullWidth
                />
              </Box>
              
              <FormControlLabel
                control={
                  <Switch defaultChecked />
                }
                label="Teacher is unavailable during this period"
              />
              
              <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                <Button 
                  variant="contained"
                  onClick={handleSaveTimeRange}
                  disabled={!startDate || !endDate}
                >
                  Save Date Range
                </Button>
              </Box>
            </Paper>
          )}
          
          <Typography variant="caption" color="text.secondary" sx={{ mt: 2, display: 'block' }}>
            Toggle switches to mark when the teacher is available (on) or unavailable (off)
          </Typography>
        </>
      )}
      {/* // Add save button for teacher availability
      // Add this at the end of the component */}
      <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end' }}>
        <Button
          variant="contained"
          color="primary"
          onClick={() => {
            // In a real app, this would save to the backend
            alert(`Saved availability settings for ${teachers.find(t => t.id === selectedTeacher)?.name} with ${weekSets.length} different week patterns`);
          }}
          disabled={!selectedTeacher}
        >
          Save Teacher Availability
        </Button>
      </Box>
    </Box>
  );
};

export default TeacherAvailabilitySettings;