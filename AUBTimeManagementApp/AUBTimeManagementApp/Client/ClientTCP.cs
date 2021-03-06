﻿using System;
using System.Net.Sockets;
using AUBTimeManagementApp.DataContracts;


namespace AUBTimeManagementApp.Client {
    class ClientTCP {
        private static TcpClient ClientSocket;
        private static NetworkStream myStream;
        private static byte[] receiveBuffer;

        public static void InitializeClientSocket(string address, int port) {
            ClientSocket = new TcpClient();
            ClientSocket.ReceiveBufferSize = 4096;
            ClientSocket.SendBufferSize = 4096;
            receiveBuffer = new byte[4096 * 2]; //*2 because we are sending and receiving data at the same time          
            ClientSocket.BeginConnect(address, port, ClientConnectCallBack, ClientSocket);
        }

        //Is called when we connect with the server
        private static void ClientConnectCallBack(IAsyncResult result) {
            ClientSocket.EndConnect(result);
            ClientSocket.NoDelay = true;
            myStream = ClientSocket.GetStream();
            myStream.BeginRead(receiveBuffer, 0, 4096 * 2, ReceiveCallBack, null);
            if (ClientSocket.Connected == false) { return; }
        }
        private static void ReceiveCallBack(IAsyncResult result) {
            if (ClientSocket == null || myStream == null) { return; }
            int readBytes = myStream.EndRead(result);
            if (readBytes <= 0) { return; }

            byte[] newBytes = new byte[readBytes];
            Array.Copy(receiveBuffer, newBytes, readBytes);
            ClientHandleData.HandleData(newBytes);
            if (ClientSocket == null || myStream == null) { return; }
            myStream.BeginRead(receiveBuffer, 0, 4096 * 2, ReceiveCallBack, null);
        }
        public static void SendData(byte[] data) {
            if (!ClientSocket.Connected) {
                ClientSocket.Close();
                throw new Exception();
            }
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((data.GetUpperBound(0) - data.GetLowerBound(0) + 1));
            bufferH.WriteBytes(data);
            byte[] tmp = bufferH.ToArray();
            myStream.Write(tmp, 0, tmp.Length);
            bufferH.Dispose();
        }

        public static void PACKET_Register(string username, string firstName, string lastName, string password, string confirmPassword, string email, DateTime dateOfBirth)
        {
            BufferHelper bufferH = new BufferHelper();
            //Writes the function ID so the server knows this is PACKAGE_Login and handles it accordingly
            bufferH.WriteInteger((int)ClientPackages.CRegister);
            int day = dateOfBirth.Day, month = dateOfBirth.Month, year = dateOfBirth.Year;

            // Write username and password on buffer
            bufferH.WriteString(username);
            bufferH.WriteString(firstName);
            bufferH.WriteString(lastName);
            bufferH.WriteString(password);
            bufferH.WriteString(confirmPassword);
            bufferH.WriteString(email);
            bufferH.WriteInteger(day);
            bufferH.WriteInteger(month);
            bufferH.WriteInteger(year);

            //Sends it to the server
            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_Login(string username, string password) {
            BufferHelper bufferH = new BufferHelper();
            //Writes the function ID so the server knows this is PACKAGE_Login and handles it accordingly
            bufferH.WriteInteger((int)ClientPackages.CLogin);

            // Write username and password on buffer
            bufferH.WriteString(username);
            bufferH.WriteString(password);

            //Sends it to the server
            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        

        public static void PACKET_GetEventInDetail(int eventId, string username)
        {
            BufferHelper bufferH = new BufferHelper();

            //Writes the function ID so the server knows this is PACKAGE_Login and handles it accordingly
            bufferH.WriteInteger((int)ClientPackages.CGetUserEvent);

            // Write username and password on buffer

            bufferH.WriteInteger(eventId);
            bufferH.WriteString(username);
            //Sends it to the server
            SendData(bufferH.ToArray());
            bufferH.Dispose();

        }

        public static void PACKET_GetUserInvitations(string username)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CGetUserInvitations);
            bufferH.WriteString(username);
            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }
        public static void PACKET_GetUserTeams(string username) 
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CGetUserTeams);
            bufferH.WriteString(username);
            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_GetUserSchedule(string username)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CGetUserSchedule);
            bufferH.WriteString(username);
            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_GetTeamSchedule(int teamID)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CGetTeamSchedule);
            bufferH.WriteInteger(teamID);
            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }
        public static void PACKET_GetMergedTeamSchedule(int teamID, DateTime startTime, DateTime endTime, int priorityThreshold) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CGetMergedTeamSchedule);
            bufferH.WriteInteger(teamID);
            bufferH.WriteString(startTime.ToString());
            bufferH.WriteString(endTime.ToString());
            bufferH.WriteInteger(priorityThreshold);
            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_FilterUserSchedule(string username, bool low, bool medium, bool high)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CFilterUserSchedule);
           
            // write username and required priorities on buffer
            bufferH.WriteString(username);
            bufferH.WriteBool(low);
            bufferH.WriteBool(medium);
            bufferH.WriteBool(high);

            // send it to the user
            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_CreateTeam(string teamName, string admin, string[] members) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CCreateTeam);

            bufferH.WriteString(teamName);
            bufferH.WriteString(admin);
            bufferH.WriteInteger(members.Length);
            
            foreach (string m in members){
                bufferH.WriteString(m);
            }

            //Sends it to the server
            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_ChangeAdminState(int teamID, string username, bool isNowAdmin) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CChangeAdminState);

            bufferH.WriteInteger(teamID);
            bufferH.WriteString(username);
            bufferH.WriteBool(isNowAdmin);

            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_RemoveMember(int teamID, string username) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CRemoveMember);

            bufferH.WriteInteger(teamID);
            bufferH.WriteString(username);

            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_AddMember(int teamID, string username) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CAddMember);

            bufferH.WriteInteger(teamID);
            bufferH.WriteString(username);

            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_CreateTeamEvent(int teamID, string eventPlanner, string eventName, int priority, DateTime startDate, DateTime endDate, string Link)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CCreateTeamEvent);

            bufferH.WriteInteger(teamID);
            bufferH.WriteString(eventPlanner);
            bufferH.WriteString(eventName);
            bufferH.WriteInteger(priority);
            bufferH.WriteString(startDate.ToString());
            bufferH.WriteString(endDate.ToString());
            bufferH.WriteString(Link);

            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }
        public static void PACKET_CreateUserEvent(string username, string eventName, int priority, DateTime start, DateTime end, string Link)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CCreateUserEvent);

            bufferH.WriteString(username);
            bufferH.WriteString(eventName);
            bufferH.WriteString(start.ToString());
            bufferH.WriteString(end.ToString());
            bufferH.WriteInteger(priority);
            bufferH.WriteString(Link);

            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_CancelUserEvent(string username, int eventID, bool isTeamEvent)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CCancelUserEvent);

            bufferH.WriteString(username);
            bufferH.WriteInteger(eventID);
            bufferH.WriteBool(isTeamEvent);

            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void Packet_ModifyUserEvent(Event updatedEvent, string username)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CModifyUserEvent);

            bufferH.WriteInteger(updatedEvent.ID);
            bufferH.WriteString(updatedEvent.eventName);
            bufferH.WriteString(updatedEvent.startTime.ToString());
            bufferH.WriteString(updatedEvent.endTime.ToString());
            bufferH.WriteInteger(updatedEvent.priority);
            bufferH.WriteString(updatedEvent.plannerUsername);
            bufferH.WriteString(username);
            bufferH.WriteString(updatedEvent.link);
            bufferH.WriteBool(updatedEvent.teamEvent);

            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_AcceptInvitation(Invitation invitation, string username)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CAcceptInvitation);

            bufferH.WriteString(username);
            bufferH.WriteInteger(invitation.invitationID);
            bufferH.WriteInteger(invitation.Event.ID);
            bufferH.WriteInteger(invitation.TeamID);

            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_DeclineInvitation(Invitation invitation, string username)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CDeclineInvitation);

            bufferH.WriteString(username);
            bufferH.WriteInteger(invitation.invitationID);

            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void PACKET_CancelTeamEvent(int eventID)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CCancelTeamEvent);

            bufferH.WriteInteger(eventID);


            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }

        public static void Packet_ModifyTeamEvent(Event updatedEvent, int teamID)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteInteger((int)ClientPackages.CModifyTeamEvent);

            bufferH.WriteInteger(teamID);
            bufferH.WriteInteger(updatedEvent.ID);
            bufferH.WriteInteger(updatedEvent.priority);
            bufferH.WriteString(updatedEvent.eventName);
            bufferH.WriteString(updatedEvent.startTime.ToString());
            bufferH.WriteString(updatedEvent.endTime.ToString());
            bufferH.WriteString(updatedEvent.plannerUsername);
            bufferH.WriteString(updatedEvent.link);


            SendData(bufferH.ToArray());
            bufferH.Dispose();
        }
    }
}
