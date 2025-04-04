import React, { useState } from 'react';
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

const ConflictResolution = ({ conflict, onResolve }) => {
  const [loading, setLoading] = useState(false);
  const [analysis, setAnalysis] = useState(null);
  const [error, setError] = useState('');
  const [expanded, setExpanded] = useState(false);
  const [selectedSolution, setSelectedSolution] = useState(null);
  const [confirmDialogOpen, setConfirmDialogOpen] = useState(false);

  const handleAnalyze = async () => {
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
    setSelectedSolution(solution);
    setConfirmDialogOpen(true);
  };

  const handleConfirmSolution = () => {
    if (onResolve && selectedSolution) {
      onResolve(selectedSolution, conflict.id);
    }
    setConfirmDialogOpen(false);
  };
  
  const handleCancelSolution = () => {
    setSelectedSolution(null);
    setConfirmDialogOpen(false);
  };

  return (
    <Card variant="outlined" sx={{ mb: 2 }}>
      <CardContent>
        {/* Conflict Summary */}
        <Box sx={{ display: 'flex', alignItems: 'flex-start', mb: 1 }}>
          <WarningIcon color="error" sx={{ mr: 1, mt: 0.5 }} />
          <Box sx={{ flex: 1 }}>
            <Typography variant="subtitle1" fontWeight="bold">
              {conflict.type} Conflict
            </Typography>
            <Typography variant="body2">{conflict.description}</Typography>
          </Box>
          <Chip
            label={conflict.status || 'Unresolved'}
            color={conflict.status === 'Resolved' ? 'success' : 'error'}
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
                label={`${course.name} (${course.code})`}
                size="small"
                variant="outlined"
              />
            ))}
          </Box>
        </Box>
        
        {/* Actions */}
        {!analysis && !loading && (
          <Button
            variant="contained"
            color="primary"
            onClick={handleAnalyze}
            disabled={loading || conflict.status === 'Resolved'}
            size="small"
          >
            Intelligent Analysis
          </Button>
        )}
        
        {loading && (
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
      </CardContent>
      
      {/* Expanded Analysis */}
      {analysis && (
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
                <Typography variant="body2">{analysis.rootCause}</Typography>
              </Paper>
              
              {/* Solutions */}
              <Typography variant="subtitle2" gutterBottom>
                Suggested Solutions:
              </Typography>
              
              {analysis.solutions.map((solution) => (
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
      
      {/* Confirmation Dialog */}
      <Dialog
        open={confirmDialogOpen}
        onClose={handleCancelSolution}
      >
        <DialogTitle>Confirm Solution</DialogTitle>
        <DialogContent>
          <Typography variant="body1" paragraph>
            Are you sure you want to apply this solution?
          </Typography>
          {selectedSolution && (
            <>
              <Typography variant="subtitle2" gutterBottom>
                Solution {selectedSolution.id} ({selectedSolution.compatibility}% compatibility)
              </Typography>
              <Typography variant="body2" paragraph>
                {selectedSolution.description}
              </Typography>
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCancelSolution}>Cancel</Button>
          <Button 
            onClick={handleConfirmSolution} 
            variant="contained" 
            color="primary"
            startIcon={<CheckCircleIcon />}
          >
            Confirm
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
};

export default ConflictResolution;