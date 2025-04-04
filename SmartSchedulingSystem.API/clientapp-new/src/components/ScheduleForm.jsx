// src/components/ScheduleForm.jsx
import React, { useState, useEffect } from 'react';
import {
    Container, Typography, Paper, Box, Button,
    FormControl, InputLabel, Select, MenuItem, Chip,
    Checkbox, FormControlLabel, Slider, Divider,
     Grid, Alert, CircularProgress
} from '@mui/material';
import { semesterApi, courseSectionApi, teacherApi, classroomApi, constraintApi, scheduleApi } from '../services/api';

const ScheduleForm = ({ onScheduleGenerated }) => {
    // 表单数据状态
    const [formData, setFormData] = useState({
        semesterId: '',
        courseSectionIds: [],
        teacherIds: [],
        classroomIds: [],
        useAIAssistance: false,
        constraintSettings: []
    });

    // 数据加载状态
    const [loading, setLoading] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState(null);

    // 选项数据状态
    const [semesters, setSemesters] = useState([]);
    const [courseSections, setCourseSections] = useState([]);
    const [teachers, setTeachers] = useState([]);
    const [classrooms, setClassrooms] = useState([]);
    const [constraints, setConstraints] = useState([]);

    // 初始加载数据
    useEffect(() => {
        const fetchInitialData = async () => {
            setLoading(true);
            try {
                // 加载学期数据
                const semestersResponse = await semesterApi.getAllSemesters();
                setSemesters(semestersResponse.data);

                // 加载约束条件
                const constraintsResponse = await constraintApi.getAllConstraints();
                setConstraints(constraintsResponse.data);

                // 初始化约束设置
                setFormData(prev => ({
                    ...prev,
                    constraintSettings: constraintsResponse.data.map(c => ({
                        constraintId: c.constraintId,
                        isActive: c.isActive,
                        weight: c.weight
                    }))
                }));

                // 加载教师数据
                const teachersResponse = await teacherApi.getAllTeachers();
                setTeachers(teachersResponse.data);

                // 加载教室数据
                const classroomsResponse = await classroomApi.getAllClassrooms();
                setClassrooms(classroomsResponse.data);

                setError(null);
            } catch (err) {
                setError('加载初始数据失败: ' + (err.response?.data?.message || err.message));
            } finally {
                setLoading(false);
            }
        };

        fetchInitialData();
    }, []);

    // 当选择学期时加载该学期的课程班级
    useEffect(() => {
        const fetchCourseSections = async () => {
            if (!formData.semesterId) return;

            setLoading(true);
            try {
                const response = await courseSectionApi.getCourseSectionsBySemester(formData.semesterId);
                setCourseSections(response.data);
                setError(null);
            } catch (err) {
                setError('加载课程班级失败: ' + (err.response?.data?.message || err.message));
            } finally {
                setLoading(false);
            }
        };

        fetchCourseSections();
    }, [formData.semesterId]);

    // 表单字段变更处理
    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData({
            ...formData,
            [name]: value
        });
    };

    // 多选字段变更处理
    const handleMultiChange = (e) => {
        const { name, value } = e.target;
        setFormData({
            ...formData,
            [name]: Array.isArray(value) ? value : [value]
        });
    };

    // 复选框变更处理
    const handleCheckboxChange = (e) => {
        const { name, checked } = e.target;
        setFormData({
            ...formData,
            [name]: checked
        });
    };

    // 约束条件变更处理
    const handleConstraintChange = (constraintId, field, value) => {
        setFormData(prev => ({
            ...prev,
            constraintSettings: prev.constraintSettings.map(cs =>
                cs.constraintId === constraintId ? { ...cs, [field]: value } : cs
            )
        }));
    };

    // 提交表单
    const handleSubmit = async (e) => {
        e.preventDefault();

        // 验证 semesterId 必须是数字
        if (!formData.semesterId || isNaN(Number(formData.semesterId))) {
            setError('请选择有效的学期');
            return;
        }

        // 确保 ids 不为空，默认为空数组
        const requestData = {
            semesterId: Number(formData.semesterId),
            courseSectionIds: formData.courseSectionIds.length ? formData.courseSectionIds : [],
            teacherIds: formData.teacherIds.length ? formData.teacherIds : [],
            classroomIds: formData.classroomIds.length ? formData.classroomIds : [],
            useAIAssistance: formData.useAIAssistance,
            constraintSettings: formData.constraintSettings || []
        };

        setSubmitting(true);
        setError(null);

        try {
            console.log('发送的排课请求数据:', requestData);
            const response = await scheduleApi.generateSchedule(requestData);

            if (onScheduleGenerated) {
                onScheduleGenerated(response.data);
            }
        } catch (err) {
            const errorMsg = err.response?.data?.message || err.message;
            setError(`生成排课失败: ${errorMsg}`);
            console.error('完整错误详情:', err);
        } finally {
            setSubmitting(false);
        }
    };

    if (loading && !semesters.length) {
        return (
            <Box display="flex" justifyContent="center" alignItems="center" height="400px">
                <CircularProgress />
            </Box>
        );
    }

    return (
        <Container maxWidth="md">
            <Paper elevation={3} sx={{ p: 4, mb: 4 }}>
                <Typography variant="h5" component="h1" gutterBottom>
                    排课参数设置
                </Typography>

                {error && (
                    <Alert severity="error" sx={{ mb: 3 }}>
                        {error}
                    </Alert>
                )}

                <form onSubmit={handleSubmit}>
                    <Grid container spacing={3}>
                        {/* 学期选择 */}
                        <Grid item xs={12}>
                            <FormControl fullWidth>
                                <InputLabel id="semester-label">学期</InputLabel>
                                <Select
                                    labelId="semester-label"
                                    id="semesterId"
                                    name="semesterId"
                                    value={formData.semesterId}
                                    onChange={handleChange}
                                    label="学期"
                                    required
                                >
                                    {semesters.map((semester) => (
                                        <MenuItem key={semester.semesterId} value={semester.semesterId}>
                                            {semester.name}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                        </Grid>

                        {/* 课程班级选择 */}
                        <Grid item xs={12}>
                            <FormControl fullWidth disabled={!formData.semesterId}>
                                <InputLabel id="course-sections-label">课程班级</InputLabel>
                                <Select
                                    labelId="course-sections-label"
                                    id="courseSectionIds"
                                    name="courseSectionIds"
                                    multiple
                                    value={formData.courseSectionIds}
                                    onChange={handleMultiChange}
                                    label="课程班级"
                                    renderValue={(selected) => (
                                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                                            {selected.map((value) => {
                                                const section = courseSections.find(cs => cs.courseSectionId === value);
                                                return (
                                                    <Chip key={value} label={section ? `${section.courseCode}-${section.sectionCode}` : value} />
                                                );
                                            })}
                                        </Box>
                                    )}
                                >
                                    {courseSections.map((section) => (
                                        <MenuItem key={section.courseSectionId} value={section.courseSectionId}>
                                            {section.courseCode} - {section.courseName} ({section.sectionCode})
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                            <Typography variant="caption" color="text.secondary">
                                不选择则排所有课程班级
                            </Typography>
                        </Grid>

                        {/* 教师选择 */}
                        <Grid item xs={12} md={6}>
                            <FormControl fullWidth>
                                <InputLabel id="teachers-label">教师</InputLabel>
                                <Select
                                    labelId="teachers-label"
                                    id="teacherIds"
                                    name="teacherIds"
                                    multiple
                                    value={formData.teacherIds}
                                    onChange={handleMultiChange}
                                    label="教师"
                                    renderValue={(selected) => (
                                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                                            {selected.map((value) => {
                                                const teacher = teachers.find(t => t.teacherId === value);
                                                return (
                                                    <Chip key={value} label={teacher ? teacher.name : value} />
                                                );
                                            })}
                                        </Box>
                                    )}
                                >
                                    {teachers.map((teacher) => (
                                        <MenuItem key={teacher.teacherId} value={teacher.teacherId}>
                                            {teacher.name}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                            <Typography variant="caption" color="text.secondary">
                                不选择则使用所有教师
                            </Typography>
                        </Grid>

                        {/* 教室选择 */}
                        <Grid item xs={12} md={6}>
                            <FormControl fullWidth>
                                <InputLabel id="classrooms-label">教室</InputLabel>
                                <Select
                                    labelId="classrooms-label"
                                    id="classroomIds"
                                    name="classroomIds"
                                    multiple
                                    value={formData.classroomIds}
                                    onChange={handleMultiChange}
                                    label="教室"
                                    renderValue={(selected) => (
                                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                                            {selected.map((value) => {
                                                const classroom = classrooms.find(c => c.classroomId === value);
                                                return (
                                                    <Chip key={value} label={classroom ? `${classroom.building}-${classroom.name}` : value} />
                                                );
                                            })}
                                        </Box>
                                    )}
                                >
                                    {classrooms.map((classroom) => (
                                        <MenuItem key={classroom.classroomId} value={classroom.classroomId}>
                                            {classroom.building} - {classroom.name} (容量: {classroom.capacity})
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                            <Typography variant="caption" color="text.secondary">
                                不选择则使用所有教室
                            </Typography>
                        </Grid>

                        {/* AI辅助选项 */}
                        <Grid item xs={12}>
                            <FormControlLabel
                                control={
                                    <Checkbox
                                        checked={formData.useAIAssistance}
                                        onChange={handleCheckboxChange}
                                        name="useAIAssistance"
                                    />
                                }
                                label="使用AI辅助排课 (获取智能建议)"
                            />
                        </Grid>

                        {/* 约束条件设置 */}
                        <Grid item xs={12}>
                            <Typography variant="h6" gutterBottom>
                                约束条件设置
                            </Typography>
                            <Divider sx={{ mb: 2 }} />

                            {constraints.map((constraint) => (
                                <Paper key={constraint.constraintId} elevation={1} sx={{ p: 2, mb: 2, bgcolor: 'background.paper' }}>
                                    <Typography variant="subtitle1" fontWeight="bold">
                                        {constraint.constraintName}
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                                        {constraint.constraintDescription}
                                    </Typography>

                                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={formData.constraintSettings.find(cs => cs.constraintId === constraint.constraintId)?.isActive || false}
                                                    onChange={(e) => handleConstraintChange(constraint.constraintId, 'isActive', e.target.checked)}
                                                    disabled={constraint.constraintType === 'Hard'} // 硬约束不能禁用
                                                />
                                            }
                                            label={constraint.constraintType === 'Hard' ? '硬约束 (必须满足)' : '启用约束'}
                                        />

                                        {constraint.constraintType !== 'Hard' && (
                                            <Box sx={{ width: 200, ml: 2 }}>
                                                <Typography id={`weight-slider-${constraint.constraintId}`} gutterBottom>
                                                    权重: {formData.constraintSettings.find(cs => cs.constraintId === constraint.constraintId)?.weight.toFixed(1) || 0}
                                                </Typography>
                                                <Slider
                                                    value={formData.constraintSettings.find(cs => cs.constraintId === constraint.constraintId)?.weight || 0}
                                                    onChange={(e, newValue) => handleConstraintChange(constraint.constraintId, 'weight', newValue)}
                                                    aria-labelledby={`weight-slider-${constraint.constraintId}`}
                                                    step={0.1}
                                                    marks
                                                    min={0}
                                                    max={1}
                                                    disabled={!formData.constraintSettings.find(cs => cs.constraintId === constraint.constraintId)?.isActive}
                                                />
                                            </Box>
                                        )}
                                    </Box>
                                </Paper>
                            ))}
                        </Grid>

                        <Grid item xs={12}>
                            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
                                <Button
                                    variant="contained"
                                    color="primary"
                                    size="large"
                                    type="submit"
                                    disabled={submitting || !formData.semesterId}
                                    sx={{ minWidth: 200 }}
                                >
                                    {submitting ? <CircularProgress size={24} /> : '生成排课'}
                                </Button>
                            </Box>
                        </Grid>
                    </Grid>
                </form>
            </Paper>
        </Container>
    );
};

export default ScheduleForm;
