# Customer Support Bot

## Overview
The Customer Support Bot is a full-stack application designed to streamline customer support interactions. It features a chatbot powered by Semantic Kernel, a React-based frontend, and a .NET backend with SignalR for real-time updates.

## Features
- **Chatbot**: AI-powered chatbot for customer support.
- **Admin Panel**: Manage users, forms, and sessions.
- **Real-Time Updates**: SignalR integration for live updates.
- **Session Management**: Redis-backed session storage.
- **Authentication**: JWT-based authentication with role-based access control.
- **Form Logging**: Log and manage customer forms.

## Technologies Used
### Backend
- **.NET 9**: Minimal APIs, Entity Framework Core.
- **SignalR**: Real-time communication.
- **Redis**: Session storage.
- **Semantic Kernel**: AI chatbot integration.
- **SQL Server**: Database.

### Frontend
- **React**: Component-based UI.
- **Vite**: Fast build tool.
- **Tailwind CSS**: Styling.
- **Radix UI**: Accessible components.

## Installation
### Prerequisites
- Node.js
- .NET SDK
- SQL Server
- Redis

### Steps
1. Clone the repository:
   ```bash
   git clone https://github.com/lliamsymonds04/customer-support-bot.git
   cd customer-support-bot
   ```

2. Install dependencies:
   ```bash
   cd frontend
   npm install
   cd ../backend
   dotnet restore
   ```

3. Configure environment variables:
   - **Frontend**: Update `.env` with `VITE_API_URL`.
   - **Backend**: Use `appsettings.json` and `dotnet user-secrets` for sensitive data.

4. Run the application:
   - **Backend**:
     ```bash
     dotnet run
     ```
   - **Frontend**:
     ```bash
     npm run dev
     ```

5. Access the application:
   - Frontend: `http://localhost:5173`
   - Backend: `http://localhost:5000`

## Usage
### Admin Panel
- Navigate to `/admin` to manage users, forms, and sessions.

### Chatbot
- Interact with the chatbot on the home page.

## Development
### Running Migrations
1. Add a migration:
   ```bash
   dotnet ef migrations add <MigrationName>
   ```
2. Apply the migration:
   ```bash
   dotnet ef database update
   ```

### Testing
- Run unit tests:
  ```bash
  dotnet test
  ```

## License
This project is licensed under the MIT License. See the LICENSE file for details.

## Contact
For questions or support, please contact [Lliam Symonds](mailto:lliamsymonds04@gmail.com).

