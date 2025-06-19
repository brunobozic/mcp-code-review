# GitLab + MCP Code Review Server Integration Guide

## Automatic Integration Options

### 1. GitLab Webhooks (Real-time Processing)

Configure GitLab to send webhooks to the MCP server when merge requests are created/updated:

**Setup Steps:**
1. **Configure Webhook in GitLab:**
   - Go to Project → Settings → Webhooks
   - URL: `http://your-mcp-server:5000/gitlab/webhook`
   - Trigger events: Merge request events
   - Secret token: Your webhook secret

2. **MCP Server Webhook Handler:**
   ```csharp
   [HttpPost("/gitlab/webhook")]
   public async Task<IActionResult> HandleGitLabWebhook([FromBody] GitLabWebhookPayload payload)
   {
       // Validate webhook signature
       if (!ValidateWebhookSignature(Request, payload)) 
           return Unauthorized();

       // Process merge request events
       if (payload.EventType == "merge_request" && payload.Action == "opened")
       {
           await ProcessMergeRequestAsync(payload.MergeRequest);
       }
       
       return Ok();
   }
   ```

3. **Automatic Code Review Process:**
   - Fetch changed files from GitLab API
   - Run multi-agent code review analysis
   - Post review comments back to merge request
   - Update MR status based on review results

### 2. GitLab CI/CD Pipeline Integration

Add MCP code review as a CI/CD pipeline step:

**`.gitlab-ci.yml` Configuration:**
```yaml
stages:
  - build
  - test
  - code-review
  - deploy

mcp-code-review:
  stage: code-review
  image: curlimages/curl:latest
  script:
    - |
      # Get changed files
      git diff --name-only $CI_MERGE_REQUEST_TARGET_BRANCH_SHA..$CI_COMMIT_SHA > changed_files.txt
      
      # Send each file for review
      while read file; do
        if [[ "$file" == *.cs ]] || [[ "$file" == *.js ]] || [[ "$file" == *.py ]]; then
          curl -X POST "$MCP_SERVER_URL/api/review" \
            -H "Content-Type: application/json" \
            -d "{
              \"fileName\": \"$file\",
              \"content\": \"$(cat $file | base64 -w 0)\",
              \"gitlabMrId\": \"$CI_MERGE_REQUEST_IID\",
              \"projectId\": \"$CI_PROJECT_ID\"
            }"
        fi
      done < changed_files.txt
  only:
    - merge_requests
  variables:
    MCP_SERVER_URL: "http://mcp-server:5000"
```

### 3. GitLab API Polling (Scheduled)

For environments where webhooks aren't feasible:

```csharp
// Background service that polls GitLab for new/updated MRs
public class GitLabPollingService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var newMRs = await _gitLabApi.GetNewMergeRequestsAsync();
            
            foreach (var mr in newMRs)
            {
                await ProcessMergeRequestAsync(mr);
            }
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

## Current Demo Integration

In our demo, we showed:
1. ✅ **Manual Integration**: Created GitLab project with merge request
2. ✅ **MCP Server Running**: Multi-agent code review system operational
3. 🔄 **Missing Link**: Automatic webhook/CI integration

## Next Steps for Full Automation

1. **Configure GitLab Webhook:**
   ```bash
   # Add webhook via GitLab API
   curl -X POST "http://localhost:8080/api/v4/projects/2/hooks" \
     -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
     -d "url=http://host.docker.internal:5000/gitlab/webhook" \
     -d "merge_requests_events=true"
   ```

2. **Implement Webhook Handler in MCP Server:**
   - Parse webhook payload
   - Extract changed files
   - Run code review analysis
   - Post results as MR comments

3. **Quality Gates:**
   - Block MR if critical security issues found
   - Require review approval for quality scores below threshold
   - Auto-approve simple changes with high confidence

## Demo Integration Flow

```
GitLab MR Created → Webhook → MCP Server → AI Analysis → Comment back to GitLab
      ↓                ↓           ↓             ↓              ↓
   Changed Files → HTTP POST → Multi-Agent → Results → GitLab API
```

This provides immediate, automated code review feedback without manual intervention.