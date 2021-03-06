﻿using System;
using System.Collections.Generic;
using AUBTimeManagementApp.DataContracts;
using AUBTimeManagementApp.GUI;
using System.Windows.Forms;

namespace AUBTimeManagementApp.Client
{
    public sealed class Client
    {
        private static readonly Client instance = new Client(); //Singleton

        private static readonly string localIP = "127.0.0.1";
        private static readonly string onlineIP = "37.209.253.194";

        private static readonly string serverIP = localIP;
        private static readonly int serverPort = 8020;

        public string username;
        private List<Team> teams;   
        private List<Invitation> Invitations;

        //Connects the users to the active open form
        public RegistrationForm registrationForm { get; private set; }
        public mainForm mainForm { get; private set; }
        public SignInUpForm signInUpForm { get; private set; }
        public TeamsForm teamsForm { get; private set; }
        public TeamDetailsForm teamDetailsForm { get; private set; }
        public TeamCalendarForm teamCalendarForm { get; private set; }
        public InvitationsForm invitationsForm { get; private set; }

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static Client()
        {
            
        }
    
        //Constructor executed once the first time Client is used
        private Client()
        {
            teams = new List<Team>();
            Invitations = new List<Invitation>();
        }
        public static Client Instance
        {
            get
            {
                return instance;
            }
        }

        public void initializeSocket() {
            ClientHandleData.InitializePacketListener();
            ClientTCP.InitializeClientSocket(serverIP, serverPort);
        }

        /// <summary>
        /// Stores a pointer to the currently opened form in Client
        /// </summary>
        /// <param name="form">The form to connect</param>
        public void setForm(Form form) {
            if (form.GetType() == typeof(RegistrationForm)) {
                registrationForm = (RegistrationForm)form;
            }
            else if (form.GetType() == typeof(mainForm)) {
                mainForm = (mainForm)form;
            }
            else if (form.GetType() == typeof(SignInUpForm)) {
                signInUpForm = (SignInUpForm)form;
            }
            else if (form.GetType() == typeof(TeamsForm)) {
                teamsForm = (TeamsForm)form;
            }
            else if (form.GetType() == typeof(TeamDetailsForm)) {
                teamDetailsForm = (TeamDetailsForm)form;
            }
            else if (form.GetType() == typeof(TeamCalendarForm)) {
                teamCalendarForm = (TeamCalendarForm)form;
            }
            else if (form.GetType() == typeof(InvitationsForm))
                invitationsForm = (InvitationsForm)form;
        }

		#region Account
		public void createAccount(string username, string firstName, string lastName, string password, string confirmPassword, string email, DateTime dateOfBirth) {            
            this.username = username;
            ClientTCP.PACKET_Register(username, firstName, lastName, password, confirmPassword, email, dateOfBirth);
        }
        public void registerReply(int OK) {
            if (registrationForm.InvokeRequired) {
                //We are calling a method of the form from a different thread
                //Need to use invoke to make it threadsafe
                registrationForm.Invoke(new MethodInvoker(delegate { registrationForm.registrationReply(OK); }));
            }
            else { registrationForm.registrationReply(OK); }
        }

        public void logIn(string username, string password) {
            this.username = username;
            ClientTCP.PACKET_Login(username, password);
        }
        public void logInReply(bool OK) {
            if (signInUpForm.InvokeRequired) {
                //We are calling a method of the form from a different thread
                //Need to use invoke to make it threadsafe
                signInUpForm.Invoke(new MethodInvoker(delegate { signInUpForm.loginReply(OK); }));
            }
            else { signInUpForm.loginReply(OK); }
        }

        public void logOut()
        {
            teams.Clear(); Invitations.Clear();
        }
        #endregion

        #region PersonalEvents

        /// <summary>
        /// Relays to the server the user's request to create a new event
        /// </summary>
        /// <param name="eventName">The name of thee event</param>
        /// <param name="priority">The priority of the event</param>
        /// <param name="start">The start time of the event</param>
        /// <param name="end">The end time of the event</param>
        public void CreateUserEvent(string eventName, int priority, DateTime start, DateTime end, string Link)
        {
            Console.WriteLine(eventName + " " + priority + " " + start.ToString() + " " + end.ToString());
            ClientTCP.PACKET_CreateUserEvent(username, eventName, priority, start, end, Link);
;       }

        /// <summary>
        /// Is executed when the client receives a reply from the server regarding an event created by the client
        /// </summary>
        /// <param name="_event">The created event</param>
        /// <param name="conflictingEvents">A list of conflicting events</param>
        public void CreateUserEventReply(Event _event, List<Event> conflictingEvents)
        {
            if (teamCalendarForm != null && teamCalendarForm.Enabled) {
                if (teamCalendarForm.mergedCalendarShown) {
                    if (teamCalendarForm.InvokeRequired)
                        teamCalendarForm.Invoke(new MethodInvoker(delegate { teamCalendarForm.SubmitButton_Click(null, null); }));
                    else
                        teamCalendarForm.SubmitButton_Click(null, null);
                }
                else {
                    if (teamCalendarForm.InvokeRequired)
                        teamCalendarForm.Invoke(new MethodInvoker(delegate { teamCalendarForm.displayEvent(_event); }));
                    else
                        teamCalendarForm.displayEvent(_event);
                }
            }
            else if (mainForm != null && mainForm.Enabled) {
                showEvent(_event.ID, _event.eventName, _event.priority, _event.startTime, _event.endTime, _event.teamEvent, _event.link);
            }

            if (conflictingEvents.Count != 0) {
                mainForm.informUserAboutConflicts(_event, conflictingEvents);
            }
        }

        /// <summary>
        /// remove event from user's schedule
        /// </summary>
        /// <param name="eventID"></param>
        public void CancelUserEvent(int eventID, bool isTeamEvent)
        {
            ClientTCP.PACKET_CancelUserEvent(username, eventID, isTeamEvent);
        }

        /// <summary>
        /// Modify personal event
        /// </summary>
        /// <param name="eventID"></param>
        public void ModifyUserEvent(Event updatedEvent)
        {
            ClientTCP.Packet_ModifyUserEvent(updatedEvent, username);
        }

        /// <summary>
        /// Displays thee event on the calendar
        /// </summary>
        public void ShowEvent(Event _event)
        {
            showEvent(_event.ID, _event.eventName, _event.priority, _event.startTime, _event.endTime, _event.teamEvent, _event.link);
        }

        /// <summary>
        /// Displays the event on the calendar
        /// </summary
        public void showEvent(int eventID, string eventName, int priority, DateTime startDate, DateTime endDate, bool teamEvent, string Link)
        {
            if (mainForm.InvokeRequired)
                mainForm.Invoke(new MethodInvoker(delegate { mainForm.displayEvent(eventID, eventName, priority, startDate, endDate, teamEvent, Link); }));
            else
                mainForm.displayEvent(eventID, eventName, priority, startDate, endDate, teamEvent, Link);
        }

        #endregion

        #region TeamEvents
        /// <summary>
        /// Relays the request to the server to create a team event
        /// </summary>
        /// <param name="TeamID">The ID of the team</param>
        /// <param name="eventName">The name of the event</param>
        /// <param name="priority">The priority of the event</param>
        /// <param name="startDate">The start time of the event</param>
        /// <param name="endDate">The end time of the event</param>
        public void CreateTeamEvent(int TeamID, string eventName, int priority, DateTime startDate, DateTime endDate, string Link)
        {
            ClientTCP.PACKET_CreateTeamEvent(TeamID, username, eventName, priority, startDate, endDate, Link);
        }

        /// <summary>
        /// Relays the request to the server to cancel a team event
        /// </summary>
        /// <param name="eventID">The ID of the event to cancel</param>
        public void CancelTeamEvent(int eventID) 
        {
            ClientTCP.PACKET_CancelTeamEvent(eventID);
        }

        /// <summary>
        /// Relays the request to the server to modify a team event
        /// </summary>
        /// <param name="updatedEvent">The updated event</param>
        /// <param name="teamID">The ID of the team</param>
        public void ModifyTeamEvent(Event updatedEvent, int teamID)
        {
            ClientTCP.Packet_ModifyTeamEvent(updatedEvent, teamID);
        }

        #endregion

        #region Team
        public List<Team> getTeams() { return teams; }
        /// <summary>
        /// Relays the request to the server to create a team
        /// </summary>
        /// <param name="teamName"></param>
        /// <param name="teamMembers"></param>
        public void createTeam(string teamName, string[] teamMembers) {
            ClientTCP.PACKET_CreateTeam(teamName, username, teamMembers);
        }
        /// <summary>
        /// Receives from the server feedback for the create team request
        /// Displays the invalid usernames if any
        /// </summary>
        public void createTeamReply(bool OK, string[] invalidUsernames) {
            string title = OK ? "The team was successfully created!" : "There was an error, the team was not created";
            string info = "";
            if (OK) {
                if (invalidUsernames.Length == 0) { info = "All usernames provided were valid."; }
                else {
                    info = "The following provided usernames were invalid:\r\n";
                    info += String.Join(", ", invalidUsernames);
                }
            }

            if (teamsForm != null && teamsForm.Enabled) {
                if (teamsForm.InvokeRequired) {
                    teamsForm.Invoke(new MethodInvoker(delegate { teamsForm.showMessage(title, info); }));
                }
                else { teamsForm.showMessage(title, info); }
            }
        }

        /// <summary>
        /// Executes when the user gets added to a team; adds the team to the list
        /// </summary>
        public void addedToATeam(string teamName, int teamID, List<string> admins, List<string> members) {
            Team newTeam = new Team(teamID, teamName);
            foreach (string m in members) { newTeam.addMember(m); }
            foreach(string a in admins) { newTeam.addAdmin(a); }
            teams.Add(newTeam);

            if (teamsForm != null && teamsForm.Enabled) {
                if (teamsForm.InvokeRequired) {
                    teamsForm.Invoke(new MethodInvoker(delegate { teamsForm.addTeamEntry(newTeam); }));
                }
                else { teamsForm.addTeamEntry(newTeam); }
            }
        }

        /// <summary>
        /// Sets username in team teamID to be either admin (if admin true) or not
        /// </summary>
        /// <param name="teamID">The ID of the team</param>
        /// <param name="username">The username of the member to set/unset as admin</param>
        /// <param name="isNowAdmin">True if we want to set the member as admin, false if we want to set him as member</param>
        public void changeAdminState(int teamID, string username, bool isNowAdmin) {
            ClientTCP.PACKET_ChangeAdminState(teamID, username, isNowAdmin);
        }

        /// <summary>
        /// Called when the user receives a notification that some admin state was changed in some team they are in
        /// </summary>
        /// <param name="teamID">The ID of the team</param>
        /// <param name="username">The username of the member that was set/unset as admin</param>
        /// <param name="isNowAdmin">True if the member is now an admin, false otherwise</param>
        public void adminStateChanged(int teamID, string username, bool isNowAdmin) {
            int idx = teams.FindIndex(a => a.teamID == teamID);
            if (idx == -1) { return; }

            if (isNowAdmin) { teams[idx].teamAdmin.Add(username); }
            else { teams[idx].teamAdmin.Remove(username); }

            if (teamDetailsForm != null && teamDetailsForm.Enabled && teamDetailsForm.team.teamID == teamID) {
                if (teamDetailsForm.InvokeRequired) {
                    teamDetailsForm.Invoke(new MethodInvoker(delegate { teamDetailsForm.tryUpdatingTeam(); }));
                }
                else { teamDetailsForm.tryUpdatingTeam(); }
            }
        }
        
        /// <summary>
        /// Remove a member from a team
        /// </summary>
        /// <param name="teamID">The ID of the team</param>
        /// <param name="username">The member to remove (could be the current user)</param>
        public void removeMember(int teamID, string username) {
            ClientTCP.PACKET_RemoveMember(teamID, username);
        }

        /// <summary>
        /// Called when the user receives a notification that some member of a team they are in was removed
        /// </summary>
        /// <param name="teamID">The ID of the team</param>
        /// <param name="username">Tge username of the removed member</param>
        public void memberRemoved(int teamID, string username) {
            int idx = teams.FindIndex(a => a.teamID == teamID);
            if (idx == -1) { return; }

            bool teamDetailsOpen = teamDetailsForm != null && teamDetailsForm.Enabled && teamDetailsForm.team.teamID == teamID;
            if (username == this.username) { 
                teams.RemoveAt(idx); 
                if (teamDetailsOpen) {
                    if (teamDetailsForm.InvokeRequired) {
                        teamDetailsForm.Invoke(new MethodInvoker(delegate { teamDetailsForm.goBack(); }));
                    }
                    else { teamDetailsForm.goBack(); }
                }

                //Remove related invitations
                List<Invitation> newInvitations = new List<Invitation>();
                foreach (Invitation inv in Invitations) {
                    if (inv.TeamID != teamID) { newInvitations.Add(inv); }
                }
                Invitations = newInvitations;
                updateInvitationNotification();
                if (invitationsForm != null && invitationsForm.Enabled) {
                    if (invitationsForm.InvokeRequired) {
                        invitationsForm.Invoke(new MethodInvoker(delegate { invitationsForm.DisplayInvitations(); }));
                    }
                    else { invitationsForm.DisplayInvitations(); }
                }
            }
            else {
                teams[idx].removeMember(username);
                if (teamDetailsOpen) {
                    if (teamDetailsForm.InvokeRequired) {
                        teamDetailsForm.Invoke(new MethodInvoker(delegate { teamDetailsForm.tryUpdatingTeam(); }));
                    }
                    else { teamDetailsForm.tryUpdatingTeam(); }
                }
            }
        }

        /// <summary>
        /// Adds a member to a team
        /// </summary>
        /// <param name="teamID">The ID of the team</param>
        /// <param name="username">The username of the user to add to the team</param>
        public void addMember(int teamID, string username) {
            ClientTCP.PACKET_AddMember(teamID, username);
        }

        /// <summary>
        /// Feedback of a request of the user made to add a member to a team
        /// </summary>
        /// <param name="OK">True if the member was added, false otherwise</param>
        public void addMemberReply(int teamID, bool OK) {
            string feedback = (OK ? "Member added" : "The username is not valid");
            bool teamDetailsOpen = teamDetailsForm != null && teamDetailsForm.Enabled && teamDetailsForm.team.teamID == teamID;
            if (teamDetailsOpen) {
                if (teamDetailsForm.InvokeRequired) {
                    teamDetailsForm.Invoke(new MethodInvoker(delegate { teamDetailsForm.addMemberFeedback(feedback); }));
                }
                else { teamDetailsForm.addMemberFeedback(feedback); }
            }
        }

        /// <summary>
        /// Called when the user receives a notification that some member of a team they are in was added
        /// </summary>
        /// <param name="teamID">The ID of the team</param>
        /// <param name="username">Tge username of the addd member</param>
        public void memberAdded(int teamID, string username) {
            int idx = teams.FindIndex(a => a.teamID == teamID);
            if (idx == -1) { return; }

            teams[idx].addMember(username);
            bool teamDetailsOpen = teamDetailsForm != null && teamDetailsForm.Enabled && teamDetailsForm.team.teamID == teamID;
            if (teamDetailsOpen) {
                if (teamDetailsForm.InvokeRequired) {
                    teamDetailsForm.Invoke(new MethodInvoker(delegate { teamDetailsForm.tryUpdatingTeam(); }));
                }
                else { teamDetailsForm.tryUpdatingTeam(); }
            }
        }

        /// <summary>
        /// Gets all the teams this user is in from the server
        /// </summary>
        public void GetUserTeams()
        {
            ClientTCP.PACKET_GetUserTeams(username);
        }

        /// <summary>
        /// Updates the teams according to the server's reply
        /// </summary>
        /// <param name="teams"></param>
        public void GetUserTeamsReply(List<Team> teams)
        {
            this.teams = teams;
        }
		#endregion

		#region User and Team Schedules
		/// <summary>
		/// Gets all the events this user is attending from the server
		/// </summary>
		public void GetUserSchedule()
        {
            ClientTCP.PACKET_GetUserSchedule(username);
        }

        /// <summary>
        /// Displays the events received from the server
        /// </summary>
        /// <param name="n">Number of events</param>
        /// <param name="eventsList">List of events to display</param>
        public void GetUserScheduleReply(int n, List<Event> eventsList)
		{
            for (int i = 0; i < n; i++)
            {
                int eventID = eventsList[i].ID;
                string name = eventsList[i].eventName;
                int priority = eventsList[i].priority;
                DateTime start = eventsList[i].startTime;
                DateTime end = eventsList[i].endTime;
                bool isTeamEvent = eventsList[i].teamEvent;
                string Link = eventsList[i].link;
                
                if (mainForm.InvokeRequired)
                    mainForm.Invoke(new MethodInvoker(delegate { mainForm.displayEvent(eventID, name, priority, start, end, isTeamEvent, Link); }));
                else
                    mainForm.displayEvent(eventID, name, priority, start, end, isTeamEvent, Link);
            }
        }

        /// <summary>
        /// Get all the events scheduled for this team form the server
        /// </summary>
        /// <param name="teamID">The ID of the team</param>
        public void GetTeamSchedule(int teamID)
        {
            ClientTCP.PACKET_GetTeamSchedule(teamID);
        }
        /// <summary>
        /// Displays the events corresponding to a specific team
        /// </summary>
        /// <param name="teamID">ID of the team in question</param>
        /// <param name="eventsList">List of events to display</param>
        public void GetTeamScheduleReply(int teamID, List<Event> eventsList)
        {
            if (teamCalendarForm != null && teamCalendarForm.Enabled && teamCalendarForm.team.teamID == teamID && !teamCalendarForm.mergedCalendarShown) {
                for (int i = 0; i < eventsList.Count; i++) {
                    if (teamCalendarForm.InvokeRequired)
                        teamCalendarForm.Invoke(new MethodInvoker(delegate { teamCalendarForm.displayEvent(eventsList[i]); }));
                    else
                        teamCalendarForm.displayEvent(eventsList[i]);
                }
            }
        }

        /// <summary>
        /// Gets the merged schedule of all members of a team
        /// </summary>
        /// <param name="teamID">The ID of the team</param>
        public void GetMergedTeamSchedule(int teamID, DateTime startTime, DateTime endTime, int priorityThreshold) {
            ClientTCP.PACKET_GetMergedTeamSchedule(teamID, startTime, endTime, priorityThreshold);
        }
       /// <summary>
       /// Displays the merged schedule according to the received frequency
       /// </summary>
       /// <param name="teamID">The ID of the team</param>
       /// <param name="freq">Shows the ratio of busy people each point in time</param>
        public void GetMergedTeamScheduleReply(int teamID, double[,] freq) {
            if (teamCalendarForm != null && teamCalendarForm.Enabled && teamCalendarForm.team.teamID == teamID && teamCalendarForm.mergedCalendarShown) {
                if (teamCalendarForm.InvokeRequired)
                    teamCalendarForm.Invoke(new MethodInvoker(delegate { teamCalendarForm.displayColorFreq(freq); }));
                else
                    teamCalendarForm.displayColorFreq(freq);
            }
        }

        /// <summary>
        /// get the events of specified priority that the user is attending from the srever
        /// </summary>
        public void FilterUserSchedule(bool low, bool medium, bool high)
        {
            ClientTCP.PACKET_FilterUserSchedule(username, low, medium, high);
        }

        /// <summary>
        /// Shows the received events which are filtered according to what was requested
        /// </summary>
        public void FilterUserScheduleReply(int n, List<Event> eventsList)
        {
            for (int i = 0; i < n; i++)
            {
                int eventID = eventsList[i].ID;
                string name = eventsList[i].eventName;
                int priority = eventsList[i].priority;
                DateTime start = eventsList[i].startTime;
                DateTime end = eventsList[i].endTime;
                bool isTeamEvent = eventsList[i].teamEvent;
                string Link = eventsList[i].link;

                if (mainForm.InvokeRequired)
                {
                    //We are calling a method of the form from a different thread
                    //Need to use invoke to make it threadsafe
                    mainForm.Invoke(new MethodInvoker(delegate { mainForm.displayEvent(eventID, name, priority, start, end, isTeamEvent, Link); }));
                }

                else { mainForm.displayEvent(eventID, name, priority, start, end, isTeamEvent, Link); }
            }
        }
        #endregion

        #region Invitations

        /// <summary>
        /// Sends a request to retrieve all the user invitations from the server
        /// </summary>
        public void GetUserInvitations()
        {
            ClientTCP.PACKET_GetUserInvitations(username);
        }
        /// <summary>
        /// Receives all the invitations from the server and set the notifications accordingly
        /// </summary>
        public void GetUserInvitationsReply(List<Invitation> invitations)
        {
            Invitations = invitations;
            updateInvitationNotification();
        }
        /// <summary>
        /// Received one new invitation; add it to the list and display it
        /// </summary>
        /// <param name="invitation">The new invitation</param>
        public void receivedInvitation(Invitation invitation) {
            Invitations.Add(invitation);
            updateInvitationNotification();

            if (invitationsForm != null && invitationsForm.Enabled) {
                if (invitationsForm.InvokeRequired) {
                    invitationsForm.Invoke(new MethodInvoker(delegate { invitationsForm.AddInvitationEntry(invitation); }));
                }
                else { invitationsForm.AddInvitationEntry(invitation); }
            }
        }

        /// <summary>
        /// Send to the server that one invitation was accepted and remove this invitation from the list
        /// </summary>
        /// <param name="invitation"></param>
        public void AcceptInvitation(Invitation invitation)
        {
            ClientTCP.PACKET_AcceptInvitation(invitation, username);
            Invitations.Remove(invitation);
            updateInvitationNotification();
        }
        /// <summary>
        /// Send to the server that one invitation was rejected and remove this invitation from the list
        /// </summary>
        public void DeclineInvitation(Invitation invitation)
        {
            ClientTCP.PACKET_DeclineInvitation(invitation, username);
            Invitations.Remove(invitation);
            updateInvitationNotification();
        }

        /// <summary>
        /// Updates the number that shows how many unanswered notifications there is
        /// </summary>
        private void updateInvitationNotification() {
            if (mainForm != null && mainForm.Enabled) {
                if (mainForm.InvokeRequired) {
                    mainForm.Invoke(new MethodInvoker(delegate { mainForm.updateInvitationNotification(Invitations.Count); }));
                }
                else { mainForm.updateInvitationNotification(Invitations.Count); }
            }
        }

        public List<Invitation> GetInvitations() { return Invitations; }

        #endregion
    }
}
