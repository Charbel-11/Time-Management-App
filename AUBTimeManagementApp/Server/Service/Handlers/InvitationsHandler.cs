﻿using AUBTimeManagementApp.Service.Storage;
using Server.DataContracts;
using System.Collections.Generic;

namespace Server.Service.Handlers
{
    public class InvitationsHandler: IInvitationsHandler
    {
        public List<int> GetUserInvitationsIDs(string username)
        {
            List<int> invitationIDs = InvitationsStorage.GetUserInvitations(username);
            return invitationIDs;
        }
        
        // Sender username is a parameter in case later 2 admins can send 2 different invitations to same event
        public void AcceptInvitation(string username, int invitationID)
        {
            // Note that although Accept and Decline Invitation in this handler do the same thing: remove the invitation
            // We can't only use one function
            // Because we might lately decide to do more than just deleting (Maybe storing the accepted invitations)

            InvitationsStorage.RemoveUserInvitation(username, invitationID);
        }

        // This is called when the user declines an invitation
        // After finishing, the corresponding eventId should be removed from the DB
        public void DeclineInvitation(string username, int invitationID)
        {
            InvitationsStorage.RemoveUserInvitation(username, invitationID);
        }

        // This function asks the invitation storage to add invitations for invitees to an event
        public int SendInvitations(List<string> AttendeesUsernames, int eventID, int teamID, string senderUsername) {
            int invitationID = InvitationsStorage.AddInvitation(eventID, teamID, senderUsername);
            foreach (string username in AttendeesUsernames) {
                if (username == senderUsername) { continue; }
                InvitationsStorage.AddUserInvitation(username, invitationID);
            }
            return invitationID;
        }
        
        public List<Invitation> getInvitations(List<int> InvitationIDs) {
            return InvitationsStorage.GetInvitations(InvitationIDs);
        }

        // removes all invitations related to a specific team sent to the user
        public void RemoveSpecificUserInvitations(int teamID, string username)
		{
            // get list of IDs of invitations related to team with id = teamID
            List<int> invitationIDs = InvitationsStorage.getTeamInvitationIDs(teamID);
            //Remove all Invitations with ID in the list sent to the user
            foreach(int invitationID in invitationIDs)
			{
                InvitationsStorage.RemoveUserInvitation(username, invitationID);
			}

        }

        public void SendInvitationsToNewMember(List<Event> events, int teamID, string username) {
            //Get the list of invitationIDs of team events that are in the list 
            List<int> invitationIDs = new List<int>();
            foreach (Event _event in events) {
                int invitationID = InvitationsStorage.getInvitationID(_event.eventID, teamID);
                invitationIDs.Add(invitationID);

                Invitation invitation = new Invitation(invitationID, _event.eventID, teamID, _event.plannerUsername);
                if (ServerTCP.UsernameToConnectionID.TryGetValue(username, out int cID))
                    ServerTCP.PACKET_SendInvitation(cID, invitation, _event);
            }

            //Add all the invitations with ID in the invitationIDs list to the newly added member's table
            foreach (int invitationID in invitationIDs) {
                InvitationsStorage.AddUserInvitation(username, invitationID);
            }            
        }
    }
}
