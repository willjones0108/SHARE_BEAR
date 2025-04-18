# JMUcare System - Installation and Usage Guide

# Team: Care Bears 

### AI Disclaimer

Artificial Intelligence (AI) tools were used extensively throughout the development of this project. 

Specifically, the following AI technologies were utilized:

OpenAI GPT-4o & Anthropic Claude 3.7 – for generating and refining content, code, debugging and documentation.

GitHub Copilot – for real-time coding suggestions and automated code completion within the development environment.

These tools were employed to enhance productivity, improve quality, and accelerate development. 
All AI-generated content was reviewed and validated by the project team to ensure accuracy and appropriateness.

## Database Connection

Please note we migrated OFF of azure back to local host. 
Some functionality may have been impacted, however no bugs were found in our testing in regards to the database.

1.	Create the Databases
•	Set up two databases:
•	AUTH
•	JMU_CARE
2.	Run the Stored Procedure
•	Locate the file sp_Lab3Login.txt in the project directory.
•	Execute the SQL script in sp_Lab3Login.txt on the AUTH database to create the required stored procedure.
3.	Verify the Connection String
•	Open the DBClass file in the project.
•	Ensure the connection strings for both AUTH and JMU_CARE databases are accurate and match your local or server database configuration.

* if you have any errors regarding the database please reach out to jonesww@dukes.jmu.edu


## Using the System

#### Note that the databse and file sharing system is located on the cloud and the project runs locally.

### Logging In

1. Run the Project
2. Enter your username and password 
3. Click "Login" to access the system

User Account Details
| Username    | Password    | Role            | Default Access                                    | 
|-------------|----------|--------------------|---------------------------------------------------| 
|admin        | password | Admin              | Full access to all grants, phases, and projects   | 
|user1| password | Contributor        |                                                   | 
|user2| password | Contributor        |                                                   | 
|user3| password | Contributor        |                                                   | 


### Dashboard Navigation

The main dashboard provides access to:

- Grants you have permissions to view
- Projects assigned to you
- Tasks requiring your attention
- Phases you're involved in
- Messages
- Calander 
- User Management (Admin)

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

You can manually adjust the phase order:

1. Navigate to the grant view showing all phases and select reorder phases
2. Use the arrows to reorder phases

## Managing Projects (Task/Folders)

Project = Task = Folder 

The Project.Type determines the type

### Creating a Project

1. From a phase view, click "Add Task/Folder"
2. Fill in project details:
   - Title
   - Description
   - Tracking Status
3. Save the project/Add permissions

### Project Permissions

Each project inherits permissions from its parent phase, but you can also set specific permissions:

1. Open project settings
2. Navigate to Permissions tab
3. Add or modify user access levels


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

### Calender Task Details

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


## Document Upload System

Documents can be uploaded and organized at both the **grant level** and within individual **folders (projects)**. This enables users to maintain relevant documentation across different parts of the system.

### Uploading Documents to a Grant

1. Navigate to the **Grant View** page
2. Scroll to the **Documents** section
3. Click **"Upload Document"**
4. Select the file from your device and confirm the upload
5. The uploaded document will now be accessible to users with permission to view the grant

### Uploading Documents to a Folder

1. Navigate to the **Folder (Project)** view
2. In the **Documents** section, click **"Upload Document"**
3. Choose a file from your device and upload
4. The document will be stored under that specific folder, helping organize materials related to the task or phase

### Supported File Types

The system currently supports common document formats, including:
- PDF
- Word Documents (.doc, .docx)
- Excel Files (.xls, .xlsx)
- Text Files (.txt)
- Images (e.g., .png, .jpg)

Users must have appropriate permissions to upload or access documents in a given grant or folder.
