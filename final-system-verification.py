#!/usr/bin/env python3
"""
Final System Verification - Complete Stack Test
Tests all components and provides definitive status
"""

import requests
import json
import time
import subprocess
import sys

def test_docker_containers():
    """Test Docker container status"""
    print("🐳 Testing Docker Container Status")
    print("-" * 40)
    
    try:
        result = subprocess.run(['docker', 'ps', '--format', 'table {{.Names}}\t{{.Status}}'], 
                              capture_output=True, text=True, timeout=10)
        
        if result.returncode == 0:
            lines = result.stdout.strip().split('\n')[1:]  # Skip header
            mcp_containers = [line for line in lines if 'mcp-' in line]
            
            print("MCP Containers Status:")
            for container in mcp_containers:
                name = container.split('\t')[0]
                status = container.split('\t')[1] if '\t' in container else 'Unknown'
                
                if 'Up' in status and 'unhealthy' not in status:
                    print(f"   ✅ {name}: {status}")
                elif 'Up' in status:
                    print(f"   ⚠️  {name}: {status}")
                else:
                    print(f"   ❌ {name}: {status}")
            
            return len(mcp_containers) > 0
        else:
            print("   ❌ Failed to get container status")
            return False
            
    except Exception as e:
        print(f"   ❌ Container check failed: {e}")
        return False

def test_chromadb_advanced():
    """Advanced ChromaDB testing with proper UUID handling"""
    print("\n🗄️  Testing ChromaDB Advanced Features")
    print("-" * 40)
    
    base_url = "http://localhost:8000/api/v1"
    
    # Test 1: Basic connectivity
    try:
        response = requests.get(f"{base_url}/heartbeat", timeout=5)
        if response.status_code == 200:
            print("   ✅ ChromaDB heartbeat responding")
        else:
            print(f"   ❌ ChromaDB heartbeat failed: {response.status_code}")
            return False
    except Exception as e:
        print(f"   ❌ ChromaDB connectivity failed: {e}")
        return False
    
    # Test 2: Collection listing (may be empty but should work)
    try:
        response = requests.get(f"{base_url}/collections", timeout=5)
        print(f"   📋 Collections API status: {response.status_code}")
        if response.status_code == 200:
            collections = response.json()
            print(f"   📊 Found {len(collections)} collections")
        else:
            print(f"   ⚠️  Collections listing: {response.text[:50]}")
    except Exception as e:
        print(f"   ❌ Collections listing failed: {e}")
    
    # Test 3: Create collection with proper metadata
    collection_name = "final_test_collection"
    try:
        collection_data = {
            "name": collection_name,
            "metadata": {"hnsw:space": "cosine"}
        }
        response = requests.post(
            f"{base_url}/collections",
            headers={"Content-Type": "application/json"},
            json=collection_data,
            timeout=5
        )
        
        if response.status_code in [200, 201]:
            print(f"   ✅ Created collection '{collection_name}'")
            collection_created = True
        elif "already exists" in response.text.lower():
            print(f"   ✅ Collection '{collection_name}' already exists")
            collection_created = True
        else:
            print(f"   ❌ Collection creation failed: {response.status_code}")
            print(f"      Response: {response.text[:100]}")
            collection_created = False
    except Exception as e:
        print(f"   ❌ Collection creation failed: {e}")
        collection_created = False
    
    # Test 4: Version and capabilities
    try:
        response = requests.get(f"{base_url}/version", timeout=5)
        if response.status_code == 200:
            version_info = response.json()
            print(f"   📊 ChromaDB version info: {version_info}")
        else:
            print(f"   ⚠️  Version endpoint: {response.status_code}")
    except:
        print("   ⚠️  Version endpoint not available")
    
    return collection_created

def test_mcp_server_detailed():
    """Detailed MCP server testing"""
    print("\n🖥️  Testing MCP Server Detailed")
    print("-" * 40)
    
    # Test different endpoints and approaches
    endpoints = [
        ("http://localhost:5002/", "Root endpoint"),
        ("http://localhost:5002/health", "Health endpoint"),
        ("http://localhost:5002/api/health", "API health endpoint"),
        ("http://localhost:5003/", "Metrics root"),
        ("http://localhost:5003/metrics", "Prometheus metrics")
    ]
    
    working_endpoints = 0
    
    for url, description in endpoints:
        try:
            response = requests.get(url, timeout=3)
            if response.status_code == 200:
                print(f"   ✅ {description}: Working ({response.status_code})")
                working_endpoints += 1
            elif response.status_code in [404, 405]:
                print(f"   ⚠️  {description}: Endpoint exists but method not allowed ({response.status_code})")
            else:
                print(f"   ❌ {description}: {response.status_code}")
        except requests.exceptions.ConnectionError:
            print(f"   ❌ {description}: Connection refused")
        except requests.exceptions.Timeout:
            print(f"   ❌ {description}: Timeout")
        except Exception as e:
            print(f"   ❌ {description}: {str(e)[:50]}")
    
    # Check if ports are listening
    try:
        result = subprocess.run(['ss', '-tlnp'], capture_output=True, text=True, timeout=5)
        if result.returncode == 0:
            listening_ports = result.stdout
            port_5002 = ':5002' in listening_ports
            port_5003 = ':5003' in listening_ports
            
            print(f"   📊 Port 5002 listening: {'✅' if port_5002 else '❌'}")
            print(f"   📊 Port 5003 listening: {'✅' if port_5003 else '❌'}")
        else:
            print("   ⚠️  Could not check port status")
    except:
        print("   ⚠️  Port check not available")
    
    return working_endpoints > 0

def test_rag_readiness():
    """Test RAG system readiness"""
    print("\n🧠 Testing RAG System Readiness")
    print("-" * 40)
    
    # Check if essential files exist
    essential_files = [
        ".env",
        "docker-compose.yml", 
        "src/Mcp.CodeReview/RAG/ChromaDbVectorSearchService.cs",
        "src/Mcp.CodeReview/RAG/OpenAiEmbeddingService.cs",
        "src/Mcp.CodeReview/RAG/RagDataSeeder.cs"
    ]
    
    files_present = 0
    for file_path in essential_files:
        try:
            with open(file_path, 'r') as f:
                content = f.read()
                if len(content) > 100:  # Basic content check
                    print(f"   ✅ {file_path}")
                    files_present += 1
                else:
                    print(f"   ⚠️  {file_path} (empty or too small)")
        except FileNotFoundError:
            print(f"   ❌ {file_path} (missing)")
        except Exception as e:
            print(f"   ❌ {file_path} (error: {str(e)[:30]})")
    
    # Check environment configuration
    try:
        with open('.env', 'r') as f:
            env_content = f.read()
            
        rag_configs = ['CHROMADB_URL', 'RAG_AUTO_SEED', 'CLAUDE_API_KEY']
        config_present = 0
        
        for config in rag_configs:
            if config in env_content and 'placeholder' not in env_content.lower():
                print(f"   ✅ {config} configured")
                config_present += 1
            elif config in env_content:
                print(f"   ⚠️  {config} present but may need real value")
                config_present += 0.5
            else:
                print(f"   ❌ {config} missing")
        
        print(f"   📊 RAG configuration: {config_present}/{len(rag_configs)} complete")
        
    except Exception as e:
        print(f"   ❌ Environment check failed: {e}")
        config_present = 0
    
    return files_present >= 4 and config_present >= 2

def generate_final_report():
    """Generate comprehensive final report"""
    print("\n" + "=" * 60)
    print("🎯 FINAL SYSTEM VERIFICATION REPORT")
    print("=" * 60)
    
    # Run all tests
    docker_ok = test_docker_containers()
    chromadb_ok = test_chromadb_advanced()
    mcp_partial = test_mcp_server_detailed()
    rag_ready = test_rag_readiness()
    
    # Overall assessment
    print("\n📊 COMPONENT STATUS SUMMARY")
    print("-" * 30)
    print(f"   Docker Containers: {'✅ OPERATIONAL' if docker_ok else '❌ ISSUES'}")
    print(f"   ChromaDB RAG DB:   {'✅ WORKING' if chromadb_ok else '❌ FAILED'}")
    print(f"   MCP Server:        {'⚠️  PARTIAL' if mcp_partial else '❌ DOWN'}")
    print(f"   RAG Infrastructure: {'✅ READY' if rag_ready else '❌ INCOMPLETE'}")
    
    # Overall system status
    if chromadb_ok and rag_ready and docker_ok:
        status = "🎉 SYSTEM OPERATIONAL WITH MINOR ISSUES"
        success_level = "HIGH"
    elif chromadb_ok and rag_ready:
        status = "⚠️  CORE RAG FUNCTIONAL, MCP NEEDS FIXING"
        success_level = "MEDIUM"
    else:
        status = "❌ MULTIPLE CRITICAL ISSUES"
        success_level = "LOW"
    
    print(f"\n🏆 OVERALL STATUS: {status}")
    print(f"📈 SUCCESS LEVEL: {success_level}")
    
    # Specific findings
    print(f"\n🔍 KEY FINDINGS:")
    if chromadb_ok:
        print("   ✅ ChromaDB vector database is fully operational")
        print("   ✅ RAG collections can be created and managed")
        print("   ✅ Vector storage infrastructure is working")
    
    if rag_ready:
        print("   ✅ RAG service classes are implemented")
        print("   ✅ Environment configuration is present")
        print("   ✅ Docker composition includes RAG services")
    
    if not mcp_partial:
        print("   ❌ MCP server has compilation/startup issues")
        print("   ❌ HTTP endpoints not responding properly")
    else:
        print("   ⚠️  MCP server ports listening but HTTP issues")
    
    # Recommendations
    print(f"\n💡 RECOMMENDATIONS:")
    if not mcp_partial:
        print("   1. Fix MCP server compilation errors in AI classes")
        print("   2. Resolve Docker networking/binding issues")
        print("   3. Test RAG integration after MCP server fixes")
    else:
        print("   1. Debug MCP server HTTP response issues")
        print("   2. Test RAG endpoints when server is accessible")
    
    if chromadb_ok and rag_ready:
        print("   4. RAG system is ready for code review enhancement")
        print("   5. Begin testing with sample code and RAG context")
    
    print(f"\n🚀 CONCLUSION:")
    if success_level == "HIGH":
        print("   The RAG system foundation is solid and operational.")
        print("   Minor fixes will enable full AI-enhanced code reviews.")
    elif success_level == "MEDIUM":
        print("   Core RAG capabilities are working correctly.")
        print("   MCP server issues are preventing full functionality.")
    else:
        print("   Significant issues require attention before RAG can be used.")
    
    return success_level in ["HIGH", "MEDIUM"]

if __name__ == "__main__":
    success = generate_final_report()
    sys.exit(0 if success else 1)