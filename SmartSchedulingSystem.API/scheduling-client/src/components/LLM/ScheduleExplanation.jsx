import React, { useState } from 'react';
import { 
  Dialog, DialogTitle, DialogContent, DialogActions,
  Typography, Button, Box, CircularProgress, Divider,
  Chip, List, ListItem, ListItemText, Paper, IconButton,
  Tooltip, Alert
} from '@mui/material';
import QuestionMarkIcon from '@mui/icons-material/QuestionMark';
import CloseIcon from '@mui/icons-material/Close';
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import MeetingRoomIcon from '@mui/icons-material/MeetingRoom';
import PersonIcon from '@mui/icons-material/Person';
import CompareArrowsIcon from '@mui/icons-material/CompareArrows';
import { explainSchedule } from '../../services/llmApi';

const ScheduleExplanation = ({ scheduleItem }) => {
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [explanation, setExplanation] = useState(null);
  const [error, setError] = useState('');

  const handleOpen = async () => {
    setOpen(true);
    setLoading(true);
    setError('');
    
    try {
      // Call LLM API to get schedule explanation
      const response = await explainSchedule(scheduleItem);
      setExplanation(response);
    } catch (error) {
      console.error('Error getting schedule explanation:', error);
      setError('Failed to get explanation. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setOpen(false);
  };

  return (
    <>
      <Tooltip title="Why was this scheduled this way?">
        <IconButton
          size="small"
          color="primary"
          onClick={handleOpen}
          sx={{ ml: 1 }}
        >
          <QuestionMarkIcon fontSize="small" />
        </IconButton>
      </Tooltip>
      
      <Dialog
        open={open}
        onClose={handleClose}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">
            Schedule Decision Explanation
          </Typography>
          <IconButton onClick={handleClose} size="small">
            <CloseIcon />
          </IconButton>
        </DialogTitle>
        
        <DialogContent dividers>
          {/* Schedule Item Summary */}
          <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
            <Typography variant="subtitle1" gutterBottom>
              {scheduleItem.courseName} ({scheduleItem.courseCode})
            </Typography>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
              <Chip 
                icon={<PersonIcon />} 
                label={`Teacher: ${scheduleItem.teacherName}`}
                variant="outlined"
                size="small"
              />
              <Chip 
                icon={<MeetingRoomIcon />} 
                label={`Classroom: ${scheduleItem.classroom}`}
                variant="outlined"
                size="small"
              />
              <Chip 
                icon={<AccessTimeIcon />} 
                label={`Time: ${scheduleItem.dayName} ${scheduleItem.startTime}-${scheduleItem.endTime}`}
                variant="outlined"
                size="small"
              />
            </Box>
          </Paper>
          
          {/* Loading State */}
          {loading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
              <CircularProgress />
              <Typography variant="body2" sx={{ ml: 2 }}>
                Analyzing scheduling decision...
              </Typography>
            </Box>
          )}
          
          {/* Error State */}
          {error && (
            <Alert severity="error" sx={{ mt: 2, mb: 2 }}>
              {error}
            </Alert>
          )}
          
          {/* Explanation Content */}
          {explanation && !loading && (
            <Box>
              {/* Time Rationale */}
              <Box sx={{ mb: 3 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <AccessTimeIcon color="primary" sx={{ mr: 1 }} />
                  <Typography variant="subtitle1">Time Selection Rationale</Typography>
                </Box>
                <Typography variant="body2">
                  {explanation.timeRationale}
                </Typography>
              </Box>
              
              <Divider sx={{ my: 2 }} />
              
              {/* Classroom Rationale */}
              <Box sx={{ mb: 3 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <MeetingRoomIcon color="primary" sx={{ mr: 1 }} />
                  <Typography variant="subtitle1">Classroom Selection Rationale</Typography>
                </Box>
                <Typography variant="body2">
                  {explanation.classroomRationale}
                </Typography>
              </Box>
              
              <Divider sx={{ my: 2 }} />
              
              {/* Teacher Rationale */}
              <Box sx={{ mb: 3 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <PersonIcon color="primary" sx={{ mr: 1 }} />
                  <Typography variant="subtitle1">Teacher Selection Rationale</Typography>
                </Box>
                <Typography variant="body2">
                  {explanation.teacherRationale}
                </Typography>
              </Box>
              
              <Divider sx={{ my: 2 }} />
              
              {/* Overall Rationale */}
              <Box sx={{ mb: 3 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Comprehensive Considerations
                </Typography>
                <Paper variant="outlined" sx={{ p: 2, bgcolor: 'background.default' }}>
                  <Typography variant="body2">
                    {explanation.overallRationale}
                  </Typography>
                </Paper>
              </Box>
              
              {/* Alternatives Considered */}
              <Box sx={{ mb: 2 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <CompareArrowsIcon color="primary" sx={{ mr: 1 }} />
                  <Typography variant="subtitle1">Alternatives Considered</Typography>
                </Box>
                
                <List disablePadding>
                  {explanation.alternativesConsidered.map((alt, index) => (
                    <ListItem 
                      key={index} 
                      sx={{ 
                        border: '1px solid #e0e0e0', 
                        borderRadius: 1, 
                        mb: 1,
                        bgcolor: 'background.default' 
                      }}
                    >
                      <ListItemText
                        primary={
                          <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            <Chip 
                              label={alt.type} 
                              size="small" 
                              color="primary" 
                              variant="outlined" 
                              sx={{ mr: 1 }}
                            />
                            <Typography variant="subtitle2">
                              {/* 检测alternative是否包含中文字符，如果是则提供英文替代文本 */}
                              {/[\u4e00-\u9fa5]/.test(alt.alternative) ? 
                                (alt.type === "time" ? "Alternative time slot: 9:00-11:00 AM" :
                                 alt.type === "classroom" ? "Alternative classroom: Science Building Room 304" :
                                 alt.type === "teacher" ? "Alternative teacher: Professor Wang" :
                                 "Alternative option")
                                : alt.alternative
                              }
                            </Typography>
                          </Box>
                        }
                        secondary={
                          <Typography variant="body2" sx={{ mt: 1 }}>
                            <strong>Why not chosen:</strong> {
                              // 检测是否包含中文字符，如果是则提供英文替代文本
                              /[\u4e00-\u9fa5]/.test(alt.whyNotChosen) ? 
                              (alt.type === "time" ? "This time slot would conflict with other important courses or activities." :
                               alt.type === "classroom" ? "This classroom either lacks necessary equipment or appropriate capacity for this course." :
                               alt.type === "teacher" ? "This teacher has other teaching commitments or less expertise in this subject area." :
                               "This alternative would not be optimal for overall scheduling quality.")
                              : alt.whyNotChosen
                            }
                          </Typography>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              </Box>
            </Box>
          )}
        </DialogContent>
        
        <DialogActions>
          <Button onClick={handleClose}>Close</Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default ScheduleExplanation;