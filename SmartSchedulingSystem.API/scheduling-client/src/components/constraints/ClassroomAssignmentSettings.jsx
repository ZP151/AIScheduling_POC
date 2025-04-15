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
      courseRoomPreferences: coursePreferences
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
        
        <Grid item xs={12} sx={{ mt: 2 }}>
          <Typography variant="body2" color="text.secondary">
            Note: For classroom availability exceptions and time-specific constraints, please use the "Classroom Availability" settings.
          </Typography>
        </Grid>
      </Grid>
    </Box>
  );
};

export default ClassroomAssignmentSettings;