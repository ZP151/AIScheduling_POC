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
  Alert
} from '@mui/material';
import { 
  mockClassrooms, 
  mockConstraints,
  mockTeachers,
  mockCourses,
  mockSemesters
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

// Import constraint components
import TeacherAvailabilitySettings from './constraints/TeacherAvailabilitySettings';
import ClassroomAssignmentSettings from './constraints/ClassroomAssignmentSettings';

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
  
  // Parameter management state
  const [localParameters, setLocalParameters] = useState({...parameters});
  // 在现有状态变量后添加
  const [newPresetDialog, setNewPresetDialog] = useState(false);
  const [newPresetName, setNewPresetName] = useState('');
  const [newPresetDesc, setNewPresetDesc] = useState('');
  
  const [parametersModified, setParametersModified] = useState(false);

  // Update tab values when props change
  useEffect(() => {
    setTabValue(initialTab);
  }, [initialTab]);
  
  useEffect(() => {
    setConstraintSubTab(initialConstraintSubTab);
  }, [initialConstraintSubTab]);
  
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
      // 检查参数是否与原始参数不同
      setParametersModified(JSON.stringify(newParams) !== JSON.stringify(parameters));
      return newParams;
    });
  };
  // 添加在其他处理函数后
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
              onChange={(e, newValue) => setConstraintSubTab(newValue)}
            >
              <Tab label="General Constraints" />
              <Tab label="Teacher Availability" />
              <Tab label="Classroom Assignment" />
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
            <ClassroomAssignmentSettings 
              courses={mockCourses}
              classrooms={mockClassrooms}
              weight={0.7}
              onUpdate={() => {}}
            />
          )}
        </Box>
      )}
      
      {/* Resource Management Tab */}
      {tabValue === 2 && (
        <Box>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="subtitle1">Resource Management</Typography>
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button 
                variant="contained" 
                color="primary" 
                startIcon={<AddIcon />}
              >
                Add Classroom
              </Button>
              <Button 
                variant="outlined" 
                startIcon={<AddIcon />}
              >
                Add Equipment
              </Button>
            </Box>
          </Box>
          
          {/* Add sub-tabs */}
          <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
            <Tabs 
              value={resourceSubTab} 
              onChange={(e, newValue) => setResourceSubTab(newValue)}
            >
              <Tab label="Classrooms" />
              <Tab label="Equipment" />
              <Tab label="Utilization Reports" />
            </Tabs>
          </Box>
          
          {/* Classrooms */}
          {resourceSubTab === 0 && (
            <TableContainer component={Paper} variant="outlined">
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>ID</TableCell>
                    <TableCell>Name</TableCell>
                    <TableCell>Building</TableCell>
                    <TableCell>Capacity</TableCell>
                    <TableCell>Has Computers</TableCell>
                    <TableCell>Type</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {mockClassrooms.map((classroom) => (
                    <TableRow key={classroom.id}>
                      <TableCell>{classroom.id}</TableCell>
                      <TableCell>{classroom.name}</TableCell>
                      <TableCell>{classroom.building}</TableCell>
                      <TableCell>{classroom.capacity}</TableCell>
                      <TableCell>{classroom.hasComputers ? 'Yes' : 'No'}</TableCell>
                      <TableCell>{classroom.type}</TableCell>
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
          )}
          
          {/* Equipment */}
          {resourceSubTab === 1 && (
            <Box>
              <Alert severity="info" sx={{ mb: 2 }}>
                Equipment management allows you to track and assign specific equipment needed for courses.
              </Alert>
              
              <TableContainer component={Paper} variant="outlined">
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>ID</TableCell>
                      <TableCell>Name</TableCell>
                      <TableCell>Type</TableCell>
                      <TableCell>Location</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    <TableRow>
                      <TableCell>1</TableCell>
                      <TableCell>Projector HD-1080</TableCell>
                      <TableCell>Projector</TableCell>
                      <TableCell>Building A-101</TableCell>
                      <TableCell>Available</TableCell>
                      <TableCell>
                        <Tooltip title="Edit">
                          <IconButton size="small">
                            <EditIcon />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>2</TableCell>
                      <TableCell>Lab Equipment Set</TableCell>
                      <TableCell>Lab Kit</TableCell>
                      <TableCell>Building B-301</TableCell>
                      <TableCell>In Use</TableCell>
                      <TableCell>
                        <Tooltip title="Edit">
                          <IconButton size="small">
                            <EditIcon />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            </Box>
          )}
          
          {/* Utilization Reports */}
          {resourceSubTab === 2 && (
            <Box>
              <Alert severity="info" sx={{ mb: 2 }}>
                Utilization reports help you analyze resource usage and identify opportunities for optimization.
              </Alert>
              
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
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button 
                variant="outlined" 
                startIcon={<AddIcon />}
              >
                Add Academic Calendar
              </Button>
              <Button 
                variant="outlined" 
                startIcon={<AddIcon />}
              >
                Add Special Period
              </Button>
            </Box>
          </Box>
          
          {/* Add sub-tabs */}
          <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
            <Tabs 
              value={timeSubTab} 
              onChange={(e, newValue) => setTimeSubTab(newValue)}
            >
              <Tab label="Academic Calendars" />
              <Tab label="Special Periods" />
              <Tab label="Standard Time Slots" />
            </Tabs>
          </Box>
          
          {/* Academic Calendars */}
          {timeSubTab === 0 && (
            <TableContainer component={Paper} variant="outlined">
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Name</TableCell>
                    <TableCell>Start Date</TableCell>
                    <TableCell>End Date</TableCell>
                    <TableCell>Total Weeks</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  <TableRow>
                    <TableCell>Spring 2025</TableCell>
                    <TableCell>January 15, 2025</TableCell>
                    <TableCell>May 10, 2025</TableCell>
                    <TableCell>16</TableCell>
                    <TableCell>
                      <Chip 
                        label="Active" 
                        color="primary" 
                        size="small" 
                      />
                    </TableCell>
                    <TableCell>
                      <Tooltip title="Edit">
                        <IconButton size="small">
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell>Summer 2025</TableCell>
                    <TableCell>June 5, 2025</TableCell>
                    <TableCell>August 15, 2025</TableCell>
                    <TableCell>10</TableCell>
                    <TableCell>
                      <Chip 
                        label="Upcoming" 
                        color="info" 
                        size="small" 
                      />
                    </TableCell>
                    <TableCell>
                      <Tooltip title="Edit">
                        <IconButton size="small">
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell>Fall 2025</TableCell>
                    <TableCell>September 1, 2025</TableCell>
                    <TableCell>December 20, 2025</TableCell>
                    <TableCell>16</TableCell>
                    <TableCell>
                      <Chip 
                        label="Upcoming" 
                        color="info" 
                        size="small" 
                      />
                    </TableCell>
                    <TableCell>
                      <Tooltip title="Edit">
                        <IconButton size="small">
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </TableContainer>
          )}
          
          {/* Special Periods */}
          {timeSubTab === 1 && (
            <TableContainer component={Paper} variant="outlined">
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Name</TableCell>
                    <TableCell>Type</TableCell>
                    <TableCell>Start Date</TableCell>
                    <TableCell>End Date</TableCell>
                    <TableCell>Affects Scheduling</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  <TableRow>
                    <TableCell>Spring Break 2025</TableCell>
                    <TableCell>Holiday</TableCell>
                    <TableCell>March 15, 2025</TableCell>
                    <TableCell>March 22, 2025</TableCell>
                    <TableCell>Yes</TableCell>
                    <TableCell>
                      <Tooltip title="Edit">
                        <IconButton size="small">
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell>Ramadan 2025</TableCell>
                    <TableCell>Special Schedule</TableCell>
                    <TableCell>March 1, 2025</TableCell>
                    <TableCell>March 30, 2025</TableCell>
                    <TableCell>Yes</TableCell>
                    <TableCell>
                      <Tooltip title="Edit">
                        <IconButton size="small">
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell>Final Exams</TableCell>
                    <TableCell>Exam Period</TableCell>
                    <TableCell>May 1, 2025</TableCell>
                    <TableCell>May 10, 2025</TableCell>
                    <TableCell>Yes</TableCell>
                    <TableCell>
                      <Tooltip title="Edit">
                        <IconButton size="small">
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </TableContainer>
          )}
          
          {/* Standard Time Slots */}
          {timeSubTab === 2 && (
            <Box>
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>
                        Regular Time Slots
                      </Typography>
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>ID</TableCell>
                              <TableCell>Start Time</TableCell>
                              <TableCell>End Time</TableCell>
                              <TableCell>Duration</TableCell>
                              <TableCell>Actions</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            <TableRow>
                              <TableCell>1</TableCell>
                              <TableCell>08:00</TableCell>
                              <TableCell>09:30</TableCell>
                              <TableCell>1h 30m</TableCell>
                              <TableCell>
                                <IconButton size="small">
                                  <EditIcon fontSize="small" />
                                </IconButton>
                              </TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell>2</TableCell>
                              <TableCell>10:00</TableCell>
                              <TableCell>11:30</TableCell>
                              <TableCell>1h 30m</TableCell>
                              <TableCell>
                                <IconButton size="small">
                                  <EditIcon fontSize="small" />
                                </IconButton>
                              </TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell>3</TableCell>
                              <TableCell>12:00</TableCell>
                              <TableCell>13:30</TableCell>
                              <TableCell>1h 30m</TableCell>
                              <TableCell>
                                <IconButton size="small">
                                  <EditIcon fontSize="small" />
                                </IconButton>
                              </TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell>4</TableCell>
                              <TableCell>14:00</TableCell>
                              <TableCell>15:30</TableCell>
                              <TableCell>1h 30m</TableCell>
                              <TableCell>
                                <IconButton size="small">
                                  <EditIcon fontSize="small" />
                                </IconButton>
                              </TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell>5</TableCell>
                              <TableCell>16:00</TableCell>
                              <TableCell>17:30</TableCell>
                              <TableCell>1h 30m</TableCell>
                              <TableCell>
                                <IconButton size="small">
                                  <EditIcon fontSize="small" />
                                </IconButton>
                              </TableCell>
                            </TableRow>
                          </TableBody>
                        </Table>
                      </TableContainer>
                    </CardContent>
                  </Card>
                </Grid>
                
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>
                        Ramadan Time Slots
                      </Typography>
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>ID</TableCell>
                              <TableCell>Start Time</TableCell>
                              <TableCell>End Time</TableCell>
                              <TableCell>Duration</TableCell>
                              <TableCell>Actions</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            <TableRow>
                              <TableCell>R1</TableCell>
                              <TableCell>09:00</TableCell>
                              <TableCell>10:15</TableCell>
                              <TableCell>1h 15m</TableCell>
                              <TableCell>
                                <IconButton size="small">
                                  <EditIcon fontSize="small" />
                                </IconButton>
                              </TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell>R2</TableCell>
                              <TableCell>10:30</TableCell>
                              <TableCell>11:45</TableCell>
                              <TableCell>1h 15m</TableCell>
                              <TableCell>
                                <IconButton size="small">
                                  <EditIcon fontSize="small" />
                                </IconButton>
                              </TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell>R3</TableCell>
                              <TableCell>12:00</TableCell>
                              <TableCell>13:15</TableCell>
                              <TableCell>1h 15m</TableCell>
                              <TableCell>
                                <IconButton size="small">
                                  <EditIcon fontSize="small" />
                                </IconButton>
                              </TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell>R4</TableCell>
                              <TableCell>13:30</TableCell>
                              <TableCell>14:45</TableCell>
                              <TableCell>1h 15m</TableCell>
                              <TableCell>
                                <IconButton size="small">
                                  <EditIcon fontSize="small" />
                                </IconButton>
                              </TableCell>
                            </TableRow>
                          </TableBody>
                        </Table>
                      </TableContainer>
                    </CardContent>
                  </Card>
                </Grid>
              </Grid>
              
              <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                <Button 
                  variant="outlined" 
                  startIcon={<AddIcon />}
                >
                  Add Time Slot Type
                </Button>
              </Box>
            </Box>
          )}
        </Box>
      )}
      
      {/* Templates & Presets Tab */}
      {tabValue === 4 && (
        <Box>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="subtitle1">Templates & Presets</Typography>
            <Button 
              variant="contained" 
              color="primary" 
              startIcon={<AddIcon />}
            >
              Save Current Schedule as Template
            </Button>
          </Box>
          
          {/* Add sub-tabs */}
          <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
            <Tabs 
              value={templateSubTab} 
              onChange={(e, newValue) => setTemplateSubTab(newValue)}
            >
              <Tab label="Schedule Templates" />
              <Tab label="Parameter Presets" />
              <Tab label="Constraint Presets" />
            </Tabs>
          </Box>
          
          {/* Schedule Templates */}
          {templateSubTab === 0 && (
            <Grid container spacing={2}>
              {[1, 2, 3, 4].map((template) => (
                <Grid item xs={12} md={6} key={template}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>
                        Template {template}: Standard {template === 1 ? 'Engineering' : template === 2 ? 'Business' : template === 3 ? 'Arts' : 'Medical'} Schedule
                      </Typography>
                      <Typography variant="body2" color="text.secondary" paragraph>
                        {template === 1 ? 'Optimized for engineering courses with lab components.' : 
                         template === 2 ? 'Business courses with emphasis on case study sessions.' : 
                         template === 3 ? 'Arts courses with flexible studio time.' : 
                         'Medical curriculum with clinical rotations.'}
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        <Button size="small" variant="outlined">Apply</Button>
                        <Button size="small" variant="outlined">Edit</Button>
                        <Button size="small" variant="outlined" color="error">Delete</Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          )}
          
          {/* Parameter Presets */}
          {templateSubTab === 1 && (
            <Box>
              <Alert severity="info" sx={{ mb: 2 }}>
                Parameter presets allow you to save and quickly apply different parameter configurations for different scheduling scenarios.
              </Alert>
              
              <TableContainer component={Paper} variant="outlined">
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Name</TableCell>
                      <TableCell>Description</TableCell>
                      <TableCell>Created Date</TableCell>
                      <TableCell>Last Used</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {parameterPresets.map((preset) => (
                      <TableRow key={preset.id}>
                        <TableCell>{preset.name}</TableCell>
                        <TableCell>{preset.description}</TableCell>
                        <TableCell>Jan 15, 2025</TableCell>
                        <TableCell>Mar 10, 2025</TableCell>
                        <TableCell>
                          <Button 
                            size="small" 
                            variant="outlined"
                            onClick={() => loadParameters(preset.id)}
                          >
                            Load
                          </Button>
                          <IconButton size="small">
                            <EditIcon />
                          </IconButton>
                          <IconButton size="small">
                            <DeleteIcon />
                          </IconButton>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Box>
          )}
          
          {/* Constraint Presets */}
          {templateSubTab === 2 && (
            <Box>
              <Alert severity="info" sx={{ mb: 2 }}>
                Constraint presets allow you to save and apply different sets of constraints for different scheduling scenarios.
              </Alert>
              
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>
                        Standard Scheduling Constraints
                      </Typography>
                      <Typography variant="body2" color="text.secondary" paragraph>
                        Basic set of constraints suitable for most general scheduling scenarios.
                      </Typography>
                      <Typography variant="body2">
                        <strong>Includes:</strong> Basic classroom capacity, teacher availability, and time preferences.
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
                        <Button size="small" variant="outlined">Apply</Button>
                        <Button size="small" variant="outlined">Edit</Button>
                        <Button size="small" variant="outlined" color="error">Delete</Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
                
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>
                        Strict Space Utilization Constraints
                      </Typography>
                      <Typography variant="body2" color="text.secondary" paragraph>
                        Optimized for maximizing classroom usage efficiency.
                      </Typography>
                      <Typography variant="body2">
                        <strong>Includes:</strong> Tight classroom matching, minimize empty periods, maximize building utilization.
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
                        <Button size="small" variant="outlined">Apply</Button>
                        <Button size="small" variant="outlined">Edit</Button>
                        <Button size="small" variant="outlined" color="error">Delete</Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
                
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>
                        Student-Focused Constraints
                      </Typography>
                      <Typography variant="body2" color="text.secondary" paragraph>
                        Prioritizes student convenience and learning experience.
                      </Typography>
                      <Typography variant="body2">
                        <strong>Includes:</strong> Minimize gaps in student schedules, balance course load across week, limit consecutive classes.
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
                        <Button size="small" variant="outlined">Apply</Button>
                        <Button size="small" variant="outlined">Edit</Button>
                        <Button size="small" variant="outlined" color="error">Delete</Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
                
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>
                        Faculty-Focused Constraints
                      </Typography>
                      <Typography variant="body2" color="text.secondary" paragraph>
                        Prioritizes faculty preferences and workload balance.
                      </Typography>
                      <Typography variant="body2">
                        <strong>Includes:</strong> Respect faculty time preferences, minimize teaching days, balance workload.
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
                        <Button size="small" variant="outlined">Apply</Button>
                        <Button size="small" variant="outlined">Edit</Button>
                        <Button size="small" variant="outlined" color="error">Delete</Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              </Grid>
            </Box>
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
    </Box>
  );
};

export default DataManagement;