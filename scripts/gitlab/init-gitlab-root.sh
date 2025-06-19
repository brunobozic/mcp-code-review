#!/bin/bash
set -e

echo "🔧 Initializing GitLab root user..."

# Wait for GitLab to be fully ready
echo "⏳ Waiting for GitLab services to be ready..."
timeout=300
counter=0

while [ $counter -lt $timeout ]; do
    if docker exec simple-gitlab gitlab-ctl status | grep -q "run: puma"; then
        echo "✅ GitLab services are running"
        break
    fi
    echo "⏳ Waiting for GitLab services... ($counter/$timeout)"
    sleep 5
    counter=$((counter + 5))
done

if [ $counter -ge $timeout ]; then
    echo "❌ Timeout waiting for GitLab services"
    exit 1
fi

# Wait a bit more for database to be fully ready
echo "⏳ Waiting for database to be ready..."
sleep 15

# Create root user with rails runner
echo "👤 Creating GitLab root user..."
docker exec simple-gitlab gitlab-rails runner "
begin
  # Check if root user already exists
  root_user = User.find_by(username: 'root')
  
  if root_user.nil?
    puts '📝 Creating new root user...'
    
    # Create root user
    root_user = User.new(
      username: 'root',
      email: 'admin@example.com',
      name: 'Administrator',
      password: 'Adm1nP@ssw0rd2025!',
      password_confirmation: 'Adm1nP@ssw0rd2025!',
      admin: true,
      confirmed_at: Time.current,
      confirmation_token: nil
    )
    
    # Skip email confirmation
    root_user.skip_confirmation!
    
    if root_user.save!
      puts '✅ Root user created successfully!'
      puts '👤 Username: root'
      puts '🔑 Password: Adm1nP@ssw0rd2025!'
      puts '✉️  Email: admin@example.com'
    else
      puts '❌ Failed to create root user:'
      puts root_user.errors.full_messages.join(', ')
      exit 1
    end
  else
    puts '✅ Root user already exists'
    puts '👤 Username: root'
    puts '🔑 Password should be: Adm1nP@ssw0rd2025!'
    
    # Update password if needed
    if root_user.valid_password?('Adm1nP@ssw0rd2025!')
      puts '✅ Password is correct'
    else
      puts '🔧 Updating root password...'
      root_user.password = 'Adm1nP@ssw0rd2025!'
      root_user.password_confirmation = 'Adm1nP@ssw0rd2025!'
      if root_user.save!
        puts '✅ Password updated successfully!'
      end
    end
  end
  
  puts '🎉 GitLab root user is ready!'
  puts ''
  puts '🌐 GitLab URL: http://localhost:8080'
  puts '👤 Username: root'
  puts '🔑 Password: Adm1nP@ssw0rd2025!'
  
rescue => e
  puts '❌ Error initializing root user:'
  puts e.message
  puts e.backtrace.first(5).join('\n')
  exit 1
end
"

echo "✅ GitLab initialization complete!"