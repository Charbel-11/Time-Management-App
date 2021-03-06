﻿using Server.Service.Handlers;
using System;
using System.Collections.Generic;
using Server.DataContracts;
using Server.Service.ControlBlocks;
using AUBTimeManagementApp.Service.Storage;

namespace Server {
    /// <summary>
    /// Here the server parse the received message and initializes the appropriate handler
    /// </summary>
    class ServerHandleData {
        public delegate void PacketF(int ConnectionID, byte[] Data);    //Signature of functions of type (int, byte[])
        public static Dictionary<int, PacketF> PacketListener;          //Maps functionID -> function; read-only hence thread-safe

        /// <summary>
        /// Maps integers to corresponding functions
        /// </summary>
        public static void InitializePacketListener() {
            PacketListener = new Dictionary<int, PacketF> {
                { (int)ClientPackages.CLogin, HandleLogin },
                { (int)ClientPackages.CRegister, HandleRegister },
                { (int)ClientPackages.CGetUserTeams, HandleGetUserTeams },
                { (int)ClientPackages.CGetUserSchedule, HandleGetUserSchedule },
                { (int)ClientPackages.CGetTeamSchedule, HandleGetTeamSchedule },
                { (int)ClientPackages.CGetMergedTeamSchedule, HandleGetMergedTeamSchedule },
                { (int)ClientPackages.CFilterUserSchedule, HandleFilterUserSchedule },
                { (int)ClientPackages.CCreateTeam, HandleCreateTeam },
                { (int)ClientPackages.CChangeAdminState, HandleChangeAdminState },
                { (int)ClientPackages.CRemoveMember, HandleRemoveMember },
                { (int)ClientPackages.CAddMember, HandleAddMember },
                {(int)ClientPackages.CCreateTeamEvent, HandleCreateTeamEvent},
                {(int)ClientPackages.CCreateUserEvent, HandleCreateUserEvent },
                {(int)ClientPackages.CGetUserEvent, HandleGetUserEvent},
                {(int)ClientPackages.CCancelUserEvent, HandleCancelUserEvent },
                {(int)ClientPackages.CModifyUserEvent, HandleModifyUserEvent },
                {(int)ClientPackages.CGetUserInvitations, HandleGetUserInvitations },
                {(int)ClientPackages.CAcceptInvitation, HandleAcceptInvitationReply },
                {(int)ClientPackages.CDeclineInvitation, HandleDeclineInvitationReply },
                {(int)ClientPackages.CCancelTeamEvent, HandleCancelTeamEvent },
                {(int)ClientPackages.CModifyTeamEvent, HandleModifyTeamEvent }
            };
        }     

        /// <summary>
        /// Makes sure that we process every byte of every packet received
        /// </summary>
        /// <param name="ConnectionID"></param>
        /// <param name="data"></param>
        /// In case of packets > 4096 bytes, HandleData will be called mutliple times
        /// and only when all the data is here (length>=plength), the loop will be entered
        public static void HandleData(int ConnectionID, byte[] data)        //Static method is fine since each thread has its own stack 
        {
            try {
                /*
                //Writing on the console what is received (for debugging)
                foreach (byte bb in data) { Console.Write(bb + " "); }
                Console.Write('\n');
                foreach (byte bb in data) { Console.Write((char)bb); }
                Console.Write('\n');
                */
                Console.Write('\n');

                if (data == null) { Console.WriteLine("No data..."); return; }

                int pLength = 0;    //Packet length
                byte[] buffer = (byte[])data.Clone();   //To avoid shallow copies

                if (!ServerTCP.ClientObjects.ContainsKey(ConnectionID)) { return; }
                if (ServerTCP.ClientObjects[ConnectionID].bufferH == null)
                    ServerTCP.ClientObjects[ConnectionID].bufferH = new BufferHelper();

                ServerTCP.ClientObjects[ConnectionID].bufferH.WriteBytes(buffer);
                if (ServerTCP.ClientObjects[ConnectionID].bufferH.Length() < 4) {
                    Console.WriteLine("Buffer is too empty");
                    ServerTCP.ClientObjects[ConnectionID].bufferH.Clear();
                    return;
                }

                if (!ServerTCP.ClientObjects.ContainsKey(ConnectionID)) { return; }
                pLength = ServerTCP.ClientObjects[ConnectionID].bufferH.ReadInteger(false);  //Advances only when the whole packet is here
                while (pLength >= 4 && pLength <= ServerTCP.ClientObjects[ConnectionID].bufferH.Length() - 4)    //-4 since readPos hasn't advanced yet
                {
                    ServerTCP.ClientObjects[ConnectionID].bufferH.ReadInteger();

                    int packageID = ServerTCP.ClientObjects[ConnectionID].bufferH.ReadInteger();
                    pLength -= 4;
                    data = ServerTCP.ClientObjects[ConnectionID].bufferH.ReadBytes(pLength);

                    //Call the appropriate function in case of a correct packageID
                    if (PacketListener.TryGetValue(packageID, out PacketF packet))
                        packet.Invoke(ConnectionID, data);
                    else { Console.WriteLine("Wrong function ID"); pLength = 0; break; }

                    if (!ServerTCP.ClientObjects.ContainsKey(ConnectionID)) { return; }

                    pLength = 0;
                    if (ServerTCP.ClientObjects[ConnectionID].bufferH.Length() >= 4)
                        pLength = ServerTCP.ClientObjects[ConnectionID].bufferH.ReadInteger(false);
                }

                if (pLength < 4 && ServerTCP.ClientObjects.ContainsKey(ConnectionID)) { ServerTCP.ClientObjects[ConnectionID].bufferH.Clear(); }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        private static void HandleRegister(int ConnectionID, byte[] data)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            // Read username and password to buffer
            string username = bufferH.ReadString();
            string firstname = bufferH.ReadString();
            string lastName = bufferH.ReadString();
            string password = bufferH.ReadString();
            string confirmPassowrd = bufferH.ReadString();
            string email = bufferH.ReadString();
            int day = bufferH.ReadInteger();
            int month = bufferH.ReadInteger();
            int year = bufferH.ReadInteger();
            DateTime datoOfBirth = new DateTime(year, month, day);

            Console.WriteLine(username + " has joined the party!!!");

            bufferH.Dispose();

            // Call AccountsHandler 
            IAccountsHandler accountHandler = new AccountsHandler();
            int isRegistered = accountHandler.ConfirmRegistration(username, firstname, lastName, email, password, confirmPassowrd, datoOfBirth);

            Console.WriteLine(isRegistered);
            // If account exists notify the front end to change scenes
            ServerTCP.PACKET_SendRegisterReply(ConnectionID, isRegistered);
        }

        //Some client used PACKAGE_Login, we handle it here
        private static void HandleLogin(int ConnectionID, byte[] data) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            // Read username and password to buffer
            string username = bufferH.ReadString();
            string password = bufferH.ReadString();

            bufferH.Dispose();

            // Call AccountsHandler 
            IAccountsHandler accountsHandler = new AccountsHandler();
            bool isUser = accountsHandler.ConfirmLogIn(username, password);

            if (isUser) {
                Console.WriteLine(username + " just signed in!");
                ServerTCP.ClientObjects[ConnectionID].username = username;
                ServerTCP.UsernameToConnectionID[username] = ConnectionID;
            }
            else {
                Console.WriteLine("Invalid Username\\Password");
            }

            // If account exists notify the front end to change scenes
            ServerTCP.PACKET_SendLoginReply(ConnectionID, isUser);
        }

        private static void HandleGetUserTeams(int ConnectionID, byte[] data) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);
            // Read username to buffer
            string username = bufferH.ReadString();
            bufferH.Dispose();

            //get the teams that the user is a member of
            ITeamsHandler teamsHandler = new TeamsHandler();
            List<Team> teams = teamsHandler.GetUserTeams(username);

            //send reply to client
            ServerTCP.PACKET_SendGetUserTeamsReply(ConnectionID, teams);
        }

        private static void HandleGetUserSchedule(int ConnectionID, byte[] data)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            // Read username to buffer
            string username = bufferH.ReadString();

            bufferH.Dispose();

            // Get list of events in the schedule
            IEventScheduleConnector _eventScheduleConnector = new EventScheduleConnector();
            List<Event> eventsList = _eventScheduleConnector.GetUserSchedule(username);

            Console.WriteLine("Will show " + eventsList.Count.ToString() + "events!");
            
            //send reply to client
            ServerTCP.PACKET_SendGetUserScheduleReply(ConnectionID, eventsList);
        }

        private static void HandleGetTeamSchedule(int ConnectionID, byte[] data)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);
            int teamID = bufferH.ReadInteger();
            bufferH.Dispose();


            // Get list of events in the schedule
            IEventScheduleConnector _eventScheduleConnector = new EventScheduleConnector();
            List<Event> eventsList = _eventScheduleConnector.GetTeamSchedule(teamID);


            Console.WriteLine("Will show " + eventsList.Count + "events!");

            //send reply to client
            ServerTCP.PACKET_SendGetTeamScheduleReply(ConnectionID, teamID, eventsList);
        }

        private static void HandleGetMergedTeamSchedule(int ConnectionID, byte[] data) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);
            int teamID = bufferH.ReadInteger();
            string startTime = bufferH.ReadString();
            string endTime = bufferH.ReadString();
            int priorityThreshold = bufferH.ReadInteger();
            bufferH.Dispose();

            TeamsHandler teamsHandler = new TeamsHandler();
            SchedulesHandler schedulesHandler = new SchedulesHandler();
            EventsHandler eventsHandler = new EventsHandler();

            //Maybe get events between start and endtime only
            List<string> members = teamsHandler.GetTeamMembers(teamID);
            List<List<Event>> allMembersEvents = new List<List<Event>>();
            foreach(string member in members) {
                List<int> memberEventsID = schedulesHandler.GetUserSchedule(member);
                List<Event> memberEvents = eventsHandler.GetEvents(memberEventsID, false, member, 0);
                allMembersEvents.Add(memberEvents);
            }

            double[,] freeTimeFreq = schedulesHandler.getMergedScheduleFreq(allMembersEvents, DateTime.Parse(startTime), DateTime.Parse(endTime), priorityThreshold);

            //send reply to client
            ServerTCP.PACKET_SendMergedSchedule(ConnectionID, teamID, freeTimeFreq);
        }

        private static void HandleFilterUserSchedule(int ConnectionID, byte[] data)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            // Read username and required priorities to buffer
            string username = bufferH.ReadString();
            bool low = bufferH.ReadBool();
            bool medium = bufferH.ReadBool();
            bool high = bufferH.ReadBool();

            bufferH.Dispose();

            // Get list of eventIds of events with required priorities in the schedule of the user
            // Get List of events from the events table in the database
            IEventsHandler eventsHandler = new EventsHandler();
            List<int> filteredEventIDs = new List<int>();
            List<Event> filteredEvents = new List<Event>();
            filteredEventIDs = eventsHandler.getFilteredUserEvents(username, low, medium, high);
            filteredEvents = eventsHandler.GetEvents(filteredEventIDs, false, username, 0);

            //send reply to client
            ServerTCP.PACKET_SendGetUserScheduleReply(ConnectionID, filteredEvents);
        }

        private static void HandleCreateTeam(int ConnectionID, byte[] data) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            string teamName = bufferH.ReadString();
            string admin = bufferH.ReadString();
            int numberOfMembers = bufferH.ReadInteger();
            List<string> members = new List<string> ();
            for(int i = 0; i < numberOfMembers; i++) {
                members.Add(bufferH.ReadString());
            }

            bufferH.Dispose();

            //create a new team
            ITeamAccountConnector teamAccountConnector = new TeamAccountConnector();
            teamAccountConnector.createTeam(ConnectionID, admin, teamName, members);
        }

        private static void HandleChangeAdminState(int ConnectionID, byte[] data) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            int teamID = bufferH.ReadInteger();
            string username = bufferH.ReadString();
            bool isNowAdmin = bufferH.ReadBool();

            bufferH.Dispose();

            //change the adminState of the user
            ITeamsHandler teamsHandler = new TeamsHandler();
            teamsHandler.ChangeAdminState(teamID, username, isNowAdmin);
        }

        private static void HandleRemoveMember(int ConnectionID, byte[] data) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            int teamID = bufferH.ReadInteger();
            string username = bufferH.ReadString();

            bufferH.Dispose();

            //remove member form the team
            ITeamEventConnector teamEventConnector = new TeamEventConnector();
            teamEventConnector.RemoveTeamMember(teamID, username, DateTime.Now);
        }

        private static void HandleAddMember(int ConnectionID, byte[] data) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            int teamID = bufferH.ReadInteger();
            string username = bufferH.ReadString();

            bufferH.Dispose();

            //Add member to the team
            ITeamAccountConnector teamAccountConnector = new TeamAccountConnector();
            teamAccountConnector.addMember(ConnectionID, teamID, username);

            //send invitations to all upcoming team events to the newly added member
            IInvitationsConnector invitationsConnector = new InvitationConnector();
            invitationsConnector.InviteNewMemberToEvents(username, teamID);
        }

        private static void HandleCreateTeamEvent(int ConnectionID, byte[] data)
        {
            Console.WriteLine("Server is adding the team event");

            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            int teamID = bufferH.ReadInteger();
            string eventPlanner = bufferH.ReadString();
            string eventName = bufferH.ReadString();
            int eventPriority = bufferH.ReadInteger();
            string eventStart = bufferH.ReadString();
            string eventEnd = bufferH.ReadString();
            string Link = bufferH.ReadString();

            bufferH.Dispose();

            //Add the event to the schedule of the planner using the connector between the events and the schedules handlers
            IEventScheduleConnector eventScheduleConnector = new EventScheduleConnector();
            KeyValuePair<Event, List<Event>> pair = eventScheduleConnector.AddUserEvent(eventPlanner, eventPriority, eventPlanner, eventName, DateTime.Parse(eventStart), DateTime.Parse(eventEnd), true, Link);
            //Add the event to the teams schedule and send invitations to team members using the connector between team and events handlers
            ITeamEventConnector teamEventConnector = new TeamEventConnector();
            teamEventConnector.CreateTeamEvent(teamID, pair.Key);

            //Send reply to client
            ServerTCP.PACKET_CreateUserEventReply(ConnectionID, pair);
        }

        private static void HandleCreateUserEvent(int ConnectionID, byte[] data)
        {
            Console.WriteLine("Server is adding the event");

            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            string username = bufferH.ReadString();
            string eventName = bufferH.ReadString();
            string eventStart = bufferH.ReadString();
            string eventEnd = bufferH.ReadString();
            int eventPriority = bufferH.ReadInteger();
            string Link = bufferH.ReadString();

            bufferH.Dispose();

            //Add an event to the user's schedule
            IEventScheduleConnector eventScheduleConnector = new EventScheduleConnector();
            KeyValuePair<Event, List<Event>> pair = eventScheduleConnector.AddUserEvent(username, eventPriority, username, eventName, DateTime.Parse(eventStart), DateTime.Parse(eventEnd), false, Link);

            //Send reply to client
            ServerTCP.PACKET_CreateUserEventReply(ConnectionID, pair);
        }
       
        private static void HandleGetUserEvent(int ConnectionID, byte[] data)
        {

            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            int eventID = bufferH.ReadInteger();
            string username = bufferH.ReadString();
            bufferH.Dispose();

            //Get the details of a single event
            IEventScheduleConnector eventScheduleConnector = new EventScheduleConnector();
            eventScheduleConnector.GetUserEventInDetail(eventID, username);
        } 

        private static void HandleCancelUserEvent(int ConnectionID, byte[] data)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            string username = bufferH.ReadString();
            int eventID = bufferH.ReadInteger();
            bool isTeamEvent = bufferH.ReadBool();

            bufferH.Dispose();
            //Cancel the event in the user's schedule
            IEventScheduleConnector eventScheduleConnector = new EventScheduleConnector();
            eventScheduleConnector.CancelUserEvent(username, eventID, isTeamEvent);
        }

        private static void HandleModifyUserEvent(int ConnectionID, byte[] data)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            int eventID = bufferH.ReadInteger();
            string eventName = bufferH.ReadString();
            string start = bufferH.ReadString();
            string end = bufferH.ReadString();
            int priority = bufferH.ReadInteger();
            string plannerUsername = bufferH.ReadString();
            string username = bufferH.ReadString();
            string link = bufferH.ReadString();
            bool isteamEvent = bufferH.ReadBool();
            bufferH.Dispose();

            Event updatedEvent = new Event(eventID, priority, plannerUsername, eventName, DateTime.Parse(start), DateTime.Parse(end), isteamEvent, link);

            //Modify the event
            IEventScheduleConnector _eventScheduleConnector= new EventScheduleConnector();
            _eventScheduleConnector.ModifyUserEvent(updatedEvent, username);


        }

        #region Invitations
        private static void HandleGetUserInvitations(int ConnectionID, byte[]data)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);
            string username = bufferH.ReadString();
            bufferH.Dispose();

            IInvitationsHandler invitationsHandler = new InvitationsHandler();
            IEventsHandler eventsHandler = new EventsHandler();

            // Get list of invitation IDs sent to the user
            List<int> invitationIDs = invitationsHandler.GetUserInvitationsIDs(username);

            // get a list of invitation objects containing the details of the invitations sent to the user
            List<Invitation> invitations = InvitationsStorage.GetInvitations(invitationIDs);

            //make a list of eventIDs
            List<int> eventIDs = new List<int>();
            foreach (Invitation invitation in invitations)
            {
                eventIDs.Add(invitation.eventID);
            }
            // Get list of eventobjects containing the details of the events the user is invited to
            List<Event> eventsList = eventsHandler.GetEvents(eventIDs, true, " ", 0);

            // Get event priority from isTeamAttendee

            //send reply to client
            ServerTCP.PACKET_SendGetUserInvitationsReply(ConnectionID, invitations, eventsList);
        }

        private static void HandleAcceptInvitationReply(int ConnectionID, byte[] Data)
        {
            Console.WriteLine("Server handling accept invitation");

            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(Data);
            string username = bufferH.ReadString();
            int invitationID = bufferH.ReadInteger();
            int eventID = bufferH.ReadInteger();
            int teamID = bufferH.ReadInteger();

            //Accept the invitation
            IInvitationsConnector invitationsConnector = new InvitationConnector();
            Event acceptedEvent = invitationsConnector.AcceptInvitation(username, invitationID, eventID, teamID);

            //send reply to client
            ServerTCP.PACKET_AcceptInvitationReply(ConnectionID, acceptedEvent);
        }

        private static void HandleDeclineInvitationReply(int ConnectionID, byte[] Data)
        {
            Console.WriteLine("Server handling decline invitation");

            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(Data);
            string username = bufferH.ReadString();
            int invitationID = bufferH.ReadInteger();

            //Decline the invitation
            IInvitationsConnector invitationsConnector = new InvitationConnector();
            invitationsConnector.DeclineInvitation(username, invitationID);
        }
        #endregion

        private static void HandleCancelTeamEvent(int ConnectionID, byte[] data)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            int eventID = bufferH.ReadInteger();

            bufferH.Dispose();


            // remove event from all tables, remove invitations to this event
            IEventsHandler eventsHandler = new EventsHandler();
            eventsHandler.CancelEvent(eventID);

        }

        private static void HandleModifyTeamEvent(int ConnectionID, byte[] data)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            int teamID = bufferH.ReadInteger();
            int eventID = bufferH.ReadInteger();
            int priority = bufferH.ReadInteger();
            string eventName = bufferH.ReadString();
            string start = bufferH.ReadString();
            string end = bufferH.ReadString();
           string planner = bufferH.ReadString();
            string Link = bufferH.ReadString();

            bufferH.Dispose();

            Event updatedEvent = new Event(eventID, priority, planner, eventName, DateTime.Parse(start), DateTime.Parse(end), true, Link);
            Console.WriteLine("modifying event with name = " + eventName);

            // update team Event
            IEventScheduleConnector _eventScheduleConnector = new EventScheduleConnector();
            _eventScheduleConnector.ModifyTeamEvent(teamID, updatedEvent);

        }

    }
}
