import React, { useState } from 'react';
import { 
  Paper, Typography, TextField, Button, Box, Divider, 
  Chip, CircularProgress, List, ListItem, ListItemText,
  Card, CardContent, Alert, IconButton
} from '@mui/material';
import PsychologyIcon from '@mui/icons-material/Psychology';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import LightbulbIcon from '@mui/icons-material/Lightbulb';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import { analyzeConstraints } from '../../services/llmApi';

const RequirementAnalyzer  = ({ onAddConstraints }) => {
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState(null);
  const [error, setError] = useState('');

  const handleInputChange = (e) => {
    setInput(e.target.value);
  };

  const handleAnalyze = async () => {
    if (input.trim() === '') return;
    
    setLoading(true);
    setError('');
    
    try {
      // Call LLM API for constraint analysis
      const response = await analyzeConstraints(input);
      setResults(response);
    } catch (error) {
      console.error('Error analyzing constraints:', error);
      setError('An error occurred while analyzing constraints. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  const handleAddAll = () => {
    if (results && onAddConstraints) {
      onAddConstraints([
        ...results.explicitConstraints,
        ...results.implicitConstraints
      ]);
    }
  };

  return (
    <Paper sx={{ p: 3, mb: 3 }}>
      <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <PsychologyIcon sx={{ mr: 1 }} />
        Scheduling Requirements Analyzer
      </Typography>
      
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        This AI tool analyzes your natural language scheduling requirements and extracts formal constraints, course relationships, and scheduling rules that can be applied to the system. Use it to quickly set up scheduling criteria from your written descriptions.
      </Typography>
      
      <TextField
        fullWidth
        multiline
        rows={4}
        label="Describe your scheduling requirements"
        placeholder="Example: We need to schedule an Advanced Mathematics course, twice a week, 2 hours each session. Professor Zhang is only available on Monday and Wednesday mornings. The class has approximately 120 students and requires projection equipment. Also, this course should preferably not be on the same day as the Physics course because it's a heavy workload for students."
        value={input}
        onChange={handleInputChange}
        variant="outlined"
        sx={{ mb: 2 }}
      />
      
      <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
        <Button
          variant="contained"
          onClick={handleAnalyze}
          disabled={loading || input.trim() === ''}
        >
          {loading ? <CircularProgress size={24} sx={{ mr: 1 }} /> : null}
          Analyze Constraints
        </Button>
      </Box>
      
      {error && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {error}
        </Alert>
      )}
      
      {results && (
        <Box sx={{ mt: 3 }}>
          <Divider sx={{ mb: 2 }}>
            <Chip label="Analysis Results" />
          </Divider>
          
          <Typography variant="subtitle1" sx={{ mt: 2, display: 'flex', alignItems: 'center' }}>
            <CheckCircleIcon color="primary" sx={{ mr: 1 }} />
            Explicit Constraints
          </Typography>
          
          <Box sx={{ mt: 1 }}>
            {results.explicitConstraints.map((constraint) => (
              <Card key={constraint.id} variant="outlined" sx={{ mb: 2 }}>
                <CardContent>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Typography variant="subtitle2">
                      {constraint.name}
                    </Typography>
                    <Chip 
                      size="small" 
                      label={constraint.type} 
                      color={constraint.type === 'Hard' ? 'error' : 'primary'} 
                    />
                  </Box>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    {constraint.description}
                  </Typography>
                  {constraint.type === 'Soft' && (
                    <Typography variant="body2" sx={{ mt: 1 }}>
                      Weight: {constraint.weight.toFixed(1)}
                    </Typography>
                  )}
                </CardContent>
              </Card>
            ))}
          </Box>
          
          <Typography variant="subtitle1" sx={{ mt: 3, display: 'flex', alignItems: 'center' }}>
            <LightbulbIcon color="primary" sx={{ mr: 1 }} />
            Implicit Constraints (System Detected)
          </Typography>
          
          <Box sx={{ mt: 1 }}>
            {results.implicitConstraints.map((constraint) => (
              <Card key={constraint.id} variant="outlined" sx={{ mb: 2 }}>
                <CardContent>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Typography variant="subtitle2">
                      {constraint.name}
                    </Typography>
                    <Chip 
                      size="small" 
                      label={constraint.type} 
                      color={constraint.type === 'Hard' ? 'error' : 'primary'} 
                    />
                  </Box>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    {constraint.description}
                  </Typography>
                  {constraint.type === 'Soft' && (
                    <Typography variant="body2" sx={{ mt: 1 }}>
                      Suggested Weight: {constraint.weight.toFixed(1)}
                    </Typography>
                  )}
                </CardContent>
              </Card>
            ))}
          </Box>
          
          {onAddConstraints && (
            <Box sx={{ mt: 3, display: 'flex', justifyContent: 'center' }}>
              <Button
                variant="contained"
                color="primary"
                startIcon={<AddCircleOutlineIcon />}
                onClick={handleAddAll}
              >
                Add All Constraints to System
              </Button>
            </Box>
          )}
        </Box>
      )}
    </Paper>
  );
};

export default RequirementAnalyzer;