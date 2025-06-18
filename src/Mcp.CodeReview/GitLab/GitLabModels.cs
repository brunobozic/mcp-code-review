using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mcp.CodeReview.GitLab
{
    /// <summary>
    /// GitLab webhook payload structure
    /// </summary>
    public class GitLabWebhookPayload
    {
        [JsonPropertyName("object_kind")]
        public string? EventType { get; set; }

        [JsonPropertyName("event_type")]
        public string? EventAction { get; set; }

        [JsonPropertyName("user")]
        public GitLabUser? User { get; set; }

        [JsonPropertyName("project")]
        public GitLabProject? Project { get; set; }

        [JsonPropertyName("object_attributes")]
        public GitLabObjectAttributes? ObjectAttributes { get; set; }

        [JsonPropertyName("merge_request")]
        public GitLabMergeRequest? MergeRequest { get; set; }

        [JsonPropertyName("repository")]
        public GitLabRepository? Repository { get; set; }

        [JsonPropertyName("ref")]
        public string? Ref { get; set; }

        [JsonPropertyName("commits")]
        public List<GitLabCommit>? Commits { get; set; }
    }

    /// <summary>
    /// GitLab project information
    /// </summary>
    public class GitLabProject
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("web_url")]
        public string? WebUrl { get; set; }

        [JsonPropertyName("path_with_namespace")]
        public string? PathWithNamespace { get; set; }

        [JsonPropertyName("default_branch")]
        public string? DefaultBranch { get; set; }

        [JsonPropertyName("visibility")]
        public string? Visibility { get; set; }
    }

    /// <summary>
    /// GitLab user information
    /// </summary>
    public class GitLabUser
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
    }

    /// <summary>
    /// GitLab merge request information
    /// </summary>
    public class GitLabMergeRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("iid")]
        public int Iid { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("source_branch")]
        public string? SourceBranch { get; set; }

        [JsonPropertyName("target_branch")]
        public string? TargetBranch { get; set; }

        [JsonPropertyName("author")]
        public GitLabUser? Author { get; set; }

        [JsonPropertyName("assignee")]
        public GitLabUser? Assignee { get; set; }

        [JsonPropertyName("assignees")]
        public List<GitLabUser>? Assignees { get; set; }

        [JsonPropertyName("reviewers")]
        public List<GitLabUser>? Reviewers { get; set; }

        [JsonPropertyName("source_project_id")]
        public int SourceProjectId { get; set; }

        [JsonPropertyName("target_project_id")]
        public int TargetProjectId { get; set; }

        [JsonPropertyName("web_url")]
        public string? WebUrl { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("work_in_progress")]
        public bool WorkInProgress { get; set; }

        [JsonPropertyName("merge_status")]
        public string? MergeStatus { get; set; }

        [JsonPropertyName("detailed_merge_status")]
        public string? DetailedMergeStatus { get; set; }

        [JsonPropertyName("changes_count")]
        public string? ChangesCount { get; set; }

        [JsonPropertyName("user_notes_count")]
        public int UserNotesCount { get; set; }

        [JsonPropertyName("upvotes")]
        public int Upvotes { get; set; }

        [JsonPropertyName("downvotes")]
        public int Downvotes { get; set; }

        [JsonPropertyName("source")]
        public GitLabBranch? Source { get; set; }

        [JsonPropertyName("target")]
        public GitLabBranch? Target { get; set; }

        [JsonPropertyName("last_commit")]
        public GitLabCommit? LastCommit { get; set; }

        [JsonPropertyName("labels")]
        public List<GitLabLabel>? Labels { get; set; }
    }

    /// <summary>
    /// GitLab object attributes (generic)
    /// </summary>
    public class GitLabObjectAttributes
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("target_branch")]
        public string? TargetBranch { get; set; }

        [JsonPropertyName("source_branch")]
        public string? SourceBranch { get; set; }

        [JsonPropertyName("author_id")]
        public int AuthorId { get; set; }

        [JsonPropertyName("assignee_id")]
        public int? AssigneeId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("merge_status")]
        public string? MergeStatus { get; set; }

        [JsonPropertyName("detailed_merge_status")]
        public string? DetailedMergeStatus { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("work_in_progress")]
        public bool WorkInProgress { get; set; }

        // Pipeline specific
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("stage")]
        public string? Stage { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

        [JsonPropertyName("finished_at")]
        public DateTime? FinishedAt { get; set; }
    }

    /// <summary>
    /// GitLab branch information
    /// </summary>
    public class GitLabBranch
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("commit")]
        public GitLabCommit? Commit { get; set; }

        [JsonPropertyName("protected")]
        public bool Protected { get; set; }
    }

    /// <summary>
    /// GitLab commit information
    /// </summary>
    public class GitLabCommit
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("author")]
        public GitLabCommitAuthor? Author { get; set; }

        [JsonPropertyName("added")]
        public List<string>? Added { get; set; }

        [JsonPropertyName("modified")]
        public List<string>? Modified { get; set; }

        [JsonPropertyName("removed")]
        public List<string>? Removed { get; set; }
    }

    /// <summary>
    /// GitLab commit author information
    /// </summary>
    public class GitLabCommitAuthor
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    /// <summary>
    /// GitLab repository information
    /// </summary>
    public class GitLabRepository
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("homepage")]
        public string? Homepage { get; set; }

        [JsonPropertyName("git_http_url")]
        public string? GitHttpUrl { get; set; }

        [JsonPropertyName("git_ssh_url")]
        public string? GitSshUrl { get; set; }

        [JsonPropertyName("visibility_level")]
        public int VisibilityLevel { get; set; }
    }

    /// <summary>
    /// GitLab file change information
    /// </summary>
    public class GitLabFileChange
    {
        [JsonPropertyName("old_path")]
        public string? OldPath { get; set; }

        [JsonPropertyName("new_path")]
        public string? NewPath { get; set; }

        [JsonPropertyName("a_mode")]
        public string? AMode { get; set; }

        [JsonPropertyName("b_mode")]
        public string? BMode { get; set; }

        [JsonPropertyName("diff")]
        public string? Diff { get; set; }

        [JsonPropertyName("new_file")]
        public bool NewFile { get; set; }

        [JsonPropertyName("renamed_file")]
        public bool RenamedFile { get; set; }

        [JsonPropertyName("deleted_file")]
        public bool DeletedFile { get; set; }
    }

    /// <summary>
    /// GitLab merge request with changes
    /// </summary>
    public class GitLabMergeRequestWithChanges : GitLabMergeRequest
    {
        [JsonPropertyName("changes")]
        public List<GitLabFileChange>? Changes { get; set; }
    }

    /// <summary>
    /// GitLab label information
    /// </summary>
    public class GitLabLabel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// GitLab diff note for posting comments on specific lines
    /// </summary>
    public class GitLabDiffNote
    {
        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("position")]
        public GitLabDiffPosition? Position { get; set; }
    }

    /// <summary>
    /// GitLab diff position for line comments
    /// </summary>
    public class GitLabDiffPosition
    {
        [JsonPropertyName("base_sha")]
        public string? BaseSha { get; set; }

        [JsonPropertyName("start_sha")]
        public string? StartSha { get; set; }

        [JsonPropertyName("head_sha")]
        public string? HeadSha { get; set; }

        [JsonPropertyName("old_path")]
        public string? OldPath { get; set; }

        [JsonPropertyName("new_path")]
        public string? NewPath { get; set; }

        [JsonPropertyName("position_type")]
        public string PositionType { get; set; } = "text";

        [JsonPropertyName("old_line")]
        public int? OldLine { get; set; }

        [JsonPropertyName("new_line")]
        public int? NewLine { get; set; }

        [JsonPropertyName("line_range")]
        public GitLabLineRange? LineRange { get; set; }
    }

    /// <summary>
    /// GitLab line range for multi-line comments
    /// </summary>
    public class GitLabLineRange
    {
        [JsonPropertyName("start")]
        public GitLabLinePosition? Start { get; set; }

        [JsonPropertyName("end")]
        public GitLabLinePosition? End { get; set; }
    }

    /// <summary>
    /// GitLab line position
    /// </summary>
    public class GitLabLinePosition
    {
        [JsonPropertyName("line_code")]
        public string? LineCode { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("old_line")]
        public int? OldLine { get; set; }

        [JsonPropertyName("new_line")]
        public int? NewLine { get; set; }
    }

    /// <summary>
    /// GitLab webhook configuration
    /// </summary>
    public class GitLabWebhookConfig
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("merge_requests_events")]
        public bool MergeRequestsEvents { get; set; } = true;

        [JsonPropertyName("push_events")]
        public bool PushEvents { get; set; } = true;

        [JsonPropertyName("issues_events")]
        public bool IssuesEvents { get; set; } = false;

        [JsonPropertyName("confidential_issues_events")]
        public bool ConfidentialIssuesEvents { get; set; } = false;

        [JsonPropertyName("tag_push_events")]
        public bool TagPushEvents { get; set; } = false;

        [JsonPropertyName("note_events")]
        public bool NoteEvents { get; set; } = false;

        [JsonPropertyName("pipeline_events")]
        public bool PipelineEvents { get; set; } = true;

        [JsonPropertyName("wiki_page_events")]
        public bool WikiPageEvents { get; set; } = false;

        [JsonPropertyName("deployment_events")]
        public bool DeploymentEvents { get; set; } = false;

        [JsonPropertyName("job_events")]
        public bool JobEvents { get; set; } = false;

        [JsonPropertyName("release_events")]
        public bool ReleaseEvents { get; set; } = false;

        [JsonPropertyName("enable_ssl_verification")]
        public bool EnableSslVerification { get; set; } = false;

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("push_events_branch_filter")]
        public string? PushEventsBranchFilter { get; set; }
    }
}