import React, { useState } from 'react';
import { 
  Paper, Typography, Button, Box, CircularProgress, 
  Divider, Chip, List, ListItem, ListItemText, 
  Card, CardContent, CardActions, Alert, 
  Dialog, DialogTitle, DialogContent, DialogActions,
  Table, TableBody, TableCell, TableContainer, 
  TableHead, TableRow, IconButton, Tooltip,
  Badge
} from '@mui/material';
import TuneIcon from '@mui/icons-material/Tune';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import InfoIcon from '@mui/icons-material/Info';
import CheckIcon from '@mui/icons-material/Check';
import AddCircleIcon from '@mui/icons-material/AddCircle';
import CloseIcon from '@mui/icons-material/Close';
import { optimizeParameters } from '../../Services/llmApi';

const ParameterOptimization = ({ currentParameters, historicalData, onApplyChanges }) => {
  const [loading, setLoading] = useState(false);
  const [optimizations, setOptimizations] = useState(null);
  const [error, setError] = useState('');
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedSuggestions, setSelectedSuggestions] = useState([]);
  const [selectedNewParameters, setSelectedNewParameters] = useState([]);

  const handleOptimize = async () => {
    setLoading(true);
    setError('');
    
    try {
      // Call LLM API for parameter optimization suggestions
      const response = await optimizeParameters(currentParameters, historicalData);
      setOptimizations(response);
      
      // By default, select all suggestions
      if (response.optimizationSuggestions) {
        setSelectedSuggestions(response.optimizationSuggestions.map(s => s.parameterName));
      }
      
      if (response.newParameterSuggestions) {
        setSelectedNewParameters(response.newParameterSuggestions.map(s => s.parameterName));
      }
      
      setDialogOpen(true);
    } catch (error) {
      console.error('Error getting optimization suggestions:', error);
      setError('Failed to get optimization suggestions. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
  };

  // In the handleApplySuggestions function in ParameterOptimization.jsx
  // Add this logic to better handle different types of parameters:

  const handleApplySuggestions = () => {
    if (onApplyChanges && optimizations) {
      const selectedOptimizations = optimizations.optimizationSuggestions.filter(
        suggestion => selectedSuggestions.includes(suggestion.parameterName)
      );
      
      const selectedNewParams = optimizations.newParameterSuggestions?.filter(
        suggestion => selectedNewParameters.includes(suggestion.parameterName)
      ) || [];
      
      // Create a more detailed changes object that specifies parameter types
      const changes = {
        parameterChanges: selectedOptimizations.map(param => {
          // Classify parameter types based on name patterns
          let paramType = "general";
          if (param.parameterName.includes("Weight") || param.parameterName.includes("Balance")) {
            paramType = "weight";
          } else if (param.parameterName.includes("Maximum") || param.parameterName.includes("Minimum")) {
            paramType = "limit";
          }
          
          return {
            ...param,
            paramType
          };
        }),
        newParameters: selectedNewParams
      };
      
      onApplyChanges(changes);
    }
    
    setDialogOpen(false);
  };

  const toggleParameterSelection = (paramName) => {
    setSelectedSuggestions(prev => 
      prev.includes(paramName) 
        ? prev.filter(p => p !== paramName)
        : [...prev, paramName]
    );
  };

  const toggleNewParameterSelection = (paramName) => {
    setSelectedNewParameters(prev => 
      prev.includes(paramName) 
        ? prev.filter(p => p !== paramName)
        : [...prev, paramName]
    );
  };

  const selectAllSuggestions = () => {
    if (optimizations && optimizations.optimizationSuggestions) {
      setSelectedSuggestions(optimizations.optimizationSuggestions.map(s => s.parameterName));
    }
    
    if (optimizations && optimizations.newParameterSuggestions) {
      setSelectedNewParameters(optimizations.newParameterSuggestions.map(s => s.parameterName));
    }
  };

  const selectNoneSuggestions = () => {
    setSelectedSuggestions([]);
    setSelectedNewParameters([]);
  };

  return (
    <>
      <Badge
        badgeContent={optimizations ? 
          (optimizations.optimizationSuggestions.length + 
           (optimizations.newParameterSuggestions?.length || 0)) : 0}
        color="primary"
        showZero={false}
        invisible={!optimizations}
        max={99}
      >
        <Button 
          variant="outlined" 
          color="primary"
          startIcon={<TuneIcon />}
          onClick={handleOptimize}
          disabled={loading}
        >
          {loading ? <CircularProgress size={24} /> : "Intelligent Parameter Optimization"}
        </Button>
      </Badge>
      
      {error && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {error}
        </Alert>
      )}
      
      {/* Optimization Suggestions Dialog */}
      <Dialog
        open={dialogOpen}
        onClose={handleCloseDialog}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">
            Parameter Optimization Suggestions
          </Typography>
          <IconButton onClick={handleCloseDialog} size="small">
            <CloseIcon />
          </IconButton>
        </DialogTitle>
        
        <DialogContent dividers>
          {optimizations && (
            <>
              <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="text.secondary">
                  This AI tool analyzes your current scheduling parameters and historical scheduling data to suggest optimizations that can improve scheduling outcomes. It can recommend adjustments to existing parameters and suggest new parameters to consider.
                </Typography>
              </Box>
              
              {/* Parameter Adjustments */}
              <Typography variant="subtitle1" gutterBottom sx={{ display: 'flex', alignItems: 'center' }}>
                <TuneIcon sx={{ mr: 1 }} />
                Parameter Adjustment Suggestions
              </Typography>
              
              <TableContainer component={Paper} variant="outlined" sx={{ mb: 3 }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell padding="checkbox" width={50}></TableCell>
                      <TableCell>Parameter</TableCell>
                      <TableCell>Current Value</TableCell>
                      <TableCell>Suggested Value</TableCell>
                      <TableCell width={150}>Change</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {optimizations.optimizationSuggestions.map((suggestion) => (
                      <TableRow 
                        key={suggestion.parameterName}
                        hover
                        selected={selectedSuggestions.includes(suggestion.parameterName)}
                        onClick={() => toggleParameterSelection(suggestion.parameterName)}
                        sx={{ cursor: 'pointer' }}
                      >
                        <TableCell padding="checkbox">
                          <CheckIcon 
                            color={selectedSuggestions.includes(suggestion.parameterName) ? "success" : "disabled"} 
                          />
                        </TableCell>
                        <TableCell>{suggestion.parameterName}</TableCell>
                        <TableCell>{suggestion.currentValue}</TableCell>
                        <TableCell>{suggestion.suggestedValue}</TableCell>
                        <TableCell>
                          <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            <Typography variant="body2" color="text.secondary">
                              {suggestion.currentValue}
                            </Typography>
                            <ArrowForwardIcon sx={{ mx: 1 }} fontSize="small" />
                            <Chip 
                              label={suggestion.suggestedValue} 
                              size="small" 
                              color="primary"
                            />
                          </Box>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
              
              {/* Adjustment Rationales */}
              <List>
                {optimizations.optimizationSuggestions.map((suggestion) => (
                  <ListItem key={suggestion.parameterName} divider>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          <Typography variant="subtitle2">
                            {suggestion.parameterName}
                          </Typography>
                          {selectedSuggestions.includes(suggestion.parameterName) && (
                            <Chip 
                              label="Selected" 
                              size="small" 
                              color="success"
                              sx={{ ml: 1 }}
                            />
                          )}
                        </Box>
                      }
                      secondary={
                        <>
                          <Typography variant="body2" component="div" sx={{ mt: 1 }}>
                            <strong>Rationale:</strong> {suggestion.rationale}
                          </Typography>
                          <Typography variant="body2" component="div" sx={{ mt: 1 }}>
                            <strong>Expected Effect:</strong> {suggestion.expectedEffect}
                          </Typography>
                        </>
                      }
                    />
                  </ListItem>
                ))}
              </List>
              
              {/* New Parameter Suggestions */}
              {optimizations.newParameterSuggestions?.length > 0 && (
                <>
                  <Divider sx={{ my: 3 }} />
                  
                  <Typography variant="subtitle1" gutterBottom sx={{ display: 'flex', alignItems: 'center' }}>
                    <AddCircleIcon sx={{ mr: 1 }} />
                    New Parameter Suggestions
                  </Typography>
                  
                  <List>
                    {optimizations.newParameterSuggestions.map((suggestion) => (
                      <ListItem 
                        key={suggestion.parameterName} 
                        divider
                        button
                        selected={selectedNewParameters.includes(suggestion.parameterName)}
                        onClick={() => toggleNewParameterSelection(suggestion.parameterName)}
                      >
                        <ListItemText
                          primary={
                            <Box sx={{ display: 'flex', alignItems: 'center' }}>
                              <Typography variant="subtitle2">
                                {suggestion.parameterName} ({suggestion.suggestedValue})
                              </Typography>
                              {selectedNewParameters.includes(suggestion.parameterName) && (
                                <Chip 
                                  label="Selected" 
                                  size="small" 
                                  color="success"
                                  sx={{ ml: 1 }}
                                />
                              )}
                            </Box>
                          }
                          secondary={
                            <>
                              <Typography variant="body2" component="div" sx={{ mt: 1 }}>
                                <strong>Rationale:</strong> {suggestion.rationale}
                              </Typography>
                              <Typography variant="body2" component="div" sx={{ mt: 1 }}>
                                <strong>Expected Effect:</strong> {suggestion.expectedEffect}
                              </Typography>
                            </>
                          }
                        />
                      </ListItem>
                    ))}
                  </List>
                </>
              )}
            </>
          )}
        </DialogContent>
        
        <DialogActions sx={{ justifyContent: 'space-between', px: 3, py: 2 }}>
          <Box>
            <Button onClick={selectAllSuggestions} sx={{ mr: 1 }}>
              Select All
            </Button>
            <Button onClick={selectNoneSuggestions}>
              Select None
            </Button>
          </Box>
          <Box>
            <Button onClick={handleCloseDialog} sx={{ mr: 1 }}>
              Cancel
            </Button>
            <Button 
              onClick={handleApplySuggestions} 
              variant="contained" 
              color="primary"
              disabled={
                selectedSuggestions.length === 0 && 
                selectedNewParameters.length === 0
              }
            >
              Apply Selected Changes
            </Button>
          </Box>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default ParameterOptimization;