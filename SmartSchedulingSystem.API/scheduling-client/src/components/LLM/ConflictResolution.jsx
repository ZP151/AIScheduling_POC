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
    if (onAnalyze) {
      // Use parent component's analyze function if provided
      onAnalyze(conflict.id);
      setExpanded(true);
      return;
    }
    
    // Otherwise use local analyze function
    setLoading(true);
    setError('');
    
    try {
      // Call LLM API for conflict analysis
      const response = await analyzeConflicts(conflict);
      setAnalysis(response);
      setExpanded(true);
    } catch (error) {
      console.error('Error analyzing conflict:', error);
      setError('An error occurred while analyzing the conflict. Please try again later.');
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
        
        {error && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {error}
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