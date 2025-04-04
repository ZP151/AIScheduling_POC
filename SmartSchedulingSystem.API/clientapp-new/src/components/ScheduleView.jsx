// src/components/ScheduleView.jsx
import React, { useState } from 'react';
import {
    Paper, Typography, Box, Button,
    Tabs, Tab, Table, TableBody,
    TableCell, TableContainer, TableHead,
    TableRow, Chip, Grid, Divider, Alert,
    Card, CardContent
} from '@mui/material';

import { scheduleApi } from '../services/api';

// 辅助函数：获取周几的中文名称
const getDayName = (day) => {
    const days = ['周日', '周一', '周二', '周三', '周四', '周五', '周六'];
    return days[day % 7];
};

const ScheduleView = ({ scheduleData, onStatusChange }) => {
    // Tab页状态
    const [tabValue, setTabValue] = useState(0);
    const [error, setError] = useState(null);

    // 如果没有数据，显示提示信息
    if (!scheduleData || !scheduleData.items || scheduleData.items.length === 0) {
        return (
            <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
                <Typography variant="h6" color="text.secondary">
                    暂无排课数据
                </Typography>
            </Paper>
        );
    }

    // 处理Tab切换
    const handleTabChange = (event, newValue) => {
        setTabValue(newValue);
    };

    // 处理排课状态变更
    const handleStatusChange = async (action) => {
        try {
            if (action === 'publish') {
                await scheduleApi.publishSchedule(scheduleData.scheduleId);
                if (onStatusChange) onStatusChange('Published');
            } else if (action === 'cancel') {
                await scheduleApi.cancelSchedule(scheduleData.scheduleId);
                if (onStatusChange) onStatusChange('Cancelled');
            }
            setError(null);
        } catch (err) {
            setError(`操作失败: ${err.response?.data?.message || err.message}`);
        }
    };

    // 按教师分组
    const groupByTeacher = () => {
        const groups = {};
        scheduleData.items.forEach(item => {
            if (!groups[item.teacherId]) {
                groups[item.teacherId] = {
                    teacherName: item.teacherName,
                    schedules: []
                };
            }
            groups[item.teacherId].schedules.push(item);
        });
        return groups;
    };

    // 按教室分组
    const groupByClassroom = () => {
        const groups = {};
        scheduleData.items.forEach(item => {
            if (!groups[item.classroomId]) {
                groups[item.classroomId] = {
                    classroomName: `${item.building}-${item.classroomName}`,
                    schedules: []
                };
            }
            groups[item.classroomId].schedules.push(item);
        });
        return groups;
    };

    // 获取时间表格数据
    const getTimeTableData = () => {
        // 获取所有时间段
        const timeSlots = [...new Set(scheduleData.items.map(item => item.timeSlotId))];
        timeSlots.sort((a, b) => {
            const itemA = scheduleData.items.find(item => item.timeSlotId === a);
            const itemB = scheduleData.items.find(item => item.timeSlotId === b);
            return itemA.dayOfWeek - itemB.dayOfWeek || itemA.startTime.localeCompare(itemB.startTime);
        });

        // 获取所有教室
        const classrooms = [...new Set(scheduleData.items.map(item => item.classroomId))];
        classrooms.sort((a, b) => {
            const itemA = scheduleData.items.find(item => item.classroomId === a);
            const itemB = scheduleData.items.find(item => item.classroomId === b);
            return itemA.building.localeCompare(itemB.building) || itemA.classroomName.localeCompare(itemB.classroomName);
        });

        // 创建时间表格
        const tableData = {};
        timeSlots.forEach(timeSlotId => {
            const item = scheduleData.items.find(item => item.timeSlotId === timeSlotId);
            const key = `${item.dayOfWeek}-${item.startTime}`;
            if (!tableData[key]) {
                tableData[key] = {
                    dayOfWeek: item.dayOfWeek,
                    dayName: getDayName(item.dayOfWeek),
                    startTime: item.startTime,
                    endTime: item.endTime,
                    classrooms: {}
                };
            }

            scheduleData.items.forEach(schedule => {
                if (schedule.dayOfWeek === item.dayOfWeek && schedule.startTime === item.startTime) {
                    tableData[key].classrooms[schedule.classroomId] = schedule;
                }
            });
        });

        return { tableData, classrooms };
    };

    // 创建日历视图
    const renderCalendarView = () => {
        const { tableData, classrooms } = getTimeTableData();

        return (
            <TableContainer component={Paper} sx={{ mt: 2 }}>
                <Table sx={{ minWidth: 650 }}>
                    <TableHead>
                        <TableRow>
                            <TableCell sx={{ width: 150 }}>时间/教室</TableCell>
                            {classrooms.map(classroomId => {
                                const item = scheduleData.items.find(item => item.classroomId === classroomId);
                                return (
                                    <TableCell key={classroomId} align="center">
                                        {item.building}-{item.classroomName}
                                    </TableCell>
                                );
                            })}
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {Object.values(tableData).sort((a, b) => a.dayOfWeek - b.dayOfWeek || a.startTime.localeCompare(b.startTime)).map((timeRow) => (
                            <TableRow key={`${timeRow.dayOfWeek}-${timeRow.startTime}`}>
                                <TableCell component="th" scope="row">
                                    {timeRow.dayName}<br />
                                    {timeRow.startTime}-{timeRow.endTime}
                                </TableCell>
                                {classrooms.map(classroomId => {
                                    const schedule = timeRow.classrooms[classroomId];
                                    return (
                                        <TableCell key={classroomId} align="center" sx={{ height: 80 }}>
                                            {schedule ? (
                                                <Box>
                                                    <Typography variant="body2" fontWeight="bold">
                                                        {schedule.courseName}
                                                    </Typography>
                                                    <Typography variant="caption" display="block">
                                                        {schedule.teacherName}
                                                    </Typography>
                                                    <Chip
                                                        size="small"
                                                        label={schedule.sectionCode}
                                                        color="primary"
                                                        variant="outlined"
                                                        sx={{ mt: 0.5 }}
                                                    />
                                                </Box>
                                            ) : null}
                                        </TableCell>
                                    );
                                })}
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        );
    };

    // 创建列表视图
    const renderListView = () => {
        return (
            <TableContainer component={Paper} sx={{ mt: 2 }}>
                <Table>
                    <TableHead>
                        <TableRow>
                            <TableCell>课程</TableCell>
                            <TableCell>班级</TableCell>
                            <TableCell>教师</TableCell>
                            <TableCell>教室</TableCell>
                            <TableCell>时间</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {scheduleData.items.sort((a, b) =>
                            a.courseName.localeCompare(b.courseName) ||
                            a.sectionCode.localeCompare(b.sectionCode)
                        ).map((item, index) => (
                            <TableRow key={index}>
                                <TableCell>{item.courseName}</TableCell>
                                <TableCell>{item.courseCode}-{item.sectionCode}</TableCell>
                                <TableCell>{item.teacherName}</TableCell>
                                <TableCell>{item.building}-{item.classroomName}</TableCell>
                                <TableCell>
                                    {getDayName(item.dayOfWeek)} {item.startTime}-{item.endTime}
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        );
    };

    // 创建教师视图
    const renderTeacherView = () => {
        const teacherGroups = groupByTeacher();

        return (
            <Grid container spacing={3} sx={{ mt: 1 }}>
                {Object.values(teacherGroups).map((group, index) => (
                    <Grid item xs={12} md={6} key={index}>
                        <Card variant="outlined">
                            <CardContent>
                                <Typography variant="h6" gutterBottom>
                                    {group.teacherName}
                                </Typography>
                                <Divider sx={{ mb: 2 }} />
                                <TableContainer>
                                    <Table size="small">
                                        <TableHead>
                                            <TableRow>
                                                <TableCell>课程</TableCell>
                                                <TableCell>时间</TableCell>
                                                <TableCell>教室</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {group.schedules.sort((a, b) =>
                                                a.dayOfWeek - b.dayOfWeek ||
                                                a.startTime.localeCompare(b.startTime)
                                            ).map((item, idx) => (
                                                <TableRow key={idx}>
                                                    <TableCell>{item.courseName}</TableCell>
                                                    <TableCell>
                                                        {getDayName(item.dayOfWeek)}<br />
                                                        {item.startTime}-{item.endTime}
                                                    </TableCell>
                                                    <TableCell>{item.building}-{item.classroomName}</TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </TableContainer>
                            </CardContent>
                        </Card>
                    </Grid>
                ))}
            </Grid>
        );
    };

    // 创建教室视图
    const renderClassroomView = () => {
        const classroomGroups = groupByClassroom();

        return (
            <Grid container spacing={3} sx={{ mt: 1 }}>
                {Object.values(classroomGroups).map((group, index) => (
                    <Grid item xs={12} md={6} key={index}>
                        <Card variant="outlined">
                            <CardContent>
                                <Typography variant="h6" gutterBottom>
                                    {group.classroomName}
                                </Typography>
                                <Divider sx={{ mb: 2 }} />
                                <TableContainer>
                                    <Table size="small">
                                        <TableHead>
                                            <TableRow>
                                                <TableCell>课程</TableCell>
                                                <TableCell>教师</TableCell>
                                                <TableCell>时间</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {group.schedules.sort((a, b) =>
                                                a.dayOfWeek - b.dayOfWeek ||
                                                a.startTime.localeCompare(b.startTime)
                                            ).map((item, idx) => (
                                                <TableRow key={idx}>
                                                    <TableCell>{item.courseName}</TableCell>
                                                    <TableCell>{item.teacherName}</TableCell>
                                                    <TableCell>
                                                        {getDayName(item.dayOfWeek)}<br />
                                                        {item.startTime}-{item.endTime}
                                                    </TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </TableContainer>
                            </CardContent>
                        </Card>
                    </Grid>
                ))}
            </Grid>
        );
    };

    return (
        <Paper elevation={3} sx={{ p: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h5" component="h2">
                    排课结果
                </Typography>
                <Box>
                    <Chip
                        label={scheduleData.status === 'Draft' ? '草稿' :
                            scheduleData.status === 'Published' ? '已发布' : '已取消'}
                        color={scheduleData.status === 'Draft' ? 'warning' :
                            scheduleData.status === 'Published' ? 'success' : 'error'}
                        sx={{ mr: 1 }}
                    />
                    <Chip
                        label={`质量分数: ${(scheduleData.score * 100).toFixed(0)}%`}
                        color={scheduleData.score > 0.9 ? 'success' :
                            scheduleData.score > 0.7 ? 'info' : 'warning'}
                        variant="outlined"
                    />
                </Box>
            </Box>

            {error && (
                <Alert severity="error" sx={{ mb: 2 }}>
                    {error}
                </Alert>
            )}

            {/* 操作按钮 */}
            {scheduleData.status === 'Draft' && (
                <Box sx={{ mb: 2 }}>
                    <Button
                        variant="contained"
                        color="primary"
                        onClick={() => handleStatusChange('publish')}
                        sx={{ mr: 1 }}
                    >
                        发布排课
                    </Button>
                    <Button
                        variant="outlined"
                        color="error"
                        onClick={() => handleStatusChange('cancel')}
                    >
                        取消排课
                    </Button>
                </Box>
            )}

            {/* 冲突警告 */}
            {scheduleData.conflicts && scheduleData.conflicts.length > 0 && (
                <Alert severity="warning" sx={{ mb: 2 }}>
                    <Typography variant="subtitle2">
                        检测到 {scheduleData.conflicts.length} 个排课冲突:
                    </Typography>
                    <ul style={{ margin: '8px 0', paddingLeft: 20 }}>
                        {scheduleData.conflicts.map((conflict, index) => (
                            <li key={index}>{conflict}</li>
                        ))}
                    </ul>
                </Alert>
            )}

            {/* 视图选择Tabs */}
            <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
                <Tabs value={tabValue} onChange={handleTabChange} aria-label="schedule view tabs">
                    <Tab  label="日历视图" id="tab-0" />
                    <Tab  label="列表视图" id="tab-1" />
                    <Tab  label="教师视图" id="tab-2" />
                    <Tab  label="教室视图" id="tab-3" />
                </Tabs>
            </Box>

            {/* Tab内容 */}
            <Box role="tabpanel" hidden={tabValue !== 0} id="tabpanel-0">
                {tabValue === 0 && renderCalendarView()}
            </Box>

            <Box role="tabpanel" hidden={tabValue !== 1} id="tabpanel-1">
                {tabValue === 1 && renderListView()}
            </Box>

            <Box role="tabpanel" hidden={tabValue !== 2} id="tabpanel-2">
                {tabValue === 2 && renderTeacherView()}
            </Box>

            <Box role="tabpanel" hidden={tabValue !== 3} id="tabpanel-3">
                {tabValue === 3 && renderClassroomView()}
            </Box>
        </Paper>
    );
};

export default ScheduleView;
