// src/pages/SchedulingPage.jsx
import React, { useState, useEffect } from 'react';
import {
    Container, Typography, Box, Paper,
    Tabs, Tab, CircularProgress, Alert,
    AppBar, Toolbar, Button
} from '@mui/material';
import {
    Edit as EditIcon
} from '@mui/icons-material';
import ScheduleForm from '../components/ScheduleForm';
import ScheduleView from '../components/ScheduleView';
import ScheduleEditor from '../components/ScheduleEditor';
import { scheduleApi } from '../services/api';

const SchedulingPage = () => {
    // 页面状态
    const [activeTab, setActiveTab] = useState(0);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    // 数据状态
    const [currentSchedule, setCurrentSchedule] = useState(null);
    const [scheduleHistory, setScheduleHistory] = useState([]);

    // 添加编辑模式状态
    const [isEditMode, setIsEditMode] = useState(false);

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
            loadScheduleHistory();
        }
    };

    // 加载历史排课记录
    const loadScheduleHistory = async (semesterId = 1) => { // 默认使用第一个学期ID
        setLoading(true);
        setError(null);

        try {
            const response = await scheduleApi.getScheduleHistory(semesterId);
            setScheduleHistory(response.data);
        } catch (err) {
            setError('加载排课历史失败: ' + (err.response?.data?.message || err.message));
        } finally {
            setLoading(false);
        }
    };

    // 初始加载历史记录
    useEffect(() => {
        loadScheduleHistory();
    }, []);

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

    return (
        <div>
            <AppBar position="static" color="primary">
                <Toolbar>
                    <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
                        智能排课系统
                    </Typography>
                    <Button color="inherit" onClick={() => loadScheduleHistory()}>
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
                            <Typography variant="h5" gutterBottom>排课历史记录</Typography>

                            {loading ? (
                                <Box display="flex" justifyContent="center" p={3}>
                                    <CircularProgress />
                                </Box>
                            ) : scheduleHistory.length === 0 ? (
                                <Paper sx={{ p: 3, textAlign: 'center' }}>
                                    <Typography variant="body1" color="text.secondary">
                                        暂无历史排课记录
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
                                                    </Typography>
                                                    <Typography variant="body2" color="text.secondary">
                                                        创建时间: {new Date(schedule.createdAt).toLocaleString()}
                                                    </Typography>
                                                    <Typography variant="body2">
                                                        包含 {schedule.items?.length || 0} 个课程安排
                                                    </Typography>
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
