# Test for advanced scheduling with Level 2 constraints
$ErrorActionPreference = "Stop"
Write-Host "Testing advanced scheduling..." -ForegroundColor Green

# API base URL
$baseUrl = "http://localhost:5192"

# First ping to make sure API is running
try {
    $pingResponse = Invoke-RestMethod -Uri "$baseUrl/api/schedule/ping" -Method Get
    Write-Host "API is available, response: $($pingResponse.message)" -ForegroundColor Green
}
catch {
    Write-Host "API is not available, please make sure the API is running" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}

# Create a raw JSON request directly to ensure all fields are included
$rawJsonRequest = @"
{
    "semesterId": 1,
    "generateMultipleSolutions": true,
    "solutionCount": 3,
    
    "courses": [],
    "teachers": [],
    "classrooms": [],
    "timeSlots": [],
    
    "teacherIds": [1, 2, 3, 4],
    "classroomIds": [1, 2, 9, 10],
    "timeSlotIds": [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
    "courseSectionIds": [1, 2, 3, 4],
    
    "courseSectionObjects": [
        {
            "id": 1,
            "courseCode": "CS101",
            "courseName": "Computer Introduction",
            "sectionCode": "A",
            "enrollment": 30
        },
        {
            "id": 2,
            "courseCode": "CS102",
            "courseName": "Programming Basics",
            "sectionCode": "A",
            "enrollment": 25
        },
        {
            "id": 3,
            "courseCode": "MATH101",
            "courseName": "Advanced Mathematics",
            "sectionCode": "A",
            "enrollment": 40
        },
        {
            "id": 4,
            "courseCode": "ENG101",
            "courseName": "College English",
            "sectionCode": "A",
            "enrollment": 35
        }
    ],
    
    "teacherObjects": [
        {
            "id": 1,
            "name": "Prof. Smith",
            "title": "Professor",
            "departmentId": 1
        },
        {
            "id": 2,
            "name": "Prof. Johnson",
            "title": "Associate Professor",
            "departmentId": 1
        },
        {
            "id": 3,
            "name": "Prof. Davis",
            "title": "Professor",
            "departmentId": 2
        },
        {
            "id": 4,
            "name": "Prof. Wilson",
            "title": "Lecturer",
            "departmentId": 3
        }
    ],
    
    "classroomObjects": [
        {
            "id": 1,
            "name": "101",
            "building": "Building A",
            "campusName": "Main Campus",
            "capacity": 40,
            "type": "Regular",
            "hasComputers": false,
            "hasProjector": true
        },
        {
            "id": 2,
            "name": "102",
            "building": "Building A",
            "campusName": "Main Campus",
            "capacity": 35,
            "type": "Regular",
            "hasComputers": false,
            "hasProjector": true
        },
        {
            "id": 9,
            "name": "501",
            "building": "Building C",
            "campusName": "Main Campus",
            "capacity": 50,
            "type": "Multimedia",
            "hasComputers": true,
            "hasProjector": true
        },
        {
            "id": 10,
            "name": "601",
            "building": "Building C",
            "campusName": "Main Campus",
            "capacity": 60,
            "type": "Multimedia",
            "hasComputers": true,
            "hasProjector": true
        }
    ],
    
    "timeSlotObjects": [
        {
            "id": 1,
            "dayOfWeek": 1,
            "dayName": "Monday",
            "startTime": "08:00",
            "endTime": "09:40"
        },
        {
            "id": 2,
            "dayOfWeek": 1,
            "dayName": "Monday",
            "startTime": "10:00",
            "endTime": "11:40"
        },
        {
            "id": 3,
            "dayOfWeek": 1,
            "dayName": "Monday",
            "startTime": "14:00",
            "endTime": "15:40"
        },
        {
            "id": 4,
            "dayOfWeek": 1,
            "dayName": "Monday",
            "startTime": "16:00",
            "endTime": "17:40"
        },
        {
            "id": 5,
            "dayOfWeek": 2,
            "dayName": "Tuesday",
            "startTime": "08:00",
            "endTime": "09:40"
        },
        {
            "id": 6,
            "dayOfWeek": 2,
            "dayName": "Tuesday",
            "startTime": "10:00",
            "endTime": "11:40"
        },
        {
            "id": 7,
            "dayOfWeek": 2,
            "dayName": "Tuesday",
            "startTime": "14:00",
            "endTime": "15:40"
        },
        {
            "id": 8,
            "dayOfWeek": 2,
            "dayName": "Tuesday",
            "startTime": "16:00",
            "endTime": "17:40"
        },
        {
            "id": 9,
            "dayOfWeek": 3,
            "dayName": "Wednesday",
            "startTime": "08:00",
            "endTime": "09:40"
        },
        {
            "id": 10,
            "dayOfWeek": 3,
            "dayName": "Wednesday",
            "startTime": "10:00",
            "endTime": "11:40"
        },
        {
            "id": 11,
            "dayOfWeek": 3,
            "dayName": "Wednesday",
            "startTime": "14:00",
            "endTime": "15:40"
        },
        {
            "id": 12,
            "dayOfWeek": 3,
            "dayName": "Wednesday",
            "startTime": "16:00",
            "endTime": "17:40"
        }
    ],
    
    "constraintSettings": {
        "enableHardConstraints": true,
        "enableSoftConstraints": true,
        "teacherAvailabilityWeight": 100,
        "classroomAvailabilityWeight": 100,
        "capacityConstraintWeight": 90,
        "teacherPreferenceWeight": 70,
        "classroomTypeWeight": 80
    }
}
"@

# Save the request to file for reference
$rawJsonRequest | Out-File "test_schedule_request.json" -Encoding utf8
Write-Host "Created test data file: test_schedule_request.json" -ForegroundColor Green

# Make the API call
Write-Host "Calling advanced scheduling API..." -ForegroundColor Green
try {
    Write-Host "Request body:" -ForegroundColor Yellow
    Write-Host $rawJsonRequest -ForegroundColor Yellow
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/schedule/generate-advanced" -Method Post -Body $rawJsonRequest -ContentType "application/json"
    
    # Save the response for reference
    $response | ConvertTo-Json -Depth 10 | Out-File "advanced_schedule_response.json" -Encoding utf8
    
    # Show a summary of the results
    Write-Host "Scheduling completed, generated $($response.totalSolutions) solutions" -ForegroundColor Green
    Write-Host "Best solution score: $($response.bestScore)" -ForegroundColor Green
    Write-Host "Average solution score: $($response.averageScore)" -ForegroundColor Green
    
    # Analyze the results to validate constraint handling
    if ($response.totalSolutions -gt 0) {
        $bestSolution = $response.solutions | Where-Object { $_.scheduleId -eq $response.primaryScheduleId }
        Write-Host "Best solution contains $($bestSolution.items.Count) assignments" -ForegroundColor Green
        
        # Check Smith's schedule to validate availability constraint
        $smithAssignments = $bestSolution.items | Where-Object { $_.teacherId -eq 1 }
        $mondayMorningAssignments = $smithAssignments | Where-Object { $_.dayOfWeek -eq 1 -and $_.startTime -eq "08:00" }
        
        if ($mondayMorningAssignments.Count -eq 0) {
            Write-Host "VALIDATION PASSED: Prof. Smith is not scheduled on Monday morning (unavailable time)" -ForegroundColor Green
        } else {
            Write-Host "VALIDATION FAILED: Prof. Smith is scheduled on Monday morning but should be unavailable" -ForegroundColor Red
            Write-Host "Assignment details: $($mondayMorningAssignments | ConvertTo-Json)" -ForegroundColor Red
        }
        
        # Check room 101 schedule to validate availability constraint
        $room101Assignments = $bestSolution.items | Where-Object { $_.classroomId -eq 1 }
        $mondayMorningRoom = $room101Assignments | Where-Object { $_.dayOfWeek -eq 1 -and $_.startTime -eq "08:00" }
        
        if ($mondayMorningRoom.Count -eq 0) {
            Write-Host "VALIDATION PASSED: Room 101 is not scheduled on Monday morning (maintenance time)" -ForegroundColor Green
        } else {
            Write-Host "VALIDATION FAILED: Room 101 is scheduled on Monday morning but should be unavailable" -ForegroundColor Red
            Write-Host "Assignment details: $($mondayMorningRoom | ConvertTo-Json)" -ForegroundColor Red
        }
        
        # Analyze unscheduled courses
        $scheduledCourses = $bestSolution.items | Select-Object -ExpandProperty courseSectionId -Unique
        $unscheduledIds = @(1, 2, 3, 4) | Where-Object { $scheduledCourses -notcontains $_ }
        
        if ($unscheduledIds.Count -gt 0) {
            Write-Host "$($unscheduledIds.Count) courses could not be scheduled:" -ForegroundColor Yellow
            foreach ($id in $unscheduledIds) {
                $courseInfo = ""
                if ($id -eq 1) { $courseInfo = "CS101 Computer Introduction" }
                if ($id -eq 2) { $courseInfo = "CS102 Programming Basics" }
                if ($id -eq 3) { $courseInfo = "MATH101 Advanced Mathematics" }
                if ($id -eq 4) { $courseInfo = "ENG101 College English" }
                Write-Host "  - $courseInfo" -ForegroundColor Yellow
            }
        } else {
            Write-Host "All courses were successfully scheduled" -ForegroundColor Green
        }
    } else {
        Write-Host "No scheduling solutions were generated" -ForegroundColor Red
        if ($response.errorMessage) {
            Write-Host "Error message: $($response.errorMessage)" -ForegroundColor Red
        }
    }
}
catch {
    Write-Host "API call failed" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    
    Write-Host "Detailed error information:" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $statusDescription = $_.Exception.Response.StatusDescription
        
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
        Write-Host "Status Description: $statusDescription" -ForegroundColor Red
        
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.BaseStream.Position = 0
            $reader.DiscardBufferedData()
            $responseBody = $reader.ReadToEnd()
            $reader.Close()
            
            Write-Host "Response Body: $responseBody" -ForegroundColor Red
        }
        catch {
            Write-Host "Could not read response body: $_" -ForegroundColor Red
        }
    }
    
    exit 1
}

Write-Host "Test completed" -ForegroundColor Green 