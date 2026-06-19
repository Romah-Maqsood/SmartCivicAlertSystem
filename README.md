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
- User management for citizens and operators
- System oversight with comprehensive incident monitoring
- Priority-based incident sorting and management
- RAG Chatbot implementation for advanced query handling
- Full system access and configuration

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

### Incident Reporting
Citizens can submit incident reports through a comprehensive form. The system supports manual entry of title, description, location, severity, and department selection. Optional photo upload allows citizens to attach images up to 5MB in JPG or PNG format. The AI Vision feature analyzes uploaded photos using Google Gemini API to automatically populate title, description, severity, and department fields. The AI department suggestion provides intelligent recommendations based on the incident description.

### SOS Emergency
The SOS feature provides one-click emergency alert functionality. Citizens enter or auto-detect their current location using browser geolocation. The system immediately notifies the Rescue Department, creates a Critical priority incident, and alerts all departments in real-time through SignalR. A confirmation toast message appears after successful submission.

### Reports Export
Citizens can export incident reports in PDF and CSV formats. The Reports page displays all incidents with checkboxes for selective download. Users can select specific incidents and download them as professional PDF documents or Excel-compatible CSV files. The download process generates only the selected reports without including the sidebar or other UI elements.

### Real-Time Notifications
SignalR integration provides live notifications for incident status changes. Notifications are department-specific, ensuring relevant personnel receive appropriate alerts. Users can mark notifications as read or resolved directly from the interface. The system supports in-app toast notifications for important updates.

### AI Features
Google Gemini Vision API powers the AI image analysis functionality. The system analyzes uploaded incident photos to detect incident type, estimate severity, and suggest appropriate departments. The AI department suggestion analyzes incident descriptions to recommend the most suitable department. The RAG Chatbot in the admin module provides advanced query handling capabilities.

### City Assistant Chatbot
The chatbot provides quick help for common queries, emergency numbers, and navigation support. Responses are professional, informative, and formatted with clean bullet points. The chatbot guides users through platform features without using emojis or informal language.

### Operator Mapping
Operators can view incident locations on OpenStreetMap integration. The mapping functionality displays incident pins with severity indicators, allowing operators to visualize incident distribution and respond efficiently.

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
- Oversee system operations
- Access RAG Chatbot for advanced queries
- Priority-based incident sorting
- Full system configuration access

---

## Contributors

- Romah Maqsood: Citizen Dashboard, AI Vision Integration, Reports Module, Profile Management, Notifications, City Assistant Chatbot
- Taqdees: Admin Dashboard, Priority System, RAG Chatbot Integration, Database Design
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
