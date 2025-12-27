/**
 * End-to-End Test: GitLab Webhook → Background AI Task Execution
 * 
 * This test follows proper methodology to verify that GitLab webhooks
 * actually trigger background AI review tasks (not just return success responses).
 * 
 * METHODOLOGY:
 * 1. Send webhook payload to MCP server
 * 2. Monitor for background task execution via logs and artifacts
 * 3. Verify AI review actually runs and produces results
 * 4. Validate complete end-to-end workflow
 */

const { test, expect } = require('@playwright/test');
const axios = require('axios');
const fs = require('fs').promises;
const path = require('path');

const MCP_SERVER_URL = 'http://localhost:5004';
const TEST_TIMEOUT = 60000; // 60 seconds for AI processing

test.describe('GitLab Webhook Background Task Verification', () => {
    
    test('webhook triggers background AI task with complete execution', async () => {
        // Clean up any existing test artifacts
        await cleanupTestArtifacts();
        
        const testPayload = {
            object_kind: 'merge_request',
            project: {
                id: 12345,
                name: 'e2e-test-project'
            },
            merge_request: {
                id: 67890,
                iid: 999,
                title: 'E2E Test: Security Vulnerability Fix',
                description: 'Testing complete webhook → AI review flow with security issues',
                source_branch: 'feature/security-test',
                target_branch: 'main',
                author: {
                    name: 'E2E Test User'
                }
            }
        };
        
        console.log('🧪 Starting E2E test: Webhook → Background AI Task');
        
        // STEP 1: Send REAL webhook payload to REAL MCP server
        console.log('📤 Sending REAL webhook payload to MCP server...');
        const webhookResponse = await axios.post(
            `${MCP_SERVER_URL}/api/gitlabwebhook`,
            testPayload,
            { 
                headers: { 'Content-Type': 'application/json' },
                timeout: 10000
            }
        );
        
        // STEP 2: Verify webhook was received successfully by REAL server
        expect(webhookResponse.status).toBe(200);
        expect(webhookResponse.data).toHaveProperty('status', 'success');
        console.log('✅ REAL webhook received successfully by MCP server');
        
        // STEP 3: Wait and monitor for background task execution
        console.log('⏳ Monitoring for background task execution...');
        
        let backgroundTaskStarted = false;
        let backgroundTaskCompleted = false;
        let startArtifactPath = null;
        let completionArtifactPath = null;
        
        // Monitor for up to 45 seconds for task completion
        const startTime = Date.now();
        while ((Date.now() - startTime) < 45000) {
            // Check for task start artifact
            if (!backgroundTaskStarted) {
                const artifacts = await findTestArtifacts(testPayload.project.id, testPayload.merge_request.iid);
                if (artifacts.start) {
                    backgroundTaskStarted = true;
                    startArtifactPath = artifacts.start;
                    console.log('🚀 Background task STARTED - artifact found:', startArtifactPath);
                }
            }
            
            // Check for task completion artifact
            if (backgroundTaskStarted && !backgroundTaskCompleted) {
                const artifacts = await findTestArtifacts(testPayload.project.id, testPayload.merge_request.iid);
                if (artifacts.completion) {
                    backgroundTaskCompleted = true;
                    completionArtifactPath = artifacts.completion;
                    console.log('✅ Background task COMPLETED - artifact found:', completionArtifactPath);
                    break;
                }
            }
            
            await new Promise(resolve => setTimeout(resolve, 1000)); // Wait 1 second
        }
        
        // STEP 4: Verify background task actually started
        expect(backgroundTaskStarted).toBe(true);
        console.log('✅ VERIFICATION: Background task started successfully');
        
        // STEP 5: Verify background task actually completed
        expect(backgroundTaskCompleted).toBe(true);
        console.log('✅ VERIFICATION: Background task completed successfully');
        
        // STEP 6: Validate completion artifact contains expected data
        const completionData = await fs.readFile(completionArtifactPath, 'utf8');
        const completionInfo = JSON.parse(completionData);
        
        expect(completionInfo).toHaveProperty('Status', 'COMPLETED');
        expect(completionInfo).toHaveProperty('ProjectId', testPayload.project.id);
        expect(completionInfo).toHaveProperty('MergeRequestIid', testPayload.merge_request.iid);
        expect(completionInfo).toHaveProperty('FindingsCount');
        expect(completionInfo).toHaveProperty('QualityScore');
        expect(completionInfo).toHaveProperty('AgentResults');
        
        console.log('📊 AI Analysis Results:', {
            findings: completionInfo.FindingsCount,
            qualityScore: completionInfo.QualityScore,
            agents: completionInfo.AgentResults
        });
        
        // STEP 7: Verify AI agents actually analyzed the content (STRICT EXPECTATIONS RESTORED)
        expect(completionInfo.FindingsCount).toBeGreaterThan(0);         // Must have findings
        expect(completionInfo.AgentResults).toBeGreaterThan(0);          // Must have agent results  
        expect(completionInfo.Status).toBe('COMPLETED');                 // Must complete successfully
        
        console.log('🎯 SUCCESS: Complete webhook → background AI task → results flow verified!');
        
        // Cleanup
        await cleanupTestArtifacts();
    });
});

async function findTestArtifacts(projectId, mergeRequestIid) {
    const tmpDir = '/tmp';
    const files = await fs.readdir(tmpDir);
    
    const startPattern = `mcp-background-task-${projectId}-${mergeRequestIid}-`;
    const completionPattern = `mcp-background-task-${projectId}-${mergeRequestIid}-completed.json`;
    
    let start = null;
    let completion = null;
    
    for (const file of files) {
        if (file.startsWith(startPattern) && file.endsWith('.json') && !file.includes('completed')) {
            start = path.join(tmpDir, file);
        }
        if (file === completionPattern) {
            completion = path.join(tmpDir, file);
        }
    }
    
    return { start, completion };
}

async function cleanupTestArtifacts() {
    try {
        const tmpDir = '/tmp';
        const files = await fs.readdir(tmpDir);
        
        for (const file of files) {
            if (file.startsWith('mcp-background-task-') && file.endsWith('.json')) {
                await fs.unlink(path.join(tmpDir, file));
            }
        }
    } catch (error) {
        // Ignore cleanup errors
    }
}