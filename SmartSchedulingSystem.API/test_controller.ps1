# 测试TestController
$apiBaseUrl = "http://localhost:5192"
$encoding = [System.Text.Encoding]::UTF8

Write-Host "测试TestController基本功能..."
try {
    # 测试基本功能
    $pingUrl = "$apiBaseUrl/api/test/ping"
    $response = Invoke-RestMethod -Uri $pingUrl -Method Get -ErrorAction Stop
    Write-Host "Test控制器基本响应成功：" $response.message
} catch {
    Write-Host "Test控制器基本响应失败: $_"
}

Write-Host "向TestController发送排课请求..."
try {
    # 测试排课功能
    $scheduleUrl = "$apiBaseUrl/api/test/mock-schedule"
    $requestData = Get-Content -Path "large_request.json" -Encoding UTF8
    $response = Invoke-RestMethod -Uri $scheduleUrl -Method Post -Body $requestData -ContentType "application/json; charset=utf-8" -ErrorAction Stop
    
    # 保存响应内容到文件
    $response | ConvertTo-Json -Depth 10 | Out-File -FilePath "test_controller_response.json" -Encoding UTF8
    
    # 显示排课结果摘要
    if ($response.solutions -ne $null) {
        Write-Host "排课成功，生成了" $response.solutions.Count "个排课方案"
        if ($response.solutions.Count -gt 0) {
            $firstSolution = $response.solutions[0]
            Write-Host "第一个方案详情："
            Write-Host "  分数：" $firstSolution.score
            Write-Host "  算法：" $firstSolution.algorithmType
            Write-Host "  项目数：" $firstSolution.items.Count
        }
    } else {
        Write-Host "排课成功，但未返回排课方案"
    }
} catch {
    Write-Host "TestController排课请求失败: $_"
} 