// ScheduleExamsForm.jsx
import React, { useState } from 'react';
import { 
  Box, 
  Typography, 
  Grid, 
  FormControl, 
  InputLabel, 
  Select, 
  MenuItem, 
  Button, 
  Chip, 
  Card, 
  CardContent, 
  Divider, 
  LinearProgress,
  Switch,
  Slider,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  FormControlLabel,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ScheduleIcon from '@mui/icons-material/Schedule';
import RoomIcon from '@mui/icons-material/Room';
import AssignmentIcon from '@mui/icons-material/Assignment';
import EventIcon from '@mui/icons-material/Event';
import TuneIcon from '@mui/icons-material/Tune';
import { 
  mockSemesters, 
  mockCourses, 
  generateScheduleApi 
} from '../services/mockData';

const ScheduleExamsForm = ({ onScheduleGenerated }) => {
  const [formData, setFormData] = useState({
    // Basic parameters
    semester: 1,
    courses: [1, 2, 3],
    
    // Exam-specific parameters
    maxExamsPerDay: 2,
    examTimeGap: 2,
    preferSameRoomForExams: true,
    commonExamScheduling: false,
    examDuration: 2, // hours
    bufferBetweenExams: 30, // minutes
    
    // Exam week settings
    examWeekStart: null,
    examWeekEnd: null,
    
    // Constraint settings
    enableExamConflictChecking: true,
    enableRoomCapacityChecking: true,
    enableProctorAssignment: false
  });
  
  const [commonExams, setCommonExams] = useState([]);
  const [isGenerating, setIsGenerating] = useState(false);

  const handleFormChange = (event) => {
    const { name, value } = event.target;
    setFormData({
      ...formData,
      [name]: value
    });
  };

  const handleSliderChange = (name, value) => {
    setFormData({
      ...formData,
      [name]: value
    });
  };

  const handleSwitchChange = (name, checked) => {
    setFormData({
      ...formData,
      [name]: checked
    });
  };

  const handleAddCommonExam = () => {
    // This would be used to add groups of courses that should have common exam times
    setCommonExams([
      ...commonExams,
      { id: Date.now(), courses: [] }
    ]);
  };

  const handleCommonExamChange = (id, courses) => {
    setCommonExams(
      commonExams.map(exam => 
        exam.id === id ? { ...exam, courses } : exam
      )
    );
  };

  const handleGenerateExamSchedule = () => {
    setIsGenerating(true);
    // Call API (mock)
    generateScheduleApi({
      ...formData, 
      scheduleType: 'exam',
      commonExams
    })
      .then(result => {
        setIsGenerating(false);
        if (onScheduleGenerated) {
          onScheduleGenerated(result.id);
        }
      })
      .catch(error => {
        console.error('Error generating exam schedule:', error);
        setIsGenerating(false);
      });
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        Create Exam Schedule
      </Typography>
      
      {/* Basic Settings */}
      <Accordion defaultExpanded sx={{ mb: 2 }}>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography variant="subtitle1" sx={{ display: 'flex', alignItems: 'center' }}>
            <EventIcon sx={{ mr: 1 }} />
            Basic Exam Parameters
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <FormControl fullWidth margin="normal">
                <InputLabel id="semester-label">Semester</InputLabel>
                <Select
                  labelId="semester-label"
                  name="semester"
                  value={formData.semester}
                  onChange={handleFormChange}
                  label="Semester"
                >
                  {mockSemesters.map(semester => (
                    <MenuItem key={semester.id} value={semester.id}>
                      {semester.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            
            <Grid item xs={12} md={6}>
              <FormControl fullWidth margin="normal">
                <InputLabel id="courses-label">Courses</InputLabel>
                <Select
                  labelId="courses-label"
                  name="courses"
                  multiple
                  value={formData.courses}
                  onChange={handleFormChange}
                  label="Courses"
                  renderValue={(selected) => (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                      {selected.map((value) => {
                        const course = mockCourses.find(c => c.id === value);
                        return <Chip key={value} label={course ? course.code : value} />;
                      })}
                    </Box>
                  )}
                >
                  {mockCourses.map(course => (
                    <MenuItem key={course.id} value={course.id}>
                      {course.code} - {course.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            
            <Grid item xs={12} md={6}>
              <Typography gutterBottom>Exam Duration (hours)</Typography>
              <Slider
                value={formData.examDuration}
                onChange={(e, newValue) => handleSliderChange('examDuration', newValue)}
                step={0.5}
                marks
                min={1}
                max={4}
                valueLabelDisplay="auto"
              />
            </Grid>
            
            <Grid item xs={12} md={6}>
              <Typography gutterBottom>Buffer Between Exams (minutes)</Typography>
              <Slider
                value={formData.bufferBetweenExams}
                onChange={(e, newValue) => handleSliderChange('bufferBetweenExams', newValue)}
                step={15}
                marks
                min={0}
                max={60}
                valueLabelDisplay="auto"
              />
            </Grid>
          </Grid>
        </AccordionDetails>
      </Accordion>
      
      {/* Exam Scheduling Constraints */}
      <Accordion defaultExpanded sx={{ mb: 2 }}>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography variant="subtitle1" sx={{ display: 'flex', alignItems: 'center' }}>
            <ScheduleIcon sx={{ mr: 1 }} />
            Exam Scheduling Constraints
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Typography gutterBottom>Maximum Exams Per Day: {formData.maxExamsPerDay}</Typography>
              <Slider
                value={formData.maxExamsPerDay}
                onChange={(e, newValue) => handleSliderChange('maxExamsPerDay', newValue)}
                step={1}
                marks
                min={1}
                max={4}
                valueLabelDisplay="auto"
              />
            </Grid>
            
            <Grid item xs={12} md={6}>
              <Typography gutterBottom>Minimum Hours Between Exams: {formData.examTimeGap}</Typography>
              <Slider
                value={formData.examTimeGap}
                onChange={(e, newValue) => handleSliderChange('examTimeGap', newValue)}
                step={0.5}
                marks
                min={0.5}
                max={4}
                valueLabelDisplay="auto"
              />
            </Grid>
            
            <Grid item xs={12} md={6}>
              <FormControlLabel
                control={
                  <Switch
                    checked={formData.preferSameRoomForExams}
                    onChange={(e) => handleSwitchChange('preferSameRoomForExams', e.target.checked)}
                  />
                }
                label="Prefer Same Room for Exams"
              />
            </Grid>
            
            <Grid item xs={12} md={6}>
              <FormControlLabel
                control={
                  <Switch
                    checked={formData.enableExamConflictChecking}
                    onChange={(e) => handleSwitchChange('enableExamConflictChecking', e.target.checked)}
                  />
                }
                label="Enable Student Exam Conflict Checking"
              />
            </Grid>
            
            <Grid item xs={12} md={6}>
              <FormControlLabel
                control={
                  <Switch
                    checked={formData.enableRoomCapacityChecking}
                    onChange={(e) => handleSwitchChange('enableRoomCapacityChecking', e.target.checked)}
                  />
                }
                label="Enable Room Capacity Checking"
              />
            </Grid>
            
            <Grid item xs={12} md={6}>
              <FormControlLabel
                control={
                  <Switch
                    checked={formData.enableProctorAssignment}
                    onChange={(e) => handleSwitchChange('enableProctorAssignment', e.target.checked)}
                  />
                }
                label="Enable Proctor Assignment"
              />
            </Grid>
          </Grid>
        </AccordionDetails>
      </Accordion>
      
      {/* Common Exam Settings */}
      <Accordion sx={{ mb: 2 }}>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography variant="subtitle1" sx={{ display: 'flex', alignItems: 'center' }}>
            <AssignmentIcon sx={{ mr: 1 }} />
            Common Exam Settings
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <FormControlLabel
            control={
              <Switch
                checked={formData.commonExamScheduling}
                onChange={(e) => handleSwitchChange('commonExamScheduling', e.target.checked)}
              />
            }
            label="Enable Common Exam Scheduling"
          />
          
          {formData.commonExamScheduling && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Common exams allow multiple course sections to have their exams at the same time.
                This is useful for large multi-section courses or related courses.
              </Typography>
              
              <Button 
                variant="outlined" 
                onClick={handleAddCommonExam}
                sx={{ mb: 2 }}
              >
                Add Common Exam Group
              </Button>
              
              {commonExams.map((examGroup, index) => (
                <Paper key={examGroup.id} variant="outlined" sx={{ p: 2, mb: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Common Exam Group {index + 1}
                  </Typography>
                  
                  <FormControl fullWidth margin="normal">
                    <InputLabel>Courses in this group</InputLabel>
                    <Select
                      multiple
                      value={examGroup.courses}
                      onChange={(e) => handleCommonExamChange(examGroup.id, e.target.value)}
                      renderValue={(selected) => (
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                          {selected.map((value) => {
                            const course = mockCourses.find(c => c.id === value);
                            return <Chip key={value} label={course ? course.code : value} />;
                          })}
                        </Box>
                      )}
                    >
                      {mockCourses.filter(c => formData.courses.includes(c.id)).map(course => (
                        <MenuItem key={course.id} value={course.id}>
                          {course.code} - {course.name}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </Paper>
              ))}
            </Box>
          )}
        </AccordionDetails>
      </Accordion>
      
      {/* Generate Button */}
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
        <Button
          variant="contained"
          color="primary"
          size="large"
          onClick={handleGenerateExamSchedule}
          disabled={isGenerating}
          sx={{ minWidth: 200 }}
        >
          {isGenerating ? 'Generating...' : 'Generate Exam Schedule'}
        </Button>
      </Box>
      
      {isGenerating && (
        <LinearProgress sx={{ mt: 2 }} />
      )}
    </Box>
  );
};

export default ScheduleExamsForm;