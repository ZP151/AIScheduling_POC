try {
    $content = Get-Content -Path "correct_request.json" -Raw
    $response = Invoke-RestMethod -Uri "http://localhost:5192/api/Schedule/generate" -Method POST -Body $content -ContentType "application/json"
    $response | ConvertTo-Json -Depth 10 > "schedule_response.json"
    Write-Host "响应已保存到 schedule_response.json"
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