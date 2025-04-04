// ClassroomAssignmentSettings.jsx
import React, { useState } from 'react';
import { 
  Box, 
  Typography, 
  Slider, 
  Paper, 
  Table, 
  TableBody, 
  TableCell, 
  TableContainer, 
  TableHead, 
  TableRow,
  FormControl,
  Select,
  MenuItem,
  Chip,
  Grid,
  TextField,
  InputLabel,
  IconButton,
  Button
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';

const ClassroomAssignmentSettings = ({ courses, classrooms, weight, onUpdate }) => {
  const [localWeight, setLocalWeight] = useState(weight);
  const [coursePreferences, setCoursePreferences] = useState({});
  const [selectedClassroom, setSelectedClassroom] = useState(null);
  const [startDateStr, setStartDateStr] = useState('');
  const [endDateStr, setEndDateStr] = useState('');
  const [unavailabilityReason, setUnavailabilityReason] = useState('Maintenance');
  const [classroomUnavailability, setClassroomUnavailability] = useState({});

  const roomTypes = [
    { id: 'Lecture', name: 'Lecture Room' },
    { id: 'Laboratory', name: 'Laboratory' },
    { id: 'ComputerLab', name: 'Computer Lab' },
    { id: 'Seminar', name: 'Seminar Room' },
    { id: 'LargeHall', name: 'Large Hall' }
  ];
  
  const handleWeightChange = (event, newValue) => {
    setLocalWeight(newValue);
    onUpdate({
      weight: newValue,
      courseRoomPreferences: coursePreferences,
      classroomUnavailability: classroomUnavailability

    });
  };
  
  const handlePreferenceChange = (courseId, value) => {
    const newPreferences = {
      ...coursePreferences,
      [courseId]: value
    };
    
    setCoursePreferences(newPreferences);
    onUpdate({
      weight: localWeight,
      courseRoomPreferences: newPreferences
    });
  };
  
  const handleAddUnavailability = () => {
    if (!selectedClassroom || !startDateStr || !endDateStr || !unavailabilityReason) return;
    
    const newUnavailability = {
        startDate: startDateStr,
        endDate: endDateStr,
        reason: unavailabilityReason
      };
    
    setClassroomUnavailability(prev => ({
      ...prev,
      [selectedClassroom]: [
        ...(prev[selectedClassroom] || []),
        newUnavailability
      ]
    }));
    
    // Reset form fields
    setStartDateStr(null);
    setEndDateStr(null);
  };
  
  const handleRemoveUnavailability = (classroomId, index) => {
    setClassroomUnavailability(prev => ({
      ...prev,
      [classroomId]: prev[classroomId].filter((_, i) => i !== index)
    }));
  };

  return (
    <Box>
      <Grid container spacing={2}>
        <Grid item xs={12}>
          <Typography gutterBottom>
            Classroom Type Matching Weight: {localWeight.toFixed(1)}
          </Typography>
          <Slider
            value={localWeight}
            onChange={handleWeightChange}
            step={0.1}
            marks
            min={0}
            max={1}
            valueLabelDisplay="auto"
          />
          
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1, mb: 2 }}>
            Higher weights will prioritize matching courses with appropriate classroom types (e.g., labs for lab courses).
          </Typography>
        </Grid>
      
        <Grid item xs={12}>
          <Typography variant="subtitle2" gutterBottom>Course-Classroom Type Preferences</Typography>
          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Course</TableCell>
                  <TableCell>Preferred Room Type</TableCell>
                  <TableCell>Suitable Rooms</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {courses.map(course => {
                  const preferredType = coursePreferences[course.id] || 'Any';
                  const suitableRooms = classrooms.filter(c => 
                    preferredType === 'Any' || c.type === preferredType
                  );
                  
                  return (
                    <TableRow key={course.id}>
                      <TableCell>{course.code} - {course.name}</TableCell>
                      <TableCell>
                        <FormControl fullWidth size="small">
                          <Select
                            value={preferredType}
                            onChange={(e) => handlePreferenceChange(course.id, e.target.value)}
                          >
                            <MenuItem value="Any">Any Room Type</MenuItem>
                            {roomTypes.map(type => (
                              <MenuItem key={type.id} value={type.id}>{type.name}</MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                      </TableCell>
                      <TableCell>
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                          {suitableRooms.length > 0 ? (
                            suitableRooms.map(room => (
                              <Chip 
                                key={room.id} 
                                label={`${room.building}-${room.name} (${room.capacity})`} 
                                size="small" 
                                variant="outlined" 
                              />
                            ))
                          ) : (
                            <Typography variant="caption" color="text.secondary">
                              No matching rooms
                            </Typography>
                          )}
                        </Box>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        </Grid>

        {/* // Add new section to ClassroomAssignmentSettings.jsx
        // Add after the course-classroom preferences table */}
        <Grid item xs={12} sx={{ mt: 3 }}>
        <Typography variant="subtitle2" gutterBottom>Classroom Availability Exceptions</Typography>
        <Paper variant="outlined" sx={{ p: 2 }}>
            <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
                <FormControl fullWidth>
                <InputLabel>Select Classroom</InputLabel>
                <Select
                    value={selectedClassroom || ''}
                    onChange={(e) => setSelectedClassroom(e.target.value)}
                    label="Select Classroom"
                >
                    {classrooms.map(room => (
                    <MenuItem key={room.id} value={room.id}>
                        {room.building}-{room.name} (Capacity: {room.capacity})
                    </MenuItem>
                    ))}
                </Select>
                </FormControl>
            </Grid>
            
            {selectedClassroom && (
                <>
                <Grid item xs={12} md={6}>
                    <FormControl fullWidth>
                    <InputLabel>Unavailability Reason</InputLabel>
                    <Select
                        value={unavailabilityReason}
                        onChange={(e) => setUnavailabilityReason(e.target.value)}
                        label="Unavailability Reason"
                    >
                        <MenuItem value="Maintenance">Maintenance</MenuItem>
                        <MenuItem value="Event">Special Event</MenuItem>
                        <MenuItem value="Exam">Exam</MenuItem>
                        <MenuItem value="Other">Other</MenuItem>
                    </Select>
                    </FormControl>
                </Grid>
                
                <Grid item xs={12} md={6}>
                    <TextField
                    label="Start Date"
                    type="date"
                    value={startDateStr}
                    onChange={(e) => setStartDateStr(e.target.value)}
                    fullWidth
                    InputLabelProps={{ shrink: true }}
                    />
                </Grid>
                
                <Grid item xs={12} md={6}>
                    <TextField
                    label="End Date"
                    type="date"
                    value={endDateStr}
                    onChange={(e) => setEndDateStr(e.target.value)}
                    fullWidth
                    InputLabelProps={{ shrink: true }}
                    />
                </Grid>
                
                <Grid item xs={12}>
                    <Button 
                    variant="contained" 
                    onClick={handleAddUnavailability}
                    disabled={!startDateStr || !endDateStr || !unavailabilityReason}
                    >
                    Add Unavailability Period
                    </Button>
                </Grid>
                </>
            )}
            </Grid>
            
            {/* List of unavailability periods */}
            {selectedClassroom && classroomUnavailability[selectedClassroom]?.length > 0 && (
            <Box sx={{ mt: 2 }}>
                <Typography variant="subtitle2" gutterBottom>Unavailability Periods</Typography>
                <TableContainer>
                <Table size="small">
                    <TableHead>
                    <TableRow>
                        <TableCell>Start Date</TableCell>
                        <TableCell>End Date</TableCell>
                        <TableCell>Reason</TableCell>
                        <TableCell>Action</TableCell>
                    </TableRow>
                    </TableHead>
                    <TableBody>
                    {classroomUnavailability[selectedClassroom].map((period, index) => (
                        <TableRow key={index}>
                        <TableCell>{period.startDate}</TableCell>
                        <TableCell>{period.endDate}</TableCell>
                        <TableCell>{period.reason}</TableCell>
                        <TableCell>
                            <IconButton 
                            size="small"
                            onClick={() => handleRemoveUnavailability(selectedClassroom, index)}
                            >
                            <DeleteIcon fontSize="small" />
                            </IconButton>
                        </TableCell>
                        </TableRow>
                    ))}
                    </TableBody>
                </Table>
                </TableContainer>
            </Box>
            )}
        </Paper>
        </Grid>

      </Grid>
    </Box>
  );
};

export default ClassroomAssignmentSettings;