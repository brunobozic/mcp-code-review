# Security Guidelines

## Authentication & Authorization

### JWT Token Security
- **Expiry Time**: Maximum 15 minutes for access tokens
- **Refresh Tokens**: Implement rotation with secure storage
- **Signing**: Use RS256 algorithm with proper key management
- **Validation**: Validate every request, check expiry and signature

### Password Security
- **Hashing**: Use BCrypt with minimum 12 salt rounds, prefer Argon2
- **Complexity**: Minimum 8 characters, require mix of character types
- **Storage**: Never store passwords in plain text or reversible encryption
- **Reset**: Implement secure password reset with time-limited tokens

### Session Management
- **Timeout**: 30 minutes of inactivity
- **Logout**: Properly invalidate sessions on logout
- **Concurrent Sessions**: Limit to prevent abuse
- **CSRF Protection**: Implement CSRF tokens for state-changing operations

## Input Validation & Sanitization

### SQL Injection Prevention
- **Parameterized Queries**: Always use prepared statements
- **ORM Usage**: Prefer Entity Framework LINQ over raw SQL
- **Input Validation**: Validate all user inputs server-side
- **Whitelist Approach**: Define allowed characters/patterns

### XSS Prevention
- **Output Encoding**: Encode all user data before display
- **Content Security Policy**: Implement strict CSP headers
- **Input Sanitization**: Remove/escape dangerous HTML/JavaScript
- **Framework Protection**: Use framework built-in XSS protection

### File Upload Security
- **File Type Validation**: Check file extensions and MIME types
- **Size Limits**: Implement reasonable file size restrictions
- **Virus Scanning**: Scan uploads for malware
- **Storage Location**: Store outside web root directory

## API Security

### Rate Limiting
- **Authentication Endpoints**: 5 attempts per minute per IP
- **General API**: 100 requests per minute per user
- **File Uploads**: Stricter limits based on file size
- **Burst Protection**: Allow short bursts but enforce averages

### HTTPS & Transport Security
- **TLS Version**: Minimum TLS 1.2, prefer TLS 1.3
- **Certificate Management**: Use valid, up-to-date certificates
- **HSTS Headers**: Implement HTTP Strict Transport Security
- **Secure Cookies**: Set Secure and HttpOnly flags

### API Versioning & Documentation
- **Version Management**: Use semantic versioning
- **Deprecation**: Provide clear deprecation timelines
- **Documentation**: Keep API documentation current
- **Security Testing**: Regular penetration testing

## Error Handling & Logging

### Secure Error Messages
- **User-Facing**: Generic error messages to users
- **Internal Logging**: Detailed errors in secure logs
- **Stack Traces**: Never expose stack traces to users
- **Correlation IDs**: Use correlation IDs for debugging

### Security Logging
- **Authentication Events**: Log all login attempts (success/failure)
- **Authorization Failures**: Log access denied events
- **Sensitive Operations**: Log password changes, privilege escalations
- **Retention**: Keep security logs for minimum 90 days

## Data Protection

### Sensitive Data Handling
- **PII Classification**: Classify and mark personally identifiable information
- **Encryption at Rest**: Encrypt sensitive data in databases
- **Encryption in Transit**: Use TLS for all data transmission
- **Data Minimization**: Collect only necessary data

### Database Security
- **Access Controls**: Use principle of least privilege
- **Connection Security**: Encrypt database connections
- **Backup Security**: Encrypt database backups
- **Audit Trails**: Maintain audit logs for data access

## Incident Response

### Security Monitoring
- **Real-time Alerts**: Configure alerts for suspicious activities
- **Log Analysis**: Regular review of security logs
- **Threat Detection**: Implement automated threat detection
- **Incident Response Plan**: Maintain updated response procedures

### Vulnerability Management
- **Regular Scans**: Automated vulnerability scanning
- **Patch Management**: Timely application of security patches
- **Dependency Scanning**: Monitor third-party dependencies
- **Security Reviews**: Regular code security reviews