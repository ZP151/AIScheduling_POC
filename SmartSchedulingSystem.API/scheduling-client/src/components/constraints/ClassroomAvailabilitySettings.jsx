import React, { useState, useEffect } from 'react';
import { 
  Box, 
  Typography, 
  FormControl, 
  InputLabel, 
  Select, 
  MenuItem, 
  Paper, 
  Table, 
  TableBody, 
  TableCell, 
  TableContainer, 
  TableHead, 
  TableRow, 
  Switch,
  Tab,
  Tabs,
  TextField,
  Button,
  Grid,
  FormControlLabel,
  Chip,
  IconButton,
  Tooltip
} from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';

const ClassroomAvailabilitySettings = ({ classrooms, availabilitySettings: externalAvailabilitySettings, semesterId, onUpdate }) => {
  // 使用本地状态来管理可用性设置
  const [localAvailability, setLocalAvailability] = useState({});
  
  const [weekMode, setWeekMode] = useState('regular');
  const [weekSets, setWeekSets] = useState([
    { id: 1, name: 'Regular Weeks', weeks: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14] },
    { id: 2, name: 'Exam Weeks', weeks: [15, 16] },
    { id: 3, name: 'Special Weeks', weeks: [5, 6] },
  ]);
  const [selectedWeekSet, setSelectedWeekSet] = useState(1);
  const [selectedClassroom, setSelectedClassroom] = useState(null);
  const [tabValue, setTabValue] = useState(0); // 0: Daily, 1: Weekly, 2: Date Range
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [unavailabilityReason, setUnavailabilityReason] = useState('Maintenance');
  
  // 同步外部和本地状态
  useEffect(() => {
    setLocalAvailability(externalAvailabilitySettings || {});
  }, [externalAvailabilitySettings]);
  
  // 添加调试用的useEffect
  useEffect(() => {
    console.log('Component initialized, current availability settings:', localAvailability);
  }, []);

  useEffect(() => {
    if (selectedClassroom) {
      console.log(`Selected classroom ${selectedClassroom}, availability settings:`, 
        localAvailability[selectedClassroom] || 'No availability settings');
    }
  }, [selectedClassroom, localAvailability]);
  
  // 确保在组件加载和选择教室时，如果没有可用性设置，自动初始化
  useEffect(() => {
    if (selectedClassroom && (!localAvailability[selectedClassroom] || Object.keys(localAvailability[selectedClassroom] || {}).length === 0)) {
      console.log(`Classroom ${selectedClassroom} has no availability settings, auto initializing...`);
      initializeAvailability(selectedClassroom);
    }
  }, [selectedClassroom, localAvailability]);
  
  const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  const timeSlots = ['08:00-10:00', '10:00-12:00', '14:00-16:00', '16:00-18:00', '19:00-21:00'];
  
  const handleClassroomChange = (event) => {
    const newClassroomId = event.target.value;
    setSelectedClassroom(newClassroomId);
    
    // 选择教室后，如果该教室没有设置过可用性，自动初始化所有时间段为可用
    if (newClassroomId && (!localAvailability[newClassroomId] || Object.keys(localAvailability[newClassroomId]).length === 0)) {
      initializeAvailability(newClassroomId);
    }
  };
  
  const handleTabChange = (event, newValue) => {
    setTabValue(newValue);
  };

  // Function to handle week selection in a week set
  const handleWeekSelection = (weekNumber, selected) => {
    setWeekSets(prev => prev.map(set => 
      set.id === selectedWeekSet
        ? {
            ...set,
            weeks: selected 
              ? [...set.weeks, weekNumber].sort((a, b) => a - b)
              : set.weeks.filter(w => w !== weekNumber)
          } 
        : set
    ));
  };

  // Function to handle week set name change
  const handleWeekSetNameChange = (e) => {
    setWeekSets(prev => prev.map(set => 
      set.id === selectedWeekSet
        ? { ...set, name: e.target.value }
        : set
    ));
  };

  const handleAvailabilityChange = (day, timeSlot, isAvailable) => {
    const classroomId = selectedClassroom;
    // 使用函数式更新以确保基于最新状态进行更新
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    if (!newSettings[classroomId]) {
      newSettings[classroomId] = {};
    }
    
    if (!newSettings[classroomId][day]) {
      newSettings[classroomId][day] = {};
    }
    
    // 确保设置为明确的布尔值
    newSettings[classroomId][day][timeSlot] = Boolean(isAvailable);
    
    // 添加调试日志
    console.log(`Setting ${day} ${timeSlot} to ${isAvailable ? 'available' : 'unavailable'}`);
    
    // 更新本地状态
    setLocalAvailability(newSettings);
    
    // 调用父组件提供的更新函数
    if (onUpdate) {
      onUpdate(newSettings);
    }
  };

  // 设置某一天所有时间段的可用性
  const handleDayAvailabilityChange = (day, isAvailable) => {
    const classroomId = selectedClassroom;
    // 创建深拷贝而不是浅拷贝
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    if (!newSettings[classroomId]) {
      newSettings[classroomId] = {};
    }
    
    if (!newSettings[classroomId][day]) {
      newSettings[classroomId][day] = {};
    }
    
    // 设置该天所有时间段
    timeSlots.forEach(slot => {
      newSettings[classroomId][day][slot] = isAvailable;
    });
    
    // 添加调试日志
    console.log(`Setting all time slots for ${day} to ${isAvailable ? 'available' : 'unavailable'}`);
    
    // 更新本地状态
    setLocalAvailability(newSettings);
    
    // 调用父组件提供的更新函数
    if (onUpdate) {
      onUpdate(newSettings);
    }
  };

  // 设置某一时间段所有天的可用性
  const handleTimeSlotAvailabilityChange = (timeSlot, isAvailable) => {
    const classroomId = selectedClassroom;
    // 创建深拷贝而不是浅拷贝
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    if (!newSettings[classroomId]) {
      newSettings[classroomId] = {};
    }
    
    // 设置该时间段所有天
    days.forEach(day => {
      if (!newSettings[classroomId][day]) {
        newSettings[classroomId][day] = {};
      }
      newSettings[classroomId][day][timeSlot] = isAvailable;
    });
    
    // 添加调试日志
    console.log(`Setting ${timeSlot} time slot for all days to ${isAvailable ? 'available' : 'unavailable'}`);
    
    // 更新本地状态
    setLocalAvailability(newSettings);
    
    // 调用父组件提供的更新函数
    if (onUpdate) {
      onUpdate(newSettings);
    }
  };
  
  // 初始化教室的可用性设置（默认全部可用）
  const initializeAvailability = (classroomId) => {
    const newSettings = JSON.parse(JSON.stringify(localAvailability || {}));
    
    newSettings[classroomId] = {};
    
    // 设置所有日期和时间段为可用
    days.forEach(day => {
      newSettings[classroomId][day] = {};
      timeSlots.forEach(slot => {
        newSettings[classroomId][day][slot] = true; // 默认设置为可用
      });
    });
    
    console.log(`Initializing classroom ${classroomId} availability to all available`);
    
    // 更新本地状态
    setLocalAvailability(newSettings);
    
    // 调用父组件提供的更新函数
    if (onUpdate) {
      onUpdate(newSettings);
    }
  };

  // 这将确保保存按钮能基于本地状态正确工作
  const handleSaveAvailability = () => {
    if (selectedClassroom) {
      // 在真实应用中，这里会保存到后端
      alert(`Saved availability settings for classroom ${classrooms.find(c => c.id === selectedClassroom)?.name} with ${weekSets.length} different week patterns`);
      
      // 如果需要，可以在这里调用特殊的保存函数
      if (onUpdate) {
        onUpdate(localAvailability);
      }
    }
  };
  
  const handleAddDateRangeException = () => {
    if (!selectedClassroom || !startDate || !endDate) return;
    
    // 实际应用中，这里会添加日期范围例外到后端
    console.log(`Adding date range exception for classroom ${selectedClassroom} from ${startDate} to ${endDate}`);
    
    // 清空表单字段
    setStartDate('');
    setEndDate('');
  };
  
  return (
    <Box>
      <Grid container spacing={2} sx={{ mb: 2 }}>
        <Grid item xs={12} md={6}>
          <FormControl fullWidth>
            <InputLabel>Select Classroom</InputLabel>
            <Select
              value={selectedClassroom || ''}
              onChange={handleClassroomChange}
              label="Select Classroom"
            >
              {classrooms.map(classroom => (
                <MenuItem key={classroom.id} value={classroom.id}>
                  {classroom.building}-{classroom.name} (Capacity: {classroom.capacity})
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Grid>
      </Grid>
      
      {selectedClassroom && (
        <>
          <Box sx={{ mb: 3 }}>
            <FormControl fullWidth>
              <InputLabel>Week Pattern</InputLabel>
              <Select
                value={selectedWeekSet}
                onChange={(e) => setSelectedWeekSet(e.target.value)}
                label="Week Pattern"
              >
                {weekSets.map(set => (
                  <MenuItem key={set.id} value={set.id}>
                    {set.name} (Weeks: {set.weeks.join(', ')})
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <Button
              variant="outlined"
              size="small"
              sx={{ mt: 1 }}
              onClick={() => {
                // Logic to create a new week set
                const newSetId = Math.max(...weekSets.map(s => s.id)) + 1;
                setWeekSets([...weekSets, { id: newSetId, name: `New Week Set ${newSetId}`, weeks: [] }]);
                setSelectedWeekSet(newSetId);
              }}
            >
              Create New Week Pattern
            </Button>
          </Box>

          {selectedWeekSet && (
            <Box sx={{ mt: 2, p: 2, border: '1px solid #e0e0e0', borderRadius: 1 }}>
              <Grid container spacing={2} alignItems="center">
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Week Set Name"
                    value={weekSets.find(s => s.id === selectedWeekSet)?.name || ''}
                    onChange={handleWeekSetNameChange}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                    <Button
                      variant="outlined"
                      color="error"
                      size="small"
                      onClick={() => {
                        if (weekSets.length > 1) {
                          setWeekSets(prev => prev.filter(s => s.id !== selectedWeekSet));
                          setSelectedWeekSet(weekSets[0].id);
                        }
                      }}
                      disabled={weekSets.length <= 1}
                    >
                      Delete Week Set
                    </Button>
                  </Box>
                </Grid>
                <Grid item xs={12}>
                  <Typography variant="subtitle2" gutterBottom>
                    Select Weeks in Set
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {Array.from({ length: 16 }, (_, i) => i + 1).map(week => {
                      const isSelected = weekSets.find(s => s.id === selectedWeekSet)?.weeks.includes(week) || false;
                      return (
                        <Chip
                          key={week}
                          label={`Week ${week}`}
                          color={isSelected ? 'primary' : 'default'}
                          variant={isSelected ? 'filled' : 'outlined'}
                          onClick={() => handleWeekSelection(week, !isSelected)}
                          sx={{ cursor: 'pointer' }}
                        />
                      );
                    })}
                  </Box>
                </Grid>
              </Grid>
            </Box>
          )}

          <Paper sx={{ mb: 2 }}>
            <Tabs value={tabValue} onChange={handleTabChange} centered>
              <Tab label="Daily Schedule" />
              <Tab label="Weekly Pattern" />
              <Tab label="Date Range Exceptions" />
            </Tabs>
          </Paper>
          
          {/* Daily Schedule Tab - 日历视图 */}
          {tabValue === 0 && (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell></TableCell>
                    {days.map((day, index) => (
                      <TableCell key={day} align="center">
                        <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                          <Typography variant="subtitle2">{day}</Typography>
                          <Tooltip title={`Toggle all time slots for ${day}`}>
                            <Button 
                              variant="outlined" 
                              size="small"
                              sx={{ mt: 1, minWidth: '90px' }}
                              onClick={() => {
                                const classroomSettings = localAvailability[selectedClassroom] || {};
                                const daySettings = classroomSettings[day] || {};
                                // 检查当前状态 - 如果大部分是可用的则切换为不可用，反之亦然
                                const availableCount = timeSlots.filter(slot => 
                                  daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined)
                                ).length;
                                const isMainlyAvailable = availableCount > timeSlots.length / 2;
                                handleDayAvailabilityChange(day, !isMainlyAvailable);
                              }}
                            >
                              {/* 根据这一天的整体可用性状态显示不同的标签 */}
                              {(() => {
                                const classroomSettings = localAvailability[selectedClassroom] || {};
                                const daySettings = classroomSettings[day] || {};
                                const availableCount = timeSlots.filter(slot => 
                                  daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined)
                                ).length;
                                if (availableCount === 0) return 'All Off';
                                if (availableCount === timeSlots.length) return 'All On';
                                return `${availableCount}/${timeSlots.length}`;
                              })()}
                            </Button>
                          </Tooltip>
                        </Box>
                      </TableCell>
                    ))}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {timeSlots.map(slot => {
                    const [startTime, endTime] = slot.split('-');
                    return (
                      <TableRow key={slot}>
                        <TableCell component="th" scope="row" sx={{ whiteSpace: 'nowrap' }}>
                          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start' }}>
                            <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                              {startTime}-{endTime}
                            </Typography>
                            <Tooltip title="Toggle this time slot for all days">
                              <Button 
                                variant="outlined" 
                                size="small"
                                sx={{ mt: 1, minWidth: '90px' }}
                                onClick={() => {
                                  const classroomSettings = localAvailability[selectedClassroom] || {};
                                  // 检查当前状态 - 如果大部分是可用的则切换为不可用，反之亦然
                                  const availableCount = days.filter(day => {
                                    const daySettings = classroomSettings[day] || {};
                                    return daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined);
                                  }).length;
                                  const isMainlyAvailable = availableCount > days.length / 2;
                                  handleTimeSlotAvailabilityChange(slot, !isMainlyAvailable);
                                }}
                              >
                                {/* 根据这一时间段的整体可用性状态显示不同的标签 */}
                                {(() => {
                                  const classroomSettings = localAvailability[selectedClassroom] || {};
                                  const availableCount = days.filter(day => {
                                    const daySettings = classroomSettings[day] || {};
                                    return daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined);
                                  }).length;
                                  if (availableCount === 0) return 'All Off';
                                  if (availableCount === days.length) return 'All On';
                                  return `${availableCount}/${days.length}`;
                                })()}
                              </Button>
                            </Tooltip>
                          </Box>
                        </TableCell>
                        {days.map(day => {
                          const classroomSettings = localAvailability[selectedClassroom] || {};
                          const daySettings = classroomSettings[day] || {};
                          const isAvailable = daySettings[slot] === true || (daySettings[slot] !== false && daySettings[slot] !== undefined); // 修改可用性判断逻辑
                          
                          return (
                            <TableCell 
                              key={`${day}-${slot}`} 
                              align="center" 
                              sx={{ 
                                height: 70, 
                                minWidth: 100,
                                backgroundColor: isAvailable ? 'rgba(76, 175, 80, 0.1)' : 'rgba(244, 67, 54, 0.1)',
                                '&:hover': {
                                  backgroundColor: isAvailable ? 'rgba(76, 175, 80, 0.2)' : 'rgba(244, 67, 54, 0.2)',
                                },
                                cursor: 'pointer',
                                transition: 'background-color 0.2s'
                              }}
                              onClick={() => handleAvailabilityChange(day, slot, !isAvailable)}
                            >
                              <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
                                {isAvailable ? (
                                  <CheckCircleIcon color="success" sx={{ fontSize: 28 }} />
                                ) : (
                                  <CancelIcon color="error" sx={{ fontSize: 28 }} />
                                )}
                              </Box>
                            </TableCell>
                          );
                        })}
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>
          )}
          
          {/* Weekly Pattern Tab */}
          {tabValue === 1 && (
            <Paper variant="outlined" sx={{ p: 2 }}>
              <Typography variant="body2" sx={{ mb: 2 }}>
                Set recurring availability patterns for the entire semester.
              </Typography>
              
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
                {days.map(day => (
                  <FormControlLabel
                    key={day}
                    control={
                      <Switch
                        checked={
                          localAvailability[selectedClassroom] &&
                          localAvailability[selectedClassroom][day] &&
                          Object.values(localAvailability[selectedClassroom][day]).some(v => v)
                        }
                        onChange={(e) => {
                          // In a real app, this would set all time slots for this day
                          timeSlots.forEach(slot => {
                            handleAvailabilityChange(day, slot, e.target.checked);
                          });
                        }}
                      />
                    }
                    label={day}
                  />
                ))}
              </Box>
              
              <Typography variant="subtitle2" gutterBottom>
                Hours of Operation
              </Typography>
              
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth size="small">
                    <InputLabel>Open Time</InputLabel>
                    <Select
                      value="08:00"
                      label="Open Time"
                    >
                      <MenuItem value="08:00">8:00 AM</MenuItem>
                      <MenuItem value="10:00">10:00 AM</MenuItem>
                      <MenuItem value="14:00">2:00 PM</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth size="small">
                    <InputLabel>Close Time</InputLabel>
                    <Select
                      value="18:00"
                      label="Close Time"
                    >
                      <MenuItem value="16:00">4:00 PM</MenuItem>
                      <MenuItem value="18:00">6:00 PM</MenuItem>
                      <MenuItem value="21:00">9:00 PM</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
              </Grid>
              
              <Typography variant="subtitle2" sx={{ mt: 2 }} gutterBottom>
                Recurring Usage Times
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {['Faculty Meetings (Mon 12:00-14:00)', 'Maintenance (Fri 16:00-18:00)'].map(timeBlock => (
                  <Chip 
                    key={timeBlock} 
                    label={timeBlock} 
                    onDelete={() => {}} 
                    color="primary" 
                    variant="outlined"
                  />
                ))}
                <Chip 
                  label="+ Add Time Block" 
                  onClick={() => {}} 
                  color="primary" 
                  variant="outlined"
                />
              </Box>
            </Paper>
          )}
          
          {/* Date Range Exceptions Tab */}
          {tabValue === 2 && (
            <Paper variant="outlined" sx={{ p: 2 }}>
              <Typography variant="body2" sx={{ mb: 2 }}>
                Set specific date ranges when this classroom is unavailable (e.g., for maintenance, events, exams).
              </Typography>
              
              <Grid container spacing={2}>
                <Grid item xs={12} md={4}>
                  <TextField
                    fullWidth
                    label="Start Date"
                    type="date"
                    value={startDate}
                    onChange={(e) => setStartDate(e.target.value)}
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>
                <Grid item xs={12} md={4}>
                  <TextField
                    fullWidth
                    label="End Date"
                    type="date"
                    value={endDate}
                    onChange={(e) => setEndDate(e.target.value)}
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>
                <Grid item xs={12} md={4}>
                  <FormControl fullWidth>
                    <InputLabel>Reason</InputLabel>
                    <Select
                      value={unavailabilityReason}
                      onChange={(e) => setUnavailabilityReason(e.target.value)}
                      label="Reason"
                    >
                      <MenuItem value="Maintenance">Maintenance</MenuItem>
                      <MenuItem value="Event">Special Event</MenuItem>
                      <MenuItem value="Exam">Exam</MenuItem>
                      <MenuItem value="Renovation">Renovation</MenuItem>
                      <MenuItem value="Other">Other</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12}>
                  <Button 
                    variant="contained" 
                    color="primary"
                    onClick={handleAddDateRangeException}
                    disabled={!startDate || !endDate}
                  >
                    Add Exception
                  </Button>
                </Grid>
              </Grid>
              
              <Typography variant="subtitle2" sx={{ mt: 3, mb: 1 }}>
                Unavailable Date Periods
              </Typography>
              
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Start Date</TableCell>
                      <TableCell>End Date</TableCell>
                      <TableCell>Reason</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    <TableRow>
                      <TableCell>2024-10-15</TableCell>
                      <TableCell>2024-10-20</TableCell>
                      <TableCell>Maintenance</TableCell>
                      <TableCell>
                        <Button size="small" color="error">
                          Delete
                        </Button>
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>2024-12-24</TableCell>
                      <TableCell>2025-01-05</TableCell>
                      <TableCell>Holiday Closure</TableCell>
                      <TableCell>
                        <Button size="small" color="error">
                          Delete
                        </Button>
                      </TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            </Paper>
          )}
        </>
      )}
      <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end' }}>
        <Button
          variant="contained"
          color="primary"
          onClick={handleSaveAvailability}
          disabled={!selectedClassroom}
        >
          Save Classroom Availability
        </Button>
      </Box>
    </Box>
  );
};

export default ClassroomAvailabilitySettings; 