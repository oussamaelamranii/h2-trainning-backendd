param([string]$BaseUrl = 'http://127.0.0.1:5189')
$ErrorActionPreference = 'Stop'
$testHost = [Uri]$BaseUrl
if ($testHost.Host -notin @('127.0.0.1', 'localhost')) { throw 'This script is restricted to local test servers.' }
$script:checks = 0
function Assert($condition, [string]$label) {
    if (-not $condition) { throw "FAIL: $label" }
    $script:checks++
    Write-Output "PASS: $label"
}
function Call([string]$method, [string]$path, $body = $null, [string]$token = '', [int]$expected = 200) {
    $args = @{ Uri = "$BaseUrl/api/$path"; Method = $method; SkipHttpErrorCheck = $true; ContentType = 'application/json' }
    if ($token) { $args.Headers = @{ Authorization = "Bearer $token" } }
    if ($null -ne $body) { $args.Body = $body | ConvertTo-Json -Depth 15 }
    $response = Invoke-WebRequest @args
    if ($response.StatusCode -ne $expected) { throw "$method $path expected $expected, received $($response.StatusCode): $($response.Content)" }
    if ($response.Content) { return ($response.Content | ConvertFrom-Json) }
}
$runId = [Guid]::NewGuid().ToString('N').Substring(0, 10)
$password = 'FeatureTest123!'
$coach = Call POST 'auth/register' @{ fullName='Test coach'; email="coach-$runId@example.test"; password=$password; role='Coach' }
$other = Call POST 'auth/register' @{ fullName='Other coach'; email="other-$runId@example.test"; password=$password; role='Coach' }
$client = Call POST 'clients' @{ fullName='Test client'; email="client-$runId@example.test"; password=$password } $coach.token 201
$client2 = Call POST 'clients' @{ fullName='Second client'; email="second-$runId@example.test"; password=$password } $coach.token 201
$auth = Call POST 'auth/login' @{ email=$client.email; password=$password }
$auth2 = Call POST 'auth/login' @{ email=$client2.email; password=$password }
$id = $client.id
$update = @{ fullName='Updated client'; age=30; heightCm=180; weightKg=81; phoneNumber='0600000000'; goal='Build strength'; waistCm=85; armCm=34; thighCm=55; chestCm=98 }
$profile = Call PUT "profile/clients/$id" $update $coach.token
Assert ($profile.weightKg -eq 81 -and $profile.bmi -eq 25 -and $profile.maxHeartRate -eq 190 -and $profile.fullName -eq 'Updated client') 'Coach edits weight, name and metrics; BMI/HR recalculate'
$mine = Call GET 'profile' $null $auth.token
Assert ($mine.weightKg -eq 81 -and $mine.phoneNumber -eq '0600000000') 'Client receives saved coach changes'
$list = Call GET 'clients' $null $coach.token
Assert (($list | Where-Object id -eq $id).lastWeight -eq 81) 'Coach client list uses latest coach weight'
$null = Call PUT "profile/clients/$id" $update $other.token 404
$null = Call PUT "profile/clients/$id" $update $auth.token 403
$update.weightKg = -1
$null = Call PUT "profile/clients/$id" $update $coach.token 400
Assert $true 'Ownership, role and weight validation reject invalid edits'
$null = Call POST 'checkins' @{ weight=79; sleepQuality=4; energyLevel=4; mood=4 } $auth.token 201
$mine = Call GET 'profile' $null $auth.token
Assert ($mine.weightKg -eq 79) 'Newer client check-in supersedes coach weight'
$update.weightKg = 80
$null = Call PUT "profile/clients/$id" $update $coach.token
$mine = Call GET 'profile' $null $auth.token
Assert ($mine.weightKg -eq 80) 'Newer coach entry supersedes check-in without overwriting history'
$update.email = "updated-$runId@example.test"
$null = Call PUT "profile/clients/$id" $update $coach.token
$auth = Call POST 'auth/login' @{email=$update.email;password=$password}
Assert ($auth.user.email -eq $update.email) 'Coach email edit also updates client login'
$update.email = $client2.email
$null = Call PUT "profile/clients/$id" $update $coach.token 400
Assert $true 'Duplicate email is rejected without changing client account'
$message = Call POST "communication/clients/$id/messages" @{ content='Keep going! Next session: focus on technique.' } $coach.token
$notifications = Call GET 'communication/notifications' $null $auth.token
Assert ($notifications.unreadMessages -eq 1) 'Direct message appears as unread on login'
$auth = Call POST 'auth/login' @{email="updated-$runId@example.test";password=$password}
$notifications = Call GET 'communication/notifications' $null $auth.token
Assert ($notifications.unreadMessages -eq 1) 'Unread messages survive a new login session'
$inbox = @(Call GET 'communication/messages' $null $auth.token)
Assert ($inbox.Count -eq 1 -and $inbox[0].content -eq $message.content) 'Client can read coach message'
$null = Call POST "communication/messages/$($message.id)/read" $null $auth2.token 204
$notifications = Call GET 'communication/notifications' $null $auth.token
Assert ($notifications.unreadMessages -eq 1) 'Another client cannot mark this message read'
$null = Call POST "communication/messages/$($message.id)/read" $null $auth.token 204
$null = Call POST "communication/messages/$($message.id)/read" $null $auth.token 204
$history = @(Call GET "communication/clients/$id/messages" $null $coach.token)
Assert ($history[0].readAt) 'Read receipt is persisted and idempotent'
$null = Call POST "communication/clients/$id/messages" @{content='Forbidden'} $other.token 404
$null = Call POST "communication/clients/$id/messages" @{content='   '} $coach.token 400
Assert $true 'Message ownership and empty-message validation enforced'
$post = Call POST 'posts' @{ title='New advice'; content='Hydrate and recover'; category='Recovery' } $coach.token 201
$foreignPost = Call POST 'posts' @{ title='Other advice'; content='Not your coach'; category='General' } $other.token 201
$notifications = Call GET 'communication/notifications' $null $auth.token
Assert ($post.id -in $notifications.unreadPostIds -and $foreignPost.id -notin $notifications.unreadPostIds) 'Only own coach advice triggers unread notification'
$null = Call POST "communication/posts/$($post.id)/read" $null $auth.token 204
$null = Call POST "communication/posts/$($post.id)/read" $null $auth.token 204
$notifications = Call GET 'communication/notifications' $null $auth.token
Assert ($post.id -notin $notifications.unreadPostIds -and $notifications.unreadMessages -eq 0) 'Read notifications clear and remain cleared'
$null = Call POST "communication/posts/$($foreignPost.id)/read" $null $auth.token 404
$program = Call POST 'programs' @{title='Verification program'; days=@(@{name='Strength';dayNumber=1;exercises=@(@{name='Squat';sets=3;reps='10'});meals=@()})} $coach.token 201
$assignment = Call POST 'assignments' @{ clientId=$id; programId=$program.id } $coach.token
$before = Call GET 'profile' $null $auth.token
$exerciseId = $program.days[0].exercises[0].id
$null = Call POST "assignments/$($assignment.id)/exercises/$exerciseId/log" @{ isCompleted=$true;weight=30 } $auth.token
$after = Call GET 'profile' $null $auth.token
Assert ($after.progress.overallScore -gt $before.progress.overallScore -and $after.progress.programCompletionPercent -eq 100) 'Program activity updates the shared progress score'
$dashboard = Call GET 'dashboard/client' $null $auth.token
Assert ($dashboard.currentProgram.id -eq $program.id) 'Client dashboard still loads assigned program'
$null = Call DELETE "posts/$($post.id)" $null $coach.token 204
Assert $true 'Deleting advice with read receipts still works'
$null = Call DELETE "clients/$id" $null $coach.token 204
Assert $true 'Deleting client with messages, receipts and weights still works'
Write-Output "All $script:checks integration checks passed. Test accounts remain only in the local verification database."
