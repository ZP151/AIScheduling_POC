# Read the response file
$response = Get-Content -Path "large_response.json" | ConvertFrom-Json

# Get the first solution
$solution = $response.solutions[0]

# Initialize counters
$teacherAssignments = @{}
$classroomAssignments = @{}
$dayAssignments = @{}
$timeSlotAssignments = @{}

# Process each item in the solution
foreach ($item in $solution.items) {
    # Count teacher assignments
    if ($teacherAssignments.ContainsKey($item.teacherId)) {
        $teacherAssignments[$item.teacherId]++
    } else {
        $teacherAssignments[$item.teacherId] = 1
    }
    
    # Count classroom assignments
    if ($classroomAssignments.ContainsKey($item.classroomId)) {
        $classroomAssignments[$item.classroomId]++
    } else {
        $classroomAssignments[$item.classroomId] = 1
    }
    
    # Count day assignments
    if ($dayAssignments.ContainsKey($item.dayOfWeek)) {
        $dayAssignments[$item.dayOfWeek]++
    } else {
        $dayAssignments[$item.dayOfWeek] = 1
    }
    
    # Count time slot assignments
    if ($timeSlotAssignments.ContainsKey($item.timeSlotId)) {
        $timeSlotAssignments[$item.timeSlotId]++
    } else {
        $timeSlotAssignments[$item.timeSlotId] = 1
    }
}

# Output analysis
Write-Host "Schedule Analysis"
Write-Host "======================================"

Write-Host "`nTeacher Workload Distribution:"
foreach ($teacherId in $teacherAssignments.Keys | Sort-Object) {
    $teacherName = ($solution.items | Where-Object { $_.teacherId -eq $teacherId } | Select-Object -First 1).teacherName
    Write-Host "  $teacherName (ID: $teacherId): $($teacherAssignments[$teacherId]) courses"
}

Write-Host "`nClassroom Usage:"
foreach ($classroomId in $classroomAssignments.Keys | Sort-Object) {
    $classroomName = ($solution.items | Where-Object { $_.classroomId -eq $classroomId } | Select-Object -First 1).classroomName
    Write-Host "  $classroomName (ID: $classroomId): $($classroomAssignments[$classroomId]) times"
}

Write-Host "`nDay of Week Distribution:"
foreach ($day in $dayAssignments.Keys | Sort-Object) {
    $dayName = ($solution.items | Where-Object { $_.dayOfWeek -eq $day } | Select-Object -First 1).dayName
    Write-Host "  $dayName (Day $day): $($dayAssignments[$day]) classes"
}

Write-Host "`nTime Slot Usage:"
foreach ($slotId in $timeSlotAssignments.Keys | Sort-Object) {
    $item = $solution.items | Where-Object { $_.timeSlotId -eq $slotId } | Select-Object -First 1
    Write-Host "  Slot $slotId ($($item.dayName) $($item.startTime)-$($item.endTime)): $($timeSlotAssignments[$slotId]) classes"
}

Write-Host "`n======================================"
Write-Host "Total Courses: $($solution.items.Count)"
Write-Host "Total Teachers: $($teacherAssignments.Keys.Count)"
Write-Host "Total Classrooms: $($classroomAssignments.Keys.Count)"
Write-Host "Total Days: $($dayAssignments.Keys.Count)"
Write-Host "Total Time Slots: $($timeSlotAssignments.Keys.Count)" 