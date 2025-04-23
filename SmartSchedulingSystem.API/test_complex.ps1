try {
    $content = Get-Content -Path "fixed_updated_request.json" -Raw
    $response = Invoke-RestMethod -Uri "http://localhost:5192/api/Schedule/generate" -Method POST -Body $content -ContentType "application/json"
    $response | ConvertTo-Json -Depth 10 > "complex_response.json"
    Write-Host "响应已保存到 complex_response.json"
    
    # 打印一些基本统计信息
    if ($response -ne $null) {
        Write-Host "排课成功! 生成了 $($response.totalSolutions) 个解决方案。"
        Write-Host "最佳评分: $($response.bestScore)"
        Write-Host "首个方案包含 $($response.solutions[0].items.Count) 个排课项目。"
    }
} catch {
    Write-Host "错误: $_"
    Write-Host "响应内容:" 
    try {
        $_.Exception.Response.GetResponseStream() | ForEach-Object {
            $reader = New-Object System.IO.StreamReader($_)
            $reader.BaseStream.Position = 0
            $reader.ReadToEnd()
        }
    } catch {
        Write-Host "无法获取响应详情"
    }
} 