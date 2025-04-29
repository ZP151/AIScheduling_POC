#!/usr/bin/env python
"""
Simple script to start the LLM API service.
This allows easy startup with just 'python run_api.py'
"""

import os
import sys
import subprocess
import platform
import signal

def check_port(port):
    """Check if the port is in use and try to free it if possible."""
    import socket
    
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        s.bind(('0.0.0.0', port))
        s.close()
        return True  # Port is available
    except socket.error:
        print(f"Port {port} is already in use. Attempting to free it...")
        
        # Try to find and terminate the process using the port
        if platform.system() == "Windows":
            try:
                # For Windows
                output = subprocess.check_output(f"netstat -ano | findstr :{port}", shell=True).decode()
                if output:
                    # Extract PID
                    lines = output.strip().split("\n")
                    for line in lines:
                        if "LISTENING" in line:
                            pid = line.strip().split()[-1]
                            try:
                                print(f"Terminating process with PID {pid}...")
                                subprocess.call(f"taskkill /PID {pid} /F", shell=True)
                                return True
                            except Exception as e:
                                print(f"Failed to terminate process: {e}")
            except Exception as e:
                print(f"Could not check port on Windows: {e}")
        else:
            try:
                # For Linux/Mac
                output = subprocess.check_output(f"lsof -i :{port} | grep LISTEN", shell=True).decode()
                if output:
                    # Extract PID
                    pid = output.strip().split()[1]
                    try:
                        print(f"Terminating process with PID {pid}...")
                        os.kill(int(pid), signal.SIGTERM)
                        return True
                    except Exception as e:
                        print(f"Failed to terminate process: {e}")
            except Exception as e:
                print(f"Could not check port on Linux/Mac: {e}")
    
    return False

def parse_arguments():
    """Parse command line arguments."""
    import argparse
    parser = argparse.ArgumentParser(description='Start the LLM API service')
    parser.add_argument('--port', type=int, default=8080, help='Port to run the service on')
    parser.add_argument('--host', type=str, default='0.0.0.0', help='Host to bind the service to')
    parser.add_argument('--no-reload', action='store_true', help='Disable hot reloading')
    return parser.parse_args()

def main():
    """Start the FastAPI server using uvicorn."""
    args = parse_arguments()
    port = args.port
    host = args.host
    reload = not args.no_reload
    
    # Check if port is available, attempt to free it if not
    check_port(port)
    
    print(f"Starting LLM API service on http://{host}:{port}")
    print("Press CTRL+C to quit")
    
    try:
        import uvicorn
        # Start uvicorn with the application
        uvicorn.run(
            "llm_api:app", 
            host=host, 
            port=port, 
            reload=reload,
            log_level="info"
        )
    except ImportError:
        print("Error: uvicorn is not installed. Please install it with: pip install uvicorn")
        return 1
    except KeyboardInterrupt:
        print("\nServer stopped manually")
    except Exception as e:
        print(f"Error starting server: {e}")
        
        # Suggest using a different port if permission error
        if "Permission denied" in str(e) or "access denied" in str(e).lower():
            print("\nTry using a different port or running as administrator:")
            print(f"python run_api.py --port 8081")
        return 1
    
    return 0

if __name__ == "__main__":
    sys.exit(main()) 