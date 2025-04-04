import React, { useState } from 'react';
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
  CardContent
} from '@mui/material';
import { mockScheduleResults, mockSemesters } from '../services/mockData';

const ScheduleHistory = ({ onHistoryItemClick }) => {
  const [filters, setFilters] = useState({
    startDate: '',
    endDate: '',
    semester: '',
    status: '',
    searchTerm: '',
    searchBy: 'all'  // 'all', 'course', 'teacher', 'classroom'
  });

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleString();
  };

  const handleViewSchedule = (scheduleId) => {
    if (onHistoryItemClick) {
      onHistoryItemClick(scheduleId);
    }
  };

  const handlePublishSchedule = (scheduleId) => {
    // 在实际应用中，这里应该是一个 API 调用
    alert(`Publishing schedule ID: ${scheduleId}`);
    // 然后刷新数据
  };

  const handleCancelSchedule = (scheduleId) => {
    // 在实际应用中，这里应该是一个 API 调用
    alert(`Cancelling schedule ID: ${scheduleId}`);
    // 然后刷新数据
  };

  // 添加处理筛选变更的函数
  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters({
      ...filters,
      [name]: value
    });
  };

  // 添加筛选函数
  const filterSchedules = () => {
    return mockScheduleResults.filter(schedule => {
      // 日期筛选
      if (filters.startDate && new Date(schedule.createdAt) < new Date(filters.startDate)) {
        return false;
      }
      if (filters.endDate && new Date(schedule.createdAt) > new Date(filters.endDate)) {
        return false;
      }
      
      // 状态筛选
      if (filters.status && schedule.status !== filters.status) {
        return false;
      }
      
      // 学期筛选 (假设每个schedule都有semesterName属性)
      if (filters.semester && schedule.semesterName !== filters.semester) {
        return false;
      }
      
      // 搜索词筛选
      if (filters.searchTerm) {
        const term = filters.searchTerm.toLowerCase();
        
        if (filters.searchBy === 'all' || filters.searchBy === 'name') {
          if (schedule.name.toLowerCase().includes(term)) {
            return true;
          }
        }
        
        if (filters.searchBy === 'all' || filters.searchBy === 'course') {
          if (schedule.details && schedule.details.some(
            detail => detail.courseCode.toLowerCase().includes(term) || 
                     detail.courseName.toLowerCase().includes(term)
          )) {
            return true;
          }
        }
        
        if (filters.searchBy === 'all' || filters.searchBy === 'teacher') {
          if (schedule.details && schedule.details.some(
            detail => detail.teacherName.toLowerCase().includes(term)
          )) {
            return true;
          }
        }
        
        if (filters.searchBy === 'all' || filters.searchBy === 'classroom') {
          if (schedule.details && schedule.details.some(
            detail => detail.classroom.toLowerCase().includes(term)
          )) {
            return true;
          }
        }
        
        return false;
      }
      
      return true;
    });
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        Schedule History
      </Typography>
      
      {/* 搜索和筛选面板 */}
      <Paper sx={{ p: 2, mb: 2 }}>
        <Grid container spacing={2}>
          <Grid item xs={12}>
            <Typography variant="subtitle1">Search and Filter</Typography>
          </Grid>
          
          {/* 搜索框 */}
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Search"
              name="searchTerm"
              value={filters.searchTerm}
              onChange={handleFilterChange}
              placeholder="Search by name, course, teacher or classroom"
            />
          </Grid>
          
          {/* 搜索类型选择 */}
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>Search By</InputLabel>
              <Select
                name="searchBy"
                value={filters.searchBy}
                onChange={handleFilterChange}
                label="Search By"
              >
                <MenuItem value="all">All Fields</MenuItem>
                <MenuItem value="name">Schedule Name</MenuItem>
                <MenuItem value="course">Course</MenuItem>
                <MenuItem value="teacher">Teacher</MenuItem>
                <MenuItem value="classroom">Classroom</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          
          {/* 日期范围筛选 - 使用标准HTML日期输入 */}
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Start Date"
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
              label="End Date"
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
              <InputLabel>Semester</InputLabel>
              <Select
                name="semester"
                value={filters.semester}
                onChange={handleFilterChange}
                label="Semester"
              >
                <MenuItem value="">All Semesters</MenuItem>
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
              <InputLabel>Status</InputLabel>
              <Select
                name="status"
                value={filters.status}
                onChange={handleFilterChange}
                label="Status"
              >
                <MenuItem value="">All Statuses</MenuItem>
                <MenuItem value="Draft">Draft</MenuItem>
                <MenuItem value="Published">Published</MenuItem>
                <MenuItem value="Cancelled">Cancelled</MenuItem>
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
              Reset Filters
            </Button>
          </Grid>
        </Grid>
      </Paper>
      
      {/* 筛选结果列表 */}
      <TableContainer component={Paper} variant="outlined" sx={{ mt: 2 }}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Schedule Name</TableCell>
              <TableCell>Created Date</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Semester</TableCell>
              <TableCell>Courses</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filterSchedules().map((schedule) => (
              <TableRow key={schedule.id}>
                <TableCell>{schedule.name}</TableCell>
                <TableCell>{formatDate(schedule.createdAt)}</TableCell>
                <TableCell>
                  <Chip 
                    label={schedule.status} 
                    color={schedule.status === 'Published' ? 'success' : 'warning'} 
                    size="small" 
                    variant="outlined" 
                  />
                </TableCell>
                <TableCell>{schedule.semesterName || 'Unknown'}</TableCell>
                <TableCell>
                  {schedule.details && (
                    `${schedule.details.length} courses`
                  )}
                </TableCell>
                <TableCell>
                  <Box sx={{ display: 'flex', gap: 1 }}>
                    <Button 
                      variant="contained" 
                      size="small" 
                      onClick={() => handleViewSchedule(schedule.id)}
                    >
                      View
                    </Button>
                    
                    {schedule.status === 'Draft' && (
                      <Button 
                        variant="outlined" 
                        size="small" 
                        color="success"
                        onClick={() => handlePublishSchedule(schedule.id)}
                      >
                        Publish
                      </Button>
                    )}
                    
                    {schedule.status !== 'Cancelled' && (
                      <Button 
                        variant="outlined" 
                        size="small" 
                        color="error"
                        onClick={() => handleCancelSchedule(schedule.id)}
                      >
                        Cancel
                      </Button>
                    )}
                  </Box>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      
      {filterSchedules().length === 0 && (
        <Box sx={{ textAlign: 'center', py: 4 }}>
          <Typography variant="body1" color="text.secondary">
            No matching schedules found. Try adjusting your filters.
          </Typography>
        </Box>
      )}
      
      {/* 分析报告段落 */}
      <Box sx={{ mt: 4 }}>
        <Typography variant="h6" gutterBottom>
          Utilization Reports
        </Typography>
        <Grid container spacing={2}>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle1" gutterBottom>
                  Classroom Utilization
                </Typography>
                <Button variant="contained" fullWidth>Generate Report</Button>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle1" gutterBottom>
                  Faculty Workload Analysis
                </Typography>
                <Button variant="contained" fullWidth>Generate Report</Button>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle1" gutterBottom>
                  Course Demand Trends
                </Typography>
                <Button variant="contained" fullWidth>Generate Report</Button>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </Box>
    </Box>
  );
};

export default ScheduleHistory;