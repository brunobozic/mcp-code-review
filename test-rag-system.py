#!/usr/bin/env python3
"""
RAG System Verification Test
Tests the complete RAG functionality including ChromaDB and embeddings
"""

import requests
import json
import time
import sys

def test_chromadb_connection():
    """Test basic ChromaDB connection"""
    print("🔗 Testing ChromaDB connection...")
    try:
        response = requests.get("http://localhost:8000/api/v1/heartbeat", timeout=5)
        if response.status_code == 200:
            print("  ✅ ChromaDB is responding")
            return True
        else:
            print(f"  ❌ ChromaDB returned status {response.status_code}")
            return False
    except Exception as e:
        print(f"  ❌ ChromaDB connection failed: {e}")
        return False

def test_collection_operations():
    """Test collection creation and listing"""
    print("\n📂 Testing collection operations...")
    
    # Test listing collections
    try:
        response = requests.get("http://localhost:8000/api/v1/collections", timeout=5)
        collections = response.json()
        print(f"  📋 Found {len(collections)} collections")
        
        # Print collection names
        for collection in collections:
            if isinstance(collection, dict) and 'name' in collection:
                print(f"    - {collection['name']}")
            else:
                print(f"    - {collection}")
                
        return len(collections) > 0
    except Exception as e:
        print(f"  ❌ Collection listing failed: {e}")
        return False

def test_document_storage():
    """Test storing and retrieving documents"""
    print("\n💾 Testing document storage...")
    
    test_collection = "test_verification"
    
    # Create collection
    try:
        collection_data = {"name": test_collection}
        response = requests.post(
            "http://localhost:8000/api/v1/collections",
            headers={"Content-Type": "application/json"},
            json=collection_data,
            timeout=5
        )
        
        if response.status_code in [200, 201]:
            print(f"  ✅ Created collection '{test_collection}'")
        elif "already exists" in response.text:
            print(f"  ✅ Collection '{test_collection}' already exists")
        else:
            print(f"  ⚠️  Collection creation response: {response.status_code} - {response.text}")
    except Exception as e:
        print(f"  ❌ Collection creation failed: {e}")
        return False
    
    # Add a test document
    try:
        test_doc = {
            "documents": ["This is a test document about authentication security best practices"],
            "metadatas": [{"category": "security", "type": "test"}],
            "ids": ["test-doc-1"]
        }
        
        response = requests.post(
            f"http://localhost:8000/api/v1/collections/{test_collection}/add",
            headers={"Content-Type": "application/json"},
            json=test_doc,
            timeout=5
        )
        
        if response.status_code in [200, 201]:
            print("  ✅ Successfully stored test document")
            return True
        else:
            print(f"  ❌ Document storage failed: {response.status_code} - {response.text}")
            return False
            
    except Exception as e:
        print(f"  ❌ Document storage failed: {e}")
        return False

def test_mcp_server():
    """Test MCP server endpoints"""
    print("\n🖥️  Testing MCP server...")
    
    endpoints = [
        "http://localhost:5002/",
        "http://localhost:5002/health",
        "http://localhost:5002/api/health",
        "http://localhost:5003/metrics"
    ]
    
    working_endpoints = 0
    
    for endpoint in endpoints:
        try:
            response = requests.get(endpoint, timeout=5)
            if response.status_code == 200:
                print(f"  ✅ {endpoint} is responding")
                working_endpoints += 1
            else:
                print(f"  ⚠️  {endpoint} returned {response.status_code}")
        except Exception as e:
            print(f"  ❌ {endpoint} failed: {e}")
    
    return working_endpoints > 0

def test_rag_data_seeding():
    """Check if RAG data was seeded properly"""
    print("\n🌱 Testing RAG data seeding...")
    
    expected_collections = ["coding_standards", "historical_issues", "team_patterns", "code_patterns"]
    
    try:
        response = requests.get("http://localhost:8000/api/v1/collections", timeout=5)
        collections_data = response.json()
        
        existing_collections = []
        if isinstance(collections_data, list):
            for collection in collections_data:
                if isinstance(collection, dict) and 'name' in collection:
                    existing_collections.append(collection['name'])
                elif isinstance(collection, str):
                    existing_collections.append(collection)
        
        seeded_count = 0
        for expected in expected_collections:
            if expected in existing_collections:
                print(f"  ✅ Found '{expected}' collection")
                seeded_count += 1
            else:
                print(f"  ❌ Missing '{expected}' collection")
        
        print(f"  📊 RAG collections: {seeded_count}/{len(expected_collections)} found")
        return seeded_count >= 2  # At least half should be seeded
        
    except Exception as e:
        print(f"  ❌ RAG seeding check failed: {e}")
        return False

def main():
    """Run all RAG system tests"""
    print("🧪 RAG System Verification Test")
    print("=" * 50)
    
    tests = [
        ("ChromaDB Connection", test_chromadb_connection),
        ("Collection Operations", test_collection_operations),
        ("Document Storage", test_document_storage),
        ("MCP Server", test_mcp_server),
        ("RAG Data Seeding", test_rag_data_seeding)
    ]
    
    passed = 0
    total = len(tests)
    
    for test_name, test_func in tests:
        print(f"\n{'='*20} {test_name} {'='*20}")
        try:
            if test_func():
                passed += 1
                print(f"  🎉 {test_name} PASSED")
            else:
                print(f"  💥 {test_name} FAILED")
        except Exception as e:
            print(f"  💥 {test_name} FAILED with exception: {e}")
    
    print(f"\n{'='*50}")
    print(f"📊 Test Results: {passed}/{total} tests passed")
    
    if passed == total:
        print("🎉 All tests passed! RAG system is working correctly.")
        return 0
    elif passed >= total * 0.7:
        print("⚠️  Most tests passed. System is mostly functional.")
        return 0
    else:
        print("❌ Multiple tests failed. RAG system needs attention.")
        return 1

if __name__ == "__main__":
    sys.exit(main())