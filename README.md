# Okta_OAuth_Config_Proj

# Main Purpose
This project is a demo application showcasing Auth0 integration for authentication and user account management in a .NET 8 web application.

# Key Features
## Authentication & Authorization:

1. OAuth login/logout using Auth0
2. User profile management
3. Protected routes requiring authentication

# Account Management:

View user profile information from Auth0
Check for multiple accounts associated with the same email
Link/unlink multiple Auth0 accounts for the same user
Delete user accounts via Auth0 Management API

# Auth0 Integration:

Uses Auth0 Web App Authentication SDK
Integrates with Auth0 Management API for advanced user operations
Handles cookie policies for secure authentication
Supports multiple identity providers through Auth0

# Technical Stack
1. Framework: ASP.NET Core 8.0 with MVC pattern
2. Authentication: Auth0.AspNetCore.Authentication package
3. Frontend: Razor views with standard MVC structure
4. Configuration: Auth0 domain and client credentials in appsettings.json

The project name suggests it was originally intended for Okta OAuth configuration, but the actual implementation uses Auth0 instead. It's essentially a working example of how to integrate Auth0 authentication into an ASP.NET Core application with advanced user management features.