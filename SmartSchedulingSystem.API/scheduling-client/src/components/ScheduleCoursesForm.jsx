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

// 导入真实API服务
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

    // API端点选择
    apiEndpointType: API_ENDPOINTS.TEST_MOCK,

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
    
    // Classroom Availability settings
    enableClassroomAvailability: true,
    classroomAvailabilitySettings: {},
  });
  
  const [isGenerating, setIsGenerating] = useState(false);
  const [feedback, setFeedback] = useState({ open: false, message: '', type: 'info' });

  // Add this state variable for debugging
  const [debugMode, setDebugMode] = useState({
    disableMockFallback: false, // 禁用模拟回退，强制使用真实API
    verboseLogging: false      // 启用详细日志记录
  });

  // Add this method to toggle debug options
  const toggleDebugOption = (option) => {
    setDebugMode(prev => ({
      ...prev,
      [option]: !prev[option]
    }));
  };

  // 添加反馈消息的处理函数
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
    console.log(`表单变更: ${name} = `, value);
    
    // 特殊处理组织单位字段的选择，清空相关的选择
    if (name === 'campus') {
      setFormData({
        ...formData,
        [name]: value,
        school: '',
        department: '',
        subject: '',
        programme: '',
        // 清空相关选择
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
        // 清空相关选择
        courses: [],
        teachers: []
      });
    } else if (name === 'department') {
      setFormData({
        ...formData,
        [name]: value,
        subject: '',
        programme: '',
        // 清空相关选择
        courses: [],
        teachers: []
      });
    } else if (name === 'subject') {
      setFormData({
        ...formData,
        [name]: value,
        // 清空相关选择
        courses: [],
        teachers: []
      });
    } else if (name === 'programme') {
      setFormData({
        ...formData,
        [name]: value,
        // 清空相关选择
        courses: []
      });
    } else {
      setFormData({
        ...formData,
        [name]: value
      });
    }
    
    // 当改变课程、教师或教室时，记录当前筛选后的列表
    if (['courses', 'teachers', 'classrooms'].includes(name)) {
      console.log(`${name} 筛选后列表:`, getFilteredItems(name));
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

  // 添加一个新的useEffect钩子来处理初始化默认选择
  useEffect(() => {
    // 组件首次加载时自动选择默认值
    if (formData.courses.length === 0 && formData.teachers.length === 0 && formData.classrooms.length === 0) {
      console.log('初始化默认选择...');
      
      // 默认选择第一个校区
      const defaultCampus = mockCampuses.length > 0 ? mockCampuses[0].id : '';
      
      // 获取该校区下的第一个学院
      const campusSchools = mockSchools.filter(school => school.campusId === defaultCampus);
      const defaultSchool = campusSchools.length > 0 ? campusSchools[0].id : '';
      
      // 获取该学院下的第一个系别
      const schoolDepts = mockDepartments.filter(dept => dept.schoolId === defaultSchool);
      const defaultDept = schoolDepts.length > 0 ? schoolDepts[0].id : '';
      
      // 更新formData
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
  }, []); // 仅在组件挂载时运行一次

  // 在组织单位选择更新后自动选择默认课程、教师和教室
  useEffect(() => {
    // 仅在有组织单位选择但没有课程、教师或教室选择时执行
    if ((formData.campus || formData.school || formData.department) && 
        (formData.courses.length === 0 || formData.teachers.length === 0 || formData.classrooms.length === 0)) {
      
      console.log('基于组织单位选择更新默认选择...');
      
      // 获取可用项目
      const availableCourses = getFilteredItems('courses').map(c => c.id).slice(0, 3);
      const availableTeachers = getFilteredItems('teachers').map(t => t.id).slice(0, 2);
      const availableClassrooms = getFilteredItems('classrooms').map(c => c.id).slice(0, 2);
      
      console.log('可用课程:', availableCourses);
      console.log('可用教师:', availableTeachers);
      console.log('可用教室:', availableClassrooms);
      
      // 检查是否有可用的选项
      if (availableCourses.length === 0 && formData.courses.length === 0) {
        showFeedback('当前筛选条件下没有可用课程', 'warning');
      }
      
      if (availableTeachers.length === 0 && formData.teachers.length === 0) {
        showFeedback('当前筛选条件下没有可用教师', 'warning');
      }
      
      if (availableClassrooms.length === 0 && formData.classrooms.length === 0) {
        showFeedback('当前筛选条件下没有可用教室', 'warning');
      }
      
      // 只更新尚未选择的项目
      setFormData(current => ({
        ...current,
        courses: current.courses.length === 0 ? availableCourses : current.courses,
        teachers: current.teachers.length === 0 ? availableTeachers : current.teachers,
        classrooms: current.classrooms.length === 0 ? availableClassrooms : current.classrooms
      }));
    }
  }, [formData.campus, formData.school, formData.department]); // 当组织单位选择变化时执行

  // Add this helper function to your component
  const getFilteredItems = (itemType) => {
    // 获取当前的筛选条件
    const filterParams = {
      campus: formData.campus,
      school: formData.school,
      department: formData.department,
      subject: formData.subject,
      programme: formData.programme
    };
    
    console.log(`筛选${itemType}，条件:`, filterParams);
    
    switch (itemType) {
      case 'courses':
        // Filter courses based on selected organizational units
        let filteredCourses = [...mockCourses];
        
        // 优先按学科筛选
        if (filterParams.subject) {
          console.log(`按学科ID ${filterParams.subject} 筛选课程`);
          filteredCourses = filteredCourses.filter(course => course.subjectId === filterParams.subject);
        } 
        // 如果没有选择学科但选择了专业
        else if (filterParams.programme) {
          console.log(`按专业ID ${filterParams.programme} 筛选课程`);
          // 假设课程有programmeId字段，如果没有，需要根据实际关联逻辑修改
          filteredCourses = filteredCourses.filter(course => course.programmeId === filterParams.programme);
        } 
        // 如果没有选择学科和专业但选择了系别
        else if (filterParams.department) {
          console.log(`按系别ID ${filterParams.department} 筛选课程`);
          // 根据系别筛选科目，然后筛选课程
          const departmentSubjects = mockSubjects.filter(subject => subject.departmentId === filterParams.department);
          const subjectIds = departmentSubjects.map(subject => subject.id);
          console.log(`系别 ${filterParams.department} 下的学科IDs:`, subjectIds);
          
          filteredCourses = filteredCourses.filter(course => 
            subjectIds.includes(course.subjectId)
          );
        } 
        // 如果只选择了学院
        else if (filterParams.school) {
          console.log(`按学院ID ${filterParams.school} 筛选课程`);
          // 获取学院下所有系别
          const schoolDepts = mockDepartments.filter(dept => dept.schoolId === filterParams.school);
          const deptIds = schoolDepts.map(dept => dept.id);
          console.log(`学院 ${filterParams.school} 下的系别IDs:`, deptIds);
          
          // 获取这些系别下的所有学科
          const deptSubjects = mockSubjects.filter(subject => 
            deptIds.includes(subject.departmentId)
          );
          const subjectIds = deptSubjects.map(subject => subject.id);
          console.log(`学院 ${filterParams.school} 下的学科IDs:`, subjectIds);
          
          filteredCourses = filteredCourses.filter(course => 
            subjectIds.includes(course.subjectId)
          );
        } 
        // 如果只选择了校区
        else if (filterParams.campus) {
          console.log(`按校区ID ${filterParams.campus} 筛选课程`);
          // 获取校区下所有学院
          const campusSchools = mockSchools.filter(school => school.campusId === filterParams.campus);
          const schoolIds = campusSchools.map(school => school.id);
          console.log(`校区 ${filterParams.campus} 下的学院IDs:`, schoolIds);
          
          // 获取这些学院下的所有系别
          const schoolDepts = mockDepartments.filter(dept => 
            schoolIds.includes(dept.schoolId)
          );
          const deptIds = schoolDepts.map(dept => dept.id);
          console.log(`校区 ${filterParams.campus} 下的系别IDs:`, deptIds);
          
          // 获取这些系别下的所有学科
          const deptSubjects = mockSubjects.filter(subject => 
            deptIds.includes(subject.departmentId)
          );
          const subjectIds = deptSubjects.map(subject => subject.id);
          console.log(`校区 ${filterParams.campus} 下的学科IDs:`, subjectIds);
          
          filteredCourses = filteredCourses.filter(course => 
            subjectIds.includes(course.subjectId)
          );
        }
        
        console.log(`筛选后的课程数量: ${filteredCourses.length}`);
        return filteredCourses;
        
      case 'teachers':
        // Filter teachers based on selected organizational units
        let filteredTeachers = [...mockTeachers];
        
        // 优先按学科筛选
        if (filterParams.subject) {
          console.log(`按学科ID ${filterParams.subject} 筛选教师`);
          const eligibleTeacherIds = mockTeacherSubjects
            .filter(ts => ts.subjectId === filterParams.subject)
            .map(ts => ts.teacherId);
          
          console.log(`可教授学科 ${filterParams.subject} 的教师IDs:`, eligibleTeacherIds);
          
          filteredTeachers = filteredTeachers.filter(teacher => 
            eligibleTeacherIds.includes(teacher.id)
          );
        } 
        // 如果没有选择学科但选择了系别
        else if (filterParams.department) {
          console.log(`按系别ID ${filterParams.department} 筛选教师`);
          filteredTeachers = filteredTeachers.filter(teacher => teacher.departmentId === filterParams.department);
        } 
        // 如果没有选择学科和系别但选择了学院
        else if (filterParams.school) {
          console.log(`按学院ID ${filterParams.school} 筛选教师`);
          // 获取学院下所有系别
          const schoolDepts = mockDepartments.filter(dept => dept.schoolId === filterParams.school);
          const deptIds = schoolDepts.map(dept => dept.id);
          console.log(`学院 ${filterParams.school} 下的系别IDs:`, deptIds);
          
          filteredTeachers = filteredTeachers.filter(teacher => 
            deptIds.includes(teacher.departmentId)
          );
        } 
        // 如果只选择了校区
        else if (filterParams.campus) {
          console.log(`按校区ID ${filterParams.campus} 筛选教师`);
          // 获取校区下所有学院
          const campusSchools = mockSchools.filter(school => school.campusId === filterParams.campus);
          const schoolIds = campusSchools.map(school => school.id);
          console.log(`校区 ${filterParams.campus} 下的学院IDs:`, schoolIds);
          
          // 获取这些学院下的所有系别
          const schoolDepts = mockDepartments.filter(dept => 
            schoolIds.includes(dept.schoolId)
          );
          const deptIds = schoolDepts.map(dept => dept.id);
          console.log(`校区 ${filterParams.campus} 下的系别IDs:`, deptIds);
          
          filteredTeachers = filteredTeachers.filter(teacher => 
            deptIds.includes(teacher.departmentId)
          );
        }
        
        console.log(`筛选后的教师数量: ${filteredTeachers.length}`);
        return filteredTeachers;
        
      case 'classrooms':
        // Filter classrooms based on selected campus
        let filteredClassrooms = [...mockClassrooms];
        
        if (filterParams.campus) {
          console.log(`按校区ID ${filterParams.campus} 筛选教室`);
          filteredClassrooms = filteredClassrooms.filter(classroom => classroom.campusId === filterParams.campus);
        }
        
        console.log(`筛选后的教室数量: ${filteredClassrooms.length}`);
        return filteredClassrooms;
        
      default:
        console.log(`未知的筛选类型: ${itemType}`);
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
    // 验证数据
    if (!formData.semester) {
        showFeedback('Please select a semester', 'error');
      return;
    }
    
    // 确保至少选择了一个课程
    if (!formData.courses || formData.courses.length === 0) {
        showFeedback('Please select at least one course', 'error');
      return;
    }
    
    // 确保至少选择了一个教师
    if (!formData.teachers || formData.teachers.length === 0) {
        showFeedback('Please select at least one teacher', 'error');
      return;
    }
    
    // 确保至少选择了一个教室
    if (!formData.classrooms || formData.classrooms.length === 0) {
        showFeedback('Please select at least one classroom', 'error');
      return;
    }
    
    setIsGenerating(true);
      showFeedback('The scheduling program is being generated, please wait...', 'info');
    
    // 准备约束设置
    const constraintSettings = formData.constraintSettings || [];

    // 准备API请求数据 - 确保与DTO格式匹配
    const apiRequestData = {
      // 基本数据
      semester: formData.semester,
      courses: formData.courses || [],  // 将在API中转换为courseSectionIds
      teachers: formData.teachers || [], // 将在API中转换为teacherIds
      classrooms: formData.classrooms || [], // 将在API中转换为classroomIds
      
      // API端点类型
      apiEndpointType: formData.apiEndpointType,
      
      // 组织单位数据
      campus: formData.campus || null,
      school: formData.school || null,
      department: formData.department || null,
      subject: formData.subject || null,
      programme: formData.programme || null,
      schedulingScope: formData.schedulingScope || 'programme',
      
      // 约束和调度参数
      constraintSettings,
      
      // 多方案生成参数
      generateMultipleSolutions: true,
      solutionCount: 3,
      
      // 系统参数（来自props）
      useAI: systemParameters?.useAI || false,
      facultyWorkloadBalance: systemParameters?.facultyWorkloadBalance || 0.8,
      studentScheduleCompactness: systemParameters?.studentScheduleCompactness || 0.7,
      minimumTravelTime: systemParameters?.minimumTravelTime || 30,
      maximumConsecutiveClasses: systemParameters?.maximumConsecutiveClasses || 3,
      campusTravelTimeWeight: systemParameters?.campusTravelTimeWeight || 0.6,
      preferredClassroomProximity: systemParameters?.preferredClassroomProximity || 0.5,
      classroomTypeMatchingWeight: systemParameters?.classroomTypeMatchingWeight || 0.7,
      
      // 其他选项
      allowCrossSchoolEnrollment: systemParameters?.allowCrossSchoolEnrollment || true,
      allowCrossDepartmentTeaching: systemParameters?.allowCrossDepartmentTeaching || true,
      prioritizeHomeBuildings: systemParameters?.prioritizeHomeBuildings || true,
      genderSegregation: systemParameters?.genderSegregation || false,
      enableRamadanSchedule: systemParameters?.enableRamadanSchedule || false,
      allowCrossListedCourses: systemParameters?.allowCrossListedCourses || true,
      enableMultiCampusConstraints: systemParameters?.enableMultiCampusConstraints || true,
      holidayExclusions: systemParameters?.holidayExclusions || true,
      
      // 调试参数
      _debug: debugMode
    };
    
    console.log('发送排课请求数据:', apiRequestData);
    
    // 调用API
    generateScheduleApi(apiRequestData)
      .then(result => {
        setIsGenerating(false);
        console.log('收到排课结果:', result);
        
        // 检查是否有错误信息
        if (result.errorMessage) {
          showFeedback(`排课方案生成部分成功: ${result.errorMessage}`, 'warning');
        } else {
          showFeedback('排课方案生成成功!', 'success');
        }
        
        if (onScheduleGenerated) {
          // 将结果传递给父组件
          onScheduleGenerated(result);
        }
      })
      .catch(error => {
        console.error('生成排课方案失败:', error);
        setIsGenerating(false);
        // 显示错误提示给用户
        showFeedback(`生成排课方案失败: ${error.message || '未知错误'}`, 'error');
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
      
      {/* API端点选择 - 单独放置 */}
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