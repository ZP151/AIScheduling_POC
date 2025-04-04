// src/pages/SchedulingPage.jsx
import React, { useState, useEffect } from 'react';
import {
    Container, Typography, Box, Paper,
    Tabs, Tab, CircularProgress, Alert,
    AppBar, Toolbar, Button, Select, MenuItem, FormControl, InputLabel,
    TextField, Grid, Chip, Divider, Card, CardContent
} from '@mui/material';

import ScheduleForm from '../components/ScheduleForm';
import ScheduleView from '../components/ScheduleView';
import ScheduleEditor from '../components/ScheduleEditor';
import { scheduleApi, semesterApi, courseApi, teacherApi } from '../services/api';

const SchedulingPage = () => {
    // 页面状态
    const [activeTab, setActiveTab] = useState(0);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    // 数据状态
    const [currentSchedule, setCurrentSchedule] = useState(null);
    const [scheduleHistory, setScheduleHistory] = useState([]);

    // 添加学期选择状态
    const [semesters, setSemesters] = useState([]);
    const [selectedSemesterId, setSelectedSemesterId] = useState(null);

    // 筛选器状态
    const [showFilters, setShowFilters] = useState(false);
    const [filters, setFilters] = useState({
        status: '',
        courseId: '',
        teacherId: '',
        minScore: '',
        maxItems: ''
    });

    // 可选项数据
    const [courses, setCourses] = useState([]);
    const [teachers, setTeachers] = useState([]);

    // 添加编辑模式状态
    const [isEditMode, setIsEditMode] = useState(false);

    // 初始加载学期列表
    useEffect(() => {
        const fetchSemesters = async () => {
            setLoading(true);
            try {
                const response = await semesterApi.getAllSemesters();
                setSemesters(response.data);

                // 如果有学期数据，选择第一个作为默认值
                if (response.data && response.data.length > 0) {
                    setSelectedSemesterId(response.data[0].semesterId);
                }
            } catch (err) {
                console.error('获取学期列表失败:', err);
                setError('获取学期列表失败: ' + (err.response?.data?.message || err.message));
            } finally {
                setLoading(false);
            }
        };

        fetchSemesters();
    }, []);

    // 初始加载筛选器选项数据
    useEffect(() => {
        const fetchFilterOptions = async () => {
            try {
                // 仅在初次展开筛选器时加载，避免不必要的API调用
                if (showFilters && courses.length === 0 && teachers.length === 0) {
                    const [coursesResponse, teachersResponse] = await Promise.all([
                        courseApi.getAllCourses(),
                        teacherApi.getAllTeachers()
                    ]);

                    setCourses(coursesResponse.data);
                    setTeachers(teachersResponse.data);
                }
            } catch (err) {
                console.error('加载筛选器选项失败:', err);
            }
        };

        fetchFilterOptions();
    }, [showFilters]);

    // 当选择的学期ID变化时，加载该学期的排课历史
    useEffect(() => {
        // 只有当选择了学期ID时才加载历史
        if (selectedSemesterId) {
            // 重置筛选器
            setFilters({
                status: '',
                courseId: '',
                teacherId: '',
                minScore: '',
                maxItems: ''
            });

            loadScheduleHistory(selectedSemesterId);
        }
    }, [selectedSemesterId]);

    // 处理筛选器变更
    const handleFilterChange = (name, value) => {
        setFilters({
            ...filters,
            [name]: value
        });
    };

    // 应用筛选器
    const applyFilters = () => {
        if (selectedSemesterId) {
            loadScheduleHistory(selectedSemesterId, filters);
        }
    };

    // 重置筛选器
    const resetFilters = () => {
        setFilters({
            status: '',
            courseId: '',
            teacherId: '',
            minScore: '',
            maxItems: ''
        });

        if (selectedSemesterId) {
            loadScheduleHistory(selectedSemesterId);
        }
    };

    // 当排课结果生成后的处理
    const handleScheduleGenerated = (scheduleResult) => {
        setCurrentSchedule(scheduleResult);
        setActiveTab(1); // 切换到查看Tab
        setIsEditMode(false); // 确保不是编辑模式
    };

    // 当排课状态变更后的处理
    const handleScheduleStatusChange = (newStatus) => {
        if (currentSchedule) {
            setCurrentSchedule({
                ...currentSchedule,
                status: newStatus
            });

            // 刷新历史记录
            if (selectedSemesterId) {
                loadScheduleHistory(selectedSemesterId, filters);
            }
        }
    };

    // 加载历史排课记录
    const loadScheduleHistory = async (semesterId, filterParams = {}) => {
        if (!semesterId) {
            console.warn('尝试加载历史记录，但未提供学期ID');
            return;
        }

        setLoading(true);
        setError(null);

        try {
            console.log(`正在加载学期ID ${semesterId} 的排课历史`, filterParams);

            // 构建查询参数
            const queryParams = new URLSearchParams();

            // 添加筛选条件
            if (filterParams.status) queryParams.append('status', filterParams.status);
            if (filterParams.courseId) queryParams.append('courseId', filterParams.courseId);
            if (filterParams.teacherId) queryParams.append('teacherId', filterParams.teacherId);
            if (filterParams.minScore) queryParams.append('minScore', filterParams.minScore);
            if (filterParams.maxItems) queryParams.append('maxItems', filterParams.maxItems);

            // 调用API
            const response = await scheduleApi.getScheduleHistory(semesterId, queryParams.toString());
            setScheduleHistory(response.data);
            console.log(`成功加载到 ${response.data.length} 条历史记录`);
        } catch (err) {
            console.error('加载排课历史失败:', err);
            setError('加载排课历史失败: ' + (err.response?.data?.message || err.message));
            setScheduleHistory([]); // 出错时设置为空数组
        } finally {
            setLoading(false);
        }
    };

    // 学期选择变更处理
    const handleSemesterChange = (event) => {
        setSelectedSemesterId(Number(event.target.value));
    };

    // 查看历史排课记录
    const viewHistorySchedule = async (scheduleId) => {
        setLoading(true);
        setError(null);

        try {
            const response = await scheduleApi.getScheduleById(scheduleId);
            setCurrentSchedule(response.data);
            setActiveTab(1); // 切换到查看Tab
            setIsEditMode(false); // 确保不是编辑模式
        } catch (err) {
            setError('获取排课记录失败: ' + (err.response?.data?.message || err.message));
        } finally {
            setLoading(false);
        }
    };

    // 添加进入编辑模式的处理函数
    const handleEditSchedule = () => {
        setIsEditMode(true);
    };

    // 添加保存编辑的处理函数
    const handleSaveEditedSchedule = (editedSchedule) => {
        setCurrentSchedule(editedSchedule);
        setIsEditMode(false);
    };

    // 处理Tab切换
    const handleTabChange = (event, newValue) => {
        // 如果从编辑模式切换，需要先退出编辑模式
        if (isEditMode && newValue !== 1) {
            setIsEditMode(false);
        }
        setActiveTab(newValue);
    };

    // 刷新当前学期的历史记录
    const refreshHistory = () => {
        if (selectedSemesterId) {
            loadScheduleHistory(selectedSemesterId, filters);
        }
    };

    // 激活筛选器面板
    const toggleFilters = () => {
        setShowFilters(!showFilters);
    };

    return (
        <div>
            <AppBar position="static" color="primary">
                <Toolbar>
                    <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
                        智能排课系统
                    </Typography>
                    <Button color="inherit" onClick={refreshHistory}>
                        刷新
                    </Button>
                </Toolbar>
            </AppBar>

            <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
                {error && (
                    <Alert severity="error" sx={{ mb: 3 }}>
                        {error}
                    </Alert>
                )}

                <Paper sx={{ mb: 3 }}>
                    <Tabs value={activeTab} onChange={handleTabChange} aria-label="schedule tabs">
                        <Tab label="创建排课" id="tab-0" />
                        <Tab label={isEditMode ? "编辑排课" : "查看排课"} id="tab-1" disabled={!currentSchedule} />
                        <Tab label="历史记录" id="tab-2" />
                    </Tabs>
                </Paper>

                <Box role="tabpanel" hidden={activeTab !== 0} id="tabpanel-0">
                    {activeTab === 0 && <ScheduleForm onScheduleGenerated={handleScheduleGenerated} />}
                </Box>

                <Box role="tabpanel" hidden={activeTab !== 1} id="tabpanel-1">
                    {activeTab === 1 && currentSchedule && !isEditMode && (
                        <ScheduleView
                            scheduleData={currentSchedule}
                            onStatusChange={handleScheduleStatusChange}
                            onEdit={handleEditSchedule}
                        />
                    )}

                    {activeTab === 1 && currentSchedule && isEditMode && (
                        <ScheduleEditor
                            scheduleData={currentSchedule}
                            onSave={handleSaveEditedSchedule}
                        />
                    )}
                </Box>

                <Box role="tabpanel" hidden={activeTab !== 2} id="tabpanel-2">
                    {activeTab === 2 && (
                        <div>
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
                                <Typography variant="h5">排课历史记录</Typography>

                                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                                    {/* 学期选择下拉框 */}
                                    <FormControl sx={{ minWidth: 200, mr: 2 }}>
                                        <InputLabel id="semester-select-label">选择学期</InputLabel>
                                        <Select
                                            labelId="semester-select-label"
                                            id="semester-select"
                                            value={selectedSemesterId || ''}
                                            label="选择学期"
                                            onChange={handleSemesterChange}
                                            disabled={loading || semesters.length === 0}
                                        >
                                            {semesters.length === 0 && (
                                                <MenuItem value="">暂无学期数据</MenuItem>
                                            )}
                                            {semesters.map((semester) => (
                                                <MenuItem key={semester.semesterId} value={semester.semesterId}>
                                                    {semester.name}
                                                </MenuItem>
                                            ))}
                                        </Select>
                                    </FormControl>

                                    {/* 筛选器按钮 */}
                                    <Button
                                        variant={showFilters ? "contained" : "outlined"}
                                        color="primary"
                                        onClick={toggleFilters}
                                    >
                                        筛选
                                    </Button>
                                </Box>
                            </Box>

                            {/* 筛选器面板 */}
                            {showFilters && (
                                <Card variant="outlined" sx={{ mb: 3 }}>
                                    <CardContent>
                                        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                                            <Typography variant="h6">
                                                筛选条件
                                            </Typography>
                                            <Button size="small" onClick={toggleFilters}>
                                                关闭
                                            </Button>
                                        </Box>

                                        <Grid container spacing={2}>
                                            {/* 状态筛选 */}
                                            <Grid item xs={12} md={3}>
                                                <FormControl fullWidth>
                                                    <InputLabel id="status-filter-label">状态</InputLabel>
                                                    <Select
                                                        labelId="status-filter-label"
                                                        value={filters.status}
                                                        label="状态"
                                                        onChange={(e) => handleFilterChange('status', e.target.value)}
                                                    >
                                                        <MenuItem value="">所有状态</MenuItem>
                                                        <MenuItem value="Draft">草稿</MenuItem>
                                                        <MenuItem value="Published">已发布</MenuItem>
                                                        <MenuItem value="Cancelled">已取消</MenuItem>
                                                    </Select>
                                                </FormControl>
                                            </Grid>

                                            {/* 课程筛选 */}
                                            <Grid item xs={12} md={3}>
                                                <FormControl fullWidth>
                                                    <InputLabel id="course-filter-label">课程</InputLabel>
                                                    <Select
                                                        labelId="course-filter-label"
                                                        value={filters.courseId}
                                                        label="课程"
                                                        onChange={(e) => handleFilterChange('courseId', e.target.value)}
                                                    >
                                                        <MenuItem value="">所有课程</MenuItem>
                                                        {courses.map((course) => (
                                                            <MenuItem key={course.courseId} value={course.courseId}>
                                                                {course.name}
                                                            </MenuItem>
                                                        ))}
                                                    </Select>
                                                </FormControl>
                                            </Grid>

                                            {/* 教师筛选 */}
                                            <Grid item xs={12} md={3}>
                                                <FormControl fullWidth>
                                                    <InputLabel id="teacher-filter-label">教师</InputLabel>
                                                    <Select
                                                        labelId="teacher-filter-label"
                                                        value={filters.teacherId}
                                                        label="教师"
                                                        onChange={(e) => handleFilterChange('teacherId', e.target.value)}
                                                    >
                                                        <MenuItem value="">所有教师</MenuItem>
                                                        {teachers.map((teacher) => (
                                                            <MenuItem key={teacher.teacherId} value={teacher.teacherId}>
                                                                {teacher.name}
                                                            </MenuItem>
                                                        ))}
                                                    </Select>
                                                </FormControl>
                                            </Grid>

                                            {/* 最低评分筛选 */}
                                            <Grid item xs={12} md={3}>
                                                <TextField
                                                    label="最低评分"
                                                    type="number"
                                                    value={filters.minScore}
                                                    onChange={(e) => handleFilterChange('minScore', e.target.value)}
                                                    InputProps={{
                                                        inputProps: { min: 0, max: 100, step: 5 }
                                                    }}
                                                    fullWidth
                                                    helperText="0-100"
                                                />
                                            </Grid>

                                            {/* 最大条目数量筛选 */}
                                            <Grid item xs={12} md={3}>
                                                <TextField
                                                    label="最大条目数"
                                                    type="number"
                                                    value={filters.maxItems}
                                                    onChange={(e) => handleFilterChange('maxItems', e.target.value)}
                                                    InputProps={{
                                                        inputProps: { min: 1 }
                                                    }}
                                                    fullWidth
                                                />
                                            </Grid>

                                            {/* 操作按钮 */}
                                            <Grid item xs={12} md={6}>
                                                <Box sx={{ display: 'flex', gap: 1, height: '100%', alignItems: 'center' }}>
                                                    <Button
                                                        variant="contained"
                                                        color="primary"
                                                        onClick={applyFilters}
                                                    >
                                                        应用筛选
                                                    </Button>
                                                    <Button
                                                        variant="outlined"
                                                        onClick={resetFilters}
                                                    >
                                                        重置
                                                    </Button>
                                                </Box>
                                            </Grid>
                                        </Grid>

                                        {/* 已选筛选条件标签 */}
                                        {Object.entries(filters).some(([key, value]) =>
                                            value && value !== ''
                                        ) && (
                                                <Box sx={{ mt: 2, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                                                    {filters.status && (
                                                        <Chip
                                                            label={`状态: ${filters.status === 'Draft' ? '草稿' :
                                                                filters.status === 'Published' ? '已发布' : '已取消'}`}
                                                            onDelete={() => handleFilterChange('status', '')}
                                                        />
                                                    )}
                                                    {filters.courseId && (
                                                        <Chip
                                                            label={`课程: ${courses.find(c => c.courseId === parseInt(filters.courseId))?.name || filters.courseId}`}
                                                            onDelete={() => handleFilterChange('courseId', '')}
                                                        />
                                                    )}
                                                    {filters.teacherId && (
                                                        <Chip
                                                            label={`教师: ${teachers.find(t => t.teacherId === parseInt(filters.teacherId))?.name || filters.teacherId}`}
                                                            onDelete={() => handleFilterChange('teacherId', '')}
                                                        />
                                                    )}
                                                    {filters.minScore && (
                                                        <Chip
                                                            label={`最低评分: ${filters.minScore}%`}
                                                            onDelete={() => handleFilterChange('minScore', '')}
                                                        />
                                                    )}
                                                    {filters.maxItems && (
                                                        <Chip
                                                            label={`最大条目数: ${filters.maxItems}`}
                                                            onDelete={() => handleFilterChange('maxItems', '')}
                                                        />
                                                    )}
                                                </Box>
                                            )}
                                    </CardContent>
                                </Card>
                            )}

                            {loading ? (
                                <Box display="flex" justifyContent="center" p={3}>
                                    <CircularProgress />
                                </Box>
                            ) : scheduleHistory.length === 0 ? (
                                <Paper sx={{ p: 3, textAlign: 'center' }}>
                                    <Typography variant="body1" color="text.secondary">
                                        {selectedSemesterId
                                            ? `该学期暂无历史排课记录${Object.values(filters).some(v => v) ? '或没有符合筛选条件的记录' : ''}`
                                            : `请选择一个学期查看排课历史`}
                                    </Typography>
                                </Paper>
                            ) : (
                                <div>
                                    {scheduleHistory.map((schedule, index) => (
                                        <Paper key={index} sx={{ p: 2, mb: 2 }}>
                                            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                                <Box>
                                                    <Typography variant="h6">
                                                        排课方案 #{schedule.scheduleId}
                                                        <Chip
                                                            label={schedule.status === 'Draft' ? '草稿' :
                                                                schedule.status === 'Published' ? '已发布' : '已取消'}
                                                            color={schedule.status === 'Draft' ? 'warning' :
                                                                schedule.status === 'Published' ? 'success' : 'error'}
                                                            size="small"
                                                            sx={{ ml: 1 }}
                                                        />
                                                    </Typography>
                                                    <Typography variant="body2" color="text.secondary">
                                                        创建时间: {new Date(schedule.createdAt).toLocaleString()}
                                                    </Typography>
                                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mt: 1 }}>
                                                        <Typography variant="body2">
                                                            包含 {schedule.items?.length || 0} 个课程安排
                                                        </Typography>
                                                        {schedule.score !== undefined && (
                                                            <Chip
                                                                label={`评分: ${(schedule.score * 100).toFixed(0)}%`}
                                                                color={schedule.score > 0.9 ? 'success' :
                                                                    schedule.score > 0.7 ? 'info' : 'warning'}
                                                                variant="outlined"
                                                                size="small"
                                                            />
                                                        )}
                                                    </Box>
                                                </Box>
                                                <Box>
                                                    <Button
                                                        variant="contained"
                                                        color="primary"
                                                        onClick={() => viewHistorySchedule(schedule.scheduleId)}
                                                    >
                                                        查看
                                                    </Button>
                                                </Box>
                                            </Box>
                                        </Paper>
                                    ))}
                                </div>
                            )}
                        </div>
                    )}
                </Box>
            </Container>
        </div>
    );
};

export default SchedulingPage;