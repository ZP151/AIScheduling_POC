$jsonContent = @'
{
  "request": {
    "semesterId": 1,
    "semesterName": "2023-2024学年第一学期",
    "algorithmType": "CP",
    "generateMultipleSolutions": true,
    "solutionCount": 3,
    
    "courseSectionObjects": [
      {
        "id": 1,
        "courseCode": "CS101",
        "courseName": "计算机导论",
        "sectionCode": "A01",
        "enrollment": 40
      },
      {
        "id": 2,
        "courseCode": "CS201",
        "courseName": "数据结构",
        "sectionCode": "A01",
        "enrollment": 35
      },
      {
        "id": 3,
        "courseCode": "CS301",
        "courseName": "算法设计",
        "sectionCode": "A01",
        "enrollment": 30
      }
    ],
    "teacherObjects": [
      {
        "id": 1,
        "name": "张教授",
        "title": "教授",
        "departmentId": 1
      },
      {
        "id": 2,
        "name": "李副教授",
        "title": "副教授",
        "departmentId": 1
      }
    ],
    "classroomObjects": [
      {
        "id": 1,
        "name": "101教室",
        "building": "主教学楼",
        "capacity": 50,
        "type": "普通教室",
        "hasComputers": false,
        "hasProjector": true,
        "campusName": "主校区"
      },
      {
        "id": 2,
        "name": "201教室",
        "building": "主教学楼",
        "capacity": 60,
        "type": "多媒体教室",
        "hasComputers": true,
        "hasProjector": true,
        "campusName": "主校区"
      }
    ],
    "timeSlotObjects": [
      {
        "id": 1,
        "dayOfWeek": 1,
        "dayName": "周一",
        "startTime": "08:00",
        "endTime": "09:40"
      },
      {
        "id": 2,
        "dayOfWeek": 1,
        "dayName": "周一",
        "startTime": "10:00",
        "endTime": "11:40"
      },
      {
        "id": 3,
        "dayOfWeek": 2,
        "dayName": "周二",
        "startTime": "08:00",
        "endTime": "09:40"
      },
      {
        "id": 4,
        "dayOfWeek": 2,
        "dayName": "周二",
        "startTime": "10:00",
        "endTime": "11:40"
      },
      {
        "id": 5,
        "dayOfWeek": 3,
        "dayName": "周三",
        "startTime": "08:00",
        "endTime": "09:40"
      },
      {
        "id": 6,
        "dayOfWeek": 3,
        "dayName": "周三",
        "startTime": "10:00",
        "endTime": "11:40"
      }
    ],
    
    "courses": [
      {
        "id": 1,
        "code": "CS101",
        "name": "计算机导论",
        "credits": 3
      },
      {
        "id": 2,
        "code": "CS201",
        "name": "数据结构",
        "credits": 4
      },
      {
        "id": 3,
        "code": "CS301",
        "name": "算法设计",
        "credits": 4
      }
    ],
    "teachers": [
      {
        "id": 1,
        "name": "张教授",
        "title": "教授",
        "departmentId": 1
      },
      {
        "id": 2,
        "name": "李副教授",
        "title": "副教授",
        "departmentId": 1
      }
    ],
    "classrooms": [
      {
        "id": 1,
        "name": "101教室",
        "building": "主教学楼",
        "capacity": 50,
        "type": "普通教室",
        "hasComputers": false,
        "hasProjector": true,
        "campusName": "主校区"
      },
      {
        "id": 2,
        "name": "201教室",
        "building": "主教学楼",
        "capacity": 60,
        "type": "多媒体教室",
        "hasComputers": true,
        "hasProjector": true,
        "campusName": "主校区"
      }
    ],
    "timeSlots": [
      {
        "id": 1,
        "dayOfWeek": 1,
        "dayName": "周一",
        "startTime": "08:00",
        "endTime": "09:40"
      },
      {
        "id": 2,
        "dayOfWeek": 1,
        "dayName": "周一",
        "startTime": "10:00",
        "endTime": "11:40"
      },
      {
        "id": 3,
        "dayOfWeek": 2,
        "dayName": "周二",
        "startTime": "08:00",
        "endTime": "09:40"
      },
      {
        "id": 4,
        "dayOfWeek": 2,
        "dayName": "周二",
        "startTime": "10:00",
        "endTime": "11:40"
      },
      {
        "id": 5,
        "dayOfWeek": 3,
        "dayName": "周三",
        "startTime": "08:00",
        "endTime": "09:40"
      },
      {
        "id": 6,
        "dayOfWeek": 3,
        "dayName": "周三",
        "startTime": "10:00",
        "endTime": "11:40"
      }
    ],
    
    "teacherIds": [1, 2],
    "timeSlotIds": [1, 2, 3, 4, 5, 6],
    "classroomIds": [1, 2],
    "courseSectionIds": [1, 2, 3],
    
    "constraintSettings": {
      "enableTeacherConflict": true,
      "enableClassroomConflict": true,
      "enableClassroomCapacity": true,
      "enableTeacherAvailability": true,
      "enableClassroomAvailability": true,
      "enableTeacherPreference": true,
      "enableClassroomTypeMatch": true
    }
  }
}
'@

# 以UTF-8格式保存到文件
$jsonContent | Out-File -FilePath "clean_request.json" -Encoding utf8

Write-Host "JSON文件已保存为clean_request.json"

# 创建新的测试脚本
$scriptContent = @'
$jsonPath = "clean_request.json"
$content = Get-Content -Path $jsonPath -Raw
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5192/api/Schedule/generate" -Method POST -Body $content -ContentType "application/json"
    $response | ConvertTo-Json -Depth 10 > "schedule_response.json"
    Write-Host "响应已保存到 schedule_response.json"
} catch {
    Write-Host "错误: $_"
    Write-Host "响应内容:" 
    $_.Exception.Response.GetResponseStream() | ForEach-Object {
        $reader = New-Object System.IO.StreamReader($_)
        $reader.BaseStream.Position = 0
        $reader.ReadToEnd()
    }
}
'@

$scriptContent | Out-File -FilePath "test_clean_request.ps1" -Encoding utf8

Write-Host "测试脚本已保存为test_clean_request.ps1" 