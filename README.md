# Smart Civic Alert System

## Overview

Smart Civic Alert System is an AI-powered city operations platform that enables citizens to report incidents, send SOS alerts, upload incident photos, track report status, and download reports. The system provides real-time notifications, department-specific incident routing, and comprehensive dashboards for citizens, operators, and administrators.

---

## Key Features

### Citizen Module
- Dashboard with incident statistics, trends, and recent activity
- Incident reporting with title, description, location, severity, and optional photo upload
- My Reports section for tracking all submitted incidents with status updates
- Interactive statistics with charts for trends, severity distribution, and status distribution
- Real-time notifications for incident status changes
- Report export as PDF and CSV with selective download via checkboxes
- SOS Emergency with one-click alert, location input, and current location detection
- Safety tips with emergency contact numbers and guidelines
- Profile management for personal information updates
- AI Vision analysis using Google Gemini API for incident photo analysis
- AI department suggestion based on incident description
- City Assistant Chatbot for common queries and navigation help

### Operator Module
- Dashboard with real-time incident monitoring and analytics
- Incident management with view, update, and resolve capabilities
- Manual incident creation for operators
- Department-specific real-time notifications
- OpenStreetMap integration for incident location visualization
- Priority-based incident handling

### Admin Module
- Dashboard with real-time system statistics and analytics
- User management for citizens and operators
- Comprehensive incident monitoring across all departments
- Priority-based incident sorting and management
- Incident status tracking and response monitoring
- Department-wise incident analytics and reporting
- Response time analysis and performance insights
- Historical incident record management
- RAG Chatbot for intelligent incident queries and data retrieval
- AI-powered report summarization
- View incident severity and status distributions
- Monitor department activities and system performance
- Full system access and administrative configuration

---

## Technology Stack

### Backend Technologies
- ASP.NET Core 8.0 for web application framework
- MongoDB for NoSQL database management
- ASP.NET Core Identity for authentication and authorization
- SignalR for real-time notifications and live updates
- Newtonsoft.Json for JSON serialization and deserialization
- BCrypt.Net-Next for secure password hashing

### AI and Machine Learning
- Google Gemini Vision API for AI-powered image analysis
- Gemini Flash model for fast image understanding
- RAG Chatbot implementation for admin module
- AI department suggestion system

### Frontend Technologies
- HTML5 and CSS3 for semantic markup and styling
- JavaScript ES6+ for client-side interactivity
- Chart.js for interactive data visualization
- Font Awesome for professional iconography
- Google Fonts Inter for modern typography
- OpenStreetMap for incident location visualization

### Development Tools
- Visual Studio 2022 as primary IDE
- GitHub for version control and collaboration
- Postman for API testing and documentation

---

## Project Structure

```
SmartCityPulse/
├── Controllers/
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── CitizenController.cs
│   ├── HomeController.cs
│   ├── IncidentController.cs
│   └── OperatorController.cs
├── Views/
│   ├── Account/
│   ├── Admin/
│   ├── Citizen/
│   │   ├── Index.cshtml
│   │   ├── MyReports.cshtml
│   │   ├── Statistics.cshtml
│   │   ├── Notifications.cshtml
│   │   ├── Reports.cshtml
│   │   ├── SafetyTips.cshtml
│   │   ├── Profile.cshtml
│   │   └── Details.cshtml
│   ├── Incident/
│   │   └── Create.cshtml
│   └── Operator/
├── Models/
│   ├── AppUser.cs
│   ├── Incident.cs
│   ├── Notification.cs
│   └── OperatorDashboardViewModel.cs
├── Data/
│   └── MongoDbContext.cs
├── Hubs/
│   └── NotificationHub.cs
├── Services/
│   ├── AIVisionService.cs
│   └── NotificationService.cs
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── uploads/
│       └── incidents/
├── appsettings.json
├── Program.cs
└── SmartCityPulse.csproj
```

---

## Setup Instructions

### Prerequisites
- .NET 8.0 SDK
- MongoDB installation or Atlas account
- Google Gemini API key for AI Vision functionality
- Visual Studio 2022 or compatible IDE

### Installation Steps

1. Clone the repository
   ```
   git clone https://github.com/Romah-Maqsood/SmartCivicAlertSystem.git
   cd SmartCivicAlertSystem
   ```

2. Configure MongoDB connection in appsettings.json
   ```
   "MongoDB": {
       "ConnectionString": "mongodb+srv://username:password@cluster.mongodb.net/",
       "DatabaseName": "SmartCivicAlertSystem"
   }
   ```

3. Configure Gemini API key in appsettings.json
   ```
   "GeminiApiKey": {
       "Citizen": "YOUR_GEMINI_API_KEY_HERE"
   }
   ```

4. Restore project dependencies
   ```
   dotnet restore
   ```

5. Build the project
   ```
   dotnet build
   ```

6. Run the application
   ```
   dotnet run
   ```

7. Access the application at https://localhost:7148

---

## Database Schema

### Incident Collection

| Field | Type | Description |
|-------|------|-------------|
| Id | ObjectId | Primary key |
| Title | String | Incident title |
| Description | String | Detailed incident description |
| Location | String | Incident location address |
| Latitude | Double | GPS latitude coordinate |
| Longitude | Double | GPS longitude coordinate |
| Severity | String | Critical, High, Medium, Low |
| Status | String | Open, In Progress, Resolved |
| Department | String | Fire, Police, Rescue |
| ReportedBy | String | User ID of reporter |
| ReportedByName | String | Name of reporter |
| ImagePath | String | Path to uploaded image |
| ReportedAt | DateTime | Report creation timestamp |
| UpdatedAt | DateTime | Last update timestamp |
| Comments | Array | Incident comments |

### User Collection

| Field | Type | Description |
|-------|------|-------------|
| Id | ObjectId | Primary key |
| Name | String | Full name |
| Email | String | Email address |
| PasswordHash | String | Secured password hash |
| Phone | String | Phone number |
| Role | String | Citizen, Operator, Admin |
| CreatedAt | DateTime | Account creation timestamp |

---

## Detailed Feature Explanations

# Incident Reporting

Citizens can report incidents by providing details such as title, description, location, severity, and an optional image. AI can automatically generate incident information from uploaded images.

# SOS Emergency

Allows users to send one-click emergency alerts with their current location. The system creates a high-priority incident and instantly notifies the relevant department.

# Reports Management

Users can view incident history, track report status, and export selected reports as PDF or CSV.

# Real-Time Notifications

SignalR provides instant notifications for incident updates, SOS alerts, and department-specific events without requiring a page refresh.

## AI Features
# AI Vision

Uses Google Gemini Vision API to analyze uploaded images and automatically generate the incident title, description, severity, and recommended department.

# AI Department Recommendation

Analyzes incident descriptions and suggests the most appropriate department for handling the incident.

# AI Report Summarizer

Generates concise summaries of incident reports, helping administrators and operators review incidents more efficiently.

# RAG Chatbot

Enables administrators to query incident data using natural language and receive context-aware responses powered by Retrieval-Augmented Generation (RAG).

# City Assistant Chatbot

Provides citizens with instant assistance for platform navigation, emergency contacts, safety tips, and common queries.

# Interactive Incident Mapping

Uses Leaflet.js and OpenStreetMap to display incident locations on an interactive map, helping operators monitor and respond to incidents efficiently.

---

## User Roles

### Citizen
- Report incidents with optional photo upload
- View and track incident status
- Download reports as PDF or CSV
- Send SOS alerts with location
- Access safety tips and guidelines
- Manage personal profile information
- Use AI Vision for photo analysis
- Interact with City Assistant Chatbot

### Operator
- View and manage all incidents
- Update incident status and details
- Receive real-time notifications
- Create incidents manually
- View department-specific analytics
- Access OpenStreetMap for incident visualization

### Admin
- Monitor all system incidents
- Manage citizen and operator users
- Manage admin profile and account
- Oversee system operations
- Track incident progress and response time
- Access RAG Chatbot for intelligent data retrivel
- Can view reports of all incidents and summarize them using AI Summerizer
- Full system configuration access

---

## Contributors

- Romah Maqsood: Citizen Dashboard, AI Vision Integration, Reports Module, Profile Management, Notifications, City Assistant Chatbot
- Taqdees: Admin Module, Priority System, RAG Chatbot Integration, Database Design , SignalR Hub , AI Report Summarizer
- Samra Ramzan: Operator Dashboard, Incident Management, OpenStreetMap Integration, Analytics

---

## License

This project is developed for educational and development purposes.

---

## Contact

For queries or support, contact the development team through GitHub at Romah-Maqsood/SmartCivicAlertSystem.

---

## Project Status

All core modules are complete and functional. The system is ready for deployment and testing.

| Module | Status |
|--------|--------|
| Citizen Dashboard | Complete |
| Operator Dashboard | Complete |
| Admin Dashboard | Complete |
| AI Integration | Complete |
| Real-Time Notifications | Complete |
| Reports Export | Complete |
| SOS Emergency | Complete |
| Chatbot Assistant | Complete |
| Profile Management | Complete |
| OpenStreetMap Integration | Complete |
| RAG Chatbot | Complete |

---

## Key Achievements

The project successfully integrates multiple technologies including ASP.NET Core, MongoDB, SignalR, Google Gemini AI, OpenStreetMap, and Chart.js. The system provides comprehensive incident management with AI-powered features, real-time notifications, and professional report generation. The modular architecture allows for easy maintenance and future enhancements.
