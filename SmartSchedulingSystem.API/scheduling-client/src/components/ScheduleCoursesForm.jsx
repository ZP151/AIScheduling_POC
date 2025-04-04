// ScheduleCoursesForm.jsx
import React, { useState,useEffect } from 'react';
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
  Radio,
  RadioGroup,
  FormControlLabel,
  FormLabel,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Slider,
  Paper
} from '@mui/material';
import Alert from '@mui/material/Alert';

import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import PeopleIcon from '@mui/icons-material/People';
import SchoolIcon from '@mui/icons-material/School';
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import LocationOnIcon from '@mui/icons-material/LocationOn';
import EventIcon from '@mui/icons-material/Event';
import TuneIcon from '@mui/icons-material/Tune';
import { 
  mockSemesters, 
  mockCampuses,
  mockSchools,
  mockDepartments,
  mockTeacherSubjects,
  mockSubjects,
  mockProgrammes,
  mockCourses, 
  mockTeachers, 
  mockClassrooms, 
  mockConstraints,
  generateScheduleApi
} from '../services/mockData';

import RequirementAnalyzer  from './LLM/RequirementAnalyzer';
import ParameterOptimization from './LLM/ParameterOptimization';
import TeacherAvailabilitySettings from './constraints/TeacherAvailabilitySettings';
import ClassroomAssignmentSettings from './constraints/ClassroomAssignmentSettings';

const ScheduleCoursesForm = ({ 
  onScheduleGenerated, 
  systemParameters, 
  activePresetId, 
  presets,
  onPresetSelect,
  navigateToSystemConfig // Add this prop
 }) => {
  const [formData, setFormData] = useState({
    // List-selectable parameters
    semester: 1,
    courses: [1, 2, 3],
    teachers: [1, 2, 3, 4],
    classrooms: [1, 2, 3, 4],

     // Add these new organizational scope parameters
    campus: '',
    school: '',
    department: '',
    subject: '',  // 新增
    programme: '',
    // Add scheduling scope type
    schedulingScope: 'programme',  // Options: 'university', 'campus', 'school', 'department', 'programme', 'custom'
    // Add cross-entity parameters
    allowCrossSchoolEnrollment: true,
    allowCrossDepartmentTeaching: true,
    prioritizeHomeBuildings: true,
    
    // Toggle parameters
    useAI: true,
    genderSegregation: true,
    enableRamadanSchedule: false,
    allowCrossListedCourses: true,
    enableMultiCampusConstraints: true,
    generateAlternatives: true,
    holidayExclusions: true,

    // Adjustable parameters
    minimumTravelTime: 30,        // Minutes between classes in different buildings
    maximumConsecutiveClasses: 3, // Maximum consecutive teaching hours
    studentScheduleCompactness: 0.7, // Preference for compact student schedules (0-1)
    facultyWorkloadBalance: 0.8,  // Preference for balanced faculty workload (0-1)
    campusTravelTimeWeight: 0.6,  // Weight for campus travel time constraints
    preferredClassroomProximity: 0.5, // Preference for classrooms near department (0-1)
    
    // Use system parameters if available
    ...systemParameters,

    // Constraint settings
    constraintSettings: mockConstraints
      .filter(c => c.name !== "Consecutive Teaching") // Remove Consecutive Teaching
      .map(c => ({
        constraintId: c.id,
        isActive: c.isActive,
        weight: c.weight
      })),
      
    // Teacher Availability settings
    enableTeacherAvailability: true,
    teacherAvailabilitySettings: {},
    
    // Classroom Assignment settings
    enableClassroomTypeMatching: true,
    classroomTypeMatchingWeight: systemParameters?.classroomTypeMatchingWeight || 0.7,
    
  });
  
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

  // Add this to your component
  useEffect(() => {
    // Reset selections when scope changes
    if (formData.schedulingScope === 'university') {
      setFormData(prev => ({
        ...prev,
        campus: '',
        school: '',
        department: '',
        programme: ''
      }));
    } else if (formData.schedulingScope === 'campus' && formData.school) {
      setFormData(prev => ({
        ...prev,
        school: '',
        department: '',
        programme: ''
      }));
    } else if (formData.schedulingScope === 'school' && formData.department) {
      setFormData(prev => ({
        ...prev,
        department: '',
        programme: ''
      }));
    } else if (formData.schedulingScope === 'department' && formData.programme) {
      setFormData(prev => ({
        ...prev,
        programme: ''
      }));
    }
  }, [formData.schedulingScope]);

  // Add this helper function to your component
  const getFilteredItems = (itemType) => {
    switch (itemType) {
      case 'courses':
        // Filter courses based on selected organizational units
        let filteredCourses = [...mockCourses];
        if (formData.subject) {
          filteredCourses = filteredCourses.filter(course => course.subjectId === formData.subject);
        } else if (formData.programme) {
          filteredCourses = filteredCourses.filter(course => course.programmeId === formData.programme);
        } else if (formData.department) {
          filteredCourses = filteredCourses.filter(course => course.departmentId === formData.department);
        } else if (formData.school) {
          filteredCourses = filteredCourses.filter(course => 
            mockDepartments.some(dept => 
              dept.schoolId === formData.school && course.departmentId === dept.id
            )
          );
        } else if (formData.campus) {
          filteredCourses = filteredCourses.filter(course => 
            mockDepartments.some(dept => 
              mockSchools.some(school => 
                school.campusId === formData.campus && dept.schoolId === school.id && course.departmentId === dept.id
              )
            )
          );
        }
        return filteredCourses;
        
      case 'teachers':
        // Filter teachers based on selected organizational units
        let filteredTeachers = [...mockTeachers];
        if (formData.subject) {
          const eligibleTeacherIds = mockTeacherSubjects
            .filter(ts => ts.subjectId === formData.subject)
            .map(ts => ts.teacherId);
          filteredTeachers = filteredTeachers.filter(teacher => 
            eligibleTeacherIds.includes(teacher.id)
          );
        } else if (formData.department) {
          filteredTeachers = filteredTeachers.filter(teacher => teacher.departmentId === formData.department);
        } else if (formData.school) {
          filteredTeachers = filteredTeachers.filter(teacher => 
            mockDepartments.some(dept => 
              dept.schoolId === formData.school && teacher.departmentId === dept.id
            )
          );
        } else if (formData.campus) {
          filteredTeachers = filteredTeachers.filter(teacher => 
            mockDepartments.some(dept => 
              mockSchools.some(school => 
                school.campusId === formData.campus && dept.schoolId === school.id && teacher.departmentId === dept.id
              )
            )
          );
        }
        return filteredTeachers;
        
      case 'classrooms':
        // Filter classrooms based on selected campus
        let filteredClassrooms = [...mockClassrooms];
        if (formData.campus) {
          filteredClassrooms = filteredClassrooms.filter(classroom => classroom.campusId === formData.campus);
        }
        return filteredClassrooms;
        
      default:
        return [];
    }
  };

  const handleConstraintChange = (constraintId, field, value) => {
    setFormData(prev => ({
      ...prev,
      constraintSettings: prev.constraintSettings.map(cs => 
        cs.constraintId === constraintId ? { ...cs, [field]: value } : cs
      )
    }));
  };

  const handleTeacherAvailabilityUpdate = (settings) => {
    setFormData(prev => ({
      ...prev,
      teacherAvailabilitySettings: settings
    }));
  };

  const handleClassroomSettingsUpdate = (settings) => {
    setFormData(prev => ({
      ...prev,
      classroomTypeMatchingWeight: settings.weight,
      courseRoomPreferences: settings.courseRoomPreferences
    }));
  };

  // In ScheduleCoursesForm.jsx, update handleGenerateSchedule function
  const handleGenerateSchedule = () => {
    setIsGenerating(true);
    // Call API (mock)
    generateScheduleApi(formData)
      .then(result => {
        setIsGenerating(false);
        if (onScheduleGenerated) {
          // Pass all schedules to the parent
          onScheduleGenerated(result.primaryScheduleId, result.schedules);
        }
      })
      .catch(error => {
        console.error('Error generating schedule:', error);
        setIsGenerating(false);
      });
  };
  
  const handleAddConstraints = (constraints) => {
    // In a real app, we'd process these constraints and add them to the system
    alert(`Added ${constraints.length} constraints to the system`);
  };
  
  const handleOptimizationApplied = (changes) => {
    console.log('Optimization changes applied:', changes);
    // Apply parameter changes
    if (changes.parameterChanges) {
      // In a real application, this would update the actual parameters
      alert(`Applied ${changes.parameterChanges.length} parameter changes`);
    }
    
    // Add new parameters
    if (changes.newParameters) {
      // In a real application, this would add new parameters to the system
      alert(`Added ${changes.newParameters.length} new parameters`);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* LLM Constraint Analysis */}
      <Accordion defaultExpanded={true} sx={{ mb: 3 }}>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography variant="subtitle1" sx={{ display: 'flex', alignItems: 'center' }}>
            <TuneIcon sx={{ mr: 1 }} />
            Scheduling Requirements Analyzer
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <RequirementAnalyzer onAddConstraints={handleAddConstraints} />
        </AccordionDetails>
      </Accordion>

      <Typography variant="h6" gutterBottom>
        Create New Schedule
      </Typography>
      
      {/* Basic Settings - List-selectable parameters */}
      <Accordion defaultExpanded sx={{ mb: 2 }}>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography variant="subtitle1" sx={{ display: 'flex', alignItems: 'center' }}>
            <EventIcon sx={{ mr: 1 }} />
            Basic Schedule Parameters
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
            
            {/* // Add this to the "Basic Settings" accordion in ScheduleCoursesForm.jsx
            // After the semester selection, before the courses selection */}

            <Grid item xs={12}>
              <Typography variant="subtitle2" gutterBottom>Scheduling Scope</Typography>
              <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
                <Grid container spacing={2}>
                  <Grid item xs={12} md={6}>
                    <FormControl fullWidth margin="normal">
                      <InputLabel id="campus-label">Campus</InputLabel>
                      <Select
                        labelId="campus-label"
                        name="campus"
                        value={formData.campus || ''}
                        onChange={handleFormChange}
                        label="Campus"
                      >
                        <MenuItem value="">All Campuses</MenuItem>
                        {mockCampuses.map(campus => (
                          <MenuItem key={campus.id} value={campus.id}>
                            {campus.name}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </Grid>
                  
                  <Grid item xs={12} md={6}>
                    <FormControl fullWidth margin="normal">
                      <InputLabel id="school-label">School/College</InputLabel>
                      <Select
                        labelId="school-label"
                        name="school"
                        value={formData.school || ''}
                        onChange={handleFormChange}
                        label="School/College"
                        disabled={!formData.campus}
                      >
                        <MenuItem value="">All Schools</MenuItem>
                        {mockSchools
                          .filter(school => !formData.campus || school.campusId === formData.campus)
                          .map(school => (
                            <MenuItem key={school.id} value={school.id}>
                              {school.name}
                            </MenuItem>
                          ))}
                      </Select>
                    </FormControl>
                  </Grid>
                  
                  <Grid item xs={12} md={6}>
                    <FormControl fullWidth margin="normal">
                      <InputLabel id="department-label">Department</InputLabel>
                      <Select
                        labelId="department-label"
                        name="department"
                        value={formData.department || ''}
                        onChange={handleFormChange}
                        label="Department"
                        disabled={!formData.school}
                      >
                        <MenuItem value="">All Departments</MenuItem>
                        {mockDepartments
                          .filter(dept => !formData.school || dept.schoolId === formData.school)
                          .map(dept => (
                            <MenuItem key={dept.id} value={dept.id}>
                              {dept.name}
                            </MenuItem>
                          ))}
                      </Select>
                    </FormControl>
                  </Grid>
                  
                  {/* // 在ScheduleCoursesForm.jsx中增加Subject选择部分，放在Department和Programme选择之间 */}
                  <Grid item xs={12} md={6}>
                    <FormControl fullWidth margin="normal">
                      <InputLabel id="subject-label">Subject</InputLabel>
                      <Select
                        labelId="subject-label"
                        name="subject"
                        value={formData.subject || ''}
                        onChange={handleFormChange}
                        label="Subject"
                        disabled={!formData.department}
                      >
                        <MenuItem value="">All Subjects</MenuItem>
                        {mockSubjects
                          .filter(subj => !formData.department || subj.departmentId === formData.department)
                          .map(subject => (
                            <MenuItem key={subject.id} value={subject.id}>
                              {subject.code} - {subject.name}
                            </MenuItem>
                          ))}
                      </Select>
                    </FormControl>
                  </Grid>

                  <Grid item xs={12} md={6}>
                    <FormControl fullWidth margin="normal">
                      <InputLabel id="programme-label">Programme</InputLabel>
                      <Select
                        labelId="programme-label"
                        name="programme"
                        value={formData.programme || ''}
                        onChange={handleFormChange}
                        label="Programme"
                        disabled={!formData.department}
                      >
                        <MenuItem value="">All Programmes</MenuItem>
                        {mockProgrammes
                          .filter(prog => !formData.department || prog.departmentId === formData.department)
                          .map(prog => (
                            <MenuItem key={prog.id} value={prog.id}>
                              {prog.name}
                            </MenuItem>
                          ))}
                      </Select>
                    </FormControl>
                  </Grid>
                </Grid>
              </Paper>
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
                  {getFilteredItems('courses').map(course => (
                    <MenuItem key={course.id} value={course.id}>
                      {course.code} - {course.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            
            <Grid item xs={12} md={6}>
              <FormControl fullWidth margin="normal">
                <InputLabel id="teachers-label">Teachers</InputLabel>
                <Select
                  labelId="teachers-label"
                  name="teachers"
                  multiple
                  value={formData.teachers}
                  onChange={handleFormChange}
                  label="Teachers"
                  renderValue={(selected) => (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                      {selected.map((value) => {
                        const teacher = mockTeachers.find(t => t.id === value);
                        return <Chip key={value} label={teacher ? teacher.name : value} />;
                      })}
                    </Box>
                  )}
                >
                  {getFilteredItems('teacher').map(teacher => (
                    <MenuItem key={teacher.id} value={teacher.id}>
                      {teacher.code} - {teacher.department}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            
            <Grid item xs={12} md={6}>
              <FormControl fullWidth margin="normal">
                <InputLabel id="classrooms-label">Classrooms</InputLabel>
                <Select
                  labelId="classrooms-label"
                  name="classrooms"
                  multiple
                  value={formData.classrooms}
                  onChange={handleFormChange}
                  label="Classrooms"
                  renderValue={(selected) => (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                      {selected.map((value) => {
                        const classroom = mockClassrooms.find(c => c.id === value);
                        return <Chip key={value} label={classroom ? `${classroom.building}-${classroom.name}` : value} />;
                      })}
                    </Box>
                  )}
                >
                  {getFilteredItems('classroom').map(classroom => (
                    <MenuItem key={classroom.id} value={classroom.id}>
                      {classroom.building}-{classroom.name} (Capacity: {classroom.capacity})
                      </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
          </Grid>
        </AccordionDetails>
      </Accordion>
      
      {/* // Replace the Advanced Parameters accordion with this: */}
      <Accordion defaultExpanded sx={{ mb: 2 }}>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography variant="subtitle1" sx={{ display: 'flex', alignItems: 'center', width: '100%' }}>
            <SchoolIcon sx={{ mr: 1 }} />
            Advanced Parameters
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <Box sx={{ mb: 3 }}>
            <Typography variant="subtitle2" gutterBottom>Active Parameter Preset</Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
              <FormControl sx={{ minWidth: 250, mr: 2 }}>
                <InputLabel>Parameter Preset</InputLabel>
                <Select
                  value={activePresetId}
                  onChange={(e) => onPresetSelect(e.target.value)}
                  label="Parameter Preset"
                >
                  {presets.map(preset => (
                    <MenuItem key={preset.id} value={preset.id}>
                      {preset.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              {/* // Then update all the navigation buttons to use this function
              // For example in the Advanced Parameters section: */}
              <Button 
                variant="outlined" 
                size="small" 
                sx={{ ml: 2 }}
                onClick={() => navigateToSystemConfig(0)} // Navigate to Parameter Settings tab
              >
                Manage Parameters
              </Button>
            </Box>
            
            <Alert severity="info" sx={{ mb: 2 }}>
              Parameters can only be modified in the System Configuration page.
              Changes made there will be reflected here automatically.
            </Alert>
          </Box>
          
          {/* Parameter categories in read-only mode */}
          <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
            <Typography variant="subtitle2" gutterBottom>Academic Parameters</Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <Typography variant="body2">
                  <strong>Gender Segregation:</strong> {systemParameters.genderSegregation ? 'Enabled' : 'Disabled'}
                </Typography>
                <Typography variant="body2">
                  <strong>Faculty Workload Balance:</strong> {systemParameters.facultyWorkloadBalance.toFixed(1)}
                </Typography>
                <Typography variant="body2">
                  <strong>Student Schedule Compactness:</strong> {systemParameters.studentScheduleCompactness.toFixed(1)}
                </Typography>
              </Grid>
            </Grid>
          </Paper>
          
          <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
            <Typography variant="subtitle2" gutterBottom>Campus & Location Parameters</Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <Typography variant="body2">
                  <strong>Cross-School Enrollment:</strong> {systemParameters.allowCrossSchoolEnrollment ? 'Allowed' : 'Not Allowed'}
                </Typography>
                <Typography variant="body2">
                  <strong>Multi-Campus Constraints:</strong> {systemParameters.enableMultiCampusConstraints ? 'Enabled' : 'Disabled'}
                </Typography>
                <Typography variant="body2">
                  <strong>Prioritize Home Buildings:</strong> {systemParameters.prioritizeHomeBuildings ? 'Yes' : 'No'}
                </Typography>
              </Grid>
              <Grid item xs={12} md={6}>
                <Typography variant="body2">
                  <strong>Minimum Travel Time:</strong> {systemParameters.minimumTravelTime} minutes
                </Typography>
                <Typography variant="body2">
                  <strong>Campus Travel Weight:</strong> {systemParameters.campusTravelTimeWeight.toFixed(1)}
                </Typography>
              </Grid>
            </Grid>
          </Paper>
          
          <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
            <Typography variant="subtitle2" gutterBottom>Time & Course Parameters</Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <Typography variant="body2">
                  <strong>Maximum Consecutive Classes:</strong> {systemParameters.maximumConsecutiveClasses}
                </Typography>
                <Typography variant="body2">
                  <strong>Holiday Exclusions:</strong> {systemParameters.holidayExclusions ? 'Enabled' : 'Disabled'}
                </Typography>
                <Typography variant="body2">
                  <strong>Ramadan Schedule:</strong> {systemParameters.enableRamadanSchedule ? 'Enabled' : 'Disabled'}
                </Typography>
              </Grid>
              <Grid item xs={12} md={6}>
                <Typography variant="body2">
                  <strong>Cross-Listed Courses:</strong> {systemParameters.allowCrossListedCourses ? 'Allowed' : 'Not Allowed'}
                </Typography>
                <Typography variant="body2">
                  <strong>Cross-Department Teaching:</strong> {systemParameters.allowCrossDepartmentTeaching ? 'Allowed' : 'Not Allowed'}
                </Typography>
                <Typography variant="body2">
                  <strong>Generate Alternatives:</strong> {systemParameters.generateAlternatives ? 'Yes' : 'No'}
                </Typography>
              </Grid>
            </Grid>
          </Paper>
          
          <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
            <ParameterOptimization 
              currentParameters={systemParameters}
              historicalData={null}
              onApplyChanges={handleOptimizationApplied}
            />
          </Box>
        </AccordionDetails>
      </Accordion>
      
      {/* Constraint Settings - Read-only Version */}
      <Accordion defaultExpanded sx={{ mb: 2 }}>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography variant="subtitle1" sx={{ display: 'flex', alignItems: 'center' }}>
            <TuneIcon sx={{ mr: 1 }} />
            Constraint Settings
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <Alert severity="info" sx={{ mb: 2 }}>
            Constraint settings have been moved to the System Configuration page for centralized management.
            // In the Constraint section:
            <Button 
              variant="outlined" 
              size="small" 
              sx={{ ml: 2 }}
              onClick={() => navigateToSystemConfig(1)} // Navigate to Constraint Management tab
            >
              Manage Constraints
            </Button>
          </Alert>

          {/* Summary of active constraints */}
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="subtitle2" gutterBottom>Active Constraints</Typography>
            
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <Box sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" color="primary" gutterBottom>
                    Hard Constraints
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {mockConstraints
                      .filter(c => c.type === 'Hard' && c.isActive)
                      .map(constraint => (
                        <Chip 
                          key={constraint.id} 
                          label={constraint.name} 
                          color="error" 
                          variant="outlined" 
                          size="small"
                        />
                      ))}
                  </Box>
                </Box>
              </Grid>
              
              <Grid item xs={12}>
                <Box>
                  <Typography variant="subtitle2" color="primary" gutterBottom>
                    Soft Constraints
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {mockConstraints
                      .filter(c => c.type === 'Soft' && c.isActive)
                      .map(constraint => (
                        <Chip 
                          key={constraint.id} 
                          label={`${constraint.name} (Weight: ${constraint.weight.toFixed(1)})`} 
                          color="primary" 
                          variant="outlined" 
                          size="small"
                        />
                      ))}
                  </Box>
                </Box>
              </Grid>
            </Grid>
          </Paper>
          
          {/* Teacher and Classroom Availability Summary */}
          <Box sx={{ mt: 3 }}>
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <Paper variant="outlined" sx={{ p: 2, height: '100%' }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Teacher Availability
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {formData.enableTeacherAvailability ? 
                      `${formData.teachers.length} teachers with configured availability constraints.` : 
                      'Teacher availability constraints are disabled.'}
                  </Typography>
                  {formData.enableTeacherAvailability && (
                     // For Teacher Availability:
                    <Button 
                    variant="text" 
                    size="small" 
                    sx={{ mt: 1 }}
                    onClick={() => navigateToSystemConfig(1, 1)} // Navigate to Teacher Availability subtab
                  >
                    View Details
                  </Button>
                  )}
                </Paper>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Paper variant="outlined" sx={{ p: 2, height: '100%' }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Classroom Assignment
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {formData.enableClassroomTypeMatching ? 
                      `Classroom type matching enabled with weight: ${formData.classroomTypeMatchingWeight.toFixed(1)}` : 
                      'Classroom type matching constraints are disabled.'}
                  </Typography>
                  {formData.enableClassroomTypeMatching && (
                     // For Classroom Assignment:
                    <Button 
                    variant="text" 
                    size="small" 
                    sx={{ mt: 1 }}
                    onClick={() => navigateToSystemConfig(1, 2)} // Navigate to Classroom Assignment subtab
                  >
                    View Details
                  </Button>
                  )}
                </Paper>
              </Grid>
            </Grid>
          </Box>
        </AccordionDetails>
      </Accordion>
      
      {/* Generate Button */}
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
        <Button
          variant="contained"
          color="primary"
          size="large"
          onClick={handleGenerateSchedule}
          disabled={isGenerating}
          sx={{ minWidth: 200 }}
        >
          {isGenerating ? 'Generating...' : 'Generate Schedule'}
        </Button>
      </Box>
      
      {isGenerating && (
        <LinearProgress sx={{ mt: 2 }} />
      )}
    </Box>
  );
};

export default ScheduleCoursesForm;