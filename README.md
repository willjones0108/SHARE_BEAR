# JMUcare System - Installation and Usage Guide

## Database Setup

### Setting Up Authentication Database

1. Create a database named `AUTH` in your SQL Server instance
2. Run the following SQL script to set up the authentication tables:


```sql
CREATE TABLE dbo.HashedCredentials (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Password NVARCHAR(MAX) NOT NULL
);

-- Create the login stored procedure
CREATE PROCEDURE sp_Lab3Login
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Password 
    FROM HashedCredentials 
    WHERE Username = @Username;
END;

```

3. Initialize with default user accounts (default password is "password" for all accounts):


```sql
INSERT INTO HashedCredentials (Username, Password)
VALUES ('admin', '1000:fF2zbk9sOvF/RDj8S2hVZlgNmNYUMUsz:jJv9Og+6vJq2E+vZ91av8XQil4I=');
INSERT INTO HashedCredentials (Username, Password) 
VALUES ('user1', '1000:fF2zbk9sOvF/RDj8S2hVZlgNmNYUMUsz:jJv9Og+6vJq2E+vZ91av8XQil4I=');
INSERT INTO HashedCredentials (Username, Password) 
VALUES ('user2', '1000:fF2zbk9sOvF/RDj8S2hVZlgNmNYUMUsz:jJv9Og+6vJq2E+vZ91av8XQil4I=');
INSERT INTO HashedCredentials (Username, Password) 
VALUES ('user3', '1000:fF2zbk9sOvF/RDj8S2hVZlgNmNYUMUsz:jJv9Og+6vJq2E+vZ91av8XQil4I=');

```

### Setting Up Main Application Database

1. Create a database named `JMU_CARE` in your SQL Server instance
2. Run the `JMUcare_SQL.txt` script to create all required tables and initial data
3. Update the connection strings in `DBclass.cs` to match your SQL Server instance:


```csharp
public static readonly string JMUcareDBConnString =
    "Server=YOUR_SERVER_NAME;Database=JMU_CARE;Trusted_Connection=True";

private static readonly string? AuthConnString =
    "Server=YOUR_SERVER_NAME;Database=AUTH;Trusted_Connection=True";

```


2. **Input Validation**: Always validate user input before processing:


```csharp
if (!ModelState.IsValid)
{
    // Repopulate data and return to form
    return Page();
}

```


## Using the System

### Logging In

1. Run the Project
2. Enter your username and password 
3. Click "Login" to access the system




### Dashboard Navigation

The main dashboard provides access to:

- Grants you have permissions to view
- Projects assigned to you
- Tasks requiring your attention
- Phases you're involved in
- System notifications and messages (To be Implemeneted)

## Managing Grants

### Creating a Grant

1. Navigate to Grants section and click "Create New Grant"
2. Fill in all required information:
   - Grant Title
   - Category
   - Funding Source
   - Amount
   - Status
   - Description
3. Assign a Grant Lead who will be responsible for the grant
4. Save the grant

### Editing a Grant

1. Open the grant you wish to edit
2. Click "Edit" to modify details
3. Make your changes and save

### Managing Grant Permissions

1. Open the grant and navigate to Permissions tab
2. Add users and set their access level:
   - **Edit**: Can modify grant details
   - **View**: Can only view grant details
   - **None**: No access

## Managing Phases

### Creating Phases

1. From a grant view, click "Add Phase"
2. Enter phase details:
   - Phase Name
   - Description
   - Status
   - Phase Lead
3. Save the phase

### Ordering Phases

Phases are displayed in order of completion. You can manually adjust their order:

1. Navigate to the grant view showing all phases and select reorder phases
2. Use the arrows to reorder phases

## Managing Projects

### Creating a Project

1. From a phase view, click "Add Project"
2. Fill in project details:
   - Title
   - Description
   - Project Type
   - Tracking Status
3. Save the project/Add permissions (Bugs Present)

### Project Permissions

Each project inherits permissions from its parent phase, but you can also set specific permissions:

1. Open project settings
2. Navigate to Permissions tab
3. Add or modify user access levels

## Task Management

### Creating Tasks

1. From a project view, click "Add Task"
2. Enter task details:
   - Task Content
   - Due Date
   - Status
3. Assign users to the task with specific roles (Bugs Present)
4. Save the task

### Updating Task Status

1. Open the task
2. Change status (Pending, In Progress, Completed, etc.)
3. Save changes

## Messaging System


### Accessing Messages

1. Navigate to the Messages section from the main navigation menu
2. The message center displays two main sections:
   - Received Messages
   - Sent Messages

### Reading Messages

1. All your received messages appear in the "Received Messages" section
2. Messages display:
   - Sender's name
   - Message content
   - Date and time sent
   - Read status
3. Click "Mark as Read" to update the message status (Bugs Present)

### Sending Messages

1. In the message center, locate the "Send a Message" section
2. Select a recipient from the dropdown menu
3. Type your message in the text area
4. Click "Send" to dispatch your message

### Managing Message Status (To be Implemented)

Messages have three possible statuses:
- **Sent**: Message has been delivered but not yet read
- **Read**: Recipient has viewed the message
- **Archived**: Message has been archived (not shown in main view)

## Calendar System

The Calendar feature helps users track task due dates and deadlines visually.

### Viewing the Calendar

1. Select "Calendar" 
2. The calendar displays all tasks color-coded by status: 
   - Green: Completed tasks
   - Yellow: In-progress tasks
   - Blue: Pending tasks
   - Red: Overdue or other status tasks

### Calendar Features

The calendar interface provides several viewing options:
- Month view (default)
- Week view
- Day view

You can navigate between months using the prev/next buttons or jump to today.

### Task Details

Click on any task in the calendar to view details:
- Task name
- Due date
- Current status
- Project association (To be Implemented)

### Calendar Integration

The calendar automatically integrates with:
1. Project tasks - shown based on due dates
2. Phases - displayed based on expected completion dates (To be Implemented)
3. Grant deadlines - important dates from grants appear in the calendar (To be Implemented)

For improved organization, tasks inherit their color coding from their current status, making it easy to identify high-priority items at a glance.
