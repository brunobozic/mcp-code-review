#!/bin/bash

echo "🔧 Setting up SonarQube Project for E-commerce API Sample"
echo "====================================================="

# Configuration
SONAR_HOST="http://localhost:9000"
SONAR_USER="admin"
SONAR_PASS="admin"
PROJECT_KEY="ecommerce-api"
PROJECT_NAME="E-commerce API Sample"
SAMPLE_PROJECT_PATH="./sample-projects/ecommerce-api"

echo "📋 Configuration:"
echo "   🔗 SonarQube URL: $SONAR_HOST"
echo "   📁 Project Key: $PROJECT_KEY"
echo "   📂 Source Path: $SAMPLE_PROJECT_PATH"

# Wait for SonarQube to be ready
echo ""
echo "⏳ Waiting for SonarQube to be ready..."
timeout=60
counter=0
while [ $counter -lt $timeout ]; do
    if curl -s -f "$SONAR_HOST/api/system/status" | grep -q '"status":"UP"'; then
        echo "✅ SonarQube is ready!"
        break
    fi
    echo "   ⏳ Waiting... ($counter/$timeout)"
    sleep 2
    counter=$((counter + 2))
done

if [ $counter -ge $timeout ]; then
    echo "❌ SonarQube is not ready after $timeout seconds"
    echo "   Please ensure SonarQube is running: docker compose -f docker-compose.testing.yml up -d sonarqube"
    exit 1
fi

# Create project in SonarQube
echo ""
echo "🚀 Creating SonarQube project..."
CREATE_RESPONSE=$(curl -s -u "$SONAR_USER:$SONAR_PASS" \
    -X POST "$SONAR_HOST/api/projects/create" \
    -d "project=$PROJECT_KEY" \
    -d "name=$PROJECT_NAME")

if echo "$CREATE_RESPONSE" | grep -q '"project"'; then
    echo "✅ Project created successfully"
elif echo "$CREATE_RESPONSE" | grep -q "already exists"; then
    echo "ℹ️  Project already exists"
else
    echo "⚠️  Project creation response: $CREATE_RESPONSE"
fi

# Generate authentication token
echo ""
echo "🔑 Generating authentication token..."
TOKEN_RESPONSE=$(curl -s -u "$SONAR_USER:$SONAR_PASS" \
    -X POST "$SONAR_HOST/api/user_tokens/generate" \
    -d "name=mcp-analysis-token")

if echo "$TOKEN_RESPONSE" | grep -q '"token"'; then
    TOKEN=$(echo "$TOKEN_RESPONSE" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
    echo "✅ Token generated: ${TOKEN:0:20}..."
else
    echo "⚠️  Using default credentials for analysis"
    TOKEN=""
fi

# Install SonarScanner if not present
echo ""
echo "🔧 Checking SonarScanner..."
if ! command -v dotnet-sonarscanner &> /dev/null; then
    echo "📦 Installing SonarScanner for .NET..."
    dotnet tool install --global dotnet-sonarscanner
    export PATH="$PATH:$HOME/.dotnet/tools"
else
    echo "✅ SonarScanner already installed"
fi

# Create sonar-project.properties file
echo ""
echo "📝 Creating SonarQube configuration..."
cat > "$SAMPLE_PROJECT_PATH/sonar-project.properties" << EOF
# SonarQube project configuration
sonar.projectKey=$PROJECT_KEY
sonar.projectName=$PROJECT_NAME
sonar.projectVersion=1.0

# Source and test paths
sonar.sources=.
sonar.exclusions=**/bin/**,**/obj/**,**/*.dll,**/*.pdb

# Language and encoding
sonar.language=cs
sonar.sourceEncoding=UTF-8

# Analysis parameters
sonar.cs.analyzer.projectOutPaths=**/bin/**/*.dll
sonar.cs.opencover.reportsPaths=**/TestResults/**/coverage.opencover.xml

# Quality gate
sonar.qualitygate.wait=true
EOF

echo "✅ Configuration file created"

# Run SonarQube analysis
echo ""
echo "🔍 Running SonarQube analysis..."
cd "$SAMPLE_PROJECT_PATH"

if [ -n "$TOKEN" ]; then
    SONAR_AUTH="-Dsonar.login=$TOKEN"
else
    SONAR_AUTH="-Dsonar.login=$SONAR_USER -Dsonar.password=$SONAR_PASS"
fi

# Build the project first
echo "🔨 Building project..."
dotnet build --configuration Release

# Run SonarQube analysis using .NET CLI
echo "📊 Starting SonarQube analysis..."
dotnet sonarscanner begin \
    -k:"$PROJECT_KEY" \
    -d:sonar.host.url="$SONAR_HOST" \
    $SONAR_AUTH \
    -d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml"

dotnet build --configuration Release

dotnet sonarscanner end $SONAR_AUTH

echo ""
echo "✅ SonarQube analysis completed!"
echo ""
echo "🌐 View Results:"
echo "   📊 SonarQube Dashboard: $SONAR_HOST/dashboard?id=$PROJECT_KEY"
echo "   🔍 Issues: $SONAR_HOST/project/issues?id=$PROJECT_KEY"
echo "   📈 Measures: $SONAR_HOST/component_measures?id=$PROJECT_KEY"

cd - > /dev/null

# Display summary of findings
echo ""
echo "🎯 Analysis Summary:"
curl -s -u "$SONAR_USER:$SONAR_PASS" \
    "$SONAR_HOST/api/measures/component?component=$PROJECT_KEY&metricKeys=bugs,vulnerabilities,security_hotspots,code_smells,coverage,duplicated_lines_density" \
    | jq -r '.component.measures[] | "\(.metric): \(.value // "N/A")"' 2>/dev/null || echo "   Use SonarQube web interface to view detailed results"

echo ""
echo "🤖 Ready for AI Analysis!"
echo "   The project is now set up in SonarQube and ready for hybrid AI analysis"
echo "   Use the MCP tools to analyze findings with AI insights:"
echo "   • AnalyzeSonarQubeFindings"
echo "   • ConductHybridCodeAnalysis" 
echo "   • AssessQualityGateWithAI"
echo ""
echo "🎉 Setup Complete!"