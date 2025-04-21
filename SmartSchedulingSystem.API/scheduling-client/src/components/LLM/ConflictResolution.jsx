import React, { useState, useEffect } from 'react';
import { 
  Paper, Typography, Box, Button, Divider, 
  Chip, CircularProgress, Card, CardContent,
  CardActions, List, ListItem, ListItemText,
  Dialog, DialogTitle, DialogContent,
  DialogActions, Alert, LinearProgress,
  Collapse, IconButton
} from '@mui/material';
import WarningIcon from '@mui/icons-material/Warning';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import { analyzeConflicts } from '../../services/llmApi';

const ConflictResolution = ({ 
  conflict, 
  onResolve, 
  onAnalyze, 
  isAnalyzing = false, 
  isAnalyzed = false,
  showSolutions = false 
}) => {
  const [loading, setLoading] = useState(false);
  const [analysis, setAnalysis] = useState(null);
  const [error, setError] = useState('');
  const [expanded, setExpanded] = useState(showSolutions);
  const [localStatus, setLocalStatus] = useState(conflict.status);

  useEffect(() => {
    setLocalStatus(conflict.status);
  }, [conflict.status]);
  
  const handleAnalyze = async () => {
    console.log('===== handleAnalyze 函数被调用 =====');
    
    // 如果有父组件的analyze函数，先调用它以保持状态更新
    if (onAnalyze) {
      console.log('外部onAnalyze函数存在，调用它进行状态更新');
      onAnalyze(conflict.id);
      // 不再提前返回，继续执行本地API调用
    }
    
    // 无论如何都执行本地分析
    setLoading(true);
    setError('');
    setExpanded(true);
    
    console.log('ConflictResolution: 开始本地API调用分析...');
    console.log('ConflictResolution: 冲突数据:', conflict);
    
    try {
      // Call LLM API for conflict analysis with timeout
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 30000); // 30秒超时
      
      let response;
      try {
        // 尝试从API获取数据
        console.log('ConflictResolution: 准备调用analyzeConflicts API...');
        response = await analyzeConflicts(conflict);
        console.log('ConflictResolution: API调用返回结果:', response);
        clearTimeout(timeoutId);
        
        // 检查是否是错误响应对象
        if (response && response.error === true) {
          console.error('ConflictResolution: API返回错误对象:', response);
          
          // 提取模拟响应
          if (response.mockResponse) {
            console.log('ConflictResolution: 使用API返回的模拟数据');
            setError(`API错误: ${response.message || '未知错误'}. 显示模拟分析。`);
            response = response.mockResponse;
          } else {
            throw new Error(response.message || '未知API错误');
          }
        }
      } catch (apiError) {
        console.error('API错误，详细信息:', apiError);
        
        // 尝试从错误对象中提取模拟响应
        if (apiError.mockResponse) {
          console.log('ConflictResolution: 使用错误对象中的模拟数据');
          response = apiError.mockResponse;
          setError(`API调用失败: ${apiError.message || '未知错误'}. 显示模拟分析。`);
        } else {
          // 使用默认模拟数据
          console.log('ConflictResolution: 使用默认模拟数据');
          response = {
            rootCause: `${conflict.type}冲突通常发生在同一资源被多个课程同时需要的情况。这种冲突可以通过调整时间或者重新分配资源来解决。`,
            solutions: [
              {
                id: 1,
                description: "调整其中一门课程的时间安排",
                compatibility: 85,
                impacts: [
                  "学生课表可能需要调整",
                  "教师日程可能需要变更"
                ]
              },
              {
                id: 2,
                description: "寻找替代资源（教室或教师）",
                compatibility: 80,
                impacts: [
                  "课程质量可能受到轻微影响",
                  "保持时间安排不变"
                ]
              },
              {
                id: 3,
                description: "分班教学",
                compatibility: 75,
                impacts: [
                  "需要额外的教学资源",
                  "可以保持原有时间和教师安排"
                ]
              }
            ],
            _isMockData: true
          };
          
          // 显示用户可见的错误信息
          setError(`API调用失败: ${apiError.message || '未知错误'}. 显示模拟分析。`);
        }
      }
      
      console.log('ConflictResolution: 分析结果:', response);
      
      // 验证响应格式是否正确
      if (!response || !response.solutions || !Array.isArray(response.solutions)) {
        console.error('ConflictResolution: 响应格式不正确:', response);
        setError('收到无效的API响应。请联系管理员或稍后再试。');
      } else {
        // 确保_isMockData标记存在
        if (response._isMockData === undefined) {
          response._isMockData = true; // 保守起见，未明确标记的假设为模拟数据
        }
        
        setAnalysis(response);
        setExpanded(true);
      }
    } catch (error) {
      console.error('ConflictResolution: 分析冲突时出错:', error);
      
      // 从错误响应对象中提取更详细的信息
      if (error.details) {
        setError(`分析冲突时出错: ${error.message}. 详情: ${error.details}`);
      } else if (error.message) {
        setError(`分析冲突时出错: ${error.message}`);
      } else {
        setError('分析冲突时出现未知错误。请稍后再试。');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleExpandToggle = () => {
    setExpanded(!expanded);
  };

  const handleApplySolution = (solution) => {
    if (onResolve) {
      setLocalStatus('Resolved');
      onResolve(solution, conflict.id);
    }
  };

  // Determine if analysis is available
  const hasAnalysis = analysis || isAnalyzed;
  const isResolved = localStatus === 'Resolved';

  return (
    <Card variant="outlined" sx={{ mb: 2, border: isResolved ? '1px solid #c8e6c9' : '1px solid #ffcdd2' }}>
      <CardContent>
        {/* Conflict Summary */}
        <Box sx={{ display: 'flex', alignItems: 'flex-start', mb: 1 }}>
          {isResolved ? 
            <CheckCircleIcon color="success" sx={{ mr: 1, mt: 0.5 }} /> :
            <WarningIcon color="error" sx={{ mr: 1, mt: 0.5 }} />
          }
          <Box sx={{ flex: 1 }}>
            <Typography variant="subtitle1" fontWeight="bold" color={isResolved ? 'success.main' : 'error.main'}>
              {conflict.type} Conflict
            </Typography>
            <Typography variant="body2">{conflict.description}</Typography>
          </Box>
          <Chip
            label={isResolved ? 'Resolved' : 'Unresolved'}
            color={isResolved ? 'success' : 'error'}
            size="small"
          />
        </Box>
        
        {/* Courses involved */}
        <Box sx={{ mt: 2, mb: 2 }}>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Courses involved:
          </Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {conflict.involvedCourses.map((course, index) => (
              <Chip 
                key={index}
                label={course.code ? `${course.name} (${course.code})` : `${course.courseCode} (${course.courseName})`}
                size="small"
                variant="outlined"
                color={isResolved ? 'success' : 'error'}
              />
            ))}
          </Box>
        </Box>
        
        {/* Actions */}
        {!hasAnalysis && !loading && !isAnalyzing && !isResolved && (
          <Button
            variant="contained"
            color="primary"
            onClick={handleAnalyze}
            disabled={loading || isAnalyzing || isResolved}
            size="small"
          >
            Intelligent Analysis
          </Button>
        )}
        
        {(loading || isAnalyzing) && (
          <Box sx={{ my: 2 }}>
            <Typography variant="body2" gutterBottom>Analyzing conflict...</Typography>
            <LinearProgress />
          </Box>
        )}
        
        {/* 错误信息显示 */}
        {error && (
          <Alert 
            severity="error" 
            sx={{ mt: 2 }}
            action={
              <Button 
                color="inherit" 
                size="small" 
                onClick={() => setError('')}
              >
                DISMISS
              </Button>
            }
          >
            {error}
          </Alert>
        )}
        
        {/* API模拟数据警告 */}
        {analysis && analysis._isMockData && !isResolved && (
          <Alert severity="warning" sx={{ mt: 2 }}>
            Note: This analysis is based on mock data as the API connection failed.
          </Alert>
        )}
        
        {/* Resolved message */}
        {isResolved && (
          <Alert severity="success" sx={{ mt: 2 }}>
            This conflict has been successfully resolved.
          </Alert>
        )}
      </CardContent>
      
      {/* Expanded Analysis */}
      {hasAnalysis && !isResolved && (
        <>
          <Divider />
          <CardActions sx={{ justifyContent: 'space-between', px: 2 }}>
            <Typography variant="subtitle2">AI Conflict Analysis</Typography>
            <IconButton 
              onClick={handleExpandToggle}
              aria-expanded={expanded}
              aria-label="show more"
              size="small"
            >
              {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
            </IconButton>
          </CardActions>
          
          <Collapse in={expanded} timeout="auto" unmountOnExit>
            <CardContent>
              {/* Root Cause */}
              <Typography variant="subtitle2" gutterBottom>
                Root Cause:
              </Typography>
              <Paper variant="outlined" sx={{ p: 2, mb: 3, bgcolor: 'background.default' }}>
                <Typography variant="body2">
                  {analysis?.rootCause || "This conflict appears to be due to competing requirements for limited resources during high-demand time slots."}
                </Typography>
              </Paper>
              
              {/* Solutions */}
              <Typography variant="subtitle2" gutterBottom>
                Suggested Solutions:
              </Typography>
              
              {/* Default solutions if no analysis available */}
              {!analysis && (
                <>
                  <Card variant="outlined" sx={{ mb: 2, border: '1px solid #e0e0e0' }}>
                    <CardContent>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                        <Typography variant="subtitle2">
                          Solution 1
                        </Typography>
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          <Typography variant="caption" sx={{ mr: 1 }}>
                            Compatibility:
                          </Typography>
                          <Chip 
                            label="85%"
                            color="success"
                            size="small"
                          />
                        </Box>
                      </Box>
                      
                      <Typography variant="body2" sx={{ mb: 2 }}>
                        {conflict.type === 'Teacher Schedule' 
                          ? "Reschedule one of the conflicting courses to a different time slot."
                          : "Move one of the courses to a nearby available classroom."}
                      </Typography>
                      
                      <Typography variant="caption" color="text.secondary">
                        Potential impacts:
                      </Typography>
                      <List dense disablePadding>
                        <ListItem disablePadding disableGutters>
                          <ListItemText 
                            primary={conflict.type === 'Teacher Schedule' 
                              ? "May cause some student schedule disruptions. All core requirements still met."
                              : "Minimal disruption to schedules. Maintains same time slots."}
                            primaryTypographyProps={{ variant: 'caption' }}
                          />
                        </ListItem>
                      </List>
                    </CardContent>
                    <CardActions sx={{ justifyContent: 'flex-end' }}>
                      <Button 
                        size="small" 
                        variant="contained"
                        onClick={() => handleApplySolution({
                          id: 1,
                          compatibility: 85,
                          description: conflict.type === 'Teacher Schedule' 
                            ? "Reschedule one of the conflicting courses to a different time slot."
                            : "Move one of the courses to a nearby available classroom."
                        })}
                      >
                        Apply This Solution
                      </Button>
                    </CardActions>
                  </Card>
                  
                  <Card variant="outlined" sx={{ mb: 2, border: '1px solid #e0e0e0' }}>
                    <CardContent>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                        <Typography variant="subtitle2">
                          Solution 2
                        </Typography>
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          <Typography variant="caption" sx={{ mr: 1 }}>
                            Compatibility:
                          </Typography>
                          <Chip 
                            label={conflict.type === 'Teacher Schedule' ? "80%" : "75%"}
                            color="warning"
                            size="small"
                          />
                        </Box>
                      </Box>
                      
                      <Typography variant="body2" sx={{ mb: 2 }}>
                        {conflict.type === 'Teacher Schedule' 
                          ? "Find an alternative room or teacher depending on the specific conflict."
                          : "Reschedule one of the courses to a different time slot."}
                      </Typography>
                      
                      <Typography variant="caption" color="text.secondary">
                        Potential impacts:
                      </Typography>
                      <List dense disablePadding>
                        <ListItem disablePadding disableGutters>
                          <ListItemText 
                            primary={conflict.type === 'Teacher Schedule' 
                              ? "Might require adjusting other course parameters. Maintains original time preferences."
                              : "May cause schedule disruptions for students and teachers."}
                            primaryTypographyProps={{ variant: 'caption' }}
                          />
                        </ListItem>
                      </List>
                    </CardContent>
                    <CardActions sx={{ justifyContent: 'flex-end' }}>
                      <Button 
                        size="small" 
                        variant="contained"
                        onClick={() => handleApplySolution({
                          id: 2,
                          compatibility: conflict.type === 'Teacher Schedule' ? 80 : 75,
                          description: conflict.type === 'Teacher Schedule' 
                            ? "Find an alternative room or teacher depending on the specific conflict."
                            : "Reschedule one of the courses to a different time slot."
                        })}
                      >
                        Apply This Solution
                      </Button>
                    </CardActions>
                  </Card>
                </>
              )}
              
              {/* Show API-generated solutions if available */}
              {analysis && analysis.solutions && analysis.solutions.map((solution) => (
                <Card key={solution.id} variant="outlined" sx={{ mb: 2, border: '1px solid #e0e0e0' }}>
                  <CardContent>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                      <Typography variant="subtitle2">
                        Solution {solution.id}
                      </Typography>
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Typography variant="caption" sx={{ mr: 1 }}>
                          Compatibility:
                        </Typography>
                        <Chip 
                          label={`${solution.compatibility}%`}
                          color={
                            solution.compatibility > 80 ? 'success' :
                            solution.compatibility > 50 ? 'warning' : 'error'
                          }
                          size="small"
                        />
                      </Box>
                    </Box>
                    
                    <Typography variant="body2" sx={{ mb: 2 }}>
                      {solution.description}
                    </Typography>
                    
                    <Typography variant="caption" color="text.secondary">
                      Potential impacts:
                    </Typography>
                    <List dense disablePadding>
                      {solution.impacts.map((impact, idx) => (
                        <ListItem key={idx} disablePadding disableGutters>
                          <ListItemText 
                            primary={impact}
                            primaryTypographyProps={{ variant: 'caption' }}
                          />
                        </ListItem>
                      ))}
                    </List>
                  </CardContent>
                  <CardActions sx={{ justifyContent: 'flex-end' }}>
                    <Button 
                      size="small" 
                      variant="contained"
                      onClick={() => handleApplySolution(solution)}
                    >
                      Apply This Solution
                    </Button>
                  </CardActions>
                </Card>
              ))}
            </CardContent>
          </Collapse>
        </>
      )}
    </Card>
  );
};

export default ConflictResolution;