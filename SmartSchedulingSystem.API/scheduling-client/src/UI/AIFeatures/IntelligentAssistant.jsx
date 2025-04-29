import React, { useState, useRef, useEffect } from 'react';
import { 
  Paper, Typography, TextField, Button, IconButton, 
  Divider, Box, Chip, CircularProgress, Tooltip,
  Fab, Dialog, DialogContent, DialogTitle
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import SendIcon from '@mui/icons-material/Send';
import SmartToyIcon from '@mui/icons-material/SmartToy';
import { chatWithLLM } from '../../Services/llmApi';

const IntelligentAssistant = () => {
  const [open, setOpen] = useState(false);
  const [message, setMessage] = useState('');
  const [conversation, setConversation] = useState([]);
  const [loading, setLoading] = useState(false);
  const messageEndRef = useRef(null);

  // Quick question suggestions
  const quickQuestions = [
    "Why are Computer Science courses scheduled in Building B?",
    "How can I optimize the current scheduling solution?",
    "How many course conflicts exist this semester?",
    "Analyze the current classroom utilization",
  ];
  //testing
  console.log('IntelligentAssistant rendered');
  
  useEffect(() => {
    // Scroll to latest message
    if (messageEndRef.current) {
      messageEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [conversation]);

  const handleOpen = () => {
    setOpen(true);
  };

  const handleClose = () => {
    setOpen(false);
  };

  const handleInputChange = (e) => {
    setMessage(e.target.value);
  };

  const handleSendMessage = async () => {
    if (message.trim() === '') return;

    // Add user message to conversation
    setConversation([...conversation, { role: 'user', content: message }]);
    setLoading(true);
    
    try {
      // Call LLM API
      const response = await chatWithLLM(
        message, 
        conversation.map(c => ({ role: c.role, content: c.content }))
      );
      
      // Add AI response to conversation
      setConversation(prev => [...prev, { 
        role: 'assistant', 
        content: response.response 
      }]);
    } catch (error) {
      console.error('Error sending message:', error);
      setConversation(prev => [...prev, { 
        role: 'assistant', 
        content: 'Sorry, I encountered an error. Please try again later.' 
      }]);
    } finally {
      setLoading(false);
      setMessage('');
    }
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const handleQuickQuestion = (question) => {
    setMessage(question);
  };

  return (
    <>
      {/* Floating button */}
      <Fab
        color="primary"
        aria-label="intelligent assistant"
        sx={{ position: 'fixed', bottom: 20, right: 20 }}
        onClick={handleOpen}
      >
        <SmartToyIcon />
      </Fab>

      {/* Dialog */}
      <Dialog
        open={open}
        onClose={handleClose}
        maxWidth="sm"
        fullWidth
        PaperProps={{
          sx: { height: '80vh', maxHeight: 600 }
        }}
      >
        <DialogTitle sx={{ pb: 1, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">Intelligent Assistant</Typography>
          <IconButton onClick={handleClose} size="small">
            <CloseIcon />
          </IconButton>
        </DialogTitle>
        
        <DialogContent dividers sx={{ p: 2, display: 'flex', flexDirection: 'column' }}>
          {/* Conversation history */}
          <Box sx={{ flexGrow: 1, overflow: 'auto', mb: 2 }}>
            {conversation.length === 0 ? (
              <Box sx={{ textAlign: 'center', mt: 4, color: 'text.secondary' }}>
                <SmartToyIcon sx={{ fontSize: 40, mb: 2, opacity: 0.7 }} />
                <Typography variant="body1">
                  Hello! I'm the intelligent assistant for the scheduling system. How can I help you today?
                </Typography>
              </Box>
            ) : (
              conversation.map((msg, index) => (
                <Box
                  key={index}
                  sx={{
                    display: 'flex',
                    justifyContent: msg.role === 'user' ? 'flex-end' : 'flex-start',
                    mb: 2
                  }}
                >
                  <Paper
                    elevation={1}
                    sx={{
                      p: 2,
                      maxWidth: '75%',
                      backgroundColor: msg.role === 'user' ? 'primary.light' : 'grey.100',
                      color: msg.role === 'user' ? 'white' : 'text.primary',
                      borderRadius: 2
                    }}
                  >
                    <Typography variant="body1">{msg.content}</Typography>
                  </Paper>
                </Box>
              ))
            )}
            {loading && (
              <Box sx={{ display: 'flex', justifyContent: 'flex-start', mb: 2 }}>
                <Paper elevation={1} sx={{ p: 2, backgroundColor: 'grey.100', borderRadius: 2 }}>
                  <CircularProgress size={20} thickness={5} sx={{ mr: 1 }} />
                  <Typography variant="body2" component="span">Thinking...</Typography>
                </Paper>
              </Box>
            )}
            <div ref={messageEndRef} />
          </Box>

          {/* Quick questions */}
          {conversation.length === 0 && (
            <Box sx={{ mb: 2 }}>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>Quick questions:</Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {quickQuestions.map((question, index) => (
                  <Chip
                    key={index}
                    label={question}
                    onClick={() => handleQuickQuestion(question)}
                    sx={{ cursor: 'pointer' }}
                  />
                ))}
              </Box>
            </Box>
          )}

          {/* Message input */}
          <Box sx={{ display: 'flex', alignItems: 'center', mt: 'auto' }}>
            <TextField
              fullWidth
              variant="outlined"
              placeholder="Type your question..."
              value={message}
              onChange={handleInputChange}
              onKeyPress={handleKeyPress}
              multiline
              maxRows={3}
              size="small"
              disabled={loading}
              sx={{ mr: 1 }}
            />
            <Button
              variant="contained"
              color="primary"
              endIcon={<SendIcon />}
              onClick={handleSendMessage}
              disabled={loading || message.trim() === ''}
            >
              Send
            </Button>
          </Box>
        </DialogContent>
      </Dialog>
    </>
  );
};

export default IntelligentAssistant;