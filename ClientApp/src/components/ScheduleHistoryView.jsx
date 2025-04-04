// src/components/ScheduleHistoryView.jsx
import React, { useState, useEffect } from 'react';
import {
    Box,
    Paper,
    Typography,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Button,
    Card,
    CardContent,
    CardActions,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Tabs,
    Tab,
    Chip,
    Grid,
    CircularProgress,
    Alert
} from '@mui/material';
import {
    History as HistoryIcon,
    Visibility as ViewIcon,
    Delete as DeleteIcon,
    CompareArrows as CompareIcon,
    FileCopy as CopyIcon
} from '@mui/icons-material';
import { scheduleApi } from '../services/api';

const ScheduleHistoryView = ({ semesterId, onSelectSchedule }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [historyData, setHistoryData] = useState([]);
    const [compareDialogOpen, setCompareDialogOpen] = useState(false);
    const [selectedItems, setSelectedItems] = useState([]);
    const [compareTabValue, setCompareTabValue] = useState(0);

    // 加载历史数据
    useEffect(() => {
        if (!semesterId) return;

        const fetchHistory = async () => {
            setLoading(true);
            try {
                const response = await scheduleApi.getScheduleHistory(semesterId);
                setHistoryData(response.data);
                setError(null);
            } catch (err) {
                console.error('加载历史记录失败:', err);
                setError('加载历史记录失败: ' + (err.response?.data?.message || err.message));
            } finally {
                setLoading(false);
            }
        };

        fetchHistory();
    }, [semesterId]);

    // 查看排课方案
    const handleViewSchedule = (scheduleId) => {
        if (onSelectSchedule) {
            onSelectSchedule(scheduleId);
        }
    };

    // 复制排课方案
    const handleCopySchedule = async (scheduleId) => {
        try {
            // 这里应调用API复制排课方案
            // 实际项目中应添加相应的API
            // const response = await scheduleApi.copySchedule(scheduleId);

            // 刷新历史记录
            const response = await scheduleApi.getScheduleHistory(semesterId);
            setHistoryData(response.data);

            setError(null);
        } catch (err) {
            console.error('复制方案失败:', err);
            setError('复制方案失败: ' + (err.response?.data?.message || err.message));
        }
    };

    // 删除排课方案
    const handleDeleteSchedule = async (scheduleId) => {
        try {
            // 这里应调用API删除排课方案
            // 实际项目中应添加相应的API
            // await scheduleApi.deleteSchedule(scheduleId);

            // 从本地状态中移除
            setHistoryData(historyData.filter(item => item.scheduleId !== scheduleId));

            setError(null);
        } catch (err) {
            console.error('删除方案失败:', err);
            setError('删除方案失败: ' + (err.response?.data?.message || err.message));
        }
    };

    // 打开比较对话框
    const handleOpenCompareDialog = (schedule1, schedule2) => {
        setSelectedItems([schedule1, schedule2]);
        setCompareDialogOpen(true);
    };

    // 关闭比较对话框
    const handleCloseCompareDialog = () => {
        setCompareDialogOpen(false);
        setSelectedItems([]);
    };

    // Tab切换处理
    const handleCompareTabChange = (event, newValue) => {
        setCompareTabValue(newValue);
    };

    // 比较两个排课方案
    const compareSchedules = () => {
        if (selectedItems.length !== 2) return null;

        const [schedule1, schedule2] = selectedItems;

        // 创建项目映射以便快速查找
        const itemMap1 = new Map();
        schedule1.items.forEach(item => {
            itemMap1.set(`${item.courseCode}-${item.sectionCode}`, item);
        });

        const itemMap2 = new Map();
        schedule2.items.forEach(item => {
            itemMap2.set(`${item.courseCode}-${item.sectionCode}`, item);
        });

        // 分析差异
        const commonItems = [];
        const onlyInFirst = [];
        const onlyInSecond = [];
        const different = [];

        // 检查第一个排课方案中的项目
        schedule1.items.forEach(item1 => {
            const key = `${item1.courseCode}-${item1.sectionCode}`;
            const item2 = itemMap2.get(key);

            if (item2) {
                if (
                    item1.teacherId === item2.teacherId &&
                    item1.classroomId === item2.classroomId &&
                    item1.timeSlotId === item2.timeSlotId
                ) {
                    // 完全相同的项目
                    commonItems.push(item1);
                } else {
                    // 相同课程但安排不同
                    different.push({
                        courseCode: item1.courseCode,
                        courseName: item1.courseName,
                        schedule1: item1,
                        schedule2: item2
                    });
                }
            } else {
                // 只在第一个方案中存在
                onlyInFirst.push(item1);
            }
        });

        // 检查只在第二个方案中存在的项目
        schedule2.items.forEach(item2 => {
            const key = `${item2.courseCode}-${item2.sectionCode}`;
            if (!itemMap1.has(key)) {
                onlyInSecond.push(item2);
            }
        });

        return {
            commonItems,
            onlyInFirst,
            onlyInSecond,
            different
        };
    };

    if (loading && !historyData.length) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
                <CircularProgress />
            </Box>
        );
    }

    if (error) {
        return (
            <Alert severity="error" sx={{ mb: 3 }}>
                {error}
            </Alert>
        );
    }

    if (!historyData.length) {
        return (
            <Paper sx={{ p: 3, textAlign: 'center' }}>
                <Typography variant="h6" color="text.secondary">
                    暂无排课历史记录
                </Typography>
            </Paper>
        );
    }

    const compareResult = compareDialogOpen ? compareSchedules() : null;

    return (
        <Box>
            <Typography variant="h5" gutterBottom>
                <HistoryIcon sx={{ verticalAlign: 'middle', mr: 1 }} />
                排课历史记录
            </Typography>

            {/* 历史记录列表 */}
            <Grid container spacing={2}>
                {historyData.map((schedule, index) => (
                    <Grid item xs={12} md={6} lg={4} key={schedule.scheduleId}>
                        <Card>
                            <CardContent>
                                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                                    <Typography variant="h6">
                                        方案 #{schedule.scheduleId}
                                    </Typography>
                                    <Chip
                                        label={schedule.status === 'Draft' ? '草稿' :
                                            schedule.status === 'Published' ? '已发布' : '已取消'}
                                        color={schedule.status === 'Draft' ? 'warning' :
                                            schedule.status === 'Published' ? 'success' : 'error'}
                                        size="small"
                                    />
                                </Box>

                                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                                    创建时间: {new Date(schedule.createdAt).toLocaleString()}
                                </Typography>

                                <Box sx={{ mt: 2 }}>
                                    <Typography variant="body2">
                                        课程数量: {schedule.items.length}
                                    </Typography>
                                    <Typography variant="body2">
                                        评分: {(schedule.score * 100).toFixed(0)}%
                                    </Typography>
                                    {schedule.conflicts && schedule.conflicts.length > 0 && (
                                        <Typography variant="body2" color="error.main">
                                            冲突数量: {schedule.conflicts.length}
                                        </Typography>
                                    )}
                                </Box>
                            </CardContent>

                            <CardActions>
                                <Button
                                    size="small"
                                    startIcon={<ViewIcon />}
                                    onClick={() => handleViewSchedule(schedule.scheduleId)}
                                >
                                    查看
                                </Button>
                                <Button
                                    size="small"
                                    startIcon={<CopyIcon />}
                                    onClick={() => handleCopySchedule(schedule.scheduleId)}
                                >
                                    复制
                                </Button>
                                {schedule.status === 'Draft' && (
                                    <Button
                                        size="small"
                                        color="error"
                                        startIcon={<DeleteIcon />}
                                        onClick={() => handleDeleteSchedule(schedule.scheduleId)}
                                    >
                                        删除
                                    </Button>
                                )}
                                {index < historyData.length - 1 && (
                                    <Button
                                        size="small"
                                        startIcon={<CompareIcon />}
                                        onClick={() => handleOpenCompareDialog(schedule, historyData[index + 1])}
                                    >
                                        与旧版比较
                                    </Button>
                                )}
                            </CardActions>
                        </Card>
                    </Grid>
                ))}
            </Grid>

            {/* 比较对话框 */}
            <Dialog
                open={compareDialogOpen}
                onClose={handleCloseCompareDialog}
                maxWidth="lg"
                fullWidth
            >
                <DialogTitle>
                    比较排课方案
                    <Typography variant="subtitle2" sx={{ mt: 1 }}>
                        #{selectedItems[0]?.scheduleId} vs #{selectedItems[1]?.scheduleId}
                    </Typography>
                </DialogTitle>

                <DialogContent>
                    {compareResult && (
                        <Box>
                            <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
                                <Tabs value={compareTabValue} onChange={handleCompareTabChange}>
                                    <Tab label={`差异 (${compareResult.different.length})`} />
                                    <Tab label={`仅在方案#${selectedItems[0]?.scheduleId}中 (${compareResult.onlyInFirst.length})`} />
                                    <Tab label={`仅在方案#${selectedItems[1]?.scheduleId}中 (${compareResult.onlyInSecond.length})`} />
                                    <Tab label={`相同项目 (${compareResult.commonItems.length})`} />
                                </Tabs>
                            </Box>

                            {/* 差异项目 */}
                            {compareTabValue === 0 && (
                                <TableContainer>
                                    <Table>
                                        <TableHead>
                                            <TableRow>
                                                <TableCell>课程</TableCell>
                                                <TableCell colSpan={3} align="center">
                                                    方案 #{selectedItems[0]?.scheduleId}
                                                </TableCell>
                                                <TableCell colSpan={3} align="center">
                                                    方案 #{selectedItems[1]?.scheduleId}
                                                </TableCell>
                                            </TableRow>
                                            <TableRow>
                                                <TableCell></TableCell>
                                                <TableCell>教师</TableCell>
                                                <TableCell>教室</TableCell>
                                                <TableCell>时间</TableCell>
                                                <TableCell>教师</TableCell>
                                                <TableCell>教室</TableCell>
                                                <TableCell>时间</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {compareResult.different.map((diff, index) => (
                                                <TableRow key={index}>
                                                    <TableCell>
                                                        <Typography variant="body2">{diff.courseName}</Typography>
                                                        <Typography variant="caption" color="text.secondary">{diff.courseCode}</Typography>
                                                    </TableCell>
                                                    <TableCell>{diff.schedule1.teacherName}</TableCell>
                                                    <TableCell>{diff.schedule1.classroom}</TableCell>
                                                    <TableCell>{diff.schedule1.dayName} {diff.schedule1.startTime}-{diff.schedule1.endTime}</TableCell>
                                                    <TableCell>{diff.schedule2.teacherName}</TableCell>
                                                    <TableCell>{diff.schedule2.classroom}</TableCell>
                                                    <TableCell>{diff.schedule2.dayName} {diff.schedule2.startTime}-{diff.schedule2.endTime}</TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </TableContainer>
                            )}

                            {/* 仅在第一个方案中的项目 */}
                            {compareTabValue === 1 && (
                                <TableContainer>
                                    <Table>
                                        <TableHead>
                                            <TableRow>
                                                <TableCell>课程</TableCell>
                                                <TableCell>教师</TableCell>
                                                <TableCell>教室</TableCell>
                                                <TableCell>时间</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {compareResult.onlyInFirst.map((item, index) => (
                                                <TableRow key={index}>
                                                    <TableCell>
                                                        <Typography variant="body2">{item.courseName}</Typography>
                                                        <Typography variant="caption" color="text.secondary">{item.courseCode}</Typography>
                                                    </TableCell>
                                                    <TableCell>{item.teacherName}</TableCell>
                                                    <TableCell>{item.classroom}</TableCell>
                                                    <TableCell>{item.dayName} {item.startTime}-{item.endTime}</TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </TableContainer>
                            )}

                            {/* 仅在第二个方案中的项目 */}
                            {compareTabValue === 2 && (
                                <TableContainer>
                                    <Table>
                                        <TableHead>
                                            <TableRow>
                                                <TableCell>课程</TableCell>
                                                <TableCell>教师</TableCell>
                                                <TableCell>教室</TableCell>
                                                <TableCell>时间</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {compareResult.onlyInSecond.map((item, index) => (
                                                <TableRow key={index}>
                                                    <TableCell>
                                                        <Typography variant="body2">{item.courseName}</Typography>
                                                        <Typography variant="caption" color="text.secondary">{item.courseCode}</Typography>
                                                    </TableCell>
                                                    <TableCell>{item.teacherName}</TableCell>
                                                    <TableCell>{item.classroom}</TableCell>
                                                    <TableCell>{item.dayName} {item.startTime}-{item.endTime}</TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </TableContainer>
                            )}

                            {/* 相同项目 */}
                            {compareTabValue === 3 && (
                                <TableContainer>
                                    <Table>
                                        <TableHead>
                                            <TableRow>
                                                <TableCell>课程</TableCell>
                                                <TableCell>教师</TableCell>
                                                <TableCell>教室</TableCell>
                                                <TableCell>时间</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {compareResult.commonItems.map((item, index) => (
                                                <TableRow key={index}>
                                                    <TableCell>
                                                        <Typography variant="body2">{item.courseName}</Typography>
                                                        <Typography variant="caption" color="text.secondary">{item.courseCode}</Typography>
                                                    </TableCell>
                                                    <TableCell>{item.teacherName}</TableCell>
                                                    <TableCell>{item.classroom}</TableCell>
                                                    <TableCell>{item.dayName} {item.startTime}-{item.endTime}</TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </TableContainer>
                            )}
                        </Box>
                    )}
                </DialogContent>

                <DialogActions>
                    <Button onClick={handleCloseCompareDialog}>关闭</Button>
                </DialogActions>
            </Dialog>
        </Box>
    );
};

export default ScheduleHistoryView;
