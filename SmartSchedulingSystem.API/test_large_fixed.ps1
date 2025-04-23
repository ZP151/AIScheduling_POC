# Record start time
$startTime = Get-Date

# Read request data
$content = Get-Content -Path "large_request.json" -Raw

# Send request
Write-Host "Sending large scheduling request..."
$response = Invoke-RestMethod -Uri "http://localhost:5192/api/Schedule/generate" -Method POST -Body $content -ContentType "application/json"

# Calculate processing time
$endTime = Get-Date
$processingTime = ($endTime - $startTime).TotalSeconds

# Save response
$response | ConvertTo-Json -Depth 10 > "large_response.json"
Write-Host "Response saved to large_response.json"

# Output statistics
Write-Host "Scheduling Statistics:"
Write-Host "----------------------------------------"
Write-Host "Total processing time: $processingTime seconds"
Write-Host "Number of solutions: $($response.totalSolutions)"
Write-Host "Best score: $($response.bestScore)"
Write-Host "Items in first solution: $($response.solutions[0].items.Count)"
Write-Host "Algorithm type: $($response.solutions[0].algorithmType)"
Write-Host "Algorithm execution time: $($response.solutions[0].executionTimeMs) ms" 