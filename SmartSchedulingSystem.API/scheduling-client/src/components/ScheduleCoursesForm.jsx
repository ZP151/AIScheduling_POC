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
  Paper,
  Snackbar
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
  mockTimeSlots
} from '../services/mockData';

// Import real API service
import { generateScheduleApi, API_ENDPOINTS } from '../services/api';

import RequirementAnalyzer  from './LLM/RequirementAnalyzer';
import ParameterOptimization from './LLM/ParameterOptimization';
import TeacherAvailabilitySettings from './constraints/TeacherAvailabilitySettings';
import ClassroomAssignmentSettings from './constraints/ClassroomAssignmentSettings';
import ClassroomAvailabilitySettings from './constraints/ClassroomAvailabilitySettings';

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
    courses: [],
    teachers: [],
    classrooms: [],

    // API endpoint selection
    apiEndpointType: API_ENDPOINTS.TEST_MOCK,

     // Add these new organizational scope parameters
    campus: '',
    school: '',
    department: '',
    subject: '',  // New
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
    
    // Classroom Availability settings
    enableClassroomAvailability: true,
    classroomAvailabilitySettings: {},
  });
  
  const [isGenerating, setIsGenerating] = useState(false);
  const [feedback, setFeedback] = useState({ open: false, message: '', type: 'info' });

  // Add this state variable for debugging
  const [debugMode, setDebugMode] = useState({
    disableMockFallback: false, // Disable mock fallback, force using real API
    verboseLogging: false      // Enable verbose logging
  });

  // Add this method to toggle debug options
  const toggleDebugOption = (option) => {
    setDebugMode(prev => ({
      ...prev,
      [option]: !prev[option]
    }));
  };

  // Add feedback message handler
  const showFeedback = (message, type = 'info') => {
    setFeedback({
      open: true,
      message,
      type
    });
  };

  const handleCloseFeedback = () => {
    setFeedback(prev => ({
      ...prev,
      open: false
    }));
  };

  const handleFormChange = (event) => {
    const { name, value } = event.target;
    console.log(`Form change: ${name} = `, value);
    
    // Special handling for organizational unit field selection, clear related selections
    if (name === 'campus') {
      setFormData({
        ...formData,
        [name]: value,
        school: '',
        department: '',
        subject: '',
        programme: '',
        // Clear related selections
        courses: [],
        teachers: [],
        classrooms: []
      });
    } else if (name === 'school') {
      setFormData({
        ...formData,
        [name]: value,
        department: '',
        subject: '',
        programme: '',
        // Clear related selections
        courses: [],
        teachers: []
      });
    } else if (name === 'department') {
      setFormData({
        ...formData,
        [name]: value,
        subject: '',
        programme: '',
        // Clear related selections
        courses: [],
        teachers: []
      });
    } else if (name === 'subject') {
      setFormData({
        ...formData,
        [name]: value,
        // Clear related selections
        courses: [],
        teachers: []
      });
    } else if (name === 'programme') {
      setFormData({
        ...formData,
        [name]: value,
        // Clear related selections
        courses: []
      });
    } else {
      setFormData({
        ...formData,
        [name]: value
      });
    }
    
    // When changing course, teacher or classroom, record the current filtered list
    if (['courses', 'teachers', 'classrooms'].includes(name)) {
      console.log(`${name} filtered list:`, getFilteredItems(name));
    }
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

  // Add a new useEffect hook to handle initial default selections
  useEffect(() => {
    // Automatically select default values when component first loads
    if (formData.courses.length === 0 && formData.teachers.length === 0 && formData.classrooms.length === 0) {
      console.log('Initializing default selections...');
      
      // Default select first campus
      const defaultCampus = mockCampuses.length > 0 ? mockCampuses[0].id : '';
      
      // Get first school under this campus
      const campusSchools = mockSchools.filter(school => school.campusId === defaultCampus);
      const defaultSchool = campusSchools.length > 0 ? campusSchools[0].id : '';
      
      // Get first department under this school
      const schoolDepts = mockDepartments.filter(dept => dept.schoolId === defaultSchool);
      const defaultDept = schoolDepts.length > 0 ? schoolDepts[0].id : '';
      
      // Update formData
      setFormData(prev => {
        const newFormData = {
          ...prev,
          campus: defaultCampus,
          school: defaultSchool,
          department: defaultDept
        };
        
        return newFormData;
      });
    }
  }, []); // Only run once when component mounts

  // Automatically select default courses, teachers and classrooms after organizational unit selection updates
  useEffect(() => {
    // Only execute when there are organizational unit selections but no course, teacher or classroom selections
    if ((formData.campus || formData.school || formData.department) && 
        (formData.courses.length === 0 || formData.teachers.length === 0 || formData.classrooms.length === 0)) {
      
      console.log('Updating default selections based on organizational unit selection...');
      
      // Get available items
      const availableCourses = getFilteredItems('courses').map(c => c.id).slice(0, 3);
      const availableTeachers = getFilteredItems('teachers').map(t => t.id).slice(0, 2);
      const availableClassrooms = getFilteredItems('classrooms').map(c => c.id).slice(0, 2);
      
      console.log('Available courses:', availableCourses);
      console.log('Available teachers:', availableTeachers);
      console.log('Available classrooms:', availableClassrooms);
      
      // Check if there are available options
      if (availableCourses.length === 0 && formData.courses.length === 0) {
        showFeedback('No available courses under current filter conditions', 'warning');
      }
      
      if (availableTeachers.length === 0 && formData.teachers.length === 0) {
        showFeedback('No available teachers under current filter conditions', 'warning');
      }
      
      if (availableClassrooms.length === 0 && formData.classrooms.length === 0) {
        showFeedback('No available classrooms under current filter conditions', 'warning');
      }
      
      // Only update items that haven't been selected yet
      setFormData(current => ({
        ...current,
        courses: current.courses.length === 0 ? availableCourses : current.courses,
        teachers: current.teachers.length === 0 ? availableTeachers : current.teachers,
        classrooms: current.classrooms.length === 0 ? availableClassrooms : current.classrooms
      }));
    }
  }, [formData.campus, formData.school, formData.department]); // Execute when organizational unit selections change

  // Add this helper function to your component
  const getFilteredItems = (itemType) => {
    // Get current filter conditions
    const filterParams = {
      campus: formData.campus,
      school: formData.school,
      department: formData.department,
      subject: formData.subject,
      programme: formData.programme
    };
    
    console.log(`Filtering ${itemType}, conditions:`, filterParams);
    
    switch (itemType) {
      case 'courses':
        // Filter courses based on selected organizational units
        let filteredCourses = [...mockCourses];
        
        // Prioritize filtering by subject
        if (filterParams.subject) {
          console.log(`Filtering courses by subject ID ${filterParams.subject}`);
          filteredCourses = filteredCourses.filter(course => course.subjectId === filterParams.subject);
        } 
        // If no subject selected but programme selected
        else if (filterParams.programme) {
          console.log(`Filtering courses by programme ID ${filterParams.programme}`);
          // Filtering courses by programme ID
          filteredCourses = filteredCourses.filter(course => course.programmeId === filterParams.programme);
        } 
        // If no subject and programme selected but department selected
        else if (filterParams.department) {
          console.log(`Filtering courses by department ID ${filterParams.department}`);
          // Filter subjects by department, then filter courses
          const departmentSubjects = mockSubjects.filter(subject => subject.departmentId === filterParams.department);
          const subjectIds = departmentSubjects.map(subject => subject.id);
          console.log(`Subject IDs under department ${filterParams.department}:`, subjectIds);
          
          filteredCourses = filteredCourses.filter(course => 
            subjectIds.includes(course.subjectId)
          );
        } 
        // If only school selected
        else if (filterParams.school) {
          console.log(`Filtering courses by school ID ${filterParams.school}`);
          // Get all departments under this school
          const schoolDepts = mockDepartments.filter(dept => dept.schoolId === filterParams.school);
          const deptIds = schoolDepts.map(dept => dept.id);
          console.log(`Department IDs under school ${filterParams.school}:`, deptIds);
          
          // Get all subjects under these departments
          const deptSubjects = mockSubjects.filter(subject => 
            deptIds.includes(subject.departmentId)
          );
          const subjectIds = deptSubjects.map(subject => subject.id);
          console.log(`Subject IDs under school ${filterParams.school}:`, subjectIds);
          
          filteredCourses = filteredCourses.filter(course => 
            subjectIds.includes(course.subjectId)
          );
        } 
        // If only campus selected
        else if (filterParams.campus) {
          console.log(`Filtering courses by campus ID ${filterParams.campus}`);
          // Get all schools under this campus
          const campusSchools = mockSchools.filter(school => school.campusId === filterParams.campus);
          const schoolIds = campusSchools.map(school => school.id);
          console.log(`School IDs under campus ${filterParams.campus}:`, schoolIds);
          
          // Get all departments under these schools
          const schoolDepts = mockDepartments.filter(dept => 
            schoolIds.includes(dept.schoolId)
          );
          const deptIds = schoolDepts.map(dept => dept.id);
          console.log(`Department IDs under campus ${filterParams.campus}:`, deptIds);
          
          // Get all subjects under these departments
          const deptSubjects = mockSubjects.filter(subject => 
            deptIds.includes(subject.departmentId)
          );
          const subjectIds = deptSubjects.map(subject => subject.id);
          console.log(`Subject IDs under campus ${filterParams.campus}:`, subjectIds);
          
          filteredCourses = filteredCourses.filter(course => 
            subjectIds.includes(course.subjectId)
          );
        }
        
        console.log(`Number of filtered courses: ${filteredCourses.length}`);
        return filteredCourses;
        
      case 'teachers':
        // Filter teachers based on selected organizational units
        let filteredTeachers = [...mockTeachers];
        
        // Prioritize filtering by subject
        if (filterParams.subject) {
          console.log(`Filtering teachers by subject ID ${filterParams.subject}`);
          const eligibleTeacherIds = mockTeacherSubjects
            .filter(ts => ts.subjectId === filterParams.subject)
            .map(ts => ts.teacherId);
          
          console.log(`Teachers who can teach subject ${filterParams.subject}:`, eligibleTeacherIds);
          
          filteredTeachers = filteredTeachers.filter(teacher => 
            eligibleTeacherIds.includes(teacher.id)
          );
        } 
        // If no subject selected but department selected
        else if (filterParams.department) {
          console.log(`Filtering teachers by department ID ${filterParams.department}`);
          filteredTeachers = filteredTeachers.filter(teacher => teacher.departmentId === filterParams.department);
        } 
        // If no subject and department selected but school selected
        else if (filterParams.school) {
          console.log(`Filtering teachers by school ID ${filterParams.school}`);
          // Get all departments under this school
          const schoolDepts = mockDepartments.filter(dept => dept.schoolId === filterParams.school);
          const deptIds = schoolDepts.map(dept => dept.id);
          console.log(`Department IDs under school ${filterParams.school}:`, deptIds);
          
          filteredTeachers = filteredTeachers.filter(teacher => 
            deptIds.includes(teacher.departmentId)
          );
        } 
        // If only campus selected
        else if (filterParams.campus) {
          console.log(`Filtering teachers by campus ID ${filterParams.campus}`);
          // Get all schools under this campus
          const campusSchools = mockSchools.filter(school => school.campusId === filterParams.campus);
          const schoolIds = campusSchools.map(school => school.id);
          console.log(`School IDs under campus ${filterParams.campus}:`, schoolIds);
          
          // Get all departments under these schools
          const schoolDepts = mockDepartments.filter(dept => 
            schoolIds.includes(dept.schoolId)
          );
          const deptIds = schoolDepts.map(dept => dept.id);
          console.log(`Department IDs under campus ${filterParams.campus}:`, deptIds);
          
          filteredTeachers = filteredTeachers.filter(teacher => 
            deptIds.includes(teacher.departmentId)
          );
        }
        
        console.log(`Number of filtered teachers: ${filteredTeachers.length}`);
        return filteredTeachers;
        
      case 'classrooms':
        // Filter classrooms based on selected campus
        let filteredClassrooms = [...mockClassrooms];
        
        if (filterParams.campus) {
          console.log(`Filtering classrooms by campus ID ${filterParams.campus}`);
          filteredClassrooms = filteredClassrooms.filter(classroom => classroom.campusId === filterParams.campus);
        }
        
        console.log(`Number of filtered classrooms: ${filteredClassrooms.length}`);
        return filteredClassrooms;
        
      default:
        console.log(`Unknown filter type: ${itemType}`);
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
      classroomTypeMatchingWeight: settings.weight
    }));
  };

  const handleClassroomAvailabilityUpdate = (settings) => {
    setFormData(prev => ({
      ...prev,
      classroomAvailabilitySettings: settings
    }));
  };

  // In ScheduleCoursesForm.jsx, update handleGenerateSchedule function
  const handleGenerateSchedule = () => {
    // Validate data
    if (!formData.semester) {
        showFeedback('Please select a semester', 'error');
      return;
    }
    
    // Ensure at least one course is selected
    if (!formData.courses || formData.courses.length === 0) {
        showFeedback('Please select at least one course', 'error');
      return;
    }
    
    // Ensure at least one teacher is selected
    if (!formData.teachers || formData.teachers.length === 0) {
        showFeedback('Please select at least one teacher', 'error');
      return;
    }
    
    // Ensure at least one classroom is selected
    if (!formData.classrooms || formData.classrooms.length === 0) {
        showFeedback('Please select at least one classroom', 'error');
      return;
    }
    
    setIsGenerating(true);
      showFeedback('The scheduling program is being generated, please wait...', 'info');
    
    // Prepare constraint settings
    const constraintSettings = formData.constraintSettings || [];

    // Prepare API request data - ensure it matches DTO format
    const apiRequestData = {
      // Basic data
      semester: formData.semester,
      courses: formData.courses || [],  // Will be converted to courseSectionIds in API
      teachers: formData.teachers || [], // Will be converted to teacherIds in API
      classrooms: formData.classrooms || [], // Will be converted to classroomIds in API
      
      // API endpoint type
      apiEndpointType: formData.apiEndpointType,
      
      // Organizational unit data
      campus: formData.campus || null,
      school: formData.school || null,
      department: formData.department || null,
      subject: formData.subject || null,
      programme: formData.programme || null,
      schedulingScope: formData.schedulingScope || 'programme',
      
      // Constraints and scheduling parameters
      constraintSettings,
      
      // Multiple solution generation parameters
      generateMultipleSolutions: true,
      solutionCount: 3,
      
      // System parameters (from props)
      useAI: systemParameters?.useAI || false,
      facultyWorkloadBalance: systemParameters?.facultyWorkloadBalance || 0.8,
      studentScheduleCompactness: systemParameters?.studentScheduleCompactness || 0.7,
      minimumTravelTime: systemParameters?.minimumTravelTime || 30,
      maximumConsecutiveClasses: systemParameters?.maximumConsecutiveClasses || 3,
      campusTravelTimeWeight: systemParameters?.campusTravelTimeWeight || 0.6,
      preferredClassroomProximity: systemParameters?.preferredClassroomProximity || 0.5,
      classroomTypeMatchingWeight: systemParameters?.classroomTypeMatchingWeight || 0.7,
      
      // Other options
      allowCrossSchoolEnrollment: systemParameters?.allowCrossSchoolEnrollment || true,
      allowCrossDepartmentTeaching: systemParameters?.allowCrossDepartmentTeaching || true,
      prioritizeHomeBuildings: systemParameters?.prioritizeHomeBuildings || true,
      genderSegregation: systemParameters?.genderSegregation || false,
      enableRamadanSchedule: systemParameters?.enableRamadanSchedule || false,
      allowCrossListedCourses: systemParameters?.allowCrossListedCourses || true,
      enableMultiCampusConstraints: systemParameters?.enableMultiCampusConstraints || true,
      holidayExclusions: systemParameters?.holidayExclusions || true,
      
      // Debug parameters
      _debug: debugMode
    };
    
    console.log('Sending scheduling request data:', apiRequestData);
    
    // Call API
    generateScheduleApi(apiRequestData)
      .then(result => {
        setIsGenerating(false);
        console.log('Received scheduling result:', result);
        
        // Check for error messages
        if (result.errorMessage) {
          showFeedback(`Schedule generation partially successful: ${result.errorMessage}`, 'warning');
        } else {
          showFeedback('Schedule generation successful!', 'success');
        }
        
        if (onScheduleGenerated) {
          // Pass result to parent component
          onScheduleGenerated(result);
        }
      })
      .catch(error => {
        console.error('Failed to generate schedule:', error);
        setIsGenerating(false);
        // Show error message to user
        showFeedback(`Failed to generate schedule: ${error.message || 'Unknown error'}`, 'error');
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
      {/* Snackbar for feedback */}
      <Snackbar
        open={feedback.open}
        autoHideDuration={6000}
        onClose={handleCloseFeedback}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <Alert onClose={handleCloseFeedback} severity={feedback.type} sx={{ width: '100%' }}>
          {feedback.message}
        </Alert>
      </Snackbar>
      
      {/* LLM Constraint Analysis */}
      <Accordion sx={{ mb: 3 }}>
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
              {/* Display filtered course count */}
              <Typography variant="caption" color="text.secondary">
                {getFilteredItems('courses').length} courses available
              </Typography>
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
                  {getFilteredItems('teachers').map(teacher => (
                    <MenuItem key={teacher.id} value={teacher.id}>
                      {teacher.name} - {teacher.department}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              {/* Display filtered teacher count */}
              <Typography variant="caption" color="text.secondary">
                {getFilteredItems('teachers').length} teachers available
              </Typography>
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
                  {getFilteredItems('classrooms').map(classroom => (
                    <MenuItem key={classroom.id} value={classroom.id}>
                      {classroom.building}-{classroom.name} (Capacity: {classroom.capacity})
                      </MenuItem>
                  ))}
                </Select>
              </FormControl>
              {/* Display filtered classroom count */}
              <Typography variant="caption" color="text.secondary">
                {getFilteredItems('classrooms').length} classrooms available
              </Typography>
            </Grid>
          </Grid>
        </AccordionDetails>
      </Accordion>
      
      {/* API endpoint selection - Separate placement */}
      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Grid container spacing={2}>
          <Grid item xs={12}>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>API Endpoint Selection</Typography>
          </Grid>
          <Grid item xs={12}>
            <FormControl fullWidth>
              <InputLabel id="api-endpoint-label">API endpoint</InputLabel>
              <Select
                labelId="api-endpoint-label"
                name="apiEndpointType"
                value={formData.apiEndpointType}
                onChange={handleFormChange}
                label="API Endpoint"
              >
                <MenuItem value={API_ENDPOINTS.TEST_MOCK}>Test Controller Mock Endpoints (Randomized Class Scheduling)</MenuItem>
                <MenuItem value={API_ENDPOINTS.MOCK}>Simulation Scheduling API</MenuItem>
                <MenuItem value={API_ENDPOINTS.SCHEDULE_BASIC}>Basic Constraints (Level 1)</MenuItem>
                <MenuItem value={API_ENDPOINTS.SCHEDULE_ADVANCED}> Advanced Constraints (Level 2)</MenuItem>
                <MenuItem value={API_ENDPOINTS.SCHEDULE_ENHANCED}> Enhanced Constraints (Level 3)</MenuItem>
              </Select>
            </FormControl>
            <Typography variant="caption" color="text.secondary">
              Select the backend algorithm
            </Typography>
          </Grid>
        </Grid>
      </Paper>
      
      {/* Advanced Parameters */}
      <Accordion sx={{ mb: 2 }}>
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
      <Accordion sx={{ mb: 2 }}>
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
                    Classroom Availability
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {formData.enableClassroomAvailability ? 
                      `${formData.classrooms.length} classrooms with configured availability constraints.` : 
                      'Classroom availability constraints are disabled.'}
                  </Typography>
                  {formData.enableClassroomAvailability && (
                    <Button 
                      variant="text" 
                      size="small" 
                      sx={{ mt: 1 }}
                      onClick={() => navigateToSystemConfig(1, 3)} // Navigate to Classroom Availability subtab (assuming it's index 3)
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