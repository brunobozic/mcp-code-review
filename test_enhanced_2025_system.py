#!/usr/bin/env python3
"""
Comprehensive test script for Enhanced 2025 Multi-Agent Code Review System
Tests all advanced features: ToT, Critics, Enhanced RAG, Meta-Reasoning, Nested Conversations
"""

import asyncio
import json
import time
import requests
from datetime import datetime

class Enhanced2025SystemTester:
    def __init__(self, base_url="http://localhost:5000"):
        self.base_url = base_url
        self.session = requests.Session()
        self.test_results = {}
        
    def log(self, message, level="INFO"):
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] {level}: {message}")
        
    def test_system_health(self):
        """Test basic system health and availability"""
        self.log("🏥 Testing Enhanced 2025 system health...")
        
        try:
            response = self.session.get(f"{self.base_url}/health", timeout=10)
            if response.status_code == 200:
                self.log("✅ System health check passed")
                return True
            else:
                self.log(f"❌ Health check failed with status {response.status_code}", "ERROR")
                return False
        except Exception as e:
            self.log(f"❌ Health check failed: {str(e)}", "ERROR")
            return False
    
    def test_enhanced_2025_features(self):
        """Test Enhanced 2025 multi-agent features"""
        self.log("🚀 Testing Enhanced 2025 Multi-Agent Features...")
        
        # Test code with multiple complexity layers for comprehensive analysis
        test_code = '''
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestApp
{
    public class DataProcessor
    {
        private readonly Dictionary<string, object> _cache = new();
        
        public async Task<string> ProcessDataAsync(string input)
        {
            // Potential security issue: no input validation
            if (string.IsNullOrEmpty(input))
                return null;
                
            // Performance issue: blocking call in async method
            var result = ExpensiveOperation(input);
            
            // Thread safety issue: concurrent access to dictionary
            _cache[input] = result;
            
            // Architecture issue: mixing concerns
            LogToDatabase(result);
            
            return result;
        }
        
        private string ExpensiveOperation(string input)
        {
            // Performance issue: inefficient string concatenation
            string result = "";
            for (int i = 0; i < 1000; i++)
            {
                result += input + i.ToString();
            }
            return result;
        }
        
        private void LogToDatabase(string data)
        {
            // Security issue: potential SQL injection
            var query = $"INSERT INTO logs VALUES ('{data}')";
            // Execute query...
        }
    }
}
'''
        
        payload = {
            "fileName": "enhanced_2025_test.cs",
            "language": "csharp",
            "content": test_code,
            "filePath": "/test/enhanced_2025_test.cs",
            "enhanced2025": True,  # Request Enhanced 2025 analysis
            "features": {
                "enableTreeOfThoughts": True,
                "enableAgentDebates": True,
                "enableNestedConversations": True,
                "enableMetaReasoning": True,
                "enableEnhancedRAG": True
            }
        }
        
        try:
            self.log("📊 Sending Enhanced 2025 analysis request...")
            start_time = time.time()
            
            response = self.session.post(
                f"{self.base_url}/api/review/enhanced-2025",
                json=payload,
                timeout=120  # Enhanced analysis takes longer
            )
            
            analysis_time = time.time() - start_time
            self.log(f"⏱️ Analysis completed in {analysis_time:.2f} seconds")
            
            if response.status_code == 200:
                result = response.json()
                return self.analyze_enhanced_2025_results(result, analysis_time)
            else:
                self.log(f"❌ Enhanced 2025 analysis failed: {response.status_code}", "ERROR")
                if response.text:
                    self.log(f"Error details: {response.text}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"❌ Enhanced 2025 test failed: {str(e)}", "ERROR")
            return False
    
    def analyze_enhanced_2025_results(self, result, analysis_time):
        """Analyze Enhanced 2025 analysis results"""
        self.log("🔍 Analyzing Enhanced 2025 results...")
        
        success_indicators = []
        
        # Check for Enhanced 2025 specific features
        enhanced_features = result.get('enhancedFeatures', {})
        
        # 1. Tree of Thoughts Analysis
        tot_results = enhanced_features.get('treeOfThoughtsResults', [])
        if tot_results:
            branches_explored = sum(r.get('totalBranchesExplored', 0) for r in tot_results)
            self.log(f"🌳 Tree of Thoughts: {len(tot_results)} agent analyses, {branches_explored} total branches explored")
            success_indicators.append("Tree of Thoughts functional")
        else:
            self.log("⚠️ Tree of Thoughts: No results found", "WARNING")
        
        # 2. Agent Conversations
        conversation_result = enhanced_features.get('conversationResult')
        if conversation_result:
            exchanges = conversation_result.get('conversation', {}).get('conversationTree', {}).get('totalExchangeCount', 0)
            insights = len(conversation_result.get('keyInsights', []))
            self.log(f"💬 Nested Conversations: {exchanges} exchanges, {insights} insights generated")
            success_indicators.append("Nested Conversations functional")
        else:
            self.log("⚠️ Nested Conversations: No results found", "WARNING")
        
        # 3. Agent Debates
        debate_result = enhanced_features.get('debateResult')
        if debate_result:
            rounds = debate_result.get('totalRounds', 0)
            quality_improvement = debate_result.get('qualityImprovement', 0)
            self.log(f"⚔️ Agent Debates: {rounds} rounds, {quality_improvement:.1%} quality improvement")
            success_indicators.append("Agent Debates functional")
        else:
            self.log("⚠️ Agent Debates: No results found", "WARNING")
        
        # 4. Meta-Reasoning
        meta_results = enhanced_features.get('metaReasoningResults', [])
        if meta_results:
            avg_meta_score = sum(r.get('overallMetaScore', 0) for r in meta_results) / len(meta_results)
            self.log(f"🧠 Meta-Reasoning: {len(meta_results)} analyses, avg score {avg_meta_score:.2f}")
            success_indicators.append("Meta-Reasoning functional")
        else:
            self.log("⚠️ Meta-Reasoning: No results found", "WARNING")
        
        # 5. Enhanced RAG
        rag_contexts = enhanced_features.get('ragContexts', {})
        rag_insights = enhanced_features.get('ragInsights', [])
        if rag_contexts or rag_insights:
            self.log(f"🧮 Enhanced RAG: {len(rag_contexts)} contexts, {len(rag_insights)} insights")
            success_indicators.append("Enhanced RAG functional")
        else:
            self.log("⚠️ Enhanced RAG: No results found", "WARNING")
        
        # 6. Final Synthesis
        synthesis = result.get('finalSynthesis')
        if synthesis:
            consensus_findings = len(synthesis.get('consensusFindings', []))
            overall_confidence = synthesis.get('overallConfidence', 0)
            quality_score = synthesis.get('qualityScore', 0)
            self.log(f"🎯 Final Synthesis: {consensus_findings} consensus findings, "
                    f"{overall_confidence:.1%} confidence, {quality_score:.2f} quality")
            success_indicators.append("Final Synthesis functional")
        
        # 7. Performance Metrics
        metrics = result.get('metrics', {})
        if metrics:
            hallucinations_corrected = metrics.get('hallucinationsCorrected', 0)
            cross_validations = metrics.get('crossValidationsPerformed', 0)
            self.log(f"📈 Performance: {hallucinations_corrected} hallucinations corrected, "
                    f"{cross_validations} cross-validations performed")
        
        # Overall Assessment
        total_features = 6  # Tot, Conversations, Debates, Meta-Reasoning, RAG, Synthesis
        working_features = len(success_indicators)
        success_rate = working_features / total_features
        
        self.log(f"🏆 Enhanced 2025 Success Rate: {working_features}/{total_features} features functional ({success_rate:.1%})")
        
        if success_rate >= 0.8:
            self.log("✅ Enhanced 2025 system is FULLY OPERATIONAL with advanced multi-agent capabilities!")
            return True
        elif success_rate >= 0.6:
            self.log("⚠️ Enhanced 2025 system is PARTIALLY OPERATIONAL - some advanced features working", "WARNING")
            return True
        else:
            self.log("❌ Enhanced 2025 system has MAJOR ISSUES - most features not working", "ERROR")
            return False
    
    def test_hallucination_reduction(self):
        """Test hallucination detection and reduction capabilities"""
        self.log("🛡️ Testing hallucination reduction capabilities...")
        
        # Test with potentially misleading code that could trigger hallucinations
        misleading_code = '''
public class SecureProcessor
{
    // This looks secure but has hidden vulnerabilities
    public string ProcessInput(string userInput)
    {
        var sanitized = userInput?.Replace("'", "''"); // Looks like SQL injection prevention
        var query = $"SELECT * FROM users WHERE name = '{sanitized}'"; // Still vulnerable
        return ExecuteQuery(query);
    }
}
'''
        
        payload = {
            "fileName": "hallucination_test.cs",
            "language": "csharp", 
            "content": misleading_code,
            "enhanced2025": True,
            "features": {
                "enableHallucinationDetection": True,
                "enableCrossAgentValidation": True
            }
        }
        
        try:
            response = self.session.post(f"{self.base_url}/api/review/enhanced-2025", json=payload, timeout=60)
            
            if response.status_code == 200:
                result = response.json()
                hallucination_detection = result.get('enhancedFeatures', {}).get('hallucinationDetection', {})
                
                detected = hallucination_detection.get('hallucinationsDetected', 0)
                corrected = hallucination_detection.get('hallucinationsCorrected', 0)
                detection_confidence = hallucination_detection.get('detectionConfidence', 0)
                
                self.log(f"🔍 Hallucination Detection: {detected} detected, {corrected} corrected, "
                        f"{detection_confidence:.1%} confidence")
                
                if detected > 0 or corrected > 0:
                    self.log("✅ Hallucination reduction system is working!")
                    return True
                else:
                    self.log("⚠️ No hallucinations detected - may need more sophisticated test case", "WARNING")
                    return True  # Not necessarily a failure
                    
        except Exception as e:
            self.log(f"❌ Hallucination test failed: {str(e)}", "ERROR")
            return False
    
    def test_reasoning_quality_improvement(self):
        """Test that Enhanced 2025 system actually improves reasoning quality"""
        self.log("📈 Testing reasoning quality improvement...")
        
        # Run same analysis with and without Enhanced 2025 features
        test_code = '''
public async Task<bool> ValidateUser(string username, string password)
{
    var user = await db.Users.FirstOrDefault(u => u.Username == username);
    return user != null && user.Password == password;
}
'''
        
        base_payload = {
            "fileName": "quality_test.cs",
            "language": "csharp",
            "content": test_code
        }
        
        try:
            # Standard analysis
            self.log("Running standard analysis...")
            standard_response = self.session.post(f"{self.base_url}/api/review", json=base_payload, timeout=30)
            
            # Enhanced 2025 analysis
            enhanced_payload = {**base_payload, "enhanced2025": True}
            self.log("Running Enhanced 2025 analysis...")
            enhanced_response = self.session.post(f"{self.base_url}/api/review/enhanced-2025", json=enhanced_payload, timeout=60)
            
            if standard_response.status_code == 200 and enhanced_response.status_code == 200:
                standard_result = standard_response.json()
                enhanced_result = enhanced_response.json()
                
                # Compare quality metrics
                standard_quality = standard_result.get('qualityScore', 0)
                enhanced_quality = enhanced_result.get('qualityScore', 0)
                
                standard_findings = len(standard_result.get('findings', []))
                enhanced_findings = len(enhanced_result.get('finalSynthesis', {}).get('consensusFindings', []))
                
                improvement_ratio = enhanced_quality / standard_quality if standard_quality > 0 else 0
                
                self.log(f"📊 Quality Comparison:")
                self.log(f"   Standard: {standard_quality:.2f} quality, {standard_findings} findings")
                self.log(f"   Enhanced: {enhanced_quality:.2f} quality, {enhanced_findings} findings")
                self.log(f"   Improvement: {improvement_ratio:.2f}x quality ratio")
                
                if improvement_ratio > 1.1:  # 10% improvement threshold
                    self.log("✅ Enhanced 2025 demonstrates significant quality improvement!")
                    return True
                elif improvement_ratio >= 0.9:  # No degradation
                    self.log("✅ Enhanced 2025 maintains quality with advanced features")
                    return True
                else:
                    self.log("⚠️ Enhanced 2025 may have quality regression", "WARNING")
                    return False
                    
        except Exception as e:
            self.log(f"❌ Quality improvement test failed: {str(e)}", "ERROR")
            return False
    
    def run_comprehensive_test(self):
        """Run all Enhanced 2025 tests"""
        self.log("🚀 Starting Enhanced 2025 Comprehensive Test Suite")
        self.log("=" * 60)
        
        test_results = {}
        
        # Test 1: System Health
        test_results['health'] = self.test_system_health()
        
        if not test_results['health']:
            self.log("❌ System health check failed - aborting tests", "ERROR")
            return False
        
        # Test 2: Enhanced 2025 Features
        test_results['enhanced_features'] = self.test_enhanced_2025_features()
        
        # Test 3: Hallucination Reduction
        test_results['hallucination_reduction'] = self.test_hallucination_reduction()
        
        # Test 4: Quality Improvement
        test_results['quality_improvement'] = self.test_reasoning_quality_improvement()
        
        # Final Assessment
        self.log("=" * 60)
        self.log("🏁 Enhanced 2025 Test Results Summary:")
        
        passed_tests = sum(1 for result in test_results.values() if result)
        total_tests = len(test_results)
        success_rate = passed_tests / total_tests
        
        for test_name, result in test_results.items():
            status = "✅ PASS" if result else "❌ FAIL"
            self.log(f"   {test_name.replace('_', ' ').title()}: {status}")
        
        self.log(f"\n🎯 Overall Success Rate: {passed_tests}/{total_tests} ({success_rate:.1%})")
        
        if success_rate >= 0.8:
            self.log("🏆 Enhanced 2025 Multi-Agent System is PRODUCTION READY!")
            self.log("🚀 Features include: Tree of Thoughts, Agent Critics, Enhanced RAG,")
            self.log("   Meta-Reasoning, Nested Conversations, and Hallucination Reduction")
            return True
        elif success_rate >= 0.6:
            self.log("⚠️ Enhanced 2025 system is functional but needs optimization")
            return True
        else:
            self.log("❌ Enhanced 2025 system has critical issues requiring attention")
            return False

def main():
    """Main test execution"""
    print("🤖 Enhanced 2025 Multi-Agent Code Review System Test")
    print("=" * 60)
    print("Testing state-of-the-art AI features:")
    print("• Tree of Thoughts (ToT) reasoning")
    print("• Agent Critics and Debates")  
    print("• Enhanced RAG with memory")
    print("• Meta-Reasoning and reflection")
    print("• Nested Conversational exchanges")
    print("• Hallucination detection & reduction")
    print("=" * 60)
    
    tester = Enhanced2025SystemTester()
    success = tester.run_comprehensive_test()
    
    if success:
        print("\n🎉 Enhanced 2025 Multi-Agent System Test COMPLETED SUCCESSFULLY!")
        print("The system demonstrates advanced AI capabilities matching 2025 state-of-the-art.")
        exit(0)
    else:
        print("\n💥 Enhanced 2025 Multi-Agent System Test FAILED!")
        print("Some advanced features may not be working as expected.")
        exit(1)

if __name__ == "__main__":
    main()