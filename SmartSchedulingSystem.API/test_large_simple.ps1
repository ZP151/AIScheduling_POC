# 记录开始时间
$startTime = Get-Date

# 读取请求数据
$content = Get-Content -Path "large_request.json" -Raw

# 发送请求
Write-Host "开始发送大规模排课请求..."
$response = Invoke-RestMethod -Uri "http://localhost:5192/api/Schedule/generate" -Method POST -Body $content -ContentType "application/json"

# 计算处理时间
$endTime = Get-Date
$processingTime = ($endTime - $startTime).TotalSeconds

# 保存响应结果
$response | ConvertTo-Json -Depth 10 > "large_response.json"
Write-Host "响应已保存到 large_response.json"

# 输出统计信息
Write-Host "排课统计信息:"
Write-Host "----------------------------------------"
Write-Host "总处理时间: $processingTime 秒"
Write-Host "生成的方案数量: $($response.totalSolutions)"
Write-Host "最佳评分: $($response.bestScore)"
Write-Host "每个方案的排课项目数量: $($response.solutions[0].items.Count)"
Write-Host "算法类型: $($response.solutions[0].algorithmType)"
Write-Host "算法执行时间: $($response.solutions[0].executionTimeMs) 毫秒" 