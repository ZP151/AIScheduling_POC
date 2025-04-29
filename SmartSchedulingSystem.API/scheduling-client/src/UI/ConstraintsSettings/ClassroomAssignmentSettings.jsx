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
  // Initialize state with default values
  const [localWeight, setLocalWeight] = useState(weight || 0.7);
  const [isActive, setIsActive] = useState(true);
  const [feedback, setFeedback] = useState({ open: false, message: '', type: 'info' });

  // Handle weight change
  const handleWeightChange = (event, newValue) => {
    setLocalWeight(newValue);
    onUpdate({
      weight: newValue
    });
  };

  // Handle active state change
  const handleActiveChange = (event) => {
    const newValue = event.target.checked;
    setIsActive(newValue);
    if (onUpdate) {
      onUpdate({
        active: newValue
      });
    }
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
        
        <Grid item xs={12} sx={{ mt: 2 }}>
          <Typography variant="body2" color="text.secondary">
            Note: Detailed course-classroom type preferences are now managed in the section below.
          </Typography>
        </Grid>
      </Grid>
    </Box>
  );
};

export default ClassroomAssignmentSettings;