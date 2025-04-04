// src/components/ScheduleEditor.jsx
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
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    FormControl,
    InputLabel,
    Select,
    MenuItem,
    IconButton,
    Chip,
    Grid,
    Alert,
    Snackbar,
    Accordion,
    AccordionSummary,
    AccordionDetails,
    Divider
} from '@mui/material';
import { scheduleApi, teacherApi, classroomApi, timeSlotApi } from '../services/api';

const ScheduleEditor = ({ scheduleData, onSave }) => {
    const [editedSchedule, setEditedSchedule] = useState(null);
    const [isEditing, setIsEditing] = useState(false);
    const [dialogOpen, setDialogOpen] = useState(false);
    const [selectedItem, setSelectedItem] = useState(null);
    const [availableTeachers, setAvailableTeachers] = useState([]);
    const [availableClassrooms, setAvailableClassrooms] = useState([]);
    const [availableTimeSlots, setAvailableTimeSlots] = useState([]);
    const [editHistory, setEditHistory] = useState([]);
    const [snackbarOpen, setSnackbarOpen] = useState(false);
    const [snackbarMessage, setSnackbarMessage] = useState('');
    const [conflicts, setConflicts] = useState([]);

    // 初始化编辑数据
    useEffect(() => {
        if (scheduleData) {
            setEditedSchedule({ ...scheduleData });
            // 清空历史记录和冲突
            setEditHistory([]);
            setConflicts([]);
        }
    }, [scheduleData]);

    // 加载可用资源
    useEffect(() => {
        const fetchResources = async () => {
            try {
                const [teachersResponse, classroomsResponse, timeSlotsResponse] = await Promise.all([
                    teacherApi.getAllTeachers(),
                    classroomApi.getAllClassrooms(),
                    timeSlotApi.getAllTimeSlots()
                ]);

                setAvailableTeachers(teachersResponse.data);
                setAvailableClassrooms(classroomsResponse.data);
                setAvailableTimeSlots(timeSlotsResponse.data);
            } catch (error) {
                console.error('Error loading resources:', error);
                setSnackbarMessage('加载资源失败');
                setSnackbarOpen(true);
            }
        };

        fetchResources();
    }, []);

    // 开始编辑模式
    const handleStartEditing = () => {
        setIsEditing(true);
    };

    // 保存编辑
    const handleSaveChanges = async () => {
        try {
            // 检查冲突
            const detectedConflicts = detectConflicts(editedSchedule.items);
            if (detectedConflicts.length > 0) {
                setConflicts(detectedConflicts);
                setSnackbarMessage(`检测到 ${detectedConflicts.length} 个冲突，请解决后再保存`);
                setSnackbarOpen(true);
                return;
            }

            // 保存到编辑历史
            const historyEntry = {
                timestamp: new Date(),
                description: `修改了排课方案 #${editedSchedule.scheduleId}`,
                changes: compareSchedules(scheduleData, editedSchedule)
            };
            setEditHistory([historyEntry, ...editHistory]);

            // 调用API保存
            // 实际项目中这里会调用API保存修改
            await scheduleApi.updateSchedule(editedSchedule);

            // 通知父组件
            if (onSave) {
                onSave(editedSchedule);
            }

            setIsEditing(false);
            setSnackbarMessage('排课方案已保存');
            setSnackbarOpen(true);
        } catch (error) {
            console.error('保存失败:', error);
            setSnackbarMessage('保存失败');
            setSnackbarOpen(true);
        }
    };

    // 取消编辑
    const handleCancelEditing = () => {
        // 重置为原始数据
        setEditedSchedule({ ...scheduleData });
        setIsEditing(false);
        setConflicts([]);
    };

    // 打开编辑对话框
    const handleOpenDialog = (item) => {
        setSelectedItem({ ...item });
        setDialogOpen(true);
    };

    // 关闭编辑对话框
    const handleCloseDialog = () => {
        setDialogOpen(false);
        setSelectedItem(null);
    };

    // 保存项目更改
    const handleSaveItem = () => {
        // 检查当前编辑是否会导致冲突
        const tempItems = editedSchedule.items.map(item =>
            item.scheduleId === selectedItem.scheduleId ? selectedItem : item
        );

        const newConflicts = detectConflicts(tempItems);

        // 更新编辑后的数据
        setEditedSchedule({
            ...editedSchedule,
            items: tempItems
        });

        // 更新冲突列表
        setConflicts(newConflicts);

        // 如果有新冲突，显示警告
        if (newConflicts.length > 0) {
            setSnackbarMessage(`修改后存在 ${newConflicts.length} 个冲突`);
            setSnackbarOpen(true);
        }

        setDialogOpen(false);
        setSelectedItem(null);
    };

    // 处理项目字段变更
    const handleItemChange = (e) => {
        const { name, value } = e.target;
        setSelectedItem({
            ...selectedItem,
            [name]: value
        });
    };

    // 删除项目
    const handleDeleteItem = (itemId) => {
        // 过滤掉要删除的项目
        const updatedItems = editedSchedule.items.filter(item => item.scheduleId !== itemId);

        // 更新编辑后的数据
        setEditedSchedule({
            ...editedSchedule,
            items: updatedItems
        });

        // 更新冲突
        setConflicts(detectConflicts(updatedItems));

        setSnackbarMessage('项目已删除');
        setSnackbarOpen(true);
    };

    // 冲突检测函数
    const detectConflicts = (items) => {
        const conflicts = [];

        // 检查教师时间冲突
        const teacherTimeMap = new Map();

        items.forEach(item => {
            const teacherTimeKey = `${item.teacherId}-${item.timeSlotId}`;

            if (teacherTimeMap.has(teacherTimeKey)) {
                conflicts.push({
                    type: 'teacher_conflict',
                    description: `教师 "${item.teacherName}" 在时间段 "${item.dayName} ${item.startTime}-${item.endTime}" 已有课程安排`,
                    items: [teacherTimeMap.get(teacherTimeKey), item]
                });
            } else {
                teacherTimeMap.set(teacherTimeKey, item);
            }
        });

        // 检查教室时间冲突
        const classroomTimeMap = new Map();

        items.forEach(item => {
            const classroomTimeKey = `${item.classroomId}-${item.timeSlotId}`;

            if (classroomTimeMap.has(classroomTimeKey)) {
                conflicts.push({
                    type: 'classroom_conflict',
                    description: `教室 "${item.classroom}" 在时间段 "${item.dayName} ${item.startTime}-${item.endTime}" 已有课程安排`,
                    items: [classroomTimeMap.get(classroomTimeKey), item]
                });
            } else {
                classroomTimeMap.set(classroomTimeKey, item);
            }
        });

        return conflicts;
    };

    // 比较两个排课方案的差异
    const compareSchedules = (original, edited) => {
        const changes = [];

        // 检查修改的项目
        original.items.forEach(origItem => {
            const editedItem = edited.items.find(item => item.scheduleId === origItem.scheduleId);

            if (editedItem) {
                // 检查是否有字段发生变化
                if (
                    origItem.teacherId !== editedItem.teacherId ||
                    origItem.classroomId !== editedItem.classroomId ||
                    origItem.timeSlotId !== editedItem.timeSlotId
                ) {
                    changes.push({
                        type: 'modified',
                        courseInfo: `${origItem.courseCode} - ${origItem.courseName}`,
                        from: {
                            teacher: origItem.teacherName,
                            classroom: origItem.classroom,
                            time: `${origItem.dayName} ${origItem.startTime}-${origItem.endTime}`
                        },
                        to: {
                            teacher: editedItem.teacherName,
                            classroom: editedItem.classroom,
                            time: `${editedItem.dayName} ${editedItem.startTime}-${editedItem.endTime}`
                        }
                    });
                }
            } else {
                // 项目被删除
                changes.push({
                    type: 'deleted',
                    courseInfo: `${origItem.courseCode} - ${origItem.courseName}`,
                    details: {
                        teacher: origItem.teacherName,
                        classroom: origItem.classroom,
                        time: `${origItem.dayName} ${origItem.startTime}-${origItem.endTime}`
                    }
                });
            }
        });

        // 检查新增的项目
        edited.items.forEach(editedItem => {
            const exists = original.items.some(item => item.scheduleId === editedItem.scheduleId);

            if (!exists) {
                changes.push({
                    type: 'added',
                    courseInfo: `${editedItem.courseCode} - ${editedItem.courseName}`,
                    details: {
                        teacher: editedItem.teacherName,
                        classroom: editedItem.classroom,
                        time: `${editedItem.dayName} ${editedItem.startTime}-${editedItem.endTime}`
                    }
                });
            }
        });

        return changes;
    };

    if (!editedSchedule) {
        return (
            <Paper sx={{ p: 3, textAlign: 'center' }}>
                <Typography variant="h6" color="text.secondary">
                    未加载排课数据
                </Typography>
            </Paper>
        );
    }

    return (
        <Box>
            <Paper sx={{ p: 3, mb: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Typography variant="h5">
                        {isEditing ? '编辑排课方案' : '排课方案详情'} #{editedSchedule.scheduleId}
                    </Typography>
                    <Box>
                        {!isEditing ? (
                            <Button
                                variant="contained"
                                color="primary"
                                onClick={handleStartEditing}
                                disabled={editedSchedule.status !== 'Draft'}
                            >
                                编辑
                            </Button>
                        ) : (
                            <>
                                <Button
                                    variant="outlined"
                                    color="secondary"
                                    onClick={handleCancelEditing}
                                    sx={{ mr: 1 }}
                                >
                                    取消
                                </Button>
                                <Button
                                    variant="contained"
                                    color="primary"
                                    onClick={handleSaveChanges}
                                >
                                    保存
                                </Button>
                            </>
                        )}
                    </Box>
                </Box>

                {/* 冲突警告 */}
                {conflicts.length > 0 && (
                    <Alert severity="error" sx={{ mb: 2 }}>
                        <Typography variant="subtitle1">
                            检测到 {conflicts.length} 个冲突:
                        </Typography>
                        <ul>
                            {conflicts.map((conflict, index) => (
                                <li key={index}>{conflict.description}</li>
                            ))}
                        </ul>
                    </Alert>
                )}

                {/* 排课表格 */}
                <TableContainer>
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell>课程</TableCell>
                                <TableCell>教师</TableCell>
                                <TableCell>教室</TableCell>
                                <TableCell>时间</TableCell>
                                {isEditing && <TableCell>操作</TableCell>}
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {editedSchedule.items.map((item) => (
                                <TableRow
                                    key={item.scheduleId}
                                    sx={{
                                        bgcolor: conflicts.some(c =>
                                            c.items && (c.items[0].scheduleId === item.scheduleId || c.items[1]?.scheduleId === item.scheduleId)
                                        ) ? 'error.light' : 'inherit'
                                    }}
                                >
                                    <TableCell>
                                        <Typography variant="body2">{item.courseName}</Typography>
                                        <Typography variant="caption" color="text.secondary">{item.courseCode}</Typography>
                                    </TableCell>
                                    <TableCell>{item.teacherName}</TableCell>
                                    <TableCell>{item.classroom}</TableCell>
                                    <TableCell>{item.dayName} {item.startTime}-{item.endTime}</TableCell>
                                    {isEditing && (
                                        <TableCell>
                                            <IconButton size="small" onClick={() => handleOpenDialog(item)}>
                                            </IconButton>
                                            <IconButton size="small" color="error" onClick={() => handleDeleteItem(item.scheduleId)}>
                                            </IconButton>
                                        </TableCell>
                                    )}
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>
            </Paper>

            {/* 编辑历史记录 */}
            {editHistory.length > 0 && (
                <Paper sx={{ p: 3, mb: 3 }}>
                    <Typography variant="h6" gutterBottom>
                        编辑历史
                    </Typography>
                    <Divider sx={{ mb: 2 }} />

                    {editHistory.map((entry, index) => (
                        <Accordion key={index} sx={{ mb: 1 }}>
                            <AccordionSummary >
                                <Typography>
                                    {new Date(entry.timestamp).toLocaleString()} - {entry.description}
                                </Typography>
                            </AccordionSummary>
                            <AccordionDetails>
                                <Typography variant="subtitle2" gutterBottom>修改详情：</Typography>
                                <Grid container spacing={2}>
                                    {entry.changes.map((change, changeIndex) => (
                                        <Grid item xs={12} key={changeIndex}>
                                            <Paper variant="outlined" sx={{ p: 2 }}>
                                                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                                                    <Chip
                                                        size="small"
                                                        label={change.type === 'modified' ? '已修改' : change.type === 'added' ? '新增' : '删除'}
                                                        color={change.type === 'modified' ? 'primary' : change.type === 'added' ? 'success' : 'error'}
                                                        sx={{ mr: 1 }}
                                                    />
                                                    <Typography variant="body1">{change.courseInfo}</Typography>
                                                </Box>

                                                {change.type === 'modified' ? (
                                                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                                                        <Box sx={{ flex: 1 }}>
                                                            <Typography variant="body2">从：</Typography>
                                                            <Typography variant="body2">教师: {change.from.teacher}</Typography>
                                                            <Typography variant="body2">教室: {change.from.classroom}</Typography>
                                                            <Typography variant="body2">时间: {change.from.time}</Typography>
                                                        </Box>
                                                        <Box sx={{ flex: 1 }}>
                                                            <Typography variant="body2">到：</Typography>
                                                            <Typography variant="body2">教师: {change.to.teacher}</Typography>
                                                            <Typography variant="body2">教室: {change.to.classroom}</Typography>
                                                            <Typography variant="body2">时间: {change.to.time}</Typography>
                                                        </Box>
                                                    </Box>
                                                ) : (
                                                    <Box>
                                                        <Typography variant="body2">教师: {change.details.teacher}</Typography>
                                                        <Typography variant="body2">教室: {change.details.classroom}</Typography>
                                                        <Typography variant="body2">时间: {change.details.time}</Typography>
                                                    </Box>
                                                )}
                                            </Paper>
                                        </Grid>
                                    ))}
                                </Grid>
                            </AccordionDetails>
                        </Accordion>
                    ))}
                </Paper>
            )}

            {/* 编辑对话框 */}
            <Dialog open={dialogOpen} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
                <DialogTitle>编辑课程安排</DialogTitle>
                <DialogContent>
                    <Box sx={{ p: 1 }}>
                        <Typography variant="subtitle1" gutterBottom>
                            {selectedItem?.courseCode} - {selectedItem?.courseName}
                        </Typography>

                        <Grid container spacing={2} sx={{ mt: 1 }}>
                            <Grid item xs={12}>
                                <FormControl fullWidth margin="normal">
                                    <InputLabel id="teacher-select-label">教师</InputLabel>
                                    <Select
                                        labelId="teacher-select-label"
                                        name="teacherId"
                                        value={selectedItem?.teacherId || ''}
                                        onChange={handleItemChange}
                                        label="教师"
                                    >
                                        {availableTeachers.map((teacher) => (
                                            <MenuItem key={teacher.teacherId} value={teacher.teacherId}>
                                                {teacher.name}
                                            </MenuItem>
                                        ))}
                                    </Select>
                                </FormControl>
                            </Grid>

                            <Grid item xs={12}>
                                <FormControl fullWidth margin="normal">
                                    <InputLabel id="classroom-select-label">教室</InputLabel>
                                    <Select
                                        labelId="classroom-select-label"
                                        name="classroomId"
                                        value={selectedItem?.classroomId || ''}
                                        onChange={handleItemChange}
                                        label="教室"
                                    >
                                        {availableClassrooms.map((classroom) => (
                                            <MenuItem key={classroom.classroomId} value={classroom.classroomId}>
                                                {classroom.building} - {classroom.name} (容量: {classroom.capacity})
                                            </MenuItem>
                                        ))}
                                    </Select>
                                </FormControl>
                            </Grid>

                            <Grid item xs={12}>
                                <FormControl fullWidth margin="normal">
                                    <InputLabel id="timeslot-select-label">时间段</InputLabel>
                                    <Select
                                        labelId="timeslot-select-label"
                                        name="timeSlotId"
                                        value={selectedItem?.timeSlotId || ''}
                                        onChange={handleItemChange}
                                        label="时间段"
                                    >
                                        {availableTimeSlots.map((timeSlot) => (
                                            <MenuItem key={timeSlot.timeSlotId} value={timeSlot.timeSlotId}>
                                                {timeSlot.dayName} {timeSlot.startTime}-{timeSlot.endTime}
                                            </MenuItem>
                                        ))}
                                    </Select>
                                </FormControl>
                            </Grid>
                        </Grid>
                    </Box>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCloseDialog} color="inherit">取消</Button>
                    <Button onClick={handleSaveItem} color="primary" variant="contained">保存</Button>
                </DialogActions>
            </Dialog>

            {/* 消息提示 */}
            <Snackbar
                open={snackbarOpen}
                autoHideDuration={6000}
                onClose={() => setSnackbarOpen(false)}
                message={snackbarMessage}
            />
        </Box>
    );
};

export default ScheduleEditor;
