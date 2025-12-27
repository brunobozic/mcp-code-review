#!/bin/bash

echo "🚀 Setting up GitLab Demo Project"
echo "=================================="

# Wait for GitLab to be ready
echo "⏱️ Waiting for GitLab to be ready..."
timeout=300
while [ $timeout -gt 0 ]; do
    if docker exec mcp-gitlab-success gitlab-ctl status | grep -q "run: puma"; then
        echo "✅ GitLab services are running"
        break
    fi
    sleep 5
    timeout=$((timeout - 5))
done

if [ $timeout -eq 0 ]; then
    echo "❌ GitLab failed to start within 5 minutes"
    exit 1
fi

# Wait a bit more for Rails to be ready
sleep 30

echo "🔑 Setting up root user password..."
docker exec mcp-gitlab-success gitlab-rails runner "
user = User.find_by_username('root')
if user
    user.password = 'GitLabAdmin2024!'
    user.password_confirmation = 'GitLabAdmin2024!'
    user.save!
    puts 'Root password updated successfully'
else
    puts 'Root user not found'
end
"

echo "📁 Creating demo project..."
docker exec mcp-gitlab-success gitlab-rails runner "
# Create demo project
project = Project.create!(
    name: 'ecommerce-api-demo',
    path: 'ecommerce-api-demo',
    description: 'Demo project for AI code review testing with intentional security and performance issues',
    visibility_level: Gitlab::VisibilityLevel::PUBLIC,
    namespace: User.find_by_username('root').namespace
)

puts \"Project created: #{project.name} (ID: #{project.id})\"
"

echo "📝 Adding demo code files..."
# Copy sample project files to GitLab project
mkdir -p /tmp/demo-project
cp -r /home/brunobozic/mcp-code-review/sample-projects/ecommerce-api/* /tmp/demo-project/

# Create initial commit
docker exec mcp-gitlab-success gitlab-rails runner "
require 'tempfile'

project = Project.find_by_path('ecommerce-api-demo')
if project
    # Sample C# files with intentional issues
    files = [
        {
            file_path: 'Controllers/PaymentController.cs',
            content: File.read('/opt/gitlab/sample-projects/ecommerce-api/Controllers/PaymentController.cs') rescue 'public class PaymentController { private string connectionString = \"Server=localhost;User=sa;Password=admin123;\"; public void ProcessPayment(string cardNumber, string amount) { string query = \"INSERT INTO payments VALUES (\" + cardNumber + \", \" + amount + \")\"; } }'
        },
        {
            file_path: 'Services/UserService.cs', 
            content: 'public class UserService { public User GetUser(string id) { var query = \"SELECT * FROM users WHERE id = \" + id; /* SQL Injection vulnerability */ return Database.Query<User>(query).FirstOrDefault(); } }'
        },
        {
            file_path: 'README.md',
            content: '# E-commerce API Demo\\n\\nThis project contains intentional security and performance issues for AI code review testing.\\n\\n## Issues to find:\\n- SQL injection vulnerabilities\\n- Hardcoded credentials\\n- Performance problems (N+1 queries)\\n- Weak encryption\\n- Missing input validation'
        }
    ]
    
    # Create initial commit
    Files::CreateService.new(
        project,
        User.find_by_username('root'),
        start_branch: 'main',
        branch_name: 'main',
        commit_message: 'Initial commit with demo code for AI review testing',
        file_path: files.first[:file_path],
        file_content: files.first[:content]
    ).execute
    
    puts 'Demo project files added successfully'
end
"

echo ""
echo "✅ GitLab Demo Project Setup Complete!"
echo "========================================="
echo "🔗 Access GitLab at: http://localhost:9191"
echo "👤 Username: root"
echo "🔑 Password: GitLabAdmin2024!"
echo "📁 Demo Project: ecommerce-api-demo"
echo ""
echo "🎯 Ready for AI code review testing!"