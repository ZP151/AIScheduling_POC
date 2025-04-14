import React, { useState, useEffect } from 'react';
import { 
  Box, 
  Typography, 
  TableContainer, 
  Table, 
  TableHead, 
  TableBody, 
  TableRow, 
  TableCell, 
  Paper, 
  Chip, 
  Button,
  Grid,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Card,
  CardContent,
  CircularProgress
} from '@mui/material';
import { mockSemesters } from '../services/mockData';
import { getScheduleHistory, publishSchedule, cancelSchedule } from '../services/api';

const ScheduleHistory = ({ onHistoryItemClick }) => {
  const [filters, setFilters] = useState({
    startDate: '',
    endDate: '',
    semester: '',
    status: '',
    searchTerm: '',
    searchBy: 'all'  // 'all', 'course', 'teacher', 'classroom'
  });

  const [schedules, setSchedules] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // 加载排课历史数据
  useEffect(() => {
    fetchScheduleHistory();
  }, []);

  const fetchScheduleHistory = async () => {
    setLoading(true);
    setError(null);
    try {
      // 暂时使用默认的semesterId=1
      const semesterId = 1;
      const results = await getScheduleHistory(semesterId);
      setSchedules(results);
    } catch (err) {
      console.error('获取排课历史记录失败:', err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleString();
  };

  const handleViewSchedule = (scheduleId) => {
    if (onHistoryItemClick) {
      onHistoryItemClick(scheduleId);
    }
  };

  const handlePublishSchedule = async (scheduleId) => {
    try {
      await publishSchedule(scheduleId);
      // 刷新数据
      fetchScheduleHistory();
    } catch (error) {
      alert(`发布排课方案失败: ${error.message}`);
    }
  };

  const handleCancelSchedule = async (scheduleId) => {
    try {
      await cancelSchedule(scheduleId);
      // 刷新数据
      fetchScheduleHistory();
    } catch (error) {
      alert(`取消排课方案失败: ${error.message}`);
    }
  };

  // 添加处理筛选变更的函数
  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters({
      ...filters,
      [name]: value
    });
  };

  // 筛选函数
  const filterSchedules = () => {
    return schedules.filter(schedule => {
      // 1. 筛选状态
      if (filters.status && schedule.status !== filters.status) return false;
      
      // 2. 筛选学期
      if (filters.semester && schedule.semesterName !== filters.semester) return false;
      
      // 3. 筛选日期范围
      if (filters.startDate) {
        const startDate = new Date(filters.startDate);
        const scheduleDate = new Date(schedule.createdAt);
        if (scheduleDate < startDate) return false;
      }
      
      if (filters.endDate) {
        const endDate = new Date(filters.endDate);
        endDate.setHours(23, 59, 59); // 设置为当天结束时间
        const scheduleDate = new Date(schedule.createdAt);
        if (scheduleDate > endDate) return false;
      }
      
      // 4. 筛选搜索词
      if (filters.searchTerm) {
        const searchTerm = filters.searchTerm.toLowerCase();
        
        // 根据搜索类型不同，搜索不同的字段
        switch (filters.searchBy) {
          case 'name':
            return schedule.name.toLowerCase().includes(searchTerm);
            
          case 'course':
            // 搜索课程代码或名称
            return schedule.details && schedule.details.some(item => 
              item.courseCode.toLowerCase().includes(searchTerm) || 
              item.courseName.toLowerCase().includes(searchTerm)
            );
            
          case 'teacher':
            // 搜索教师名称
            return schedule.details && schedule.details.some(item => 
              item.teacherName.toLowerCase().includes(searchTerm)
            );
            
          case 'classroom':
            // 搜索教室信息
            return schedule.details && schedule.details.some(item => 
              item.classroom.toLowerCase().includes(searchTerm)
            );
            
          case 'all':
          default:
            // 搜索多个字段
            return (
              schedule.name.toLowerCase().includes(searchTerm) ||
              (schedule.details && schedule.details.some(item => 
                item.courseCode.toLowerCase().includes(searchTerm) || 
                item.courseName.toLowerCase().includes(searchTerm) ||
                item.teacherName.toLowerCase().includes(searchTerm) ||
                item.classroom.toLowerCase().includes(searchTerm)
              ))
            );
        }
      }
      
      // 通过所有筛选条件
      return true;
    });
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        排课历史记录
      </Typography>
      
      {/* 搜索和筛选面板 */}
      <Paper sx={{ p: 2, mb: 2 }}>
        <Grid container spacing={2}>
          <Grid item xs={12}>
            <Typography variant="subtitle1">搜索和筛选</Typography>
          </Grid>
          
          {/* 搜索框 */}
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="搜索"
              name="searchTerm"
              value={filters.searchTerm}
              onChange={handleFilterChange}
              placeholder="按名称、课程、教师或教室搜索"
            />
          </Grid>
          
          {/* 搜索类型选择 */}
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>搜索类型</InputLabel>
              <Select
                name="searchBy"
                value={filters.searchBy}
                onChange={handleFilterChange}
                label="搜索类型"
              >
                <MenuItem value="all">所有字段</MenuItem>
                <MenuItem value="name">排课名称</MenuItem>
                <MenuItem value="course">课程</MenuItem>
                <MenuItem value="teacher">教师</MenuItem>
                <MenuItem value="classroom">教室</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          
          {/* 日期范围筛选 - 使用标准HTML日期输入 */}
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="开始日期"
              type="date"
              name="startDate"
              value={filters.startDate}
              onChange={handleFilterChange}
              InputLabelProps={{ shrink: true }}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="结束日期"
              type="date"
              name="endDate"
              value={filters.endDate}
              onChange={handleFilterChange}
              InputLabelProps={{ shrink: true }}
            />
          </Grid>
          
          {/* 学期筛选 */}
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>学期</InputLabel>
              <Select
                name="semester"
                value={filters.semester}
                onChange={handleFilterChange}
                label="学期"
              >
                <MenuItem value="">所有学期</MenuItem>
                {mockSemesters.map(semester => (
                  <MenuItem key={semester.id} value={semester.name}>
                    {semester.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          
          {/* 状态筛选 */}
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>状态</InputLabel>
              <Select
                name="status"
                value={filters.status}
                onChange={handleFilterChange}
                label="状态"
              >
                <MenuItem value="">所有状态</MenuItem>
                <MenuItem value="Draft">草稿</MenuItem>
                <MenuItem value="Published">已发布</MenuItem>
                <MenuItem value="Cancelled">已取消</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          
          {/* 重置按钮 */}
          <Grid item xs={12}>
            <Button 
              variant="outlined" 
              onClick={() => setFilters({
                startDate: '',
                endDate: '',
                semester: '',
                status: '',
                searchTerm: '',
                searchBy: 'all'
              })}
            >
              重置筛选
            </Button>
          </Grid>
        </Grid>
      </Paper>
      
      {/* 加载状态显示 */}
      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', my: 3 }}>
          <CircularProgress />
        </Box>
      )}
      
      {/* 错误信息显示 */}
      {error && (
        <Box sx={{ mb: 2, p: 2, bgcolor: 'error.light', borderRadius: 1 }}>
          <Typography color="error">
            加载排课历史记录失败: {error}
          </Typography>
        </Box>
      )}
      
      {/* 筛选结果列表 */}
      {!loading && !error && (
        <TableContainer component={Paper} variant="outlined" sx={{ mt: 2 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>排课名称</TableCell>
                <TableCell>创建日期</TableCell>
                <TableCell>状态</TableCell>
                <TableCell>学期</TableCell>
                <TableCell>课程数</TableCell>
                <TableCell>操作</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filterSchedules().map((schedule) => (
                <TableRow key={schedule.id}>
                  <TableCell>{schedule.name}</TableCell>
                  <TableCell>{formatDate(schedule.createdAt)}</TableCell>
                  <TableCell>
                    <Chip 
                      label={schedule.status === 'Draft' ? '草稿' : 
                            schedule.status === 'Published' ? '已发布' : '已取消'} 
                      color={schedule.status === 'Published' ? 'success' : 
                            schedule.status === 'Draft' ? 'warning' : 'error'} 
                      size="small" 
                      variant="outlined" 
                    />
                  </TableCell>
                  <TableCell>{schedule.semesterName || '未知'}</TableCell>
                  <TableCell>
                    {schedule.details && (
                      `${schedule.details.length} 门课程`
                    )}
                  </TableCell>
                  <TableCell>
                    <Box sx={{ display: 'flex', gap: 1 }}>
                      <Button 
                        variant="contained" 
                        size="small" 
                        onClick={() => handleViewSchedule(schedule.id)}
                      >
                        查看
                      </Button>
                      
                      {schedule.status === 'Draft' && (
                        <Button 
                          variant="outlined" 
                          size="small" 
                          color="success"
                          onClick={() => handlePublishSchedule(schedule.id)}
                        >
                          发布
                        </Button>
                      )}
                      
                      {schedule.status !== 'Cancelled' && (
                        <Button 
                          variant="outlined" 
                          size="small" 
                          color="error"
                          onClick={() => handleCancelSchedule(schedule.id)}
                        >
                          取消
                        </Button>
                      )}
                    </Box>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
      
      {!loading && !error && filterSchedules().length === 0 && (
        <Box sx={{ textAlign: 'center', py: 4 }}>
          <Typography variant="body1" color="text.secondary">
            未找到匹配的排课记录。请尝试调整筛选条件。
          </Typography>
        </Box>
      )}
      
      {/* 分析报告段落 */}
      <Box sx={{ mt: 4 }}>
        <Typography variant="h6" gutterBottom>
          使用报告
        </Typography>
        <Grid container spacing={2}>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle1" gutterBottom>
                  教室利用分析
                </Typography>
                <Button variant="contained" fullWidth>生成报告</Button>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle1" gutterBottom>
                  教师工作负荷分析
                </Typography>
                <Button variant="contained" fullWidth>生成报告</Button>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle1" gutterBottom>
                  课程需求趋势
                </Typography>
                <Button variant="contained" fullWidth>生成报告</Button>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </Box>
    </Box>
  );
};

export default ScheduleHistory;