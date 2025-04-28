import React, { useState,useEffect } from 'react';
import { 
  Box, 
  Typography, 
  Tabs, 
  Tab, 
  Button, 
  TableContainer, 
  Table, 
  TableHead, 
  TableBody, 
  TableRow, 
  TableCell, 
  Paper, 
  IconButton,
  Tooltip,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Divider,
  Card,
  CardContent,
  Grid,
  Slider,
  List,
  ListItem,
  ListItemText,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  Alert,
  FormControlLabel,
  Switch
} from '@mui/material';
import { 
  mockClassrooms, 
  mockConstraints,
  mockTeachers,
  mockCourses,
  mockSemesters,
  mockEquipmentTypes,
  mockClassroomEquipment,
  mockCampuses,
  mockCourseRoomTypeMatching,
  mockRoomTypeEquipment,
  mockCourseSubjectTypes,
  findSuitableClassrooms,
  assignClassroomsApi
} from '../services/mockData';

import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import SaveIcon from '@mui/icons-material/Save';
import ImportExportIcon from '@mui/icons-material/ImportExport';
import FileUploadIcon from '@mui/icons-material/FileUpload';
import FileDownloadIcon from '@mui/icons-material/FileDownload';
import SettingsIcon from '@mui/icons-material/Settings';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import EventIcon from '@mui/icons-material/Event';
import ScheduleIcon from '@mui/icons-material/Schedule';
import RestartAltIcon from '@mui/icons-material/RestartAlt';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import RefreshIcon from '@mui/icons-material/Refresh';

// Import constraint components
import TeacherAvailabilitySettings from './constraints/TeacherAvailabilitySettings';
import ClassroomAssignmentSettings from './constraints/ClassroomAssignmentSettings';
import ClassroomAvailabilitySettings from './constraints/ClassroomAvailabilitySettings';

const DataManagement = ({ 
  parameters, 
  onParametersUpdate, 
  activePresetId, 
  presets,
  onPresetSelect,
  initialTab = 0,
  initialConstraintSubTab = 0 }) => {
  // Main tab state
  const [tabValue, setTabValue] = useState(initialTab);
  // Sub-tab states
  const [constraintSubTab, setConstraintSubTab] = useState(initialConstraintSubTab);
  const [resourceSubTab, setResourceSubTab] = useState(0);
  const [timeSubTab, setTimeSubTab] = useState(0);
  const [templateSubTab, setTemplateSubTab] = useState(0);
  const [equipmentSubTab, setEquipmentSubTab] = useState(0);
  
  // Parameter management state
  const [localParameters, setLocalParameters] = useState({...parameters});
  
  // Constraint Management Preview Status
  const [constraintPreview, setConstraintPreview] = useState({
    isLoading: false,
    classroomAssignmentReady: false,
    classroomAssignments: [],
    conflicts: 0,
    error: null
  });
  
  // Add after existing state variables
  const [newPresetDialog, setNewPresetDialog] = useState(false);
  const [newPresetName, setNewPresetName] = useState('');
  const [newPresetDesc, setNewPresetDesc] = useState('');
  
  const [parametersModified, setParametersModified] = useState(false);

  // Add classroom and equipment management related state
  const [selectedClassroom, setSelectedClassroom] = useState(null);
  const [classroomEditDialog, setClassroomEditDialog] = useState(false);
  const [classroomEquipmentList, setClassroomEquipmentList] = useState([]);
  const [selectedEquipment, setSelectedEquipment] = useState(null);
  const [editingEquipment, setEditingEquipment] = useState({
    quantity: 0,
    status: 'Good'
  });
  
  // Add edit classroom state - remove building and campus field edit permissions
  const [editingClassroom, setEditingClassroom] = useState({
    name: '',
    capacity: 0,
    type: '',
    features: []
  });
  
  // Add course classroom type matching related state
  const [selectedCourse, setSelectedCourse] = useState(mockCourses[0]?.id || '');
  const [selectedRoomType, setSelectedRoomType] = useState('');
  const [requiredFeatures, setRequiredFeatures] = useState([]);
  const [matchingClassrooms, setMatchingClassrooms] = useState([]);

  // Update tab values when props change
  useEffect(() => {
    setTabValue(initialTab);
  }, [initialTab]);
  
  useEffect(() => {
    setConstraintSubTab(initialConstraintSubTab);
  }, [initialConstraintSubTab]);
  
  // Update classroom type and equipment requirements when course is selected
  useEffect(() => {
    if (selectedCourse && constraintSubTab === 2) {
      // Get course information
      const course = mockCourses.find(c => c.id === selectedCourse);
      if (course) {
        // Get course type
        const courseSubject = mockCourseSubjectTypes.find(cs => cs.subjectId === course.subjectId);
        const courseType = courseSubject?.courseType || '';
        
        // Find recommended classroom types and equipment for this course type in matchRoomTypeMatching
        const matching = mockCourseRoomTypeMatching.find(m => m.courseType === courseType);
        
        if (matching) {
          // Set default classroom type to the first recommended type
          setSelectedRoomType(matching.preferredRoomTypes[0] || '');
          // Set required features
          setRequiredFeatures(matching.requiredFeatures || []);
          
          // Find matching classrooms
          updateMatchingClassrooms(matching.preferredRoomTypes[0], matching.requiredFeatures);
        } else {
          setSelectedRoomType('');
          setRequiredFeatures([]);
          setMatchingClassrooms([]);
        }
      }
    }
  }, [selectedCourse, constraintSubTab]);
  
  // When classroom type or equipment requirements change, update matching classrooms
  const updateMatchingClassrooms = (roomType, features) => {
    if (!roomType && !features) {
      setMatchingClassrooms([]);
      return;
    }
    
    let filtered = [...mockClassrooms];
    
    // Filter by classroom type
    if (roomType && roomType !== 'any') {
      filtered = filtered.filter(room => room.type === roomType);
    }
    
    // Filter by necessary equipment
    if (features && features.length > 0) {
      filtered = filtered.filter(room => {
        // Check if the classroom has all necessary equipment
        return features.every(feature => 
          room.features && room.features.includes(feature)
        );
      });
    }
    
    // Calculate matching score
    filtered = filtered.map(room => {
      // If classroom type fully matches, higher score
      const typeScore = room.type === roomType ? 1.0 : 0.5;
      
      // Calculate equipment matching rate
      let featureScore = 1.0;
      if (features && features.length > 0) {
        const matchedFeatures = features.filter(f => room.features.includes(f)).length;
        featureScore = matchedFeatures / features.length;
      }
      
      // Composite score (simple weighted average)
      const totalScore = (typeScore * 0.6) + (featureScore * 0.4);
      
      return {
        ...room,
        matchScore: totalScore
      };
    });
    
    // Sort by matching score
    filtered.sort((a, b) => b.matchScore - a.matchScore);
    
    setMatchingClassrooms(filtered);
  };
  
  // Parameter presets
  const parameterPresets = [
    { id: 1, name: "Default Parameters", description: "System default configuration" },
    { id: 2, name: "Optimized for Engineering", description: "Prioritizes lab availability" },
    { id: 3, name: "Space Optimization", description: "Maximizes classroom utilization" },
    { id: 4, name: "Faculty Preference", description: "Prioritizes faculty scheduling preferences" }
  ];
  
  // State for currently editing preset
  const [editingPresetId, setEditingPresetId] = useState(null);

  const handleTabChange = (event, newValue) => {
    setTabValue(newValue);
    
    // When switching to constraint management tab, preload classroom assignment test data
    if (newValue === 1) {
      // Reset constraint management preview status
      setConstraintPreview({
        isLoading: false,
        classroomAssignmentReady: false,
        classroomAssignments: [],
        conflicts: 0,
        error: null
      });
    }
  };
  
  // Update saveParametersToSystem to use the parent's update function
  const saveParametersToSystem = () => {
  if (onParametersUpdate) {
    onParametersUpdate(localParameters);
  }
  alert('System parameters have been updated successfully');
  };

   // Update loadParameters to use the parent's preset selection
  const loadParameters = (presetId) => {
    if (onPresetSelect) {
      onPresetSelect(presetId);
    }
  };
  
  // Parameter change handler
  const handleParameterChange = (param, value) => {
    setLocalParameters(prev => {
      const newParams = {
        ...prev,
        [param]: value
      };
      // Check if parameters are different from original parameters
      setParametersModified(JSON.stringify(newParams) !== JSON.stringify(parameters));
      return newParams;
    });
  };
  // Add after other processing functions
  const saveCurrentAsPreset = () => {
    if (newPresetName.trim() === '') return;
    
    const newPresetId = Math.max(...presets.map(p => p.id)) + 1;
    const newPreset = {
      id: newPresetId,
      name: newPresetName,
      description: newPresetDesc || `Custom preset created on ${new Date().toLocaleDateString()}`
    };
    
    alert(`Added new preset: ${newPresetName}`);
    
    setNewPresetDialog(false);
    setNewPresetName('');
    setNewPresetDesc('');
  };

  // Modify classroom selection processing function
  const handleClassroomSelect = (classroomId) => {
    const classroom = mockClassrooms.find(c => c.id === classroomId);
    setSelectedClassroom(classroom);
    
    // Only load equipment list on equipment management tab
    if (resourceSubTab === 2) {
      const equipment = mockClassroomEquipment.filter(
        item => item.classroomId === classroomId
      );
      setClassroomEquipmentList(equipment);
    }
  };
  
  // Add classroom edit function
  const handleClassroomEdit = (classroomId) => {
    const classroom = mockClassrooms.find(c => c.id === classroomId);
    if (classroom) {
      setEditingClassroom({
        id: classroom.id,
        name: classroom.name,
        capacity: classroom.capacity,
        type: classroom.type,
        features: classroom.features || []
      });
      setClassroomEditDialog(true);
    }
  };
  
  // Add classroom update function
  const handleClassroomUpdate = () => {
    // Update classroom information logic
    alert(`Classroom ${editingClassroom.name} information has been updated`);
    setClassroomEditDialog(false);
  };
  
  const handleEquipmentSelect = (equipmentId) => {
    const equipment = classroomEquipmentList.find(e => e.id === equipmentId);
    if (equipment) {
      setSelectedEquipment(equipment);
      setEditingEquipment({
        quantity: equipment.quantity,
        status: equipment.status
      });
    }
  };
  
  const handleEquipmentUpdate = () => {
    // Update Device Information Logic
    alert(`Equipment ${selectedEquipment.id} has been updated`);
    setSelectedEquipment(null);
  };

  // Classroom assignment test function
  const handleClassroomAssignmentTest = () => {
    // Use mock data example, later replace with real API call
    setConstraintPreview({
      ...constraintPreview,
      isLoading: true,
      classroomAssignmentReady: false,
      error: null
    });
    
    // Collect selected courses for testing
    const coursesToTest = [selectedCourse].filter(Boolean);
    
    if (coursesToTest.length === 0) {
      setConstraintPreview({
        ...constraintPreview,
        isLoading: false,
        error: "Please select at least one course for testing"
      });
      return;
    }
    
    // Simulate API call delay
    setTimeout(() => {
      try {
        // This should call the assignClassroomsApi function, but it may not be implemented yet, so we use mock data
        const testResults = coursesToTest.map(courseId => {
          const course = mockCourses.find(c => c.id === courseId);
          const subject = mockCourseSubjectTypes.find(cs => cs.subjectId === course?.subjectId);
          const courseType = subject?.courseType || '';
          
          // Get matching classroom type information
          const matching = mockCourseRoomTypeMatching.find(m => m.courseType === courseType);
          
          // Find suitable classroom
          const suitableRoom = matchingClassrooms.length > 0 ? 
            matchingClassrooms[0] : 
            mockClassrooms.find(r => r.type === (matching?.preferredRoomTypes[0] || ''));
          
          return {
            courseId,
            roomId: suitableRoom?.id || null,
            matchScore: suitableRoom ? 
              (suitableRoom.type === selectedRoomType ? 0.95 : 0.75) : 0,
            roomName: suitableRoom ? 
              `${suitableRoom.building}-${suitableRoom.name}` : 'No suitable classroom found',
            conflict: !suitableRoom
          };
        });
        
        setConstraintPreview({
          ...constraintPreview,
          isLoading: false,
          classroomAssignmentReady: true,
          classroomAssignments: testResults,
          conflicts: testResults.filter(a => a.conflict).length
        });
      } catch (error) {
        setConstraintPreview({
          ...constraintPreview,
          isLoading: false,
          error: error.message || "Error occurred during classroom assignment test"
        });
      }
    }, 1500);
  };

  // Constraint management sub-tab switch function
  const handleConstraintSubTabChange = (event, newValue) => {
    setConstraintSubTab(newValue);
  };

  // Render classroom assignment test results
  const renderClassroomAssignmentPreview = () => {
    if (constraintPreview.error) {
      return (
        <Alert severity="error" sx={{ mt: 2, mb: 2 }}>
          {constraintPreview.error}
        </Alert>
      );
    }
    
    if (!constraintPreview.classroomAssignmentReady) {
      return (
        <Box sx={{ textAlign: 'center', py: 3 }}>
          <Button 
            variant="contained" 
            onClick={handleClassroomAssignmentTest}
            disabled={constraintPreview.isLoading || !selectedCourse}
          >
            {constraintPreview.isLoading ? 'Processing...' : 'Test Classroom Assignment Algorithm'}
          </Button>
        </Box>
      );
    }
    
    return (
      <Box>
        <Typography variant="h6" gutterBottom>
          Course-Classroom Assignment Results
          {constraintPreview.conflicts > 0 && (
            <Chip 
              color="error" 
              size="small" 
              label={`${constraintPreview.conflicts} conflicts`} 
              sx={{ ml: 2 }}
            />
          )}
        </Typography>
        
        <TableContainer component={Paper}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Course</TableCell>
                <TableCell>Assigned Room</TableCell>
                <TableCell>Match Score</TableCell>
                <TableCell>Room Features</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {constraintPreview.classroomAssignments.map((assignment, index) => {
                const course = mockCourses.find(c => c.id === assignment.courseId);
                const room = mockClassrooms.find(r => r.id === assignment.roomId);
                
                return (
                  <TableRow key={index} hover selected={assignment.conflict}>
                    <TableCell>{course ? `${course.code}: ${course.name}` : 'Unknown Course'}</TableCell>
                    <TableCell>
                      {assignment.conflict ? (
                        <Typography color="error">{assignment.roomName}</Typography>
                      ) : (
                        <Typography>{assignment.roomName}</Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      {assignment.matchScore ? assignment.matchScore.toFixed(2) : 'N/A'}
                    </TableCell>
                    <TableCell>
                      {room ? (room.features ? room.features.join(', ') : 'None') : 'N/A'}
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
        
        <Box sx={{ mt: 2 }}>
          <Button 
            variant="outlined" 
            onClick={handleClassroomAssignmentTest}
            startIcon={<RefreshIcon />}
          >
            Retest
          </Button>
        </Box>
      </Box>
    );
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        System Configuration
      </Typography>
      
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
        <Tabs 
          value={tabValue} 
          onChange={handleTabChange} 
          aria-label="data management tabs"
          variant="scrollable"
          scrollButtons="auto"
        >
          <Tab label="Parameter Settings" />
          <Tab label="Constraint Management" />
          <Tab label="Resource Management" />
          <Tab label="Time Management" />
          <Tab label="Developer Options" />
          <Tab label="Templates & Presets" />
        </Tabs>
      </Box>
      
      {/* Parameter Settings Tab */}
      {tabValue === 0 && (
        <Box>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="subtitle1">Parameter Settings Management</Typography>
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button 
                variant="outlined" 
                startIcon={<SaveIcon />}
                onClick={() => setNewPresetDialog(true)}
              >
                Save Current as Preset
              </Button>
              <Button 
                variant="outlined" 
                startIcon={<FileUploadIcon />}
              >
                Import Parameters
              </Button>
              <Button 
                variant="outlined" 
                startIcon={<FileDownloadIcon />}
              >
                Export Parameters
              </Button>
              <Button 
                variant="outlined" 
                startIcon={<RestartAltIcon />}
                onClick={() => {
                  setLocalParameters({...parameters});
                  setParametersModified(false);
                }}
                sx={{ ml: 1 }}
              >
                Reset Changes
              </Button>
            </Box>
          </Box>
          
          <Grid container spacing={2}>
            <Grid item xs={12} md={4}>
              <Card variant="outlined" sx={{ mb: 2 }}>
                <CardContent>
                  <Typography variant="subtitle2" gutterBottom>
                    Available Parameter Presets
                  </Typography>
                  <List>
                    {presets.map(preset => (
                      <ListItem 
                        button 
                        key={preset.id}
                        selected={editingPresetId === preset.id}
                        onClick={() => loadParameters(preset.id)}
                      >
                        <ListItemText 
                          primary={preset.name} 
                          secondary={preset.description} 
                        />
                        {editingPresetId === preset.id && (
                          <Chip 
                            size="small" 
                            label="Active" 
                            color="primary" 
                            icon={<CheckCircleIcon />} 
                          />
                        )}
                      </ListItem>
                    ))}
                  </List>
                </CardContent>
              </Card>
            </Grid>
            
            <Grid item xs={12} md={8}>
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="subtitle2" gutterBottom>
                    Parameter Categories
                  </Typography>
                  <Accordion defaultExpanded>
                    <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                      <Typography>Academic Parameters</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>Parameter</TableCell>
                              <TableCell>Value</TableCell>
                              <TableCell>Description</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {/* Gender Segregation Setting */}
                            <TableRow>
                              <TableCell>Gender Segregation</TableCell>
                              <TableCell>
                                <FormControl>
                                  <Select
                                    value={localParameters.genderSegregation}
                                    onChange={(e) => handleParameterChange('genderSegregation', e.target.value)}
                                    size="small"
                                  >
                                    <MenuItem value={true}>Enabled</MenuItem>
                                    <MenuItem value={false}>Disabled (Mixed Classes)</MenuItem>
                                  </Select>
                                </FormControl>
                              </TableCell>
                              <TableCell>Determines if classes should be segregated by gender</TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell>Faculty Workload Balance</TableCell>
                              <TableCell>
                                <Slider
                                  value={localParameters.facultyWorkloadBalance}
                                  onChange={(e, newValue) => handleParameterChange('facultyWorkloadBalance', newValue)}
                                  step={0.1}
                                  min={0}
                                  max={1}
                                  valueLabelDisplay="auto"
                                />
                              </TableCell>
                              <TableCell>Balances teaching load among faculty</TableCell>
                            </TableRow>
                            
                            
                          </TableBody>
                        </Table>
                      </TableContainer>
                    </AccordionDetails>
                  </Accordion>
                  
                  <Accordion>
                    <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                      <Typography>Campus & Location Parameters</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>Parameter</TableCell>
                              <TableCell>Value</TableCell>
                              <TableCell>Description</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {/* Cross-School Enrollment */}
                            <TableRow>
                              <TableCell>Allow Cross-School Enrollment</TableCell>
                              <TableCell>
                                <FormControl>
                                  <Select
                                    value={parameters.allowCrossSchoolEnrollment}
                                    onChange={(e) => handleParameterChange('allowCrossSchoolEnrollment', e.target.value)}
                                    size="small"
                                  >
                                    <MenuItem value={true}>Yes</MenuItem>
                                    <MenuItem value={false}>No</MenuItem>
                                  </Select>
                                </FormControl>
                              </TableCell>
                              <TableCell>Allows students to enroll in courses from different schools</TableCell>
                            </TableRow>
                            
                            {/* Multi-Campus Constraints */}
                            <TableRow>
                              <TableCell>Enable Multi-Campus Constraints</TableCell>
                              <TableCell>
                                <FormControl>
                                  <Select
                                    value={parameters.enableMultiCampusConstraints}
                                    onChange={(e) => handleParameterChange('enableMultiCampusConstraints', e.target.value)}
                                    size="small"
                                  >
                                    <MenuItem value={true}>Yes</MenuItem>
                                    <MenuItem value={false}>No</MenuItem>
                                  </Select>
                                </FormControl>
                              </TableCell>
                              <TableCell>Consider constraints between multiple campuses</TableCell>
                            </TableRow>
                            
                            {/* Prioritize Home Buildings */}
                            <TableRow>
                              <TableCell>Prioritize Home Buildings</TableCell>
                              <TableCell>
                                <FormControl>
                                  <Select
                                    value={parameters.prioritizeHomeBuildings}
                                    onChange={(e) => handleParameterChange('prioritizeHomeBuildings', e.target.value)}
                                    size="small"
                                  >
                                    <MenuItem value={true}>Yes</MenuItem>
                                    <MenuItem value={false}>No</MenuItem>
                                  </Select>
                                </FormControl>
                              </TableCell>
                              <TableCell>Prioritize scheduling courses in departmental buildings to reduce travel</TableCell>
                            </TableRow>
                            
                            {/* Campus Travel Time Weight */}
                            <TableRow>
                              <TableCell>Campus Travel Time Weight</TableCell>
                              <TableCell>
                                <Slider
                                  value={parameters.campusTravelTimeWeight}
                                  onChange={(e, newValue) => handleParameterChange('campusTravelTimeWeight', newValue)}
                                  step={0.1}
                                  min={0}
                                  max={1}
                                  valueLabelDisplay="auto"
                                />
                              </TableCell>
                              <TableCell>Weight given to minimizing campus travel time</TableCell>
                            </TableRow>
                            
                            {/* Minimum Travel Time */}
                            <TableRow>
                              <TableCell>Minimum Travel Time (minutes)</TableCell>
                              <TableCell>
                                <Slider
                                  value={parameters.minimumTravelTime}
                                  onChange={(e, newValue) => handleParameterChange('minimumTravelTime', newValue)}
                                  step={5}
                                  min={0}
                                  max={60}
                                  valueLabelDisplay="auto"
                                />
                              </TableCell>
                              <TableCell>Minimum time required between classes in different buildings</TableCell>
                            </TableRow>
                          </TableBody>
                        </Table>
                      </TableContainer>
                    </AccordionDetails>
                  </Accordion>

                  <Accordion>
                    <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                      <Typography>Time Constraint Parameters</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>Parameter</TableCell>
                              <TableCell>Value</TableCell>
                              <TableCell>Description</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {/* Maximum Consecutive Classes */}
                            <TableRow>
                              <TableCell>Maximum Consecutive Classes</TableCell>
                              <TableCell>
                                <Slider
                                  value={parameters.maximumConsecutiveClasses}
                                  onChange={(e, newValue) => handleParameterChange('maximumConsecutiveClasses', newValue)}
                                  step={1}
                                  min={1}
                                  max={6}
                                  marks
                                  valueLabelDisplay="auto"
                                />
                              </TableCell>
                              <TableCell>Maximum number of consecutive teaching hours for faculty</TableCell>
                            </TableRow>
                            
                            {/* Holiday Exclusions */}
                            <TableRow>
                              <TableCell>Exclude Holiday Dates</TableCell>
                              <TableCell>
                                <FormControl>
                                  <Select
                                    value={parameters.holidayExclusions}
                                    onChange={(e) => handleParameterChange('holidayExclusions', e.target.value)}
                                    size="small"
                                  >
                                    <MenuItem value={true}>Yes</MenuItem>
                                    <MenuItem value={false}>No</MenuItem>
                                  </Select>
                                </FormControl>
                              </TableCell>
                              <TableCell>Automatically exclude holidays when scheduling</TableCell>
                            </TableRow>
                            
                            {/* Ramadan Schedule */}
                            <TableRow>
                              <TableCell>Enable Ramadan Schedule</TableCell>
                              <TableCell>
                                <FormControl>
                                  <Select
                                    value={parameters.enableRamadanSchedule}
                                    onChange={(e) => handleParameterChange('enableRamadanSchedule', e.target.value)}
                                    size="small"
                                  >
                                    <MenuItem value={true}>Yes</MenuItem>
                                    <MenuItem value={false}>No</MenuItem>
                                  </Select>
                                </FormControl>
                              </TableCell>
                              <TableCell>Enable special timetables during Ramadan</TableCell>
                            </TableRow>
                          </TableBody>
                        </Table>
                      </TableContainer>
                    </AccordionDetails>
                  </Accordion>

                  <Accordion>
                    <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                      <Typography>Course Organization Parameters</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>Parameter</TableCell>
                              <TableCell>Value</TableCell>
                              <TableCell>Description</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {/* Cross-Listed Courses */}
                            <TableRow>
                              <TableCell>Allow Cross-Listed Courses</TableCell>
                              <TableCell>
                                <FormControl>
                                  <Select
                                    value={parameters.allowCrossListedCourses}
                                    onChange={(e) => handleParameterChange('allowCrossListedCourses', e.target.value)}
                                    size="small"
                                  >
                                    <MenuItem value={true}>Yes</MenuItem>
                                    <MenuItem value={false}>No</MenuItem>
                                  </Select>
                                </FormControl>
                              </TableCell>
                              <TableCell>Allow courses with different codes to share the same schedule</TableCell>
                            </TableRow>
                            
                            {/* Cross-Department Teaching */}
                            <TableRow>
                              <TableCell>Allow Cross-Department Teaching</TableCell>
                              <TableCell>
                                <FormControl>
                                  <Select
                                    value={parameters.allowCrossDepartmentTeaching}
                                    onChange={(e) => handleParameterChange('allowCrossDepartmentTeaching', e.target.value)}
                                    size="small"
                                  >
                                    <MenuItem value={true}>Yes</MenuItem>
                                    <MenuItem value={false}>No</MenuItem>
                                  </Select>
                                </FormControl>
                              </TableCell>
                              <TableCell>Allow faculty to teach courses in different departments</TableCell>
                            </TableRow>
                            
                            {/* Generate Alternatives */}
                            <TableRow>
                              <TableCell>Generate Alternative Schedules</TableCell>
                              <TableCell>
                                <FormControl>
                                  <Select
                                    value={parameters.generateAlternatives}
                                    onChange={(e) => handleParameterChange('generateAlternatives', e.target.value)}
                                    size="small"
                                  >
                                    <MenuItem value={true}>Yes</MenuItem>
                                    <MenuItem value={false}>No</MenuItem>
                                  </Select>
                                </FormControl>
                              </TableCell>
                              <TableCell>Generate multiple scheduling options to choose from</TableCell>
                            </TableRow>
                          </TableBody>
                        </Table>
                      </TableContainer>
                    </AccordionDetails>
                  </Accordion>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          <Typography sx={{ mt: 2 }}>
            The system will now use {localParameters.useAI ? 'AI-enhanced' : 'traditional'} scheduling algorithms by default.
          </Typography>
        </Box>
      )}
      
      {/* Constraint Management Tab */}
      {tabValue === 1 && (
        <Box>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="subtitle1">Constraint Management</Typography>
            <Button 
              variant="contained" 
              color="primary" 
              startIcon={<AddIcon />}
            >
              Add New Constraint
            </Button>
          </Box>
          
          {/* Add sub-tabs */}
          <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
            <Tabs 
              value={constraintSubTab} 
              onChange={handleConstraintSubTabChange}
            >
              <Tab label="General Constraints" />
              <Tab label="Teacher Availability" />
              <Tab label="Classroom Assignment" />
              <Tab label="Classroom Availability" />
            </Tabs>
          </Box>
          
          {/* General constraints */}
          {constraintSubTab === 0 && (
            <>
              <TableContainer component={Paper} variant="outlined">
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Name</TableCell>
                      <TableCell>Type</TableCell>
                      <TableCell>Description</TableCell>
                      <TableCell>Weight</TableCell>
                      <TableCell>Active</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {mockConstraints.map((constraint) => (
                      <TableRow key={constraint.id}>
                        <TableCell>{constraint.name}</TableCell>
                        <TableCell>
                          <Chip 
                            label={constraint.type} 
                            color={constraint.type === 'Hard' ? 'error' : 'primary'} 
                            size="small" 
                          />
                        </TableCell>
                        <TableCell>{constraint.description}</TableCell>
                        <TableCell>{constraint.weight.toFixed(1)}</TableCell>
                        <TableCell>{constraint.isActive ? 'Yes' : 'No'}</TableCell>
                        <TableCell>
                          <Tooltip title="Edit">
                            <IconButton size="small">
                              <EditIcon />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="Delete">
                            <IconButton size="small">
                              <DeleteIcon />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
              
              {/* Constraint categories */}
              <Box sx={{ mt: 3 }}>
                <Typography variant="subtitle2" gutterBottom>Constraint Categories</Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12} md={4}>
                    <Card variant="outlined">
                      <CardContent>
                        <Typography variant="subtitle2">Time Constraints</Typography>
                        <Typography variant="body2" color="text.secondary">
                          Constraints related to scheduling times
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Card variant="outlined">
                      <CardContent>
                        <Typography variant="subtitle2">Space Constraints</Typography>
                        <Typography variant="body2" color="text.secondary">
                          Constraints related to room assignments
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Card variant="outlined">
                      <CardContent>
                        <Typography variant="subtitle2">Resource Constraints</Typography>
                        <Typography variant="body2" color="text.secondary">
                          Constraints related to equipment and resources
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                </Grid>
              </Box>
            </>
          )}
          
          {/* Teacher Availability */}
          {constraintSubTab === 1 && (
            <TeacherAvailabilitySettings 
              teachers={mockTeachers} 
              availabilitySettings={{}}
              semesterId={1}
              onUpdate={() => {}}
            />
          )}
          
          {/* Classroom Assignment */}
          {constraintSubTab === 2 && (
            <Box>
            <ClassroomAssignmentSettings 
              courses={mockCourses}
              classrooms={mockClassrooms}
              weight={0.7}
              onUpdate={() => {}}
            />
              
              {/* 课程-教室类型偏好与设备关系 */}
              <Box sx={{ mt: 4 }}>
                <Typography variant="h6" gutterBottom>Course-Classroom Type Preferences</Typography>
                <Typography variant="body2" color="text.secondary" paragraph>
                  Define which classroom types are preferred for specific course types and their required equipment. These preferences will be used during the scheduling process.
                </Typography>
                
                <Grid container spacing={3}>
                  <Grid item xs={12} md={4}>
                    <FormControl fullWidth sx={{ mb: 2 }}>
                      <InputLabel>Select Course</InputLabel>
                      <Select
                        value={selectedCourse}
                        label="Select Course"
                        onChange={(e) => setSelectedCourse(e.target.value)}
                      >
                        {mockCourses.map(course => (
                          <MenuItem key={course.id} value={course.id}>
                            {course.code}: {course.name}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                    
                    <FormControl fullWidth sx={{ mb: 2 }}>
                      <InputLabel>Preferred Room Type</InputLabel>
                      <Select
                        value={selectedRoomType}
                        label="Preferred Room Type"
                        onChange={(e) => {
                          setSelectedRoomType(e.target.value);
                          updateMatchingClassrooms(e.target.value, requiredFeatures);
                        }}
                      >
                        <MenuItem value="ComputerLab">Computer Lab</MenuItem>
                        <MenuItem value="Lecture">Lecture Room</MenuItem>
                        <MenuItem value="LargeHall">Large Hall</MenuItem>
                        <MenuItem value="Laboratory">Laboratory</MenuItem>
                        <MenuItem value="any">Any Room Type</MenuItem>
                      </Select>
                    </FormControl>
                    
                    <FormControl fullWidth sx={{ mb: 2 }}>
                      <InputLabel>Required Features</InputLabel>
                      <Select
                        multiple
                        value={requiredFeatures}
                        label="Required Features"
                        onChange={(e) => {
                          setRequiredFeatures(e.target.value);
                          updateMatchingClassrooms(selectedRoomType, e.target.value);
                        }}
                        renderValue={(selected) => (
                          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                            {selected.map((value) => (
                              <Chip key={value} label={value} size="small" />
                            ))}
                          </Box>
                        )}
                      >
                        <MenuItem value="Projector">Projector</MenuItem>
                        <MenuItem value="Computers">Computers</MenuItem>
                        <MenuItem value="Interactive Whiteboard">Interactive Whiteboard</MenuItem>
                        <MenuItem value="Advanced Audio">Advanced Audio</MenuItem>
                        <MenuItem value="Lab Equipment">Lab Equipment</MenuItem>
                        <MenuItem value="Safety Facilities">Safety Facilities</MenuItem>
                        <MenuItem value="Whiteboard">Whiteboard</MenuItem>
                        <MenuItem value="Audio System">Audio System</MenuItem>
                        <MenuItem value="Dual Projector">Dual Projector</MenuItem>
                      </Select>
                    </FormControl>
                    
                    <Button 
                      variant="contained" 
                      color="primary" 
                      fullWidth
                      onClick={handleClassroomAssignmentTest}
                    >
                      Test Classroom Assignment
                    </Button>
                  </Grid>
                  
                  <Grid item xs={12} md={8}>
                    <Typography variant="subtitle1" gutterBottom>Available Matching Classrooms</Typography>
                    <TableContainer component={Paper} variant="outlined">
                      <Table size="small">
                        <TableHead>
                          <TableRow>
                            <TableCell>Classroom</TableCell>
                            <TableCell>Type</TableCell>
                            <TableCell>Capacity</TableCell>
                            <TableCell>Features</TableCell>
                            <TableCell>Match Score</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {matchingClassrooms.length > 0 ? (
                            matchingClassrooms.map((room) => (
                              <TableRow key={room.id} hover>
                                <TableCell>{room.building}-{room.name}</TableCell>
                                <TableCell>{room.type}</TableCell>
                                <TableCell>{room.capacity}</TableCell>
                                <TableCell>
                                  {room.features.map((feature, i) => (
                                    <Chip 
                                      key={i} 
                                      label={feature} 
                                      size="small" 
                                      color={requiredFeatures.includes(feature) ? 'primary' : 'default'}
                                      variant={requiredFeatures.includes(feature) ? 'filled' : 'outlined'}
                                      sx={{ mr: 0.5, mb: 0.5 }}
                                    />
                                  ))}
                                </TableCell>
                                <TableCell>
                                  <Chip 
                                    label={
                                      room.matchScore >= 0.9 ? "Excellent" : 
                                      room.matchScore >= 0.7 ? "Good" : 
                                      room.matchScore >= 0.5 ? "Fair" : "Poor"
                                    }
                                    color={
                                      room.matchScore >= 0.9 ? "success" : 
                                      room.matchScore >= 0.7 ? "primary" : 
                                      room.matchScore >= 0.5 ? "warning" : "error"
                                    }
                                    size="small"
                                  />
                                </TableCell>
                              </TableRow>
                            ))
                          ) : (
                            <TableRow>
                              <TableCell colSpan={5} align="center">
                                No matching classrooms found. Try adjusting your criteria.
                              </TableCell>
                            </TableRow>
                          )}
                        </TableBody>
                      </Table>
                    </TableContainer>
                    
                    {constraintPreview.classroomAssignmentReady && renderClassroomAssignmentPreview()}
                    
                    <Box sx={{ mt: 2, textAlign: 'right' }}>
                      <Button 
                        variant="contained" 
                        color="primary"
                        onClick={() => {
                          // 保存当前的课程-教室类型偏好设置
                          alert('Course classroom type preferences have been saved');
                        }}
                      >
                        Save Preferences
                      </Button>
                    </Box>
                  </Grid>
                </Grid>
                
                <Box sx={{ mt: 4 }}>
                  <Typography variant="subtitle1" gutterBottom>Equipment Available by Classroom Type</Typography>
                  <TableContainer component={Paper} variant="outlined">
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Room Type</TableCell>
                          <TableCell>Standard Equipment</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {mockRoomTypeEquipment.map((item, index) => (
                          <TableRow key={index}>
                            <TableCell>{item.roomType}</TableCell>
                            <TableCell>
                              {item.equipmentTypeIds.map(id => {
                                const equipment = mockEquipmentTypes.find(e => e.id === id);
                                return equipment ? (
                                  <Chip 
                                    key={id}
                                    label={equipment.name}
                                    size="small"
                                    sx={{ mr: 0.5, mb: 0.5 }}
                                    color={requiredFeatures.includes(equipment.name) ? 'primary' : 'default'}
                                  />
                                ) : null;
                              })}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Box>
              </Box>
            </Box>
          )}
          
          {/* Classroom Availability */}
          {constraintSubTab === 3 && (
            <ClassroomAvailabilitySettings 
              classrooms={mockClassrooms}
              availabilitySettings={{}}
              semesterId={1}
              onUpdate={() => {}}
            />
          )}
        </Box>
      )}
      
      {parametersModified && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Parameters have been modified. Click "Save All Parameter Settings" to apply changes.
        </Alert>
      )}
       {/* Add save button at the bottom */}
      <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end' }}>
              <Button 
                variant="contained" 
                color="primary" 
          onClick={saveParametersToSystem}
              >
          Save All Parameter Settings
              </Button>
      </Box>

      {/* 添加在最外层 </Box> 标签之前 */}
      <Dialog open={newPresetDialog} onClose={() => setNewPresetDialog(false)}>
        <DialogTitle>Save Current Parameters as Preset</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Preset Name"
            fullWidth
            value={newPresetName}
            onChange={(e) => setNewPresetName(e.target.value)}
          />
          <TextField
            margin="dense"
            label="Description (Optional)"
            fullWidth
            value={newPresetDesc}
            onChange={(e) => setNewPresetDesc(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setNewPresetDialog(false)}>Cancel</Button>
          <Button onClick={saveCurrentAsPreset} disabled={!newPresetName.trim()}>Save</Button>
        </DialogActions>
      </Dialog>

      {/* 添加教室设备状态管理对话框 */}
      <Dialog 
        open={classroomEditDialog} 
        onClose={() => setClassroomEditDialog(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>
          Edit Classroom
          {editingClassroom && (
            <Typography variant="subtitle2" color="text.secondary">
              Building: {selectedClassroom?.building} | Campus: {mockCampuses.find(c => c.id === selectedClassroom?.campusId)?.name || 'Unknown'}
            </Typography>
          )}
        </DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12} md={6}>
              <TextField
                label="Classroom Name"
                fullWidth
                value={editingClassroom.name}
                onChange={(e) => setEditingClassroom({...editingClassroom, name: e.target.value})}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Capacity"
                type="number"
                fullWidth
                value={editingClassroom.capacity}
                onChange={(e) => setEditingClassroom({...editingClassroom, capacity: parseInt(e.target.value, 10)})}
                InputProps={{ inputProps: { min: 0 } }}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControl fullWidth>
                <InputLabel>Classroom Type</InputLabel>
                <Select
                  value={editingClassroom.type}
                  label="Classroom Type"
                  onChange={(e) => setEditingClassroom({...editingClassroom, type: e.target.value})}
                >
                  <MenuItem value="ComputerLab">Computer Lab</MenuItem>
                  <MenuItem value="Lecture">Lecture Room</MenuItem>
                  <MenuItem value="LargeHall">Large Hall</MenuItem>
                  <MenuItem value="Laboratory">Laboratory</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12}>
              <FormControl fullWidth>
                <InputLabel>Features</InputLabel>
                <Select
                  multiple
                  value={editingClassroom.features || []}
                  label="Features"
                  onChange={(e) => setEditingClassroom({...editingClassroom, features: e.target.value})}
                  renderValue={(selected) => (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                      {selected.map((value) => (
                        <Chip key={value} label={value} size="small" />
                      ))}
            </Box>
                  )}
                >
                  <MenuItem value="Projector">Projector</MenuItem>
                  <MenuItem value="Computers">Computers</MenuItem>
                  <MenuItem value="Interactive Whiteboard">Interactive Whiteboard</MenuItem>
                  <MenuItem value="Advanced Audio">Advanced Audio</MenuItem>
                  <MenuItem value="Lab Equipment">Lab Equipment</MenuItem>
                  <MenuItem value="Safety Facilities">Safety Facilities</MenuItem>
                  <MenuItem value="Whiteboard">Whiteboard</MenuItem>
                  <MenuItem value="Audio System">Audio System</MenuItem>
                  <MenuItem value="Dual Projector">Dual Projector</MenuItem>
                </Select>
              </FormControl>
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setClassroomEditDialog(false)}>Cancel</Button>
          <Button onClick={handleClassroomUpdate} variant="contained" color="primary">Save Changes</Button>
        </DialogActions>
      </Dialog>

      {/* Resource Management Tab */}
      {tabValue === 2 && (
        <Box>
          <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
            <Tabs 
              value={resourceSubTab} 
              onChange={(e, newValue) => setResourceSubTab(newValue)}
            >
              <Tab label="Classrooms" />
              <Tab label="Equipment Types" />
              <Tab label="Classroom Equipment" />
              <Tab label="Utilization Reports" />
            </Tabs>
          </Box>
          
          {/* Classrooms */}
          {resourceSubTab === 0 && (
            <Box>
              <TableContainer component={Paper} variant="outlined" sx={{ mb: 3 }}>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>ID</TableCell>
                    <TableCell>Name</TableCell>
                    <TableCell>Building</TableCell>
                    <TableCell>Capacity</TableCell>
                      <TableCell>Features</TableCell>
                    <TableCell>Type</TableCell>
                      <TableCell>Campus</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {mockClassrooms.map((classroom) => (
                      <TableRow 
                        key={classroom.id} 
                        hover 
                        selected={selectedClassroom?.id === classroom.id}
                        onClick={() => setSelectedClassroom(classroom)}
                      >
                      <TableCell>{classroom.id}</TableCell>
                      <TableCell>{classroom.name}</TableCell>
                      <TableCell>{classroom.building}</TableCell>
                      <TableCell>{classroom.capacity}</TableCell>
                        <TableCell>
                          {classroom.features && classroom.features.map((feature, index) => (
                            <Chip 
                              key={index} 
                              label={feature} 
                              size="small" 
                              sx={{ mr: 0.5, mb: 0.5 }}
                            />
                          ))}
                        </TableCell>
                      <TableCell>{classroom.type}</TableCell>
                      <TableCell>
                          {mockCampuses.find(c => c.id === classroom.campusId)?.name || 'Unknown'}
                        </TableCell>
                        <TableCell>
                          <Tooltip title="Edit Classroom">
                            <IconButton size="small" onClick={(e) => {
                              e.stopPropagation();
                              handleClassroomEdit(classroom.id);
                            }}>
                              <EditIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
              
              <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end' }}>
                <Button 
                  variant="contained" 
                  color="primary"
                  startIcon={<AddIcon />}
                >
                  Add New Classroom
                </Button>
              </Box>
            </Box>
          )}
          
          {/* Teachers */}
          {resourceSubTab === 1 && (
            <Box>
              <Alert severity="info" sx={{ mb: 2 }}>
                Equipment types define the physical resources available in classrooms.
              </Alert>
              <TableContainer component={Paper} variant="outlined">
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>ID</TableCell>
                      <TableCell>Name</TableCell>
                      <TableCell>Description</TableCell>
                      <TableCell>Movable</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {mockEquipmentTypes.map((equipment) => (
                      <TableRow key={equipment.id}>
                        <TableCell>{equipment.id}</TableCell>
                        <TableCell>{equipment.name}</TableCell>
                        <TableCell>{equipment.description}</TableCell>
                      <TableCell>
                          <Chip 
                            size="small" 
                            label={equipment.movable ? "Yes" : "No"} 
                            color={equipment.movable ? "success" : "default"}
                          />
                        </TableCell>
                        <TableCell>
                          <IconButton size="small">
                            <EditIcon />
                          </IconButton>
                      </TableCell>
                    </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
              
              <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end' }}>
                <Button 
                  variant="contained" 
                  color="primary"
                  startIcon={<AddIcon />}
                >
                  Add New Equipment Type
                </Button>
              </Box>
            </Box>
          )}
          
          {/* Classroom Equipment */}
          {resourceSubTab === 2 && (
            <Box>
              <Alert severity="info" sx={{ mb: 2 }}>
                View and manage fixed equipment and facilities for each classroom.
              </Alert>
              
              {/* Simplified filter panel - only keep classroom selection */}
              <Paper variant="outlined" sx={{ mb: 3, p: 2 }}>
                <Grid container spacing={2} alignItems="center">
                  <Grid item xs={12} md={6}>
                    <FormControl fullWidth size="small">
                      <InputLabel>Select Classroom</InputLabel>
                      <Select 
                        value={selectedClassroom?.id || ''}
                        onChange={(e) => handleClassroomSelect(e.target.value)}
                        label="Select Classroom"
                      >
                        <MenuItem value="">All Classrooms</MenuItem>
                        {mockClassrooms.map(classroom => (
                          <MenuItem key={classroom.id} value={classroom.id}>
                            {classroom.building + '-' + classroom.name} ({classroom.type})
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </Grid>
                  <Grid item xs={12} md={6} sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                    {selectedClassroom && (
                      <Button 
                        variant="outlined" 
                        startIcon={<AddIcon />}
                      >
                        Add New Equipment
                      </Button>
                    )}
                  </Grid>
                </Grid>
              </Paper>
              
              {/* Equipment inventory and standard configs tabs */}
              <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
                <Tabs 
                  value={equipmentSubTab} 
                  onChange={(e, newValue) => setEquipmentSubTab(newValue)}
                >
                  <Tab label="Equipment Inventory" />
                  <Tab label="Standard Configurations" />
                </Tabs>
              </Box>
              
              {/* Equipment management interface */}
              {equipmentSubTab === 0 && (
                <Grid container spacing={2}>
                  <Grid item xs={12} md={7}>
                    {/* Classroom equipment inventory */}
                    <Typography variant="subtitle1" gutterBottom>Classroom Equipment Inventory</Typography>
                    <TableContainer component={Paper} variant="outlined" sx={{ mb: 3 }}>
                      <Table>
                        <TableHead>
                    <TableRow>
                            <TableCell>Classroom</TableCell>
                            <TableCell>Equipment Name</TableCell>
                            <TableCell>Quantity</TableCell>
                            <TableCell>Status</TableCell>
                            <TableCell>Actions</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {selectedClassroom ? (
                            classroomEquipmentList.map((item) => {
                              const classroom = mockClassrooms.find(c => c.id === item.classroomId);
                              const equipment = mockEquipmentTypes.find(e => e.id === item.equipmentTypeId);
                              if (!classroom || !equipment) return null;
                              
                              return (
                                <TableRow 
                                  key={item.id}
                                  selected={selectedEquipment && selectedEquipment.id === item.id}
                                  onClick={() => handleEquipmentSelect(item.id)}
                                >
                                  <TableCell>{`${classroom.building}-${classroom.name}`}</TableCell>
                                  <TableCell>{equipment.name}</TableCell>
                                  <TableCell>{item.quantity}</TableCell>
                                  <TableCell>
                                    <Chip 
                                      size="small" 
                                      label={item.status} 
                                      color={item.status === 'Good' ? 'success' : item.status === 'Partially Damaged' ? 'warning' : 'error'} 
                                    />
                                  </TableCell>
                      <TableCell>
                        <Tooltip title="Edit">
                                      <IconButton size="small" onClick={() => handleEquipmentSelect(item.id)}>
                            <EditIcon />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                              );
                            })
                          ) : (
                            <TableRow>
                              <TableCell colSpan={5} align="center">Please select a classroom first</TableCell>
                            </TableRow>
                          )}
                  </TableBody>
                </Table>
              </TableContainer>
                  </Grid>
                  
                  <Grid item xs={12} md={5}>
                    <Card variant="outlined">
                      <CardContent>
                        <Typography variant="subtitle2" gutterBottom>
                          {selectedEquipment ? 'Edit Equipment' : 'Equipment Details'}
                        </Typography>
                        
                        {selectedEquipment ? (
                          <Box>
                            <TextField
                              label="Quantity"
                              type="number"
                              fullWidth
                              margin="normal"
                              value={editingEquipment.quantity}
                              onChange={(e) => setEditingEquipment(prev => ({
                                ...prev,
                                quantity: parseInt(e.target.value, 10)
                              }))}
                              InputProps={{ inputProps: { min: 0 } }}
                            />
                            
                            <FormControl fullWidth margin="normal">
                              <InputLabel>Status</InputLabel>
                              <Select
                                value={editingEquipment.status}
                                label="Status"
                                onChange={(e) => setEditingEquipment(prev => ({
                                  ...prev,
                                  status: e.target.value
                                }))}
                              >
                                <MenuItem value="Good">Good</MenuItem>
                                <MenuItem value="Partially Damaged">Partially Damaged</MenuItem>
                                <MenuItem value="Needs Repair">Needs Repair</MenuItem>
                              </Select>
                            </FormControl>
                            
                            <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
                              <Button 
                                variant="contained" 
                                onClick={handleEquipmentUpdate}
                                disabled={!selectedEquipment}
                              >
                                Update
                              </Button>
                              <Button 
                                variant="outlined" 
                                onClick={() => setSelectedEquipment(null)}
                              >
                                Cancel
                              </Button>
                            </Box>
                          </Box>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            {selectedClassroom ? 'Select an equipment from the list to edit its details.' : 'Please select a classroom first to view equipment.'}
                          </Typography>
                        )}
                      </CardContent>
                    </Card>
                  </Grid>
                </Grid>
              )}
              
              {/* Standard Configurations Tab */}
              {equipmentSubTab === 1 && (
                <Box>
                  <Typography variant="subtitle1" gutterBottom>Equipment Standard Configurations</Typography>
                  <Grid container spacing={3}>
                    <Grid item xs={12} md={6}>
                      <Card variant="outlined">
                        <CardContent>
                          <Typography variant="h6" gutterBottom>Room Type Equipment Standards</Typography>
                          <TableContainer component={Paper} variant="outlined">
                            <Table size="small">
                              <TableHead>
                                <TableRow>
                                  <TableCell>Room Type</TableCell>
                                  <TableCell>Standard Equipment</TableCell>
                                </TableRow>
                              </TableHead>
                              <TableBody>
                                {mockRoomTypeEquipment.map((item, index) => (
                                  <TableRow key={index}>
                                    <TableCell>{item.roomType}</TableCell>
                                    <TableCell>
                                      {item.equipmentTypeIds.map(id => {
                                        const equipment = mockEquipmentTypes.find(e => e.id === id);
                                        return equipment ? (
                                          <Chip 
                                            key={id}
                                            label={equipment.name}
                                            size="small"
                                            sx={{ mr: 0.5, mb: 0.5 }}
                                          />
                                        ) : null;
                                      })}
                                    </TableCell>
                                  </TableRow>
                                ))}
                              </TableBody>
                            </Table>
                          </TableContainer>
                          <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                            <Button 
                              variant="outlined" 
                              startIcon={<EditIcon />} 
                              size="small"
                            >
                              Edit Standards
                            </Button>
                          </Box>
                        </CardContent>
                      </Card>
                    </Grid>
                    
                    <Grid item xs={12} md={6}>
                      <Card variant="outlined">
                        <CardContent>
                          <Typography variant="h6" gutterBottom>Equipment Maintenance Schedule</Typography>
                          <TableContainer component={Paper} variant="outlined">
                            <Table size="small">
                              <TableHead>
                                <TableRow>
                                  <TableCell>Equipment Type</TableCell>
                                  <TableCell>Maintenance Frequency</TableCell>
                                  <TableCell>Last Updated</TableCell>
                                </TableRow>
                              </TableHead>
                              <TableBody>
                                {mockEquipmentTypes.slice(0, 5).map((item) => (
                                  <TableRow key={item.id}>
                                    <TableCell>{item.name}</TableCell>
                                    <TableCell>
                                      {item.id % 3 === 0 ? 'Monthly' : item.id % 3 === 1 ? 'Quarterly' : 'Annually'}
                                    </TableCell>
                                    <TableCell>
                                      {new Date(Date.now() - (Math.random() * 10000000000)).toLocaleDateString()}
                                    </TableCell>
                                  </TableRow>
                                ))}
                              </TableBody>
                            </Table>
                          </TableContainer>
                          <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                            <Button 
                              variant="outlined" 
                              startIcon={<ScheduleIcon />} 
                              size="small"
                            >
                              Manage Schedule
                            </Button>
                          </Box>
                        </CardContent>
                      </Card>
                    </Grid>
                    
                    <Grid item xs={12}>
                      <Card variant="outlined">
                        <CardContent>
                          <Typography variant="h6" gutterBottom>Equipment Supply Chain</Typography>
                          <Alert severity="info" sx={{ mb: 2 }}>
                            Configure equipment vendors, reordering thresholds, and maintenance service providers.
                          </Alert>
                          <TableContainer component={Paper} variant="outlined">
                            <Table size="small">
                              <TableHead>
                                <TableRow>
                                  <TableCell>Equipment Category</TableCell>
                                  <TableCell>Preferred Vendor</TableCell>
                                  <TableCell>Lead Time</TableCell>
                                  <TableCell>Reorder Level</TableCell>
                                </TableRow>
                              </TableHead>
                              <TableBody>
                                <TableRow>
                                  <TableCell>Computer Equipment</TableCell>
                                  <TableCell>TechSupply Inc.</TableCell>
                                  <TableCell>2-3 weeks</TableCell>
                                  <TableCell>5 units</TableCell>
                                </TableRow>
                                <TableRow>
                                  <TableCell>Audio Visual</TableCell>
                                  <TableCell>AV Solutions Ltd.</TableCell>
                                  <TableCell>1-2 weeks</TableCell>
                                  <TableCell>3 units</TableCell>
                                </TableRow>
                                <TableRow>
                                  <TableCell>Lab Equipment</TableCell>
                                  <TableCell>ScienceTools Co.</TableCell>
                                  <TableCell>4-6 weeks</TableCell>
                                  <TableCell>2 units</TableCell>
                                </TableRow>
                              </TableBody>
                            </Table>
                          </TableContainer>
                        </CardContent>
                      </Card>
                    </Grid>
                  </Grid>
                </Box>
              )}
            </Box>
          )}
          
          {/* Utilization Reports */}
          {resourceSubTab === 3 && (
            <Box>
              <Alert severity="info" sx={{ mb: 2 }}>
                Utilization reports help you analyze resource usage and identify opportunities for optimization.
              </Alert>
              
              {/* 优化统计图表 */}
              <Grid container spacing={2} sx={{ mb: 3 }}>
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>Classroom Distribution by Type</Typography>
                      <Box sx={{ height: 250, bgcolor: '#f5f5f5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <Typography variant="body2" color="text.secondary">
                          Classroom Types Chart (placeholder)
                        </Typography>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>Equipment Status Overview</Typography>
                      <Box sx={{ height: 250, bgcolor: '#f5f5f5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <Typography variant="body2" color="text.secondary">
                          Equipment Status Chart (placeholder)
                        </Typography>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              </Grid>
              
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>Classroom Utilization</Typography>
                      <Box sx={{ height: 300, bgcolor: '#f5f5f5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <Typography variant="body2" color="text.secondary">
                          Classroom Utilization Chart (placeholder)
                        </Typography>
                      </Box>
                      <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                        <Button variant="outlined" size="small">Generate Report</Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>Equipment Usage</Typography>
                      <Box sx={{ height: 300, bgcolor: '#f5f5f5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <Typography variant="body2" color="text.secondary">
                          Equipment Usage Chart (placeholder)
                        </Typography>
                      </Box>
                      <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                        <Button variant="outlined" size="small">Generate Report</Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
                
                <Grid item xs={12}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>Maintenance Schedule</Typography>
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>Equipment</TableCell>
                              <TableCell>Location</TableCell>
                              <TableCell>Last Maintained</TableCell>
                              <TableCell>Next Maintenance</TableCell>
                              <TableCell>Status</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            <TableRow>
                              <TableCell>Projectors</TableCell>
                              <TableCell>Building A</TableCell>
                              <TableCell>2024-10-15</TableCell>
                              <TableCell>2025-04-15</TableCell>
                              <TableCell><Chip size="small" label="Good" color="success" /></TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell>Computers</TableCell>
                              <TableCell>Computer Lab 101</TableCell>
                              <TableCell>2024-11-20</TableCell>
                              <TableCell>2025-02-20</TableCell>
                              <TableCell><Chip size="small" label="Scheduled" color="info" /></TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell>Audio System</TableCell>
                              <TableCell>Large Hall 301</TableCell>
                              <TableCell>2024-08-05</TableCell>
                              <TableCell>2025-01-05</TableCell>
                              <TableCell><Chip size="small" label="Overdue" color="error" /></TableCell>
                            </TableRow>
                          </TableBody>
                        </Table>
                      </TableContainer>
                      <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                        <Button variant="outlined" size="small">Schedule Maintenance</Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              </Grid>
            </Box>
          )}
        </Box>
      )}
      
      {/* Time Management Tab */}
      {tabValue === 3 && (
        <Box>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="subtitle1">Time Management</Typography>
              <Button 
              variant="contained" 
              color="primary" 
              startIcon={<EventIcon />}
            >
              Add New Time Slot
              </Button>
          </Box>
          
          <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
            <Tabs 
              value={timeSubTab} 
              onChange={(e, newValue) => setTimeSubTab(newValue)}
            >
              <Tab label="Academic Calendar" />
              <Tab label="Class Time Slots" />
              <Tab label="Exam Periods" />
              <Tab label="Holidays & Breaks" />
            </Tabs>
          </Box>
          
          {/* Academic Calendar Tab */}
          {timeSubTab === 0 && (
            <Box>
              <Card variant="outlined" sx={{ mb: 3 }}>
                <CardContent>
                  <Typography variant="subtitle1" gutterBottom>Academic Year Configuration</Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={12} md={6}>
            <TableContainer component={Paper} variant="outlined">
                        <Table size="small">
                <TableHead>
                  <TableRow>
                              <TableCell>Semester</TableCell>
                    <TableCell>Start Date</TableCell>
                    <TableCell>End Date</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                            {mockSemesters.map((semester) => (
                              <TableRow key={semester.id} hover>
                                <TableCell>{semester.name}</TableCell>
                                <TableCell>{new Date(semester.startDate).toLocaleDateString()}</TableCell>
                                <TableCell>{new Date(semester.endDate).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <Chip 
                        size="small" 
                                    label={semester.isActive ? "Active" : "Inactive"} 
                                    color={semester.isActive ? "success" : "default"} 
                      />
                    </TableCell>
                    <TableCell>
                      <Tooltip title="Edit">
                        <IconButton size="small">
                                      <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                            ))}
                </TableBody>
              </Table>
            </TableContainer>
                    </Grid>
                    <Grid item xs={12} md={6}>
                      <Card>
                        <CardContent>
                          <Typography variant="subtitle2" gutterBottom>Academic Year Timeline</Typography>
                          <Box sx={{ height: 200, bgcolor: '#f5f5f5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                            <Typography variant="body2" color="text.secondary">
                              Academic Calendar Timeline (placeholder)
                            </Typography>
                          </Box>
                        </CardContent>
                      </Card>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
              
              <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 2 }}>
                <Button 
                  variant="outlined" 
                  startIcon={<FileDownloadIcon />}
                  sx={{ mr: 1 }}
                >
                  Export Calendar
                </Button>
                <Button 
                  variant="contained"
                  startIcon={<SaveIcon />}
                >
                  Save Changes
                </Button>
              </Box>
            </Box>
          )}
          
          {/* Class Time Slots Tab */}
          {timeSubTab === 1 && (
            <Box>
              <Card variant="outlined" sx={{ mb: 3 }}>
                <CardContent>
                  <Typography variant="subtitle1" gutterBottom>Class Time Slot Configuration</Typography>
                  
            <TableContainer component={Paper} variant="outlined">
                    <Table size="small">
                <TableHead>
                  <TableRow>
                          <TableCell>Slot ID</TableCell>
                          <TableCell>Start Time</TableCell>
                          <TableCell>End Time</TableCell>
                          <TableCell>Days</TableCell>
                          <TableCell>Campus</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                        <TableRow hover>
                          <TableCell>TS001</TableCell>
                          <TableCell>08:00 AM</TableCell>
                          <TableCell>09:30 AM</TableCell>
                          <TableCell>Mon, Wed</TableCell>
                          <TableCell>Main Campus</TableCell>
                    <TableCell>
                      <Tooltip title="Edit">
                        <IconButton size="small">
                                <EditIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Delete">
                              <IconButton size="small">
                                <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                        <TableRow hover>
                          <TableCell>TS002</TableCell>
                          <TableCell>10:00 AM</TableCell>
                          <TableCell>11:30 AM</TableCell>
                          <TableCell>Mon, Wed, Fri</TableCell>
                          <TableCell>Main Campus</TableCell>
                    <TableCell>
                      <Tooltip title="Edit">
                        <IconButton size="small">
                                <EditIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Delete">
                              <IconButton size="small">
                                <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                        <TableRow hover>
                          <TableCell>TS003</TableCell>
                          <TableCell>01:00 PM</TableCell>
                          <TableCell>02:30 PM</TableCell>
                          <TableCell>Tue, Thu</TableCell>
                          <TableCell>All Campuses</TableCell>
                    <TableCell>
                      <Tooltip title="Edit">
                        <IconButton size="small">
                                <EditIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Delete">
                              <IconButton size="small">
                                <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </TableContainer>
                </CardContent>
              </Card>
              
              <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 2 }}>
                <Button 
                  variant="contained"
                  startIcon={<AddIcon />}
                >
                  Add Time Slot
                </Button>
              </Box>
            </Box>
          )}
          
          {/* Other time sub tabs */}
          {timeSubTab > 1 && (
            <Box sx={{ p: 3, textAlign: 'center' }}>
              <Typography variant="h6" color="text.secondary">
                {timeSubTab === 2 ? 'Exam Periods' : 'Holidays & Breaks'} Configuration
              </Typography>
              <Typography variant="body1" color="text.secondary" sx={{ mt: 2 }}>
                This section is under development.
              </Typography>
            </Box>
          )}
        </Box>
      )}
      
      {/* Developer Options Tab */}
      {tabValue === 4 && (
            <Box>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="subtitle1">Developer Options</Typography>
            <Button 
              variant="contained" 
              color="primary" 
              startIcon={<SettingsIcon />}
            >
              System Diagnostics
            </Button>
          </Box>
          
          <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                  <Typography variant="h6" gutterBottom>API Configuration</Typography>
                  <TableContainer component={Paper} variant="outlined" sx={{ mb: 2 }}>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                          <TableCell>Setting</TableCell>
                          <TableCell>Value</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            <TableRow>
                          <TableCell>API Base URL</TableCell>
                              <TableCell>
                            <TextField 
                              size="small" 
                              fullWidth 
                              defaultValue="https://api.smartscheduling.edu/v1"
                            />
                              </TableCell>
                            </TableRow>
                            <TableRow>
                          <TableCell>API Timeout (ms)</TableCell>
                              <TableCell>
                            <TextField 
                              size="small" 
                              type="number" 
                              defaultValue="30000"
                            />
                              </TableCell>
                            </TableRow>
                            <TableRow>
                          <TableCell>Enable Caching</TableCell>
                              <TableCell>
                            <FormControlLabel 
                              control={<Switch defaultChecked />} 
                              label="Enabled" 
                            />
                              </TableCell>
                            </TableRow>
                          </TableBody>
                        </Table>
                      </TableContainer>
                  
                  <Typography variant="h6" gutterBottom>Algorithm Settings</Typography>
                  <TableContainer component={Paper} variant="outlined">
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                          <TableCell>Setting</TableCell>
                          <TableCell>Value</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            <TableRow>
                          <TableCell>Max Iterations</TableCell>
                              <TableCell>
                            <TextField 
                              size="small" 
                              type="number" 
                              defaultValue="1000"
                            />
                              </TableCell>
                            </TableRow>
                            <TableRow>
                          <TableCell>Convergence Threshold</TableCell>
                              <TableCell>
                            <TextField 
                              size="small" 
                              type="number" 
                              defaultValue="0.001"
                            />
                              </TableCell>
                            </TableRow>
                            <TableRow>
                          <TableCell>Enable Advanced Heuristics</TableCell>
                              <TableCell>
                            <FormControlLabel 
                              control={<Switch defaultChecked />} 
                              label="Enabled" 
                            />
                              </TableCell>
                            </TableRow>
                          </TableBody>
                        </Table>
                      </TableContainer>
                    </CardContent>
                  </Card>
              </Grid>
              
            <Grid item xs={12} md={6}>
              <Card variant="outlined" sx={{ mb: 3 }}>
                <CardContent>
                  <Typography variant="h6" gutterBottom>System Logs</Typography>
                  <Box sx={{ bgcolor: '#000', color: '#0f0', p: 2, borderRadius: 1, height: 200, overflow: 'auto', fontFamily: 'monospace', fontSize: '0.8rem' }}>
                    [INFO] 2023-10-15 08:00:15 - System initialized<br/>
                    [INFO] 2023-10-15 08:01:23 - User authentication successful<br/>
                    [INFO] 2023-10-15 08:05:45 - Schedule optimization started<br/>
                    [WARNING] 2023-10-15 08:06:12 - Performance bottleneck detected<br/>
                    [INFO] 2023-10-15 08:10:33 - Schedule optimization completed<br/>
                    [INFO] 2023-10-15 08:15:21 - Data synchronization started<br/>
                    [ERROR] 2023-10-15 08:16:05 - Failed to connect to remote database<br/>
                    [INFO] 2023-10-15 08:20:45 - System recovery successful<br/>
              </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 1 }}>
                    <Button size="small" variant="outlined">Clear Logs</Button>
                    <Button size="small" variant="outlined" sx={{ ml: 1 }}>Download Logs</Button>
            </Box>
                </CardContent>
              </Card>
              
              <Card variant="outlined">
            <CardContent>
                  <Typography variant="h6" gutterBottom>System Performance</Typography>
                  <Box sx={{ height: 200, bgcolor: '#f5f5f5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Performance Metrics Chart (placeholder)
              </Typography>
                  </Box>
                  <Alert severity="info" sx={{ mt: 2 }}>
                    System is currently operating at optimal performance levels.
                  </Alert>
                </CardContent>
              </Card>
                </Grid>
                
                <Grid item xs={12}>
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="h6" gutterBottom>Data Management</Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={12} md={4}>
                      <Card variant="outlined">
                        <CardContent>
                          <Typography variant="subtitle2">Database Backup</Typography>
                          <Box sx={{ mt: 2, display: 'flex', flexDirection: 'column', gap: 1 }}>
                            <Button variant="outlined" startIcon={<FileDownloadIcon />}>
                              Backup Database
                            </Button>
                            <Button variant="outlined" startIcon={<FileUploadIcon />}>
                              Restore from Backup
                            </Button>
              </Box>
            </CardContent>
          </Card>
                    </Grid>
                    <Grid item xs={12} md={4}>
          <Card variant="outlined">
            <CardContent>
                          <Typography variant="subtitle2">Data Import/Export</Typography>
                          <Box sx={{ mt: 2, display: 'flex', flexDirection: 'column', gap: 1 }}>
                            <Button variant="outlined" startIcon={<FileUploadIcon />}>
                              Import Data
                </Button>
                            <Button variant="outlined" startIcon={<FileDownloadIcon />}>
                              Export Data
                </Button>
                          </Box>
                        </CardContent>
                      </Card>
                    </Grid>
                    <Grid item xs={12} md={4}>
                      <Card variant="outlined">
                        <CardContent>
                          <Typography variant="subtitle2">System Maintenance</Typography>
                          <Box sx={{ mt: 2, display: 'flex', flexDirection: 'column', gap: 1 }}>
                            <Button variant="outlined" color="warning">
                              Clear Cache
                            </Button>
                            <Button variant="outlined" color="error">
                              Reset System
                </Button>
              </Box>
                        </CardContent>
                      </Card>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
            </Grid>
          </Grid>
        </Box>
      )}
      
      {/* Templates & Presets Tab */}
      {tabValue === 5 && (
        <Box>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="subtitle1">Templates & Presets</Typography>
            <Box sx={{ display: 'flex', gap: 1 }}>
            <Button 
                variant="outlined" 
              startIcon={<AddIcon />}
                onClick={() => setNewPresetDialog(true)}
              >
                Create New Template
              </Button>
              <Button 
                variant="outlined" 
                startIcon={<FileUploadIcon />}
              >
                Import Templates
            </Button>
            </Box>
          </Box>
          
          <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
            <Tabs 
              value={templateSubTab} 
              onChange={(e, newValue) => setTemplateSubTab(newValue)}
            >
              <Tab label="Schedule Templates" />
              <Tab label="Report Templates" />
              <Tab label="Parameter Presets" />
            </Tabs>
          </Box>
          
          {/* Schedule Templates Tab */}
          {templateSubTab === 0 && (
            <Grid container spacing={3}>
              {[1, 2, 3, 4].map((item) => (
                <Grid item xs={12} md={4} key={item}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="h6" gutterBottom>{`Template ${item}`}</Typography>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {item === 1 
                          ? "Standard Semester Schedule" 
                          : item === 2 
                            ? "Engineering Department Layout" 
                            : item === 3 
                              ? "Summer Session Plan" 
                              : "Final Exam Week"}
                      </Typography>
                      <Box sx={{ height: 120, bgcolor: '#f5f5f5', display: 'flex', alignItems: 'center', justifyContent: 'center', mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          Schedule Preview
                      </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Button size="small" variant="outlined" startIcon={<EditIcon />}>
                          Edit
                        </Button>
                        <Button size="small" variant="contained">
                          Apply
                        </Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
              
              <Grid item xs={12} md={4}>
                <Card variant="outlined" sx={{ height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: '#f9f9f9' }}>
                  <CardContent sx={{ textAlign: 'center' }}>
                    <AddIcon sx={{ fontSize: 40, color: 'text.secondary', mb: 1 }} />
                    <Typography variant="body1">Create New Template</Typography>
                          <Button 
                            variant="outlined"
                      sx={{ mt: 2 }}
                      startIcon={<AddIcon />}
                          >
                      Add Template
                          </Button>
                    </CardContent>
                  </Card>
                </Grid>
                </Grid>
          )}
          
          {/* Other template sub tabs */}
          {templateSubTab > 0 && (
            <Box sx={{ p: 3, textAlign: 'center' }}>
              <Typography variant="h6" color="text.secondary">
                {templateSubTab === 1 ? 'Report Templates' : 'Parameter Presets'} 
                      </Typography>
              <Typography variant="body1" color="text.secondary" sx={{ mt: 2 }}>
                This section is under development.
                      </Typography>
            </Box>
          )}
        </Box>
      )}
    </Box>
  );
};

export default DataManagement;